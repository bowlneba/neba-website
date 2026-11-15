import { describe, test, expect, beforeEach } from '@jest/globals';
import {
    normalize,
    parseCategories,
    buildIndex,
    matches,
    applyFilter,
    initialize,
    refresh,
    dispose,
} from './HallOfFame.razor.js';

const card = (name, categories) =>
    `<div data-hof-card data-name="${name}" data-categories='${JSON.stringify(categories)}'>${name}</div>`;

function render() {
    document.body.innerHTML = `
        <div id="root">
            <input data-hof-search type="search">
            <button data-hof-category="">All</button>
            <button data-hof-category="Superior Performance">Superior Performance</button>
            <button data-hof-category="Friend of NEBA">Friend of NEBA</button>
            <p data-hof-status role="status"></p>
            <div data-hof-stage>
                <section data-hof-year id="y2024"><span data-hof-shown></span>
                    ${card('Jane Smith', ['Superior Performance'])}
                    ${card('José Marchetti', ['Superior Performance', 'Meritorious Service'])}
                </section>
                <section data-hof-year id="y2017"><span data-hof-shown></span>
                    ${card('Tom Davis', ['Friend of NEBA'])}
                </section>
            </div>
        </div>`;
    return document.getElementById('root');
}

const visibleNames = (root) =>
    [...root.querySelectorAll('[data-hof-card]:not([hidden])')].map((c) => c.dataset.name);
const status = (root) => root.querySelector('[data-hof-status]');
const pressed = (root) =>
    [...root.querySelectorAll('[data-hof-category]')].map((c) => c.getAttribute('aria-pressed'));
const type = (root, text) => {
    const input = root.querySelector('[data-hof-search]');
    input.value = text;
    input.dispatchEvent(new Event('input'));
};

