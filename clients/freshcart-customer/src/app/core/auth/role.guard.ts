import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';

import { AuthStore } from './auth.store';
import { createSignInRedirectUrlTree } from './sign-in-redirect';

export const roleGuard =
  (allowedRoles: readonly string[]): CanMatchFn =>
  (route, segments) => {
    const authStore = inject(AuthStore);
    const router = inject(Router);

    if (!authStore.isAuthenticated()) {
      return createSignInRedirectUrlTree(router, segments);
    }

    const hasAllowedRole = authStore.roles().some((roleName) => allowedRoles.includes(roleName));
    return hasAllowedRole ? true : router.createUrlTree(['/']);
  };
