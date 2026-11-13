import { describe, test, expect, jest } from '@jest/globals';
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

describe('first search with rows behind Show more', () => {
    const tick = () => new Promise((resolve) => setTimeout(resolve, 0));

    function renderPartial(complete) {
        const root = render();
        root.dataset.ptsComplete = complete ? 'true' : 'false';
        return root;
    }

    function type(root, value) {
        const input = root.querySelector('[data-pts-search]');
        input.value = value;
        input.dispatchEvent(new Event('input'));
    }

    test('asks the page to show every row once, instead of filtering a partial list', () => {
        const root = renderPartial(false);
        const dotNet = { invokeMethodAsync: jest.fn().mockResolvedValue(undefined) };
        initialize(root, dotNet);

        type(root, 'tom');

        expect(dotNet.invokeMethodAsync).toHaveBeenCalledWith('ExpandForSearch');
        expect(visible(root)).toHaveLength(3);

        type(root, 'tomm');

        expect(dotNet.invokeMethodAsync).toHaveBeenCalledTimes(1);
    });

    test('filters directly when every row is already shown', () => {
        const root = renderPartial(true);
        const dotNet = { invokeMethodAsync: jest.fn() };
        initialize(root, dotNet);

        type(root, 'tom');

        expect(dotNet.invokeMethodAsync).not.toHaveBeenCalled();
        expect(visible(root)).toEqual(['Tom Davis']);
    });

    test('does not ask to expand for an empty search', () => {
        const root = renderPartial(false);
        const dotNet = { invokeMethodAsync: jest.fn() };
        initialize(root, dotNet);

        type(root, '   ');

        expect(dotNet.invokeMethodAsync).not.toHaveBeenCalled();
    });

    test('asks again after a failed expand', async () => {
        const root = renderPartial(false);
        const dotNet = { invokeMethodAsync: jest.fn().mockRejectedValue(new Error('disconnected')) };
        initialize(root, dotNet);

        type(root, 'tom');
        await tick();
        type(root, 'tom');

        expect(dotNet.invokeMethodAsync).toHaveBeenCalledTimes(2);
    });

    test('filters rows the page adds, without a second call', async () => {
        const root = renderPartial(false);
        const dotNet = { invokeMethodAsync: jest.fn().mockResolvedValue(undefined) };
        initialize(root, dotNet);
        type(root, 'tom');

        root.querySelector('#below').insertAdjacentHTML('beforeend', row('Tomas Reyes') + row('Ann Lee'));
        await tick();

        expect(visible(root)).toEqual(['Tom Davis', 'Tomas Reyes']);
    });

    test('stops watching for rows after dispose', async () => {
        const root = renderPartial(false);
        initialize(root, null);
        type(root, 'tom');
        dispose(root);

        root.querySelector('#below').insertAdjacentHTML('beforeend', row('Ann Lee'));
        await tick();

        expect(visible(root)).toContain('Ann Lee');
    });
});
