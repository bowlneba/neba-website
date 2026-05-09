import { test, expect } from '@playwright/test';

const LEGACY_SEASON_TEXT = '2020-2021 Season';
const LEGACY_SEASON_YEAR = '2021';
const LEGACY_BOWLER_NAME = 'Legacy Leader';
const PRIMARY_BOWLER_ID = '01JX1111111111111111111111';
const CURRENT_SEASON_YEAR = '2025';

test.describe('Stats season navigation state', () => {
  test.use({ viewport: { width: 1200, height: 900 } });

  test('navigating from stats to individual keeps selected season', async ({ page }) => {
    await page.goto(`/stats?season=${LEGACY_SEASON_YEAR}`);

    await expect(page.locator('h1')).toContainText('Season Statistics');
    await expect(page.locator('.stats-season-btn.active')).toContainText(LEGACY_SEASON_TEXT);

    await page.locator('a.stats-name-link', { hasText: LEGACY_BOWLER_NAME }).first().click();

    await expect(page).toHaveURL(new RegExp(String.raw`/stats/.+\?season=${LEGACY_SEASON_YEAR}$`));
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

test.describe('Individual stats — points race modal', () => {
  test.use({ viewport: { width: 1200, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.goto(`/stats/${PRIMARY_BOWLER_ID}?season=${CURRENT_SEASON_YEAR}`);
    await page.waitForSelector('.indiv-points-race-card');
  });

  test('clicking a points race card opens the modal', async ({ page }) => {
    await page.locator('.indiv-points-race-card').first().click();
    await expect(page.locator('.indiv-modal')).toBeVisible();
  });

  test('modal closes via close button', async ({ page }) => {
    await page.locator('.indiv-points-race-card').first().click();
    await expect(page.locator('.indiv-modal')).toBeVisible();

    await page.locator('.indiv-modal-close').click();
    await expect(page.locator('.indiv-modal')).not.toBeVisible();
  });

  test('modal closes via backdrop click', async ({ page }) => {
    await page.locator('.indiv-points-race-card').first().click();
    await expect(page.locator('.indiv-modal')).toBeVisible();

    await page.locator('.indiv-modal-backdrop').click({ position: { x: 10, y: 10 } });
    await expect(page.locator('.indiv-modal')).not.toBeVisible();
  });
});
