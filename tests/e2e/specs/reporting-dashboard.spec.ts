import { test, expect } from '@playwright/test';

/**
 * Smoke test for the admin reporting dashboard. Uses the dedicated admin SPA host. Skipped
 * unless ADMIN_BASE_URL is set, which avoids running against environments where the admin
 * portal is not exposed (e.g. local-customer-only dev).
 */
const adminBaseUrl = process.env['ADMIN_BASE_URL'];
test.skip(!adminBaseUrl, 'ADMIN_BASE_URL not configured');

test('admin sales overview renders headline tiles + chart for the back-office user', async ({ page }) => {
  await page.goto(`${adminBaseUrl}/dashboards/sales`);

  await page.getByLabel(/email/i).fill('admin@freshcart.test');
  await page.getByLabel(/^password$/i).fill('Admin-P@ssw0rd-2026');
  await page.getByRole('button', { name: /sign in/i }).click();

  await expect(page.getByRole('heading', { name: /sales overview/i })).toBeVisible({ timeout: 15_000 });

  // Headline tiles
  const expectedTileCodes = ['kpi.sales.gmv', 'kpi.sales.net-revenue', 'kpi.sales.order-count', 'kpi.sales.aov'];
  for (const code of expectedTileCodes) {
    await expect(page.getByTestId(`kpi-tile-${code}`)).toBeVisible();
  }

  // Trend chart canvas
  await expect(page.getByTestId('sales-trend-chart')).toBeVisible();

  // Excel export button is reachable
  await expect(page.getByRole('link', { name: /download excel/i })).toBeVisible();
});
