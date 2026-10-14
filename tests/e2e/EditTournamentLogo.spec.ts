import { test, expect, type Page } from '@playwright/test';

test.describe.configure({ mode: 'serial' });

const MOCK_TOURNAMENT_ID = '01JX0000000000000000000010';
const TOURNAMENT_NAME = 'NEBA Spring Classic';
const LOGO_PNG = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=',
  'base64'
);

async function uploadLogo(page: Page) {
  await page.locator('input.neba-file-upload-input').setInputFiles({
    name: 'logo.png',
    mimeType: 'image/png',
    buffer: LOGO_PNG,
  });
  await expect(page.locator('.neba-file-upload-item-status--success')).toBeVisible();
}

test.describe('Edit tournament logo (unauthenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show the edit-logo button on the tournament detail page', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}`);
    await page.waitForSelector('.td-hero');
    await expect(page.locator('button.tournament-logo-edit')).toHaveCount(0);
  });

  test('does not show the edit-logo button on the tournament schedule', async ({ page }) => {
    await page.goto('/tournaments');
    await page.waitForSelector('.tournament-tab-bar');
    await page.locator('.neba-segment-button', { hasText: 'Past' }).click();
    await page.waitForSelector('.tournament-past-card');
    await expect(page.locator('button.tournament-logo-edit')).toHaveCount(0);
  });
});

test.describe('Edit tournament logo (logo permission only)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Tournaments.EditTournamentLogo');
  });

  test('shows the edit-logo button without the full Edit Tournament link on the detail page', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}`);
    await page.waitForSelector('.td-hero');

    await expect(page.getByRole('button', { name: `Edit logo for ${TOURNAMENT_NAME}` })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Edit Tournament' })).toHaveCount(0);
  });

  test('uploads a replacement logo and saves it from the detail page', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}`);
    await page.waitForSelector('.td-hero');

    await page.getByRole('button', { name: `Edit logo for ${TOURNAMENT_NAME}` }).click();
    await expect(page.getByRole('dialog')).toContainText('Edit logo');
    await expect(page.locator('.elm-subtitle')).toContainText(TOURNAMENT_NAME);
    await expect(page.getByRole('button', { name: 'Save logo' })).toBeDisabled();

    await uploadLogo(page);
    await expect(page.locator('.elm-badge')).toContainText('New logo');

    await page.getByRole('button', { name: 'Save logo' }).click();

    await expect(page.locator('.neba-toast')).toContainText('Logo Saved');
    await expect(page.locator('.elm-subtitle')).toHaveCount(0);

    const lastRequest = await page.request
      .get(`http://localhost:5151/__mock/last-request?method=PUT&path=/tournaments/${MOCK_TOURNAMENT_ID}/logo`)
      .then((r) => r.json());
    expect(lastRequest.input.logo.path).toBe('tournaments/logo/e2e-test-logo.png');
  });

  test('asks before discarding an uploaded logo when Cancel is clicked', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}`);
    await page.waitForSelector('.td-hero');

    await page.getByRole('button', { name: `Edit logo for ${TOURNAMENT_NAME}` }).click();
    await uploadLogo(page);

    await page.getByRole('button', { name: 'Cancel' }).click();
    await expect(page.getByText('Discard unsaved changes?')).toBeVisible();

    await page.getByRole('button', { name: 'Discard', exact: true }).click();
    await expect(page.locator('.elm-subtitle')).toHaveCount(0);
  });

  test('opens the edit-logo modal for a tournament card on the schedule', async ({ page }) => {
    await page.goto('/tournaments');
    await page.waitForSelector('.tournament-tab-bar');
    await page.locator('.neba-segment-button', { hasText: 'Past' }).click();
    await page.waitForSelector('.tournament-past-card');

    await page
      .locator('.tournament-past-card', { hasText: TOURNAMENT_NAME })
      .getByRole('button', { name: `Edit logo for ${TOURNAMENT_NAME}` })
      .click();

    await expect(page.locator('.elm-subtitle')).toContainText(TOURNAMENT_NAME);
  });
});
