import { test, expect, type Page } from '@playwright/test';
import path from 'node:path';

import { MOCK_TOURNAMENT_STATUS_ACTIONS_ID } from '../mock-api/mock-api-server';

/**
 * Generates the screenshots embedded in docs/help/cancel-tournament.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * The cancel action here is cancelled (backed out of) rather than confirmed, so this script
 * can be re-run against the same mock data without needing a reset step, and so it doesn't
 * flip MOCK_TOURNAMENT_STATUS_ACTIONS_ID's status out from under TournamentDetail.spec.ts's
 * own "manage status" tests, which share this fixture.
 */

const outDir = path.join('docs', 'help', 'images', 'cancel-tournament');

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

test.describe('cancel-tournament help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Tournaments.ManageTournamentStatus');
  });

  test('detail cancel button + confirm dialog', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_STATUS_ACTIONS_ID}`);
    await page.waitForSelector('h1');

    await expect(page.getByRole('button', { name: 'Cancel Tournament' })).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'detail-cancel-button.png') });

    await page.getByRole('button', { name: 'Cancel Tournament' }).click();
    await page.waitForSelector('.neba-modal-content');
    await waitForModalAnimations(page);

    await expect(page.locator('.neba-modal-content')).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'detail-confirm-dialog.png') });

    await page.locator('button.confirm-action-modal-cancel').click();

    await expect(page.locator('.neba-modal-content')).toBeHidden();
  });
});
