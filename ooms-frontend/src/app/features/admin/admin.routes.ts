import { Routes } from '@angular/router';
import { roleGuard } from '../../core/guards/auth.guard';

/** Roles that may touch the catalogue (OrderTaker is deliberately excluded). */
const CATALOG = { roles: ['SystemAdmin', 'SuperAdmin', 'Admin'] };

/**
 * Admin console. Mounted under /admin behind `adminGuard`; individual pages
 * add `roleGuard` where the backend is stricter than "any admin role".
 */
export const ADMIN_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'orders' },

  {
    path: 'orders',
    title: 'Live orders · OOMS admin',
    loadComponent: () => import('./pages/orders.page').then((m) => m.AdminOrdersPage),
  },
  {
    path: 'products',
    title: 'Products · OOMS admin',
    canActivate: [roleGuard],
    data: CATALOG,
    loadComponent: () => import('./pages/products.page').then((m) => m.AdminProductsPage),
  },
  {
    path: 'categories',
    title: 'Categories · OOMS admin',
    canActivate: [roleGuard],
    data: CATALOG,
    loadComponent: () => import('./pages/categories.page').then((m) => m.AdminCategoriesPage),
  },
  {
    path: 'menu',
    title: 'Branch menu · OOMS admin',
    canActivate: [roleGuard],
    data: CATALOG,
    loadComponent: () => import('./pages/branch-menu.page').then((m) => m.AdminBranchMenuPage),
  },
  {
    path: 'modifiers',
    title: 'Modifiers & add-ons · OOMS admin',
    canActivate: [roleGuard],
    data: CATALOG,
    loadComponent: () => import('./pages/modifiers.page').then((m) => m.AdminModifiersPage),
  },
  {
    path: 'branches',
    title: 'Branches · OOMS admin',
    canActivate: [roleGuard],
    data: CATALOG,
    loadComponent: () => import('./pages/branches.page').then((m) => m.AdminBranchesPage),
  },
  {
    path: 'taxes',
    title: 'Taxes · OOMS admin',
    canActivate: [roleGuard],
    data: CATALOG,
    loadComponent: () => import('./pages/taxes.page').then((m) => m.AdminTaxesPage),
  },
  {
    path: 'users',
    title: 'Users & roles · OOMS admin',
    canActivate: [roleGuard],
    data: CATALOG,
    loadComponent: () => import('./pages/users.page').then((m) => m.AdminUsersPage),
  },

  { path: '**', redirectTo: 'orders' },
];
