import { OrderStatus } from './orders.models';
import { badgeClassForStatus, isCancellableStatus, isTerminalStatus, labelForStatus } from './order-status';

describe('order status helpers', () => {
  it('treatsConfirmedCancelledAndRefundedAsTerminal', () => {
    expect(isTerminalStatus('Confirmed')).toBeTrue();
    expect(isTerminalStatus('Cancelled')).toBeTrue();
    expect(isTerminalStatus('Refunded')).toBeTrue();
  });

  it('treatsInFlightStatusesAsNonTerminal', () => {
    const inFlight: readonly OrderStatus[] = ['Submitted', 'StockReserved', 'Paid'];
    for (const status of inFlight) {
      expect(isTerminalStatus(status)).toBeFalse();
    }
  });

  it('allowsCancellationOnlyBeforePayment', () => {
    expect(isCancellableStatus('Submitted')).toBeTrue();
    expect(isCancellableStatus('StockReserved')).toBeTrue();
    expect(isCancellableStatus('Paid')).toBeFalse();
    expect(isCancellableStatus('Confirmed')).toBeFalse();
  });

  it('mapsEveryStatusToADistinctBadgeClass', () => {
    const statuses: readonly OrderStatus[] = [
      'Submitted',
      'StockReserved',
      'Paid',
      'Confirmed',
      'Cancelled',
      'Refunded',
    ];
    const classes = statuses.map(badgeClassForStatus);
    expect(new Set(classes).size).toBe(statuses.length);
  });

  it('rendersAHumanLabelForStockReserved', () => {
    expect(labelForStatus('StockReserved')).toBe('Stock reserved');
  });
});
