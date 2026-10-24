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
    await expect(page.locator('.neba-modal-content')).toBeVisible();
  });

  test('modal closes via close button', async ({ page }) => {
    await page.locator('.indiv-points-race-card').first().click();
    await expect(page.locator('.neba-modal-content')).toBeVisible();

    await page.locator('.neba-modal-close').click();
    await expect(page.locator('.neba-modal-content')).not.toBeVisible();
  });

  test('modal closes via backdrop click', async ({ page }) => {
    await page.locator('.indiv-points-race-card').first().click();
    await expect(page.locator('.neba-modal-content')).toBeVisible();

    await page.locator('.neba-modal-backdrop').click({ position: { x: 10, y: 10 } });
    await expect(page.locator('.neba-modal-content')).not.toBeVisible();
  });
});

test.describe('Stats phone layout', () => {
  test.use({ viewport: { width: 375, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto(`/stats?season=${CURRENT_SEASON_YEAR}`);
    await page.waitForSelector('.stats-section.neba-collapsible table.neba-data-table tbody tr[data-expandable]');
  });

  test('secondary columns are hidden until the row is tapped', async ({ page }) => {
    const row = page.locator('table.neba-data-table tbody tr[data-expandable]').first();
    const secondaryCell = row.locator('td[data-secondary]').first();

    await expect(secondaryCell).toBeHidden();

    await row.click({ position: { x: 5, y: 5 } });

    await expect(row).toHaveAttribute('data-open', '');
    await expect(row).toHaveAttribute('aria-expanded', 'true');
    await expect(secondaryCell).toBeVisible();

    await row.click({ position: { x: 5, y: 5 } });

    await expect(row).not.toHaveAttribute('data-open', '');
    await expect(secondaryCell).toBeHidden();
  });

  test('tapping a section header collapses and expands the section', async ({ page }) => {
    const section = page.locator('.stats-section.neba-collapsible').first();
    const header = section.locator('> .neba-collapsible-header');
    const table = section.locator('table.neba-data-table');

    await expect(header).toHaveAttribute('aria-expanded', 'true');

    await header.locator('h2').click();

    await expect(section).toHaveAttribute('data-collapsed', '');
    await expect(header).toHaveAttribute('aria-expanded', 'false');
    await expect(table).toBeHidden();

    await header.locator('h2').click();

    await expect(section).not.toHaveAttribute('data-collapsed', '');
    await expect(table).toBeVisible();
  });

  test('section header toggles from the keyboard', async ({ page }) => {
    const section = page.locator('.stats-section.neba-collapsible').first();
    const header = section.locator('> .neba-collapsible-header');

    await header.focus();
    await page.keyboard.press('Enter');

    await expect(section).toHaveAttribute('data-collapsed', '');
  });
});

test.describe('Stats desktop layout', () => {
  test.use({ viewport: { width: 1200, height: 900 } });

  test('tapping a row does not expand it on wider screens', async ({ page }) => {
    await page.goto(`/stats?season=${CURRENT_SEASON_YEAR}`);
    await page.waitForSelector('table.neba-data-table tbody tr[data-expandable]');

    const row = page.locator('table.neba-data-table tbody tr[data-expandable]').first();
    await row.click({ position: { x: 5, y: 5 } });

    await expect(row).not.toHaveAttribute('data-open', '');
    await expect(row.locator('td[data-secondary]').first()).toBeVisible();
  });
});
