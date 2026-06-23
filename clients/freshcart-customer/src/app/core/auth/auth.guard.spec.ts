import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Route, UrlSegment, UrlTree, provideRouter } from '@angular/router';

import { ApiRoutes } from '../config/api-routes';
import { authGuard } from './auth.guard';
import { AuthStore } from './auth.store';

describe('authGuard', () => {
  const route: Route = {};
  const checkoutSegments = [new UrlSegment('checkout', {})];

  let httpTesting: HttpTestingController;
  let store: InstanceType<typeof AuthStore>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    httpTesting = TestBed.inject(HttpTestingController);
    store = TestBed.inject(AuthStore);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  async function initializeSessionAs(roles: readonly string[] | null): Promise<void> {
    const initialization = store.initialize();
    const profileRequest = httpTesting.expectOne(ApiRoutes.account.me);
    if (roles === null) {
      profileRequest.flush({ title: 'Authentication is required.' }, { status: 401, statusText: 'Unauthorized' });
    } else {
      profileRequest.flush({
        userId: 'b3f2c1a0-0000-4000-8000-000000000001',
        email: 'shopper@freshcart.test',
        displayName: 'Sam Shopper',
        roles,
        multiFactorEnabled: false,
      });
    }
    await initialization;
  }

  it('allowsAuthenticatedUsersToMatchTheRoute', async () => {
    await initializeSessionAs(['Customer']);

    const guardResult = TestBed.runInInjectionContext(() => authGuard(route, checkoutSegments));

    expect(guardResult).toBeTrue();
  });

  it('redirectsAnonymousVisitorsToSignInWithTheAttemptedUrlAsReturnUrl', async () => {
    await initializeSessionAs(null);

    const guardResult = TestBed.runInInjectionContext(() => authGuard(route, checkoutSegments));

    expect(guardResult).toBeInstanceOf(UrlTree);
    const redirect = guardResult as UrlTree;
    expect(redirect.root.children['primary']?.segments.map((segment) => segment.path)).toEqual([
      'auth',
      'sign-in',
    ]);
    expect(redirect.queryParams['returnUrl']).toBe('/checkout');
  });
});
