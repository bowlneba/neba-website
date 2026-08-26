import { test, expect } from '@playwright/test';

// "error state" below mutates a shared mock-server response override for
// /news. Running it in parallel races against other tests in this file that
// load /news without expecting an override, so the whole file runs serially.
test.describe.configure({ mode: 'serial' });

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
    const cards = page.locator('.article-card .article-card-link');
    await expect(cards.nth(0)).toHaveAttribute('href', '/news/june-lane-pattern');
    await expect(cards.nth(1)).toHaveAttribute('href', '/news/points-race-update');
  });

  test('does not show pagination when only one page exists', async ({ page }) => {
    await expect(page.locator('.pagination-nav')).not.toBeVisible();
  });
});

test.describe('News list page — loading state', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('shows skeleton loader while articles are loading', async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/delay?path=/news&ms=3000');

    await page.goto('/news');
    await expect(page.locator('[aria-busy="true"]')).toBeVisible();
    await page.request.post('http://localhost:5151/__mock/reset?path=/news');
  });
});

test.describe('News list page — error state', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('shows error alert when API returns an error', async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/fail?path=/news&status=500');
    await page.goto('/news');
    await page.waitForSelector('.neba-alert');
    await expect(page.locator('.neba-alert')).toContainText('Error Loading Articles');
    await page.request.post('http://localhost:5151/__mock/reset?path=/news');
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
    await page.request.post('http://localhost:5151/__mock/delay?path=/news/season-champions-2026&ms=3000');

    await page.goto('/news/season-champions-2026');
    await expect(page.locator('[aria-busy="true"]')).toBeVisible();
    await page.request.post('http://localhost:5151/__mock/reset?path=/news/season-champions-2026');
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
    await page.request.post('http://localhost:5151/__mock/reset?path=/news/season-champions-2026');
  });
});

test.describe('News list page — delete article (unauthenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show a delete icon on article cards', async ({ page }) => {
    await page.goto('/news');
    await page.waitForSelector('.news-hero');
    await expect(page.locator('.article-card button.card-delete-btn')).toHaveCount(0);
  });
});

test.describe('News detail page — delete article (unauthenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show the danger zone or delete button', async ({ page }) => {
    await page.goto('/news/season-champions-2026');
    await page.waitForSelector('.news-detail-layout');
    await expect(page.locator('.sidebar-danger-zone')).toHaveCount(0);
  });
});

test.describe('News list page — delete article (authenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=News.DeleteArticle');
  });

  test('shows a delete icon on article cards', async ({ page }) => {
    await page.goto('/news');
    await page.waitForSelector('.news-hero');
    await expect(page.locator('.article-card button.card-delete-btn')).toHaveCount(2);
  });

  test('removes the article card after confirming delete', async ({ page }) => {
    await page.goto('/news');
    await page.waitForSelector('.news-hero');

    const cardTitles = page.locator('.article-card .card-title');
    const deletedTitle = await cardTitles.first().textContent();

    await page.locator('.article-card').first().locator('button.card-delete-btn').click();
    await expect(page.locator('.neba-modal-content')).toContainText('Delete article?');
    await expect(page.locator('.neba-modal-content')).toContainText(deletedTitle ?? '');

    await page.locator('button.confirm-action-modal-confirm').click();

    await expect(page.locator('.article-card')).toHaveCount(1);
    await expect(page.locator('.article-card .card-title')).not.toContainText(deletedTitle ?? '');
  });

  test('cancelling the confirm dialog leaves the article in place', async ({ page }) => {
    await page.goto('/news');
    await page.waitForSelector('.news-hero');

    await page.locator('.article-card').first().locator('button.card-delete-btn').click();
    await expect(page.locator('.neba-modal-content')).toBeVisible();

    await page.locator('button.confirm-action-modal-cancel').click();

    await expect(page.locator('.neba-modal-content')).toHaveCount(0);
    await expect(page.locator('.article-card')).toHaveCount(2);
  });

  test('shows a delete icon on the hero and removes it after confirming delete', async ({ page }) => {
    await page.goto('/news');
    await page.waitForSelector('.news-hero');

    await expect(page.locator('.hero-delete-btn')).toHaveCount(1);

    const heroTitle = await page.locator('.hero-title').textContent();

    await page.locator('.hero-delete-btn').click();
    await expect(page.locator('.neba-modal-content')).toContainText('Delete article?');
    await expect(page.locator('.neba-modal-content')).toContainText(heroTitle ?? '');

    await page.locator('button.confirm-action-modal-confirm').click();

    await expect(page.locator('.hero-title')).not.toContainText(heroTitle ?? '');
  });
});

