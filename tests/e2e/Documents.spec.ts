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
