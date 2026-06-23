import { Routes } from '@angular/router';

export const ordersRoutes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    title: 'Orders',
    loadComponent: () => import('./orders.page').then((feature) => feature.OrdersPage),
  },
  {
    path: ':orderId/confirmation',
    title: 'Order confirmation',
    loadComponent: () =>
      import('../checkout/order-confirmation.page').then((feature) => feature.OrderConfirmationPage),
  },
  {
    path: ':orderId',
    title: 'Order detail',
    loadComponent: () => import('./order-detail.page').then((feature) => feature.OrderDetailPage),
  },
];
