export interface BasketItem {
  readonly productId: string;
  readonly productSku: string;
  readonly productName: string;
  readonly primaryCategory: string;
  readonly unitPrice: number;
  readonly discountedUnitPrice: number;
  readonly lineTotal: number;
  readonly quantity: number;
  readonly imageUrl: string | null;
  readonly isDigital: boolean;
}

export interface BasketTotals {
  readonly subtotal: number;
  readonly discountTotal: number;
  readonly taxTotal: number;
  readonly grandTotal: number;
}

export interface BasketDto {
  readonly items: readonly BasketItem[];
  readonly currencyCode: string;
  readonly subtotal: number;
  readonly discountTotal: number;
  readonly taxTotal: number;
  readonly grandTotal: number;
  readonly appliedCoupon: string | null;
}

export type BasketStatus = 'initial' | 'loading' | 'ready' | 'mutating' | 'error';
