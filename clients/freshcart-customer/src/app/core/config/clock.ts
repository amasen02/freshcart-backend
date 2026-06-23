import { InjectionToken } from '@angular/core';

export interface Clock {
  now(): number;
}

export const Clock = new InjectionToken<Clock>('Clock', {
  providedIn: 'root',
  factory: () => ({ now: () => Date.now() }),
});
