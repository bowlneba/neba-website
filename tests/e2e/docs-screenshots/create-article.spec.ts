import { test, expect } from '@playwright/test';
import path from 'node:path';

/**
 * Generates the screenshots embedded in docs/help/create-article.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * The create form has no natural "undo" step (unlike delete's cancel-confirm dialog), so this
 * script stops at the blank, pre-submit form rather than actually creating a mock article.
 */

const outDir = path.join('docs', 'help', 'images', 'create-article');

test.describe.configure({ mode: 'serial' });

test.describe('create-article help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=News.CreateArticle');
  });

  test('news list FAB + blank create form', async ({ page }) => {
    await page.goto('/news');
    await page.waitForSelector('.news-hero');

    await expect(page.getByRole('link', { name: 'Create Article' })).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'news-list-fab.png') });

    await page.getByRole('link', { name: 'Create Article' }).click();
    await page.waitForSelector('#title');

    // fullPage so the Tournament/Header Image/Attachments sections (added after the initial
    // title/slug/content/status/publish-date fields) are captured, not just the first viewport.
    await page.screenshot({ path: path.join(outDir, 'create-form.png'), fullPage: true });
  });
});
