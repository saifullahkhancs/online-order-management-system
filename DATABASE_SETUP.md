# Database Setup — OOMS on SQL Server

The OOMS backend is **database-first**: there are no EF Core migrations. The schema is created by a
single generated script and the scaffolded models in
`Restaurant.Infrastructure/Persistence/Models/` mirror it exactly.

**The script:** [`database/ooms-schema.sql`](./database/ooms-schema.sql) — 1,726 lines, 53 tables,
11 indexes, 60 foreign keys, plus lookup and optional demo data.

> Complete this guide **before** starting the API — see [`BACKEND_SETUP.md`](./BACKEND_SETUP.md).
> The Angular client is covered in [`FRONTEND_SETUP.md`](./FRONTEND_SETUP.md).

---

## 1. Prerequisites

Any of these will do:

| Option | Best for | Get it |
| --- | --- | --- |
| SQL Server 2019/2022 Developer or Express | Windows dev boxes | <https://www.microsoft.com/sql-server/sql-server-downloads> |
| LocalDB (ships with Visual Studio) | quickest Windows start | included in the "Data storage and processing" workload |
| `mcr.microsoft.com/mssql/server` Docker image | macOS / Linux | see §2.3 |
| Azure SQL Database | cloud / shared team DB | Azure portal |

Plus one client tool: **SQL Server Management Studio (SSMS)**, **Azure Data Studio**, or the
**`sqlcmd`** CLI.

---

## 2. Create the database

The script creates the `OOMS` database itself if it does not exist, sets `RECOVERY SIMPLE`, then
switches into it. Every object is created only when missing, so **the script is idempotent and safe
to re-run**.

### 2.1 `sqlcmd`

```bash
cd database

# SQL authentication
sqlcmd -S localhost -U sa -P "Your_Strong_Pass1" -i ooms-schema.sql

# Windows authentication
sqlcmd -S . -E -i ooms-schema.sql

# named instance / custom port
sqlcmd -S localhost,1433 -U sa -P "Your_Strong_Pass1" -i ooms-schema.sql
```

On newer `sqlcmd` (v18+) add `-C` to trust a self-signed certificate:

```bash
sqlcmd -S localhost -U sa -P "Your_Strong_Pass1" -C -i ooms-schema.sql
```

### 2.2 SSMS / Azure Data Studio

1. Connect to your server.
2. **File → Open → File…** → `database/ooms-schema.sql`.
3. Press **Execute** (F5). No database needs to be selected first — the script issues its own
   `CREATE DATABASE` / `USE`.

### 2.3 Docker (macOS / Linux)

```bash
docker run -d --name ooms-sql \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=Your_Strong_Pass1" \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest

# wait ~15s for startup, then:
docker cp database/ooms-schema.sql ooms-sql:/tmp/
docker exec -it ooms-sql /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "Your_Strong_Pass1" -C -i /tmp/ooms-schema.sql
```

Matching connection string:

```
Server=localhost,1433;Database=OOMS;User Id=sa;Password=Your_Strong_Pass1;TrustServerCertificate=True;Encrypt=False
```

### 2.4 Azure SQL

Azure SQL does not allow `CREATE DATABASE` from an arbitrary connection. Create the empty `OOMS`
database in the portal first, connect **to that database**, then run the script — it will detect the
database exists and skip straight to the tables.

---

## 3. What the script contains

| Section | Contents |
| --- | --- |
| **1. Database** | `CREATE DATABASE OOMS` if missing, `RECOVERY SIMPLE`, `USE [OOMS]` |
| **2. Tables** | 53 tables in topological (dependency) order |
| **3. Indexes** | unique constraints and lookup helper indexes |
| **4. Foreign keys** | 60 constraints, added after all tables exist |
| **5. Lookup data** | `OrderTypes`, `OrderStatuses`, `PaymentMethods`, `PaymentStatuses`, `DiscountAppliesTo`, `OrderStatusTransitionsRules`, `Roles` |
| **6. Demo data** | a head office, branch, categories, products, modifiers, add-ons, taxes and a published branch menu — **guarded by `@SeedDemoData`** |

### Skipping the demo data

Around line 1520:

```sql
DECLARE @SeedDemoData BIT = 1;
```

