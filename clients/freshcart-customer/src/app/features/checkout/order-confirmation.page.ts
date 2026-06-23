import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, of, switchMap, takeWhile, timer } from 'rxjs';

import { OrderPollIntervalMs, OrderPollMaxAttempts } from '../../core/config/timing.tokens';
import { ApiError } from '../../core/http/api-error.model';
import { NotificationsStore } from '../../core/realtime/notifications.store';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner.component';
import { MoneyPipe } from '../../shared/pipes/money.pipe';
import { OrdersApiService } from '../orders/orders-api.service';
import { OrderDetail, OrderStatus } from '../orders/orders.models';
import { isTerminalStatus } from '../orders/order-status';
import { OrderStatusTimelineComponent } from './order-status-timeline.component';

@Component({
  selector: 'fc-order-confirmation-page',
  imports: [RouterLink, MoneyPipe, LoadingSpinnerComponent, OrderStatusTimelineComponent],
  templateUrl: './order-confirmation.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderConfirmationPage {
  private readonly ordersApi = inject(OrdersApiService);
  private readonly notificationsStore = inject(NotificationsStore);
  private readonly destroyRef = inject(DestroyRef);
  private readonly pollIntervalMs = inject(OrderPollIntervalMs);
  private readonly maximumPollAttempts = inject(OrderPollMaxAttempts);

  readonly orderId = input.required<string>();

  protected readonly order = signal<OrderDetail | null>(null);
  protected readonly loadError = signal<ApiError | null>(null);
  protected readonly status = computed<OrderStatus | null>(() => this.order()?.status ?? null);
  protected readonly isAwaitingFirstLoad = computed(() => this.order() === null && this.loadError() === null);

  constructor() {
    // Realtime is the primary path: when the notifications channel reports a status change for this
    // order, refetch immediately. The bounded poll below remains the fallback when the hub is down,
    // with takeWhile + takeUntilDestroyed guaranteeing the timer stops at a terminal status, after
    // the attempt cap, or on teardown.
    effect(() => {
      const hasStatusUpdate = this.notificationsStore
        .items()
        .some((notification) => notification.orderId === this.orderId());
      if (hasStatusUpdate && !isTerminalStatus(this.status() ?? 'Submitted')) {
        untracked(() => this.refetchOrder());
      }
    });

    let attemptsRemaining = this.maximumPollAttempts;
    timer(0, this.pollIntervalMs)
      .pipe(
        takeWhile(() => attemptsRemaining > 0 && !isTerminalStatus(this.status() ?? 'Submitted')),
        switchMap(() => {
          attemptsRemaining -= 1;
          return this.ordersApi.getOrder(this.orderId()).pipe(
            catchError((error: unknown) => {
              if (this.order() === null) {
                this.loadError.set(ApiError.fromUnknown(error));
              }
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((order) => {
        if (order) {
          this.loadError.set(null);
          this.order.set(order);
        }
      });
  }

  private refetchOrder(): void {
    this.ordersApi
      .getOrder(this.orderId())
      .pipe(
        catchError(() => of(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((order) => {
        if (order) {
          this.loadError.set(null);
          this.order.set(order);
        }
      });
  }
}
