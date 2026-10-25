import { test, expect, type Page } from '@playwright/test';
import path from 'node:path';

/**
 * Generates the screenshots embedded in docs/help/change-password.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * Signs in as the user ID the mock /security/password/change route answers with, so the
 * session cookie rewrite after a successful change is accepted (see ChangePassword.spec.ts).
 */

const outDir = path.join('docs', 'help', 'images', 'change-password');

/**
 * .account-dropdown's opacity/transform reveal is a 0.2s CSS transition; wait on the animation's
 * actual completion (Web Animations API) instead of a fixed sleep, so the screenshot isn't taken mid-fade.
 */
async function waitForDropdownAnimation(page: Page): Promise<void> {
  await page.locator('.account-dropdown').evaluate(async (el) => {
    await Promise.all(el.getAnimations().map((animation) => animation.finished));
  });
}

/**
 * Fills the three fields, retrying until the submit button enables. A page opened a moment ago is
 * still server-rendered until its Blazor circuit attaches, and text typed before then is lost.
 */
async function fillForm(page: Page, current: string, next: string): Promise<void> {
  await expect(async () => {
    await page.locator('#current-password').fill(current);
    await page.getByLabel('New Password', { exact: true }).fill(next);
    await page.getByLabel('Confirm New Password').fill(next);
    await expect(page.getByRole('button', { name: 'Change password' })).toBeEnabled({ timeout: 1000 });
  }).toPass({ timeout: 20000 });
}

test.describe.configure({ mode: 'serial' });

test.describe('change-password help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.afterEach(async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/reset?path=/security/password/change');
  });

  test('account menu, filled form, and success', async ({ page }) => {
    await page.request.post('/__test/login?permissions=&userId=01JX0000000000000000000001');

    await page.goto('/');
    await page.waitForSelector('.account-menu');

    await page.getByRole('button', { name: 'Account menu' }).hover();
    await expect(page.getByRole('link', { name: 'Change Password' })).toBeVisible();
    await waitForDropdownAnimation(page);

    await page.screenshot({ path: path.join(outDir, 'account-menu.png') });

    await page.getByRole('link', { name: 'Change Password' }).click();
    await page.waitForSelector('#current-password');

    await fillForm(page, 'OldPassw0rd!', 'NewPassw0rd!');

    await page.screenshot({ path: path.join(outDir, 'form-filled.png'), fullPage: true });

    await page.getByRole('button', { name: 'Change password' }).click();
    await expect(page.locator('.neba-alert')).toContainText('Your password was changed.', { timeout: 15000 });

    await page.screenshot({ path: path.join(outDir, 'success.png'), fullPage: true });
  });

  test('wrong current password', async ({ page }) => {
    await page.request.post('/__test/login?permissions=&userId=01JX0000000000000000000001');
    await page.request.post(
      'http://localhost:5151/__mock/fail?path=/security/password/change&status=422&detail=' +
        encodeURIComponent('Current password is incorrect.')
    );

    await page.goto('/account/change-password');
    await page.waitForSelector('#current-password');

    await fillForm(page, 'WrongPassw0rd!', 'NewPassw0rd!');
    await page.getByRole('button', { name: 'Change password' }).click();

    await expect(page.locator('.neba-alert')).toContainText('Current password is incorrect.', { timeout: 15000 });

    await page.screenshot({ path: path.join(outDir, 'wrong-password.png'), fullPage: true });
  });
});
