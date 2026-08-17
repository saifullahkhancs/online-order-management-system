# Frontend Setup — OOMS Angular client

A single Angular 22 application that contains **both** the public customer storefront and the
authenticated admin console, separated by role-based routing.

* Standalone components + signals throughout — **no NgModules**
* Zoneless change detection
* Tailwind CSS v4 with a custom token layer
* Lazy-loaded routes, one chunk per page

> The API must be running first — see [`BACKEND_SETUP.md`](./BACKEND_SETUP.md) and
> [`DATABASE_SETUP.md`](./DATABASE_SETUP.md).

---

## 1. Prerequisites

| Requirement | Version |
| --- | --- |
| Node.js | **20.19+ / 22.12+** (Angular 22 requirement) |
| npm | 10+ |
| Running OOMS API | `http://localhost:5108` |

```bash
node --version   # v22.x
npm --version    # 10.x
```

The Angular CLI does not need to be installed globally — `npx ng` uses the local one.

---

## 2. Install and run

```bash
cd ooms-frontend
npm install
npm start           # → http://localhost:4200
```

`npm start` runs `ng serve`, which uses `proxy.conf.json` (already wired into `angular.json`) to
forward API traffic to the backend:

```jsonc
{
  "/api":  { "target": "http://localhost:5108", "secure": false, "changeOrigin": true },
  "/hubs": { "target": "http://localhost:5108", "secure": false, "changeOrigin": true, "ws": true }
}
```

`ws: true` on `/hubs` is what lets the SignalR WebSocket through. Because everything is proxied, the
browser only ever talks to `localhost:4200` — same origin, no CORS, and the app works unchanged
behind any reverse proxy.

**Pointing at a different API:** edit `proxy.conf.json`, not the environment files.

### Scripts

| Command | Purpose |
| --- | --- |
| `npm start` | dev server with proxy and HMR |
| `npm run build` | development build |
| `npm run build:prod` | optimised production bundle → `dist/ooms-frontend` |
| `npm run watch` | rebuild on change |
| `npm test` | unit tests (Vitest) |

---

## 3. Environments

`src/environments/environment.ts` (dev) and `environment.prod.ts` (swapped in by `fileReplacements`
during a production build):

```ts
export const environment = {
  production: false,
  apiBaseUrl: '',                 // empty = same origin = proxied
  orderHubUrl: '/hubs/orderHub',
  defaultHeadOfficeId: 1,         // storefront's head office
  currency: 'PKR',
  currencyLocale: 'en-PK',
};
```

| Key | Notes |
| --- | --- |
| `apiBaseUrl` | Keep **empty** whenever a proxy or shared origin is in play. Set an absolute origin (`https://api.example.com`) only for a cross-origin deployment — then widen the API's CORS policy to match. |
| `defaultHeadOfficeId` | The storefront starts from `GET /api/Menus/GetBranchByRestId/{id}`. Change this if your head office row is not id 1. |
| `currency` / `currencyLocale` | Feed `Intl.NumberFormat` in the `money` pipe. |

---

## 4. Project structure

```
src/app/
├── core/                          singletons — imported by features, never the reverse
│   ├── models/                    API contracts (api, auth, catalog, cart, order)
│   ├── services/
│   │   ├── api.service.ts         HttpClient wrapper; unwraps the ApiResponse envelope
│   │   ├── auth.service.ts        session signal, login/logout, role helpers
│   │   ├── storage.service.ts     safe localStorage access
│   │   ├── toast.service.ts       global notifications
│   │   ├── menu.service.ts        anonymous catalogue reads
│   │   ├── cart.service.ts        cart state + server sync
│   │   ├── order.service.ts       checkout, tracking, admin order ops
│   │   ├── admin.service.ts       all admin CRUD
│   │   └── live-order.service.ts  SignalR client
│   ├── guards/auth.guard.ts       authGuard, adminGuard, roleGuard, guestOnlyGuard
│   ├── interceptors/              attaches the Bearer token, handles 401
│   └── utils/format.ts            money/when/ago/status pipes, trackingSteps()
│
├── shared/components/
│   ├── ui.components.ts           Spinner, Loading, EmptyState, ErrorState, Img, Modal, Confirm
│   └── toast-host.component.ts
│
├── features/
│   ├── shop/                      storefront (anonymous)
│   │   ├── shop-shell.ts          header, branch pill, cart badge, footer
│   │   ├── branch-context.service.ts
│   │   ├── components/product-sheet.component.ts
│   │   └── pages/                 home, branches, menu, cart, checkout, my-orders, track-order
│   ├── auth/login.page.ts
│   └── admin/                     console (role-gated)
│       ├── admin-shell.ts         sidebar / drawer / account menu
│       ├── admin.routes.ts
│       └── pages/                 orders, products, categories, branch-menu,
│                                  modifiers, branches, taxes, users
│
├── app.config.ts                  providers: zoneless, router, http + interceptor
├── app.routes.ts                  top-level lazy routes
└── app.ts                         root component
```

---

## 5. Routes

### Storefront — anonymous, guest checkout supported

