import { test, expect, type Page } from '@playwright/test';

import { fillDateInput } from './fixtures/date-input-helpers';
import {
  MOCK_SERIES_ID,
  MOCK_SERIES_LOGO_ID,
  MOCK_SERIES_NAME,
  MOCK_SERIES_NO_FINALE_ID,
  MOCK_SERIES_PLACEMENT_STAGE_TOURNAMENT_ID,
  MOCK_SERIES_QUALIFYING_STAGE_TOURNAMENT_ID,
  MOCK_TOURNAMENT_ID,
  perProjectTournamentId,
} from './mock-api/mock-api-server';

const LOGO_PNG = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=',
  'base64'
);

async function openPastTab(page: Page) {
  await page.goto('/tournaments');
  await page.waitForSelector('.tournament-tab-bar');
  await page.locator('.neba-segment-button', { hasText: 'Past' }).click();
  await page.waitForSelector('.tournament-past-card');
}

test.describe('Series (public)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('lists the current Series with its next Stage and opens the field chart', async ({ page }) => {
    await page.goto('/series');

    const current = page.locator('.series-current').first();
    await expect(current).toContainText(MOCK_SERIES_NAME);
    await expect(current.locator('.series-current__next')).toContainText('Westgate Lanes');
    await expect(current.locator('img')).toBeVisible();

    await current.click();
    await expect(page).toHaveURL(new RegExp(`/series/${MOCK_SERIES_ID}$`));

    const bars = page.locator('.series-field__bar');
    await expect(bars.nth(0)).toHaveText('Up to 62');
    await expect(bars.nth(1)).toHaveText('40');
    await expect(bars.nth(2)).toHaveText('20');
    await expect(bars.nth(3)).toHaveText('4');
  });

  test('a Stage tournament link goes to the tournament', async ({ page }) => {
    await page.goto(`/series/${MOCK_SERIES_ID}`);
    await page.locator('.series-stage-list a').first().click();
    await expect(page).toHaveURL(/\/tournaments\/01JX00000000000000000007\d\d$/);
  });

  test('shows the Finale block with its date, time and host', async ({ page }) => {
    await page.goto(`/series/${MOCK_SERIES_ID}`);

    const finale = page.locator('.series-finale');
    await expect(finale).toContainText('Sat 9/28 at 2:00 PM');
    await expect(finale).toContainText("Callahan's Bowl-O-Rama, Seekonk, MA");
    await expect(page.locator('.series-stage-list li').last()).toContainText('Top 4 to the Finale');
  });

  test('a Series without a Finale shows final standings and the no-Finale caption', async ({ page }) => {
    await page.goto(`/series/${MOCK_SERIES_NO_FINALE_ID}`);

    await expect(page.locator('.series-stage-list li').last()).toContainText('Final standings');
    await expect(page.locator('.series-stage-list li').last()).toContainText('(no Finale)');
    await expect(page.locator('.series-field__caption')).toContainText('No Finale');
    await expect(page.locator('.series-finale')).toHaveCount(0);
  });

  test('the Tournaments menu has a Series link', async ({ page }) => {
    await page.goto('/');
    const navItem = page.locator('.neba-nav-item[data-action="toggle-dropdown"]').first();
    await navItem.hover();
    await expect(navItem.locator('.neba-dropdown-link', { hasText: 'Series' })).toBeVisible();
  });
});

