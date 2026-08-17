# Backend Setup — Restaurant API (.NET 8)

The ASP.NET Core Web API that powers the Online Order Management System (OOMS). It exposes the
storefront and admin REST endpoints, issues JWTs, uploads images to Cloudinary and hosts a SignalR
hub for live order updates.

> **Read the other two guides too:** [`DATABASE_SETUP.md`](./DATABASE_SETUP.md) must be completed
> **before** the API will start, and [`FRONTEND_SETUP.md`](./FRONTEND_SETUP.md) covers the Angular
> client.

---

## 1. Prerequisites

| Requirement | Version | Notes |
| --- | --- | --- |
| .NET SDK | **8.0** or later | `dotnet --version` should print `8.x` or `9.x` |
| SQL Server | 2019+ / Express / LocalDB / Azure SQL | See `DATABASE_SETUP.md` |
| IDE | Visual Studio 2022 17.8+, Rider, or VS Code + C# Dev Kit | Optional but recommended |
| Cloudinary account | free tier is fine | Only needed for product/category image uploads |

Install the SDK from <https://dotnet.microsoft.com/download/dotnet/8.0>.

---

## 2. Solution layout

`OOMS_Solution.sln` follows Clean Architecture — dependencies point inward only.

```
OOMS_Solution.sln
├── Restaurant.API/              ← startup project (controllers, DI, SignalR hub, JWT)
│   ├── Controllers/             ← 19 controllers
│   ├── Hubs/OrderHub.cs         ← SignalR hub mapped at /hubs/orderHub
│   ├── Program.cs               ← composition root
│   └── appsettings.json         ← connection string, JWT, Cloudinary
├── Restaurant.Application/      ← DTOs, interfaces, enums, services (AuthService, UserContextService)
├── Restaurant.Domain/           ← entities, business rules — depends on nothing
└── Restaurant.Infrastructure/   ← EF Core DbContext, scaffolded models, repositories, seed data
```

**Startup project is `Restaurant.API`.** Everything else is a class library.

---

## 3. Configure `appsettings.json`

Open `Restaurant.API/appsettings.json`.

### 3.1 Connection string

```jsonc
"ConnectionStrings": {
  "DefaultConnection": "Data Source=.;Initial Catalog=OOMS;Trusted_Connection=True;TrustServerCertificate=True"
}
```

Pick the line that matches your setup:

| Scenario | Connection string |
| --- | --- |
| Local SQL Server, Windows auth | `Data Source=.;Initial Catalog=OOMS;Trusted_Connection=True;TrustServerCertificate=True` |
| Local SQL Server, SQL auth | `Server=localhost,1433;Database=OOMS;User Id=sa;Password=Your_Strong_Pass1;TrustServerCertificate=True` |
| LocalDB | `Server=(localdb)\\MSSQLLocalDB;Database=OOMS;Trusted_Connection=True` |
| Docker on macOS/Linux | `Server=localhost,1433;Database=OOMS;User Id=sa;Password=Your_Strong_Pass1;TrustServerCertificate=True;Encrypt=False` |
| Azure SQL | `Server=tcp:<srv>.database.windows.net,1433;Database=OOMS;User ID=<u>;Password=<p>;Encrypt=True` |

### 3.2 JWT

```jsonc
"Jwt": {
  "Key": "…at least 32 characters…",
  "Issuer": "RestaurantAPI",
  "Audience": "RestaurantUsers",
  "ExpireMinutes": 720            // 12 hours
}
```

`Issuer` and `Audience` are both **validated**, and `ClockSkew` is set to zero in `Program.cs`, so a
token is rejected the second it expires. Keep the key at 32+ characters or `SymmetricSecurityKey`
throws at startup.

### 3.3 Cloudinary

```jsonc
"CloudinarySettings": {
  "CloudName": "…",
  "ApiKey": "…",
  "ApiSecret": "…"
}
```

Grab these from your Cloudinary dashboard. Product images go to `restaurant/products`, category
images to `restaurant/categories`.

> ⚠️ **Security note:** the repository currently ships **real-looking JWT and Cloudinary secrets
> committed to `appsettings.json`**, and the file also contains a live-database connection string in
> a trailing comment. Before deploying anywhere public: rotate those credentials, move them to user
> secrets or environment variables, and remove them from source control.
>
> ```bash
> cd Restaurant.API
> dotnet user-secrets init
> dotnet user-secrets set "CloudinarySettings:ApiSecret" "…"
> dotnet user-secrets set "Jwt:Key" "…"
> ```
>
> In containers/App Service, use the double-underscore form:
> `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `CloudinarySettings__ApiSecret`.

---

## 4. Restore, build, run

```bash
cd /path/to/online-order-management-system

dotnet restore OOMS_Solution.sln
dotnet build   OOMS_Solution.sln -c Debug

