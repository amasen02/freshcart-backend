import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin } from 'rxjs';

import { ApiRoutes } from '../../core/config/api-routes';
import {
  DashboardData,
  RevenueBreakdown,
  SalesOverview,
  SalesTimeSeries,
  TopProducts,
} from './dashboard.models';

const DefaultPreset = 'Last30Days';
const DefaultBucket = 'Daily';
const TopProductsTake = 10;

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly httpClient = inject(HttpClient);

  loadDashboard(): Observable<DashboardData> {
    return forkJoin({
      overview: this.getSalesOverview(),
      timeSeries: this.getSalesTimeSeries(),
      breakdown: this.getRevenueBreakdown(),
      topProducts: this.getTopProducts(),
    });
  }

  private getSalesOverview(): Observable<SalesOverview> {
    return this.httpClient.get<SalesOverview>(ApiRoutes.reporting.salesOverview, {
      params: new HttpParams().set('preset', DefaultPreset),
    });
  }

  private getSalesTimeSeries(): Observable<SalesTimeSeries> {
    return this.httpClient.get<SalesTimeSeries>(ApiRoutes.reporting.salesTimeSeries, {
      params: new HttpParams().set('preset', DefaultPreset).set('bucket', DefaultBucket),
    });
  }

  private getRevenueBreakdown(): Observable<RevenueBreakdown> {
    return this.httpClient.get<RevenueBreakdown>(ApiRoutes.reporting.salesBreakdown, {
      params: new HttpParams().set('preset', DefaultPreset),
    });
  }

  private getTopProducts(): Observable<TopProducts> {
    return this.httpClient.get<TopProducts>(ApiRoutes.reporting.topProducts, {
      params: new HttpParams().set('preset', DefaultPreset).set('take', TopProductsTake),
    });
  }
}