describe('HallOfFame filter', () => {
    let root;

    beforeEach(() => {
        root = render();
    });

    describe('normalize', () => {
        test('should lowercase, trim and strip accents', () => {
            expect(normalize('  José ')).toBe('jose');
        });

        test('should treat null as empty', () => {
            expect(normalize(null)).toBe('');
        });
    });

    describe('parseCategories', () => {
        test('should read a JSON array, including names with a pipe', () => {
            expect(parseCategories('["A|B","C"]')).toEqual(['A|B', 'C']);
        });

        test('should return an empty list for missing, malformed or non-array data', () => {
            expect(parseCategories(undefined)).toEqual([]);
            expect(parseCategories('not json')).toEqual([]);
            expect(parseCategories('{"a":1}')).toEqual([]);
        });
    });

    describe('matches', () => {
        test('should match a name fragment ignoring case and accents', () => {
            const entry = buildIndex(root).find((e) => e.card.dataset.name === 'José Marchetti');
            expect(matches(entry, 'jose', '')).toBe(true);
            expect(matches(entry, 'march', '')).toBe(true);
            expect(matches(entry, 'smith', '')).toBe(false);
        });

        test('should require the chosen category exactly', () => {
            const entry = buildIndex(root).find((e) => e.card.dataset.name === 'José Marchetti');
            expect(matches(entry, '', 'Meritorious Service')).toBe(true);
            expect(matches(entry, '', 'Friend of NEBA')).toBe(false);
        });
    });

    describe('applyFilter', () => {
        test('should show everyone and clear the status when nothing is filtered', () => {
            applyFilter(root, '', '');

            expect(visibleNames(root)).toHaveLength(3);
            expect(status(root).textContent).toBe('');
            expect(root.querySelector('#y2024 [data-hof-shown]').textContent).toBe('');
        });

        test('should hide non-matching cards, hide empty classes and report counts', () => {
            applyFilter(root, 'tom', '');

            expect(visibleNames(root)).toEqual(['Tom Davis']);
            expect(root.querySelector('#y2024').hidden).toBe(true);
            expect(root.querySelector('#y2017').hidden).toBe(false);
            expect(root.querySelector('#y2017 [data-hof-shown]').textContent).toBe('1 shown');
            expect(status(root).textContent).toBe('Showing 1 of 3');
        });

        test('should combine the name and category filters', () => {
            applyFilter(root, 'jane', 'Superior Performance');
            expect(visibleNames(root)).toEqual(['Jane Smith']);

            applyFilter(root, 'tom', 'Superior Performance');
            expect(visibleNames(root)).toEqual([]);
        });

        test('should say so in the live region and hide the stage when nothing matches', () => {
            applyFilter(root, 'zzz', '');

            expect(status(root).textContent).toBe('No inductees match your search.');
            expect(status(root).classList.contains('hof-status--empty')).toBe(true);
            expect(root.querySelector('[data-hof-stage]').hidden).toBe(true);
        });

        test('should bring everything back when the filters clear', () => {
            applyFilter(root, 'zzz', '');
            applyFilter(root, '', '');

            expect(visibleNames(root)).toHaveLength(3);
            expect(status(root).textContent).toBe('');
            expect(status(root).classList.contains('hof-status--empty')).toBe(false);
            expect(root.querySelector('[data-hof-stage]').hidden).toBe(false);
        });
    });

    describe('initialize', () => {
        test('should mark All as the pressed chip', () => {
            initialize(root);

            expect(pressed(root)).toEqual(['true', 'false', 'false']);
        });

        test('should filter as the visitor types', () => {
            initialize(root);

            type(root, 'davis');

            expect(visibleNames(root)).toEqual(['Tom Davis']);
        });

        test('should filter by category and mark only the chosen chip pressed', () => {
            initialize(root);
            const chips = [...root.querySelectorAll('[data-hof-category]')];

            chips[2].click();

            expect(visibleNames(root)).toEqual(['Tom Davis']);
            expect(pressed(root)).toEqual(['false', 'false', 'true']);

            chips[0].click();

            expect(visibleNames(root)).toHaveLength(3);
            expect(pressed(root)).toEqual(['true', 'false', 'false']);
        });

        test('should reset stale pressed states when it attaches', () => {
            root.querySelectorAll('[data-hof-category]')[2].setAttribute('aria-pressed', 'true');

            initialize(root);

            expect(pressed(root)).toEqual(['true', 'false', 'false']);
        });

        test('should not attach twice to the same root', () => {
            initialize(root);
            initialize(root);

            type(root, 'jane');

            expect(visibleNames(root)).toEqual(['Jane Smith']);
        });

        test('should ignore a missing root', () => {
            expect(() => initialize(null)).not.toThrow();
        });

        test('should stop filtering after dispose', () => {
            initialize(root);
            dispose(root);

            type(root, 'tom');

            expect(visibleNames(root)).toHaveLength(3);
        });

        test('should ignore dispose for a root that was never initialized', () => {
            expect(() => dispose(root)).not.toThrow();
        });
    });

    describe('refresh', () => {
        test('should apply the current search and category to cards the page replaced', () => {
            initialize(root);
            type(root, 'smith');
            root.querySelectorAll('[data-hof-category]')[1].click();
            expect(visibleNames(root)).toEqual(['Jane Smith']);

            // The page re-renders: a new card appears, and a patched one now shows a different bowler.
            const year = root.querySelector('#y2024');
            year.insertAdjacentHTML('beforeend', card('Ann Smithson', ['Superior Performance']));
            root.querySelector('[data-name="Jane Smith"]').dataset.name = 'Bob Jones';
            root.querySelector('[data-name="Bob Jones"]').hidden = false;

            refresh(root);

            expect(visibleNames(root)).toEqual(['Ann Smithson']);
            expect(status(root).textContent).toBe('Showing 1 of 4');
            expect(pressed(root)).toEqual(['false', 'true', 'false']);
        });

        test('should ignore a root that was never initialized', () => {
            expect(() => refresh(root)).not.toThrow();
        });
    });
});