test.describe('News detail page — delete article (authenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=News.DeleteArticle');
  });

  test('shows the danger zone delete button', async ({ page }) => {
    await page.goto('/news/june-lane-pattern');
    await page.waitForSelector('.news-detail-layout');
    await expect(page.locator('.sidebar-danger-zone-btn')).toBeVisible();
  });

  test('navigates back to the news list after confirming delete', async ({ page }) => {
    await page.goto('/news/june-lane-pattern');
    await page.waitForSelector('.news-detail-layout');

    await page.locator('.sidebar-danger-zone-btn').click();
    await expect(page.locator('.neba-modal-content')).toContainText('Delete article?');

    await page.locator('button.confirm-action-modal-confirm').click();

    await expect(page).toHaveURL(/\/news$/);
  });

  test('shows an error alert and stays on the page when delete fails', async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/fail?path=/news/01JX0000000000000000000103&status=403');

    await page.goto('/news/points-race-update');
    await page.waitForSelector('.news-detail-layout');

    await page.locator('.sidebar-danger-zone-btn').click();
    await page.locator('button.confirm-action-modal-confirm').click();

    await expect(page.locator('.neba-toast')).toContainText('Delete Failed');
    await expect(page).toHaveURL(/\/news\/points-race-update$/);

    await page.request.post('http://localhost:5151/__mock/reset?path=/news/01JX0000000000000000000103');
  });
});

test.describe('News detail page — edit article (authenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=News.EditArticle');
  });

  test('saves changes and navigates to the article detail page', async ({ page }) => {
    await page.goto('/news/june-lane-pattern/edit');
    await page.waitForSelector('#title');

    await page.locator('#title').fill('Updated Lane Pattern Title');
    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-toast')).toContainText('Article Updated');
    await expect(page).toHaveURL(/\/news\/june-lane-pattern$/);
  });

  test('shows an error alert and stays on the page when saving fails', async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/fail?path=/news/01JX0000000000000000000102&status=409');

    await page.goto('/news/june-lane-pattern/edit');
    await page.waitForSelector('#title');

    await page.locator('#title').fill('Updated Lane Pattern Title');
    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-alert-title')).toContainText('Unable to Save Article');
    await expect(page).toHaveURL(/\/news\/june-lane-pattern\/edit$/);

    await page.request.post('http://localhost:5151/__mock/reset?path=/news/01JX0000000000000000000102');
  });
});

test.describe('News list page — create article (unauthenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show the Create Article button', async ({ page }) => {
    await page.goto('/news');
    await page.waitForSelector('.news-hero');
    await expect(page.getByRole('link', { name: 'Create Article' })).toHaveCount(0);
  });

  test('shows a permission message when navigating directly to the create page', async ({ page }) => {
    await page.goto('/news/new');
    await expect(page.locator('.news-empty-text')).toContainText("don't have permission to create articles");
  });
});

test.describe('News list page — create article (authenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=News.CreateArticle');
  });

  test('shows the Create Article button and navigates to the create page', async ({ page }) => {
    await page.goto('/news');
    await page.waitForSelector('.news-hero');

    await expect(page.getByRole('link', { name: 'Create Article' })).toBeVisible();
    await page.getByRole('link', { name: 'Create Article' }).click();

    await expect(page).toHaveURL(/\/news\/new$/);
    await page.waitForSelector('#title');
  });

  test('shows validation errors when submitting an empty form', async ({ page }) => {
    await page.goto('/news/new');
    await page.waitForSelector('#title');

    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-card')).toContainText('Title is required.');
    await expect(page.locator('.neba-card')).toContainText('Content is required.');
    await expect(page).toHaveURL(/\/news\/new$/);
  });

  test('creates the article and navigates to its detail page', async ({ page }) => {
    await page.goto('/news/new');
    await page.waitForSelector('#title');

    await page.locator('#title').fill('New Playwright Article');

    await page.locator('.ql-editor').click();
    await page.keyboard.type('This article was created by an end-to-end test.');

    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-toast')).toContainText('Article Created');
    await expect(page).toHaveURL(/\/news\/new-playwright-article$/);
  });

  test('shows an error alert and stays on the page when creation fails', async ({ page }) => {
    await page.request.post('http://localhost:5151/__mock/fail?path=/news&status=409');

    await page.goto('/news/new');
    await page.waitForSelector('#title');

    await page.locator('#title').fill('Conflicting Article');

    await page.locator('.ql-editor').click();
    await page.keyboard.type('Some content that should fail to save.');

    await page.locator('button[type="submit"].neba-btn-primary').click();

    await expect(page.locator('.neba-alert-title')).toContainText('Unable to Create Article');
    await expect(page).toHaveURL(/\/news\/new$/);

    await page.request.post('http://localhost:5151/__mock/reset?path=/news');
  });
});
