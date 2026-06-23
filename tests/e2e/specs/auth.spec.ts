import { test, expect } from '@playwright/test';
import { CustomerJourneyPage } from './support/customer-journey.page';
import { freshCustomerProfile } from './support/test-data';

test.describe('Authentication — cookie-first sign-in', () => {
  test('a brand-new customer can sign up, receive an HttpOnly session cookie, sign out and sign in again', async ({ page, context }) => {
    const customerJourney = new CustomerJourneyPage(page);
    const profile = freshCustomerProfile();

    // Arrange
    await customerJourney.openStorefront();

    // Act — sign up
    await customerJourney.signUp(profile);

    // Assert — HttpOnly + Secure + SameSite=Strict session cookie was issued
    const cookiesAfterSignUp = await context.cookies();
    const sessionCookie = cookiesAfterSignUp.find(cookie => cookie.name === 'FreshCart.Session');
    expect(sessionCookie, 'Session cookie must be present after sign-up').toBeDefined();
    expect(sessionCookie?.httpOnly).toBe(true);
    expect(sessionCookie?.sameSite?.toLowerCase()).toBe('strict');

    // The XSRF-TOKEN companion must also be set (readable so the SPA can echo it).
    const antiForgeryCookie = cookiesAfterSignUp.find(cookie => cookie.name === 'XSRF-TOKEN');
    expect(antiForgeryCookie, 'Anti-forgery cookie must be present after sign-up').toBeDefined();
    expect(antiForgeryCookie?.httpOnly).toBe(false);

    // Act — sign out, then sign in again
    await page.getByRole('button', { name: /sign out/i }).click();
    await expect(page.getByRole('link', { name: /sign in/i })).toBeVisible();

    await customerJourney.signIn(profile.email, profile.password);
    await expect(page.getByRole('button', { name: /sign out/i })).toBeVisible();
  });
});
