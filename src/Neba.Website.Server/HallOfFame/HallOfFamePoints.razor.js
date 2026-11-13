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

/** Reads the rows again after the page added some, then applies the current search to them. */
export function refresh(root) {
    const instance = instances.get(root);
    if (!instance) {
        return;
    }

    instance.index = buildIndex(root);
    instance.expanding = false;
    applyFilter(root, instance.search, instance.index);
}

/**
 * dotNet is optional. The page passes itself so the first search can ask it to show the rows still behind
 * "Show more"; the page then calls refresh once they are in the document.
 */
export function initialize(root, dotNet = null) {
    if (!root || instances.has(root)) {
        return;
    }

    const input = root.querySelector('[data-pts-search]');
    const instance = { index: buildIndex(root), search: input?.value ?? '', expanding: false, cleanup: null };

    const onInput = () => {
        instance.search = input.value;

        if (dotNet && !instance.expanding && instance.search.trim() !== '' && root.dataset.ptsComplete === 'false') {
            instance.expanding = true;
            dotNet.invokeMethodAsync('ExpandForSearch').catch(() => {
                instance.expanding = false;
            });
            return;
        }

        applyFilter(root, instance.search, instance.index);
    };
    input?.addEventListener('input', onInput);
    instance.cleanup = () => input?.removeEventListener('input', onInput);

    instances.set(root, instance);
    applyFilter(root, instance.search, instance.index);
}

export function dispose(root) {
    const instance = instances.get(root);
    if (!instance) {
        return;
    }

    instance.cleanup?.();
    instances.delete(root);
}
