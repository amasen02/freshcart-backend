import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth.guard';
import { guestGuard } from './core/auth/guest.guard';
import { roleGuard } from './core/auth/role.guard';

export const appRoutes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    title: 'Fresh groceries delivered',
    loadComponent: () => import('./features/home/home.page').then((feature) => feature.HomePage),
  },
  {
    path: 'catalog',
    loadChildren: () => import('./features/catalog/catalog.routes').then((feature) => feature.catalogRoutes),
  },
  {
    path: 'product/:slug',
    loadComponent: () =>
      import('./features/catalog/product-detail.page').then((feature) => feature.ProductDetailPage),
  },
  {
    path: 'basket',
    title: 'Basket',
    loadComponent: () => import('./features/basket/basket.page').then((feature) => feature.BasketPage),
  },
  {
    path: 'checkout',
    title: 'Checkout',
    canMatch: [authGuard],
    loadComponent: () => import('./features/checkout/checkout.page').then((feature) => feature.CheckoutPage),
  },
  {
    path: 'orders',
    canMatch: [authGuard],
    loadChildren: () => import('./features/orders/orders.routes').then((feature) => feature.ordersRoutes),
  },
  {
    path: 'account',
    title: 'Account',
    canMatch: [authGuard],
    loadComponent: () => import('./features/account/account.page').then((feature) => feature.AccountPage),
  },
  {
    path: 'dashboard',
    title: 'Dashboard',
    canMatch: [authGuard, roleGuard(['Administrator', 'Manager'])],
    loadComponent: () =>
      import('./features/dashboard/dashboard.page').then((feature) => feature.DashboardPage),
  },
  {
    path: 'support/console',
    title: 'Support console',
    canMatch: [authGuard, roleGuard(['SupportAgent'])],
    loadComponent: () =>
      import('./features/support/agent-console.page').then((feature) => feature.AgentConsolePage),
  },
  {
    path: 'auth',
    canMatch: [guestGuard],
    loadChildren: () => import('./features/auth/auth.routes').then((feature) => feature.authRoutes),
  },
  {
    path: '**',
    title: 'Page not found',
    loadComponent: () =>
      import('./shared/components/not-found.component').then((feature) => feature.NotFoundComponent),
  },
];