test.describe('Series on the tournament pages (public)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('the Past tab tags a Stage tournament and links to its Series', async ({ page }) => {
    await openPastTab(page);

    const card = page.locator('.tournament-past-card', { hasText: 'NEBA Series Qualifier' });
    const tag = card.locator('a.tournament-series-tag');
    await expect(tag).toContainText(MOCK_SERIES_NAME);
    await expect(tag).toContainText('Qualifying Round');

    await tag.click();
    await expect(page).toHaveURL(new RegExp(`/series/${MOCK_SERIES_ID}$`));
  });

  test('a tournament that is not a Stage has no tag', async ({ page }) => {
    await openPastTab(page);

    const card = page.locator('.tournament-past-card', { hasText: 'NEBA Spring Classic' });
    await expect(card).toBeVisible();
    await expect(card.locator('.tournament-series-tag')).toHaveCount(0);
  });

  test('the tournament page of a Stage shows the Series callout', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_SERIES_QUALIFYING_STAGE_TOURNAMENT_ID}`);
    await page.waitForSelector('.td-hero');

    const callout = page.locator('.tournament-series-callout');
    await expect(callout).toContainText('This tournament is part of a Series');
    await expect(callout).toContainText('Qualifying Round');
    await expect(callout).toContainText('Top 8 finishers enter the Series field.');
    await expect(callout.getByRole('link', { name: /View Series/ })).toHaveAttribute('href', `/series/${MOCK_SERIES_ID}`);
  });

  test('the callout on a Placement Round names the Finale', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_SERIES_PLACEMENT_STAGE_TOURNAMENT_ID}`);
    await page.waitForSelector('.td-hero');

    const callout = page.locator('.tournament-series-callout');
    await expect(callout).toContainText('Placement Round');
    await expect(callout).toContainText('Finale: Sat 9/28 at 2:00 PM.');
  });

  test('a tournament that is not a Stage has no callout', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}`);
    await page.waitForSelector('.td-hero');
    await expect(page.locator('.tournament-series-callout')).toHaveCount(0);
  });

  test('the Upcoming tab shows Finales as cards in date order', async ({ page }) => {
    await page.goto('/tournaments');
    await page.waitForSelector('.tournament-tab-bar');

    const cards = page.locator('.series-finale-card');
    await expect(cards).toHaveCount(2);
    await expect(cards.nth(0)).toContainText(`${MOCK_SERIES_NAME} Finale`);
    await expect(cards.nth(0)).toContainText("Callahan's Bowl-O-Rama, Seekonk, MA");
    await expect(cards.nth(0)).toContainText('4 finalists');
    await expect(cards.nth(0).locator('img')).toBeVisible();
    await expect(cards.nth(1)).toContainText('Fall Challenge Series Finale');
    await expect(cards.nth(1)).toContainText('Host center to be announced');
    await expect(cards.nth(1).locator('.series-logo-tile__initials')).toHaveText('FC');
  });

  test('a Finale title goes to its Series', async ({ page }) => {
    await page.goto('/tournaments');
    await page.waitForSelector('.series-finale-card');

    await page.locator('.series-finale-card h3 a').first().click();
    await expect(page).toHaveURL(new RegExp(`/series/${MOCK_SERIES_ID}$`));
  });

  test('a format filter hides Finales and All shows them again', async ({ page }) => {
    await page.goto('/tournaments');
    await page.waitForSelector('.series-finale-card');

    await page.locator('.tournament-filter-bar__type-filter button', { hasText: 'Doubles' }).click();
    await expect(page.locator('.series-finale-card')).toHaveCount(0);

    await page.locator('.tournament-filter-bar__type-filter button', { hasText: 'All' }).click();
    await expect(page.locator('.series-finale-card')).toHaveCount(2);
  });

  test('the Past tab shows the past Finale with the past styling', async ({ page }) => {
    await openPastTab(page);

    const card = page.locator('.series-finale-card');
    await expect(card).toHaveCount(1);
    await expect(card).toContainText('Winter Classic Series Finale');
    await expect(card).toHaveClass(/series-finale-card--past/);
  });
});

test.describe('Series (admin)', () => {
  test.use({ viewport: { width: 1200, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post(
      '/__test/login?permissions=Series.CreateSeries,Series.EditSeries,Series.DeleteSeries,Series.EditSeriesLogo'
    );
  });

  test('creates a Series from Qualifying, Survivor and Placement rows', async ({ page }) => {
    await page.goto('/series/new');
    await page.waitForSelector('#series-name');

    await page.locator('#series-name').fill('E2E Series');

    for (let index = 0; index < 3; index++) {
      await page.getByRole('button', { name: '+ Add Stage' }).click();
    }

    await page.locator('#stage-0-kind').selectOption('Qualifying');
    await page.locator('#stage-0-tournament').selectOption({ label: 'Sat 4/11 · Series Pick One' });
    await page.locator('#stage-1-kind').selectOption('Survivor');
    await page.locator('#stage-1-tournament').selectOption({ label: 'Sat 4/18 · Series Pick Two' });
    await fillDateInput(page, 'stage-1-squad-date', '2026-04-18');
    await page.locator('#stage-1-squad-time').fill('09:00');
    await page.locator('#stage-2-kind').selectOption('Placement');
    await page.locator('#stage-2-tournament').selectOption({ label: 'Sat 4/25 · Series Pick Three' });

    await page.getByRole('button', { name: 'Create Series' }).click();

    await expect(page).toHaveURL(new RegExp(`/series/${MOCK_SERIES_ID}$`));

    const sent = await page.request
      .get('http://localhost:5151/__mock/last-request?method=POST&path=/series')
      .then((r) => r.json());
    expect(sent.series.name).toBe('E2E Series');
    expect(sent.series.stages.map((stage: { kind: string }) => stage.kind)).toEqual(['Qualifying', 'Survivor', 'Placement']);
    expect(sent.series.stages[1].squadStartTime).toBe('09:00:00');
  });

  test('shows the server message under the row it names', async ({ page }) => {
    await page.goto('/series/new');
    await page.waitForSelector('#series-name');

    await page.locator('#series-name').fill('fail');
    for (let index = 0; index < 2; index++) {
      await page.getByRole('button', { name: '+ Add Stage' }).click();
    }

    await page.locator('#stage-0-tournament').selectOption({ label: 'Sat 4/11 · Series Pick One' });
    await page.locator('#stage-1-tournament').selectOption({ label: 'Sat 4/18 · Series Pick Two' });
    await page.getByRole('button', { name: 'Create Series' }).click();

    const rows = page.locator('li.series-grid__row');
    await expect(rows.nth(1).locator('.series-grid__error')).toContainText('This Stage must follow the previous Stage by date.');
    await expect(rows.nth(0).locator('.series-grid__error')).toHaveCount(0);
    await expect(page.getByText('1 Stage needs changes before this Series can be saved.')).toBeVisible();
    await expect(page).toHaveURL(/\/series\/new$/);
  });

  test('shows the Finale error under the Finale section when the count is empty', async ({ page }) => {
    await page.goto('/series/new');
    await page.waitForSelector('#series-name');

    await page.locator('#series-name').fill('E2E Finale Series');
    await page.getByLabel('This Series has a Finale').check();
    await page.locator('#finale-count').fill('');
    await page.getByRole('button', { name: 'Create Series' }).click();

    await expect(page.locator('.series-finale-editor .series-editor__row-error')).toContainText(
      'The Finale needs at least one finalist.'
    );
    await expect(page).toHaveURL(/\/series\/new$/);
  });

  test('deletes a Series after confirming and goes back to the list', async ({ page }) => {
    await page.goto(`/series/${MOCK_SERIES_ID}`);
    await page.waitForSelector('.series-field');

    await page.getByRole('button', { name: 'Delete' }).first().click();
    await page.locator('.confirm-action-modal-confirm').click();

    await expect(page).toHaveURL(/\/series$/);
  });

  test('the locked Series cannot be deleted', async ({ page }) => {
    await page.goto(`/series/${MOCK_SERIES_NO_FINALE_ID}`);
    await page.waitForSelector('.series-field');

    await expect(page.locator('button.neba-btn-danger')).toBeDisabled();
    await expect(page.locator('.series-detail__lock-note')).toContainText("can't be deleted");
  });
});

test.describe('Series logo (admin)', () => {
  test.use({ viewport: { width: 1200, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Series.EditSeriesLogo');
  });

  test('uploads a logo from the detail page and the hero shows it', async ({ page }, testInfo) => {
    const seriesId = perProjectTournamentId(MOCK_SERIES_LOGO_ID, testInfo.project.name);
    await page.goto(`/series/${seriesId}`);
    await page.waitForSelector('.series-field');
    await expect(page.locator('.page-title-bar img')).toHaveCount(0);

    await page.getByRole('button', { name: 'Edit logo for Logo Edit Series' }).click();
    await page.locator('input.neba-file-upload-input').setInputFiles({ name: 'logo.png', mimeType: 'image/png', buffer: LOGO_PNG });
    await expect(page.locator('.neba-file-upload-item-status--success')).toBeVisible();
    await page.getByRole('button', { name: 'Save logo' }).click();

    await expect(page.locator('.neba-toast')).toContainText('Logo Saved');
    await expect(page.locator('.page-title-bar img')).toHaveAttribute('src', /series\/logo\/e2e-test-logo\.png$/);

    const sent = await page.request
      .get(`http://localhost:5151/__mock/last-request?method=PUT&path=/series/${seriesId}/logo`)
      .then((r) => r.json());
    expect(sent.input.logo.path).toBe('series/logo/e2e-test-logo.png');
  });
});

