import { test, expect } from '@playwright/test';
import path from 'node:path';

/**
 * Generates the screenshots embedded in docs/help/create-series.md.
 *
 * Run via `npm run docs:screenshots` (playwright.docs.config.ts), never as part of the
 * normal E2E suite — see ADR-0007 (docs/adr/0007-in-repo-user-help-documentation.md).
 *
 * The script fills the create form but never submits it, so it is safe to re-run.
 */

const outDir = path.join('docs', 'help', 'images', 'create-series');

test.describe.configure({ mode: 'serial' });

test.describe('create-series help screenshots', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Series.CreateSeries');
  });

  test('series list with the create button, then the filled create form', async ({ page }) => {
    await page.goto('/series');
    await page.waitForSelector('.series-current');
    await expect(page.getByRole('link', { name: 'Create Series' })).toBeVisible();

    await page.screenshot({ path: path.join(outDir, 'series-list-create-button.png') });

    await page.getByRole('link', { name: 'Create Series' }).click();
    await page.waitForSelector('#series-name');

    await page.locator('#series-name').fill('2027 Vincent Hall Memorial Series');
    for (let index = 0; index < 3; index++) {
      await page.getByRole('button', { name: '+ Add Stage' }).click();
    }

    await page.locator('#stage-0-kind').selectOption('Qualifying');
    await page.locator('#stage-0-tournament').selectOption({ label: 'Sat 4/11 · Series Pick One' });
    await page.locator('#stage-1-kind').selectOption('Survivor');
    await page.locator('#stage-1-tournament').selectOption({ label: 'Sat 4/18 · Series Pick Two' });
    await page.locator('#stage-2-kind').selectOption('Placement');
    await page.locator('#stage-2-tournament').selectOption({ label: 'Sat 4/25 · Series Pick Three' });
    await page.getByLabel('This Series has a Finale').check();
    await page.locator('#finale-count').fill('4');

    await page.evaluate(() => window.scrollTo(0, 0));

    // fullPage so the Stage grid and the Finale section are captured, not just the first viewport.
    await page.screenshot({ path: path.join(outDir, 'create-form.png'), fullPage: true });
  });
});
