import { test, expect, type Page } from '@playwright/test';
import path from 'node:path';

/**
 * Generates the screenshots embedded in docs/help/list-users.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * This page has no mutating action of its own (viewing/filtering only), so the whole
 * script is safe to re-run against the same mock data.
 */

const outDir = path.join('docs', 'help', 'images', 'list-users');

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

test.describe('list-users help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=System.GetUsers,System.ResetUserPassword');
  });

  test('account menu + users table + filtered table', async ({ page }) => {
    await page.goto('/');
    await page.waitForSelector('.account-menu');

    await page.getByRole('button', { name: 'Account menu' }).hover();
    await expect(page.getByRole('menuitem', { name: 'Users' })).toBeVisible();
    await waitForDropdownAnimation(page);

    await page.screenshot({ path: path.join(outDir, 'account-menu.png') });

    await page.getByRole('menuitem', { name: 'Users' }).click();
    await page.waitForSelector('.neba-table');

    await page.screenshot({ path: path.join(outDir, 'users-table.png'), fullPage: true });

    await page.getByPlaceholder('Filter by email or role…').fill('webmaster');

    // Filtering round-trips over SignalR (@bind:after on a Blazor Server page), so the table
    // doesn't update synchronously with the fill() call above — wait for the filtered row count
    // before capturing, or the screenshot can catch the pre-filter table mid-round-trip.
    await expect(page.locator('.neba-table tbody tr')).toHaveCount(1);

    await page.screenshot({ path: path.join(outDir, 'users-filtered.png'), fullPage: true });
  });
});