# run the API (http profile)
dotnet run --project Restaurant.API --launch-profile http
```

| Profile | URLs |
| --- | --- |
| `http` | `http://localhost:5108` |
| `https` | `https://localhost:7131` + `http://localhost:5108` |
| IIS Express | `http://localhost:44933`, SSL `44369` |

Swagger UI (Development only): **<http://localhost:5108/swagger>**

Trust the dev certificate once if you use the https profile:

```bash
dotnet dev-certs https --trust
```

### What happens on first start

`Program.cs` runs `SeedData.SeedSystemAdminAsync(db)` inside a scope after the host is built. It
creates the `SystemAdmin` role and the bootstrap account if they are missing:

| Field | Value |
| --- | --- |
| Email | `dev.aliqasim@gmail.com` |
| Password | `SystemAdmin@123` |
| Role | `SystemAdmin` |

**Change this password immediately** — see `Restaurant.Infrastructure/Persistence/SeedData/SeedData.cs`.

---

## 5. Verify it works

```bash
# 1. Log in
curl -X POST http://localhost:5108/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"dev.aliqasim@gmail.com","password":"SystemAdmin@123"}'

# 2. Anonymous menu read (needs a branch + published products)
curl "http://localhost:5108/api/Menus?branchId=1"

# 3. Authenticated call
TOKEN="…paste data.token…"
curl http://localhost:5108/api/Branches -H "Authorization: Bearer $TOKEN"
```

---

## 6. API conventions the client relies on

### 6.1 Envelope

Almost every controller returns `ApiResponse` **and** sets the HTTP status code to match:

```jsonc
{
  "statusCode": 200,
  "message": "Success",
  "data": { /* payload */ }
}
```

`RoleController` is the exception — it returns bare payloads. The Angular `ApiService` unwraps both
shapes transparently.

### 6.2 Authentication

* `POST /api/Auth/login` → `data` contains `userId, username, email, headOfficeId, headOfficeName,
  branches[], roles[], token`.
* Send `Authorization: Bearer <token>` on protected calls.
* `JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear()` is called, so the role claim stays as
  the short name **`role`** rather than the long Microsoft URI.
* Roles: `SystemAdmin`, `SuperAdmin`, `Admin`, `OrderTaker`.

### 6.3 Anonymous endpoints (storefront)

```
GET  /api/Menus?branchId=
GET  /api/Menus/GetBranchByRestId/{restId}
GET  /api/Menus/GetProductDetails?productId=
GET  /api/Branches
GET  /api/BranchProduct
ALL  /api/Cart/*
POST /api/Order/place-order
POST /api/Order/cancel/{orderId}
GET  /api/Order/order-status/{orderId}
GET  /api/Order/GetOrderDetails/{orderId}
```

Guests are tracked with a client-generated `GuestSessionToken` GUID that flows cart → order →
tracking.

### 6.4 Two behaviours that surprise people

**Cart `AddItem` is an upsert with an absolute quantity.** `POST /api/Cart/AddItem` matches on
`(cartId, productId)` and *replaces* the quantity — it does not add to it. `UpdateItem` is commented
out in `CartRepository`, so quantity changes go through `AddItem`. Never remove-then-re-add.

**`place-order` ignores the server cart.** The client must send the complete `OrderDto`. The server
re-validates every id against the branch and throws `"<Thing> Invalid at index: N"` on a mismatch:

| Collection | Validated against |
| --- | --- |
| `orderItems[].productId` | `BranchProducts` for `dto.BranchId` |
| `itemAddons[].addonId` | `AddOns` |
| `itemModifiers[].modifierId` | `Modifiers` |
| `orderTaxes[].taxId` | `BranchTaxes` for `dto.BranchId` |

Item-level discounts and item-level taxes are accepted but ignored. `PlaceOrderAsync` also writes a
`Payments` row hardcoded to **`Cash` / `Pending`**, which is why the storefront offers no card
option.

### 6.5 Multipart endpoints

`POST`/`PUT` on **`/api/Category`** and **`/api/Products`** are `[FromForm]`: the entity fields plus
an optional `IFormFile imageFile`. Send `multipart/form-data`, not JSON. Omitting the file keeps the
existing image.

### 6.6 Order status transitions

Statuses live in `Restaurant.Application/Enums/CheckoutEnums.cs`:

| Id | Status | | Id | Status |
| --- | --- | --- | --- | --- |
| 1 | Pending | | 6 | Delivered *(terminal)* |
| 2 | Confirmed | | 7 | Completed *(terminal)* |
| 3 | Preparing | | 8 | Cancelled *(terminal)* |
| 4 | ReadyForPickup | | 9 | Rejected *(terminal)* |
| 5 | OutForDelivery | | 10 | Refunded *(terminal)* |

* Delivery: `1 → 2 → 3 → 5 → 6`
* Pickup / DineIn / Takeaway: `1 → 2 → 3 → 4 → 7`
* Customer cancellation: only from `1` or `2` → `8`

