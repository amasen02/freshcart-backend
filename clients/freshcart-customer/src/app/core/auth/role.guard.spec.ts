import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Route, UrlSegment, UrlTree, provideRouter } from '@angular/router';

import { ApiRoutes } from '../config/api-routes';
import { AuthStore } from './auth.store';
import { roleGuard } from './role.guard';

describe('roleGuard', () => {
  const route: Route = {};
  const dashboardSegments = [new UrlSegment('dashboard', {})];
  const dashboardGuard = roleGuard(['Administrator', 'Manager']);

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
        email: 'manager@freshcart.test',
        displayName: 'Morgan Manager',
        roles,
        multiFactorEnabled: false,
      });
    }
    await initialization;
  }

  it('allowsUsersHoldingOneOfTheAllowedRoles', async () => {
    await initializeSessionAs(['Customer', 'Manager']);

    const guardResult = TestBed.runInInjectionContext(() => dashboardGuard(route, dashboardSegments));

    expect(guardResult).toBeTrue();
  });

  it('redirectsAuthenticatedUsersWithoutAnAllowedRoleToHome', async () => {
    await initializeSessionAs(['Customer']);

    const guardResult = TestBed.runInInjectionContext(() => dashboardGuard(route, dashboardSegments));

    expect(guardResult).toBeInstanceOf(UrlTree);
    expect((guardResult as UrlTree).toString()).toBe('/');
  });

  it('redirectsAnonymousVisitorsToSignInWithTheAttemptedUrlAsReturnUrl', async () => {
    await initializeSessionAs(null);

    const guardResult = TestBed.runInInjectionContext(() => dashboardGuard(route, dashboardSegments));

    expect(guardResult).toBeInstanceOf(UrlTree);
    const redirect = guardResult as UrlTree;
    expect(redirect.root.children['primary']?.segments.map((segment) => segment.path)).toEqual([
      'auth',
      'sign-in',
    ]);
    expect(redirect.queryParams['returnUrl']).toBe('/dashboard');
  });
});