Set it to `0` before running for a clean, production-style database with lookups only. The block is
additionally guarded by `IF NOT EXISTS (SELECT 1 FROM dbo.[HeadOffice])`, so it never duplicates
itself on a re-run.

---

## 4. Reference data you must not change

These tables are configured `ValueGeneratedNever` in `AppDbContext` — the ids are **hardcoded in
`Restaurant.Application/Enums/CheckoutEnums.cs`** and in the Angular client. Renaming a row is fine;
renumbering one will break checkout.

### OrderTypes

| Id | Name |
| --- | --- |
| 1 | Delivery |
| 2 | Pickup |
| 3 | DineIn |
| 4 | Takeaway |

### OrderStatuses

| Id | Name | Terminal |
| --- | --- | --- |
| 1 | Pending | |
| 2 | Confirmed | |
| 3 | Preparing | |
| 4 | ReadyForPickup | |
| 5 | OutForDelivery | |
| 6 | Delivered | ✔ |
| 7 | Completed | ✔ |
| 8 | Cancelled | ✔ |
| 9 | Rejected | ✔ |
| 10 | Refunded | ✔ |

### PaymentMethods / PaymentStatuses

| Id | Method | | Id | Status |
| --- | --- | --- | --- | --- |
| 1 | Cash | | 1 | Pending |
| 2 | Card | | 2 | Authorized |
| 3 | Online | | 3 | Paid |
| 4 | Wallet | | 4 | Failed |
| 5 | Split | | 5 | Refunded |
| | | | 6 | Cancelled |

> `OrderRepository.PlaceOrderAsync` currently hardcodes the payment row to **Cash / Pending**, so
> methods 2–5 exist in the lookup but are not reachable from the storefront.

### OrderStatusTransitionsRules

This table drives `GET /api/Order/available-statuses/{orderId}`, which is what populates the status
dropdown in the admin console.

* **Delivery (type 1):** `1 → 2 → 3 → 5 → 6`
* **Pickup / DineIn / Takeaway (types 2, 3, 4):** `1 → 2 → 3 → 4 → 7`
* **Customer cancellation:** from `1` or `2` → `8`

**If this table is empty the admin status dropdown will be empty and orders can never advance.**

---

## 5. Schema overview

### Organisation

```
HeadOffice ──1:n── Branches ──1:n── BranchTimings
     │                 │
     │                 ├──1:n── BranchProduct   (which products this branch sells, at what price)
     │                 ├──1:n── BranchTaxes     (which taxes apply here)
     │                 ├──1:n── Tables          (dine-in)
     │                 └──1:n── CardMachines
     │
     ├──1:n── Categories ──n:m── Products (via CategoryProduct)
     ├──1:n── Products
     ├──1:n── ModifierCategory ──1:n── Modifier
     ├──1:n── AddOnCategory ──1:n── AddOn
     ├──1:n── Taxes
     └──1:n── Users
```

`Products` is the master catalogue; **`BranchProduct` is what makes a product orderable** at a given
branch and carries the branch-specific price. `GET /api/Menus?branchId=` only returns products with
an active `BranchProduct` row — this is the single most common reason a product "doesn't show up".

### Options

* **Modifiers** *change* an item (size, doneness). Grouped by `ModifierCategory`, which may be
  `IsRequired`. Linked to products through `ProductModifier`.
* **Add-ons** are *extras* with their own quantity (extra cheese). Grouped by `AddOnCategory`, which
  carries `MinSelect`/`MaxSelect`. Linked through `ProductAddOn`.

### Cart → Order

```
Carts ──1:n── CartItems ──1:n── CartItemModifiers
                        └──1:n── CartItemAddons

Orders ──1:n── OrderItems ──1:n── OrderItemModifiers
       │                   └──1:n── OrderItemAddons
       ├──1:n── OrderTaxes
       ├──1:n── OrderStatusHistory
       └──1:n── Payments
```

Carts are keyed by **either** `UserId` (signed-in customer) **or** `GuestSessionToken`
(a client-generated GUID). The same token carries through order placement and tracking so guests can
follow their order without an account.