| Path | Page |
| --- | --- |
| `/` | Home — hero, featured categories, branch prompt |
| `/branches` | Branch picker (required before ordering) |
| `/menu` | Menu; deep links `?category=<id>` and `?product=<id>` |
| `/cart` | Cart with quantity controls and live tax breakdown |
| `/checkout` | Order type, contact/address, review, place order |
| `/orders` | Recent orders on this device + "track by number" |
| `/orders/:orderId` | Tracking; `?placed=1` shows the success banner |

### Admin — `adminGuard` (`SystemAdmin`, `SuperAdmin`, `Admin`, `OrderTaker`)

| Path | Page | Extra role gate |
| --- | --- | --- |
| `/admin/orders` | Live order board + full history | — |
| `/admin/products` | Product catalogue (image upload) | catalogue roles |
| `/admin/categories` | Categories (image upload) | catalogue roles |
| `/admin/menu` | Publish products per branch, branch pricing | catalogue roles |
| `/admin/modifiers` | Modifier and add-on groups | catalogue roles |
| `/admin/branches` | Branches + addresses | catalogue roles |
| `/admin/taxes` | Tax rules and branch assignment | catalogue roles |
| `/admin/users` | Users, role assignment, password reset | catalogue roles |

"Catalogue roles" = `SystemAdmin`, `SuperAdmin`, `Admin`. An `OrderTaker` sees only the order board —
the sidebar filters itself by role so nobody is offered a page the API would reject with a 403.

`/auth/login` is wrapped in `guestOnlyGuard`, which bounces already-signed-in admins to `/admin`.

Sign in with the seeded account created by the API on first run — the credentials are in
`Restaurant.Infrastructure/Persistence/SeedData/SeedData.cs` and in §4 of `BACKEND_SETUP.md`.

---

## 6. State management

No NgRx — signals plus a handful of injectable services.

```ts
// AuthService
readonly session        = this._session.asReadonly();
readonly isAuthenticated = computed(() => this._session() !== null);
readonly roles           = computed(() => this._session()?.roles ?? []);
readonly canAccessAdmin  = computed(() => this.hasAnyRole(ADMIN_ROLES));

// CartService
count() items() taxes() subTotal() grandTotal() isEmpty() loading() mutating()
```

Components read these directly in templates. Every component is `ChangeDetectionStrategy.OnPush`
and the app is **zoneless** (`provideZonelessChangeDetection()`), so signal writes are what drive
re-rendering.

### The guest session token

Anonymous shoppers get a client-generated GUID stored in `localStorage`. It is sent on every cart
call, embedded in the placed order, and reused for tracking and cancellation — the same identity
flows cart → order → tracking without an account.

### Two API quirks the client works around

**Cart quantity is absolute.** `POST /api/Cart/AddItem` upserts by `(cart, product)` and *replaces*
the quantity. `CartService.increment()`/`decrement()` therefore send the new total, never a delta,
and never remove-then-re-add.

**Checkout sends the whole order.** `place-order` ignores the server cart, so `CheckoutPage` builds
the complete `OrderDto` — items, modifiers, add-ons, taxes — from the cart snapshot. The server
re-validates every id against the branch and answers `"<Thing> Invalid at index: N"` on a mismatch,
which the UI surfaces verbatim.

**No card payment.** `OrderRepository` hardcodes a `Cash`/`Pending` payment row, so the checkout UI
deliberately offers cash only. Adding card payment is a backend change first.

---

## 7. Real-time orders

`LiveOrderService` connects to `/hubs/orderHub`, calls `JoinBranchGroup(branchId)` for each branch on
the signed-in account, and listens for `ReceiveOrder`. Automatic reconnect is configured with a
backoff of 0 / 2 / 5 / 10 / 30 seconds, and group membership is re-established after a reconnect.

The admin order board shows a pill reflecting the connection:

* **Live** (green) — connected to the hub, new orders arrive instantly
* **Polling** (grey) — hub unreachable; the board refreshes every 15 s instead

Polling is visibility-aware (paused when the tab is hidden or a modal is open), and the customer
tracking page polls order status every 20 s with the same rules, stopping at a terminal status.

> ⚠️ **The server-side broadcast is currently commented out** in
> `Restaurant.API/Controllers/OrderController.cs` (~line 39). Until those lines are uncommented the
> pill will stay on **Polling**. Nothing breaks — the fallback covers it. See §7 of
> `BACKEND_SETUP.md`.

---

## 8. Design notes

### Tokens over hardcoded values

`src/styles.css` defines the design system with Tailwind v4's `@theme`, which turns each custom
property into a real utility class:

| Token | Utilities | Value |
| --- | --- | --- |
| `--color-brand-*` | `bg-brand-500`, `text-brand-600`, `ring-brand-200` | warm orange ramp (50→900), appetising and high-contrast on white |
| `--color-ink-*` | `text-ink-500`, `bg-ink-100`, `border-ink-200` | slate neutral ramp (50→950) for text, borders and surfaces |
| `--color-success` / `warning` / `danger` / `info` | `text-danger`, `bg-success` | semantic states |
| `--radius-card` / `--radius-pill` | `rounded-card`, `rounded-pill` | 0.875rem / 999px |
| `--shadow-card` / `--shadow-pop` | `shadow-card`, `shadow-pop` | soft resting shadow; lifted modal shadow |
| `--font-sans` | inherited | Inter with a system fallback stack |

