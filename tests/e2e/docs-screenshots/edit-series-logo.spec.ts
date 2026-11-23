import { test, expect, type Page } from '@playwright/test';
import path from 'node:path';

import { MOCK_SERIES_ID, MOCK_SERIES_NAME } from '../mock-api/mock-api-server';

/**
 * Generates the screenshots embedded in docs/help/edit-series-logo.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * The script opens the logo window and cancels it, so nothing is saved and it is safe to re-run.
 */

const outDir = path.join('docs', 'help', 'images', 'edit-series-logo');

/** Wait for the modal's own animations to finish so the screenshot is not taken mid-transition. */
async function waitForModalAnimations(page: Page): Promise<void> {
  await page.locator('.neba-modal-container').evaluate(async (el) => {
    await Promise.all(el.getAnimations().map((animation) => animation.finished));
  });
}

test.describe.configure({ mode: 'serial' });

test.describe('edit-series-logo help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Series.EditSeriesLogo');
  });

  test('detail logo button + edit logo window', async ({ page }) => {
    await page.goto(`/series/${MOCK_SERIES_ID}`);
    await page.waitForSelector('.series-field');

    const button = page.getByRole('button', { name: `Edit logo for ${MOCK_SERIES_NAME}` });
    await expect(button).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'detail-edit-logo-button.png') });

    await button.click();
    await page.waitForSelector('.neba-modal-content');
    await waitForModalAnimations(page);
    await expect(page.locator('.elm-subtitle')).toContainText(MOCK_SERIES_NAME);

    await page.screenshot({ path: path.join(outDir, 'edit-logo-window.png') });

    await page.getByRole('button', { name: 'Cancel' }).click();
    await expect(page.locator('.neba-modal-content')).toBeHidden();
  });
});
