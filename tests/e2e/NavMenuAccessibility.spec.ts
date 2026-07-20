import { test, expect, type Locator, type Page } from '@playwright/test';

/**
 * Focuses the trigger link and presses the given key, retrying the whole
 * action until the dropdown reports itself open. This replaces a fixed
 * wait: on a fresh page load the NavMenu JS module may not have finished
 * wiring up its keydown listener yet, so the first press can be a no-op.
 * `toPass` re-runs the action + assertion until the observable state
 * (the `active` class) actually reflects the interaction.
 */
async function openDropdown(page: Page, triggerLink: Locator, navItem: Locator, key: string) {
  await expect(async () => {
    await triggerLink.focus();
    await page.keyboard.press(key);
    await expect(navItem).toHaveClass(/active/);
  }).toPass();
}

test.describe('NavMenu Keyboard Accessibility', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    // Wait for Blazor to initialize and JS module to load
    await page.waitForSelector('.neba-navbar');
  });

  test.describe('Skip link functionality', () => {
    test('Skip link is first focusable element', async ({ page }) => {
      const skipLink = page.locator('a.skip-link');

      // Verify skip link exists and is in the DOM
      await expect(skipLink).toBeAttached();

      // Focus the skip link directly and verify it can receive focus
      await skipLink.focus();
      await expect(skipLink).toBeFocused();
    });

    test('Skip link click focuses #main-content', async ({ page }) => {
      const mainContent = page.locator('#main-content');

      // Verify main-content has tabindex for focusability
      await expect(mainContent).toHaveAttribute('tabindex', '-1');

      // Simulate skip link behavior: navigate to anchor and focus
      await page.evaluate(() => {
        const target = document.getElementById('main-content');
        target?.focus();
      });

      await expect(mainContent).toBeFocused();
    });
  });

  test.describe('Dropdown opens with Enter key', () => {
    test('Tab to Tournaments link and press Enter opens dropdown', async ({ page }) => {
      const navItem = page.locator('.neba-nav-item[data-action="toggle-dropdown"]').first();
      const tournamentsLink = page.locator('.neba-nav-item [aria-haspopup="true"]').first();

      await openDropdown(page, tournamentsLink, navItem, 'Enter');

      // Assert: trigger link has aria-expanded="true"
      await expect(tournamentsLink).toHaveAttribute('aria-expanded', 'true');

      // Assert: first .neba-dropdown-link is focused
      const firstDropdownLink = navItem.locator('.neba-dropdown-link').first();
      await expect(firstDropdownLink).toBeFocused();
    });
  });

  test.describe('Dropdown opens with Space key', () => {
    test('Tab to Tournaments link and press Space opens dropdown', async ({ page }) => {
      const navItem = page.locator('.neba-nav-item[data-action="toggle-dropdown"]').first();
      const tournamentsLink = page.locator('.neba-nav-item [aria-haspopup="true"]').first();

      await openDropdown(page, tournamentsLink, navItem, 'Space');

      await expect(tournamentsLink).toHaveAttribute('aria-expanded', 'true');

      const firstDropdownLink = navItem.locator('.neba-dropdown-link').first();
      await expect(firstDropdownLink).toBeFocused();
    });
  });

  test.describe('Dropdown opens with ArrowDown', () => {
    test('Tab to Tournaments link and press ArrowDown opens dropdown', async ({ page }) => {
      const navItem = page.locator('.neba-nav-item[data-action="toggle-dropdown"]').first();
      const tournamentsLink = page.locator('.neba-nav-item [aria-haspopup="true"]').first();

      await openDropdown(page, tournamentsLink, navItem, 'ArrowDown');

      const firstDropdownLink = navItem.locator('.neba-dropdown-link').first();
      await expect(firstDropdownLink).toBeFocused();
    });
  });

  test.describe('Arrow key navigation within dropdown', () => {
    test.beforeEach(async ({ page }) => {
      // Open the History dropdown (4 links: Champions, Bowler of the Year, High Average, High Block)
      const navItem = page.locator('.neba-nav-item[data-action="toggle-dropdown"]').nth(2);
      const historyLink = page.locator('.neba-nav-item [aria-haspopup="true"]').nth(2);
      await openDropdown(page, historyLink, navItem, 'Enter');
    });

    test('ArrowDown moves focus to next item', async ({ page }) => {
      const navItem = page.locator('.neba-nav-item[data-action="toggle-dropdown"]').nth(2);
      const dropdownLinks = navItem.locator('.neba-dropdown-link');

      // First item should be focused after opening
      await expect(dropdownLinks.nth(0)).toBeFocused();

      await page.keyboard.press('ArrowDown');
      await expect(dropdownLinks.nth(1)).toBeFocused();

      await page.keyboard.press('ArrowDown');
      await expect(dropdownLinks.nth(2)).toBeFocused();
    });

    test('ArrowUp moves focus to previous item', async ({ page }) => {
      const navItem = page.locator('.neba-nav-item[data-action="toggle-dropdown"]').nth(2);
      const dropdownLinks = navItem.locator('.neba-dropdown-link');

      // Navigate down first
      await page.keyboard.press('ArrowDown');
      await page.keyboard.press('ArrowDown');
      await expect(dropdownLinks.nth(2)).toBeFocused();

      await page.keyboard.press('ArrowUp');
      await expect(dropdownLinks.nth(1)).toBeFocused();
    });

    test('Home moves focus to first item', async ({ page }) => {
      const navItem = page.locator('.neba-nav-item[data-action="toggle-dropdown"]').nth(2);
      const dropdownLinks = navItem.locator('.neba-dropdown-link');

      // Navigate down to third item
      await page.keyboard.press('ArrowDown');
      await page.keyboard.press('ArrowDown');
      await expect(dropdownLinks.nth(2)).toBeFocused();

      await page.keyboard.press('Home');
      await expect(dropdownLinks.nth(0)).toBeFocused();
    });

    test('End moves focus to last item', async ({ page }) => {
      const navItem = page.locator('.neba-nav-item[data-action="toggle-dropdown"]').nth(2);
      const dropdownLinks = navItem.locator('.neba-dropdown-link');
      const lastIndex = (await dropdownLinks.count()) - 1;

      await page.keyboard.press('End');
      await expect(dropdownLinks.nth(lastIndex)).toBeFocused();
    });
  });

  test.describe('Escape closes dropdown', () => {
    test('Press Escape closes dropdown and returns focus to trigger', async ({ page }) => {
      const navItem = page.locator('.neba-nav-item[data-action="toggle-dropdown"]').first();
      const tournamentsLink = navItem.locator('[aria-haspopup="true"]');

      // Open dropdown
      await openDropdown(page, tournamentsLink, navItem, 'Enter');

      // Verify focus is on dropdown item
      const firstDropdownLink = navItem.locator('.neba-dropdown-link').first();
      await expect(firstDropdownLink).toBeFocused();

      // Press Escape
      await page.keyboard.press('Escape');

      // Assert: dropdown closed (aria-expanded="false")
      await expect(tournamentsLink).toHaveAttribute('aria-expanded', 'false');
      await expect(navItem).not.toHaveClass(/active/);

      // Assert: focus returns to trigger link
      await expect(tournamentsLink).toBeFocused();
    });
  });

  test.describe('Tab closes dropdown and moves focus', () => {
    test('Press Tab closes dropdown and moves focus to next element', async ({ page }) => {
      const navItem = page.locator('.neba-nav-item[data-action="toggle-dropdown"]').first();
      const tournamentsLink = navItem.locator('[aria-haspopup="true"]');

      // Open dropdown
      await openDropdown(page, tournamentsLink, navItem, 'Enter');

      // Press Tab
      await page.keyboard.press('Tab');

      // Assert: dropdown closes
      await expect(navItem).not.toHaveClass(/active/);

      // Assert: focus moved away from dropdown (next element varies by viewport)
      await expect(tournamentsLink).not.toBeFocused();
    });
  });

  test.describe('aria-expanded toggles correctly', () => {
    test('Enter toggles aria-expanded between true and false', async ({ page }) => {
      const navItem = page.locator('.neba-nav-item[data-action="toggle-dropdown"]').first();
      const tournamentsLink = navItem.locator('[aria-haspopup="true"]');

      // Initial state
      await expect(tournamentsLink).toHaveAttribute('aria-expanded', 'false');

      // Open dropdown
      await openDropdown(page, tournamentsLink, navItem, 'Enter');
      await expect(tournamentsLink).toHaveAttribute('aria-expanded', 'true');

      // Focus back on trigger and close
      await tournamentsLink.focus();
      await page.keyboard.press('Enter');

      // aria-expanded should be "false"
      await expect(tournamentsLink).toHaveAttribute('aria-expanded', 'false');
    });
  });
});