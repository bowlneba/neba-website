import { test, expect } from '@playwright/test';

const MOCK_TOURNAMENT_ID = '01JX0000000000000000000010';

test.describe('Tournament Detail page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}`);
    await page.waitForSelector('.td-hero');
  });

  test('renders tournament name', async ({ page }) => {
    await expect(page.locator('.td-hero__title')).toContainText('NEBA Spring Classic');
  });

  test('past tournament uses past hero styling', async ({ page }) => {
    await expect(page.locator('.td-hero')).toHaveClass(/td-hero--past/);
  });

  test('displays champion pill with winner name', async ({ page }) => {
    await expect(page.locator('.td-hero__champion-pill')).toContainText('Current Leader');
  });

  test('back link navigates to tournaments list', async ({ page }) => {
    await page.locator('.tournament-detail__back-link').click();
    await expect(page).toHaveURL('/tournaments');
  });
});

test.describe('Tournament Detail — not found', () => {
  test('unknown tournament id redirects to not-found', async ({ page }) => {
    await page.goto('/tournaments/does-not-exist');
    await expect(page).toHaveURL('/not-found');
  });
});
