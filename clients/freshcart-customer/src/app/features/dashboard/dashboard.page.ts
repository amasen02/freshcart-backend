import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ApiError } from '../../core/http/api-error.model';
import { NotificationsStore } from '../../core/realtime/notifications.store';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner.component';
import { MoneyPipe } from '../../shared/pipes/money.pipe';
import { DoughnutChartData, DoughnutChartComponent } from './doughnut-chart.component';
import { KpiCardComponent } from './kpi-card.component';
import { LineChartComponent, LineChartSeries } from './line-chart.component';
import { DashboardApiService } from './dashboard-api.service';
import { DashboardData } from './dashboard.models';

const RevenueDatasetLabel = 'Net revenue';
const DashboardCurrencyCode = 'USD';

@Component({
  selector: 'fc-dashboard-page',
  imports: [
    MoneyPipe,
    EmptyStateComponent,
    LoadingSpinnerComponent,
    KpiCardComponent,
    LineChartComponent,
    DoughnutChartComponent,
  ],
  templateUrl: './dashboard.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPage {
  private readonly dashboardApi = inject(DashboardApiService);
  private readonly notificationsStore = inject(NotificationsStore);

  protected readonly data = signal<DashboardData | null>(null);
  protected readonly error = signal<ApiError | null>(null);
  protected readonly isRefreshing = signal(false);
  protected readonly isInitialLoad = computed(() => this.data() === null && this.error() === null);

  // Solid once the notifications channel reports a live connection; mirrors the hub state directly.
  protected readonly isLive = this.notificationsStore.isConnected;

  protected readonly revenueSeries = computed<LineChartSeries>(() => {
    const points = this.data()?.timeSeries.points ?? [];
    return {
      labels: points.map((point) => point.day),
      values: points.map((point) => point.netRevenue),
      datasetLabel: RevenueDatasetLabel,
    };
  });

  protected readonly categoryBreakdown = computed<DoughnutChartData>(() => {
    const rows = this.data()?.breakdown.byCategory ?? [];
    return {
      labels: rows.map((row) => row.categoryName),
      values: rows.map((row) => row.netRevenue),
      title: 'Revenue by category',
    };
  });

  protected readonly currencyCode = DashboardCurrencyCode;

  constructor() {
    void this.refresh();

    let lastSeenTick = this.notificationsStore.dashboardTick();
    effect(() => {
      const tick = this.notificationsStore.dashboardTick();
      if (tick !== lastSeenTick) {
        lastSeenTick = tick;
        untracked(() => void this.refresh());
      }
    });
  }

  async refresh(): Promise<void> {
    if (this.isRefreshing()) {
      return;
    }
    this.isRefreshing.set(true);
    try {
      const dashboard = await firstValueFrom(this.dashboardApi.loadDashboard());
      this.data.set(dashboard);
      this.error.set(null);
    } catch (caught: unknown) {
      if (this.data() === null) {
        this.error.set(ApiError.fromUnknown(caught));
      }
    } finally {
      this.isRefreshing.set(false);
    }
  }
}
