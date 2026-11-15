import { normalize } from './HallOfFame.razor.js';

const instances = new WeakMap();

/** Reads the rows once, so typing does not re-normalize every name on every keystroke. */
export function buildIndex(root) {
    return [...root.querySelectorAll('[data-pts-row]')].map((row) => ({
        row,
        name: normalize(row.dataset.name),
    }));
}

/** Hides rows whose name does not contain the term, hides sections left empty, and updates the status text. Writes only hidden and text. */
export function applyFilter(root, search, index = buildIndex(root)) {
    const term = normalize(search);
    let shown = 0;

    index.forEach((entry) => {
        const visible = entry.name.includes(term);
        entry.row.hidden = !visible;
        if (visible) {
            shown += 1;
        }
    });

    root.querySelectorAll('[data-pts-section]').forEach((section) => {
        section.hidden = !section.querySelector('[data-pts-row]:not([hidden])');
    });

    // One always-present live region, so a zero-result search is announced as a text change.
    const status = root.querySelector('[data-pts-status]');
    if (status) {
        const none = shown === 0 && index.length > 0;
        status.classList.toggle('pts-status--empty', none);

        if (none) {
            status.textContent = 'No bowlers match your search.';
        } else {
            status.textContent = term === '' ? '' : `Showing ${shown} of ${index.length}`;
        }
    }

    return shown;
}

/**
 * dotNet is optional. The page passes itself so the first search can ask it to show the rows still behind
 * "Show more". A MutationObserver reads and filters rows the page adds, in the same task they appear, so a
 * row that does not match the search never shows.
 */
export function initialize(root, dotNet = null) {
    if (!root || instances.has(root)) {
        return;
    }

    const input = root.querySelector('[data-pts-search]');
    const instance = { index: buildIndex(root), search: input?.value ?? '', expanding: false, cleanup: null };

    const filter = () => applyFilter(root, instance.search, instance.index);

    const onInput = () => {
        instance.search = input.value;

        if (dotNet && !instance.expanding && instance.search.trim() !== '' && root.dataset.ptsComplete === 'false') {
            instance.expanding = true;
            // The observer filters the new rows. Filtering here covers the rows already on the page, and clears the
            // flag whether the call worked or not, so a failure never leaves the search stuck.
            const done = () => {
                instance.expanding = false;
                filter();
            };
            dotNet.invokeMethodAsync('ExpandForSearch').then(done, done);
            return;
        }

        filter();
    };
    input?.addEventListener('input', onInput);

    const observer = new MutationObserver((mutations) => {
        const added = mutations.some((mutation) => [...mutation.addedNodes].some(
            (node) => node.nodeType === Node.ELEMENT_NODE && (node.matches('[data-pts-row]') || node.querySelector('[data-pts-row]'))));
        if (added) {
            instance.index = buildIndex(root);
            filter();
        }
    });
    observer.observe(root, { childList: true, subtree: true });

    instance.cleanup = () => {
        input?.removeEventListener('input', onInput);
        observer.disconnect();
    };

    instances.set(root, instance);
    filter();
}

export function dispose(root) {
    const instance = instances.get(root);
    if (!instance) {
        return;
    }

    instance.cleanup?.();
    instances.delete(root);
}
