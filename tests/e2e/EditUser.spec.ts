import { test, expect, type Locator } from '@playwright/test';

test.describe.configure({ mode: 'serial' });

// The radios are visually hidden; users click the labels, so the specs do too.
async function choose(row: Locator, state: 'added' | 'default' | 'removed'): Promise<void> {
  await row.locator('label[for$="-' + state + '"]').click();
}

const INVITED_USER_ID = '01JX0000000000000000000500';
const ADMIN_USER_ID = '01JX0000000000000000000498';
const CALLER_PERMISSIONS =
  'System.GetUsers,System.EditUser,Tournaments.DeleteTournament,Tournaments.ManageTournamentStatus';

test.describe('Edit User page (unauthenticated)', () => {
  test.use({ viewport: { width: 1200, height: 900 } });

  test('shows a permission message', async ({ page }) => {
    await page.goto('/account/users/' + INVITED_USER_ID + '/edit');
    await expect(page.locator('.news-empty-text')).toContainText("don't have permission to edit users");
  });
});

test.describe('Edit User page (authenticated)', () => {
  test.use({ viewport: { width: 1200, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=' + CALLER_PERMISSIONS);
  });

  // Overrides are keyed by path only (not method), so reset regardless of how a test ended.
  test.afterEach(async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/reset?path=/security/users/' + INVITED_USER_ID);
  });

  test('opens from the Users list, changes a switch and saves', async ({ page }) => {
    await page.goto('/account/users');
    await page.getByRole('link', { name: 'Edit invited.staff@bowlneba.com' }).click();
    await expect(page).toHaveURL(new RegExp('/account/users/' + INVITED_USER_ID + '/edit$'));

    const row = page.getByRole('row', { name: /Delete Tournament/ });
    await choose(row, 'added');
    await expect(row).toContainText('Allowed');

    await page.getByRole('button', { name: 'Save changes' }).click();

    await expect(page).toHaveURL(/\/account\/users$/);
    await expect(page.locator('.neba-toast-title')).toContainText('User Updated');

    const sent = await (
      await page.request.get(
        'http://localhost:5151/__mock/last-request?method=PUT&path=/security/users/' + INVITED_USER_ID,
      )
    ).json();
    expect(sent.user.addedPermissions).toEqual(['Tournaments.DeleteTournament']);
    expect(sent.user.removedPermissions).toEqual([]);
  });

  test('disables Added for a permission the caller does not hold', async ({ page }) => {
    await page.goto('/account/users/' + INVITED_USER_ID + '/edit');

    const row = page.getByRole('row', { name: /Clear Cache/ });
    await expect(row.getByRole('radio', { name: 'Added' })).toBeDisabled();
    await expect(row.getByRole('radio', { name: 'Removed' })).toBeEnabled();
  });

  test('shows the Admin note and no switches for an Admin user', async ({ page }) => {
    await page.goto('/account/users/' + ADMIN_USER_ID + '/edit');

    await expect(page.locator('.neba-alert')).toContainText('Admins have every permission');
    await expect(page.getByRole('radio')).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Save changes' })).toHaveCount(0);
  });

  test('shows an error alert when saving fails', async ({ page }) => {
    await page.goto('/account/users/' + INVITED_USER_ID + '/edit');
    await page.waitForSelector('input[type="radio"]');

    // Set after the page loaded: the mock keys overrides by path, so a failure set earlier would also fail the GET.
    await page.request.post('http://localhost:5151/__mock/fail?path=/security/users/' + INVITED_USER_ID + '&status=403');

    await choose(page.getByRole('row', { name: /Delete Tournament/ }), 'added');
    await page.getByRole('button', { name: 'Save changes' }).click();

    await expect(page.locator('.neba-alert')).toContainText('Unable to Save User');
    await expect(page).toHaveURL(new RegExp('/account/users/' + INVITED_USER_ID + '/edit$'));
  });
});

test.describe('Edit User page (own account)', () => {
  test.use({ viewport: { width: 1200, height: 900 } });

  test('shows the switches read-only and no Save', async ({ page }) => {
    await page.request.post(
      '/__test/login?permissions=' + CALLER_PERMISSIONS + '&userId=' + INVITED_USER_ID,
    );

    await page.goto('/account/users/' + INVITED_USER_ID + '/edit');

    await expect(page.locator('.neba-alert')).toContainText("can't change your own permissions");
    await expect(page.getByRole('radio', { name: 'Removed' }).first()).toBeDisabled();
    await expect(page.getByRole('button', { name: 'Save changes' })).toHaveCount(0);
  });
});
