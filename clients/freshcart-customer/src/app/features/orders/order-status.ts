import { OrderStatus } from './orders.models';

export const TerminalOrderStatuses: readonly OrderStatus[] = ['Confirmed', 'Cancelled', 'Refunded'];
export const CancellableOrderStatuses: readonly OrderStatus[] = ['Submitted', 'StockReserved'];

const StatusBadgeClasses: Readonly<Record<OrderStatus, string>> = {
  Submitted: 'text-bg-secondary',
  StockReserved: 'text-bg-info',
  Paid: 'text-bg-primary',
  Confirmed: 'text-bg-success',
  Cancelled: 'text-bg-danger',
  Refunded: 'text-bg-warning',
};

const StatusLabels: Readonly<Record<OrderStatus, string>> = {
  Submitted: 'Submitted',
  StockReserved: 'Stock reserved',
  Paid: 'Paid',
  Confirmed: 'Confirmed',
  Cancelled: 'Cancelled',
  Refunded: 'Refunded',
};

export function badgeClassForStatus(status: OrderStatus): string {
  return StatusBadgeClasses[status];
}

export function labelForStatus(status: OrderStatus): string {
  return StatusLabels[status];
}

export function isTerminalStatus(status: OrderStatus): boolean {
  return TerminalOrderStatuses.includes(status);
}

export function isCancellableStatus(status: OrderStatus): boolean {
  return CancellableOrderStatuses.includes(status);
}
