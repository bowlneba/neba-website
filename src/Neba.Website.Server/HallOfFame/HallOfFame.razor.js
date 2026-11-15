const instances = new WeakMap();

/** Lowercase, trimmed, accent-free text, so "jose" finds "José". */
export function normalize(text) {
    return (text ?? '')
        .normalize('NFD')
        .replace(/\p{M}/gu, '')
        .trim()
        .toLowerCase();
}

/** The page renders each card's categories as a JSON array, so a name can hold any character. */
export function parseCategories(raw) {
    try {
        const value = JSON.parse(raw ?? '[]');
        return Array.isArray(value) ? value : [];
    } catch {
        return [];
    }
}

/** Reads the cards once, so typing does not re-normalize every name on every keystroke. */
export function buildIndex(root) {
    return [...root.querySelectorAll('[data-hof-card]')].map((card) => ({
        card,
        name: normalize(card.dataset.name),
        categories: new Set(parseCategories(card.dataset.categories)),
        year: card.closest('[data-hof-year]'),
    }));
}

/** An entry matches when its name contains the term and, if a category is chosen, it carries that category. */
export function matches(entry, term, category) {
    return entry.name.includes(term) && (category === '' || entry.categories.has(category));
}

/** Hides cards that do not match, hides classes left empty, and updates the status text. Writes only hidden and text. */
export function applyFilter(root, search, category, index = buildIndex(root)) {
    const term = normalize(search);
    const filtering = term !== '' || category !== '';
    const shownByYear = new Map();
    let shown = 0;

    index.forEach((entry) => {
        const visible = matches(entry, term, category);
        entry.card.hidden = !visible;

        if (visible) {
            shown += 1;
            shownByYear.set(entry.year, (shownByYear.get(entry.year) ?? 0) + 1);
        }
    });

    root.querySelectorAll('[data-hof-year]').forEach((year) => {
        const inYear = shownByYear.get(year) ?? 0;
        year.hidden = inYear === 0;

        const label = year.querySelector('[data-hof-shown]');
        if (label) {
            label.textContent = filtering ? `${inYear} shown` : '';
        }
    });

    const stage = root.querySelector('[data-hof-stage]');
    if (stage) {
        stage.hidden = shown === 0;
    }

    // One always-present live region, so a zero-result search is announced as a text change.
    const status = root.querySelector('[data-hof-status]');
    if (status) {
        const none = shown === 0 && index.length > 0;
        status.classList.toggle('hof-status--empty', none);

        if (none) {
            status.textContent = 'No inductees match your search.';
        } else {
            status.textContent = filtering ? `Showing ${shown} of ${index.length}` : '';
        }
    }
}

export function initialize(root) {
    if (!root || instances.has(root)) {
        return;
    }

    const searchInput = root.querySelector('[data-hof-search]');
    const chips = [...root.querySelectorAll('[data-hof-category]')];
    const instance = { index: buildIndex(root), search: searchInput?.value ?? '', category: '', cleanups: [] };

    const refresh = () => applyFilter(root, instance.search, instance.category, instance.index);

    // This script owns the pressed state; the page markup leaves aria-pressed off.
    const press = (chosen) => chips.forEach((chip) => chip.setAttribute('aria-pressed', chip === chosen ? 'true' : 'false'));
    press(chips.find((chip) => (chip.dataset.hofCategory ?? '') === '') ?? null);

    const onInput = () => {
        instance.search = searchInput.value;
        refresh();
    };
    searchInput?.addEventListener('input', onInput);
    instance.cleanups.push(() => searchInput?.removeEventListener('input', onInput));

    chips.forEach((chip) => {
        const onClick = () => {
            instance.category = chip.dataset.hofCategory ?? '';
            press(chip);
            refresh();
        };
        chip.addEventListener('click', onClick);
        instance.cleanups.push(() => chip.removeEventListener('click', onClick));
    });

    instances.set(root, instance);
    refresh();
}

/** Called after the page re-renders the cards (a reload after a photo save): re-read them and apply the current filter again. */
export function refresh(root) {
    const instance = instances.get(root);
    if (!instance) {
        return;
    }

    instance.index = buildIndex(root);
    applyFilter(root, instance.search, instance.category, instance.index);
}

export function dispose(root) {
    const instance = instances.get(root);
    if (!instance) {
        return;
    }

    instance.cleanups.forEach((cleanup) => cleanup());
    instances.delete(root);
}
