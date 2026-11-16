import { test, expect, Page } from '@playwright/test';

import {
  MOCK_TOURNAMENT_ID,
  MOCK_TOURNAMENT_TEAM_ERRORS_ID,
  MOCK_TOURNAMENT_TEAM_ID,
  perProjectTournamentId,
} from './mock-api/mock-api-server';

const checkTeamButton = (page: Page) => page.getByRole('button', { name: 'Check team', exact: true });

// Types the first letters of a name into a bowler slot and picks the matching search result.
async function pickBowler(page: Page, slot: number, typed: string, optionText: string): Promise<void> {
  const input = page.locator(`#bowler-${slot}`);
  await input.click();
  await input.fill(typed);

  const option = page.locator('.neba-autocomplete-option', { hasText: optionText });
  await expect(option).toBeVisible();
  await option.click();
}

// The page is public: nobody signs in for any of these tests.
test.describe('Team eligibility page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_TEAM_ID}/team-eligibility`);
    await page.waitForSelector('h1');
  });

  test('shows the tournament, the team rules and one picker per team member', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Check team eligibility');
    await expect(page.locator('.team-eligibility__lead')).toContainText('NEBA Team Doubles');
    await expect(page.locator('.team-rules__body')).toContainText('A doubles team may have at most one champion.');
    await expect(page.locator('.team-rules__body')).not.toContainText('Re-entries are permitted.');
    await expect(page.locator('.bowler-picker')).toHaveCount(2);
    await expect(checkTeamButton(page)).toBeDisabled();
  });

  test('has a page title', async ({ page }) => {
    await expect(page).toHaveTitle('Check Team Eligibility - BowlNEBA');
  });

  test('asks for at least three letters before searching', async ({ page }) => {
    await page.locator('#bowler-1').click();
    await page.locator('#bowler-1').fill('ch');

    await expect(page.locator('.neba-autocomplete-empty')).toContainText('Type at least 3 letters to search.');
  });

  test('lists bowlers with their USBC ID, or just the name when there is none', async ({ page }) => {
    await page.locator('#bowler-1').click();
    await page.locator('#bowler-1').fill('nina');
    await expect(page.locator('.neba-autocomplete-option')).toHaveText('Nina Newcomer');

    await page.locator('#bowler-1').fill('champion');
    await expect(page.locator('.neba-autocomplete-option', { hasText: 'Chris Champion (1111-00001)' })).toBeVisible();
  });

  test('says a team that is over the champion limit is not eligible to bowl, and shows how to ask about it', async ({ page }) => {
    await pickBowler(page, 1, 'chris', 'Chris Champion');
    await pickBowler(page, 2, 'cora', 'Cora Champion');
    await expect(checkTeamButton(page)).toBeEnabled();

    await checkTeamButton(page).click();

    await expect(page.locator('.result')).toContainText("This team is not eligible to bowl.");
    await expect(page.locator('.result')).toContainText('A team may have at most 1 previous champion.');
    await expect(page.locator('.result__contact a')).toHaveAttribute('href', 'mailto:info@bowlneba.com');
  });

  test('shows the facts behind the answer, each bowler with how they count and the cards that decided it', async ({ page }) => {
    await pickBowler(page, 1, 'chris', 'Chris Champion');
    await pickBowler(page, 2, 'cora', 'Cora Champion');

    await checkTeamButton(page).click();

    const rows = page.locator('.facts__row');
    await expect(rows).toHaveCount(2);
    await expect(rows.nth(0).locator('.facts__pill')).toHaveText('Multi-time champion');
    await expect(rows.nth(0).locator('.facts__name small')).toContainText('Last NEBA title: 2024');
    // Doubles shows both windows for everyone, so the window does not show who is 60 or older.
    await expect(rows.nth(0).locator('.facts__card')).toHaveCount(2);
    await expect(rows.nth(0).locator('.facts__card--hit')).toHaveCount(2);
    await expect(rows.nth(1).locator('.facts__pill')).toHaveText('One-time champion');
    await expect(page.locator('.facts__check--fail')).toContainText('Champions: 2 (at most 1)');
  });

  test('shows a bowler who is not in the system as counted as a non-champion', async ({ page }) => {
    await pickBowler(page, 1, 'chris', 'Chris Champion');
    await page.locator('.bowler-picker').nth(1).getByRole('button', { name: 'Mark as not in our system' }).click();

    await checkTeamButton(page).click();

    const rows = page.locator('.facts__row');
    await expect(rows).toHaveCount(2);
    await expect(rows.nth(1).locator('.facts__pill')).toHaveText('Counted as a non-champion');
  });

  test('does not scroll sideways on a phone, whatever the facts', async ({ page }) => {
    await page.setViewportSize({ width: 360, height: 800 });
    await pickBowler(page, 1, 'chris', 'Chris Champion');
    await pickBowler(page, 2, 'cora', 'Cora Champion');

    await checkTeamButton(page).click();
    await expect(page.locator('.facts__row').first()).toBeVisible();

    const overflows = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
    expect(overflows).toBe(false);
  });

  test('says a team that is eligible to bowl, with a note that pro titles are not checked', async ({ page }) => {
    await pickBowler(page, 1, 'chris', 'Chris Champion');
    await pickBowler(page, 2, 'nina', 'Nina Newcomer');

    await checkTeamButton(page).click();

    await expect(page.locator('.result')).toContainText('This team is eligible to bowl.');
    await expect(page.locator('.result__note')).toContainText('PBA or PWBA');
    await expect(page.locator('.result__contact')).toHaveCount(0);
  });

  test('recognises the defending champions and names the tournament they won', async ({ page }) => {
    await pickBowler(page, 1, 'dana', 'Dana Defender');
    await pickBowler(page, 2, 'dave', 'Dave Defender');

    await checkTeamButton(page).click();

    await expect(page.locator('.result')).toContainText('These are the defending champions.');
    await expect(page.locator('.result')).toContainText('This team won NEBA Team Doubles (2098 Season).');
  });

  test('does not offer a bowler who is already on the team in another slot', async ({ page }) => {
    await pickBowler(page, 1, 'chris', 'Chris Champion');

    const input = page.locator('#bowler-2');
    await input.click();
    await input.fill('champion');

    await expect(page.locator('.neba-autocomplete-option', { hasText: 'Cora Champion' })).toBeVisible();
    await expect(page.locator('.neba-autocomplete-option', { hasText: 'Chris Champion' })).toHaveCount(0);
  });

  test('lets a bowler who is not in the system be marked, and undone', async ({ page }) => {
    await pickBowler(page, 1, 'nina', 'Nina Newcomer');

    await page.locator('.bowler-picker').nth(1).getByRole('button', { name: 'Mark as not in our system' }).click();
    await expect(page.locator('.bowler-picker__tag')).toContainText('Not in our system');
    await expect(checkTeamButton(page)).toBeEnabled();

    await page.locator('.bowler-picker').nth(1).getByRole('button', { name: 'Undo' }).click();
    await expect(page.locator('#bowler-2')).toBeVisible();
    await expect(checkTeamButton(page)).toBeDisabled();
  });

  test('offers to mark a bowler as not in the system when the search finds nobody', async ({ page }) => {
    await page.locator('#bowler-1').click();
    await page.locator('#bowler-1').fill('zzz');

    await expect(page.locator('.neba-autocomplete-empty')).toContainText('No bowler found.');
    await page.locator('.neba-autocomplete-empty').getByRole('button', { name: 'Mark as not in our system' }).click();

    await expect(page.locator('.bowler-picker__tag')).toContainText('Not in our system');
  });

  test('clears the answer when a bowler changes, and "Check another team" keeps the team', async ({ page }) => {
    await pickBowler(page, 1, 'chris', 'Chris Champion');
    await pickBowler(page, 2, 'nina', 'Nina Newcomer');
    await checkTeamButton(page).click();
    await expect(page.locator('.result')).toBeVisible();

    await page.getByRole('button', { name: 'Check another team' }).click();

    await expect(page.locator('.result')).toHaveCount(0);
    await expect(checkTeamButton(page)).toBeEnabled();
    await expect(page.locator('#bowler-1')).toHaveValue('Chris Champion (1111-00001)');
  });
});

