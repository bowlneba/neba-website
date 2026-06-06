import { test, expect } from '@playwright/test';

test.describe('Contact page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/contact');
    await page.waitForSelector('h1');
  });

  test('renders the page header', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Connect with NEBA');
  });

  test('displays response time metadata', async ({ page }) => {
    await expect(page.locator('text=Within 2 business days')).toBeVisible();
    await expect(page.locator('text=Weekdays & evenings')).toBeVisible();
    await expect(page.locator('text=New England, USA')).toBeVisible();
  });

  test('displays the three contact cards', async ({ page }) => {
    await expect(page.locator('text=Anything else')).toBeVisible();
    await expect(page.locator('text=Tournaments & membership')).toBeVisible();
    await expect(page.locator('text=Something broken here?')).toBeVisible();
  });

  test('general inquiries card links to info@bowlneba.com', async ({ page }) => {
    await expect(page.locator('a[href="mailto:info@bowlneba.com"]').first()).toBeVisible();
  });

  test('tournaments card links to manager@bowlneba.com', async ({ page }) => {
    await expect(page.locator('a[href="mailto:manager@bowlneba.com"]').first()).toBeVisible();
  });

  test('website issues card links to website@bowlneba.com', async ({ page }) => {
    await expect(page.locator('a[href="mailto:website@bowlneba.com"]').first()).toBeVisible();
  });

  test('quick reference section lists all three email addresses', async ({ page }) => {
    const quickRef = page.locator('dl');
    await expect(quickRef.locator('a[href="mailto:info@bowlneba.com"]')).toBeVisible();
    await expect(quickRef.locator('a[href="mailto:manager@bowlneba.com"]')).toBeVisible();
    await expect(quickRef.locator('a[href="mailto:website@bowlneba.com"]')).toBeVisible();
  });

  test('displays the Follow NEBA social links section', async ({ page }) => {
    await expect(page.locator('h2', { hasText: 'Follow NEBA' })).toBeVisible();
    await expect(page.locator('a', { hasText: 'Facebook' })).toBeVisible();
    await expect(page.locator('a', { hasText: 'Instagram' })).toBeVisible();
    await expect(page.locator('a', { hasText: 'YouTube' })).toBeVisible();
    await expect(page.locator('a', { hasText: 'Discord' })).toBeVisible();
  });

  test('social links open in a new tab', async ({ page }) => {
    const facebook = page.locator('a', { hasText: 'Facebook' });
    await expect(facebook).toHaveAttribute('target', '_blank');
    await expect(facebook).toHaveAttribute('rel', 'noopener noreferrer');
  });

  test('displays before-you-email guidance section', async ({ page }) => {
    await expect(page.locator('text=Before you email about a result')).toBeVisible();
    await expect(page.locator('text=tournament name')).toBeVisible();
  });
});
