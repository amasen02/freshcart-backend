import type { Page } from '@playwright/test';
import { expect } from '@playwright/test';

/**
 * Page-object wrapper for the customer journey. Hides selector churn from the spec files —
 * if the SPA renames a button, only this file needs to change.
 */
export class CustomerJourneyPage {
  constructor(private readonly page: Page) {}

  async openStorefront(): Promise<void> {
    await this.page.goto('/');
    await expect(this.page).toHaveTitle(/FreshCart/i);
  }

  async signUp(profile: { email: string; password: string; displayName: string }): Promise<void> {
    await this.page.getByRole('link', { name: /sign up/i }).click();
    await this.page.getByLabel(/email/i).fill(profile.email);
    await this.page.getByLabel(/display name/i).fill(profile.displayName);
    await this.page.getByLabel(/^password$/i).fill(profile.password);
    await this.page.getByLabel(/confirm password/i).fill(profile.password);
    await this.page.getByRole('button', { name: /create account/i }).click();
    await expect(this.page.getByRole('button', { name: /sign out/i })).toBeVisible();
  }

  async signIn(email: string, password: string): Promise<void> {
    await this.page.getByRole('link', { name: /sign in/i }).click();
    await this.page.getByLabel(/email/i).fill(email);
    await this.page.getByLabel(/^password$/i).fill(password);
    await this.page.getByRole('button', { name: /sign in/i }).click();
    await expect(this.page.getByRole('button', { name: /sign out/i })).toBeVisible();
  }

  async addFirstProductToBasket(): Promise<string> {
    await this.page.getByRole('link', { name: /catalog/i }).click();
    const firstProductCard = this.page.getByTestId('product-card').first();
    const productName = (await firstProductCard.getByTestId('product-name').textContent())?.trim() ?? '';
    await firstProductCard.getByRole('button', { name: /add to basket/i }).click();
    return productName;
  }

  async openBasket(): Promise<void> {
    await this.page.getByRole('link', { name: /basket/i }).click();
    await expect(this.page.getByRole('heading', { name: /your basket/i })).toBeVisible();
  }

  async proceedToCheckout(): Promise<void> {
    await this.page.getByRole('button', { name: /checkout/i }).click();
    await expect(this.page).toHaveURL(/\/checkout/);
  }

  async fillCheckoutForm(profile: { displayName: string }): Promise<void> {
    await this.page.getByLabel(/full name/i).fill(profile.displayName);
    await this.page.getByLabel(/address line/i).fill('221B Baker Street');
    await this.page.getByLabel(/city/i).fill('London');
    await this.page.getByLabel(/postal code/i).fill('NW1 6XE');
    await this.page.getByLabel(/country/i).selectOption('GB');
    await this.page.getByLabel(/payment method/i).selectOption('card');
    await this.page.getByRole('button', { name: /place order/i }).click();
  }

  async expectOrderConfirmation(): Promise<string> {
    const confirmationHeading = this.page.getByRole('heading', { name: /thanks|order placed|confirmation/i });
    await expect(confirmationHeading).toBeVisible({ timeout: 15_000 });
    const orderNumber = await this.page.getByTestId('order-number').textContent();
    return orderNumber?.trim() ?? '';
  }

  async expectRealtimeNotificationToast(): Promise<void> {
    // SignalR push from the Notification service — should arrive within a few seconds.
    await expect(this.page.getByTestId('notification-toast')).toBeVisible({ timeout: 10_000 });
  }
}
