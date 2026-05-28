import { test, expect } from '@playwright/test';

test.describe('About page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/about');
    await page.waitForSelector('#about-title-sponsor-name');
  });

  test('renders the page header', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('About NEBA');
  });

  test('displays the title sponsor from API', async ({ page }) => {
    await expect(page.locator('#about-title-sponsor-name')).toContainText('Acme Bowling Supply');
  });

  test('displays the premier partners section', async ({ page }) => {
    await expect(page.locator('.sponsor-card-premier')).toHaveCount(1);
  });

  test('does not display standard-tier sponsors', async ({ page }) => {
    await expect(page.locator('text=Regional Lanes')).not.toBeVisible();
  });

  test('navigates to sponsor detail when premier card is clicked', async ({ page }) => {
    await page.locator('.sponsor-card-premier').click();
    await page.waitForSelector('.sponsor-detail');
    await expect(page).toHaveURL(/\/sponsors\/pro-shop-plus/);
  });
});
