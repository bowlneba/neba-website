import { describe, test, expect, beforeEach } from '@jest/globals';
import { normalize, matches, applyFilter, initialize, dispose } from './HallOfFame.razor.js';

const card = (name, categories) =>
    `<div data-hof-card data-name="${name}" data-categories="${categories.join('|')}">${name}</div>`;

function render() {
    document.body.innerHTML = `
        <div id="root">
            <input data-hof-search type="search">
            <button data-hof-category="" aria-pressed="true">All</button>
            <button data-hof-category="Superior Performance" aria-pressed="false">Superior Performance</button>
            <button data-hof-category="Friend of NEBA" aria-pressed="false">Friend of NEBA</button>
            <p data-hof-status></p>
            <p data-hof-empty hidden>No match</p>
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

    describe('matches', () => {
        test('should match a name fragment ignoring case and accents', () => {
            const c = root.querySelector('[data-name="José Marchetti"]');
            expect(matches(c, 'jose', '')).toBe(true);
            expect(matches(c, 'march', '')).toBe(true);
            expect(matches(c, 'smith', '')).toBe(false);
        });

        test('should require the chosen category exactly', () => {
            const c = root.querySelector('[data-name="José Marchetti"]');
            expect(matches(c, '', 'Meritorious Service')).toBe(true);
            expect(matches(c, '', 'Friend of NEBA')).toBe(false);
        });
    });

    describe('applyFilter', () => {
        test('should show everyone and clear the status when nothing is filtered', () => {
            applyFilter(root, '', '');

            expect(visibleNames(root)).toHaveLength(3);
            expect(root.querySelector('[data-hof-status]').textContent).toBe('');
            expect(root.querySelector('#y2024 [data-hof-shown]').textContent).toBe('');
        });

        test('should hide non-matching cards, hide empty classes and report counts', () => {
            applyFilter(root, 'tom', '');

            expect(visibleNames(root)).toEqual(['Tom Davis']);
            expect(root.querySelector('#y2024').hidden).toBe(true);
            expect(root.querySelector('#y2017').hidden).toBe(false);
            expect(root.querySelector('#y2017 [data-hof-shown]').textContent).toBe('1 shown');
            expect(root.querySelector('[data-hof-status]').textContent).toBe('Showing 1 of 3');
        });

        test('should combine the name and category filters', () => {
            applyFilter(root, 'jane', 'Superior Performance');
            expect(visibleNames(root)).toEqual(['Jane Smith']);

            applyFilter(root, 'tom', 'Superior Performance');
            expect(visibleNames(root)).toEqual([]);
        });

        test('should show the empty message and hide the stage when nothing matches', () => {
            applyFilter(root, 'zzz', '');

            expect(root.querySelector('[data-hof-empty]').hidden).toBe(false);
            expect(root.querySelector('[data-hof-stage]').hidden).toBe(true);
            expect(root.querySelector('[data-hof-status]').textContent).toBe('');
        });

        test('should bring everything back when the filters clear', () => {
            applyFilter(root, 'zzz', '');
            applyFilter(root, '', '');

            expect(visibleNames(root)).toHaveLength(3);
            expect(root.querySelector('[data-hof-empty]').hidden).toBe(true);
            expect(root.querySelector('[data-hof-stage]').hidden).toBe(false);
        });
    });

    describe('initialize', () => {
        test('should filter as the visitor types', () => {
            initialize(root);
            const input = root.querySelector('[data-hof-search]');

            input.value = 'davis';
            input.dispatchEvent(new Event('input'));

            expect(visibleNames(root)).toEqual(['Tom Davis']);
        });

        test('should filter by category and mark only the chosen chip pressed', () => {
            initialize(root);
            const chips = [...root.querySelectorAll('[data-hof-category]')];

            chips[2].click();

            expect(visibleNames(root)).toEqual(['Tom Davis']);
            expect(chips.map((c) => c.getAttribute('aria-pressed'))).toEqual(['false', 'false', 'true']);

            chips[0].click();

            expect(visibleNames(root)).toHaveLength(3);
            expect(chips.map((c) => c.getAttribute('aria-pressed'))).toEqual(['true', 'false', 'false']);
        });

        test('should not attach twice to the same root', () => {
            initialize(root);
            initialize(root);
            const input = root.querySelector('[data-hof-search]');

            input.value = 'jane';
            input.dispatchEvent(new Event('input'));

            expect(visibleNames(root)).toEqual(['Jane Smith']);
        });

        test('should ignore a missing root', () => {
            expect(() => initialize(null)).not.toThrow();
        });

        test('should stop filtering after dispose', () => {
            initialize(root);
            dispose(root);
            const input = root.querySelector('[data-hof-search]');

            input.value = 'tom';
            input.dispatchEvent(new Event('input'));

            expect(visibleNames(root)).toHaveLength(3);
        });

        test('should ignore dispose for a root that was never initialized', () => {
            expect(() => dispose(root)).not.toThrow();
        });
    });
});
