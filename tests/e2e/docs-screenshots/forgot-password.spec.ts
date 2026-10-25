import { test, expect } from '@playwright/test';
import path from 'node:path';

/**
 * Generates the screenshots embedded in docs/help/forgot-password.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * The mock API answers /security/password/forgot with 204 for every email, like the real endpoint.
 */

const outDir = path.join('docs', 'help', 'images', 'forgot-password');

test.describe.configure({ mode: 'serial' });

test.describe('forgot-password help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('login page link, blank form, and confirmation', async ({ page }) => {
    await page.goto('/account/login');
    await page.waitForSelector('form');
    await expect(page.getByRole('link', { name: 'Forgot password?' })).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'login-link.png') });

    await page.getByRole('link', { name: 'Forgot password?' }).click();
    await page.waitForSelector('#email');

    await page.screenshot({ path: path.join(outDir, 'form.png') });

    await page.getByLabel('Email').fill('dana.whitlock@bowlneba.com');
    await page.getByRole('button', { name: 'Send reset link' }).click();
    await expect(page.getByText("If an account exists for that email")).toBeVisible({ timeout: 15000 });

    await page.screenshot({ path: path.join(outDir, 'sent.png') });
  });

  test('set password page opened from a reset link', async ({ page }) => {
    await page.goto('/account/set-password?userId=01JXXXXXXXXXXXXXXXXXXXXXXXXX&token=placeholder-token&reason=reset');
    await page.waitForSelector('form');

    await expect(page.getByRole('heading', { name: 'Choose a New Password' })).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'set-password-form.png') });
  });
});
