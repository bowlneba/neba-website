import { test, expect } from '@playwright/test';

test.describe.configure({ mode: 'serial' });

const MOCK_TOURNAMENT_ID = '01JX0000000000000000000010';

test.describe('Tournament detail page — edit tournament (unauthenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show the Edit Tournament button', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}`);
    await page.waitForSelector('.td-hero');
    await expect(page.getByRole('link', { name: 'Edit Tournament' })).toHaveCount(0);
  });

  test('shows a permission message when navigating directly to the edit page', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}/edit`);
    await expect(page.locator('.news-empty-text')).toContainText("don't have permission to edit tournaments");
  });
});

test.describe('Tournament detail page — edit tournament (authenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Tournaments.EditTournament');
  });

  // A failed assertion earlier in a test can abort it before its own /__mock/reset call runs,
  // leaving the override in place and breaking later tests that hit the same path. Resetting
  // here runs regardless of outcome.
  test.afterEach(async ({ page }) => {
    await page.request.post(`http://localhost:5151/__mock/reset?path=/tournaments/${MOCK_TOURNAMENT_ID}`);
  });

  test('shows the Edit Tournament button and navigates to the edit page', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}`);
    await page.waitForSelector('.td-hero');

    await expect(page.getByRole('link', { name: 'Edit Tournament' })).toBeVisible();
    await page.getByRole('link', { name: 'Edit Tournament' }).click();

    await expect(page).toHaveURL(new RegExp(`/tournaments/${MOCK_TOURNAMENT_ID}/edit$`));
    await page.waitForSelector('#name');
  });

  test("pre-fills the form with the tournament's existing data", async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}/edit`);
    await page.waitForSelector('#name');

    await expect(page.locator('#name')).toHaveValue('NEBA Spring Classic');
    await expect(page.locator('#tournament-type')).toHaveValue('Open');
    await expect(page.locator('#entry-fee')).toHaveValue('75');
  });

  test('shows validation errors when clearing the required name field', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}/edit`);
    await page.waitForSelector('#name');

    await page.locator('#name').fill('');
    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-card')).toContainText('Name is required.');
    await expect(page).toHaveURL(new RegExp(`/tournaments/${MOCK_TOURNAMENT_ID}/edit$`));
  });

  test('saves changes and navigates to the tournament detail page', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}/edit`);
    await page.waitForSelector('#name');

    await page.locator('#name').fill('NEBA Spring Classic Updated');
    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-toast')).toContainText('Tournament Updated');
    await expect(page).toHaveURL(new RegExp(`/tournaments/${MOCK_TOURNAMENT_ID}$`));
  });

  test('shows an error alert and stays on the page when saving fails', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}/edit`);
    await page.waitForSelector('#name');

    // Set after the page loads — GET and PUT for this tournament share the same mock path
    // (keyed by id, not slug), so registering the override before navigating would also fail
    // the prefill GET.
    await page.request.post(
      `http://localhost:5151/__mock/fail?path=/tournaments/${MOCK_TOURNAMENT_ID}&status=409`
    );

    await page.locator('#name').fill('NEBA Spring Classic Updated');
    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-alert-title')).toContainText('Unable to Save Tournament');
    await expect(page).toHaveURL(new RegExp(`/tournaments/${MOCK_TOURNAMENT_ID}/edit$`));

    await page.request.post(`http://localhost:5151/__mock/reset?path=/tournaments/${MOCK_TOURNAMENT_ID}`);
  });

  test('cancel prompts to discard unsaved changes and returns to the detail page on confirm', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}/edit`);
    await page.waitForSelector('#name');

    await page.locator('#name').fill('Unsaved Change');
    await page.locator('button.neba-btn-secondary', { hasText: 'Cancel' }).click();

    await expect(page.locator('.neba-modal-backdrop')).toContainText('Discard unsaved changes?');
    await page.locator('button.confirm-action-modal-confirm').click();

    await expect(page).toHaveURL(new RegExp(`/tournaments/${MOCK_TOURNAMENT_ID}$`));
  });
});
