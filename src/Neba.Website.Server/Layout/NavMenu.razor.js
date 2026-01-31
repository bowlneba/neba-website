let dotNetReference = null;
let isInitialized = false;
let navigationObserver = null;

const handlers = {
    scroll: null,
    click: null,
    keydown: null,
    dropdownClick: null,
    dropdownKeydown: null
};

/**
 * Get breakpoint value from CSS custom property
 * @param {string} name - Breakpoint name (e.g., 'tablet-max')
 * @returns {number} - Breakpoint value in pixels
 */
function getBreakpointValue(name) {
    const value = getComputedStyle(document.documentElement)
        .getPropertyValue(`--neba-breakpoint-${name}`)
        .trim();

    return Number.parseInt(value, 10) || 1024;
}

/**
 * Handle scroll - add/remove shadow class on navbar
 */
function handleScroll() {
    const navbar = document.querySelector('.neba-navbar');

    if (!navbar) {
        return;
    }

    if (window.scrollY > 10) {
        navbar.classList.add('scrolled');
    }
    else {
        navbar.classList.remove('scrolled');
    }
}

/**
 * Handle click outside - close mobile menu if open
 * @param {MouseEvent} event
 */
function handleClickOutside(event) {
    const menu = document.querySelector('[data-menu]');
    const toggle = document.querySelector('[data-menu-toggle]');

    // Only process on mobile (when menu can be toggled)
    const breakpoint = getBreakpointValue('tablet-max');
    if (window.innerWidth > breakpoint) {
        return;
    }

    // Check if menu is open (has active class)
    if (!menu?.classList.contains('active')) return;

    // If click is outside both menu and toggle button
    if (!menu.contains(event.target) && !toggle?.contains(event.target)) {
        dotNetReference?.invokeMethodAsync('CloseMenu');
    }
}

/**
 * Handle keydown - Escape closes mobile menu and dropdowns
 * @param {KeyboardEvent} event
 */
function handleKeydown(event) {
    if (event.key === 'Escape') {
        // Close any open dropdowns
        closeAllDropdowns();

        // Close mobile menu if open
        const menu = document.querySelector('[data-menu]');
        if (menu?.classList.contains('active')) {
            dotNetReference?.invokeMethodAsync('CloseMenu');
            const toggle = document.querySelector('[data-menu-toggle]');
            toggle?.focus();
        }
    }
}

/**
 * Close all open dropdowns
 */
function closeAllDropdowns() {
    const openItems = document.querySelectorAll('.neba-nav-item.active');
    openItems.forEach(item => {
        item.classList.remove('active');
        const link = item.querySelector('[aria-haspopup]');
        link?.setAttribute('aria-expanded', 'false');
    });
}

/**
 * Update aria-current="page" on active navigation links
 * Called on initial load and after navigation
 */
function updateAriaCurrent() {
    // Remove existing aria-current from all nav links
    const allNavLinks = document.querySelectorAll('.neba-nav-link, .neba-dropdown-link');
    allNavLinks.forEach(link => {
        link.removeAttribute('aria-current');
    });

    // Add aria-current="page" to active links (Blazor adds 'active' class)
    const activeLinks = document.querySelectorAll('.neba-nav-link.active, .neba-dropdown-link.active');
    activeLinks.forEach(link => {
        link.setAttribute('aria-current', 'page');
    });
}

/**
 * Set up MutationObserver to watch for active class changes on nav links
 */
function setupNavigationObserver() {
    const navMenu = document.querySelector('.neba-nav-menu');
    if (!navMenu) return;

    navigationObserver = new MutationObserver((mutations) => {
        let shouldUpdate = false;
        for (const mutation of mutations) {
            if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
                shouldUpdate = true;
                break;
            }
        }
        if (shouldUpdate) {
            updateAriaCurrent();
        }
    });

    navigationObserver.observe(navMenu, {
        attributes: true,
        attributeFilter: ['class'],
        subtree: true
    });

    // Initial update
    updateAriaCurrent();
}

/**
 * Handle dropdown toggle clicks
 * @param {MouseEvent} event
 */
function handleDropdownClick(event) {
    const navItem = event.target.closest('[data-action="toggle-dropdown"]');
    if (!navItem) {
        // Click outside dropdown - close all
        closeAllDropdowns();
        return;
    }

    const link = navItem.querySelector('.neba-nav-link');
    if (!link?.contains(event.target)) {
        // Click was on dropdown item, not the toggle link
        return;
    }

    event.preventDefault();

    const isOpen = navItem.classList.contains('active');

    // Close all other dropdowns first
    closeAllDropdowns();

    // Toggle this dropdown
    if (!isOpen) {
        navItem.classList.add('active');
        link.setAttribute('aria-expanded', 'true');

        // Focus first menu item when opening
        const firstMenuItem = navItem.querySelector('.neba-dropdown-link');
        firstMenuItem?.focus();
    }
}

/**
 * Handle keyboard navigation within dropdowns
 * @param {KeyboardEvent} event
 */
