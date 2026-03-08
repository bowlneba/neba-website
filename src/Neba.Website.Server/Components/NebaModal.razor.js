/**
 * Sets body overflow to prevent or restore scrolling when a modal is open.
 * @param {boolean} hidden - True to prevent scrolling, false to restore it.
 */
export function setBodyOverflow(hidden) {
    document.body.style.overflow = hidden ? 'hidden' : '';
}

const focusableSelector = [
    'a[href]',
    'area[href]',
    'button:not([disabled])',
    'input:not([disabled]):not([type="hidden"])',
    'select:not([disabled])',
    'textarea:not([disabled])',
    'iframe',
    'object',
    'embed',
    '[contenteditable]:not([contenteditable="false"])',
    '[tabindex]:not([tabindex="-1"])'
].join(',');

/** @type {HTMLElement | null} */
let trappedElement = null;

/** @type {((event: KeyboardEvent) => void) | null} */
let trapKeyDownHandler = null;

/**
 * Focuses a DOM element without scrolling the page.
 * @param {HTMLElement | null} element - The element to focus.
 */
export function focusElement(element) {
    if (!element || typeof element.focus !== 'function') {
        return;
    }

    element.focus({ preventScroll: true });
}

/**
 * Enables a keyboard focus trap for the provided dialog element.
 * @param {HTMLElement | null} element - The dialog element to trap focus within.
 */
export function enableFocusTrap(element) {
    if (!element) {
        return;
    }

    disableFocusTrap();

    trapKeyDownHandler = (event) => {
        if (event.key !== 'Tab') {
            return;
        }

        const focusableElements = getFocusableElements(element);

        if (focusableElements.length === 0) {
            event.preventDefault();
            focusElement(element);
            return;
        }

        const firstFocusable = focusableElements[0];
        const lastFocusable = focusableElements.at(-1);
        const activeElement = document.activeElement;

        if (!lastFocusable) {
            return;
        }

        if (event.shiftKey) {
            if (activeElement === firstFocusable || !element.contains(activeElement)) {
                event.preventDefault();
                focusElement(lastFocusable);
            }

            return;
        }

        if (activeElement === lastFocusable || !element.contains(activeElement)) {
            event.preventDefault();
            focusElement(firstFocusable);
        }
    };

    element.addEventListener('keydown', trapKeyDownHandler);
    trappedElement = element;
}

/**
 * Disables the currently active keyboard focus trap.
 */
export function disableFocusTrap() {
    if (trappedElement && trapKeyDownHandler) {
        trappedElement.removeEventListener('keydown', trapKeyDownHandler);
    }

    trappedElement = null;
    trapKeyDownHandler = null;
}

/**
 * Gets all currently focusable elements within the provided container.
 * @param {HTMLElement} container - The parent container.
 * @returns {HTMLElement[]} Focusable child elements in tab order.
 */
function getFocusableElements(container) {
    return Array.from(container.querySelectorAll(focusableSelector))
        .filter((candidate) => isFocusable(candidate));
}

/**
 * Determines whether an element can currently receive focus.
 * @param {Element} element - The element to evaluate.
 * @returns {element is HTMLElement} True when focusable.
 */
function isFocusable(element) {
    if (!(element instanceof HTMLElement)) {
        return false;
    }

    if (element.hasAttribute('disabled') || element.getAttribute('aria-hidden') === 'true') {
        return false;
    }

    return element.getClientRects().length > 0;
}
