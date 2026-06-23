import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Route, UrlSegment, UrlTree, provideRouter } from '@angular/router';

import { ApiRoutes } from '../config/api-routes';
import { AuthStore } from './auth.store';
import { guestGuard } from './guest.guard';

describe('guestGuard', () => {
  const route: Route = {};
  const authSegments = [new UrlSegment('auth', {}), new UrlSegment('sign-in', {})];

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

  it('allowsAnonymousVisitorsToReachTheAuthPages', async () => {
    const initialization = store.initialize();
    httpTesting
      .expectOne(ApiRoutes.account.me)
      .flush({ title: 'Authentication is required.' }, { status: 401, statusText: 'Unauthorized' });
    await initialization;

    const guardResult = TestBed.runInInjectionContext(() => guestGuard(route, authSegments));

    expect(guardResult).toBeTrue();
  });

  it('redirectsAuthenticatedUsersAwayFromTheAuthPages', async () => {
    const initialization = store.initialize();
    httpTesting.expectOne(ApiRoutes.account.me).flush({
      userId: 'b3f2c1a0-0000-4000-8000-000000000001',
      email: 'shopper@freshcart.test',
      displayName: 'Sam Shopper',
      roles: ['Customer'],
      multiFactorEnabled: false,
    });
    await initialization;

    const guardResult = TestBed.runInInjectionContext(() => guestGuard(route, authSegments));

    expect(guardResult).toBeInstanceOf(UrlTree);
    expect((guardResult as UrlTree).toString()).toBe('/');
  });
});
