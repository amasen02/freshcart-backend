import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { OrderStatus } from '../orders/orders.models';

type StepState = 'complete' | 'current' | 'upcoming';

interface TimelineStep {
  readonly status: OrderStatus;
  readonly label: string;
  readonly state: StepState;
}

const HappyPath: readonly { status: OrderStatus; label: string }[] = [
  { status: 'Submitted', label: 'Submitted' },
  { status: 'StockReserved', label: 'Stock reserved' },
  { status: 'Paid', label: 'Paid' },
  { status: 'Confirmed', label: 'Confirmed' },
];

@Component({
  selector: 'fc-order-status-timeline',
  template: `
    @if (isCancelled()) {
      <div class="alert alert-danger d-flex align-items-center" role="alert">
        <i class="bi bi-x-circle-fill me-2" aria-hidden="true"></i>
        <span>This order was cancelled.</span>
      </div>
    } @else {
      <ol class="list-unstyled d-flex flex-column flex-md-row justify-content-between gap-3 mb-0">
        @for (step of steps(); track step.status) {
          <li class="d-flex flex-md-column align-items-center text-md-center gap-2 flex-fill">
            <span
              class="d-inline-flex align-items-center justify-content-center rounded-circle border fc-timeline-step"
              [class.bg-success]="step.state === 'complete'"
              [class.text-white]="step.state === 'complete'"
              [class.border-primary]="step.state === 'current'"
              [class.text-primary]="step.state === 'current'">
              @if (step.state === 'complete') {
                <i class="bi bi-check-lg" aria-hidden="true"></i>
              } @else if (step.state === 'current') {
                <span class="spinner-border spinner-border-sm" aria-hidden="true"></span>
              } @else {
                <i class="bi bi-circle" aria-hidden="true"></i>
              }
            </span>
            <span
              class="small"
              [class.fw-semibold]="step.state !== 'upcoming'"
              [class.text-body-secondary]="step.state === 'upcoming'">
              {{ step.label }}
            </span>
          </li>
        }
      </ol>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderStatusTimelineComponent {
  readonly status = input.required<OrderStatus>();

  protected readonly isCancelled = computed(
    () => this.status() === 'Cancelled' || this.status() === 'Refunded',
  );

  protected readonly steps = computed<readonly TimelineStep[]>(() => {
    const status = this.status();
    const currentIndex = HappyPath.findIndex((step) => step.status === status);
    const everyStepDone = status === 'Confirmed';
    return HappyPath.map((step, index) => ({
      status: step.status,
      label: step.label,
      state: resolveState(index, currentIndex, everyStepDone),
    }));
  });
}

function resolveState(index: number, currentIndex: number, everyStepDone: boolean): StepState {
  if (everyStepDone || currentIndex < 0 || index < currentIndex) {
    return 'complete';
  }
  return index === currentIndex ? 'current' : 'upcoming';
}
