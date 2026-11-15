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

  test.describe('inductee profile', () => {
    test('opens from the name with the header and a plain points list', async ({ page }) => {
      await page.locator('.inductee-name', { hasText: 'Jane Smith' }).click();

      const dialog = page.getByRole('dialog', { name: 'Jane Smith' });
      await expect(dialog.getByText('Class of 2024')).toBeVisible();
      await expect(dialog.locator('.ip-chips li')).toHaveText(['Superior Performance']);
      await expect(dialog.locator('.pm-stat')).toHaveCount(3);
      await expect(dialog.getByText('Singles Title')).toBeVisible();
      await expect(dialog.locator('.pm-reached')).toHaveCount(0);
      await expect(dialog).not.toContainText('of 36');
    });

    test('opens from the photo circle', async ({ page }) => {
      await page.getByRole('button', { name: "View Jane Smith's Hall of Fame profile" }).click();
      await expect(page.getByRole('dialog', { name: 'Jane Smith' })).toBeVisible();
    });

    test('closes with Escape', async ({ page }) => {
      await page.locator('.inductee-name', { hasText: 'Jane Smith' }).click();
      const dialog = page.getByRole('dialog', { name: 'Jane Smith' });
      await expect(dialog).toBeVisible();
      // The modal moves focus into itself after it renders; Escape only closes it once that has happened.
      await expect(dialog).toBeFocused();

      await page.keyboard.press('Escape');

      await expect(dialog).toBeHidden();
    });

    test('shows a message for an inductee with no points', async ({ page }) => {
      await page.locator('.inductee-name', { hasText: 'Bob Johnson' }).click();
      await expect(page.getByRole('dialog', { name: 'Bob Johnson' })).toContainText('not shown yet');
    });

    test('shows an error with Retry when the points cannot load', async ({ page }) => {
      await page.locator('.inductee-name', { hasText: 'Alice Williams' }).click();
      await expect(page.getByRole('dialog', { name: 'Alice Williams' })).toContainText('Could not load points');
      await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible();
    });
  });
});
