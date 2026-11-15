import { test, expect } from '@playwright/test';

test.describe.configure({ mode: 'serial' });

test.describe('Change Password page (authenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  // The session-token endpoint only lets a token pair rewrite the cookie of the user it was issued
  // for, so sign in as the user ID the mock /security/password/change route answers with.
  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=&userId=01JX0000000000000000000001');
  });

  // A failed assertion earlier in a test can abort it before its own reset runs, leaving the
  // /security/password/change override in place and breaking a later test.
  test.afterEach(async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/reset?path=/security/password/change');
  });

  async function fillForm(page: import('@playwright/test').Page, current: string, next: string) {
    await page.goto('/account/change-password');
    await page.waitForSelector('form');

    // The page is server-rendered until its Blazor circuit attaches, and text typed before then is
    // lost, so refill until the submit button enables.
    const submit = page.getByRole('button', { name: 'Change password' });
    await expect(async () => {
      await page.locator('#current-password').fill(current);
      await page.getByLabel('New Password', { exact: true }).fill(next);
      await page.getByLabel('Confirm New Password').fill(next);
      await expect(submit).toBeEnabled({ timeout: 1000 });
    }).toPass({ timeout: 20000 });

    return submit;
  }

  test('is reachable from the account menu', async ({ page }) => {
    await page.goto('/');
    await page.waitForSelector('.account-menu');

    await page.getByRole('button', { name: 'Account menu' }).hover();
    await page.getByRole('link', { name: 'Change Password' }).click();

    await expect(page).toHaveURL(/\/account\/change-password$/);
    await expect(page.getByRole('heading', { name: 'Change Password' })).toBeVisible();
  });

  test('changes the password and stays signed in', async ({ page }) => {
    const submit = await fillForm(page, 'OldPassw0rd!', 'NewPassw0rd!');
    await submit.click();

    await expect(page.locator('.neba-alert')).toContainText('Your password was changed.', { timeout: 15000 });
    await expect(page.locator('.account-menu')).toHaveCount(1);
    await expect(page.locator('#current-password')).toHaveValue('');
  });

  test('shows the API message when the current password is wrong', async ({ page }) => {
    await page.request.post(
      'http://localhost:5151/__mock/fail?path=/security/password/change&status=422&detail=' +
        encodeURIComponent('Current password is incorrect.')
    );

    const submit = await fillForm(page, 'WrongPassw0rd!', 'NewPassw0rd!');
    await submit.click();

    await expect(page.locator('.neba-alert')).toContainText('Current password is incorrect.', { timeout: 15000 });
    await expect(page).toHaveURL(/\/account\/change-password$/);
  });

  test('shows the lockout message', async ({ page }) => {
    await page.request.post(
      'http://localhost:5151/__mock/fail?path=/security/password/change&status=409&detail=' +
        encodeURIComponent('Too many incorrect attempts. Try again in a few minutes.')
    );

    const submit = await fillForm(page, 'OldPassw0rd!', 'NewPassw0rd!');
    await submit.click();

    await expect(page.locator('.neba-alert')).toContainText('Too many incorrect attempts.', { timeout: 15000 });
  });

  test('keeps submit disabled when the new password equals the current one', async ({ page }) => {
    await page.goto('/account/change-password');
    await page.waitForSelector('form');

    await page.locator('#current-password').fill('SamePassw0rd!');
    await page.getByLabel('New Password', { exact: true }).fill('SamePassw0rd!');
    await page.getByLabel('Confirm New Password').fill('SamePassw0rd!');

    await expect(page.getByText('New password must be different from the current password.')).toBeVisible({ timeout: 15000 });
    await expect(page.getByRole('button', { name: 'Change password' })).toBeDisabled();
  });
});

test.describe('Change Password page (anonymous)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show the form to a signed-out visitor', async ({ page }) => {
    await page.goto('/account/change-password');

    await expect(page.locator('#current-password')).toHaveCount(0);
  });
});
