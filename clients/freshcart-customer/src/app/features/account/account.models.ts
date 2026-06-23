export interface EnrollMultiFactorResponse {
  readonly sharedKey: string;
  readonly authenticatorUri: string;
}

export interface VerifyMultiFactorResponse {
  readonly recoveryCodes: readonly string[];
}

export interface MultiFactorCodeRequest {
  readonly verificationCode: string;
}
