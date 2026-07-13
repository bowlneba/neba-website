import { test, expect } from '@playwright/test';
import path from 'node:path';

/**
 * Generates the screenshots embedded in docs/help/edit-article.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * Unlike delete, editing has no natural "undo" step, so this script stops at the populated,
 * pre-submit form rather than actually saving changes to the mock article.
 */

const outDir = path.join('docs', 'help', 'images', 'edit-article');

test.describe.configure({ mode: 'serial' });

test.describe('edit-article help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=News.EditArticle');
  });

  test('news list edit icons + populated edit form', async ({ page }) => {
    await page.goto('/news');
    await page.waitForSelector('.news-hero');

    await expect(page.locator('.article-card').first().locator('a.card-edit-btn')).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'list-with-edit-icons.png') });

    await page.locator('.article-card').first().locator('a.card-edit-btn').click();
    await page.waitForSelector('#title');

    // fullPage so the Tournament/Header Image/Attachments sections (below the initial
    // title/slug/content/status/publish-date fields) are captured, not just the first viewport.
    await page.screenshot({ path: path.join(outDir, 'edit-form.png'), fullPage: true });
  });
});
