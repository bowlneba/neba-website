import { test, expect, type Page } from '@playwright/test';
import path from 'node:path';

import { MOCK_SERIES_ID } from '../mock-api/mock-api-server';

/**
 * Generates the screenshots embedded in docs/help/delete-series.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * The script cancels the confirmation dialog, so nothing is deleted and it is safe to re-run.
 */

const outDir = path.join('docs', 'help', 'images', 'delete-series');

/** Wait for the modal's own animations to finish so the screenshot is not taken mid-transition. */
async function waitForModalAnimations(page: Page): Promise<void> {
  await page.locator('.neba-modal-container').evaluate(async (el) => {
    await Promise.all(el.getAnimations().map((animation) => animation.finished));
  });
}

test.describe.configure({ mode: 'serial' });

test.describe('delete-series help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Series.DeleteSeries');
  });

  test('detail delete button + confirm dialog', async ({ page }) => {
    await page.goto(`/series/${MOCK_SERIES_ID}`);
    await page.waitForSelector('.series-field');
    await expect(page.getByRole('button', { name: 'Delete' }).first()).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'detail-delete-button.png') });

    await page.getByRole('button', { name: 'Delete' }).first().click();
    await page.waitForSelector('.neba-modal-content');
    await waitForModalAnimations(page);

    await page.screenshot({ path: path.join(outDir, 'detail-confirm-dialog.png') });

    await page.locator('button.confirm-action-modal-cancel').click();
    await expect(page.locator('.neba-modal-content')).toBeHidden();
  });
});
