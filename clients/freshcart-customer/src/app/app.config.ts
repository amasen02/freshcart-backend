import { provideHttpClient, withFetch, withInterceptors, withXsrfConfiguration } from '@angular/common/http';
import {
  ApplicationConfig,
  ErrorHandler,
  inject,
  provideAppInitializer,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import {
  TitleStrategy,
  provideRouter,
  withComponentInputBinding,
  withInMemoryScrolling,
  withViewTransitions,
} from '@angular/router';

import { appRoutes } from './app.routes';
import { AuthStore } from './core/auth/auth.store';
import { GlobalErrorHandler } from './core/errors/global-error-handler';
import { apiErrorInterceptor } from './core/http/api-error.interceptor';
import { credentialsInterceptor } from './core/http/credentials.interceptor';
import { FreshcartTitleStrategy } from './core/routing/freshcart-title.strategy';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(
      appRoutes,
      withComponentInputBinding(),
      withInMemoryScrolling({ scrollPositionRestoration: 'top' }),
      withViewTransitions(),
    ),
    provideHttpClient(
      withFetch(),
      withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName: 'X-XSRF-TOKEN' }),
      withInterceptors([credentialsInterceptor, apiErrorInterceptor]),
    ),
    provideAnimationsAsync(),
    provideAppInitializer(() => inject(AuthStore).initialize()),
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    { provide: TitleStrategy, useExisting: FreshcartTitleStrategy },
  ],
};
