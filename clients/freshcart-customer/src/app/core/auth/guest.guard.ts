import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';

import { AuthStore } from './auth.store';

export const guestGuard: CanMatchFn = () => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  return authStore.isAuthenticated() ? router.createUrlTree(['/']) : true;
};
