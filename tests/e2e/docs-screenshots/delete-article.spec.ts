import { test, expect, type Page } from '@playwright/test';
import path from 'node:path';

/**
 * Generates the screenshots embedded in docs/help/delete-article.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * Every delete action here is cancelled rather than confirmed, so this script can be
 * re-run against the same mock data without needing a reset step.
 */

const outDir = path.join('docs', 'help', 'images', 'delete-article');

/**
 * .neba-modal-container's fadeIn/slideIn CSS animations run 0.2s; waitForSelector resolves
 * as soon as the element mounts, mid-animation. Wait on the animations' actual completion
 * (Web Animations API) instead of a fixed sleep, so the screenshot isn't taken mid-transition.
 */
async function waitForModalAnimations(page: Page): Promise<void> {
  await page.locator('.neba-modal-container').evaluate(async (el) => {
    await Promise.all(el.getAnimations().map((animation) => animation.finished));
  });
}

test.describe.configure({ mode: 'serial' });

test.describe('delete-article help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=News.DeleteArticle');
  });

  test('list page with delete icons + confirm dialog', async ({ page }) => {
    await page.goto('/news');
    await page.waitForSelector('.news-hero');

    await page.screenshot({ path: path.join(outDir, 'list-with-delete-icons.png') });

    await page.locator('.article-card').first().locator('button.card-delete-btn').click();
    await page.waitForSelector('.neba-modal-content');
    await waitForModalAnimations(page);

    await expect(page.locator('.neba-modal-content')).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'list-confirm-dialog.png') });

    await page.locator('button.confirm-action-modal-cancel').click();

    await expect(page.locator('.neba-modal-content')).toBeHidden();
  });

  test('detail page danger zone + confirm dialog', async ({ page }) => {
    await page.goto('/news/june-lane-pattern');
    await page.waitForSelector('.news-detail-layout');

    await page.screenshot({ path: path.join(outDir, 'detail-danger-zone.png') });

    await page.locator('.sidebar-danger-zone-btn').click();
    await page.waitForSelector('.neba-modal-content');
    await waitForModalAnimations(page);

    await expect(page.locator('.neba-modal-content')).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'detail-confirm-dialog.png') });

    await page.locator('button.confirm-action-modal-cancel').click();

    await expect(page.locator('.neba-modal-content')).toBeHidden();
  });
});
