// RoleButtonSpaceGuard - shared helper for role="button" elements driven by @onkeydown
//
// Blazor's @onkeydown:preventDefault directive is static per render, not per keystroke,
// so it can't be scoped to "only the Space key" without blocking every other key
// (including Tab) on the element. This module attaches a real client-side keydown
// listener, scoped to the Space key only, via event delegation on a stable container
// so it keeps working across re-renders that add/remove role="button" descendants.

const attachedContainers = new Set();

/**
 * Prevents the browser's default page-scroll action when Space is pressed on a
 * descendant role="button" element inside the given container, without affecting
 * Tab, Enter, or any other key.
 * @param {string} containerId - id of a stable container that contains the role="button" elements
 */
export function guardSpaceKey(containerId) {
    if (attachedContainers.has(containerId)) {
        return;
    }

    const container = document.getElementById(containerId);
    if (!container) {
        return;
    }

    container.addEventListener('keydown', (event) => {
        if (event.key !== ' ') {
            return;
        }

        const target = event.target;
        if (target instanceof Element && target.getAttribute('role') === 'button') {
            event.preventDefault();
        }
    });

    attachedContainers.add(containerId);
}
