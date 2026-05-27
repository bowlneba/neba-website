import { test, expect } from '@playwright/test';
import { PRIMARY_BOWLER_ID } from './mock-api/mock-api-server';

const MOCK_ADMIN = 'http://localhost:5151/__mock';

// Hero stat values from mock data:
//   Total titles: 4 (2 for Current Leader + 2 for Current Rival)
//   Champions: 2 distinct bowlers
//   HOF: 1 (Current Leader only)
//   First year: 2023

test.describe('Champions page — Page Load & Hero', () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/history/champions');
    await page.waitForSelector('.hero-stats');
  });

  test('navigates to /history/champions without error', async ({ page }) => {
    await expect(page).not.toHaveURL(/error/);
    await expect(page.locator('.hero')).toBeVisible();
  });

  test('renders the page h1 heading', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Champions');
  });

  test('hero stats display total titles count', async ({ page }) => {
    await expect(page.locator('.hero-stat__num').filter({ hasText: '4' })).toBeVisible();
  });

  test('hero stats display total unique champions count', async ({ page }) => {
    await expect(page.locator('.hero-stat__num').filter({ hasText: '2' }).first()).toBeVisible();
  });

  test('hero stats display Hall of Fame count', async ({ page }) => {
    await expect(page.locator('.hero-stat__num').filter({ hasText: '1' })).toBeVisible();
  });

  test('hero stats display first year from data', async ({ page }) => {
    await expect(page.locator('.hero-stat__num').filter({ hasText: '2023' })).toBeVisible();
  });
});

test.describe('Champions page — By Titles View (default)', () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/history/champions');
    await page.waitForSelector('.hero-stats');
  });

  test('By Titles tab is active by default', async ({ page }) => {
    await expect(page.locator('.segmented__btn.is-active')).toContainText('By Titles');
  });

  test('tier sections render for the data present', async ({ page }) => {
    await expect(page.locator('.tier-section')).toHaveCount(3);
  });

  test('bowler card shows name', async ({ page }) => {
    await expect(page.locator('.bowler-card__name', { hasText: 'Current Leader' })).toBeVisible();
    await expect(page.locator('.bowler-card__name', { hasText: 'Current Rival' })).toBeVisible();
  });

  test('bowler card shows title count', async ({ page }) => {
    const cards = page.locator('.bowler-card');
    await expect(cards.first().locator('.bowler-card__count')).toContainText('2');
  });

  test('HOF badge is visible on Hall of Fame bowler card', async ({ page }) => {
    const leaderCard = page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) });
    await expect(leaderCard.locator('.hof-badge')).toBeVisible();
  });

  test('HOF badge is NOT visible on non-HOF bowler card', async ({ page }) => {
    const rivalCard = page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Rival' }) });
    await expect(rivalCard.locator('.hof-badge')).not.toBeVisible();
  });

  test('bowler card is a button element', async ({ page }) => {
    const card = page.locator('.bowler-card').first();
    await expect(card).toHaveRole('button');
  });
});

test.describe('Champions page — Tier Collapse / Expand', () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/history/champions');
    await page.waitForSelector('.hero-stats');
  });

  test('clicking a tier header collapses that tier', async ({ page }) => {
    const stdSection = page.locator('.tier-section[data-tier="std"]');
    await stdSection.locator('.tier-head').click();
    await expect(stdSection).toHaveClass(/is-collapsed/);
  });

  test('clicking a collapsed tier header expands it', async ({ page }) => {
    const stdSection = page.locator('.tier-section[data-tier="std"]');
    await stdSection.locator('.tier-head').click();
    await expect(stdSection).toHaveClass(/is-collapsed/);
    await stdSection.locator('.tier-head').click();
    await expect(stdSection).not.toHaveClass(/is-collapsed/);
  });

  test('collapsed tier hides its bowler grid', async ({ page }) => {
    const stdSection = page.locator('.tier-section[data-tier="std"]');
    await stdSection.locator('.tier-head').click();
    await expect(stdSection.locator('.bowler-card').first()).not.toBeVisible();
  });

  test('Enter key on tier header collapses the tier', async ({ page }) => {
    const stdSection = page.locator('.tier-section[data-tier="std"]');
    const head = stdSection.locator('.tier-head');
    await head.focus();
    await head.press('Enter');
    await expect(stdSection).toHaveClass(/is-collapsed/);
  });

  test('Space key on tier header collapses the tier', async ({ page }) => {
    const stdSection = page.locator('.tier-section[data-tier="std"]');
    const head = stdSection.locator('.tier-head');
    await head.focus();
    await head.press(' ');
    await expect(stdSection).toHaveClass(/is-collapsed/);
  });
});

