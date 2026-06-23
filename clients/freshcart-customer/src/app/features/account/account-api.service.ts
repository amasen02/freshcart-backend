import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiRoutes } from '../../core/config/api-routes';
import { EnrollMultiFactorResponse, VerifyMultiFactorResponse } from './account.models';

@Injectable({ providedIn: 'root' })
export class AccountApiService {
  private readonly httpClient = inject(HttpClient);

  enrollMultiFactor(): Observable<EnrollMultiFactorResponse> {
    return this.httpClient.post<EnrollMultiFactorResponse>(ApiRoutes.account.mfaEnroll, {});
  }

  verifyMultiFactor(verificationCode: string): Observable<VerifyMultiFactorResponse> {
    return this.httpClient.post<VerifyMultiFactorResponse>(ApiRoutes.account.mfaVerify, { verificationCode });
  }

  disableMultiFactor(verificationCode: string): Observable<void> {
    return this.httpClient.post<void>(ApiRoutes.account.mfaDisable, { verificationCode });
  }
}
