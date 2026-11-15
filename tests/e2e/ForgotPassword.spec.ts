import { test, expect } from '@playwright/test';

test.describe.configure({ mode: 'serial' });

const NEUTRAL_MESSAGE =
  "If an account exists for that email, we've sent a password reset link. The link expires in 1 hour.";

test.describe('Forgot Password page (anonymous)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  // A failed assertion earlier in a test can abort it before its own reset runs, leaving the
  // /security/password/forgot override in place and breaking a later test's POST.
  test.afterEach(async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/reset?path=/security/password/forgot');
  });

  test('is reachable from the login page', async ({ page }) => {
    await page.goto('/account/login');
    await page.getByRole('link', { name: 'Forgot password?' }).click();

    await expect(page).toHaveURL(/\/account\/forgot-password$/);
    await expect(page.getByRole('heading', { name: 'Forgot Your Password?' })).toBeVisible();
  });

  test('shows the same neutral message for any email', async ({ page }) => {
    for (const email of ['known@bowlneba.com', 'nobody@example.com']) {
      await page.goto('/account/forgot-password');
      await page.waitForSelector('form');

      await page.getByLabel('Email').fill(email);
      await page.getByRole('button', { name: 'Send reset link' }).click();

      await expect(page.getByText(NEUTRAL_MESSAGE)).toBeVisible({ timeout: 15000 });
      await expect(page.getByText(email)).toHaveCount(0);
    }
  });

  test('shows a generic error and keeps the form when the request fails', async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/fail?path=/security/password/forgot&status=500');

    await page.goto('/account/forgot-password');
    await page.waitForSelector('form');

    await page.getByLabel('Email').fill('known@bowlneba.com');
    await page.getByRole('button', { name: 'Send reset link' }).click();

    await expect(page.getByText("We couldn't send your request.")).toBeVisible({ timeout: 15000 });
    await expect(page.getByText(NEUTRAL_MESSAGE)).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Send reset link' })).toBeVisible();
  });

  test('asks for a valid email without calling the API', async ({ page }) => {
    await page.goto('/account/forgot-password');
    await page.waitForSelector('form');

    await page.getByLabel('Email').fill('not-an-email');
    await page.getByRole('button', { name: 'Send reset link' }).click();

    await expect(page.getByText('Enter a valid email address.')).toBeVisible({ timeout: 15000 });
    await expect(page.getByText(NEUTRAL_MESSAGE)).toHaveCount(0);
  });
});
