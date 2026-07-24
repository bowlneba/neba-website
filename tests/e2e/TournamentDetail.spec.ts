import { test, expect } from '@playwright/test';

const MOCK_TOURNAMENT_ID = '01JX0000000000000000000010';
const MOCK_TOURNAMENT_OIL_REVEAL_PENDING_ID = '01JX0000000000000000000030';
const MOCK_TOURNAMENT_OIL_REVEALED_ID = '01JX0000000000000000000031';
const MOCK_TOURNAMENT_OIL_REVEAL_MGMT_ID = '01JX0000000000000000000032';
const MOCK_TOURNAMENT_SPONSOR_MGMT_ID = '01JX0000000000000000000040';

test.describe('Tournament Detail page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}`);
    await page.waitForSelector('.td-hero');
  });

  test('renders tournament name', async ({ page }) => {
    await expect(page.locator('.td-hero__title')).toContainText('NEBA Spring Classic');
  });

  test('past tournament uses past hero styling', async ({ page }) => {
    await expect(page.locator('.td-hero')).toHaveClass(/td-hero--past/);
  });

  test('displays champion pill with winner name', async ({ page }) => {
    await expect(page.locator('.td-hero__champion-pill')).toContainText('Current Leader');
  });

  test('back link navigates to tournaments list', async ({ page }) => {
    await page.locator('.tournament-detail__back-link').click();
    await expect(page).toHaveURL('/tournaments');
  });

  test('displays the title sponsor name in the "Presented by" hero text', async ({ page }) => {
    await expect(page.locator('.td-hero__sponsor')).toContainText('Pro Shop Plus');
  });

  test('renders sponsor cards for every tournament sponsor', async ({ page }) => {
    const cards = page.locator('.td-rail-sponsor-card');
    await expect(cards).toHaveCount(2);
    await expect(cards.nth(0)).toContainText('Pro Shop Plus');
    await expect(cards.nth(1)).toContainText('Regional Lanes');
  });

  test('clicking a sponsor card navigates to the sponsor detail page', async ({ page }) => {
    await page.locator('.td-rail-sponsor-card', { hasText: 'Pro Shop Plus' }).click();
    await expect(page).toHaveURL('/sponsors/pro-shop-plus');
  });
});

test.describe('Tournament Detail — not found', () => {
  test('unknown tournament id redirects to not-found', async ({ page }) => {
    await page.goto('/tournaments/does-not-exist');
    await expect(page).toHaveURL('/not-found');
  });
});

test.describe('Tournament Detail — manage sponsors permission boundary', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show the Manage Sponsors section for a viewer without the permission', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}`);
    await page.waitForSelector('.td-hero');

    await expect(page.locator('.mts-panel')).toHaveCount(0);
    await expect(page.getByText('Manage Sponsors')).toHaveCount(0);
  });
});

test.describe('Tournament Detail — manage sponsors (authorized)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });
  test.describe.configure({ mode: 'serial' });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Tournaments.ManageSponsors');
  });

  // Resets the add-sponsor failure override (if any test below set it) so it never leaks into
  // a later test — same rationale as CreateTournament.spec.ts's afterEach.
  test.afterEach(async ({ page }) => {
    await page.request.post(
      `http://localhost:5151/__mock/reset?path=/tournaments/${MOCK_TOURNAMENT_SPONSOR_MGMT_ID}/sponsors`
    );
  });

  test('shows the Manage Sponsors section with an empty state for an authorized viewer', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_SPONSOR_MGMT_ID}`);
    await page.waitForSelector('.td-hero');

    await expect(page.locator('.mts-panel')).toBeVisible();
    await expect(page.locator('.mts-panel__empty')).toContainText('No sponsors attached to this tournament yet.');
  });

  test('adds a sponsor via the Add Sponsor modal', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_SPONSOR_MGMT_ID}`);
    await page.waitForSelector('.td-hero');

    await page.locator('.mts-panel__header button', { hasText: '+ Add Sponsor' }).click();
    await expect(page.locator('.neba-modal-backdrop')).toBeVisible();

    await page.locator('#sponsor-pick').selectOption({ label: 'Acme Bowling Supply' });
    await page.locator('#sponsor-amount').fill('750');
    await page.locator('#title-sponsor').check();

    await page.locator('.mts-modal-actions button.neba-btn-primary', { hasText: 'Add Sponsor' }).click();

    await expect(page.locator('.neba-toast')).toContainText('Sponsor Added');

    const row = page.locator('.mts-row', { hasText: 'Acme Bowling Supply' });
    await expect(row).toBeVisible();
    await expect(row).toContainText('Title Sponsor');
    await expect(row).toContainText('$750');
  });

  test('removes a sponsor after confirming', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_SPONSOR_MGMT_ID}`);
    await page.waitForSelector('.td-hero');

    const row = page.locator('.mts-row', { hasText: 'Acme Bowling Supply' });
    await expect(row).toBeVisible();
    await row.locator('.mts-row__remove').click();

    await expect(page.locator('.neba-modal-backdrop')).toContainText('Remove sponsor?');
    await page.locator('button.confirm-action-modal-confirm').click();

    await expect(page.locator('.neba-toast')).toContainText('Sponsor Removed');
    await expect(page.locator('.mts-panel__empty')).toContainText('No sponsors attached to this tournament yet.');
  });

  test('shows an error alert and keeps the modal state when adding a sponsor fails', async ({ page }) => {
    await page.request.post(
      `http://localhost:5151/__mock/fail?path=/tournaments/${MOCK_TOURNAMENT_SPONSOR_MGMT_ID}/sponsors&status=409`
    );

    await page.goto(`/tournaments/${MOCK_TOURNAMENT_SPONSOR_MGMT_ID}`);
    await page.waitForSelector('.td-hero');

    await page.locator('.mts-panel__header button', { hasText: '+ Add Sponsor' }).click();
    await page.locator('#sponsor-pick').selectOption({ label: 'Acme Bowling Supply' });
    await page.locator('#sponsor-amount').fill('500');
    await page.locator('.mts-modal-actions button.neba-btn-primary', { hasText: 'Add Sponsor' }).click();

    await expect(page.locator('.neba-alert-title')).toContainText("Couldn't Add Sponsor");
  });
});

test.describe('Tournament Detail — oil pattern reveal gating', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('hides full pattern details before reveal for an ordinary viewer', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_OIL_REVEAL_PENDING_ID}`);
    await page.waitForSelector('.td-hero');

    const section = page.locator('.tournament-detail__section').filter({ hasText: 'Oil Pattern' });
    await expect(section).toBeVisible();
    await expect(section.locator('.td-pattern-card')).toHaveCount(0);
    await expect(section.locator('.td-hero__chip', { hasText: 'Medium' })).toBeVisible();
  });

  test('shows full pattern details once the reveal date/time has passed', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_OIL_REVEALED_ID}`);
    await page.waitForSelector('.td-hero');

    await expect(page.locator('.td-pattern-card__name')).toContainText('Scorpion');
    await expect(page.locator('.td-reveal-note')).toContainText('Revealed to the public');
  });

  test('shows full pattern details before reveal for a viewer with tournament-management permission', async ({ page }) => {
    await page.request.post('/__test/login?permissions=Tournaments.ManageSponsors');

    await page.goto(`/tournaments/${MOCK_TOURNAMENT_OIL_REVEAL_MGMT_ID}`);
    await page.waitForSelector('.td-hero');

    await expect(page.locator('.td-pattern-card__name')).toContainText('Scorpion');
    await expect(page.locator('.td-reveal-note')).toContainText('Full details reveal to the public on');
  });
});
