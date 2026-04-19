import { test, expect } from '@playwright/test';

const LEGACY_SEASON_TEXT = '2020-2021 Season';
const LEGACY_SEASON_YEAR = '2021';
const LEGACY_BOWLER_NAME = 'Legacy Leader';

test.describe('Stats season navigation state', () => {
  test.use({ viewport: { width: 1200, height: 900 } });

  test('navigating from stats to individual keeps selected season', async ({ page }) => {
    await page.goto(`/stats?season=${LEGACY_SEASON_YEAR}`);

    await expect(page.locator('h1')).toContainText('Season Statistics');
    await expect(page.locator('.stats-season-btn.active')).toContainText(LEGACY_SEASON_TEXT);

    await page.locator('a.stats-name-link', { hasText: LEGACY_BOWLER_NAME }).first().click();

    await expect(page).toHaveURL(new RegExp(`/stats/.+\\?season=${LEGACY_SEASON_YEAR}$`));
    await expect(page.locator('.stats-season-btn.active')).toContainText(LEGACY_SEASON_TEXT);
    await expect(page.locator('.indiv-bowler-name')).toContainText(LEGACY_BOWLER_NAME);
  });

  test('navigating back from individual restores season on stats page', async ({ page }) => {
    await page.goto(`/stats?season=${LEGACY_SEASON_YEAR}`);
    await page.locator('a.stats-name-link', { hasText: LEGACY_BOWLER_NAME }).first().click();

    await page.locator('.indiv-back-link').click();

    await expect(page).toHaveURL(`/stats?season=${LEGACY_SEASON_YEAR}`);
    await expect(page.locator('.stats-season-btn.active')).toContainText(LEGACY_SEASON_TEXT);
  });
});
