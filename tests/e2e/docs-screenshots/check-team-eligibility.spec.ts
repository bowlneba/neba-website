import { test, expect, Page } from '@playwright/test';
import path from 'node:path';

import { MOCK_TOURNAMENT_TEAM_ID } from '../mock-api/mock-api-server';
import { fillDateInput } from '../fixtures/date-input-helpers';

/**
 * Generates the screenshots embedded in docs/help/check-team-eligibility.md, and the
 * previous-tournament screenshots in docs/help/create-tournament.md and edit-tournament.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * The eligibility check is read-only and the forms are never submitted, so the script is
 * safe to re-run against the same mock data without a reset step.
 */

const outDir = path.join('docs', 'help', 'images', 'check-team-eligibility');

async function pickBowler(page: Page, slot: number, typed: string, optionText: string): Promise<void> {
  const input = page.locator(`#bowler-${slot}`);
  await input.click();
  await input.fill(typed);
  await page.locator('.neba-autocomplete-option', { hasText: optionText }).click();
}

test.describe.configure({ mode: 'serial' });

test.describe('check-team-eligibility help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('tournament detail with the link and the defending champions', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_TEAM_ID}`);
    await page.waitForSelector('.td-hero');
    await expect(page.locator('a.td-team-link')).toBeVisible();

    // The hero and the rules and eligibility cards under it.
    await page.locator('main.tournament-detail').screenshot({ path: path.join(outDir, 'detail-hero.png'), clip: { x: 0, y: 0, width: 1200, height: 900 } }).catch(async () => {
      await page.screenshot({ path: path.join(outDir, 'detail-hero.png'), fullPage: true });
    });
  });

  test('checker page, picked team and each answer', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_TEAM_ID}/team-eligibility`);
    await page.waitForSelector('.bowler-picker');

    await page.screenshot({ path: path.join(outDir, 'checker-page.png'), fullPage: true });

    await page.locator('#bowler-1').click();
    await page.locator('#bowler-1').fill('champion');
    await expect(page.locator('.neba-autocomplete-option').first()).toBeVisible();
    await page.screenshot({ path: path.join(outDir, 'search-results.png') });
    await page.locator('.neba-autocomplete-option', { hasText: 'Chris Champion' }).click();

    await pickBowler(page, 2, 'cora', 'Cora Champion');
    await page.getByRole('button', { name: 'Check team', exact: true }).click();
    await expect(page.locator('.result')).toBeVisible();
    await page.locator('.result').scrollIntoViewIfNeeded();
    await page.screenshot({ path: path.join(outDir, 'result-not-eligible.png'), fullPage: true });

    await page.getByRole('button', { name: 'Check another team' }).click();
    await page.locator('#bowler-2').click();
    await page.locator('#bowler-2').fill('nina');
    await page.locator('.neba-autocomplete-option', { hasText: 'Nina Newcomer' }).click();
    await page.getByRole('button', { name: 'Check team', exact: true }).click();
    await expect(page.locator('.result--ok')).toBeVisible();
    await page.screenshot({ path: path.join(outDir, 'result-eligible.png'), fullPage: true });
  });

  test('bowler not in the system', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_TEAM_ID}/team-eligibility`);
    await page.waitForSelector('.bowler-picker');

    await page.locator('#bowler-2').click();
    await page.locator('#bowler-2').fill('zzz');
    await expect(page.locator('.neba-autocomplete-empty')).toContainText('No bowler found.');
    await page.locator('.bowler-picker').nth(1).screenshot({ path: path.join(outDir, 'not-in-system-search.png') });

    await page.locator('.neba-autocomplete-empty').getByRole('button', { name: 'Mark as not in our system' }).click();
    await expect(page.locator('.bowler-picker__tag')).toBeVisible();
    await page.locator('.bowler-picker').nth(1).screenshot({ path: path.join(outDir, 'not-in-system-marked.png') });
  });

  test('previous tournament picker on the create form', async ({ page }) => {
    await page.request.post('/__test/login?permissions=Tournaments.CreateTournament');
    await page.goto('/tournaments/new');
    await page.waitForSelector('#name');

    await fillDateInput(page, 'start-date', '2027-03-06');
    await fillDateInput(page, 'end-date', '2027-03-07');
    await expect(page.locator('#previous-tournament option', { hasText: 'NEBA Spring Classic (03/2026)' })).toHaveCount(1);

    await page.locator('#previous-tournament').locator('xpath=ancestor::div[1]').screenshot({
      path: path.join('docs', 'help', 'images', 'create-tournament', 'previous-tournament.png'),
    });
  });
});
