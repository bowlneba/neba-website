import { test, expect } from '@playwright/test';

test.describe('Hall of Fame points page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('is reachable from the Hall of Fame page', async ({ page }) => {
    await page.goto('/hall-of-fame');
    await page.waitForSelector('.inductee-item');
    await page.getByRole('link', { name: /See who is eligible and who is closest/ }).click();
    await expect(page).toHaveURL(/\/hall-of-fame\/points$/);
    await expect(page.locator('h1')).toContainText('Hall of Fame Points');
  });

  test.describe('list', () => {
    test.beforeEach(async ({ page }) => {
      await page.goto('/hall-of-fame/points');
      await page.waitForSelector('.pts-qrow');
    });

    test('puts the eligible bowler on the stage and the others below', async ({ page }) => {
      await expect(page.locator('.pts-stage .pts-name', { hasText: 'Ray Dunleavy' })).toBeVisible();
      await expect(page.locator('.pts-below .pts-name', { hasText: 'Ken Almeida' })).toBeVisible();
      await expect(page.locator('.pts-below .pts-row', { hasText: 'Ken Almeida' })).toContainText('needs 1 more');
    });

    test('shows the rules from the API', async ({ page }) => {
      await expect(page.locator('.pts-rule', { hasText: 'Bowler of the Year' })).toContainText('12');
    });

    test('filters by name without a round trip and says when nothing matches', async ({ page }) => {
      await page.locator('[data-pts-search]').pressSequentially('jose');
      await expect(page.locator('.pts-name', { hasText: 'José Marchetti' })).toBeVisible();
      await expect(page.locator('.pts-name', { hasText: 'Ken Almeida' })).toBeHidden();
      await expect(page.locator('.pts-status')).toHaveText('Showing 1 of 4');

      await page.locator('[data-pts-search]').fill('zzz');
      await expect(page.locator('.pts-status')).toHaveText('No bowlers match your search.');
    });

    test('opens the detail modal with the breakdown and membership years', async ({ page }) => {
      await page.locator('.pts-name', { hasText: 'Ken Almeida' }).click();
      const dialog = page.getByRole('dialog');
      await expect(dialog.getByRole('heading', { name: 'Ken Almeida' })).toBeVisible();
      await expect(dialog.getByRole('heading', { name: 'Points earned' })).toBeVisible();
      await expect(dialog.getByRole('heading', { name: 'Membership years' })).toBeVisible();
    });

    test('shows the membership note in the modal for an incomplete history', async ({ page }) => {
      await page.locator('.pts-name', { hasText: 'Walt Brennan' }).click();
      await expect(page.getByRole('dialog')).toContainText("first membership is not on file");
    });
  });

  test('stacks the rows on a phone', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 800 });
    await page.goto('/hall-of-fame/points');
    await page.waitForSelector('.pts-row');
    await expect(page.locator('.pts-axis')).toBeHidden();
  });
});
