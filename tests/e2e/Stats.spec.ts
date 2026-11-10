import { test, expect, type Page } from '@playwright/test';

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

test.describe('Stats - full standings modal', () => {
  const showAll = (page: Page) =>
    page.getByRole('button', { name: /Show all 560 Bowler of the Year standings/ });

  const dataRows = (page: Page) =>
    page.locator('.stats-modal-body tbody tr.stats-row, .stats-modal-body tbody tr.stats-row-first');

  test('renders a window of rows and reaches the last row', async ({ page }) => {
    await page.setViewportSize({ width: 1100, height: 900 });
    await page.goto(`/stats?season=${CURRENT_SEASON_YEAR}`);
    await showAll(page).click();
    await expect(dataRows(page).first()).toBeVisible();

    expect(await dataRows(page).count()).toBeLessThan(100);
    // Virtualize starts from an estimated row height, so the total height shifts as rows render: keep scrolling.
    await expect(async () => {
      await page.locator('.stats-modal-body').evaluate(el => el.scrollTo(0, el.scrollHeight));
      await expect(page.locator('.stats-modal-body').getByText('Bowler 560')).toBeVisible({ timeout: 500 });
    }).toPass();
  });

  test('shows every column without side scroll', async ({ page }) => {
    for (const width of [1100, 900, 390]) {
      await page.setViewportSize({ width, height: 900 });
      await page.goto(`/stats?season=${CURRENT_SEASON_YEAR}`);
      await showAll(page).click();

      const body = page.locator('.stats-modal-body');
      await expect(body).toBeVisible();
      await expect(dataRows(page).first()).toBeVisible();
      expect(await body.evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true);
      // Wide: the header shows. Compact: the header hides and each row labels its own $ Won cell.
      await expect(body.locator('th:has-text("$ Won"), td[data-label="$ Won"]').and(page.locator(':visible')).first()).toBeVisible();
      await page.locator('.neba-modal-close').click();
    }
  });

  test('only the grid scrolls, not the modal box', async ({ page }) => {
    for (const size of [{ width: 1100, height: 900 }, { width: 390, height: 844 }]) {
      await page.setViewportSize(size);
      await page.goto(`/stats?season=${CURRENT_SEASON_YEAR}`);
      await showAll(page).click();
      await expect(dataRows(page).first()).toBeVisible();

      const box = page.locator('.neba-modal-content');
      expect(await box.evaluate(el => el.scrollHeight <= el.clientHeight)).toBe(true);
      expect(await page.locator('.stats-modal-body').evaluate(el => el.scrollHeight > el.clientHeight)).toBe(true);
      await page.locator('.neba-modal-close').click();
    }
  });

  test('the modal fills the screen on a phone', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(`/stats?season=${CURRENT_SEASON_YEAR}`);
    await showAll(page).click();

    // The modal scales in over 0.2s, so wait for the final box.
    await expect.poll(async () => (await page.locator('.neba-modal-content').boundingBox())?.width).toBe(390);
    const box = await page.locator('.neba-modal-content').boundingBox();
    expect(box?.x).toBe(0);
  });
});
