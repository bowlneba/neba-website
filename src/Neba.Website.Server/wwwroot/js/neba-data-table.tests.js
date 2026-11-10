import { enhanceTable, enhanceAll, enhanceCollapsible, init, PRIMARY_COLUMNS } from './neba-data-table.js';

function setMobile(matches) {
    globalThis.matchMedia = jest.fn().mockReturnValue({
        matches,
        addEventListener: jest.fn()
    });
}

function renderTable(extra = '') {
    document.body.innerHTML = `
        <table class="neba-data-table">
            <thead><tr><th>Rank</th><th>Name</th><th>Points</th><th>Entries</th><th>$ Won</th></tr></thead>
            <tbody>
                <tr id="r1"><td>1st</td><td><a href="/b/1">Ann</a></td><td>10</td><td>3</td><td>$5</td></tr>
                <tr id="group"><td colspan="5">Group</td></tr>
                ${extra}
            </tbody>
        </table>`;
    return document.querySelector('table');
}

describe('neba-data-table', () => {
    beforeEach(() => setMobile(true));

    test('enhanceTable labels columns after the primary ones as secondary', () => {
        const table = renderTable();

        enhanceTable(table);

        const cells = document.querySelectorAll('#r1 td');
        expect(PRIMARY_COLUMNS).toBe(3);
        expect(cells[2].hasAttribute('data-secondary')).toBe(false);
        expect(cells[3].hasAttribute('data-secondary')).toBe(true);
        expect(cells[3].dataset.label).toBe('Entries');
        expect(cells[4].dataset.label).toBe('$ Won');
        expect(document.querySelectorAll('thead th[data-secondary]')).toHaveLength(2);
    });

    test('enhanceTable skips rows whose cell count does not match the header', () => {
        enhanceTable(renderTable());

        expect(document.getElementById('group').hasAttribute('data-expandable')).toBe(false);
        expect(document.getElementById('r1').hasAttribute('data-expandable')).toBe(true);
    });

    test('enhanceTable leaves tables with three or fewer columns alone', () => {
        document.body.innerHTML = `<table class="neba-data-table"><thead><tr><th>A</th><th>B</th><th>C</th></tr></thead>
            <tbody><tr><td>1</td><td>2</td><td>3</td></tr></tbody></table>`;

        enhanceAll();

        expect(document.querySelector('tr[data-expandable]')).toBeNull();
    });

    test('enhanceTable makes rows focusable on mobile only', () => {
        const table = renderTable();

        enhanceTable(table);
        expect(document.getElementById('r1').tabIndex).toBe(0);

        setMobile(false);
        enhanceTable(table);
        expect(document.getElementById('r1').hasAttribute('tabindex')).toBe(false);
    });

    test('enhanceTable clears earlier markup when the header drops to the primary columns', () => {
        const table = renderTable();
        enhanceTable(table);
        document.getElementById('r1').setAttribute('data-open', '');

        table.querySelector('thead tr').innerHTML = '<th>Rank</th><th>Name</th><th>Points</th>';
        enhanceTable(table);

        expect(table.querySelector('[data-secondary]')).toBeNull();
        expect(table.querySelector('[data-label]')).toBeNull();
        expect(table.querySelector('[data-expandable]')).toBeNull();
        expect(document.getElementById('r1').hasAttribute('data-open')).toBe(false);
        expect(document.getElementById('r1').hasAttribute('tabindex')).toBe(false);
    });

    test('enhanceTable closes open rows on wider screens', () => {
        const table = renderTable();
        enhanceTable(table);
        document.getElementById('r1').setAttribute('data-open', '');

        setMobile(false);
        enhanceTable(table);

        expect(document.getElementById('r1').hasAttribute('data-open')).toBe(false);
    });

    describe('interaction', () => {
        beforeAll(() => {
            globalThis.requestAnimationFrame = cb => cb();
            init();
        });

        test('tapping a row opens it and tapping again closes it', () => {
            enhanceTable(renderTable());
            const row = document.getElementById('r1');

            row.querySelector('td:nth-child(3)').click();
            expect(row.hasAttribute('data-open')).toBe(true);
            expect(row.getAttribute('aria-expanded')).toBe('true');

            row.querySelector('td:nth-child(3)').click();
            expect(row.hasAttribute('data-open')).toBe(false);
            expect(row.getAttribute('aria-expanded')).toBe('false');
        });

        test('tapping a link inside the row does not toggle it', () => {
            enhanceTable(renderTable());
            const row = document.getElementById('r1');

            row.querySelector('a').addEventListener('click', e => e.preventDefault());
            row.querySelector('a').click();

            expect(row.hasAttribute('data-open')).toBe(false);
        });

        test('Enter toggles a focused row', () => {
            enhanceTable(renderTable());
            const row = document.getElementById('r1');

            row.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));

            expect(row.hasAttribute('data-open')).toBe(true);
        });

        test('the observer enhances a table added after load', async () => {
            const holder = document.createElement('div');
            holder.innerHTML = renderTable().outerHTML;
            document.body.innerHTML = '';

            document.body.appendChild(holder);
            await Promise.resolve();

            expect(holder.querySelector('td[data-secondary]')).not.toBeNull();
        });

        test('the observer enhances rows added to an existing table', async () => {
            const table = renderTable();
            enhanceTable(table);
            await Promise.resolve();

            const row = table.querySelector('tbody').appendChild(document.createElement('tr'));
            row.id = 'late';
            row.innerHTML = '<td>2nd</td><td>Bo</td><td>8</td><td>2</td><td>$1</td>';
            await Promise.resolve();

            expect(row.hasAttribute('data-expandable')).toBe(true);
            expect(row.querySelector('td[data-secondary]').dataset.label).toBe('Entries');
        });

        test('a large table built in the live DOM is enhanced once, not once per added node', async () => {
            // Blazor attaches each element before it builds the children, so every row and cell is an added node.
            const table = renderTable();
            const body = table.querySelector('tbody');
            const headerReads = jest.spyOn(HTMLTableElement.prototype, 'querySelectorAll');
            await Promise.resolve();
            headerReads.mockClear();

            for (let i = 0; i < 100; i++) {
                const row = body.appendChild(document.createElement('tr'));
                for (const text of ['1st', 'Ann', '10', '3', '$5']) {
                    row.appendChild(document.createElement('td')).textContent = text;
                }
            }
            await Promise.resolve();

            const enhancements = headerReads.mock.calls.filter(([selector]) => selector === 'thead th').length;
            headerReads.mockRestore();
            expect(enhancements).toBe(1);
            expect(body.querySelectorAll('tr[data-expandable]')).toHaveLength(101);
        });

        test('rows do not toggle on wider screens', () => {
            const table = renderTable();
            enhanceTable(table);
            setMobile(false);
            enhanceTable(table);   // the media-query change listener does this in the page
            const row = document.getElementById('r1');

            row.querySelector('td:nth-child(3)').click();

            expect(row.hasAttribute('data-open')).toBe(false);
        });
    });

    describe('collapsible sections', () => {
        function renderSection() {
            document.body.innerHTML = `
                <section class="neba-collapsible">
                    <div class="neba-collapsible-header"><h2>Title</h2><button id="action">Action</button></div>
                    <div id="body">Body</div>
                </section>`;
            return document.querySelector('.neba-collapsible-header');
        }

        test('enhanceCollapsible makes the header a button on mobile only', () => {
            const header = renderSection();

            enhanceCollapsible(header);
            expect(header.getAttribute('role')).toBe('button');
            expect(header.getAttribute('aria-expanded')).toBe('true');

            setMobile(false);
            enhanceCollapsible(header);
            expect(header.hasAttribute('role')).toBe(false);
        });

        test('tapping the header collapses and expands the section', () => {
            const header = renderSection();
            enhanceAll();
            const section = header.parentElement;

            header.querySelector('h2').click();
            expect(section.hasAttribute('data-collapsed')).toBe(true);
            expect(header.getAttribute('aria-expanded')).toBe('false');

            header.querySelector('h2').click();
            expect(section.hasAttribute('data-collapsed')).toBe(false);
        });

        test('a collapsed section expands on wider screens', () => {
            const header = renderSection();
            enhanceAll();
            header.querySelector('h2').click();

            setMobile(false);
            enhanceCollapsible(header);

            expect(header.parentElement.hasAttribute('data-collapsed')).toBe(false);
        });

        test('a button inside the header does not toggle the section', () => {
            renderSection();
            enhanceAll();

            document.getElementById('action').click();

            expect(document.querySelector('.neba-collapsible').hasAttribute('data-collapsed')).toBe(false);
        });
    });
});