test.describe('Champions page — By Year View', () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/history/champions');
    await page.waitForSelector('.hero-stats');
    await page.click('button:has-text("By Year")');
  });

  test('switching to By Year tab shows year sections', async ({ page }) => {
    await expect(page.locator('.year-section')).toHaveCount(2);
  });

  test('By Year tab becomes active after click', async ({ page }) => {
    await expect(page.locator('.segmented__btn.is-active')).toContainText('By Year');
  });

  test('year section headers show the year number', async ({ page }) => {
    await expect(page.locator('.year-head__title', { hasText: '2024' })).toBeVisible();
    await expect(page.locator('.year-head__title', { hasText: '2023' })).toBeVisible();
  });

  test('year section header shows event count', async ({ page }) => {
    const year2024 = page.locator('.year-section').filter({ has: page.locator('.year-head__title', { hasText: '2024' }) });
    await expect(year2024.locator('.year-head__meta')).toContainText('2');
  });

  test('year section header shows champion count for that year', async ({ page }) => {
    const year2024 = page.locator('.year-section').filter({ has: page.locator('.year-head__title', { hasText: '2024' }) });
    await expect(year2024.locator('.year-head__meta')).toContainText('2');
  });

  test('table columns are: Month, Tournament Type, Champions', async ({ page }) => {
    const header = page.locator('.year-table thead tr').first();
    await expect(header.locator('th').nth(0)).toContainText('Month');
    await expect(header.locator('th').nth(1)).toContainText('Tournament Type');
    await expect(header.locator('th').nth(2)).toContainText('Champions');
  });

  test('type pill renders with correct tournament type text', async ({ page }) => {
    await expect(page.locator('.type-pill', { hasText: 'Singles' }).first()).toBeVisible();
    await expect(page.locator('.type-pill', { hasText: 'Doubles' }).first()).toBeVisible();
  });

  test('Singles type pill has singles CSS class', async ({ page }) => {
    const pill = page.locator('.type-pill', { hasText: 'Singles' }).first();
    await expect(pill).toHaveClass(/singles/i);
  });

  test('Doubles type pill has doubles CSS class', async ({ page }) => {
    const pill = page.locator('.type-pill', { hasText: 'Doubles' }).first();
    await expect(pill).toHaveClass(/doubles/i);
  });

  test('champion name appears as a link in the champions column', async ({ page }) => {
    await expect(page.locator('.champ-link', { hasText: 'Current Leader' }).first()).toBeVisible();
  });

  test('separator dot between multiple champions in same event', async ({ page }) => {
    await expect(page.locator('.champ-sep')).toBeVisible();
  });

  test('most recent 3 years are expanded by default; older years are collapsed', async ({ page }) => {
    // Mock data has only 2024 and 2023 — both within top 3, both should be expanded
    const year2024 = page.locator('.year-section').filter({ has: page.locator('.year-head__title', { hasText: '2024' }) });
    const year2023 = page.locator('.year-section').filter({ has: page.locator('.year-head__title', { hasText: '2023' }) });
    await expect(year2024).not.toHaveClass(/is-collapsed/);
    await expect(year2023).not.toHaveClass(/is-collapsed/);
  });
});

test.describe('Champions page — Year Section Collapse / Expand', () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/history/champions');
    await page.waitForSelector('.hero-stats');
    await page.click('button:has-text("By Year")');
  });

  test('clicking a year header collapses that year', async ({ page }) => {
    const year2024 = page.locator('.year-section').filter({ has: page.locator('.year-head__title', { hasText: '2024' }) });
    await year2024.locator('.year-head').click();
    await expect(year2024).toHaveClass(/is-collapsed/);
  });

  test('clicking a collapsed year header expands it', async ({ page }) => {
    const year2024 = page.locator('.year-section').filter({ has: page.locator('.year-head__title', { hasText: '2024' }) });
    await year2024.locator('.year-head').click();
    await expect(year2024).toHaveClass(/is-collapsed/);
    await year2024.locator('.year-head').click();
    await expect(year2024).not.toHaveClass(/is-collapsed/);
  });

  test('collapsed year hides its table', async ({ page }) => {
    const year2024 = page.locator('.year-section').filter({ has: page.locator('.year-head__title', { hasText: '2024' }) });
    await year2024.locator('.year-head').click();
    await expect(year2024.locator('.year-table')).not.toBeVisible();
  });

  test('Enter key on year header toggles collapse', async ({ page }) => {
    const year2024 = page.locator('.year-section').filter({ has: page.locator('.year-head__title', { hasText: '2024' }) });
    const head = year2024.locator('.year-head');
    await head.focus();
    await head.press('Enter');
    await expect(year2024).toHaveClass(/is-collapsed/);
  });
});

