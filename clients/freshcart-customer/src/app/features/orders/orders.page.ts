import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { NgbPagination } from '@ng-bootstrap/ng-bootstrap';
import { firstValueFrom } from 'rxjs';

import { ApiError } from '../../core/http/api-error.model';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner.component';
import { MoneyPipe } from '../../shared/pipes/money.pipe';
import { totalPageCount } from '../catalog/catalog.models';
import { OrdersApiService } from './orders-api.service';
import { OrderStatus, OrderSummary } from './orders.models';
import { badgeClassForStatus, labelForStatus } from './order-status';

const OrdersPageSize = 10;

type ListStatus = 'loading' | 'ready' | 'error';

@Component({
  selector: 'fc-orders-page',
  imports: [DatePipe, NgbPagination, MoneyPipe, LoadingSpinnerComponent, EmptyStateComponent],
  templateUrl: './orders.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrdersPage {
  private readonly ordersApi = inject(OrdersApiService);
  private readonly router = inject(Router);

  protected readonly pageSize = OrdersPageSize;
  protected readonly orders = signal<readonly OrderSummary[]>([]);
  protected readonly totalItemCount = signal(0);
  protected readonly pageNumber = signal(1);
  protected readonly status = signal<ListStatus>('loading');
  protected readonly error = signal<ApiError | null>(null);

  protected readonly totalPages = computed(() =>
    totalPageCount({ totalItemCount: this.totalItemCount(), pageSize: OrdersPageSize }),
  );
  protected readonly isEmpty = computed(() => this.status() === 'ready' && this.orders().length === 0);

  constructor() {
    void this.loadPage(1);
  }

  protected onPageChange(pageNumber: number): void {
    void this.loadPage(pageNumber);
  }

  protected openOrder(orderId: string): void {
    void this.router.navigate(['/orders', orderId]);
  }

  protected badgeClass(orderStatus: OrderStatus): string {
    return badgeClassForStatus(orderStatus);
  }

  protected statusLabel(orderStatus: OrderStatus): string {
    return labelForStatus(orderStatus);
  }

  private async loadPage(pageNumber: number): Promise<void> {
    this.status.set('loading');
    this.error.set(null);
    try {
      const page = await firstValueFrom(this.ordersApi.listOrders(pageNumber, OrdersPageSize));
      this.orders.set(page.items);
      this.totalItemCount.set(page.totalItemCount);
      this.pageNumber.set(page.pageNumber);
      this.status.set('ready');
    } catch (error: unknown) {
      this.error.set(ApiError.fromUnknown(error));
      this.status.set('error');
    }
  }
}
