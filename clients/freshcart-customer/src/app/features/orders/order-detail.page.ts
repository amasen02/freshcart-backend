import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, combineLatest, firstValueFrom, map, of, switchMap } from 'rxjs';

import { ApiError } from '../../core/http/api-error.model';
import { NotificationToastService } from '../../core/notifications/toast.service';
import { ConfirmDialogService } from '../../shared/components/confirm-dialog.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner.component';
import { MoneyPipe } from '../../shared/pipes/money.pipe';
import { OrderStatusTimelineComponent } from '../checkout/order-status-timeline.component';
import { DeliveryApiService } from './delivery-api.service';
import { DeliveryTracking } from './delivery.models';
import { OrdersApiService } from './orders-api.service';
import { OrderDetail } from './orders.models';
import { isCancellableStatus, labelForStatus } from './order-status';

const CancelReason = 'Cancelled by customer';

interface OrderResourceState {
  readonly order: OrderDetail | null;
  readonly error: ApiError | null;
}

@Component({
  selector: 'fc-order-detail-page',
  imports: [DatePipe, RouterLink, MoneyPipe, LoadingSpinnerComponent, OrderStatusTimelineComponent],
  templateUrl: './order-detail.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderDetailPage {
  private readonly ordersApi = inject(OrdersApiService);
  private readonly deliveryApi = inject(DeliveryApiService);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly toastService = inject(NotificationToastService);

  readonly orderId = input.required<string>();

  protected readonly isCancelling = signal(false);
  private readonly reloadToken = signal(0);

  private readonly orderResource = toSignal(
    combineLatest([toObservable(this.orderId), toObservable(this.reloadToken)]).pipe(
      switchMap(([orderId]) =>
        this.ordersApi.getOrder(orderId).pipe(
          map((order): OrderResourceState => ({ order, error: null })),
          catchError((error: unknown) => of<OrderResourceState>({ order: null, error: ApiError.fromUnknown(error) })),
        ),
      ),
    ),
    { initialValue: { order: null, error: null } satisfies OrderResourceState },
  );

  protected readonly order = computed<OrderDetail | null>(() => this.orderResource().order);
  protected readonly loadError = computed<ApiError | null>(() => this.orderResource().error);
  protected readonly isLoading = computed(() => this.order() === null && this.loadError() === null);
  protected readonly canCancel = computed(() => {
    const current = this.order();
    return current !== null && isCancellableStatus(current.status);
  });
  protected readonly requiresShipping = computed(() => this.order()?.lines.some((line) => !line.isDigital) ?? false);

  private readonly deliverySku = computed(() => (this.requiresShipping() ? this.orderId() : null));

  protected readonly delivery = toSignal<DeliveryTracking | null>(
    toObservable(this.deliverySku).pipe(
      switchMap((orderId) =>
        orderId === null
          ? of<DeliveryTracking | null>(null)
          : this.deliveryApi.getTrackingForOrder(orderId).pipe(
              // A 404 means delivery has not been scheduled yet; tracking is auxiliary, so any
              // failure simply hides the block rather than breaking the order view.
              catchError(() => of<DeliveryTracking | null>(null)),
            ),
      ),
    ),
    { initialValue: null },
  );

  protected statusLabel(): string {
    const current = this.order();
    return current ? labelForStatus(current.status) : '';
  }

  protected async cancelOrder(): Promise<void> {
    const current = this.order();
    if (!current || !this.canCancel() || this.isCancelling()) {
      return;
    }
    const confirmed = await this.confirmDialog.confirm({
      title: 'Cancel order',
      message: 'Are you sure you want to cancel this order?',
      confirmLabel: 'Cancel order',
      cancelLabel: 'Keep order',
      destructive: true,
    });
    if (!confirmed) {
      return;
    }
    this.isCancelling.set(true);
    try {
      await firstValueFrom(this.ordersApi.cancelOrder(current.orderId, { reason: CancelReason }));
      this.toastService.showSuccess('Your order has been cancelled.');
      this.reloadToken.update((token) => token + 1);
    } catch (error: unknown) {
      this.toastService.showDanger(ApiError.fromUnknown(error).detail);
    } finally {
      this.isCancelling.set(false);
    }
  }
}
