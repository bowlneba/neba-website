// Responsive data tables: see wwwroot/neba-data-table.css for the convention.
// Labels secondary cells from the header row and toggles row detail on phones.

export const MOBILE_QUERY = '(max-width: 767px)';
export const PRIMARY_COLUMNS = 3;
export const COMPACT_CONTAINER_WIDTH = 800;
// Room for a classic scrollbar (about 17px) plus slack; switching modes can add or remove one.
export const SCROLLBAR_ALLOWANCE = 30;
export const COMPACT_RELEASE_WIDTH = COMPACT_CONTAINER_WIDTH + SCROLLBAR_ALLOWANCE;
const CONTAINER_SELECTOR = '[data-table-container]';
const EXPANDABLE_ROW = 'table.neba-data-table[data-compact="expandable"] tbody tr[data-expandable]';

const staticContainers = new WeakSet();

const isMobile = () => globalThis.matchMedia?.(MOBILE_QUERY).matches ?? false;

/**
 * 'expandable': a page grid on a phone (rows open on tap).
 * 'static': a table in a table container that is narrow or on a phone (rows show every stat, no tap).
 * null: show all columns.
 * @param {HTMLTableElement} table
 */
export function compactMode(table) {
    const container = table.closest(CONTAINER_SELECTOR);
    if (!container) {
        return isMobile() ? 'expandable' : null;
    }

    // A table already static stays static until the container clears a wider threshold. Switching modes changes
    // row height, which can add or remove a scrollbar and shift clientWidth by a few pixels; without the gap a
    // container near the threshold would flip back and forth.
    const width = container.clientWidth;
    // The state lives on the container, not the table: Blazor can replace the table element and its attributes.
    const limit = staticContainers.has(container) ? COMPACT_RELEASE_WIDTH : COMPACT_CONTAINER_WIDTH;
    const narrow = width > 0 && width < limit;
    const isStatic = narrow || isMobile();
    if (isStatic) {
        staticContainers.add(container);
    } else {
        staticContainers.delete(container);
    }

    return isStatic ? 'static' : null;
}

let resizeObserver;
const watched = new WeakSet();

/** Re-enhance a container's tables when its width changes (the modal opens, or the window resizes). */
function watchContainer(table) {
    const container = table.closest(CONTAINER_SELECTOR);
    if (!container || watched.has(container) || typeof ResizeObserver !== 'function') {
        return;
    }

    watched.add(container);
    resizeObserver ??= new ResizeObserver(entries => {
        for (const { target } of entries) {
            if (!target.isConnected) {
                resizeObserver.unobserve(target);
                continue;
            }

            target.querySelectorAll('table.neba-data-table').forEach(enhanceTable);
        }
    });
    resizeObserver.observe(container);
}

/**
 * Remove everything enhanceTable added, for a table whose header no longer needs it.
 * @param {HTMLTableElement} table
 */
function resetTable(table) {
    delete table.dataset.compact;

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

    const mode = compactMode(table);
    if (mode) {
        table.dataset.compact = mode;
    } else {
        delete table.dataset.compact;
    }

    watchContainer(table);

    for (const row of table.querySelectorAll('tbody tr')) {
        const cells = Array.from(row.children).filter(c => c.tagName === 'TD');
        if (cells.length !== headers.length) {
            continue;   // group, empty-state and Virtualize spacer rows have no matching cells
        }

        cells.forEach((cell, i) => {
            if (i >= PRIMARY_COLUMNS) {
                cell.dataset.secondary = '';
                cell.dataset.label = headers[i].textContent.trim();
            }
        });

        row.dataset.expandable = '';

        if (mode === 'expandable') {
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
 * Record the table and collapsible header that contain the node, and, when deep, any inside it.
 * Collecting them in sets lets a flush enhance each one once, however many nodes changed inside it:
 * a table rendered row by row adds thousands of nodes, and a pass over the table per node is quadratic.
 * @param {Element} node
 * @param {boolean} deep
 * @param {Set<Element>} tables
 * @param {Set<Element>} headers
 */
function collectAround(node, deep, tables, headers) {
    const table = node.closest('table.neba-data-table');
    if (table) {
        tables.add(table);
    }

    const header = node.closest('.neba-collapsible > .neba-collapsible-header');
    if (header) {
        headers.add(header);
    }

    if (deep) {
        node.querySelectorAll('table.neba-data-table').forEach(inner => tables.add(inner));
        node.querySelectorAll('.neba-collapsible > .neba-collapsible-header').forEach(inner => headers.add(inner));
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
    if (!(event.target instanceof Element)) {
        return;
    }

    // Let links and controls inside a row do their own job
    if (event.target.closest('a, button, input, select, textarea, label')) {
        return;
    }

    const header = event.target.closest('.neba-collapsible > .neba-collapsible-header');
    if (header) {
        if (isMobile()) {
            toggleSection(header);
        }

        return;
    }

    const row = event.target.closest(EXPANDABLE_ROW);
    if (row) {
        toggleRow(row);
    }
}

function handleKeydown(event) {
    if ((event.key !== 'Enter' && event.key !== ' ') || !(event.target instanceof Element)) {
        return;
    }

    if (isMobile() && event.target.matches('.neba-collapsible > .neba-collapsible-header')) {
        event.preventDefault();
        toggleSection(event.target);
    } else if (event.target.matches(EXPANDABLE_ROW)) {
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
            const tables = new Set();
            const headers = new Set();
            addedNodes.filter(node => node.isConnected).forEach(node => collectAround(node, true, tables, headers));
            changedNodes.filter(node => node.isConnected).forEach(node => collectAround(node, false, tables, headers));
            tables.forEach(enhanceTable);
            headers.forEach(enhanceCollapsible);
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
