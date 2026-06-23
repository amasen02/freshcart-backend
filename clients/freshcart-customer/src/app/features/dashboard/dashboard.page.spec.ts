import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ApiRoutes } from '../../core/config/api-routes';
import {
  RevenueBreakdown,
  SalesOverview,
  SalesTimeSeries,
  TopProducts,
} from './dashboard.models';
import { DashboardPage } from './dashboard.page';

const emptyOverview: SalesOverview = {
  currentPeriod: { fromUtc: '2026-05-19T00:00:00Z', toUtcExclusive: '2026-06-18T00:00:00Z' },
  previousPeriod: { fromUtc: '2026-04-19T00:00:00Z', toUtcExclusive: '2026-05-19T00:00:00Z' },
  current: emptySnapshot(),
  previous: emptySnapshot(),
  tiles: [
    {
      code: 'gmv',
      displayName: 'Gross merchandise value',
      currentValue: 1200,
      previousValue: 1000,
      unit: 'Currency',
      description: null,
      deltaPercentage: 20,
      trend: 'Up',
    },
  ],
};

const emptyTimeSeries: SalesTimeSeries = {
  period: { fromUtc: '2026-05-19T00:00:00Z', toUtcExclusive: '2026-06-18T00:00:00Z' },
  bucket: 'Daily',
  points: [],
};

const emptyBreakdown: RevenueBreakdown = {
  period: { fromUtc: '2026-05-19T00:00:00Z', toUtcExclusive: '2026-06-18T00:00:00Z' },
  byCategory: [],
  byPaymentMethod: [],
};

const emptyTopProducts: TopProducts = {
  period: { fromUtc: '2026-05-19T00:00:00Z', toUtcExclusive: '2026-06-18T00:00:00Z' },
  mode: 'BestSellers',
  rows: [],
};

function emptySnapshot() {
  return {
    day: '2026-06-18',
    orderCount: 0,
    uniqueCustomerCount: 0,
    grossRevenue: 0,
    discountTotal: 0,
    refundTotal: 0,
    taxTotal: 0,
    shippingTotal: 0,
    netRevenue: 0,
  };
}

describe('DashboardPage', () => {
  let httpTesting: HttpTestingController;
  let fixture: ComponentFixture<DashboardPage>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    httpTesting = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(DashboardPage);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  function flushAllReports(): void {
    httpTesting.expectOne((c) => c.url === ApiRoutes.reporting.salesOverview).flush(emptyOverview);
    httpTesting.expectOne((c) => c.url === ApiRoutes.reporting.salesTimeSeries).flush(emptyTimeSeries);
    httpTesting.expectOne((c) => c.url === ApiRoutes.reporting.salesBreakdown).flush(emptyBreakdown);
    httpTesting.expectOne((c) => c.url === ApiRoutes.reporting.topProducts).flush(emptyTopProducts);
  }

  const settle = (): Promise<void> => new Promise((resolve) => setTimeout(resolve));

  it('fetchesAllFourReportsOnFirstLoad', async () => {
    fixture.detectChanges();
    flushAllReports();
    await settle();

    expect(fixture.componentInstance['data']()).not.toBeNull();
    expect(fixture.componentInstance['isRefreshing']()).toBeFalse();
  });

  it('ignoresARefreshTriggeredWhileAnotherRefreshIsStillInFlight', async () => {
    fixture.detectChanges();

    await fixture.componentInstance.refresh();

    const reportRequests = httpTesting.match((c) => c.url.startsWith('/api/reporting'));
    expect(reportRequests.length).toBe(4);
    expect(reportRequests.filter((r) => r.request.url === ApiRoutes.reporting.salesOverview).length).toBe(1);
    reportRequests.forEach((request) => request.flush({ tiles: [], byCategory: [], byPaymentMethod: [], points: [], rows: [] }));
    await settle();
  });

  it('allowsAFreshRefreshOnceTheInFlightOneCompletes', async () => {
    fixture.detectChanges();
    flushAllReports();
    await settle();

    const followUp = fixture.componentInstance.refresh();
    flushAllReports();
    await followUp;

    expect(fixture.componentInstance['data']()).not.toBeNull();
  });
});
