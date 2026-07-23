import { test, expect, Page } from '@playwright/test';

test.describe.configure({ mode: 'serial' });

// NebaDateInput renders month/day/year as separate segment inputs (see
// src/Neba.Website.Server/Components/NebaDateInput.razor) — `id` lands on the month segment only,
// and the segment's own JS auto-advances focus on keydown, which .fill() never fires. Fill each
// segment directly so the 'input' event listener (which does run on .fill()) reports the value.
async function fillDateInput(page: Page, monthInputId: string, isoDate: string): Promise<void> {
  const [year, month, day] = isoDate.split('-');
  const monthInput = page.locator(`#${monthInputId}`);
  const container = monthInput.locator('xpath=..');

  await monthInput.fill(month);
  await container.locator('[data-segment="day"]').fill(day);
  await container.locator('[data-segment="year"]').fill(year);
  await expect(monthInput).toHaveValue(month);
  await expect(container.locator('[data-segment="day"]')).toHaveValue(day);
  await expect(container.locator('[data-segment="year"]')).toHaveValue(year);
}

// NebaDateTimeInput follows the same segment-per-input structure as NebaDateInput (see
// fillDateInput above), plus hour/minute/meridiem segments. The meridiem segment has no 'input'
// listener of its own (see NebaDateTimeInput.razor.js) — it's only reported to .NET when a later
// numeric segment's own 'input' event fires — so meridiem must be filled before the last numeric
// segment (minute), not after.
async function fillDateTimeInput(page: Page, monthInputId: string, isoDateTime: string): Promise<void> {
  const [datePart, timePart] = isoDateTime.split('T');
  const [year, month, day] = datePart.split('-');
  const [hour24Str, minute] = timePart.split(':');
  const hour24 = Number.parseInt(hour24Str, 10);
  const meridiem = hour24 >= 12 ? 'PM' : 'AM';
  const hour12 = (hour24 % 12 === 0 ? 12 : hour24 % 12).toString().padStart(2, '0');

  const monthInput = page.locator(`#${monthInputId}`);
  const container = monthInput.locator('xpath=..');

  await monthInput.fill(month);
  await container.locator('[data-segment="day"]').fill(day);
  await container.locator('[data-segment="year"]').fill(year);
  await container.locator('[data-segment="hour"]').fill(hour12);
  await container.locator('[data-segment="meridiem"]').fill(meridiem);
  await container.locator('[data-segment="minute"]').fill(minute);

  await expect(monthInput).toHaveValue(month);
  await expect(container.locator('[data-segment="year"]')).toHaveValue(year);
  await expect(container.locator('[data-segment="minute"]')).toHaveValue(minute);
  await expect(container.locator('[data-segment="meridiem"]')).toHaveValue(meridiem);
}

test.describe('Tournaments page — create tournament (unauthenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show the Create Tournament button', async ({ page }) => {
    await page.goto('/tournaments');
    await page.waitForSelector('h1');
    await expect(page.getByRole('link', { name: 'Create Tournament' })).toHaveCount(0);
  });

  test('shows a permission message when navigating directly to the create page', async ({ page }) => {
    await page.goto('/tournaments/new');
    await expect(page.locator('.news-empty-text')).toContainText("don't have permission to create tournaments");
  });
});

