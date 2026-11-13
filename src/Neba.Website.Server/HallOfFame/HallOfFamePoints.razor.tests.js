import { describe, test, expect } from '@jest/globals';
import { buildIndex, applyFilter, initialize, dispose } from './HallOfFamePoints.razor.js';

const row = (name) => `<div data-pts-row data-name="${name}">${name}</div>`;

function render() {
    document.body.innerHTML = `
        <div id="root">
            <input data-pts-search type="search">
            <p data-pts-status role="status"></p>
            <section data-pts-section id="eligible">${row('Marcus Hale')}</section>
            <section data-pts-section id="below">${row('José Marchetti')}${row('Tom Davis')}</section>
        </div>`;
    return document.getElementById('root');
}

const visible = (root) => [...root.querySelectorAll('[data-pts-row]:not([hidden])')].map((r) => r.dataset.name);
const status = (root) => root.querySelector('[data-pts-status]');

describe('applyFilter', () => {
    test('shows every row and clears the status for an empty term', () => {
        const root = render();
        applyFilter(root, '');
        expect(visible(root)).toHaveLength(3);
        expect(status(root).textContent).toBe('');
    });

    test('matches without accents and ignores case', () => {
        const root = render();
        applyFilter(root, 'JOSE');
        expect(visible(root)).toEqual(['José Marchetti']);
        expect(status(root).textContent).toBe('Showing 1 of 3');
    });

    test('hides a section when none of its rows match', () => {
        const root = render();
        applyFilter(root, 'marcus');
        expect(root.querySelector('#eligible').hidden).toBe(false);
        expect(root.querySelector('#below').hidden).toBe(true);
    });

    test('says so when nothing matches', () => {
        const root = render();
        applyFilter(root, 'zzz');
        expect(visible(root)).toHaveLength(0);
        expect(status(root).textContent).toBe('No bowlers match your search.');
        expect(status(root).classList.contains('pts-status--empty')).toBe(true);
    });
});

describe('initialize and dispose', () => {
    test('filters as the user types', () => {
        const root = render();
        initialize(root);
        const input = root.querySelector('[data-pts-search]');
        input.value = 'tom';
        input.dispatchEvent(new Event('input'));
        expect(visible(root)).toEqual(['Tom Davis']);
    });

    test('stops filtering after dispose', () => {
        const root = render();
        initialize(root);
        dispose(root);
        const input = root.querySelector('[data-pts-search]');
        input.value = 'tom';
        input.dispatchEvent(new Event('input'));
        expect(visible(root)).toHaveLength(3);
    });

    test('indexes each row once', () => {
        expect(buildIndex(render())).toHaveLength(3);
    });
});