test.describe('Champions page — Segment Tab Switching', () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/history/champions');
    await page.waitForSelector('.hero-stats');
  });

  test('switching to By Year does not reset By Titles collapse state', async ({ page }) => {
    const stdSection = page.locator('.tier-section[data-tier="std"]');
    await stdSection.locator('.tier-head').click();
    await expect(stdSection).toHaveClass(/is-collapsed/);
    await page.click('button:has-text("By Year")');
    await page.click('button:has-text("By Titles")');
    await expect(stdSection).toHaveClass(/is-collapsed/);
  });

  test('switching back to By Titles restores its previous state', async ({ page }) => {
    const stdSection = page.locator('.tier-section[data-tier="std"]');
    await stdSection.locator('.tier-head').click();
    await page.click('button:has-text("By Year")');
    await page.click('button:has-text("By Titles")');
    await expect(page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) })).not.toBeVisible();
  });
});

test.describe('Champions page — Expand All / Collapse All', () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/history/champions');
    await page.waitForSelector('.hero-stats');
  });

  test('Collapse All collapses all tiers in By Titles view', async ({ page }) => {
    await page.click('button:has-text("Collapse All")');
    const collapsed = page.locator('.tier-section.is-collapsed');
    await expect(collapsed).toHaveCount(3);
  });

  test('Expand All expands all tiers in By Titles view', async ({ page }) => {
    await page.click('button:has-text("Collapse All")');
    await page.click('button:has-text("Expand All")');
    const collapsed = page.locator('.tier-section.is-collapsed');
    await expect(collapsed).toHaveCount(0);
  });

  test('Collapse All does not affect By Year view state', async ({ page }) => {
    await page.click('button:has-text("Collapse All")');
    await page.click('button:has-text("By Year")');
    await expect(page.locator('.year-section.is-collapsed')).toHaveCount(0);
  });

  test('Collapse All collapses all years in By Year view', async ({ page }) => {
    await page.click('button:has-text("By Year")');
    await page.click('button:has-text("Collapse All")');
    await expect(page.locator('.year-section.is-collapsed')).toHaveCount(2);
  });

  test('Expand All expands all years in By Year view', async ({ page }) => {
    await page.click('button:has-text("By Year")');
    await page.click('button:has-text("Collapse All")');
    await page.click('button:has-text("Expand All")');
    await expect(page.locator('.year-section.is-collapsed')).toHaveCount(0);
  });
});

test.describe('Champions page — Bowler Titles Modal (Opens)', () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/history/champions');
    await page.waitForSelector('.hero-stats');
  });

  test('clicking a bowler card in By Titles view opens the modal', async ({ page }) => {
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.neba-modal-backdrop')).toBeVisible();
  });

  test('modal displays the bowler name in the header', async ({ page }) => {
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.modal-title')).toContainText('Current Leader');
  });

  test('modal displays the title count in the header', async ({ page }) => {
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.modal-summary-inline__count')).toContainText('2');
  });

  test('modal shows HOF badge in header for Hall of Fame bowler', async ({ page }) => {
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.modal-summary-inline .hof-badge')).toBeVisible();
  });

  test('modal does NOT show HOF badge for non-HOF bowler', async ({ page }) => {
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Rival' }) }).click();
    await expect(page.locator('.modal-summary-inline .hof-badge')).not.toBeVisible();
  });
});

