import { HttpClient } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';

import { ApiRoutes } from '../config/api-routes';
import { ApiError } from '../http/api-error.model';
import { NotificationToastService } from '../notifications/toast.service';
import { AuthStatus, SignInCredentials, SignUpDetails } from './auth.models';
import { CurrentUser } from './current-user.model';

// Must match the BadRequestException detail thrown by Identity's SignInCommandHandler when MFA is enabled.
export const MultiFactorCodeRequiredDetail = 'Multi-factor code is required.';

const BackofficeRoles: readonly string[] = ['Administrator', 'Manager', 'SupportAgent'];
const SessionExpiredMessage = 'Your session has expired. Sign in again to continue.';
const SignInRoutePath = '/auth/sign-in';

interface AuthState {
  user: CurrentUser | null;
  status: AuthStatus;
  error: ApiError | null;
}

const initialAuthState: AuthState = {
  user: null,
  status: 'unknown',
  error: null,
};

export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState(initialAuthState),
  withComputed(({ user, status, error }) => ({
    isAuthenticated: computed(() => status() === 'authenticated'),
    displayName: computed(() => user()?.displayName ?? ''),
    roles: computed(() => user()?.roles ?? []),
    hasBackofficeAccess: computed(() =>
      (user()?.roles ?? []).some((roleName) => BackofficeRoles.includes(roleName)),
    ),
    multiFactorChallengeRequired: computed(() => error()?.detail === MultiFactorCodeRequiredDetail),
  })),
  withMethods((store) => {
    const httpClient = inject(HttpClient);
    const router = inject(Router);
    const toastService = inject(NotificationToastService);

    const loadCurrentUserAsync = (): Promise<CurrentUser> =>
      firstValueFrom(httpClient.get<CurrentUser>(ApiRoutes.account.me));

    const primeAntiForgeryTokenAsync = (): Promise<unknown> =>
      firstValueFrom(httpClient.get(ApiRoutes.auth.antiForgeryToken, { observe: 'response' }));

    const completeSessionAsync = async (): Promise<void> => {
      await primeAntiForgeryTokenAsync();
      const currentUser = await loadCurrentUserAsync();
      patchState(store, { user: currentUser, status: 'authenticated', error: null });
    };

    return {
      async initialize(): Promise<void> {
        try {
          const currentUser = await loadCurrentUserAsync();
          patchState(store, { user: currentUser, status: 'authenticated', error: null });
        } catch {
          patchState(store, { user: null, status: 'anonymous', error: null });
        }
      },

      async signIn(credentials: SignInCredentials): Promise<boolean> {
        patchState(store, { status: 'authenticating', error: null });
        try {
          await firstValueFrom(
            httpClient.post(ApiRoutes.auth.signIn, {
              email: credentials.email,
              password: credentials.password,
              multiFactorCode: credentials.multiFactorCode,
              useCookie: true,
              rememberMe: credentials.rememberMe,
            }),
          );
          await completeSessionAsync();
          return true;
        } catch (error: unknown) {
          patchState(store, { user: null, status: 'anonymous', error: ApiError.fromUnknown(error) });
          return false;
        }
      },

      async signUp(details: SignUpDetails): Promise<boolean> {
        patchState(store, { status: 'authenticating', error: null });
        try {
          await firstValueFrom(
            httpClient.post(ApiRoutes.auth.signUp, {
              email: details.email,
              password: details.password,
              displayName: details.displayName,
              marketingConsent: details.marketingConsent,
              signInImmediately: true,
              useCookie: true,
            }),
          );
          await completeSessionAsync();
          return true;
        } catch (error: unknown) {
          patchState(store, { user: null, status: 'anonymous', error: ApiError.fromUnknown(error) });
          return false;
        }
      },

      async signOut(): Promise<void> {
        try {
          await firstValueFrom(httpClient.post(ApiRoutes.auth.signOut, {}, { observe: 'response' }));
        } catch {
          // The server session may already be gone; local sign-out must succeed regardless.
        } finally {
          patchState(store, { user: null, status: 'anonymous', error: null });
          await router.navigateByUrl('/');
        }
      },

      async refreshCurrentUser(): Promise<void> {
        const currentUser = await loadCurrentUserAsync();
        patchState(store, { user: currentUser, status: 'authenticated', error: null });
      },

      handleSessionExpired(): void {
        if (store.status() !== 'authenticated') {
          return;
        }
        patchState(store, { user: null, status: 'anonymous', error: null });
        toastService.showInfo(SessionExpiredMessage);
        void router.navigate([SignInRoutePath], { queryParams: { returnUrl: router.url } });
      },

      clearError(): void {
        patchState(store, { error: null });
      },
    };
  }),
);
