import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { ApiRoutes } from '../config/api-routes';
import { NotificationToastService } from '../notifications/toast.service';
import { AuthStore, MultiFactorCodeRequiredDetail } from './auth.store';
import { CurrentUser } from './current-user.model';

const settle = (): Promise<void> => new Promise((resolve) => setTimeout(resolve));

describe('AuthStore', () => {
  const customer: CurrentUser = {
    userId: 'b3f2c1a0-0000-4000-8000-000000000001',
    email: 'shopper@freshcart.test',
    displayName: 'Sam Shopper',
    roles: ['Customer'],
    multiFactorEnabled: false,
  };

  let httpTesting: HttpTestingController;
  let store: InstanceType<typeof AuthStore>;
  let router: Router;

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
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('initializeMarksTheSessionAnonymousAndSilentWhenTheProfileProbeReturns401', async () => {
    const initialization = store.initialize();

    httpTesting
      .expectOne(ApiRoutes.account.me)
      .flush({ title: 'Authentication is required.' }, { status: 401, statusText: 'Unauthorized' });
    await initialization;

    expect(store.status()).toBe('anonymous');
    expect(store.user()).toBeNull();
    expect(store.error()).toBeNull();
    expect(store.isAuthenticated()).toBeFalse();
  });

  it('initializeHydratesTheAuthenticatedProfileFromTheCurrentUserEndpoint', async () => {
    const initialization = store.initialize();

    httpTesting.expectOne(ApiRoutes.account.me).flush(customer);
    await initialization;

    expect(store.status()).toBe('authenticated');
    expect(store.displayName()).toBe('Sam Shopper');
    expect(store.roles()).toEqual(['Customer']);
    expect(store.hasBackofficeAccess()).toBeFalse();
  });

  it('hasBackofficeAccessIsTrueForManagerRoleHolders', async () => {
    const initialization = store.initialize();

    httpTesting.expectOne(ApiRoutes.account.me).flush({ ...customer, roles: ['Customer', 'Manager'] });
    await initialization;

    expect(store.hasBackofficeAccess()).toBeTrue();
  });

  it('signInPostsCookieModeCredentialsPrimesTheXsrfCookieAndLoadsTheProfile', async () => {
    const signInPromise = store.signIn({
      email: customer.email,
      password: 'CorrectHorse!Battery9Staple',
      multiFactorCode: null,
      rememberMe: true,
    });
    expect(store.status()).toBe('authenticating');

    const signInRequest = httpTesting.expectOne(ApiRoutes.auth.signIn);
    expect(signInRequest.request.method).toBe('POST');
    expect(signInRequest.request.body).toEqual({
      email: customer.email,
      password: 'CorrectHorse!Battery9Staple',
      multiFactorCode: null,
      useCookie: true,
      rememberMe: true,
    });
    signInRequest.flush({ profile: customer, accessToken: null, refreshToken: null, accessTokenExpiresOnUtc: null });
    await settle();

    httpTesting
      .expectOne(ApiRoutes.auth.antiForgeryToken)
      .flush(null, { status: 204, statusText: 'No Content' });
    await settle();

    httpTesting.expectOne(ApiRoutes.account.me).flush(customer);
    await expectAsync(signInPromise).toBeResolvedTo(true);

    expect(store.status()).toBe('authenticated');
    expect(store.user()).toEqual(customer);
    expect(store.error()).toBeNull();
  });

  it('signInSurfacesTheProblemDetailsErrorWhenCredentialsAreRejected', async () => {
    const signInPromise = store.signIn({
      email: customer.email,
      password: 'wrong',
      multiFactorCode: null,
      rememberMe: false,
    });

    httpTesting
      .expectOne(ApiRoutes.auth.signIn)
      .flush(
        { title: 'Bad request.', detail: 'Email or password is incorrect.' },
        { status: 400, statusText: 'Bad Request' },
      );

    await expectAsync(signInPromise).toBeResolvedTo(false);
    expect(store.status()).toBe('anonymous');
    expect(store.error()?.detail).toBe('Email or password is incorrect.');
    expect(store.multiFactorChallengeRequired()).toBeFalse();
  });

  it('signInSignalsTheMultiFactorChallengeWhenTheServerRequiresACode', async () => {
    const signInPromise = store.signIn({
      email: customer.email,
      password: 'CorrectHorse!Battery9Staple',
      multiFactorCode: null,
      rememberMe: false,
    });

    httpTesting
      .expectOne(ApiRoutes.auth.signIn)
      .flush(
        { title: 'Bad request.', detail: MultiFactorCodeRequiredDetail },
        { status: 400, statusText: 'Bad Request' },
      );

    await expectAsync(signInPromise).toBeResolvedTo(false);
    expect(store.multiFactorChallengeRequired()).toBeTrue();
  });

  it('signUpRegistersWithImmediateCookieSignInAndHydratesTheProfile', async () => {
    const signUpPromise = store.signUp({
      email: customer.email,
      password: 'CorrectHorse!Battery9Staple',
      displayName: customer.displayName,
      marketingConsent: true,
    });

    const signUpRequest = httpTesting.expectOne(ApiRoutes.auth.signUp);
    expect(signUpRequest.request.body).toEqual({
      email: customer.email,
      password: 'CorrectHorse!Battery9Staple',
      displayName: customer.displayName,
      marketingConsent: true,
      signInImmediately: true,
      useCookie: true,
    });
    signUpRequest.flush({ userId: customer.userId, email: customer.email, displayName: customer.displayName });
    await settle();

    httpTesting
      .expectOne(ApiRoutes.auth.antiForgeryToken)
      .flush(null, { status: 204, statusText: 'No Content' });
    await settle();

    httpTesting.expectOne(ApiRoutes.account.me).flush(customer);
    await expectAsync(signUpPromise).toBeResolvedTo(true);

    expect(store.status()).toBe('authenticated');
  });

  it('signOutPostsToTheServerResetsStateAndNavigatesHome', async () => {
    const initialization = store.initialize();
    httpTesting.expectOne(ApiRoutes.account.me).flush(customer);
    await initialization;

    const navigateSpy = spyOn(router, 'navigateByUrl').and.resolveTo(true);
    const signOutPromise = store.signOut();

    httpTesting.expectOne(ApiRoutes.auth.signOut).flush(null, { status: 204, statusText: 'No Content' });
    await signOutPromise;

    expect(store.status()).toBe('anonymous');
    expect(store.user()).toBeNull();
    expect(navigateSpy).toHaveBeenCalledWith('/');
  });

  it('signOutResetsLocalStateEvenWhenTheServerCallFails', async () => {
    const initialization = store.initialize();
    httpTesting.expectOne(ApiRoutes.account.me).flush(customer);
    await initialization;

    const navigateSpy = spyOn(router, 'navigateByUrl').and.resolveTo(true);
    const signOutPromise = store.signOut();

    httpTesting
      .expectOne(ApiRoutes.auth.signOut)
      .flush({ title: 'An unexpected error occurred.' }, { status: 500, statusText: 'Internal Server Error' });

    await expectAsync(signOutPromise).toBeResolved();
    expect(store.status()).toBe('anonymous');
    expect(navigateSpy).toHaveBeenCalledWith('/');
  });

  it('handleSessionExpiredResetsTheSessionToastsAndRedirectsToSignIn', async () => {
    const initialization = store.initialize();
    httpTesting.expectOne(ApiRoutes.account.me).flush(customer);
    await initialization;

    const toastService = TestBed.inject(NotificationToastService);
    const navigateSpy = spyOn(router, 'navigate').and.resolveTo(true);

    store.handleSessionExpired();

    expect(store.status()).toBe('anonymous');
    expect(store.user()).toBeNull();
    expect(toastService.toasts().length).toBe(1);
    expect(toastService.toasts()[0]?.kind).toBe('info');
    expect(navigateSpy).toHaveBeenCalledWith(['/auth/sign-in'], { queryParams: { returnUrl: router.url } });
  });

  it('handleSessionExpiredIsANoOpForVisitorsWhoNeverAuthenticated', () => {
    const navigateSpy = spyOn(router, 'navigate').and.resolveTo(true);
    const toastService = TestBed.inject(NotificationToastService);

    store.handleSessionExpired();

    expect(store.status()).toBe('unknown');
    expect(toastService.toasts().length).toBe(0);
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('clearErrorRemovesTheSurfacedError', async () => {
    const signInPromise = store.signIn({
      email: customer.email,
      password: 'wrong',
      multiFactorCode: null,
      rememberMe: false,
    });
    httpTesting
      .expectOne(ApiRoutes.auth.signIn)
      .flush({ title: 'Bad request.', detail: 'Email or password is incorrect.' }, { status: 400, statusText: 'Bad Request' });
    await signInPromise;
    expect(store.error()).not.toBeNull();

    store.clearError();

    expect(store.error()).toBeNull();
  });
});
