import { randomUUID } from 'node:crypto';

/**
 * Generates a fresh, deterministic-ish customer profile for each test run so they can be run
 * in parallel against a shared environment without colliding on email addresses.
 */
export function freshCustomerProfile() {
  const uniqueSuffix = randomUUID().replace(/-/g, '').slice(0, 12);
  return {
    email: `e2e+${uniqueSuffix}@freshcart.test`,
    password: 'P@ssw0rd-E2E-Test-2026',
    displayName: `E2E Test Customer ${uniqueSuffix}`,
  } as const;
}
