import { test, expect } from '@playwright/test';
import path from 'node:path';

/**
 * Generates the screenshots embedded in docs/help/create-tournament.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * The create form has no natural "undo" step (unlike delete's cancel-confirm dialog), so this
 * script stops at the blank, pre-submit form rather than actually creating a mock tournament.
 */

const outDir = path.join('docs', 'help', 'images', 'create-tournament');

test.describe.configure({ mode: 'serial' });

test.describe('create-tournament help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Tournaments.CreateTournament');
  });

  test('tournaments list FAB + blank create form', async ({ page }) => {
    await page.goto('/tournaments');
    await page.waitForSelector('h1');

    await expect(page.getByRole('link', { name: 'Create Tournament' })).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'tournaments-list-fab.png') });

    await page.getByRole('link', { name: 'Create Tournament' }).click();
    await page.waitForSelector('#name');

    // fullPage so every section (Venue & Entry Fee, Oil Pattern, Logo) is captured, not
    // just the first viewport.
    await page.screenshot({ path: path.join(outDir, 'create-form.png'), fullPage: true });
  });
});
