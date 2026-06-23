import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiRoutes } from '../../core/config/api-routes';
import { DeliveryTracking } from './delivery.models';

@Injectable({ providedIn: 'root' })
export class DeliveryApiService {
  private readonly httpClient = inject(HttpClient);

  getTrackingForOrder(orderId: string): Observable<DeliveryTracking> {
    return this.httpClient.get<DeliveryTracking>(ApiRoutes.delivery.byOrderId(orderId));
  }
}
