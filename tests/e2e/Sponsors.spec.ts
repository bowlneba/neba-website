import { test, expect } from '@playwright/test';

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
    await expect(page.locator('.sponsor-detail__badge--tier')).toContainText('Premium');
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

  test('renders the promotional notes', async ({ page }) => {
    const promo = page.locator('.sponsor-detail__promo');
    await expect(promo).toBeVisible();
    await expect(promo).toContainText('10% discount for NEBA members');
  });

  test('renders the live read script', async ({ page }) => {
    await expect(page.locator('.sponsor-detail__live-read')).toContainText(
      'Pro Shop Plus — where champions are made'
    );
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
