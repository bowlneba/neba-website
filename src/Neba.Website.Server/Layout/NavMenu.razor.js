let dotNetReference = null;
let isInitialized = false;

const handlers = {
    scroll: null,
    click: null,
    keydown: null,
    dropdownClick: null
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

    // Add event listeners
    globalThis.addEventListener('scroll', handlers.scroll, { passive: true });
    document.addEventListener('click', handlers.click);
    document.addEventListener('keydown', handlers.keydown);
    document.addEventListener('click', handlers.dropdownClick);

    // Initial scroll check
    handleScroll();

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

    // Clear references
    handlers.scroll = null;
    handlers.click = null;
    handlers.keydown = null;
    handlers.dropdownClick = null;
    dotNetReference = null;
    isInitialized = false;
}

// === DROPDOWN SUPPORT (Future) ===
export function toggleDropdown(element) {
    const navItem = element.closest('.neba-nav-item');
    const link = navItem?.querySelector('[aria-haspopup]');
    const isExpanded = navItem?.classList.contains('active');

    navItem?.classList.toggle('active');
    link?.setAttribute('aria-expanded', (!isExpanded).toString());
}

function handleDropdownKeydown(event, item) {
    const link = item.querySelector('[aria-haspopup]');
    const breakpoint = getBreakpoint('tablet-max');

    if (window.innerWidth > breakpoint) {
        if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            toggleDropdown(item);
        } else if (event.key === 'Escape') {
            item.classList.remove('active');
            link?.setAttribute('aria-expanded', 'false');
            link?.focus();
        }
    }
}

// Export functions for Blazor interop
export { preventBodyScroll };