import { Routes } from '@angular/router';

export const catalogRoutes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    title: 'Catalog',
    loadComponent: () => import('./product-list.page').then((feature) => feature.ProductListPage),
  },
];
