import { test, expect } from '@playwright/test';
import path from 'node:path';

import {
  MOCK_TOURNAMENT_INVITATIONAL_EMPTY_ID,
  MOCK_TOURNAMENT_INVITATIONAL_ERRORS_ID,
  MOCK_TOURNAMENT_INVITATIONAL_UPCOMING_ID,
  perProjectTournamentId,
} from '../mock-api/mock-api-server';

/**
 * Generates the screenshots embedded in docs/help/invitational-eligibility.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * The list is read-only and the forced failure is reset, so the script is safe to re-run.
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

    await section.locator('.eligible-bowlers__search').fill('nobody');
    await expect(section.locator('.eligible-bowlers__no-match')).toBeVisible();
    await section.screenshot({ path: path.join(outDir, 'no-match.png') });
  });

  test('nobody qualified yet', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_INVITATIONAL_EMPTY_ID}`);
    const section = page.locator('.eligible-bowlers');
    await expect(section.locator('.eligible-bowlers__empty')).toBeVisible();
    await section.screenshot({ path: path.join(outDir, 'empty.png') });
  });

  test('the list failed to load', async ({ page }, testInfo) => {
    const tournamentId = perProjectTournamentId(MOCK_TOURNAMENT_INVITATIONAL_ERRORS_ID, testInfo.project.name);
    const failPath = `/tournaments/${tournamentId}/eligible-bowlers`;

    try {
      // A 4xx: a 5xx would make the site retry past the expect timeout.
      await page.request.post(`http://localhost:5151/__mock/fail?path=${failPath}&status=400`);
      await page.goto(`/tournaments/${tournamentId}`);
      const section = page.locator('.eligible-bowlers');
      await expect(section.locator('.eligible-bowlers__error')).toBeVisible();
      await section.screenshot({ path: path.join(outDir, 'load-failed.png') });
    } finally {
      await page.request.post(`http://localhost:5151/__mock/reset?path=${failPath}`);
    }
  });
});
