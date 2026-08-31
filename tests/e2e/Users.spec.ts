import { test, expect } from '@playwright/test';

test.describe.configure({ mode: 'serial' });

test.describe('Users page (unauthenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('shows a permission message when navigating directly to the page', async ({ page }) => {
    await page.goto('/account/users');
    await expect(page.locator('.news-empty-text')).toContainText("don't have permission to view users");
  });
});

test.describe('Users page (GetUsers only, no ResetUserPassword)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=System.GetUsers');
  });

  test('shows the user list but hides the Reset Password button', async ({ page }) => {
    await page.goto('/account/users');
    await page.waitForSelector('.neba-table');

    await expect(page.locator('.neba-table')).toContainText('webmaster@bowlneba.com');
    await expect(page.getByRole('button', { name: 'Reset Password' })).toHaveCount(0);
  });
});

test.describe('Users page (ResetUserPassword only, no GetUsers)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=System.ResetUserPassword');
  });

  test('shows a permission message and hides the Users menu item', async ({ page }) => {
    await page.goto('/');
    await page.waitForSelector('.account-menu');
    await page.getByRole('button', { name: 'Account menu' }).hover();
    await expect(page.getByRole('menuitem', { name: 'Users' })).toHaveCount(0);

    await page.goto('/account/users');
    await expect(page.locator('.news-empty-text')).toContainText("don't have permission to view users");
  });
});

test.describe('Users page (authenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=System.GetUsers,System.ResetUserPassword');
  });

  // A failed assertion earlier in a test can abort it before its own /__mock/reset call runs,
  // leaving an override in place and breaking a later test — resetting here runs regardless of outcome.
  test.afterEach(async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/reset?path=/security/password/reset');
    await page.request.post('http://localhost:5151/__mock/reset?path=/security/users');
  });

  test('shows the Users menu item and navigates to the users page', async ({ page }) => {
    await page.goto('/');
    await page.waitForSelector('.account-menu');

    await page.getByRole('button', { name: 'Account menu' }).hover();
    await expect(page.getByRole('menuitem', { name: 'Users' })).toBeVisible();
    await page.getByRole('menuitem', { name: 'Users' }).click();

    await expect(page).toHaveURL(/\/account\/users$/);
    await expect(page.locator('.neba-table')).toContainText('webmaster@bowlneba.com');
  });

  test('shows an error alert and stays on the page when loading the user list fails', async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/fail?path=/security/users&status=500');

    await page.goto('/account/users');
    await page.waitForSelector('.neba-alert');

    await expect(page.locator('.neba-alert')).toContainText('Error Loading Users');
    await expect(page).toHaveURL(/\/account\/users$/);
    await expect(page.locator('.neba-table tbody')).not.toContainText('webmaster@bowlneba.com');
  });

  test('lists users with their roles and status', async ({ page }) => {
    await page.goto('/account/users');
    await page.waitForSelector('.neba-table');

    const rows = page.locator('.neba-table tbody tr');
    await expect(rows).toHaveCount(2);
    await expect(rows.nth(0)).toContainText('webmaster@bowlneba.com');
    await expect(rows.nth(0)).toContainText('Webmaster');
    await expect(rows.nth(0)).toContainText('Active');
    await expect(rows.nth(1)).toContainText('invited.staff@bowlneba.com');
    await expect(rows.nth(1)).toContainText('Invite Pending');
  });

  test('filters the user list by email', async ({ page }) => {
    await page.goto('/account/users');
    await page.waitForSelector('.neba-table');

    await page.getByPlaceholder('Filter by email or role…').fill('invited');

    const rows = page.locator('.neba-table tbody tr');
    await expect(rows).toHaveCount(1);
    await expect(rows.nth(0)).toContainText('invited.staff@bowlneba.com');
  });

  test('filters the user list by role', async ({ page }) => {
    await page.goto('/account/users');
    await page.waitForSelector('.neba-table');

    await page.getByPlaceholder('Filter by email or role…').fill('journalist');

    const rows = page.locator('.neba-table tbody tr');
    await expect(rows).toHaveCount(1);
    await expect(rows.nth(0)).toContainText('invited.staff@bowlneba.com');
  });

  test('shows a no-matches message when the filter matches nothing', async ({ page }) => {
    await page.goto('/account/users');
    await page.waitForSelector('.neba-table');

    await page.getByPlaceholder('Filter by email or role…').fill('nobody-matches-this');

    await expect(page.locator('.neba-table tbody tr')).toHaveCount(1);
    await expect(page.locator('.neba-table tbody')).toContainText('No users match "nobody-matches-this"');
  });

  test('resets a user password after confirming', async ({ page }) => {
    await page.goto('/account/users');
    await page.waitForSelector('.neba-table');

    await page.locator('.neba-table tbody tr').nth(0).getByRole('button', { name: 'Reset Password' }).click();
    await expect(page.locator('text=Send "webmaster@bowlneba.com" a link to set a new password?')).toBeVisible();

    await page.locator('.confirm-action-modal-confirm').click();

    await expect(page.locator('.neba-toast')).toContainText('Password Reset Sent');
  });

  test('cancelling the confirmation does not reset the password', async ({ page }) => {
    await page.goto('/account/users');
    await page.waitForSelector('.neba-table');

    await page.locator('.neba-table tbody tr').nth(0).getByRole('button', { name: 'Reset Password' }).click();
    await page.locator('.confirm-action-modal-cancel').click();

    await expect(page.locator('text=Send "webmaster@bowlneba.com"')).toHaveCount(0);
  });

  test('shows an error toast when the reset request fails', async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/fail?path=/security/password/reset&status=404');

    await page.goto('/account/users');
    await page.waitForSelector('.neba-table');

    await page.locator('.neba-table tbody tr').nth(0).getByRole('button', { name: 'Reset Password' }).click();
    await page.locator('.confirm-action-modal-confirm').click();

    await expect(page.locator('.neba-toast')).toContainText('Reset Password Failed');
  });
});