test.describe('Champions page — Bowler Titles Modal (Loading & Data)', () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/history/champions');
    await page.waitForSelector('.hero-stats');
  });

  test.afterEach(async ({ page }) => {
    await page.request.post(`${MOCK_ADMIN}/reset`);
  });

  test('modal shows loading indicator while fetching titles', async ({ page }) => {
    await page.request.post(`${MOCK_ADMIN}/delay?path=/bowlers/${PRIMARY_BOWLER_ID}/titles&ms=500`);
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.neba-modal-backdrop .modal-state.is-active')).toBeVisible();
  });

  test('modal displays titles table after successful fetch', async ({ page }) => {
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.titles-table')).toBeVisible();
  });

  test('titles table has columns: #, Tournament, Date, Type', async ({ page }) => {
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.titles-table')).toBeVisible();
    const header = page.locator('.titles-table thead tr');
    await expect(header.locator('th').nth(0)).toContainText('#');
    await expect(header.locator('th').nth(1)).toContainText('Tournament');
    await expect(header.locator('th').nth(2)).toContainText('Date');
    await expect(header.locator('th').nth(3)).toContainText('Type');
  });

  test('titles are sorted most-recent-first', async ({ page }) => {
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.titles-table')).toBeVisible();
    const firstRow = page.locator('.titles-table tbody tr').first();
    await expect(firstRow.locator('.tt-date')).toContainText('Oct 2024');
  });

  test('row number counts up from oldest title (1 = first career title)', async ({ page }) => {
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.titles-table')).toBeVisible();
    // Sorted most-recent-first: row 1 = Oct 2024 (newest), row 2 = Apr 2024 (oldest)
    // But row number counts from oldest: newest gets rowNum = 2, oldest gets rowNum = 1
    const rows = page.locator('.titles-table tbody tr');
    await expect(rows.nth(0).locator('.tt-num')).toContainText('2');
    await expect(rows.nth(1).locator('.tt-num')).toContainText('1');
  });

  test('tournament name is a link to /tournaments/{id}', async ({ page }) => {
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.titles-table')).toBeVisible();
    const link = page.locator('.titles-table .tt-tourn a').first();
    await expect(link).toHaveAttribute('href', /\/tournaments\//);
  });

  test('type pill renders in the titles table', async ({ page }) => {
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.titles-table')).toBeVisible();
    await expect(page.locator('.titles-table .type-pill').first()).toBeVisible();
  });
});

test.describe('Champions page — Bowler Titles Modal (Error & Retry)', () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/history/champions');
    await page.waitForSelector('.hero-stats');
  });

  test.afterEach(async ({ page }) => {
    await page.request.post(`${MOCK_ADMIN}/reset`);
  });

  test('modal shows error state when API returns 500 for bowler titles', async ({ page }) => {
    await page.request.post(`${MOCK_ADMIN}/fail?path=/bowlers/${PRIMARY_BOWLER_ID}/titles&status=500`);
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.modal-state.is-active')).toBeVisible();
    await expect(page.locator('.modal-state__title')).toContainText("Couldn't load titles");
  });

  test('Retry button re-fires the API request', async ({ page }) => {
    await page.request.post(`${MOCK_ADMIN}/fail?path=/bowlers/${PRIMARY_BOWLER_ID}/titles&status=500`);
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.modal-state__retry')).toBeVisible();
    // Clear the failure so the retry call succeeds — proves a second request was made and handled
    await page.request.post(`${MOCK_ADMIN}/reset`);
    await page.locator('.modal-state__retry').click();
    await expect(page.locator('.titles-table')).toBeVisible();
  });

  test('modal shows data after successful retry', async ({ page }) => {
    await page.request.post(`${MOCK_ADMIN}/fail?path=/bowlers/${PRIMARY_BOWLER_ID}/titles&status=500`);
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.modal-state__retry')).toBeVisible();
    await page.request.post(`${MOCK_ADMIN}/reset`);
    await page.locator('.modal-state__retry').click();
    await expect(page.locator('.titles-table')).toBeVisible();
  });
});

test.describe('Champions page — Bowler Titles Modal (Opens from Year View)', () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/history/champions');
    await page.waitForSelector('.hero-stats');
    await page.click('button:has-text("By Year")');
  });

  test('clicking a champion link in By Year view opens the modal', async ({ page }) => {
    await page.locator('.champ-link', { hasText: 'Current Leader' }).first().click();
    await expect(page.locator('.neba-modal-backdrop')).toBeVisible();
  });

  test('modal shows correct bowler name from year view link', async ({ page }) => {
    await page.locator('.champ-link', { hasText: 'Current Leader' }).first().click();
    await expect(page.locator('.modal-title')).toContainText('Current Leader');
  });
});

