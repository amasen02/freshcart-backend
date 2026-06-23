import { DecimalPipe, PercentPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { MoneyPipe } from '../../shared/pipes/money.pipe';
import { KpiMetric } from './dashboard.models';

const DefaultCurrencyCode = 'USD';

@Component({
  selector: 'fc-kpi-card',
  imports: [DecimalPipe, PercentPipe, MoneyPipe],
  template: `
    <div class="card h-100 shadow-sm">
      <div class="card-body">
        <p class="text-body-secondary small mb-1">{{ metric().displayName }}</p>
        <p class="h4 mb-2">
          @switch (metric().unit) {
            @case ('Currency') {
              {{ metric().currentValue | fcMoney: currencyCode() }}
            }
            @case ('Percentage') {
              {{ metric().currentValue / 100 | percent: '1.0-1' }}
            }
            @default {
              {{ metric().currentValue | number: '1.0-0' }}
            }
          }
        </p>
        @if (metric().deltaPercentage !== null) {
          <span class="badge" [class]="badgeClass()">
            <i class="bi" [class.bi-arrow-up]="metric().trend === 'Up'"
              [class.bi-arrow-down]="metric().trend === 'Down'"
              [class.bi-dash]="metric().trend === 'Flat'" aria-hidden="true"></i>
            {{ absoluteDelta() | number: '1.0-1' }}%
          </span>
        }
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KpiCardComponent {
  readonly metric = input.required<KpiMetric>();
  readonly currencyCode = input(DefaultCurrencyCode);

  protected readonly absoluteDelta = computed(() => Math.abs(this.metric().deltaPercentage ?? 0));
  protected readonly badgeClass = computed(() => {
    switch (this.metric().trend) {
      case 'Up':
        return 'text-bg-success';
      case 'Down':
        return 'text-bg-danger';
      default:
        return 'text-bg-secondary';
    }
  });
}
