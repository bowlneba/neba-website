import { test, expect } from '@playwright/test';

test.describe('Tournament Rules page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/tournaments/rules');
    await page.waitForSelector('.neba-document-container');
  });

  test('renders the document content', async ({ page }) => {
    const content = page.locator('.neba-document-content');
    await expect(content.locator('h1')).toContainText('NEBA Tournament Rules');
    await expect(content.locator('h2').first()).toContainText('Section 1: Eligibility');
  });

  test('renders the table of contents', async ({ page }) => {
    await expect(page.locator('.neba-document-toc')).toBeVisible();
  });
});

test.describe('Bylaws page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/bylaws');
    await page.waitForSelector('.neba-document-container');
  });

  test('renders the document content', async ({ page }) => {
    const content = page.locator('.neba-document-content');
    await expect(content.locator('h1')).toContainText('NEBA Bylaws');
    await expect(content.locator('h2').first()).toContainText('Article I: Name');
  });

  test('renders the table of contents', async ({ page }) => {
    await expect(page.locator('.neba-document-toc')).toBeVisible();
  });
});

test.describe('Document slideover panel', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/tournaments/rules');
    await page.waitForSelector('.neba-document-container');
  });

  test('opens when an internal document link is clicked', async ({ page }) => {
    await page.locator('.neba-document-content a[href="/bylaws"]').click();

    const slideover = page.locator('.neba-document-slideover');
    await expect(slideover).toHaveClass(/active/);
  });

  test('displays the linked document content', async ({ page }) => {
    await page.locator('.neba-document-content a[href="/bylaws"]').click();

    const slideover = page.locator('.neba-document-slideover');
    await expect(slideover).toHaveClass(/active/);
    await expect(slideover.locator('.neba-document-slideover-content h1')).toContainText('NEBA Bylaws');
  });

  test('shows the document title in the slideover header', async ({ page }) => {
    await page.locator('.neba-document-content a[href="/bylaws"]').click();

    await expect(page.locator('.neba-document-slideover-title')).toContainText('Bylaws');
  });

  test('closes when the close button is clicked', async ({ page }) => {
    await page.locator('.neba-document-content a[href="/bylaws"]').click();

    const slideover = page.locator('.neba-document-slideover');
    await expect(slideover).toHaveClass(/active/);

    await page.locator('.neba-document-slideover-close').click();
    await expect(slideover).not.toHaveClass(/active/);
  });

  test('closes when Escape is pressed', async ({ page }) => {
    await page.locator('.neba-document-content a[href="/bylaws"]').click();

    const slideover = page.locator('.neba-document-slideover');
    await expect(slideover).toHaveClass(/active/);

    await page.keyboard.press('Escape');
    await expect(slideover).not.toHaveClass(/active/);
  });
});

const REFRESH_BUTTON = '.toc-sticky button[title="Refresh from Google Drive"]';

test.describe('Refresh document (unauthenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show the refresh button', async ({ page }) => {
    await page.goto('/bylaws');
    await page.waitForSelector('.neba-document-container');

    await expect(page.locator(REFRESH_BUTTON)).toHaveCount(0);
  });
});

test.describe('Refresh document (authenticated, no permission)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show the refresh button', async ({ page }) => {
    await page.request.post('/__test/login?permissions=');

    await page.goto('/bylaws');
    await page.waitForSelector('.neba-document-container');

    await expect(page.locator(REFRESH_BUTTON)).toHaveCount(0);
  });
});

test.describe('Refresh document (authenticated, with permission)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Documents.RefreshDocument');
  });

  // The mock server's failure overrides are global and the browser projects run in parallel. Only
  // the failure test touches the tournament-rules refresh path, and every project's run of it wants
  // the same failure, so it sets the override without resetting; resetting from one project would
  // clear it under another project's in-flight run. The success test uses bylaws so it never sees
  // the override.

  test('shows the refresh button and reloads the page when clicked', async ({ page }) => {
    await page.goto('/bylaws');
    await page.waitForSelector('.neba-document-container');

    // A forced reload wipes this marker; a client-side re-render would not.
    await page.evaluate(() => {
      (window as unknown as { __beforeRefresh?: boolean }).__beforeRefresh = true;
    });

    await expect(page.locator(REFRESH_BUTTON)).toBeVisible();
    await page.locator(REFRESH_BUTTON).click();

    await page.waitForFunction(
      () => (window as unknown as { __beforeRefresh?: boolean }).__beforeRefresh === undefined,
    );
    await page.waitForSelector('.neba-document-container');
    await expect(page.locator('.neba-document-content h1')).toContainText('NEBA Bylaws');
  });

  test('shows an error toast and re-enables the button when the server returns a failure', async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/fail?path=/documents/tournament-rules/cache&status=403');

    await page.goto('/tournaments/rules');
    await page.waitForSelector('.neba-document-container');

    await page.locator(REFRESH_BUTTON).click();

    await expect(page.locator('.neba-toast-title')).toContainText('Refresh Failed');
    await expect(page.locator(REFRESH_BUTTON)).toBeEnabled();
    await expect(page.locator('.neba-document-content h1')).toContainText('NEBA Tournament Rules');
  });
});