function renderInContainer(width) {
    document.body.innerHTML = `<div data-table-container>${renderTable().outerHTML}</div>`;
    const container = document.querySelector('[data-table-container]');
    Object.defineProperty(container, 'clientWidth', { configurable: true, value: width });
    return { container, table: container.querySelector('table') };
}

describe('table container', () => {
    beforeEach(() => setMobile(false));

    test('a narrow container makes the table static even on a wide screen', () => {
        const { table } = renderInContainer(700);

        enhanceTable(table);

        expect(table.dataset.compact).toBe('static');
        expect(table.querySelector('tbody tr#r1').hasAttribute('tabindex')).toBe(false);
    });

    test('a wide container shows all columns', () => {
        const { table } = renderInContainer(960);

        enhanceTable(table);

        expect(table.dataset.compact).toBeUndefined();
    });

    test('a static table stays static until the container clears the release width', () => {
        const { container, table } = renderInContainer(700);
        enhanceTable(table);

        Object.defineProperty(container, 'clientWidth', { configurable: true, value: 815 });
        enhanceTable(table);
        expect(table.dataset.compact).toBe('static');

        Object.defineProperty(container, 'clientWidth', { configurable: true, value: 830 });
        enhanceTable(table);
        expect(table.dataset.compact).toBeUndefined();
    });

    test('a full table switches to static only below the compact width', () => {
        const { container, table } = renderInContainer(960);
        enhanceTable(table);

        Object.defineProperty(container, 'clientWidth', { configurable: true, value: 815 });
        enhanceTable(table);
        expect(table.dataset.compact).toBeUndefined();

        Object.defineProperty(container, 'clientWidth', { configurable: true, value: 799 });
        enhanceTable(table);
        expect(table.dataset.compact).toBe('static');
    });

    test('a container on a phone is static', () => {
        setMobile(true);
        const { table } = renderInContainer(960);

        enhanceTable(table);

        expect(table.dataset.compact).toBe('static');
    });

    test('rows in a static table do not toggle on click', () => {
        init();
        const { table } = renderInContainer(700);
        enhanceTable(table);
        const row = table.querySelector('#r1');

        row.querySelector('td:nth-child(3)').click();

        expect(row.hasAttribute('data-open')).toBe(false);
    });

    test('resizing the container re-evaluates the table', () => {
        let notify;
        globalThis.ResizeObserver = jest.fn(cb => {
            notify = cb;
            return { observe: jest.fn(), unobserve: jest.fn() };
        });
        const { container, table } = renderInContainer(960);
        enhanceTable(table);
        expect(table.dataset.compact).toBeUndefined();

        Object.defineProperty(container, 'clientWidth', { configurable: true, value: 700 });
        notify([{ target: container }]);

        expect(table.dataset.compact).toBe('static');
        delete globalThis.ResizeObserver;
    });
});

test('a page grid on a phone is expandable and still toggles', () => {
    setMobile(true);
    init();
    const table = renderTable();
    enhanceTable(table);

    document.getElementById('r1').querySelector('td:nth-child(3)').click();

    expect(table.dataset.compact).toBe('expandable');
    expect(document.getElementById('r1').hasAttribute('data-open')).toBe(true);
});