Brand orange for anything appetite-driven or actionable; slate for everything structural. Status
colours are never invented at the call site — `StatusClassPipe` maps an order status string to a
fixed badge palette so "Preparing" is the same blue everywhere.

### Component classes must use `@utility`, not `@layer components`

**This is the one Tailwind v4 rule to remember here.** A class declared inside
`@layer components { … }` cannot be referenced by `@apply` elsewhere — the build fails with
`Cannot apply unknown utility class 'btn'`. Shared base classes are therefore declared with
`@utility`:

```css
@utility btn { /* base button */ }
@utility card { /* surface */ }
@utility badge { /* pill */ }
@utility input { /* form control */ }

@layer components {
  .btn-primary { @apply btn bg-brand-500 text-white hover:bg-brand-600; }
  .btn-secondary { @apply btn bg-white text-ink-700 ring-1 ring-ink-200; }
  /* … btn-ghost, btn-danger, btn-sm, btn-lg, label, field-error,
       table-wrap, tbl, skeleton */
}
```

Variants that build on a base go in `@layer components`; anything that will itself be `@apply`-ed
must be an `@utility`. Follow the same rule for any new shared class.

### Layout and interaction

* **Mobile-first.** The storefront is a single column below `sm`, and the admin sidebar collapses
  into a slide-over drawer below `lg`. Tap targets stay ≥ 36 px.
* **Every async surface has four states** — loading (skeleton or spinner), empty, error with a
  retry, and content. `EmptyStateComponent` and `ErrorStateComponent` exist so no page invents its
  own.
* **Optimistic where it is safe.** Cart quantity changes update the UI immediately and reconcile
  with the server response; destructive actions always confirm first via `ConfirmComponent`.
* **Modals** are a single `ModalComponent` with a `modalFooter` content slot, backdrop dismissal and
  `aria-modal`. Sheets slide up from the bottom on mobile and centre on desktop.
* **Images** go through `ImgComponent`, which lazy-loads and swaps in an inline SVG placeholder on
  error — Cloudinary URLs from seed data are frequently missing in dev.
* **Accessibility.** Semantic landmarks, labelled inputs, `aria-label` on icon-only buttons, visible
  focus rings from Tailwind defaults, and status changes announced through toasts rather than colour
  alone.

### Bundle discipline

Every route is `loadComponent`, so the initial payload stays ~90 kB transfer and each page ships its
own chunk. Check with `npm run build:prod` — the CLI prints a per-chunk table.

---

## 9. Production build

```bash
npm run build:prod
# → dist/ooms-frontend/browser/
```

Two deployment shapes:

**A. Served by the API (recommended — no CORS).** Copy `dist/ooms-frontend/browser/*` into
`Restaurant.API/wwwroot/`, then in `Program.cs`:

```csharp
app.UseStaticFiles();
app.MapFallbackToFile("index.html");   // after MapControllers()
```

Keep `apiBaseUrl: ''` — same origin.

**B. Separate static host** (Nginx, S3 + CloudFront, Netlify…). Set `apiBaseUrl` in
`environment.prod.ts` to the API origin **or**, better, proxy `/api` and `/hubs` from the static host
so the bundle stays origin-agnostic. Nginx:

```nginx
location /api  { proxy_pass http://api-host:5108; }
location /hubs {
    proxy_pass http://api-host:5108;
    proxy_http_version 1.1;
    proxy_set_header Upgrade    $http_upgrade;   # required for SignalR
    proxy_set_header Connection "upgrade";
}
location / { try_files $uri $uri/ /index.html; }   # SPA fallback
```

The `try_files` fallback is mandatory — without it a hard refresh on `/admin/orders` returns 404.

---

## 10. Troubleshooting

| Symptom | Cause / fix |
| --- | --- |
| `502` / `ECONNREFUSED` on `/api/...` | The .NET API is not running on `localhost:5108`. Start it, or fix the target in `proxy.conf.json`. |
| Storefront menu is empty | No active `BranchProduct` rows for the selected branch. Publish products under **Admin → Branch menu**. |
| Branch picker is empty | `defaultHeadOfficeId` does not match a real head office row. |
| Login says the account lacks admin access | The user has no admin role. Assign one under **Admin → Users & roles**, or use the seeded `SystemAdmin`. |
| Redirected to `/auth/login` immediately after signing in | Token expired (12 h) or the clock is skewed — the API sets `ClockSkew` to zero. |
| Live pill never turns green | Expected today: the server broadcast is commented out. See §7. |
| `Cannot apply unknown utility class 'btn'` | A shared class was declared in `@layer components` instead of `@utility`. See §8. |
| 404 on refresh in production | Missing SPA fallback on the static host. See §9. |
| Images all show the placeholder | Cloudinary URLs in the demo seed are unreachable — upload real images through the admin UI. |
| Blank page, console shows a signal write error | A signal was written during change detection. Move the write into an event handler or `effect()`. |