function handleDropdownKeydown(event) {
    const dropdown = event.target instanceof Element ? event.target.closest('.neba-dropdown') : null;
    const navItem = event.target instanceof Element ? event.target.closest('.neba-nav-item') : null;

    if (!dropdown && !navItem?.querySelector('[aria-haspopup]')) {
        return;
    }

    const triggerLink = navItem?.querySelector('[aria-haspopup]');
    const menuItems = navItem ? Array.from(navItem.querySelectorAll('.neba-dropdown-link')) : [];
    const currentIndex = menuItems.indexOf(document.activeElement);

    switch (event.key) {
        case 'ArrowDown':
            event.preventDefault();
            if (document.activeElement === triggerLink) {
                // Open dropdown and focus first item
                if (!navItem.classList.contains('active')) {
                    closeAllDropdowns();
                    navItem.classList.add('active');
                    triggerLink.setAttribute('aria-expanded', 'true');
                }
                menuItems[0]?.focus();
            } else if (currentIndex >= 0 && currentIndex < menuItems.length - 1) {
                // Move to next item
                menuItems[currentIndex + 1].focus();
            }
            break;

        case 'ArrowUp':
            event.preventDefault();
            if (currentIndex > 0) {
                // Move to previous item
                menuItems[currentIndex - 1].focus();
            } else if (currentIndex === 0) {
                // Return focus to trigger
                triggerLink?.focus();
            }
            break;

        case 'Home':
            event.preventDefault();
            if (menuItems.length > 0) {
                menuItems[0].focus();
            }
            break;

        case 'End':
            event.preventDefault();
            if (menuItems.length > 0) {
                menuItems[menuItems.length - 1].focus();
            }
            break;

        case 'Enter':
        case ' ':
            if (document.activeElement === triggerLink) {
                event.preventDefault();
                if (navItem.classList.contains('active')) {
                    closeAllDropdowns();
                } else {
                    closeAllDropdowns();
                    navItem.classList.add('active');
                    triggerLink.setAttribute('aria-expanded', 'true');
                    menuItems[0]?.focus();
                }
            }
            break;

        case 'Tab':
            // Close dropdown when tabbing out
            if (navItem?.classList.contains('active')) {
                closeAllDropdowns();
            }
            break;
    }
}

/**
 * Prevent body scrolling when mobile menu is open
 * @param {boolean} prevent - Whether to prevent scrolling
 */
function preventBodyScroll(prevent) {
    const body = document.body;
    const html = document.documentElement;

    if (prevent) {
        // Store current scroll position
        const scrollY = window.scrollY;

        // Prevent scrolling by fixing body position
        body.style.position = 'fixed';
        body.style.top = `-${scrollY}px`;
        body.style.width = '100%';
        body.style.overflow = 'hidden';

        // Store scroll position for restoration
        body.dataset.scrollY = scrollY;
    } else {
        // Restore scrolling
        const scrollY = parseInt(body.dataset.scrollY || '0', 10);

        // Remove fixed positioning
        body.style.position = '';
        body.style.top = '';
        body.style.width = '';
        body.style.overflow = '';

        // Restore scroll position
        window.scrollTo(0, scrollY);

        // Clean up
        delete body.dataset.scrollY;
    }
}

/**
 * Initialize navigation interactivity
 * @param {DotNetObjectReference} dotNetRef - Reference to Blazor component
 */
export function initialize(dotNetRef) {
    if (isInitialized) {
        dispose();
    }

    dotNetReference = dotNetRef;

    // Create bounded handlers
    handlers.scroll = handleScroll;
    handlers.click = handleClickOutside;
    handlers.keydown = handleKeydown;
    handlers.dropdownClick = handleDropdownClick;
    handlers.dropdownKeydown = handleDropdownKeydown;

    // Add event listeners
    globalThis.addEventListener('scroll', handlers.scroll, { passive: true });
    document.addEventListener('click', handlers.click);
    document.addEventListener('keydown', handlers.keydown);
    document.addEventListener('click', handlers.dropdownClick);
    document.addEventListener('keydown', handlers.dropdownKeydown);

    // Initial scroll check
    handleScroll();

    // Set up aria-current observer for navigation changes
    setupNavigationObserver();

    isInitialized = true;
}

/** Clean up event listeners and references */
export function dispose() {
    if (handlers.scroll) {
        globalThis.removeEventListener('scroll', handlers.scroll);
    }

    if (handlers.click) {
        document.removeEventListener('click', handlers.click);
    }

    if (handlers.keydown) {
        document.removeEventListener('keydown', handlers.keydown);
    }

    if (handlers.dropdownClick) {
        document.removeEventListener('click', handlers.dropdownClick);
    }

    if (handlers.dropdownKeydown) {
        document.removeEventListener('keydown', handlers.dropdownKeydown);
    }

    // Disconnect navigation observer
    if (navigationObserver) {
        navigationObserver.disconnect();
        navigationObserver = null;
    }

    // Clear references
    handlers.scroll = null;
    handlers.click = null;
    handlers.keydown = null;
    handlers.dropdownClick = null;
    handlers.dropdownKeydown = null;
    dotNetReference = null;
    isInitialized = false;
}

// Export functions for Blazor interop
export { preventBodyScroll, updateAriaCurrent };