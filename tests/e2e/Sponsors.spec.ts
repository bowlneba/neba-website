import { test, expect } from '@playwright/test';

test.describe.configure({ mode: 'serial' });

test.describe('Sponsors list page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/sponsors');
    await page.waitForSelector('#title-sponsor-name');
  });

  test('renders the page header', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Our Partners');
  });

  test('displays the title sponsor', async ({ page }) => {
    await expect(page.locator('#title-sponsor-name')).toContainText('Acme Bowling Supply');
  });

  test('displays the title sponsor tagline', async ({ page }) => {
    await expect(page.locator('text=Setting the standard since 1962')).toBeVisible();
  });

  test('displays the premier partners section', async ({ page }) => {
    await expect(page.locator('#premier-partners-heading')).toBeVisible();
    await expect(page.locator('.sponsor-card-premier')).toHaveCount(1);
  });

  test('displays the association sponsors section', async ({ page }) => {
    await expect(page.locator('#association-sponsors-heading')).toBeVisible();
    await expect(page.locator('.sponsor-tile')).toHaveCount(1);
  });

  test('clicking a premier sponsor navigates to the detail page', async ({ page }) => {
    await page.locator('.sponsor-card-premier').click();
    await page.waitForSelector('.sponsor-detail');
    await expect(page).toHaveURL(/\/sponsors\/pro-shop-plus/);
  });
});

test.describe('Sponsor detail page', () => {
  test.use({ viewport: { width: 1200, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/sponsors/pro-shop-plus');
    await page.waitForSelector('.sponsor-detail');
  });

  test('renders the sponsor name', async ({ page }) => {
    await expect(page.locator('.sponsor-detail__title')).toContainText('Pro Shop Plus');
  });

  test('renders the tier and category badges', async ({ page }) => {
    await expect(page.locator('.sponsor-detail__badge--tier')).toContainText('Premier');
    await expect(page.locator('.sponsor-detail__badge--category')).toContainText('Pro Shop');
  });

  test('renders the sponsor tagline', async ({ page }) => {
    await expect(page.locator('.sponsor-detail__tagline')).toContainText('Everything for the serious bowler');
  });

  test('renders the about section with description', async ({ page }) => {
    const about = page.locator('.sponsor-detail__about');
    await expect(about).toBeVisible();
    await expect(about).toContainText('Your local pro shop for all bowling needs');
  });

  test('renders the visit website button with correct href', async ({ page }) => {
    const btn = page.locator('.sponsor-detail__website-btn');
    await expect(btn).toBeVisible();
    await expect(btn).toHaveAttribute('href', 'https://example.com/proshopplus');
  });

  test('renders the business address', async ({ page }) => {
    const address = page.locator('.sponsor-detail__address');
    await expect(address).toContainText('123 Main Street');
    await expect(address).toContainText('Boston, MA 02101');
  });

  test('renders the contact email', async ({ page }) => {
    await expect(page.locator('.sponsor-detail__email-link')).toContainText(
      'info@proshopplus.example.com'
    );
  });

  test('renders phone numbers in formatted form', async ({ page }) => {
    await expect(page.locator('.sponsor-detail__phone-number')).toContainText('(617) 555-0123');
  });

  test('renders the Instagram social link', async ({ page }) => {
    const link = page.locator('a[aria-label="Instagram"]');
    await expect(link).toBeVisible();
    await expect(link).toHaveAttribute('href', 'https://instagram.com/proshopplus');
  });

  test('back link returns to the sponsor directory', async ({ page }) => {
    await page.locator('.sponsor-detail__back-link').click();
    await expect(page).toHaveURL('/sponsors');
  });
});

test.describe('Sponsor not found', () => {
  test('redirects to /not-found for an inactive sponsor', async ({ page }) => {
    await page.goto('/sponsors/old-sponsor');
    await page.waitForURL('**/not-found');
    await expect(page).toHaveURL(/\/not-found/);
  });
});

test.describe('Sponsors list page — create sponsor (unauthenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show the Create Sponsor button', async ({ page }) => {
    await page.goto('/sponsors');
    await page.waitForSelector('#title-sponsor-name');
    await expect(page.getByRole('link', { name: 'Create Sponsor' })).toHaveCount(0);
  });

  test('shows a permission message when navigating directly to the create page', async ({ page }) => {
    await page.goto('/sponsors/new');
    await expect(page.locator('.news-empty-text')).toContainText("don't have permission to create sponsors");
  });
});