The admin UI never hardcodes these — it calls `GET /api/Order/available-statuses/{orderId}`, which
reads the `OrderStatusTransitionsRules` table. **If that table is empty the dropdown will be empty**,
so make sure the lookup seed from `DATABASE_SETUP.md` ran.

---

## 7. SignalR live orders

The hub is registered in `Program.cs`:

```csharp
builder.Services.AddSignalR();
app.MapHub<OrderHub>("/hubs/orderHub");
```

`OrderHub.JoinBranchGroup(branchId)` puts a connection into the group `branch-{id}` and echoes
`BranchRegistered` back to the caller.

**The broadcast is currently commented out.** In `Restaurant.API/Controllers/OrderController.cs`
around line 39:

```csharp
//    await _hubContext.Clients
//        .Group($"branch-{dto.BranchId}")
//        .SendAsync("ReceiveOrder", orderDtO);
```

Uncomment those lines to enable real-time pushes. The Angular admin board already listens for
`ReceiveOrder` and shows a green **Live** pill when connected; while the broadcast is disabled it
falls back to polling every 15 seconds, so nothing breaks either way.

---

## 8. CORS

`Program.cs` defines a permissive development policy:

```csharp
policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
```

`AllowCredentials()` with a reflected origin is required for SignalR. **Tighten this for
production** — replace the predicate with `.WithOrigins("https://your-frontend.com")`.

The Angular dev server proxies `/api` and `/hubs` to `localhost:5108`, so during local development
the browser sees a same-origin app and CORS never comes into play.

---

## 9. Rescaffolding the EF models (optional)

The database is **database-first**. There are no migrations — `Persistence/Models/` was generated by
`Scaffold-DbContext`. To regenerate after a schema change, run in the Package Manager Console with
*Startup project* = `Restaurant.API` and *Default project* = `Restaurant.Infrastructure`:

```powershell
Scaffold-DbContext "Server=.;Database=OOMS;Trusted_Connection=True;TrustServerCertificate=True" `
  Microsoft.EntityFrameworkCore.SqlServer `
  -OutputDir Persistence/Models -Context AppDbContext -ContextDir Persistence -Force -NoOnConfiguring
```

CLI equivalent:

```bash
dotnet ef dbcontext scaffold \
  "Server=.;Database=OOMS;Trusted_Connection=True;TrustServerCertificate=True" \
  Microsoft.EntityFrameworkCore.SqlServer \
  --output-dir Persistence/Models --context AppDbContext --context-dir Persistence \
  --force --no-onconfiguring --project Restaurant.Infrastructure --startup-project Restaurant.API
```

`-Force` **overwrites** the models folder. Any hand-edits there will be lost — keep custom logic in
partial classes or the Domain layer.

---

## 10. Publishing

```bash
dotnet publish Restaurant.API -c Release -o ./publish
```

Deployment checklist:

1. `ASPNETCORE_ENVIRONMENT=Production` (this also hides Swagger).
2. Supply `ConnectionStrings__DefaultConnection`, `Jwt__Key`, `CloudinarySettings__*` as environment
   variables or key-vault references — not in `appsettings.json`.
3. Narrow the CORS policy to your real frontend origin.
4. Rotate the seeded `SystemAdmin` password.
5. `app.UseHttpsRedirection()` is active — terminate TLS at the proxy or supply a certificate.
6. Optionally serve the built Angular bundle from `wwwroot` so the API and SPA share an origin and
   CORS becomes unnecessary. Add `app.UseStaticFiles()` and
   `app.MapFallbackToFile("index.html")`.

---

## 11. Troubleshooting

| Symptom | Cause / fix |
| --- | --- |
| `SqlException: Cannot open database "OOMS"` | Database not created. Run `database/ooms-schema.sql`. |
| `A network-related or instance-specific error` | Wrong `Data Source`, SQL Browser off, or TCP/IP disabled in SQL Server Configuration Manager. |
| `IDX10720: Unable to create KeyedHashAlgorithm` | `Jwt:Key` shorter than 32 characters. |
| `401` on every protected call | Missing `Bearer ` prefix, or the token expired (`ClockSkew` is zero). |
| `403` with a valid token | The account lacks the role the controller demands. Check `UserRoles`. |
| Login succeeds but roles are empty | No rows in `UserRoles` for that user — assign via **Users & roles** in the admin UI. |
| Image upload throws | Cloudinary credentials wrong, or the request was sent as JSON instead of `multipart/form-data`. |
| Empty status dropdown in the admin UI | `OrderStatusTransitionsRules` not seeded. |
| `"Product Invalid at index: 0"` on checkout | The product has no active `BranchProducts` row for that branch. Publish it under **Branch menu**. |
| Swagger 404 | You are running in Production; Swagger is Development-only. |
