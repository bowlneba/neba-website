const instances = new WeakMap();

/** Lowercase, trimmed, accent-free text, so "jose" finds "José". */
export function normalize(text) {
    return (text ?? '')
        .normalize('NFD')
        .replace(/\p{M}/gu, '')
        .trim()
        .toLowerCase();
}

/** A card matches when its name contains the term and, if a category is chosen, it carries that category. */
export function matches(card, term, category) {
    const name = normalize(card.dataset.name);
    const categories = (card.dataset.categories ?? '').split('|');

    return name.includes(term) && (category === '' || categories.includes(category));
}

/** Hides cards that do not match, hides classes left empty, and updates the status text. Reads and writes only hidden and text. */
export function applyFilter(root, search, category) {
    const term = normalize(search);
    const filtering = term !== '' || category !== '';
    const cards = [...root.querySelectorAll('[data-hof-card]')];
    let shown = 0;

    cards.forEach((card) => {
        const visible = matches(card, term, category);
        card.hidden = !visible;
        shown += visible ? 1 : 0;
    });

    root.querySelectorAll('[data-hof-year]').forEach((year) => {
        const inYear = year.querySelectorAll('[data-hof-card]:not([hidden])').length;
        year.hidden = inYear === 0;

        const label = year.querySelector('[data-hof-shown]');
        if (label) {
            label.textContent = filtering ? `${inYear} shown` : '';
        }
    });

    const stage = root.querySelector('[data-hof-stage]');
    const empty = root.querySelector('[data-hof-empty]');
    const status = root.querySelector('[data-hof-status]');

    if (stage) {
        stage.hidden = shown === 0;
    }

    if (empty) {
        empty.hidden = shown !== 0;
    }

    if (status) {
        status.textContent = filtering && shown > 0 ? `Showing ${shown} of ${cards.length}` : '';
    }
}

export function initialize(root) {
    if (!root || instances.has(root)) {
        return;
    }

    const searchInput = root.querySelector('[data-hof-search]');
    const chips = [...root.querySelectorAll('[data-hof-category]')];
    const state = { search: searchInput?.value ?? '', category: '' };
    const cleanups = [];

    const refresh = () => applyFilter(root, state.search, state.category);

    const onInput = () => {
        state.search = searchInput.value;
        refresh();
    };
    searchInput?.addEventListener('input', onInput);
    cleanups.push(() => searchInput?.removeEventListener('input', onInput));

    chips.forEach((chip) => {
        const onClick = () => {
            state.category = chip.dataset.hofCategory ?? '';
            chips.forEach((other) => other.setAttribute('aria-pressed', other === chip ? 'true' : 'false'));
            refresh();
        };
        chip.addEventListener('click', onClick);
        cleanups.push(() => chip.removeEventListener('click', onClick));
    });

    instances.set(root, cleanups);
    refresh();
}

export function dispose(root) {
    const cleanups = instances.get(root);
    if (!cleanups) {
        return;
    }

    cleanups.forEach((cleanup) => cleanup());
    instances.delete(root);
}
