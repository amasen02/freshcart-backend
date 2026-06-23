import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiRoutes } from '../../core/config/api-routes';
import { PaginatedResult } from '../catalog/catalog.models';
import { CancelOrderRequest, OrderDetail, OrderSummary } from './orders.models';

@Injectable({ providedIn: 'root' })
export class OrdersApiService {
  private readonly httpClient = inject(HttpClient);

  listOrders(pageNumber: number, pageSize: number): Observable<PaginatedResult<OrderSummary>> {
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    return this.httpClient.get<PaginatedResult<OrderSummary>>(ApiRoutes.orders.root, { params });
  }

  getOrder(orderId: string): Observable<OrderDetail> {
    return this.httpClient.get<OrderDetail>(ApiRoutes.orders.byId(orderId));
  }

  cancelOrder(orderId: string, request: CancelOrderRequest): Observable<void> {
    return this.httpClient.post<void>(ApiRoutes.orders.cancel(orderId), request);
  }
}
