import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { AuthStore } from '../auth/auth.store';
import { ApiRoutes } from '../config/api-routes';
import { apiErrorInterceptor } from './api-error.interceptor';
import { ApiError } from './api-error.model';

const settle = (): Promise<void> => new Promise((resolve) => setTimeout(resolve));
const waitForRetryWindow = (): Promise<void> => new Promise((resolve) => setTimeout(resolve, 450));

describe('apiErrorInterceptor', () => {
  let httpClient: HttpClient;
  let httpTesting: HttpTestingController;
  let store: InstanceType<typeof AuthStore>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([apiErrorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    httpClient = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
    store = TestBed.inject(AuthStore);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  async function authenticateStore(): Promise<void> {
    const initialization = store.initialize();
    httpTesting.expectOne(ApiRoutes.account.me).flush({
      userId: 'b3f2c1a0-0000-4000-8000-000000000001',
      email: 'shopper@freshcart.test',
      displayName: 'Sam Shopper',
      roles: ['Customer'],
      multiFactorEnabled: false,
    });
    await initialization;
  }

  it('mapsProblemDetailsResponsesIntoTypedApiErrors', async () => {
    let capturedError: unknown;
    httpClient.post('/api/orders', {}).subscribe({ error: (error: unknown) => (capturedError = error) });

    httpTesting.expectOne('/api/orders').flush(
      {
        title: 'One or more validation errors occurred.',
        detail: 'Validation failed.',
        traceId: 'trace-0001',
        validationErrors: { Quantity: ['Quantity must be positive.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await settle();

    expect(capturedError).toBeInstanceOf(ApiError);
    const apiError = capturedError as ApiError;
    expect(apiError.status).toBe(400);
    expect(apiError.title).toBe('One or more validation errors occurred.');
    expect(apiError.detail).toBe('Validation failed.');
    expect(apiError.traceId).toBe('trace-0001');
    expect(apiError.validationErrors['Quantity']).toEqual(['Quantity must be positive.']);
    expect(apiError.hasValidationErrors).toBeTrue();
  });

  it('mapsNetworkFailuresToAFriendlyApiError', async () => {
    let capturedError: unknown;
    httpClient.post('/api/orders', {}).subscribe({ error: (error: unknown) => (capturedError = error) });

    httpTesting.expectOne('/api/orders').error(new ProgressEvent('error'));
    await settle();

    const apiError = capturedError as ApiError;
    expect(apiError.status).toBe(0);
    expect(apiError.detail).toBe('Unable to reach the server. Check your connection and try again.');
  });

  it('retriesAGetRequestExactlyOnceAfterServiceUnavailable', async () => {
    let receivedBody: unknown;
    httpClient.get('/api/products').subscribe({ next: (body) => (receivedBody = body) });

    httpTesting.expectOne('/api/products').flush(null, { status: 503, statusText: 'Service Unavailable' });
    await waitForRetryWindow();

    httpTesting.expectOne('/api/products').flush({ items: [] });
    await settle();

    expect(receivedBody).toEqual({ items: [] });
  });

  it('failsTheGetRequestWhenTheSingleRetryAlsoReturnsServiceUnavailable', async () => {
    let capturedError: unknown;
    httpClient.get('/api/products').subscribe({ error: (error: unknown) => (capturedError = error) });

    httpTesting.expectOne('/api/products').flush(null, { status: 503, statusText: 'Service Unavailable' });
    await waitForRetryWindow();
    httpTesting.expectOne('/api/products').flush(null, { status: 503, statusText: 'Service Unavailable' });
    await settle();

    expect((capturedError as ApiError).status).toBe(503);
  });

  it('neverRetriesNonIdempotentRequests', async () => {
    let capturedError: unknown;
    httpClient.post('/api/basket/items', {}).subscribe({ error: (error: unknown) => (capturedError = error) });

    httpTesting
      .expectOne('/api/basket/items')
      .flush(null, { status: 503, statusText: 'Service Unavailable' });
    await waitForRetryWindow();

    httpTesting.expectNone('/api/basket/items');
    expect((capturedError as ApiError).status).toBe(503);
  });

  it('expiresTheSessionWhenAProtectedPathAnswers401', async () => {
    await authenticateStore();
    const router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);

    httpClient.get('/api/orders').subscribe({ error: () => undefined });
    httpTesting
      .expectOne('/api/orders')
      .flush({ title: 'Authentication is required.' }, { status: 401, statusText: 'Unauthorized' });
    await settle();

    expect(store.status()).toBe('anonymous');
    expect(router.navigate).toHaveBeenCalled();
  });

  it('leavesTheSessionAloneWhenSignInItselfAnswers401', async () => {
    await authenticateStore();
    const router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);

    httpClient.post(ApiRoutes.auth.signIn, {}).subscribe({ error: () => undefined });
    httpTesting
      .expectOne(ApiRoutes.auth.signIn)
      .flush({ title: 'Authentication is required.' }, { status: 401, statusText: 'Unauthorized' });
    await settle();

    expect(store.status()).toBe('authenticated');
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
