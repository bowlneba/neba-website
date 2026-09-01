import { test, expect } from '@playwright/test';

test.describe.configure({ mode: 'serial' });

test.describe('Create User page (unauthenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show the account menu', async ({ page }) => {
    await page.goto('/');
    await page.waitForSelector('h1');
    await expect(page.locator('.account-menu')).toHaveCount(0);
  });

  test('shows a permission message when navigating directly to the create page', async ({ page }) => {
    await page.goto('/account/create-user');
    await expect(page.locator('.news-empty-text')).toContainText("don't have permission to create users");
  });
});

test.describe('Create User page (authenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=System.CreateUser');
  });

  // A failed assertion earlier in a test can abort it before its own /__mock/reset call runs,
  // leaving the /security/users override in place and breaking a later test's POST — the mock
  // server keys overrides by path only, not method. Resetting here runs regardless of outcome.
  test.afterEach(async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/reset?path=/security/users');
  });

  test('shows the Create User menu item and navigates to the create page', async ({ page }) => {
    await page.goto('/');
    await page.waitForSelector('.account-menu');

    await page.getByRole('button', { name: 'Account menu' }).hover();
    await expect(page.getByRole('link', { name: 'Create User' })).toBeVisible();
    await page.getByRole('link', { name: 'Create User' }).click();

    await expect(page).toHaveURL(/\/account\/create-user$/);
    await page.waitForSelector('#email');
  });

  test('shows a validation error when submitting with no email', async ({ page }) => {
    await page.goto('/account/create-user');
    await page.waitForSelector('#email');

    await page.locator('button[type="submit"]').click();

    await expect(page.locator('.neba-card')).toContainText('Email is required.');
    await expect(page).toHaveURL(/\/account\/create-user$/);
  });

  test('shows a validation error when submitting with no roles selected', async ({ page }) => {
    await page.goto('/account/create-user');
    await page.waitForSelector('#email');

    await page.locator('#email').fill('new.staff@bowlneba.com');
    await page.locator('button[type="submit"]').click();

    await expect(page.locator('.neba-card')).toContainText('Select at least one role.');
    await expect(page).toHaveURL(/\/account\/create-user$/);
  });

  test('creates the user and shows a success message', async ({ page }) => {
    await page.goto('/account/create-user');
    await page.waitForSelector('#email');

    await page.locator('#email').fill('new.staff@bowlneba.com');
    await page.getByRole('checkbox', { name: 'Webmaster' }).check();

    await page.locator('button[type="submit"]').click();

    await expect(page.locator('.neba-alert-title')).toContainText('Invite Sent');
    await expect(page.locator('.neba-alert')).toContainText('new.staff@bowlneba.com');
    await expect(page).toHaveURL(/\/account\/create-user$/);
  });

  test('shows an error alert and stays on the page when creation fails', async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/fail?path=/security/users&status=409');

    await page.goto('/account/create-user');
    await page.waitForSelector('#email');

    await page.locator('#email').fill('conflict@bowlneba.com');
    await page.getByRole('checkbox', { name: 'Manager' }).check();

    await page.locator('button[type="submit"]').click();

    await expect(page.locator('.neba-alert-title')).toContainText('Unable to Create User');
    await expect(page).toHaveURL(/\/account\/create-user$/);
  });
});
