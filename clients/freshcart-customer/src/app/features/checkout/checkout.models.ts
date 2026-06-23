export interface CheckoutAddress {
  readonly line1: string;
  readonly line2: string | null;
  readonly city: string;
  readonly postalCode: string;
  readonly countryCode: string;
}

export interface CheckoutRequest {
  readonly paymentMethod: string;
  readonly billingAddress: CheckoutAddress;
  readonly shippingAddress: CheckoutAddress | null;
}

export interface CheckoutResult {
  readonly orderId: string;
}

export interface PaymentMethodChoice {
  readonly value: string;
  readonly label: string;
  readonly icon: string;
}

export const PaymentMethodChoices: readonly PaymentMethodChoice[] = [
  { value: 'CreditCard', label: 'Credit card', icon: 'credit-card' },
  { value: 'PayPal', label: 'PayPal', icon: 'paypal' },
  { value: 'CashOnDelivery', label: 'Cash on delivery', icon: 'cash-coin' },
];

export interface CountryChoice {
  readonly code: string;
  readonly name: string;
}

export const CountryChoices: readonly CountryChoice[] = [
  { code: 'GB', name: 'United Kingdom' },
  { code: 'US', name: 'United States' },
  { code: 'IE', name: 'Ireland' },
  { code: 'DE', name: 'Germany' },
  { code: 'FR', name: 'France' },
  { code: 'AU', name: 'Australia' },
];
