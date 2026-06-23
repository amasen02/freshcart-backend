import { HttpErrorResponse, HttpInterceptorFn, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, retry, throwError, timer } from 'rxjs';

import { AuthStore } from '../auth/auth.store';
import { ApiRoutes } from '../config/api-routes';
import { ApiError } from './api-error.model';

const TransientRetryCount = 1;
const ServiceUnavailableRetryDelayMs = 400;

// Sign-in and sign-up legitimately answer 401-adjacent failures for anonymous callers;
// treating those as an expired session would loop the user back to the form they are on.
const SessionExemptPaths: readonly string[] = [ApiRoutes.auth.signIn, ApiRoutes.auth.signUp];

export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const authStore = inject(AuthStore);

  const responseStream =
    request.method === 'GET'
      ? next(request).pipe(retry({ count: TransientRetryCount, delay: retryAfterServiceUnavailable }))
      : next(request);

  return responseStream.pipe(
    catchError((error: unknown) => {
      const apiError = ApiError.fromUnknown(error);
      if (apiError.status === HttpStatusCode.Unauthorized && isSessionProtectedPath(request.url)) {
        authStore.handleSessionExpired();
      }
      return throwError(() => apiError);
    }),
  );
};

function retryAfterServiceUnavailable(error: unknown): Observable<number> {
  if (error instanceof HttpErrorResponse && error.status === HttpStatusCode.ServiceUnavailable) {
    return timer(ServiceUnavailableRetryDelayMs);
  }
  return throwError(() => error);
}

function isSessionProtectedPath(url: string): boolean {
  return url.startsWith('/api/') && !SessionExemptPaths.includes(url);
}
