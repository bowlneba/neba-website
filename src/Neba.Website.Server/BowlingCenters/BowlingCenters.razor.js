// BowlingCenters - Component-scoped JavaScript module
// Handles list scrolling functionality

import { guardSpaceKey } from '../js/RoleButtonSpaceGuard.js';

/**
 * Scrolls the centers list container to the top
 */
export function scrollToTop() {
    const element = document.querySelector('#centers-scroll-container');
    if (element) {
        element.scrollTop = 0;
    }
}

/**
 * Prevents the browser's default page-scroll action when Space is pressed on a
 * role="button" center card, without blocking Tab/Enter or any other key.
 */
export function initSpaceKeyGuard() {
    guardSpaceKey('centers-scroll-container');
}
