import { test, expect, type Page } from '@playwright/test';
import path from 'node:path';

/**
 * Generates the screenshots embedded in docs/help/create-user.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * The create form has no natural "undo" step (unlike delete's cancel-confirm dialog), so this
 * script stops at the blank, pre-submit form rather than actually inviting a mock user. The
 * Set Password page is anonymous and doesn't validate its query-string token client-side, so it
 * can be captured directly with placeholder values without a real invite link.
 */

const outDir = path.join('docs', 'help', 'images', 'create-user');

/**
 * .account-dropdown's opacity/transform reveal is a 0.2s CSS transition; waitForSelector/toBeVisible
 * resolve as soon as visibility flips, mid-transition. Wait on the animation's actual completion
 * (Web Animations API) instead of a fixed sleep, so the screenshot isn't taken mid-fade.
 */
async function waitForDropdownAnimation(page: Page): Promise<void> {
  await page.locator('.account-dropdown').evaluate(async (el) => {
    await Promise.all(el.getAnimations().map((animation) => animation.finished));
  });
}

test.describe.configure({ mode: 'serial' });

test.describe('create-user help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('account menu + blank create form', async ({ page }) => {
    await page.request.post('/__test/login?permissions=System.CreateUser');

    await page.goto('/');
    await page.waitForSelector('.account-menu');

    await page.getByRole('button', { name: 'Account menu' }).hover();
    await expect(page.getByRole('link', { name: 'Create User' })).toBeVisible();
    await waitForDropdownAnimation(page);

    await page.screenshot({ path: path.join(outDir, 'account-menu.png') });

    await page.getByRole('link', { name: 'Create User' }).click();
    await page.waitForSelector('#email');

    await page.screenshot({ path: path.join(outDir, 'create-form.png'), fullPage: true });
  });

  test('set password form', async ({ page }) => {
    await page.goto('/account/set-password?userId=01JXXXXXXXXXXXXXXXXXXXXXXXXX&token=placeholder-token');
    await page.waitForSelector('form');

    await expect(page.locator('form')).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'set-password-form.png') });
  });
});