test.describe('Team eligibility page — other cases', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('says a singles tournament is not a team event', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}/team-eligibility`);

    await expect(page.locator('h1')).toContainText('This is not a team event');
  });

  test('sends a visitor to the not-found page when the tournament does not exist', async ({ page }) => {
    await page.goto('/tournaments/01JX0000000000000000009999/team-eligibility');

    await expect(page).toHaveURL(/\/not-found$/);
  });

  test('shows an error and stays on the page when the check fails', async ({ page }, testInfo) => {
    const tournamentId = perProjectTournamentId(MOCK_TOURNAMENT_TEAM_ERRORS_ID, testInfo.project.name);
    const failPath = `/tournaments/${tournamentId}/team-eligibility`;

    try {
      await page.goto(failPath);
      await page.waitForSelector('h1');
      await pickBowler(page, 1, 'chris', 'Chris Champion');
      await pickBowler(page, 2, 'nina', 'Nina Newcomer');

      // A 4xx without a reason: the site shows its generic message. (A 5xx would make the site retry first.)
      await page.request.post(`http://localhost:5151/__mock/fail?path=${failPath}&status=400`);
      await checkTeamButton(page).click();

      await expect(page.locator('.neba-alert-title')).toContainText('Unable to check this team');
      await expect(page.locator('.neba-alert')).toContainText("We couldn't check this team.");
      await expect(page).toHaveURL(new RegExp(`${failPath}$`));
      await expect(page.locator('.result')).toHaveCount(0);
    } finally {
      await page.request.post(`http://localhost:5151/__mock/reset?path=${failPath}`);
    }
  });

  test('shows the reason from the API when the team does not match the tournament', async ({ page }, testInfo) => {
    const tournamentId = perProjectTournamentId(MOCK_TOURNAMENT_TEAM_ERRORS_ID, testInfo.project.name);
    const failPath = `/tournaments/${tournamentId}/team-eligibility`;

    try {
      await page.goto(failPath);
      await page.waitForSelector('h1');
      await pickBowler(page, 1, 'chris', 'Chris Champion');
      await pickBowler(page, 2, 'nina', 'Nina Newcomer');

      await page.request.post(
        `http://localhost:5151/__mock/fail?path=${failPath}&status=422&detail=` +
          encodeURIComponent('This tournament needs exactly 2 bowlers.'));
      await checkTeamButton(page).click();

      await expect(page.locator('.neba-alert')).toContainText('This tournament needs exactly 2 bowlers.');
    } finally {
      await page.request.post(`http://localhost:5151/__mock/reset?path=${failPath}`);
    }
  });
});
