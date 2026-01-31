import { test, expect } from '@playwright/test';

test('homepage has title', async ({ page }) => {
  await page.goto('/');

  // Expect the page to have a title
  await expect(page).toHaveTitle(/Neba/i);
});

test('navigation menu is visible', async ({ page }) => {
  await page.goto('/');

  // Check that the nav menu exists
  const nav = page.locator('nav');
  await expect(nav).toBeVisible();
});
