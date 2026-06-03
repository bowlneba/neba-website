import { test, expect } from '@playwright/test';

test.describe('News list page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/news');
    await page.waitForSelector('.news-hero');
  });

  test('renders the page header', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('News');
  });

  test('displays the featured hero with the latest article', async ({ page }) => {
    await expect(page.locator('.news-hero')).toBeVisible();
    await expect(page.locator('.hero-title')).toContainText('2025–26 Season Champions Crowned');
  });

  test('hero links to the article detail page', async ({ page }) => {
    const heroHref = await page.locator('.news-hero').getAttribute('href');
    expect(heroHref).toBe('/news/season-champions-2026');
  });

  test('displays the hero featured tag', async ({ page }) => {
    await expect(page.locator('.hero-featured-tag')).toBeVisible();
  });

  test('displays remaining articles in the 3-column grid', async ({ page }) => {
    await expect(page.locator('.news-grid')).toBeVisible();
    await expect(page.locator('.article-card')).toHaveCount(2);
  });

  test('article cards link to their detail pages', async ({ page }) => {
    const cards = page.locator('.article-card');
    await expect(cards.nth(0)).toHaveAttribute('href', '/news/june-lane-pattern');
    await expect(cards.nth(1)).toHaveAttribute('href', '/news/points-race-update');
  });

  test('does not show pagination when only one page exists', async ({ page }) => {
    await expect(page.locator('.news-pagination')).not.toBeVisible();
  });
});

test.describe('News list page — loading state', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('shows skeleton loader while articles are loading', async ({ page }) => {
    await page.route('**/news**', async route => {
      await new Promise(resolve => setTimeout(resolve, 2000));
      await route.continue();
    });

    await page.goto('/news');
    await expect(page.locator('[aria-busy="true"]')).toBeVisible();
  });
});

test.describe('News list page — error state', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('shows error alert when API returns an error', async ({ page }) => {
    await page.goto('/__mock/override?path=/news&status=500');
    await page.goto('/news');
    await page.waitForSelector('.neba-alert');
    await expect(page.locator('.neba-alert')).toContainText('Error Loading Articles');
    await page.goto('/__mock/reset');
  });
});
