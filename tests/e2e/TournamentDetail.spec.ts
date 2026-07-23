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

  test('displays the title sponsor name in the "Presented by" hero text', async ({ page }) => {
    await expect(page.locator('.td-hero__sponsor')).toContainText('Pro Shop Plus');
  });

  test('renders sponsor cards for every tournament sponsor', async ({ page }) => {
    const cards = page.locator('.td-rail-sponsor-card');
    await expect(cards).toHaveCount(2);
    await expect(cards.nth(0)).toContainText('Pro Shop Plus');
    await expect(cards.nth(1)).toContainText('Regional Lanes');
  });

  test('clicking a sponsor card navigates to the sponsor detail page', async ({ page }) => {
    await page.locator('.td-rail-sponsor-card', { hasText: 'Pro Shop Plus' }).click();
    await expect(page).toHaveURL('/sponsors/pro-shop-plus');
  });
});

test.describe('Tournament Detail — not found', () => {
  test('unknown tournament id redirects to not-found', async ({ page }) => {
    await page.goto('/tournaments/does-not-exist');
    await expect(page).toHaveURL('/not-found');
  });
});
