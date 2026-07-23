import { test, expect, type Page } from '@playwright/test';
import path from 'node:path';

import { MOCK_TOURNAMENT_ID } from '../mock-api/mock-api-server';

/**
 * Generates the screenshots embedded in docs/help/manage-tournament-sponsors.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * Every action here is cancelled rather than confirmed, so this script can be re-run
 * against the same mock data without needing a reset step.
 */

const outDir = path.join('docs', 'help', 'images', 'manage-tournament-sponsors');

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

test.describe('manage-tournament-sponsors help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Tournaments.ManageSponsors');
  });

  test('panel with add button + add sponsor dialog', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}`);
    await page.waitForSelector('.mts-panel');

    await page.locator('.mts-panel').scrollIntoViewIfNeeded();
    await page.screenshot({ path: path.join(outDir, 'panel-with-add-button.png') });

    await page.locator('.mts-panel__header button').click();
    await page.waitForSelector('.neba-modal-content');
    await waitForModalAnimations(page);

    await expect(page.locator('.neba-modal-content')).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'add-sponsor-dialog.png') });

    await page.locator('.neba-modal-content .neba-btn-secondary').click();

    await expect(page.locator('.neba-modal-content')).toBeHidden();
  });

  test('remove sponsor confirm dialog', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}`);
    await page.waitForSelector('.mts-row__remove');

    await page.locator('.mts-row__remove').first().click();
    await page.waitForSelector('.neba-modal-content');
    await waitForModalAnimations(page);

    await expect(page.locator('.neba-modal-content')).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'remove-confirm-dialog.png') });

    await page.locator('button.confirm-action-modal-cancel').click();

    await expect(page.locator('.neba-modal-content')).toBeHidden();
  });
});
