import { test, expect, type Locator, type Page } from '@playwright/test';
import path from 'node:path';

import {
  MOCK_ADVANCEMENT_CANCELLED_ID,
  MOCK_ADVANCEMENT_COMPLETE_ID,
  MOCK_ADVANCEMENT_MID_SEASON_ID,
  MOCK_ADVANCEMENT_OVERRIDES_ID,
  MOCK_ADVANCEMENT_REVIEW_ID,
  MOCK_ADVANCEMENT_SQUAD_ID,
  perProjectAdvancementSeriesId,
} from '../mock-api/mock-api-server';

/**
 * Generates the screenshots embedded in docs/help/series-advancement.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * Every shot only reads or opens a dialog and cancels it, so the script is safe to re-run.
 * The mock server's fixtures stay as they are, but each shot resets its Series first anyway.
 */

const outDir = path.join('docs', 'help', 'images', 'series-advancement');
const MOCK_API = 'http://localhost:5151';
const PROJECT = 'chrome';

const MANAGER = 'Series.EditSeries,Series.MakeStageOfficial';
const ADMIN = 'Series.EditSeries,Series.ManageAdvancement,Series.MakeStageOfficial';

const seriesId = (baseId: string) => perProjectAdvancementSeriesId(baseId, PROJECT);
const shot = (name: string) => path.join(outDir, `${name}.png`);
const card = (page: Page, order: number) => page.getByTestId('series-stage-card').nth(order - 1);

async function open(page: Page, baseId: string, permissions: string) {
  const id = seriesId(baseId);
  const reset = await page.request.post(`${MOCK_API}/__mock/series-advancement/reset?seriesId=${id}`);
  expect(reset.status()).toBe(204);

  await page.request.post(`/__test/login?permissions=${permissions}`);
  await page.goto(`/series/${id}`);
  await page.waitForSelector('[data-testid="series-stage-card"]');
}

async function expand(page: Page, order: number): Promise<Locator> {
  const toggle = card(page, order).locator('.series-expander');
  await toggle.click();
  await expect(toggle).toHaveAttribute('aria-expanded', 'true');
  return card(page, order).getByTestId('stage-output');
}

test.describe.configure({ mode: 'serial' });

test.describe('series-advancement help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 900 } });

  test('the rail, a complete Series', async ({ page }) => {
    await open(page, MOCK_ADVANCEMENT_COMPLETE_ID, MANAGER);
    await page.getByTestId('series-progress').screenshot({ path: shot('rail') });
  });

  test('the rail on a phone', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await open(page, MOCK_ADVANCEMENT_COMPLETE_ID, MANAGER);
    await page.getByTestId('series-progress').screenshot({ path: shot('rail-phone') });
  });

  test('the Stale and Needs squad labels', async ({ page }) => {
    await open(page, MOCK_ADVANCEMENT_MID_SEASON_ID, MANAGER);
    await page.getByTestId('series-progress').screenshot({ path: shot('indicators') });
  });

  test('a Stage that is ready to mark official', async ({ page }) => {
    await open(page, MOCK_ADVANCEMENT_REVIEW_ID, MANAGER);
    await card(page, 1).screenshot({ path: shot('mark-official-button') });
  });

  test('the Mark official confirmation', async ({ page }) => {
    await open(page, MOCK_ADVANCEMENT_REVIEW_ID, MANAGER);
    await card(page, 1).getByTestId('stage-make-official').click();
    await expect(page.getByText('Mark Stage 1 official?')).toBeVisible();
    await page.screenshot({ path: shot('mark-official-confirm') });
    await page.locator('.confirm-action-modal-cancel:visible').click();
  });

  test('a Qualifying Stage with a Backfill entry', async ({ page }) => {
    await open(page, MOCK_ADVANCEMENT_COMPLETE_ID, MANAGER);
    await expand(page, 2);
    await card(page, 2).screenshot({ path: shot('qualifying-output-backfill') });
  });

  test('the Recompute confirmation', async ({ page }) => {
    await open(page, MOCK_ADVANCEMENT_COMPLETE_ID, MANAGER);
    await card(page, 1).getByTestId('stage-recompute').click();
    await expect(page.getByText('Recompute from Stage 1?')).toBeVisible();
    await page.screenshot({ path: shot('recompute-confirm') });
    await page.locator('.confirm-action-modal-cancel:visible').click();
  });

  test('the Pick squad dialog', async ({ page }) => {
    await open(page, MOCK_ADVANCEMENT_SQUAD_ID, MANAGER);
    await card(page, 3).getByTestId('stage-pick-squad').click();
    await expect(page.getByTestId('stage-squad-option')).toHaveCount(2);
    await page.screenshot({ path: shot('pick-squad') });
    await page.getByTestId('stage-squad-cancel').click();
  });

  test('a Manager sees the Stage without override controls', async ({ page }) => {
    await open(page, MOCK_ADVANCEMENT_OVERRIDES_ID, MANAGER);
    await expand(page, 2);
    await card(page, 2).screenshot({ path: shot('manager-view') });
  });

  test('an Admin sees the override controls', async ({ page }) => {
    await open(page, MOCK_ADVANCEMENT_OVERRIDES_ID, ADMIN);
    await expand(page, 2);
    await card(page, 2).screenshot({ path: shot('admin-overrides') });
  });

  test('the Add bowler dialog', async ({ page }) => {
    await open(page, MOCK_ADVANCEMENT_MID_SEASON_ID, ADMIN);
    await expand(page, 1);
    await card(page, 1).getByTestId('stage-add-bowler').click();
    await expect(page.getByTestId('stage-override-modal')).toBeVisible();
    await page.locator('#override-bowler').fill('Leah');
    await expect(page.locator('.neba-autocomplete-option').first()).toBeVisible();
    await page.screenshot({ path: shot('add-bowler') });
    await page.keyboard.press('Escape');
  });

  test('the rail flags a cancelled Stage', async ({ page }) => {
    await open(page, MOCK_ADVANCEMENT_CANCELLED_ID, MANAGER);
    await expect(card(page, 2).getByTestId('stage-flag-cancelled')).toBeVisible();
    await card(page, 2).screenshot({ path: shot('cancelled-stage-rail') });
  });

  test('the Edit Series page lets the editor remove a cancelled Stage', async ({ page }) => {
    const id = seriesId(MOCK_ADVANCEMENT_CANCELLED_ID);
    await open(page, MOCK_ADVANCEMENT_CANCELLED_ID, MANAGER);
    await page.goto(`/series/${id}/edit`);
    await page.waitForSelector('[data-testid="stage-remove-cancelled"]');
    await page.screenshot({ path: shot('cancelled-stage-edit'), fullPage: true });
  });
});
