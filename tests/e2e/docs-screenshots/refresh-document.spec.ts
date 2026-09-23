import { test, expect } from '@playwright/test';
import path from 'node:path';

/**
 * Generates the screenshots embedded in docs/help/refresh-document.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * The Refresh button is never clicked: a click force-reloads the page, and there's no
 * confirmation step to back out of, so this script stops at the pre-click state.
 */

const outDir = path.join('docs', 'help', 'images', 'refresh-document');

test.describe('refresh-document help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Documents.RefreshDocument');
  });

  test('bylaws refresh button', async ({ page }) => {
    await page.goto('/bylaws');
    await page.waitForSelector('.neba-document-container');

    await expect(page.locator('.toc-sticky button[title="Refresh from Google Drive"]')).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'bylaws-refresh-button.png') });
  });
});
