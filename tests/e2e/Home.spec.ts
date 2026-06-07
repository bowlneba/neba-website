import { test, expect } from '@playwright/test';

test.describe('Home page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForSelector('h1');
  });

  test('renders the page title', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('New England Bowling Association');
  });

  test('displays the tagline', async ({ page }) => {
    await expect(page.locator('text=Building Bowling Excellence Since 1963')).toBeVisible();
  });

  test('displays quick stats', async ({ page }) => {
    await expect(page.locator('text=60+')).toBeVisible();
    await expect(page.locator('text=Years of History')).toBeVisible();
    await expect(page.locator('text=1000+')).toBeVisible();
    await expect(page.locator('text=Tournaments Held')).toBeVisible();
    await expect(page.locator('text=500+')).toBeVisible();
    await expect(page.locator('text=Active Bowlers')).toBeVisible();
  });

  test('displays motto cards', async ({ page }) => {
    await expect(page.getByText('Run by bowlers', { exact: true })).toBeVisible();
    await expect(page.getByText('Built on respect', { exact: true })).toBeVisible();
    await expect(page.getByText('Driven by competition', { exact: true })).toBeVisible();
  });

  test('quick links navigate to the correct pages', async ({ page }) => {
    await expect(page.locator('a[href="/tournaments"]')).toBeVisible();
    await expect(page.locator('a[href="/stats"]')).toBeVisible();
    await expect(page.locator('a[href="/history/champions"]')).toBeVisible();
    await expect(page.locator('a[href="/hall-of-fame"]')).toBeVisible();
  });

  test('displays the About NEBA section', async ({ page }) => {
    await expect(page.locator('h2', { hasText: 'About NEBA' })).toBeVisible();
    await expect(page.locator('text=premier bowling tour in the region since 1963')).toBeVisible();
  });
});
