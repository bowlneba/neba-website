import { test, expect } from '@playwright/test';
import path from 'node:path';

/**
 * Generates the screenshots embedded in docs/help/edit-sponsor.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * Unlike delete, editing has no natural "undo" step, so this script stops at the populated,
 * pre-submit form rather than actually saving changes to the mock sponsor.
 */

const outDir = path.join('docs', 'help', 'images', 'edit-sponsor');

test.describe.configure({ mode: 'serial' });

test.describe('edit-sponsor help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Sponsors.EditSponsor');
  });

  test('sponsor detail edit button + populated edit form', async ({ page }) => {
    await page.goto('/sponsors/pro-shop-plus');
    await page.waitForSelector('.sponsor-detail__badge, h1');

    await expect(page.getByRole('link', { name: 'Edit Sponsor' })).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'sponsor-detail-edit-button.png') });

    await page.getByRole('link', { name: 'Edit Sponsor' }).click();
    await page.waitForSelector('#name');

    // fullPage so every section (Logo, Links & Content, Business Address, Phone Numbers,
    // Contact Person) is captured, not just the first viewport.
    await page.screenshot({ path: path.join(outDir, 'edit-form.png'), fullPage: true });
  });
});
