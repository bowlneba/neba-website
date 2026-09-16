import { test, expect } from '@playwright/test';

test.describe.configure({ mode: 'serial' });

test.describe('Clear Cache (unauthenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show the account menu', async ({ page }) => {
    await page.goto('/');
    await page.waitForSelector('h1');
    await expect(page.locator('.account-menu')).toHaveCount(0);
  });
});

test.describe('Clear Cache (authenticated, no permission)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show the Clear Cache menu item', async ({ page }) => {
    await page.request.post('/__test/login?permissions=');

    await page.goto('/');
    await page.waitForSelector('.account-menu');

    await page.getByRole('button', { name: 'Account menu' }).hover();
    await expect(page.getByRole('button', { name: 'Clear Cache' })).toHaveCount(0);
  });
});

test.describe('Clear Cache (authenticated, with permission)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Cache.Clear');
  });

  // A failed assertion earlier in a test can abort it before its own /__mock/reset call runs,
  // leaving the /cache override in place and breaking a later test - see CreateUser.spec.ts.
  test.afterEach(async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/reset?path=/cache');
  });

  test('shows the Clear Cache menu item and clears the cache on click', async ({ page }) => {
    await page.goto('/');
    await page.waitForSelector('.account-menu');

    await page.getByRole('button', { name: 'Account menu' }).hover();
    await expect(page.getByRole('button', { name: 'Clear Cache' })).toBeVisible();
    await page.getByRole('button', { name: 'Clear Cache' }).click();

    await expect(page.locator('.neba-toast-title')).toContainText('Cache Cleared');
    await expect(page.locator('.neba-toast-message')).toContainText('Cache was cleared.');
  });

  test('shows an error toast when the server returns a failure', async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/fail?path=/cache&status=403');

    await page.goto('/');
    await page.waitForSelector('.account-menu');

    await page.getByRole('button', { name: 'Account menu' }).hover();
    await page.getByRole('button', { name: 'Clear Cache' }).click();

    await expect(page.locator('.neba-toast-title')).toContainText('Cache Clear Failed');
    await expect(page.locator('.neba-toast-message')).toContainText('403');
  });
});
