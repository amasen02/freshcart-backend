export interface CurrentUser {
  readonly userId: string;
  readonly email: string;
  readonly displayName: string;
  readonly roles: readonly string[];
  readonly multiFactorEnabled: boolean;
}
