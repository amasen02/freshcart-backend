import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { BasketStore } from '../../core/basket/basket.store';
import { ConfirmDialogService } from '../../shared/components/confirm-dialog.service';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner.component';
import { MoneyPipe } from '../../shared/pipes/money.pipe';

const MinimumLineQuantity = 1;
const MaximumLineQuantity = 99;

@Component({
  selector: 'fc-basket-page',
  imports: [ReactiveFormsModule, RouterLink, MoneyPipe, LoadingSpinnerComponent, EmptyStateComponent],
  templateUrl: './basket.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BasketPage {
  protected readonly basketStore = inject(BasketStore);
  private readonly confirmDialog = inject(ConfirmDialogService);

  protected readonly minimumLineQuantity = MinimumLineQuantity;
  protected readonly maximumLineQuantity = MaximumLineQuantity;

  protected readonly couponControl = new FormControl<string>('', {
    nonNullable: true,
    validators: [Validators.required],
  });

  protected readonly isMutating = computed(() => this.basketStore.status() === 'mutating');
  private readonly pendingProductIds = signal<ReadonlySet<string>>(new Set<string>());

  constructor() {
    if (this.basketStore.status() === 'initial') {
      void this.basketStore.load();
    }
  }

  protected isLinePending(productId: string): boolean {
    return this.pendingProductIds().has(productId);
  }

  protected async changeQuantity(productId: string, nextQuantity: number): Promise<void> {
    const clamped = Math.min(MaximumLineQuantity, Math.max(MinimumLineQuantity, nextQuantity));
    await this.runLineMutation(productId, () => this.basketStore.updateQuantity(productId, clamped));
  }

  protected async removeLine(productId: string, productName: string): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Remove item',
      message: `Remove ${productName} from your basket?`,
      confirmLabel: 'Remove',
      destructive: true,
    });
    if (!confirmed) {
      return;
    }
    await this.runLineMutation(productId, () => this.basketStore.removeItem(productId));
  }

  protected async applyCoupon(): Promise<void> {
    if (this.couponControl.invalid) {
      this.couponControl.markAsTouched();
      return;
    }
    const code = this.couponControl.value.trim();
    await this.basketStore.applyCoupon(code);
    this.couponControl.reset();
  }

  protected async removeCoupon(): Promise<void> {
    await this.basketStore.removeCoupon();
  }

  private async runLineMutation(productId: string, mutation: () => Promise<void>): Promise<void> {
    if (this.isLinePending(productId)) {
      return;
    }
    this.setPending(productId, true);
    try {
      await mutation();
    } finally {
      this.setPending(productId, false);
    }
  }

  private setPending(productId: string, isPending: boolean): void {
    this.pendingProductIds.update((current) => {
      const next = new Set(current);
      if (isPending) {
        next.add(productId);
      } else {
        next.delete(productId);
      }
      return next;
    });
  }
}
