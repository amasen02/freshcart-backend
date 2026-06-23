import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiRoutes } from '../../core/config/api-routes';
import { CheckoutRequest, CheckoutResult } from './checkout.models';

@Injectable({ providedIn: 'root' })
export class CheckoutService {
  private readonly httpClient = inject(HttpClient);

  startCheckout(request: CheckoutRequest): Observable<CheckoutResult> {
    return this.httpClient.post<CheckoutResult>(ApiRoutes.basket.checkout, request);
  }
}
