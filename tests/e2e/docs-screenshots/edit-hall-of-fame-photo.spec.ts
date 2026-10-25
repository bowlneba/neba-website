import { test, expect, type Page } from '@playwright/test';
import path from 'node:path';

/**
 * Generates the screenshots embedded in docs/help/edit-hall-of-fame-photo.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * Non-mutating: the mock upload only stages a file, and every flow ends by closing the window
 * without saving, so the script can be re-run against the same mock data. The mock's Hall of
 * Fame photos are also reset first, in case an e2e run saved a change.
 */

const outDir = path.join('docs', 'help', 'images', 'edit-hall-of-fame-photo');

const PHOTO_PNG = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=',
  'base64'
);

test.describe.configure({ mode: 'serial' });

test.beforeAll(async ({ playwright }) => {
  const api = await playwright.request.newContext();
  await api.post('http://localhost:5151/__mock/hall-of-fame/reset');
  await api.dispose();
});

async function closeWithoutSaving(page: Page, hasUnsavedChanges: boolean) {
  await page.getByRole('button', { name: 'Cancel' }).click();
  if (hasUnsavedChanges) {
    await page.getByRole('button', { name: 'Discard', exact: true }).click();
  }
  await expect(page.locator('.ehp-subtitle')).toHaveCount(0);
}

test.describe('edit-hall-of-fame-photo help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 1000 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=HallOfFame.EditHallOfFamePhoto');
    await page.goto('/hall-of-fame');
    await page.waitForSelector('.inductee-item');
  });

  test('the Hall of Fame page with edit buttons', async ({ page }) => {
    await expect(page.locator('button.hof-photo-edit').first()).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'edit-buttons.png') });
  });

  test('the window for an inductee with no photo', async ({ page }) => {
    await page.getByRole('button', { name: 'Edit photo for Jane Smith' }).click();
    await expect(page.locator('.ehp-empty__box')).toContainText('No photo yet');

    await page.getByRole('dialog').screenshot({ path: path.join(outDir, 'no-photo.png') });
    await closeWithoutSaving(page, false);
  });

  test('the window for an inductee with a current photo', async ({ page }) => {
    await page.getByRole('button', { name: 'Edit photo for Bob Johnson' }).click();
    await expect(page.locator('.ehp-state')).toContainText('Current photo');
    await page.waitForFunction(() => {
      const image = document.querySelector<HTMLImageElement>('[data-fp-surface] img');
      return !!image && image.naturalWidth > 0;
    });

    await page.getByRole('dialog').screenshot({ path: path.join(outDir, 'current-photo.png') });
    await closeWithoutSaving(page, false);
  });

  test('a new photo zoomed in with the ring on the face', async ({ page }) => {
    await page.getByRole('button', { name: 'Edit photo for Jane Smith' }).click();
    await page.locator('input.neba-file-upload-input').setInputFiles({
      name: 'photo.png',
      mimeType: 'image/png',
      buffer: PHOTO_PNG,
    });
    await expect(page.locator('.neba-file-upload-item-status--success')).toBeVisible();
    await page.waitForFunction(() => {
      const image = document.querySelector<HTMLImageElement>('[data-fp-surface] img');
      return !!image && image.naturalWidth > 0;
    });

    await page.getByLabel('Zoom').fill('200');
    const box = (await page.locator('.focal-picker__surface').boundingBox())!;
    // The mock photo's face is about 70% across and 38% down the picture.
    await page.mouse.click(box.x + box.width * 0.7, box.y + box.height * 0.38);
    await page.mouse.move(0, 0);

    await page.getByRole('dialog').screenshot({ path: path.join(outDir, 'new-photo-aligned.png') });
    await closeWithoutSaving(page, true);
  });
});
