import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';

import { AuthStore } from './auth.store';
import { createSignInRedirectUrlTree } from './sign-in-redirect';

export const authGuard: CanMatchFn = (route, segments) => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  return authStore.isAuthenticated() ? true : createSignInRedirectUrlTree(router, segments);
};