test.describe('Tournaments page — create tournament (authenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Tournaments.CreateTournament');
  });

  // A failed assertion earlier in a test can abort it before its own /__mock/reset call runs,
  // leaving the /tournaments override in place and breaking every later test's POST /tournaments —
  // the mock server keys overrides by path only, not method. Resetting here runs regardless of outcome.
  test.afterEach(async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/reset?path=/tournaments');
  });

  test('shows the Create Tournament button and navigates to the create page', async ({ page }) => {
    await page.goto('/tournaments');
    await page.waitForSelector('h1');

    await expect(page.getByRole('link', { name: 'Create Tournament' })).toBeVisible();
    await page.getByRole('link', { name: 'Create Tournament' }).click();

    await expect(page).toHaveURL(/\/tournaments\/new$/);
    await page.waitForSelector('#name');
  });

  test('shows validation errors when submitting an empty form', async ({ page }) => {
    await page.goto('/tournaments/new');
    await page.waitForSelector('#name');

    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-card')).toContainText('Name is required.');
    await expect(page.locator('.neba-card')).toContainText('Start date is required.');
    await expect(page.locator('.neba-card')).toContainText('End date is required.');
    await expect(page).toHaveURL(/\/tournaments\/new$/);
  });

  test('creates the tournament and navigates to its detail page', async ({ page }) => {
    await page.goto('/tournaments/new');
    await page.waitForSelector('#name');

    await page.locator('#name').fill('New Playwright Tournament');
    await fillDateInput(page, 'start-date', '2026-10-04');
    await fillDateInput(page, 'end-date', '2026-10-05');

    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-toast')).toContainText('Tournament Created');
    await expect(page).toHaveURL(/\/tournaments\/01JX000000000000000000\d+$/);
  });

  test('uploads a logo and includes it when creating the tournament', async ({ page }) => {
    await page.goto('/tournaments/new');
    await page.waitForSelector('#name');

    await page.locator('#name').fill('Logo Upload Tournament');
    await fillDateInput(page, 'start-date', '2026-11-01');
    await fillDateInput(page, 'end-date', '2026-11-02');

    await page.locator('input.neba-file-upload-input').setInputFiles({
      name: 'logo.png',
      mimeType: 'image/png',
      buffer: Buffer.from(
        'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=',
        'base64'
      ),
    });

    await expect(page.locator('.neba-file-upload-item-status--success')).toBeVisible();

    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-toast')).toContainText('Tournament Created');
    await expect(page).toHaveURL(/\/tournaments\/01JX000000000000000000\d+$/);
  });

  test('shows an error alert and stays on the page when creation fails', async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/fail?path=/tournaments&status=409');

    await page.goto('/tournaments/new');
    await page.waitForSelector('#name');

    await page.locator('#name').fill('Conflicting Tournament');
    await fillDateInput(page, 'start-date', '2026-12-01');
    await fillDateInput(page, 'end-date', '2026-12-02');

    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-alert-title')).toContainText('Unable to Create Tournament');
    await expect(page).toHaveURL(/\/tournaments\/new$/);
  });

  test('picks an existing oil pattern and shows its summary card', async ({ page }) => {
    await page.goto('/tournaments/new');
    await page.waitForSelector('#name');

    await page.getByRole('button', { name: 'Pick Existing' }).click();
    await page.locator('#pattern-select').selectOption({ label: 'Typhoon — 40 ft' });

    await expect(page.getByText('Typhoon', { exact: true })).toBeVisible();
    await expect(page.locator('.neba-badge', { hasText: 'Medium' }).first()).toBeVisible();
  });

  test('creates a new oil pattern inline via the picker', async ({ page }) => {
    await page.goto('/tournaments/new');
    await page.waitForSelector('#name');

    await page.getByRole('button', { name: 'Create New' }).click();

    await page.locator('#new-pattern-name').fill('Playwright Pattern');
    await page.locator('#new-pattern-length').fill('44');
    await page.locator('#new-pattern-volume').fill('25');
    await page.locator('#new-pattern-left').fill('6');
    await page.locator('#new-pattern-right').fill('4');

    await page.locator('button.neba-btn-primary.neba-btn-sm', { hasText: 'Add Pattern' }).click();

    // A successful "Add Pattern" switches the picker back to "Pick Existing" mode with the new
    // pattern selected, proving the create-then-select round trip happened.
    await expect(page.locator('#pattern-select')).toBeVisible();
    await expect(page.getByText('Playwright Pattern', { exact: true })).toBeVisible();
  });

  test('sets the oil pattern reveal date/time', async ({ page }) => {
    await page.goto('/tournaments/new');
    await page.waitForSelector('#name');

    await fillDateTimeInput(page, 'oil-pattern-reveal', '2026-12-01T14:30');

    const container = page.locator('#oil-pattern-reveal').locator('xpath=..');
    await expect(container.locator('[data-segment="month"]')).toHaveValue('12');
    await expect(container.locator('[data-segment="day"]')).toHaveValue('01');
    await expect(container.locator('[data-segment="hour"]')).toHaveValue('02');
    await expect(container.locator('[data-segment="meridiem"]')).toHaveValue('PM');
  });

  test('warns before discarding unsaved changes on Cancel', async ({ page }) => {
    await page.goto('/tournaments/new');
    await page.waitForSelector('#name');

    await page.locator('#name').fill('Unsaved Tournament');
    await page.locator('button.neba-btn-secondary', { hasText: 'Cancel' }).click();

    await expect(page.locator('.neba-modal-backdrop')).toContainText('Discard unsaved changes?');

    await page.locator('button.confirm-action-modal-confirm').click();
    await expect(page).toHaveURL(/\/tournaments$/);
  });
});
