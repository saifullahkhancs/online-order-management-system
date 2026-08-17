import { Routes } from '@angular/router';
import { adminGuard, guestOnlyGuard } from './core/guards/auth.guard';

/**
 * Two top-level areas, both lazily loaded:
 *
 *   /            storefront  - anonymous, guest checkout supported
 *   /admin       console     - requires SystemAdmin|SuperAdmin|Admin|OrderTaker
 *   /auth/login  sign in
 */
export const routes: Routes = [
  {
    path: 'auth/login',
    canActivate: [guestOnlyGuard],
    title: 'Sign in · OOMS',
    loadComponent: () => import('./features/auth/login.page').then((m) => m.LoginPage),
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () => import('./features/admin/admin-shell').then((m) => m.AdminShell),
    loadChildren: () => import('./features/admin/admin.routes').then((m) => m.ADMIN_ROUTES),
  },
  {
    path: '',
    loadComponent: () => import('./features/shop/shop-shell').then((m) => m.ShopShell),
    loadChildren: () => import('./features/shop/shop.routes').then((m) => m.SHOP_ROUTES),
  },
  { path: '**', redirectTo: '' },
];
