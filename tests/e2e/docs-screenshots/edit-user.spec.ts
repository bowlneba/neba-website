import { test, expect } from '@playwright/test';
import path from 'node:path';

/**
 * Generates the screenshots embedded in docs/help/edit-user.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * Nothing here saves a change, so the script is safe to re-run against the same mock data.
 */

const outDir = path.join('docs', 'help', 'images', 'edit-user');

test.describe.configure({ mode: 'serial' });

test.describe('edit-user help screenshots', () => {
  // Tall enough to hold the whole table, so no full-page capture is needed (a full-page capture
  // repeats the sticky header mid-image).
  test.use({ viewport: { width: 1200, height: 2300 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post(
      '/__test/login?permissions=System.GetUsers,System.EditUser,Tournaments.DeleteTournament,Tournaments.ManageTournamentStatus',
    );
  });

  test('users list with edit icons + edit form with changes', async ({ page }) => {
    await page.goto('/account/users');
    await page.waitForSelector('.neba-table');

    await page.screenshot({ path: path.join(outDir, 'users-list.png') });

    await page.getByRole('link', { name: 'Edit invited.staff@bowlneba.com' }).click();
    await page.waitForSelector('input[type="radio"]');

    const deleteRow = page.getByRole('row', { name: /Delete Tournament/ });
    await deleteRow.locator('label[for$="-added"]').click();
    await expect(deleteRow).toContainText('Allowed');

    // The switch thumb slides over 150ms; wait for the animation to finish before capturing.
    await deleteRow.locator('.override-thumb').evaluate(async (el) => {
      await Promise.all(el.getAnimations().map((animation) => animation.finished));
    });

    await page.screenshot({ path: path.join(outDir, 'edit-form.png') });
  });
});
