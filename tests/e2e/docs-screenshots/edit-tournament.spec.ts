import { test, expect } from '@playwright/test';
import path from 'node:path';

import { MOCK_TOURNAMENT_ID } from '../mock-api/mock-api-server';

/**
 * Generates the screenshots embedded in docs/help/edit-tournament.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * This script only navigates to the pre-filled edit form and never submits it, so it's
 * safe to re-run against the same mock data without a reset step.
 */

const outDir = path.join('docs', 'help', 'images', 'edit-tournament');

test.describe.configure({ mode: 'serial' });

test.describe('edit-tournament help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Tournaments.EditTournament');
  });

  test('detail edit button + pre-filled edit form', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}`);
    await page.waitForSelector('h1');

    await expect(page.getByRole('link', { name: 'Edit Tournament' })).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'detail-edit-button.png') });

    await page.getByRole('link', { name: 'Edit Tournament' }).click();
    await page.waitForSelector('#name');

    // fullPage so every section (Venue & Entry Fee, Oil Pattern, Logo) is captured, not
    // just the first viewport.
    await page.screenshot({ path: path.join(outDir, 'edit-form.png'), fullPage: true });
  });
});