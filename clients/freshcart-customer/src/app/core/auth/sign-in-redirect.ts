import { Router, UrlSegment, UrlTree } from '@angular/router';

const SignInRoutePath = '/auth/sign-in';

export function createSignInRedirectUrlTree(router: Router, segments: readonly UrlSegment[]): UrlTree {
  const attemptedUrl = `/${segments.map((segment) => segment.path).join('/')}`;
  return router.createUrlTree([SignInRoutePath], { queryParams: { returnUrl: attemptedUrl } });
}
