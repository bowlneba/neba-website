import { test, expect } from '@playwright/test';
import path from 'node:path';

import { MOCK_SERIES_ID } from '../mock-api/mock-api-server';

/**
 * Generates the screenshots embedded in docs/help/edit-series.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * The script only opens the pre-filled edit form and never saves it, so it is safe to re-run.
 */

const outDir = path.join('docs', 'help', 'images', 'edit-series');

test.describe.configure({ mode: 'serial' });

test.describe('edit-series help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Series.EditSeries');
  });

  test('detail edit button + pre-filled edit form', async ({ page }) => {
    await page.goto(`/series/${MOCK_SERIES_ID}`);
    await page.waitForSelector('.series-field');
    await expect(page.getByRole('link', { name: 'Edit' })).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'detail-edit-button.png') });

    await page.getByRole('link', { name: 'Edit' }).click();
    await page.waitForSelector('#series-name');
    await expect(page.locator('li.series-grid__row')).toHaveCount(11);

    await page.screenshot({ path: path.join(outDir, 'edit-form.png'), fullPage: true });
  });
});