test.describe('Series on a phone', () => {
  test.use({ viewport: { width: 390, height: 844 } });

  test('the detail rows stack on a phone', async ({ page }) => {
    await page.goto(`/series/${MOCK_SERIES_ID}`);
    await page.waitForSelector('.series-stage-list');

    const row = page.locator('.series-stage-list li').first();
    const tournament = await row.locator('.series-stage__tournament').boundingBox();
    const advancing = await row.locator('.series-stage__advancing').boundingBox();
    expect(tournament).not.toBeNull();
    expect(advancing).not.toBeNull();
    // The advancing text wraps under the tournament instead of sitting beside it.
    expect(advancing!.y).toBeGreaterThanOrEqual(tournament!.y + tournament!.height - 1);
  });

  test('the editor grid collapses to a narrow layout', async ({ page }) => {
    await page.request.post('/__test/login?permissions=Series.CreateSeries');
    await page.goto('/series/new');
    await page.waitForSelector('#series-name');
    await page.getByRole('button', { name: '+ Add Stage' }).click();

    await expect(page.locator('.series-grid__head')).toBeHidden();
    await expect(page.locator('li.series-grid__row .series-grid__cell-label').first()).toBeVisible();
    const hasHorizontalScroll = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
    expect(hasHorizontalScroll).toBe(false);
  });
});
