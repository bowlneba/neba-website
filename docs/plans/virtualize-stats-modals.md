# Virtualize Stats Modals

The ten "Show all" full-standings modals on `/stats` render every row at once. Render only the rows near the viewport with Blazor `Virtualize`, and make the eight-column Bowler of the Year table fit the modal without side scroll. Plan for release 1.2.0, from issue #238 (items 1 and 2).

## Decisions locked in during scoping

- **Scope:** items 1 and 2 of the 1.2.0 plan in #238. Item 3 (move the 10.0.13 packages to 10.0.14) is already done.
- **Not critical:** read-only public page, no money or PII. The NFR checkpoint is skipped.
- **UI only:** the modal makes no API call; the rows are already in memory. No API, contract, or database change.
- **Permissions:** `/stats` stays anonymous. No new policy or role.
- **Virtualize, not paging:** `Virtualize` with `SpacerElement="tr"` inside each table's `<tbody>`. The modal body (`.stats-modal-body`) is the scroll container.
- **Row height:** `Virtualize` needs every row the same height (Microsoft docs). So inside the modals there is no tap-to-expand: in the collapsed layout each row has one fixed height and shows all its stats. `ItemSize` is set per table from that height. The page's top-10 grids keep tap-to-expand (they are not virtualized).
- **Refactor depth:** keep the ten `Render*Table` fragments in `SeasonStats.razor`. Swap each `@foreach` for `Virtualize`. No new generic table component.
- **Modal alignment:** the phone screenshot showed the modal at 90% size, top left. A global `.neba-modal-content` rule in `neba_theme.css` (`max-width: 90vw; max-height: 90vh`) fights the component's CSS. Fixed first, in `NebaModal.razor.css`, for every modal.
- **Base:** merge `origin/main` (1.1.4 hotfix) into this branch before the JS work.
- **Side scroll:** seen at 768-1000px, where the 720px modal is wider than the phone breakpoint. The modal-body table collapses by modal width, not only by viewport width. The standings modal max width goes from 720px to 960px, so desktop and tablet keep all columns and only narrow modals collapse.

## Flow (confirmed)

1. `/stats` shows ten grids, each with the top 10 rows and a "Show all N" button (unchanged).
2. "Show all N" opens the modal at once, from memory.
3. The modal renders only rows near the viewport plus a buffer.
4. Scrolling swaps rows in and out; spacer rows keep the scrollbar size right.
5. In a narrow modal, each row shows rank, name, and key value, with the other stats below, three to a line. No tapping needed.
6. Clicking a name closes the modal and opens the bowler page (unchanged).
7. A wide enough modal shows all columns; a narrow one uses the rank / name / key value layout.

## Phase 1: API

No API change.

- **Domain / Application / Infrastructure / API / Contracts:** none. The stats query already returns every row, and the page holds them in memory. Row counts are small (about 560 at most), so a server cap or page adds work and gains nothing.
- **Tests:** none for this phase.
- **Test factories:** none.
- **Deferred to Phase 2:** all rendering, scrolling, and layout changes.

## Phase 2: UI

No new pages or routes, no `DirtyFormGuard` (no data entry), no FAB. `SeasonStats.razor` keeps its `<PageTitle>` and `InteractiveServer` render mode (prerender off). No Refit change: `IStatsApiService` is untouched.