test.describe('Champions page — Bowler Titles Modal (Closes)', () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/history/champions');
    await page.waitForSelector('.hero-stats');
  });

  test('clicking the modal close button closes the modal', async ({ page }) => {
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.neba-modal-backdrop')).toBeVisible();
    await page.locator('.neba-modal-close').click();
    await expect(page.locator('.neba-modal-backdrop')).not.toBeVisible();
  });

  test('clicking outside the modal (backdrop) closes it', async ({ page }) => {
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.neba-modal-backdrop')).toBeVisible();
    // Click the backdrop outside the modal content
    await page.locator('.neba-modal-backdrop').click({ position: { x: 10, y: 10 } });
    await expect(page.locator('.neba-modal-backdrop')).not.toBeVisible();
  });
});

test.describe('Champions page — Loading Skeleton', () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test.afterEach(async ({ page }) => {
    await page.request.post(`${MOCK_ADMIN}/reset`);
  });

  test('shows skeleton boxes in hero stats while API is loading', async ({ page }) => {
    await page.request.post(`${MOCK_ADMIN}/delay?path=/tournaments/champions&ms=600`);
    await page.goto('/history/champions');
    await page.waitForSelector('h1');
    await expect(page.locator('.hero .neba-skeleton').first()).toBeVisible();
  });

  test('shows champions skeleton in content area while API is loading', async ({ page }) => {
    await page.request.post(`${MOCK_ADMIN}/delay?path=/tournaments/champions&ms=600`);
    await page.goto('/history/champions');
    await page.waitForSelector('h1');
    await expect(page.locator('.champions-skeleton')).toBeVisible();
  });

  test('skeleton resolves to real content after data loads', async ({ page }) => {
    await page.goto('/history/champions');
    await page.waitForSelector('.hero-stats');
    await expect(page.locator('.champions-skeleton')).not.toBeVisible();
    await expect(page.locator('.tier-section').first()).toBeVisible();
  });
});

test.describe('Champions page — Error State on Page Load', () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test.afterEach(async ({ page }) => {
    await page.request.post(`${MOCK_ADMIN}/reset`);
  });

  test('shows error alert when champions API fails to load', async ({ page }) => {
    await page.request.post(`${MOCK_ADMIN}/fail?path=/tournaments/champions&status=500`);
    await page.goto('/history/champions');
    await page.waitForSelector('h1');
    await expect(page.locator('.neba-alert')).toBeVisible();
  });

  test('hero stats show -- placeholder when API fails', async ({ page }) => {
    await page.request.post(`${MOCK_ADMIN}/fail?path=/tournaments/champions&status=500`);
    await page.goto('/history/champions');
    await page.waitForSelector('h1');
    const statNums = page.locator('.hero-stat__num');
    await expect(statNums.first()).toContainText('--');
  });

  test('error alert can be dismissed', async ({ page }) => {
    await page.request.post(`${MOCK_ADMIN}/fail?path=/tournaments/champions&status=500`);
    await page.goto('/history/champions');
    await page.waitForSelector('.neba-alert');
    await page.locator('.neba-alert [data-dismiss], .neba-alert button').first().click();
    await expect(page.locator('.neba-alert')).not.toBeVisible();
  });
});

test.describe('Champions page — Accessibility', () => {
  test.use({ viewport: { width: 1280, height: 900 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/history/champions');
    await page.waitForSelector('.hero-stats');
  });

  test('tier headers have role="button" and tabindex="0"', async ({ page }) => {
    const heads = page.locator('.tier-head');
    const count = await heads.count();
    for (let i = 0; i < count; i++) {
      await expect(heads.nth(i)).toHaveAttribute('role', 'button');
      await expect(heads.nth(i)).toHaveAttribute('tabindex', '0');
    }
  });

  test('year headers have role="button" and tabindex="0"', async ({ page }) => {
    await page.click('button:has-text("By Year")');
    const heads = page.locator('.year-head');
    const count = await heads.count();
    for (let i = 0; i < count; i++) {
      await expect(heads.nth(i)).toHaveAttribute('role', 'button');
      await expect(heads.nth(i)).toHaveAttribute('tabindex', '0');
    }
  });

  test('modal close button has aria-label', async ({ page }) => {
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.neba-modal-close')).toHaveAttribute('aria-label', 'Close modal');
  });
});

test.describe('Champions page — Responsive (mobile)', () => {
  test.use({ viewport: { width: 375, height: 812 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/history/champions');
    await page.waitForSelector('.hero-stats');
  });

  test('modal-summary-card is visible on mobile', async ({ page }) => {
    await page.locator('.bowler-card', { has: page.locator('.bowler-card__name', { hasText: 'Current Leader' }) }).click();
    await expect(page.locator('.modal-summary-card')).toBeVisible();
  });
});
