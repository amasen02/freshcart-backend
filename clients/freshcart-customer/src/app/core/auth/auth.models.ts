export type AuthStatus = 'unknown' | 'authenticating' | 'authenticated' | 'anonymous';

export interface SignInCredentials {
  readonly email: string;
  readonly password: string;
  readonly multiFactorCode: string | null;
  readonly rememberMe: boolean;
}

export interface SignUpDetails {
  readonly email: string;
  readonly password: string;
  readonly displayName: string;
  readonly marketingConsent: boolean;
}
