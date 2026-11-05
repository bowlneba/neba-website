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
    await expect(page.locator('.inductee-item', { hasText: 'Tom Davis' }).locator('.inductee-category')).toHaveCount(2);
  });

  test('filters inductees by name', async ({ page }) => {
    await page.locator('#hof-search').pressSequentially('alice');
    await expect(page.locator('.inductee-name', { hasText: 'Alice Williams' })).toBeVisible();
    await expect(page.locator('.inductee-name', { hasText: 'Jane Smith' })).toBeHidden();
    await expect(page.locator('.hof-status')).toHaveText('Showing 1 of 4');
    await expect(page.locator('.hof-year-block:visible')).toHaveCount(1);
  });

  test('filters inductees by category', async ({ page }) => {
    await page.getByRole('button', { name: 'Friend of NEBA' }).click();
    await expect(page.locator('.inductee-name', { hasText: 'Alice Williams' })).toBeVisible();
    await expect(page.locator('.inductee-name', { hasText: 'Bob Johnson' })).toBeHidden();
    await expect(page.getByRole('button', { name: 'Friend of NEBA' })).toHaveAttribute('aria-pressed', 'true');
  });

  test('shows a message when no inductee matches, and recovers when the search clears', async ({ page }) => {
    await page.locator('#hof-search').fill('zzz');
    await expect(page.locator('.hof-status')).toHaveText('No inductees match your search.');
    await expect(page.locator('.hof-stage')).toBeHidden();

    await page.locator('#hof-search').fill('');
    await expect(page.locator('.hof-status')).toHaveText('');
    await expect(page.locator('.inductee-name', { hasText: 'Jane Smith' })).toBeVisible();
  });

  test('eligibility criteria section is always visible', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Eligibility & Criteria' })).toBeVisible();
  });

  test('the criteria link in the About section scrolls to the criteria', async ({ page }) => {
    await page.getByRole('link', { name: 'See the eligibility criteria' }).click();
    await expect(page).toHaveURL(/\/hall-of-fame#hof-criteria$/);
    await expect(page.locator('#hof-criteria')).toBeInViewport();
  });
});
