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

        test('rows do not toggle on wider screens', () => {
            enhanceTable(renderTable());
            setMobile(false);
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
