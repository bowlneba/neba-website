// Responsive data tables: see wwwroot/neba-data-table.css for the convention.
// Labels secondary cells from the header row and toggles row detail on phones.

export const MOBILE_QUERY = '(max-width: 767px)';
export const PRIMARY_COLUMNS = 3;

const isMobile = () => globalThis.matchMedia?.(MOBILE_QUERY).matches ?? false;

/**
 * Remove everything enhanceTable added, for a table whose header no longer needs it.
 * @param {HTMLTableElement} table
 */
function resetTable(table) {
    table.querySelectorAll('[data-secondary]').forEach(el => {
        el.removeAttribute('data-secondary');
        el.removeAttribute('data-label');
    });

    table.querySelectorAll('tr[data-expandable]').forEach(row => {
        for (const name of ['data-expandable', 'data-open', 'tabindex', 'aria-expanded']) {
            row.removeAttribute(name);
        }
    });
}

/**
 * Mark the secondary header and cells of a table and make its rows expandable.
 * Safe to call repeatedly; rows whose cell count differs from the header (group or empty-state rows) are skipped.
 * @param {HTMLTableElement} table
 */
export function enhanceTable(table) {
    const headers = Array.from(table.querySelectorAll('thead th'));
    if (headers.length <= PRIMARY_COLUMNS) {
        resetTable(table);
        return;
    }

    headers.forEach((th, i) => {
        if (i >= PRIMARY_COLUMNS) {
            th.dataset.secondary = '';
        }
    });

    const mobile = isMobile();

    for (const row of table.querySelectorAll('tbody tr')) {
        const cells = Array.from(row.children).filter(c => c.tagName === 'TD');
        if (cells.length !== headers.length) {
            continue;
        }

        cells.forEach((cell, i) => {
            if (i >= PRIMARY_COLUMNS) {
                cell.dataset.secondary = '';
                cell.dataset.label = headers[i].textContent.trim();
            }
        });

        row.dataset.expandable = '';

        if (mobile) {
            row.tabIndex = 0;
            row.setAttribute('aria-expanded', row.hasAttribute('data-open') ? 'true' : 'false');
        } else {
            row.removeAttribute('tabindex');
            row.removeAttribute('aria-expanded');
            row.removeAttribute('data-open');
        }
    }
}

/**
 * Make the header of a .neba-collapsible section a toggle on phones.
 * @param {Element} header
 */
export function enhanceCollapsible(header) {
    const section = header.closest('.neba-collapsible');
    if (!section) {
        return;
    }

    if (isMobile()) {
        header.tabIndex = 0;
        header.setAttribute('role', 'button');
        header.setAttribute('aria-expanded', section.hasAttribute('data-collapsed') ? 'false' : 'true');
    } else {
        header.removeAttribute('tabindex');
        header.removeAttribute('role');
        header.removeAttribute('aria-expanded');
        section.removeAttribute('data-collapsed');
    }
}

/**
 * Enhance the table and collapsible header that contain the node, and, when deep, any inside it.
 * @param {Element} node
 * @param {boolean} deep
 */
function enhanceAround(node, deep) {
    const table = node.closest('table.neba-data-table');
    if (table) {
        enhanceTable(table);
    }

    const header = node.closest('.neba-collapsible > .neba-collapsible-header');
    if (header) {
        enhanceCollapsible(header);
    }

    if (deep && node.querySelector('table.neba-data-table, .neba-collapsible-header')) {
        enhanceAll(node);
    }
}

export function enhanceAll(root = document) {
    root.querySelectorAll('table.neba-data-table').forEach(enhanceTable);
    root.querySelectorAll('.neba-collapsible > .neba-collapsible-header').forEach(enhanceCollapsible);
}

function toggleSection(header) {
    const collapsed = header.closest('.neba-collapsible').toggleAttribute('data-collapsed');
    header.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
}

function toggleRow(row) {
    const open = row.toggleAttribute('data-open');
    row.setAttribute('aria-expanded', open ? 'true' : 'false');
}

function handleClick(event) {
    if (!isMobile() || !(event.target instanceof Element)) {
        return;
    }

    // Let links and controls inside a row do their own job
    if (event.target.closest('a, button, input, select, textarea, label')) {
        return;
    }

    const header = event.target.closest('.neba-collapsible > .neba-collapsible-header');
    if (header) {
        toggleSection(header);
        return;
    }

    const row = event.target.closest('table.neba-data-table tbody tr[data-expandable]');
    if (row) {
        toggleRow(row);
    }
}

function handleKeydown(event) {
    if (!isMobile() || (event.key !== 'Enter' && event.key !== ' ')) {
        return;
    }

    if (!(event.target instanceof Element)) {
        return;
    }

    if (event.target.matches('.neba-collapsible > .neba-collapsible-header')) {
        event.preventDefault();
        toggleSection(event.target);
    } else if (event.target.matches('table.neba-data-table tbody tr[data-expandable]')) {
        event.preventDefault();
        toggleRow(event.target);
    }
}

let started = false;

/** Wire up the document once: Blazor renders and re-renders tables after load, so watch for them. */
export function init() {
    if (started) {
        return;
    }

    started = true;

    let scheduled = false;
    let everything = false;
    const added = new Set();
    const changed = new Set();
    const schedule = () => {
        if (scheduled) {
            return;
        }

        scheduled = true;
        requestAnimationFrame(() => {
            scheduled = false;
            if (!globalThis.document) {
                return;
            }

            if (everything) {
                everything = false;
                added.clear();
                changed.clear();
                enhanceAll();
                return;
            }

            const addedNodes = Array.from(added);
            const changedNodes = Array.from(changed);
            added.clear();
            changed.clear();
            addedNodes.filter(node => node.isConnected).forEach(node => enhanceAround(node, true));
            changedNodes.filter(node => node.isConnected).forEach(node => enhanceAround(node, false));
        });
    };

    const observer = new MutationObserver(mutations => {
        for (const mutation of mutations) {
            // The parent covers rows added to or removed from an existing table
            if (mutation.target instanceof Element) {
                changed.add(mutation.target);
            }

            mutation.addedNodes.forEach(node => {
                if (node instanceof Element) {
                    added.add(node);
                }
            });
        }

        schedule();
    });

    document.addEventListener('click', handleClick);
    document.addEventListener('keydown', handleKeydown);
    observer.observe(document.body, { childList: true, subtree: true });
    globalThis.matchMedia?.(MOBILE_QUERY).addEventListener?.('change', () => {
        everything = true;
        schedule();
    });

    enhanceAll();
}
