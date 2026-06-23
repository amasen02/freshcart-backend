import { Routes } from '@angular/router';

export const authRoutes: Routes = [
  {
    path: 'sign-in',
    title: 'Sign in',
    loadComponent: () => import('./sign-in.page').then((feature) => feature.SignInPage),
  },
  {
    path: 'sign-up',
    title: 'Create account',
    loadComponent: () => import('./sign-up.page').then((feature) => feature.SignUpPage),
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'sign-in',
  },
];
