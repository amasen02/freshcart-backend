import { InjectionToken } from '@angular/core';

export const SearchDebounceMs = new InjectionToken<number>('SearchDebounceMs', {
  providedIn: 'root',
  factory: () => 300,
});

export const OrderPollIntervalMs = new InjectionToken<number>('OrderPollIntervalMs', {
  providedIn: 'root',
  factory: () => 5000,
});

export const OrderPollMaxAttempts = new InjectionToken<number>('OrderPollMaxAttempts', {
  providedIn: 'root',
  factory: () => 12,
});

export const TypingIndicatorDebounceMs = new InjectionToken<number>('TypingIndicatorDebounceMs', {
  providedIn: 'root',
  factory: () => 400,
});
