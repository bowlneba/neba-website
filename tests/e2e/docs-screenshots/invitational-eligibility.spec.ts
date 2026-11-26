import { test, expect } from '@playwright/test';
import path from 'node:path';

import { MOCK_TOURNAMENT_INVITATIONAL_UPCOMING_ID } from '../mock-api/mock-api-server';

/**
 * Generates the screenshots embedded in docs/help/invitational-eligibility.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * The list is read-only, so the script is safe to re-run.
 */

const outDir = path.join('docs', 'help', 'images', 'invitational-eligibility');

test.describe('invitational-eligibility help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('the list and a search', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_INVITATIONAL_UPCOMING_ID}`);
    const section = page.locator('.eligible-bowlers');
    const rows = section.locator('.eligible-bowlers__table tbody tr');
    await expect(rows).toHaveCount(3);
    await section.screenshot({ path: path.join(outDir, 'list.png') });

    // Text typed before the Blazor circuit attaches is lost, so refill until the list narrows.
    await expect(async () => {
      await section.locator('.eligible-bowlers__search').fill('baxter');
      await expect(rows).toHaveCount(1, { timeout: 1000 });
    }).toPass({ timeout: 20000 });
    await section.screenshot({ path: path.join(outDir, 'search.png') });
  });
});