test.describe('Sponsors list page — create sponsor (authenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Sponsors.CreateSponsor');
  });

  test('shows the Create Sponsor button and navigates to the create page', async ({ page }) => {
    await page.goto('/sponsors');
    await page.waitForSelector('#title-sponsor-name');

    await expect(page.getByRole('link', { name: 'Create Sponsor' })).toBeVisible();
    await page.getByRole('link', { name: 'Create Sponsor' }).click();

    await expect(page).toHaveURL(/\/sponsors\/new$/);
    await page.waitForSelector('#name');
  });

  test('shows validation errors when submitting an empty form', async ({ page }) => {
    await page.goto('/sponsors/new');
    await page.waitForSelector('#name');

    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-card')).toContainText('Name is required.');
    await expect(page).toHaveURL(/\/sponsors\/new$/);
  });

  test('creates the sponsor and navigates to its detail page', async ({ page }) => {
    await page.goto('/sponsors/new');
    await page.waitForSelector('#name');

    await page.locator('#name').fill('New Playwright Sponsor');

    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-toast')).toContainText('Sponsor Created');
    await expect(page).toHaveURL(/\/sponsors\/new-playwright-sponsor$/);
  });

  test('uploads a logo and includes it when creating the sponsor', async ({ page }) => {
    await page.goto('/sponsors/new');
    await page.waitForSelector('#name');

    await page.locator('#name').fill('Logo Upload Sponsor');

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

    await expect(page.locator('.neba-toast')).toContainText('Sponsor Created');
    await expect(page).toHaveURL(/\/sponsors\/logo-upload-sponsor$/);
  });

  test('shows an error alert and stays on the page when creation fails', async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/fail?path=/sponsors&status=409');

    await page.goto('/sponsors/new');
    await page.waitForSelector('#name');

    await page.locator('#name').fill('Conflicting Sponsor');

    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-alert-title')).toContainText('Unable to Create Sponsor');
    await expect(page).toHaveURL(/\/sponsors\/new$/);

    await page.request.post('http://localhost:5151/__mock/reset?path=/sponsors');
  });
});

test.describe('Sponsor detail page — edit sponsor (unauthenticated)', () => {
  test.use({ viewport: { width: 1200, height: 900 } });

  test('does not show the Edit Sponsor button', async ({ page }) => {
    await page.goto('/sponsors/pro-shop-plus');
    await page.waitForSelector('.sponsor-detail');
    await expect(page.getByRole('link', { name: 'Edit Sponsor' })).toHaveCount(0);
  });

  test('shows a permission message when navigating directly to the edit page', async ({ page }) => {
    await page.goto('/sponsors/pro-shop-plus/edit');
    await expect(page.locator('.news-empty-text')).toContainText("don't have permission to edit sponsors");
  });
});

test.describe('Sponsor detail page — edit sponsor (authenticated)', () => {
  test.use({ viewport: { width: 1200, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Sponsors.EditSponsor');
  });

  test('shows the Edit Sponsor button and navigates to the edit page', async ({ page }) => {
    await page.goto('/sponsors/pro-shop-plus');
    await page.waitForSelector('.sponsor-detail');

    await expect(page.getByRole('link', { name: 'Edit Sponsor' })).toBeVisible();
    await page.getByRole('link', { name: 'Edit Sponsor' }).click();

    await expect(page).toHaveURL(/\/sponsors\/pro-shop-plus\/edit$/);
    await page.waitForSelector('#name');
  });

  test('pre-fills the form with the sponsor\'s existing data', async ({ page }) => {
    await page.goto('/sponsors/pro-shop-plus/edit');
    await page.waitForSelector('#name');

    await expect(page.locator('#name')).toHaveValue('Pro Shop Plus');
    await expect(page.locator('#tier')).toHaveValue('Premier');
    await expect(page.locator('#category')).toHaveValue('Pro Shop');
    await expect(page.locator('#website-url')).toHaveValue('https://example.com/proshopplus');
    await expect(page.locator('#instagram-url')).toHaveValue('https://instagram.com/proshopplus');
    await expect(page.locator('#business-street')).toHaveValue('123 Main Street');
    await expect(page.locator('#business-city')).toHaveValue('Boston');
    await expect(page.locator('#business-email')).toHaveValue('info@proshopplus.example.com');
    await expect(page.locator('.create-sponsor-phone-number')).toHaveValue('6175550123');
  });

  test('shows validation errors when clearing the required name field', async ({ page }) => {
    await page.goto('/sponsors/pro-shop-plus/edit');
    await page.waitForSelector('#name');

    await page.locator('#name').fill('');
    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-card')).toContainText('Name is required.');
    await expect(page).toHaveURL(/\/sponsors\/pro-shop-plus\/edit$/);
  });

  test('saves changes and navigates to the sponsor detail page', async ({ page }) => {
    await page.goto('/sponsors/pro-shop-plus/edit');
    await page.waitForSelector('#name');

    await page.locator('#name').fill('Pro Shop Plus Updated');
    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-toast')).toContainText('Sponsor Updated');
    await expect(page).toHaveURL(/\/sponsors\/pro-shop-plus$/);
  });

  test('shows an error alert and stays on the page when saving fails', async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/fail?path=/sponsors/01JX0000000000000000000001&status=409');

    await page.goto('/sponsors/pro-shop-plus/edit');
    await page.waitForSelector('#name');

    await page.locator('#name').fill('Pro Shop Plus Updated');
    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-alert-title')).toContainText('Unable to Save Sponsor');
    await expect(page).toHaveURL(/\/sponsors\/pro-shop-plus\/edit$/);

    await page.request.post('http://localhost:5151/__mock/reset?path=/sponsors/01JX0000000000000000000001');
  });

  test('cancel prompts to discard unsaved changes and returns to the detail page on confirm', async ({ page }) => {
    await page.goto('/sponsors/pro-shop-plus/edit');
    await page.waitForSelector('#name');

    await page.locator('#name').fill('Unsaved Change');
    await page.locator('button.neba-btn-secondary:has-text("Cancel")').click();

    await expect(page.locator('.neba-modal-content')).toContainText('Discard unsaved changes?');
    await page.locator('button.confirm-action-modal-confirm').click();

    await expect(page).toHaveURL(/\/sponsors\/pro-shop-plus$/);
  });
});