Note the cart and the order are **independent**: `place-order` does not read the cart, the client
sends the full order payload and the server re-validates every id. See §6.4 of `BACKEND_SETUP.md`.

### Users

```
Users ──n:m── Roles (via UserRoles)
Users ──n:m── Branches (via UserBranches)
```

Role names are matched as literal strings by `[Authorize(Roles = …)]`: `SystemAdmin`, `SuperAdmin`,
`Admin`, `OrderTaker`.

---

## 6. Verify the install

```sql
USE OOMS;

-- 53 expected
SELECT COUNT(*) AS Tables FROM sys.tables;

-- 60 expected
SELECT COUNT(*) AS ForeignKeys FROM sys.foreign_keys;

-- lookups
SELECT * FROM dbo.OrderStatuses ORDER BY OrderStatusId;
SELECT COUNT(*) AS TransitionRules FROM dbo.OrderStatusTransitionsRules;

-- demo data (if @SeedDemoData = 1)
SELECT b.BranchId, b.BranchName, COUNT(bp.BranchProductId) AS PublishedProducts
FROM dbo.Branches b
LEFT JOIN dbo.BranchProduct bp ON bp.BranchId = b.BranchId AND bp.IsActive = 1
GROUP BY b.BranchId, b.BranchName;
```

Then point the API at it and start it — `SeedData.SeedSystemAdminAsync` will add the bootstrap
`SystemAdmin` account (`dev.aliqasim@gmail.com` / `SystemAdmin@123`) on first run if it is missing.

---

## 7. Regenerating the script or the models

### Models from the database

The EF entities are scaffolded, never hand-written. After changing the schema, regenerate them (see
§9 of `BACKEND_SETUP.md`):

```powershell
Scaffold-DbContext "Server=.;Database=OOMS;Trusted_Connection=True;TrustServerCertificate=True" `
  Microsoft.EntityFrameworkCore.SqlServer `
  -OutputDir Persistence/Models -Context AppDbContext -ContextDir Persistence -Force -NoOnConfiguring
```

### A fresh script from an existing database

In SSMS: right-click the database → **Tasks → Generate Scripts…** → select all objects → set
*Types of data to script* to **Schema and data** if you want the lookups included.

### Starting over

```sql
USE master;
ALTER DATABASE OOMS SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE OOMS;
```

Then re-run `ooms-schema.sql`.

---

## 8. Backup and restore

```sql
BACKUP DATABASE OOMS
  TO DISK = N'C:\backups\OOMS.bak'
  WITH FORMAT, INIT, COMPRESSION, STATS = 10;

RESTORE DATABASE OOMS
  FROM DISK = N'C:\backups\OOMS.bak'
  WITH REPLACE, RECOVERY;
```

The database is created with `RECOVERY SIMPLE`, which suits development (no log backups needed).
Switch to `FULL` in production if you need point-in-time restore:

```sql
ALTER DATABASE OOMS SET RECOVERY FULL;
```

---

## 9. Troubleshooting

| Symptom | Cause / fix |
| --- | --- |
| `Login failed for user 'sa'` | SQL authentication is disabled. Enable *Mixed Mode* in SSMS → Server Properties → Security, then restart the service. |
| `A network-related or instance-specific error` | TCP/IP disabled. Open SQL Server Configuration Manager → Protocols → enable **TCP/IP** → restart. |
| `SSL Provider: certificate chain … not trusted` | Add `TrustServerCertificate=True` to the connection string, or `-C` to `sqlcmd`. |
| `CREATE DATABASE permission denied` | Your login is not `dbcreator`/`sysadmin`. Create `OOMS` manually, then run the script connected to it. |
| Script runs but tables are missing | You executed only a selection. Clear the selection and press F5 again — batches are separated by `GO`. |
| `There is already an object named …` | Harmless on a re-run; the guards skip existing objects. If it recurs, the object exists with a different shape — drop and recreate the database. |
| Empty status dropdown in the admin console | `OrderStatusTransitionsRules` is empty. Re-run section 5 of the script. |
| Storefront menu is empty | No active `BranchProduct` rows for that branch. Publish products under **Branch menu** in the admin console. |
| `"Tax Invalid at index: 0"` on checkout | The tax being sent has no `BranchTaxes` row for the ordering branch. |
