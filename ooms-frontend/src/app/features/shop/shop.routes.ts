import { Routes } from '@angular/router';

/** Customer storefront. Every route here works without signing in. */
export const SHOP_ROUTES: Routes = [
  {
    path: '',
    title: 'Order online · OOMS',
    loadComponent: () => import('./pages/home.page').then((m) => m.HomePage),
  },
  {
    path: 'branches',
    title: 'Choose a branch · OOMS',
    loadComponent: () => import('./pages/branches.page').then((m) => m.BranchesPage),
  },
  {
    path: 'menu',
    title: 'Menu · OOMS',
    loadComponent: () => import('./pages/menu.page').then((m) => m.MenuPage),
  },
  {
    path: 'cart',
    title: 'Your cart · OOMS',
    loadComponent: () => import('./pages/cart.page').then((m) => m.CartPage),
  },
  {
    path: 'checkout',
    title: 'Checkout · OOMS',
    loadComponent: () => import('./pages/checkout.page').then((m) => m.CheckoutPage),
  },
  {
    path: 'orders',
    title: 'My orders · OOMS',
    loadComponent: () => import('./pages/my-orders.page').then((m) => m.MyOrdersPage),
  },
  {
    path: 'orders/:orderId',
    title: 'Track order · OOMS',
    loadComponent: () => import('./pages/track-order.page').then((m) => m.TrackOrderPage),
  },
];
