export type OrderStatus =
  | 'Submitted'
  | 'StockReserved'
  | 'Paid'
  | 'Confirmed'
  | 'Cancelled'
  | 'Refunded';

export interface OrderSummary {
  readonly orderId: string;
  readonly status: OrderStatus;
  readonly grandTotal: number;
  readonly currencyCode: string;
  readonly lineCount: number;
  readonly submittedOnUtc: string;
}

export interface OrderAddress {
  readonly line1: string;
  readonly line2: string | null;
  readonly city: string;
  readonly postalCode: string;
  readonly countryCode: string;
}

export interface OrderLine {
  readonly productId: string;
  readonly productSku: string;
  readonly productName: string;
  readonly primaryCategory: string;
  readonly unitPrice: number;
  readonly quantity: number;
  readonly isDigital: boolean;
  readonly lineTotal: number;
}

export interface OrderDetail {
  readonly orderId: string;
  readonly customerId: string;
  readonly status: OrderStatus;
  readonly customerEmail: string;
  readonly customerDisplayName: string;
  readonly paymentMethod: string;
  readonly subtotal: number;
  readonly discountTotal: number;
  readonly taxTotal: number;
  readonly shippingTotal: number;
  readonly grandTotal: number;
  readonly currencyCode: string;
  readonly billingAddress: OrderAddress;
  readonly shippingAddress: OrderAddress | null;
  readonly failureReason: string | null;
  readonly submittedOnUtc: string;
  readonly confirmedOnUtc: string | null;
  readonly cancelledOnUtc: string | null;
  readonly lines: readonly OrderLine[];
}

export interface CancelOrderRequest {
  readonly reason: string;
}
