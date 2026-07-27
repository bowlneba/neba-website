import { test, expect } from '@playwright/test';

test.describe.configure({ mode: 'serial' });

test.describe('Set Password page (anonymous)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  // A failed assertion earlier in a test can abort it before its own /__mock/reset call runs,
  // leaving the /security/password/set-from-token override in place and breaking a later test's
  // POST — the mock server keys overrides by path only, not method. Resetting here runs
  // regardless of outcome.
  test.afterEach(async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/reset?path=/security/password/set-from-token');
  });

  test('loads without authentication given a valid userId and token', async ({ page }) => {
    await page.goto('/account/set-password?userId=01JX0000000000000000000399&token=valid-token');
    await expect(page.getByRole('heading', { name: 'Set Your Password' })).toBeVisible();
  });

  test('disables submit and shows a mismatch message when passwords differ', async ({ page }) => {
    await page.goto('/account/set-password?userId=01JX0000000000000000000399&token=valid-token');
    await page.waitForSelector('form');

    await page.getByLabel('New Password', { exact: true }).fill('NewPassw0rd!');
    await page.getByLabel('Confirm New Password').fill('DifferentPassw0rd!');

    await expect(page.getByText('Passwords do not match.')).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toBeDisabled();
  });

  test('sets the password and redirects to login with a success message', async ({ page }) => {
    await page.goto('/account/set-password?userId=01JX0000000000000000000399&token=valid-token');
    await page.waitForSelector('form');

    await page.getByLabel('New Password', { exact: true }).fill('NewPassw0rd!');
    await page.getByLabel('Confirm New Password').fill('NewPassw0rd!');

    // Blazor Server round-trips each keystroke over SignalR before CanSubmit re-enables the
    // button, so wait for it to become enabled rather than clicking immediately after fill().
    await expect(page.locator('button[type="submit"]')).toBeEnabled({ timeout: 15000 });
    await page.locator('button[type="submit"]').click();

    await expect(page).toHaveURL(/\/account\/login\?passwordSet=true$/);
    await expect(page.locator('.neba-alert')).toContainText('Your password has been set');
  });

  test('shows an invalid-link error when the token has expired or is unknown', async ({ page }) => {
    await page.request.post(
      'http://localhost:5151/__mock/fail?path=/security/password/set-from-token&status=400'
    );

    await page.goto('/account/set-password?userId=01JX0000000000000000000399&token=expired-token');
    await page.waitForSelector('form');

    await page.getByLabel('New Password', { exact: true }).fill('NewPassw0rd!');
    await page.getByLabel('Confirm New Password').fill('NewPassw0rd!');

    // Blazor Server round-trips each keystroke over SignalR before CanSubmit re-enables the
    // button, so wait for it to become enabled rather than clicking immediately after fill().
    await expect(page.locator('button[type="submit"]')).toBeEnabled({ timeout: 15000 });
    await page.locator('button[type="submit"]').click();

    await expect(page.locator('.neba-alert')).toContainText('invalid or has expired');
    await expect(page).toHaveURL(/\/account\/set-password/);
  });

  test('shows an invalid-link error when the userId or token is missing from the URL', async ({ page }) => {
    await page.goto('/account/set-password');
    await page.waitForSelector('form');

    await page.getByLabel('New Password', { exact: true }).fill('NewPassw0rd!');
    await page.getByLabel('Confirm New Password').fill('NewPassw0rd!');

    // Blazor Server round-trips each keystroke over SignalR before CanSubmit re-enables the
    // button, so wait for it to become enabled rather than clicking immediately after fill().
    await expect(page.locator('button[type="submit"]')).toBeEnabled({ timeout: 15000 });
    await page.locator('button[type="submit"]').click();

    await expect(page.locator('.neba-alert')).toContainText('invalid or has expired');
  });
});
