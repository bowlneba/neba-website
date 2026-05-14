import { test, expect } from '@playwright/test';

import {
  MOCK_SEASONS,
  MOCK_SEASON_TOURNAMENTS,
  MOCK_TOURNAMENT_DETAIL,
} from './mock-api/mock-api-server';

test.describe('Tournaments page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  const activeSeason = MOCK_SEASONS.items[0];
  const pastTournament = MOCK_SEASON_TOURNAMENTS.items[0];

  test.beforeEach(async ({ page }) => {
    await page.goto('/tournaments');
    await page.waitForSelector('.tournament-tab-bar');
  });

  test('renders page title', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Tournaments');
  });

  test('displays active season description', async ({ page }) => {
    await expect(page.locator('.tournaments-page__eyebrow')).toContainText(activeSeason.description);
  });

  test('Past tab shows tournament cards', async ({ page }) => {
    await page.locator('.neba-segment-button', { hasText: 'Past' }).click();
    await page.waitForSelector('.tournament-past-card');
    await expect(page.locator('.tournament-past-card__title', { hasText: pastTournament.name })).toBeVisible();
  });

  test('Past tab card links to tournament detail', async ({ page }) => {
    await page.locator('.neba-segment-button', { hasText: 'Past' }).click();
    await page.waitForSelector('.tournament-past-card');
    await page.locator('.tournament-past-card__title a').first().click();
    await expect(page).toHaveURL(new RegExp(`/tournaments/${MOCK_TOURNAMENT_DETAIL.id}$`));
  });
});