**Base:** this branch (`statsGridPaging`) does not have the 1.1.4 hotfix (#239, `collectAround` in `neba-data-table.js`). Merge `origin/main` first; every JS change below builds on the hotfix version of the file.

### Modal alignment (do first)

Cause, found from the phone screenshot: the box is exactly 90% of the viewport in both directions, pinned top left. `wwwroot/neba_theme.css` (line 1007) has a global `.neba-modal-content { max-width: 90vw; max-height: 90vh; ... }`. `NebaModal` is the only user of that class. The component's own CSS sizes the box through `.neba-modal-container` (full screen on phones, `max-width` inline), so the global caps shrink the content below its container, and flex leaves it at the top left. With the new 960px width the same caps would shrink the box on any screen under about 1070px.

`Components/NebaModal.razor.css`:

```css
.neba-modal-content {
    background: white;
    border-radius: 0.75rem;
    box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
    display: flex;
    flex-direction: column;
    height: 100%;
    /* neba_theme.css also caps .neba-modal-content at 90vw x 90vh. The container sets the size,
       so lift the caps or the box shrinks and sits at the top left. */
    max-width: none;
    max-height: none;
    pointer-events: auto;
}
```

The scoped selector (`.neba-modal-content[b-...]`) outranks the global one. Effect on every modal in the app: on phones (640px and under) the box now fills the screen, as the component's phone rules already intend. The global rule's `padding: 1.5rem` stays; changing it is out of scope.

### Pages / Components

`Stats/SeasonStats.razor`: constants and a helper next to `ModalMaxWidth`.

```csharp
// Virtualize measures real row heights after the first render, so this is only the opening estimate.
private const float StandingsRowHeight = 41f;
private const int StandingsOverscan = 8;

private string ModalMaxWidth => _modalWide ? "1120px" : "960px";

// Items needs ICollection<T>. The view-model lists are collection expressions, which implement it, so the
// same instance comes back on every render and Virtualize does not refetch.
private static ICollection<T> AsCollection<T>(IReadOnlyCollection<T> rows) => rows as ICollection<T> ?? [.. rows];
```

The modal body gets the container marker (`data-table-container`):

```razor
<NebaModal IsOpen="@_modalOpen" OnClose="CloseModal" Title="@_modalTitle" MaxWidth="@ModalMaxWidth">
    <div class="stats-modal-body stats-table-wrapper" data-table-container>
        @_modalContent
    </div>
</NebaModal>
```

`RenderBoyStandingsTable`: the `@foreach` becomes `Virtualize`. The other nine fragments get the same swap; their `<tr>` markup does not change.

```razor
<tbody>
    <Virtualize Context="row" Items="@AsCollection(rows)" ItemSize="@StandingsRowHeight"
                OverscanCount="@StandingsOverscan" SpacerElement="tr">
        <tr class="@(row.Rank == 1 ? "stats-row-first" : "stats-row")">
            <td class="@(row.Rank == 1 ? "rank-cell rank-cell-accent-blue-500" : "rank-cell")">@row.Rank.ToOrdinal()</td>
            <td><a href="@GetIndividualStatsUrl(row.BowlerId)" class="stats-name-link" @onclick="CloseModal">@row.BowlerName</a></td>
            <td class="text-right tabular-nums">@row.Points</td>
            <td class="text-right tabular-nums">@row.Tournaments</td>
            <td class="text-right tabular-nums">@row.Entries</td>
            <td class="text-right tabular-nums">@row.Finals</td>
            <td class="text-right tabular-nums">@(row.AverageFinish?.ToString("F1") ?? "—")</td>
            <td class="text-right tabular-nums" aria-label="@($"Winnings: {row.Winnings.ToString("C0", CurrencyCulture)}")">@row.Winnings.ToString("C0", CurrencyCulture)</td>
        </tr>
    </Virtualize>
</tbody>
```

The other nine: `RenderHighAverageTable`, `RenderHighBlockTable`, `RenderMatchPlayAverageTable`, `RenderMatchPlayRecordTable`, `RenderMatchPlayAppearancesTable`, `RenderPointsPerEntryTable`, `RenderPointsPerTournamentTable`, `RenderFinalsPerEntryTable`, `RenderAverageFinishTable`.

The page's top-10 grids keep `rows.Take(displayCount)` with `@foreach`.

### Responsive table

`wwwroot/js/neba-data-table.js` (on top of the hotfix version):

```js
export const MOBILE_QUERY = '(max-width: 767px)';
export const PRIMARY_COLUMNS = 3;
export const COMPACT_CONTAINER_WIDTH = 800;   // measured: the eight-column table needs about this much
const CONTAINER_SELECTOR = '[data-table-container]';

const isMobile = () => globalThis.matchMedia?.(MOBILE_QUERY).matches ?? false;

/**
 * 'expandable': a page grid on a phone (rows open on tap).
 * 'static': a table in a table container that is narrow or on a phone (rows show every stat, no tap).
 * null: show all columns.
 * @param {HTMLTableElement} table
 */
export function compactMode(table) {
    const container = table.closest(CONTAINER_SELECTOR);
    if (!container) {
        return isMobile() ? 'expandable' : null;
    }

    const narrow = container.clientWidth > 0 && container.clientWidth < COMPACT_CONTAINER_WIDTH;
    return narrow || isMobile() ? 'static' : null;
}

let resizeObserver;
const watched = new WeakSet();

/** Re-enhance a container's tables when its width changes (the modal opens, or the window resizes). */
function watchContainer(table) {
    const container = table.closest(CONTAINER_SELECTOR);
    if (!container || watched.has(container) || typeof ResizeObserver !== 'function') {
        return;
    }

    watched.add(container);
    resizeObserver ??= new ResizeObserver(entries => {
        for (const { target } of entries) {
            if (!target.isConnected) {
                resizeObserver.unobserve(target);
                continue;
            }

            target.querySelectorAll('table.neba-data-table').forEach(enhanceTable);
        }
    });
    resizeObserver.observe(container);
}
```

`resetTable` also clears the mode (`delete table.dataset.compact;`). `enhanceTable` changes from `const mobile = isMobile();` to:

```js
    const mode = compactMode(table);
    if (mode) {
        table.dataset.compact = mode;
    } else {
        delete table.dataset.compact;
    }

    watchContainer(table);

    for (const row of table.querySelectorAll('tbody tr')) {
        const cells = Array.from(row.children).filter(c => c.tagName === 'TD');
        if (cells.length !== headers.length) {
            continue;   // the Virtualize spacer rows have no cells, so they fall out here
        }

        cells.forEach((cell, i) => {
            if (i >= PRIMARY_COLUMNS) {
                cell.dataset.secondary = '';
                cell.dataset.label = headers[i].textContent.trim();
            }
        });

        row.dataset.expandable = '';

        if (mode === 'expandable') {
            row.tabIndex = 0;
            row.setAttribute('aria-expanded', row.hasAttribute('data-open') ? 'true' : 'false');
        } else {
            row.removeAttribute('tabindex');
            row.removeAttribute('aria-expanded');
            row.removeAttribute('data-open');
        }
    }
```

Click and key handlers toggle only `expandable` tables; section headers stay on the phone query:

```js
const EXPANDABLE_ROW = 'table.neba-data-table[data-compact="expandable"] tbody tr[data-expandable]';

function handleClick(event) {
    if (!(event.target instanceof Element)) {
        return;
    }

    // Let links and controls inside a row do their own job
    if (event.target.closest('a, button, input, select, textarea, label')) {
        return;
    }

    const header = event.target.closest('.neba-collapsible > .neba-collapsible-header');
    if (header) {
        if (isMobile()) {
            toggleSection(header);
        }

        return;
    }

    const row = event.target.closest(EXPANDABLE_ROW);
    if (row) {
        toggleRow(row);
    }
}

function handleKeydown(event) {
    if ((event.key !== 'Enter' && event.key !== ' ') || !(event.target instanceof Element)) {
        return;
    }

    if (isMobile() && event.target.matches('.neba-collapsible > .neba-collapsible-header')) {
        event.preventDefault();
        toggleSection(event.target);
    } else if (event.target.matches(EXPANDABLE_ROW)) {
        event.preventDefault();
        toggleRow(event.target);
    }
}
```

`wwwroot/neba-data-table.css`:
- Wrap target: every selector inside the `@media (max-width: 767px)` data-table block that starts `table.neba-data-table` becomes `table.neba-data-table[data-compact="expandable"]`, and the `@media` line goes. The collapsible-section block keeps its media query. The reduced-motion block drops the media-feature `max-width` part and selects the same `[data-compact="expandable"]` chevron.
- New block for the modal layout:

```css
/* Static compact layout (tables in a narrow container, such as the standings modal).
   Every row has one fixed shape, because Virtualize needs equal row heights:
   line 1 is rank, name, key value; the other stats follow, three to a line, label above value. */
table.neba-data-table[data-compact="static"],
table.neba-data-table[data-compact="static"] thead,
table.neba-data-table[data-compact="static"] tbody {
    display: block;
}

table.neba-data-table[data-compact="static"] thead tr,
table.neba-data-table[data-compact="static"] tbody tr[data-expandable] {
    display: grid;
    grid-template-columns: 3rem repeat(6, minmax(0, 1fr));
}

table.neba-data-table[data-compact="static"] tbody tr[data-expandable] {
    align-content: start;
    padding: 0.5rem 0;
    background: #f9fafb;
    border-bottom: 1px solid #e5e7eb;
}

table.neba-data-table[data-compact="static"] :is(thead th, tbody td):nth-child(1) { grid-column: 1; }
table.neba-data-table[data-compact="static"] :is(thead th, tbody td):nth-child(2) { grid-column: 2 / 6; }
table.neba-data-table[data-compact="static"] :is(thead th, tbody td):nth-child(3) { grid-column: 6 / 8; text-align: right; }

table.neba-data-table[data-compact="static"] thead th[data-secondary] { display: none; }

table.neba-data-table[data-compact="static"] tbody tr[data-expandable] > td {
    border: 0;
    padding: 0.25rem 0.5rem;
}

table.neba-data-table[data-compact="static"] tbody tr[data-expandable] > td[data-secondary] {
    display: flex;
    flex-direction: column;
    grid-column: span 2;
    gap: 0.125rem;
    padding-top: 0.5rem;
    text-align: left;
    font-variant-numeric: tabular-nums;
}

/* The first extra stat starts under the name */
table.neba-data-table[data-compact="static"] tbody tr[data-expandable] > td[data-secondary]:nth-child(4) {
    grid-column: 2 / span 2;
}

table.neba-data-table[data-compact="static"] tbody tr[data-expandable] > td[data-secondary]::before {
    content: attr(data-label);
    font-size: 0.6875rem;
    font-weight: 600;
    letter-spacing: 0.05em;
    color: #6b7280;
}
```

(The mockup used one fixed `height` per row for the eight-column table; the real CSS needs none, since the secondary count fixes the row shape per table. Check in a browser that rows within one table stay equal height.)

`Stats/SeasonStats.razor.css`: `.stats-modal-body` stays the only vertical scroller (`max-height: 70vh; overflow-y: auto`). Keep `overflow-x: auto` as a last-resort guard.

Browser checks before the code is final:
- `Virtualize` documents `block` and `table-row` row displays; the static layout uses `display: grid` on rows. Confirm the spacer rows and scroll position hold. If not, make the row `display: block` and place its cells another way.
- `.neba-modal-body` must not scroll as well as `.stats-modal-body`.
- No unstyled flash on phones: the page does not prerender, so no table exists before the script runs.
- Set `COMPACT_CONTAINER_WIDTH` from the measured width of the eight-column table.

### Tests

Jest, `wwwroot/js/neba-data-table.tests.js` (new cases; the hotfix pass-count test stays):

```js
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

    test('rows in a static table do not toggle on click', () => {
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
    });
});

test('a page grid on a phone is expandable and still toggles', () => {
    setMobile(true);
    const table = renderTable();
    enhanceTable(table);

    document.getElementById('r1').querySelector('td:nth-child(3)').click();

    expect(table.dataset.compact).toBe('expandable');
    expect(document.getElementById('r1').hasAttribute('data-open')).toBe(true);
});
```

bUnit, `tests/Neba.Website.Tests/Stats/SeasonStatsTests.cs`:

```csharp
[Fact(DisplayName = "Should render only a window of rows in the standings modal when the standings are long")]
public async Task Modal_ShouldRenderWindowOfRows_WhenStandingsAreLong()
{
    // Arrange
    const int rowCount = 300;
    var model = CreateStatsModel(firstRowName: "Alpha Bowler") with
    {
        HighAverage = HighAverageRowViewModelFactory.Bogus(rowCount)
    };
    _statsApi.EnqueueResult(model);
    var cut = _ctx.Render<SeasonStatsPage>();
    await cut.Find("#tab-qualifying").ClickAsync(new());

    // Act
    await cut.Find("[aria-label^='Show all'][aria-label*='High Average']").ClickAsync(new());

    // Assert
    cut.Find("[data-table-container]").ShouldNotBeNull();
    cut.FindAll("[data-table-container] tbody tr").Count.ShouldBeLessThan(rowCount);
}
```

(Check first how bUnit renders `Virtualize` with loose JS interop. The assertion above holds either way; tighten it if bUnit renders an initial window.)

Playwright, `tests/e2e/Stats.spec.ts`. The mock API gets a 560-entry Bowler of the Year list for the current season in `mock-api/mock-api-server.ts` (read the file first; copy every `required` field of the contract, as the Required-field note in memory warns).

```ts
test.describe('Stats - full standings modal', () => {
  const showAll = (page: Page) =>
    page.getByRole('button', { name: /Show all 560 Bowler of the Year standings/ });
  const dataRows = (page: Page) => page.locator('.stats-modal-body tbody tr.stats-row, .stats-modal-body tbody tr.stats-row-first');

  test('renders a window of rows and reaches the last row', async ({ page }) => {
    await page.setViewportSize({ width: 1100, height: 900 });
    await page.goto(`/stats?season=${CURRENT_SEASON_YEAR}`);
    await showAll(page).click();

    expect(await dataRows(page).count()).toBeLessThan(100);
    await page.locator('.stats-modal-body').evaluate(el => el.scrollTo(0, el.scrollHeight));
    await expect(page.locator('.stats-modal-body').getByText('Bowler 560')).toBeVisible();
  });

  test('shows every column without side scroll', async ({ page }) => {
    for (const width of [1100, 900, 390]) {
      await page.setViewportSize({ width, height: 900 });
      await page.goto(`/stats?season=${CURRENT_SEASON_YEAR}`);
      await showAll(page).click();

      const body = page.locator('.stats-modal-body');
      await expect(body).toBeVisible();
      expect(await body.evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true);
      await expect(page.locator('.stats-modal-body').getByText('$ Won').first()).toBeVisible();
      await page.locator('.neba-modal-close').click();
    }
  });

  test('the modal fills the screen on a phone', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(`/stats?season=${CURRENT_SEASON_YEAR}`);
    await showAll(page).click();

    const box = await page.locator('.neba-modal-content').boundingBox();
    expect(box?.x).toBe(0);
    expect(box?.width).toBe(390);
  });
});
```

(`Page` comes from `@playwright/test`. At 390px "$ Won" shows as the cell label, so the text check covers the phone case.)

### Mockups

- `docs/plans/mockups/virtualize-stats-modals/boy-standings-modal.html`: the Bowler of the Year full-standings modal with 560 sample rows. Buttons switch the screen (desktop 1100, tablet 900, phone 390) and the modal max width (960 chosen, or 720 old). It shows the collapsed fixed-height layout and how few rows exist in the table at once. One mockup only, because the layout is a small change to the existing phone design.
- **Decided:** the modal max width is 960px. The mockup defaults to it; the 720 button shows the old width, where the table collapsed on every screen.
- The collapsed rows are fixed height with all stats visible (no tap-to-expand), because `Virtualize` needs equal row heights.

### Out of scope

- A generic `StandingsTable<TRow>` component (the ten fragments stay).
- Paging, "Show more" buttons, and any API change.
- The Points Race modal (it is a chart, not a table).

## Commit plan

Each commit builds and passes its own tests. Stage the listed files by name. Every message ends with `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`.

1. **Add plan for virtualized stats modals**
   - Files: `docs/plans/virtualize-stats-modals.md`, `docs/plans/mockups/virtualize-stats-modals/boy-standings-modal.html`
   - Why: the plan and its mockup travel with the work they describe.

2. **Merge main into statsGridPaging** (a merge, not a rewrite; resolve `Directory.Build.props` to 1.2.0)
   - Files: none of ours; brings in the 1.1.4 hotfix (#239) and its `neba-data-table.js` change.
   - Why: every JS change below builds on `collectAround`.

3. **Stop global modal caps from shrinking the modal**
   - Files: `src/Neba.Website.Server/Components/NebaModal.razor.css`, `tests/e2e/mock-api/mock-api-server.ts` (560-entry Bowler of the Year list for the current season), `tests/e2e/Stats.spec.ts` (modal fills the screen on a phone).
   - Why: the 90vw by 90vh caps in `neba_theme.css` pin the box top left; this fixes every modal and gives the later e2e tests their data.

4. **Collapse data tables by container width**
   - Files: `src/Neba.Website.Server/wwwroot/js/neba-data-table.js`, `src/Neba.Website.Server/wwwroot/neba-data-table.css`, `src/Neba.Website.Server/wwwroot/js/neba-data-table.tests.js`
   - Why: tables key off the viewport today; the new `static` mode lets a table in a narrow container show every stat without tapping. Nothing uses the container marker yet, so page behavior does not change.

5. **Virtualize the full-standings modals and widen them to 960px**
   - Files: `src/Neba.Website.Server/Stats/SeasonStats.razor`, `src/Neba.Website.Server/Stats/SeasonStats.razor.css` (if the scroll check needs it), `tests/Neba.Website.Tests/Stats/SeasonStatsTests.cs`, `tests/e2e/Stats.spec.ts` (row window, last row reachable, no side scroll at 1100, 900, and 390px)
   - Why: the modals render about 25 rows instead of up to 560, and the eight-column table fits without side scroll. This closes items 1 and 2 of the 1.2.0 plan in #238.

If the work drifts from this list, change the list; do not merge commits.
