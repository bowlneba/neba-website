import { test, expect } from '@playwright/test';

test.describe('Hall of Fame page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/hall-of-fame');
    await page.waitForSelector('.inductee-item');
  });

  test('renders page header', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Hall of Fame');
  });

  test('displays inductees grouped by year', async ({ page }) => {
    await expect(page.locator('text=Class of 2024')).toBeVisible();
    await expect(page.locator('text=Class of 2023')).toBeVisible();
  });

  test('displays inductee names', async ({ page }) => {
    await expect(page.locator('.inductee-name', { hasText: 'Jane Smith' })).toBeVisible();
    await expect(page.locator('.inductee-name', { hasText: 'Bob Johnson' })).toBeVisible();
    await expect(page.locator('.inductee-name', { hasText: 'Alice Williams' })).toBeVisible();
  });

  test('shows multi-category inductee with combined categories', async ({ page }) => {
    await expect(page.locator('.inductee-name', { hasText: 'Tom Davis' })).toBeVisible();
    await expect(page.locator('.combined-categories')).toBeVisible();
  });

  test('eligibility criteria section is always visible', async ({ page }) => {
    await expect(page.locator('text=Eligibility')).toBeVisible();
  });
});
