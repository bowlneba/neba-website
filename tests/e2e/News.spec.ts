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
    await page.route('http://localhost:5151/news', async route => {
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
    await page.request.post('http://localhost:5151/__mock/fail?path=/news&status=500');
    await page.goto('/news');
    await page.waitForSelector('.neba-alert');
    await expect(page.locator('.neba-alert')).toContainText('Error Loading Articles');
    await page.request.post('http://localhost:5151/__mock/reset');
  });
});

test.describe('News detail page', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/news/season-champions-2026');
    await page.waitForSelector('.news-detail-layout');
  });

  test('renders the article title', async ({ page }) => {
    await expect(page.locator('h1.news-detail-title')).toContainText('2025–26 Season Champions Crowned');
  });

  test('renders the publish date', async ({ page }) => {
    await expect(page.locator('.news-detail-date')).toContainText('May 15, 2026');
  });

  test('renders the article body content', async ({ page }) => {
    await expect(page.locator('.news-detail-body')).toContainText('Tournament of Champions');
  });

  test('renders the Article Info panel in the sidebar', async ({ page }) => {
    await expect(page.locator('.news-detail-sidebar')).toContainText('Article Info');
    await expect(page.locator('.news-detail-sidebar')).toContainText('May 15, 2026');
  });

  test('renders Go to Tournament link in sidebar when tournamentId is set', async ({ page }) => {
    await expect(page.locator('.sidebar-tournament-btn')).toBeVisible();
    await expect(page.locator('.sidebar-tournament-btn')).toContainText('Go to Tournament');
    const href = await page.locator('.sidebar-tournament-btn').getAttribute('href');
    expect(href).toContain('/tournaments/');
  });

  test('renders the Attached Files panel when attachments exist', async ({ page }) => {
    await expect(page.locator('.news-detail-sidebar')).toContainText('Attached Files');
    await expect(page.locator('.sidebar-file-row')).toHaveCount(2);
  });

  test('attachment download links point to file URLs', async ({ page }) => {
    const firstLink = page.locator('.sidebar-file-row').first();
    const href = await firstLink.getAttribute('href');
    expect(href).toBeTruthy();
    expect(href).toContain('bowlneba.com');
  });

  test('renders two Back to News links', async ({ page }) => {
    await expect(page.locator('.news-back-link')).toHaveCount(2);
    const hrefs = await page.locator('.news-back-link').evaluateAll(
      els => els.map(el => el.getAttribute('href'))
    );
    expect(hrefs.every(h => h === '/news')).toBe(true);
  });
});

test.describe('News detail page — no tournament link', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show tournament link when tournamentId is null', async ({ page }) => {
    await page.goto('/news/june-lane-pattern');
    await page.waitForSelector('.news-detail-layout');
    await expect(page.locator('.sidebar-tournament-btn')).not.toBeVisible();
  });
});

test.describe('News detail page — no attachments', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show Attached Files panel when article has no attachments', async ({ page }) => {
    await page.goto('/news/points-race-update');
    await page.waitForSelector('.news-detail-layout');
    await expect(page.locator('.news-detail-sidebar')).not.toContainText('Attached Files');
  });
});

test.describe('News detail page — loading state', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('shows skeleton loader while article is loading', async ({ page }) => {
    await page.route('http://localhost:5151/news/**', async route => {
      await new Promise(resolve => setTimeout(resolve, 2000));
      await route.continue();
    });

    await page.goto('/news/season-champions-2026');
    await expect(page.locator('[aria-busy="true"]')).toBeVisible();
  });
});

test.describe('News detail page — not found', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('shows not-found message for unknown slug', async ({ page }) => {
    await page.goto('/news/does-not-exist');
    await page.waitForSelector('.news-detail-not-found');
    await expect(page.locator('.news-detail-not-found')).toContainText('could not be found');
  });

  test('shows Back to News link in not-found state', async ({ page }) => {
    await page.goto('/news/does-not-exist');
    await page.waitForSelector('.news-detail-not-found');
    await expect(page.locator('.news-detail-not-found a')).toContainText('Back to News');
  });
});

test.describe('News detail page — error state', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('shows error alert when API returns a server error', async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/fail?path=/news/season-champions-2026&status=500');
    await page.goto('/news/season-champions-2026');
    await page.waitForSelector('.neba-alert');
    await expect(page.locator('.neba-alert')).toContainText('Error Loading Article');
    await page.request.post('http://localhost:5151/__mock/reset');
  });
});
