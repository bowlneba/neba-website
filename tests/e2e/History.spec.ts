import { test, expect } from '@playwright/test';

test.describe('Bowler of the Year awards page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/history/bowler-of-the-year');
    await page.waitForSelector('h1');
  });

  test('renders page header', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Bowler of the Year');
  });

  test('displays award cards grouped by season', async ({ page }) => {
    await expect(page.locator('text=2024-2025')).toBeVisible();
    await expect(page.locator('text=2023-2024')).toBeVisible();
  });

  test('displays award winners', async ({ page }) => {
    await expect(page.locator('text=Current Leader')).toBeVisible();
    await expect(page.locator('text=Legacy Leader')).toBeVisible();
  });
});

test.describe('High Average awards page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/history/high-average');
    await page.waitForSelector('h1');
  });

  test('renders page header', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('High Average');
  });

  test('displays award cards grouped by season', async ({ page }) => {
    await expect(page.locator('text=2024-2025')).toBeVisible();
    await expect(page.locator('text=2023-2024')).toBeVisible();
  });

  test('displays winners with average', async ({ page }) => {
    await expect(page.locator('text=Current Leader')).toBeVisible();
    await expect(page.locator('text=228.42')).toBeVisible();
  });
});

test.describe('High Block awards page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/history/high-block');
    await page.waitForSelector('h1');
  });

  test('renders page header', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('High Block');
  });

  test('displays award cards grouped by season', async ({ page }) => {
    await expect(page.locator('text=2024-2025')).toBeVisible();
    await expect(page.locator('text=2023-2024')).toBeVisible();
  });

  test('displays winners with score', async ({ page }) => {
    await expect(page.locator('text=Current Leader')).toBeVisible();
    await expect(page.locator('text=1198')).toBeVisible();
  });
});
