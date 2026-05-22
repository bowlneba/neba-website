# Champions Page — Blazor Conversion Instructions

Convert the NEBA Champions HTML prototype into a set of Blazor Server components. Work through each section in order. Do not skip ahead.

---

## 1. File Layout

Create the following files. The page lives under `History/Champions/`; all paths are relative to the Blazor project root.

```
  History/
    Champions/
      Champions.razor
      Champions.razor.css
      ChampionsHelpers.cs
      TitleCountView.razor
      TitleCountView.razor.css
      YearView.razor
      YearView.razor.css
      BowlerTitlesModal.razor
      BowlerTitlesModal.razor.css  
```

---

## 2. View-Model Types (reference only — do not create these)

```csharp
record TitleViewModel(
    string TournamentId,
    string TournamentName,
    string TournamentDate,   // pre-formatted display string, e.g. "Apr 2024"
    string TournamentType);

record BowlerTitlesViewModel(
    string BowlerName,
    bool   HallOfFame,
    IReadOnlyCollection<TitleViewModel> Titles);

record BowlerTitleSummaryViewModel(
    string BowlerId,
    string BowlerName,
    int    TitleCount,
    bool   HallOfFame);

record BowlerTitleViewModel(
    string BowlerId,
    string BowlerName,
    string TournamentId,
    int    TournamentMonth,
    int    TournamentYear,
    string TournamentType,
    bool   HallOfFame);

record TitlesByYearViewModel(
    int    Year,
    IReadOnlyCollection<BowlerTitleViewModel> Titles);
```

---

## 3. Data Access Pattern

Champions belong to the tournament domain. **Do not create a dedicated champions service.**

### 3.1 ITournamentApiService / TournamentApiService (extend, do not replace)

Add two new methods to the existing `ITournamentApiService` interface and `TournamentApiService` implementation in `Tournaments/`:

```csharp
Task<ErrorOr<IReadOnlyCollection<BowlerTitleSummaryViewModel>>> GetTitleSummariesAsync(CancellationToken ct = default);
Task<ErrorOr<IReadOnlyCollection<TitlesByYearViewModel>>>       GetTitlesByYearAsync(CancellationToken ct = default);
```

Each method:

1. Calls `ITournamentsApi.ListTournamentChampionsAsync` independently via `ApiExecutor.ExecuteAsync`
2. On error: returns `result.Errors`
3. On success: re-groups the flat `TournamentChampionResponse` collection into the appropriate view model

**GetTitleSummariesAsync grouping logic:**

- Flatten tournaments → champions into `(ChampionResponse champion, TournamentChampionResponse tournament)` pairs
- Group by `champion.BowlerId`
- For each group: `TitleCount = group.Count()`, `HallOfFame = group.First().champion.HallOfFame`
- Order result by `TitleCount` descending

**GetTitlesByYearAsync grouping logic:**

- Group responses by `tournament.TournamentDate.Year`
- For each year group, collect all `BowlerTitleViewModel`s from every tournament in that year
- `BowlerTitleViewModel.TournamentMonth` = `tournament.TournamentDate.Month`, `.TournamentYear` = `tournament.TournamentDate.Year`
- Order result by `Year` descending

### 3.2 BowlerTitlesModal data access

`BowlerTitlesModal.razor` injects `ApiExecutor` + `IBowlersApi` **directly** — no service wrapper. This matches the pattern used in `HallOfFame.razor` and `BowlerOfTheYear.razor`.

```razor
@inject ApiExecutor ApiExecutor
@inject IBowlersApi BowlersApi
```

Call pattern:

```csharp
var result = await ApiExecutor.ExecuteAsync(
    "BowlersApi",
    nameof(LoadTitlesAsync),
    ct => BowlersApi.GetBowlerTitlesAsync(BowlerId!, ct));
```

`BowlerTitlesResponse.Titles` items have `TournamentDate` as `DateOnly` — format it to `TitleViewModel.TournamentDate` display string (e.g. `"Apr 2024"`) in the mapping.

---

## 4. Shared NEBA Components (actual APIs — use exactly as documented here)

### NebaModal

**Location**: `Components/NebaModal.razor`

Current parameters:

- `IsOpen` (bool, EditorRequired)
- `OnClose` (EventCallback, EditorRequired)
- `Title` (string?, optional) — plain string rendered as `<h2>` in the built-in header
- `ChildContent` (RenderFragment?, optional) — rendered in `neba-modal-body`
- `FooterContent` (RenderFragment?, optional)
- `ShowCloseButton` (bool, default true)
- `CloseOnBackdropClick` (bool, default true)
- `MaxWidth` (string?, optional)
- `CssClass` (string?, optional)

**Required enhancement before implementing BowlerTitlesModal:** Add a `HeaderContent` render fragment parameter:

```csharp
/// <summary>
/// Optional custom header content. When provided, replaces the built-in Title h2 element.
/// The close button (if ShowCloseButton is true) renders alongside this content.
/// </summary>
[Parameter]
public RenderFragment? HeaderContent { get; set; }
```

In the template, change the header block from:

```razor
@if (!string.IsNullOrWhiteSpace(Title))
{
    <div class="neba-modal-header">
        <h2 class="neba-modal-title" id="@_titleId">@Title</h2>
        ...
    </div>
}
```

To:

```razor
@if (HeaderContent is not null || !string.IsNullOrWhiteSpace(Title))
{
    <div class="neba-modal-header">
        @if (HeaderContent is not null)
        {
            @HeaderContent
        }
        else
        {
            <h2 class="neba-modal-title" id="@_titleId">@Title</h2>
        }
        @if (ShowCloseButton)
        {
            <button type="button" class="neba-modal-close" @onclick="HandleClose" aria-label="Close modal">
                ✕
            </button>
        }
    </div>
}
```

Also update `DialogLabelledBy` to stay null when using `HeaderContent` (no title ID to reference); the `DialogAriaLabel` fallback covers accessibility.

### NebaSegmentedControl

**Does not exist.** Use two `<button>` elements with scoped CSS classes `segmented__btn` and `is-active`. No fallback hedge needed — go straight to this implementation.

```razor
<div class="segmented">
    <button class="@(_activeView == ChampionsView.Titles ? "segmented__btn is-active" : "segmented__btn")"
            @onclick="@(() => _activeView = ChampionsView.Titles)">
        By Titles
    </button>
    <button class="@(_activeView == ChampionsView.Year ? "segmented__btn is-active" : "segmented__btn")"
            @onclick="@(() => _activeView = ChampionsView.Year)">
        By Year
    </button>
</div>
```

### NebaLoadingIndicator

Exists. No parameters needed. Use as `<NebaLoadingIndicator />`.

### NebaAlert

Exists. Required parameters: `Severity` (`NotifySeverity` enum) and `Message` (string).

Error alert pattern (match `HallOfFame.razor` and `BowlerOfTheYear.razor` exactly):

```razor
@if (!string.IsNullOrWhiteSpace(_error))
{
    <NebaAlert Severity="NotifySeverity.Error"
               Title="Error Loading Champions"
               Message="@_error"
               Dismissible="true"
               OnDismiss="@(() => _error = null)" />
}
```

---

## 5. Global Razor / CSS Rules

- **All C# attribute values use `@` prefix.** Example: `class="@cssClass"` not `class="cssClass"` when the value is a C# expression.
- **Never use bare `<` inside switch expressions in `@code`.** Use helper methods or string-returning properties instead.
- **`[Parameter, EditorRequired]`** on every non-optional parameter. Never use the `required` keyword on parameters.
- **Scoped CSS only.** No inline `style=""` attributes. All visual rules go in the paired `.razor.css` file.
- **`::deep` selectors** when styling child component internals from a parent scoped file.
- Use `@key` on every `@foreach` that renders components or complex DOM trees.
- `@rendermode @(new InteractiveServerRenderMode(prerender: false))` on `Champions.razor`.

---

## 6. Champions.razor

### Route

```razor
@page "/history/champions"
```

### Injections

```razor
@inject ApiExecutor ApiExecutor
@inject ITournamentApiService TournamentApiService
@inject IBowlersApi BowlersApi
```

> `ITournamentApiService` is in `Neba.Website.Server.Tournaments`. `IBowlersApi` is in `Neba.Api.Contracts.Bowlers`. Add the appropriate `@using` directives.

### Template Structure

```
<page-shell>
  <Hero>                     ← static section, stats populated from loaded data
  <Toolbar>
    <segmented buttons>      ← "By Titles" / "By Year"
    <expand-all btn>
    <collapse-all btn>
  </Toolbar>

  @if (!string.IsNullOrWhiteSpace(_error)) → <NebaAlert ...>
  @else if (_loading)                      → <NebaLoadingIndicator>
  @else
    <TitleCountView>   (visibility via CSS — see section below)
    <YearView>         (visibility via CSS — see section below)

  <BowlerTitlesModal>
</page-shell>
```

### State Fields

```csharp
private bool   _loading = true;
private string? _error  = null;

private IReadOnlyCollection<BowlerTitleSummaryViewModel> _summaries
    = Array.Empty<BowlerTitleSummaryViewModel>();
private IReadOnlyCollection<TitlesByYearViewModel> _years
    = Array.Empty<TitlesByYearViewModel>();

private ChampionsView _activeView = ChampionsView.Titles;

// Modal
private string? _modalBowlerId;
private string? _modalBowlerName;
private int     _modalTitleCount;
private bool    _modalHallOfFame;
private bool    _modalOpen;

// Component refs for Expand/Collapse All
private TitleCountView? _titleCountView;
private YearView?       _yearView;
```

### Enum

```csharp
private enum ChampionsView { Titles, Year }
```

### Hero Stats (computed properties)

```csharp
private int TotalTitles     => _summaries.Sum(s => s.TitleCount);
private int TotalChampions  => _summaries.Count;
private int HallOfFamers    => _summaries.Count(s => s.HallOfFame);
private int FirstYear       => _years.Count > 0 ? _years.Min(y => y.Year) : 0;
```

### Lifecycle

```csharp
protected override async Task OnInitializedAsync()
{
    var summariesTask = TournamentApiService.GetTitleSummariesAsync();
    var yearsTask     = TournamentApiService.GetTitlesByYearAsync();

    var (summariesResult, yearsResult) = await (summariesTask, yearsTask);

    _loading = false;

    if (summariesResult.IsError)
    {
        _error = summariesResult.FirstError.Description;
        return;
    }
    if (yearsResult.IsError)
    {
        _error = yearsResult.FirstError.Description;
        return;
    }

    _summaries = summariesResult.Value;
    _years     = yearsResult.Value;
}
```

Both API calls fire concurrently via `await (task1, task2)` tuple pattern.

### Segmented Buttons

Use plain buttons per Section 4:

```razor
<div class="segmented">
    <button class="@(_activeView == ChampionsView.Titles ? "segmented__btn is-active" : "segmented__btn")"
            @onclick="@(() => _activeView = ChampionsView.Titles)">
        By Titles
    </button>
    <button class="@(_activeView == ChampionsView.Year ? "segmented__btn is-active" : "segmented__btn")"
            @onclick="@(() => _activeView = ChampionsView.Year)">
        By Year
    </button>
</div>
```

### Expand / Collapse All Buttons

```csharp
private void ExpandAll()
{
    if (_activeView == ChampionsView.Titles) _titleCountView?.ExpandAll();
    else _yearView?.ExpandAll();
}

private void CollapseAll()
{
    if (_activeView == ChampionsView.Titles) _titleCountView?.CollapseAll();
    else _yearView?.CollapseAll();
}
```

### Conditional Visibility

Do **not** destroy/recreate the child views on toggle — use CSS `display` control:

```razor
<div class="@(_activeView == ChampionsView.Titles ? "view is-active" : "view")">
    <TitleCountView @ref="_titleCountView"
                    Summaries="@_summaries"
                    OnBowlerSelected="@OpenModal" />
</div>
<div class="@(_activeView == ChampionsView.Year ? "view is-active" : "view")">
    <YearView @ref="_yearView"
              Years="@_years"
              Summaries="@_summaries"
              OnBowlerSelected="@OpenModal" />
</div>
```

### Modal Wiring

```csharp
private void OpenModal(BowlerTitleSummaryViewModel bowler)
{
    _modalBowlerId   = bowler.BowlerId;
    _modalBowlerName = bowler.BowlerName;
    _modalTitleCount = bowler.TitleCount;
    _modalHallOfFame = bowler.HallOfFame;
    _modalOpen       = true;
}
```

```razor
<BowlerTitlesModal IsOpen="@_modalOpen"
                   BowlerId="@_modalBowlerId"
                   BowlerName="@_modalBowlerName"
                   TitleCount="@_modalTitleCount"
                   HallOfFame="@_modalHallOfFame"
                   OnClose="@(() => _modalOpen = false)" />
```

### Champions.razor.css

Copy the following CSS blocks from the prototype, adapting selectors to plain scoped CSS:

- `.page` (max-width, margin, padding)
- `.hero` and all `.hero-*` descendants
- `.toolbar`, `.toolbar-spacer`, `.toolbar-actions`, `.toolbar-btn`
- `.segmented`, `.segmented__btn`, `.segmented__btn.is-active`
- `.view`, `.view.is-active`

Responsive (add within this file):
```css
@media (max-width: 760px) {
    .hero              { padding: 2.25rem 1.5rem 2.5rem; }
    .hero h1           { font-size: 2.4rem; }
    .hero-stats        { gap: 1.5rem; }
    .hero-stat__num    { font-size: 1.5rem; }
}
```

---

## 7. TitleCountView.razor

### Parameters

```csharp
[Parameter, EditorRequired]
public IReadOnlyCollection<BowlerTitleSummaryViewModel> Summaries { get; set; } = default!;

[Parameter, EditorRequired]
public EventCallback<BowlerTitleSummaryViewModel> OnBowlerSelected { get; set; }
```

### Public Methods (called by Champions.razor via @ref)

```csharp
public void ExpandAll()   => _collapsedTiers.Clear();
public void CollapseAll() => _collapsedTiers.UnionWith(new[] { "elite", "mid", "std" });
```

### State

```csharp
private readonly HashSet<string> _collapsedTiers = new();

private void ToggleTier(string tier) =>
    _ = _collapsedTiers.Contains(tier)
        ? _collapsedTiers.Remove(tier)
        : _collapsedTiers.Add(tier);

private bool IsCollapsed(string tier) => _collapsedTiers.Contains(tier);
```

### Tier Grouping (computed properties)

```csharp
private IReadOnlyCollection<BowlerTitleSummaryViewModel> EliteBowlers =>
    Summaries.Where(s => s.TitleCount >= 20).OrderByDescending(s => s.TitleCount).ToList();

private IReadOnlyCollection<BowlerTitleSummaryViewModel> MidBowlers =>
    Summaries.Where(s => s.TitleCount is >= 10 and <= 19).OrderByDescending(s => s.TitleCount).ToList();

private IReadOnlyCollection<BowlerTitleSummaryViewModel> StdBowlers =>
    Summaries.Where(s => s.TitleCount < 10).OrderByDescending(s => s.TitleCount).ToList();
```

### Tier Section Template

Render three tier sections. Structure for one tier:

```razor
<div class="@TierSectionClass("elite")" data-tier="elite">
    <div class="tier-head" role="button" tabindex="0"
         @onclick="@(() => ToggleTier("elite"))"
         @onkeydown="@(e => { if (e.Key is "Enter" or " ") ToggleTier("elite"); })">
        <span class="tier-head__chevron"><span class="msym">expand_more</span></span>
        <span class="tier-head__rule"></span>
        <h2 class="tier-head__title font-display">
            Elite <span class="tier-head__range">20+ titles</span>
        </h2>
        <span class="tier-head__count">@EliteBowlers.Count bowlers</span>
    </div>
    <div class="tier-grid">
        @foreach (var bowler in EliteBowlers)
        {
            <button class="bowler-card"
                    @key="@bowler.BowlerId"
                    @onclick="@(() => OnBowlerSelected.InvokeAsync(bowler))">
                <span class="bowler-card__count">@bowler.TitleCount</span>
                <span class="bowler-card__body">
                    <span class="bowler-card__name">@bowler.BowlerName</span>
                </span>
                @if (bowler.HallOfFame)
                {
                    <img src="/images/neba-hof.jpg" class="hof-badge" alt="Hall of Fame" />
                }
                <span class="bowler-card__chev"><span class="msym">chevron_right</span></span>
            </button>
        }
    </div>
</div>
```

Helper method for collapsed class:

```csharp
private string TierSectionClass(string tier) =>
    IsCollapsed(tier) ? "tier-section is-collapsed" : "tier-section";
```

Tier labels and ranges:

| Tier key | `data-tier` | Title | Range text |
| --- | --- | --- | --- |
| `elite` | `elite` | Elite | 20+ titles |
| `mid` | `mid` | Champions | 10 – 19 titles |
| `std` | `std` | Title Winners | 1 – 9 titles |

> **Note:** The bowler card does **not** include a `meta` span (year range) — that data is not in `BowlerTitleSummaryViewModel`. Omit `.bowler-card__meta` entirely.

### TitleCountView.razor.css

Copy verbatim from the prototype:

- `.tier-section`, `.tier-section:first-child`
- `.tier-head` and all `.tier-head__*`
- `.tier-section.is-collapsed .tier-head__chevron`
- `.tier-grid`, `.tier-section.is-collapsed .tier-grid`
- `.bowler-card` and all `.bowler-card__*` variants
- `.tier-section[data-tier="elite"]` overrides
- `.tier-section[data-tier="mid"]` overrides
- `.hof-badge { height: 1.25rem; width: auto; vertical-align: middle; }` (size to taste)

Responsive:

```css
@media (max-width: 760px) {
    .tier-head__title { font-size: 1.1rem; }
    .tier-head__range { display: block; margin-left: 0; font-size: 0.82rem; }
}
```

---

## 8. YearView.razor

### Parameters

```csharp
[Parameter, EditorRequired]
public IReadOnlyCollection<TitlesByYearViewModel> Years { get; set; } = default!;

[Parameter, EditorRequired]
public IReadOnlyCollection<BowlerTitleSummaryViewModel> Summaries { get; set; } = default!;

[Parameter, EditorRequired]
public EventCallback<BowlerTitleSummaryViewModel> OnBowlerSelected { get; set; }
```

### Public Methods

```csharp
public void ExpandAll()   => _collapsedYears.Clear();
public void CollapseAll() => _collapsedYears.UnionWith(Years.Select(y => y.Year));
```

### State

```csharp
private HashSet<int> _collapsedYears = new();

protected override void OnParametersSet()
{
    var mostRecent = Years.OrderByDescending(y => y.Year).Take(3).Select(y => y.Year).ToHashSet();
    _collapsedYears = Years.Select(y => y.Year).Where(y => !mostRecent.Contains(y)).ToHashSet();
}

private void ToggleYear(int year) =>
    _ = _collapsedYears.Contains(year)
        ? _collapsedYears.Remove(year)
        : _collapsedYears.Add(year);

private string YearSectionClass(int year) =>
    _collapsedYears.Contains(year) ? "year-section is-collapsed" : "year-section";
```

### Table Row Grouping

```csharp
private IEnumerable<(int Month, string TournamentId, string TournamentType, IList<BowlerTitleViewModel> Champions)>
    GetEventRows(TitlesByYearViewModel yearVm) =>
        yearVm.Titles
              .GroupBy(t => t.TournamentId)
              .OrderBy(g => g.First().TournamentMonth)
              .Select(g => (
                  Month:          g.First().TournamentMonth,
                  TournamentId:   g.Key,
                  TournamentType: g.First().TournamentType,
                  Champions:      (IList<BowlerTitleViewModel>)g.ToList()
              ));
```

### Year Section Template

```razor
@foreach (var year in Years.OrderByDescending(y => y.Year))
{
    var eventRows = GetEventRows(year).ToList();
    var champCount = year.Titles.Select(t => t.BowlerId).Distinct().Count();

    <div class="@YearSectionClass(year.Year)" @key="@year.Year">
        <div class="year-head" role="button" tabindex="0"
             @onclick="@(() => ToggleYear(year.Year))"
             @onkeydown="@(e => { if (e.Key is "Enter" or " ") ToggleYear(year.Year); })">
            <span class="year-head__chevron"><span class="msym">expand_more</span></span>
            <h2 class="year-head__title font-display">@year.Year</h2>
            <span class="year-head__meta">
                <span><b>@eventRows.Count</b> events</span>
                <span><b>@champCount</b> champions</span>
            </span>
        </div>
        <div class="year-body">
            <table class="year-table">
                <thead>
                    <tr>
                        <th class="col-month">Month</th>
                        <th class="col-type">Tournament Type</th>
                        <th class="col-champs">Champions</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var row in eventRows)
                    {
                        <tr @key="@row.TournamentId">
                            <td class="col-month">@ChampionsHelpers.MonthAbbreviation(row.Month)</td>
                            <td class="col-type">
                                <span class="@("type-pill " + ChampionsHelpers.TypePillClass(row.TournamentType))">
                                    @row.TournamentType
                                </span>
                            </td>
                            <td class="col-champs">
                                @for (int i = 0; i < row.Champions.Count; i++)
                                {
                                    var champion = row.Champions[i];
                                    var summary  = LookupSummary(champion.BowlerId);
                                    <a class="champ-link"
                                       @key="@champion.BowlerId"
                                       href="javascript:void(0)"
                                       @onclick="@(() => InvokeChampion(summary, champion))"
                                       @onclick:preventDefault>@champion.BowlerName</a>
                                    @if (i < row.Champions.Count - 1)
                                    {
                                        <span class="champ-sep">·</span>
                                    }
                                }
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    </div>
}
```

### Helper Methods

```csharp
private BowlerTitleSummaryViewModel LookupSummary(string bowlerId) =>
    Summaries.FirstOrDefault(s => s.BowlerId == bowlerId)
    ?? new BowlerTitleSummaryViewModel(bowlerId, string.Empty, 0, false);

private Task InvokeChampion(BowlerTitleSummaryViewModel summary, BowlerTitleViewModel champion)
{
    var effective = summary.BowlerId != string.Empty
        ? summary
        : new BowlerTitleSummaryViewModel(champion.BowlerId, champion.BowlerName, 0, champion.HallOfFame);
    return OnBowlerSelected.InvokeAsync(effective);
}
```

### YearView.razor.css

Copy verbatim from the prototype:

- `.year-section`, `.year-section:first-child`
- `.year-head` and all `.year-head__*`
- `.year-section.is-collapsed .year-head__chevron`
- `.year-section.is-collapsed .year-head`
- `.year-body`, `.year-section.is-collapsed .year-body`
- `.year-table` and all `.year-table *` variants
- `.champ-link`, `.champ-sep`
- `.type-pill` base and all `.type-*` color variants (see Section 10)

Responsive:

```css
@media (max-width: 760px) {
    .year-table .col-month,
    .year-table .col-type      { width: auto; }
    .year-table thead th,
    .year-table tbody td       { padding: 0.7rem 0.85rem; }
}
```

---

## 9. BowlerTitlesModal.razor

### Modal Parameters

```csharp
[Parameter, EditorRequired]
public bool IsOpen { get; set; }

[Parameter, EditorRequired]
public EventCallback OnClose { get; set; }

// Nullable — modal may be mounted before a bowler is selected
[Parameter] public string? BowlerId   { get; set; }
[Parameter] public string? BowlerName { get; set; }
[Parameter] public int     TitleCount { get; set; }
[Parameter] public bool    HallOfFame { get; set; }
```

### Modal Injections

```razor
@inject ApiExecutor ApiExecutor
@inject IBowlersApi BowlersApi
```

### Modal State

```csharp
private enum ModalState { Loading, Error, Data }

private ModalState             _state = ModalState.Loading;
private BowlerTitlesViewModel? _data  = null;
private string?                _lastLoadedBowlerId;
```

### Lifecycle — Load on BowlerId Change

```csharp
protected override async Task OnParametersSetAsync()
{
    if (!IsOpen || BowlerId is null || BowlerId == _lastLoadedBowlerId)
        return;

    await LoadTitlesAsync();
}

private async Task LoadTitlesAsync()
{
    _lastLoadedBowlerId = BowlerId;
    _state = ModalState.Loading;
    _data  = null;
    StateHasChanged();

    var result = await ApiExecutor.ExecuteAsync(
        "BowlersApi",
        nameof(LoadTitlesAsync),
        ct => BowlersApi.GetBowlerTitlesAsync(BowlerId!, ct));

    if (result.IsError)
    {
        _state = ModalState.Error;
        return;
    }

    _data  = result.Value.ToViewModel();   // mapping extension: BowlerTitlesResponse → BowlerTitlesViewModel
    _state = ModalState.Data;
}

private Task RetryAsync()
{
    _lastLoadedBowlerId = null;   // force reload
    return LoadTitlesAsync();
}
```

### Sorted Titles

```csharp
// Sorted most-recent-first; row number = position from oldest (1 = first career title)
private IReadOnlyList<TitleViewModel> SortedTitles =>
    _data?.Titles
          .OrderByDescending(t => t.TournamentDate)   // pre-formatted "Apr 2024" sorts correctly as ISO
          .ToList()
    ?? Array.Empty<TitleViewModel>();
```

> **Note**: `TitleViewModel.TournamentDate` is the pre-formatted display string produced by the mapping extension (from `DateOnly` in the API response). Sort should happen in the mapping or by the underlying `DateOnly` value before formatting — keep the `DateOnly` available in the mapping if you need correct chronological sort.

### Template

Use `<NebaModal>` with the `HeaderContent` render fragment added in Section 4:

```razor
<NebaModal IsOpen="@IsOpen" OnClose="@OnClose" MaxWidth="680px">
    <HeaderContent>
        <div class="modal-title-wrap">
            <span class="modal-eyebrow">Champion record</span>
            <h2 class="modal-title font-display">@BowlerName</h2>
        </div>
        <div class="modal-summary-inline">
            <span class="modal-summary-inline__count">@TitleCount</span>
            <span class="modal-summary-inline__label">titles</span>
            @if (HallOfFame)
            {
                <img src="/images/neba-hof.jpg" class="hof-badge" alt="Hall of Fame" />
            }
        </div>
    </HeaderContent>
    <ChildContent>
        @* Portrait / mobile summary card *@
        <div class="modal-summary-card">
            <span class="modal-summary-card__count">@TitleCount</span>
            <div class="modal-summary-card__meta">
                <span class="modal-summary-card__label">Career titles</span>
                <span class="modal-summary-card__name">@BowlerName</span>
            </div>
            @if (HallOfFame)
            {
                <img src="/images/neba-hof.jpg" class="hof-badge" alt="Hall of Fame" />
            }
        </div>

        <div class="modal-body">
            @if (_state == ModalState.Loading)
            {
                <div class="modal-state is-active">
                    <NebaLoadingIndicator />
                    <div class="modal-state__text">Loading title history…</div>
                </div>
            }
            else if (_state == ModalState.Error)
            {
                <div class="modal-state is-active">
                    <span class="msym">error</span>
                    <h3 class="modal-state__title">Couldn't load titles</h3>
                    <div class="modal-state__text">
                        Something went wrong fetching this bowler's record.
                    </div>
                    <button class="modal-state__retry" @onclick="RetryAsync">Retry</button>
                </div>
            }
            else
            {
                <table class="titles-table">
                    <thead>
                        <tr>
                            <th class="tt-num">#</th>
                            <th class="tt-tourn">Tournament</th>
                            <th class="tt-date">Date</th>
                            <th class="tt-type">Type</th>
                        </tr>
                    </thead>
                    <tbody>
                        @{ var sorted = SortedTitles; }
                        @for (int i = 0; i < sorted.Count; i++)
                        {
                            var title  = sorted[i];
                            var rowNum = sorted.Count - i;   // 1 = oldest career title
                            <tr @key="@title.TournamentId">
                                <td class="tt-num">@rowNum</td>
                                <td class="tt-tourn">
                                    <a href="/tournaments/@title.TournamentId">
                                        @title.TournamentName
                                    </a>
                                </td>
                                <td class="tt-date">@title.TournamentDate</td>
                                <td class="tt-type">
                                    <span class="@("type-pill " + ChampionsHelpers.TypePillClass(title.TournamentType))">
                                        @title.TournamentType
                                    </span>
                                </td>
                            </tr>
                        }
                    </tbody>
                </table>
            }
        </div>
    </ChildContent>
</NebaModal>
```

### BowlerTitlesModal.razor.css

Copy verbatim from the prototype:

- `.modal-header` and all `.modal-*` (eyebrow, title, summary-inline, close)
- `.modal-summary-card` and all `.modal-summary-card__*`
- `.modal-body`
- `.titles-table` and all `.titles-table *` variants
- `.modal-state`, `.modal-state.is-active`, `.modal-state__*`
- `.modal-state__retry`
- `.type-pill` base and all `.type-*` color variants
- `.hof-badge { height: 1.5rem; width: auto; vertical-align: middle; }`

Responsive:

```css
@media (max-width: 640px) and (orientation: portrait) {
    .modal-summary-inline { display: none; }
    .modal-summary-card   { display: flex; }
    .modal-title          { font-size: 1.3rem; white-space: normal; }
}
@media (max-width: 540px) {
    .modal-summary-inline { display: none; }
    .modal-summary-card   { display: flex; }
}
```

---

## 10. Type Pill CSS Reference

Include in both `YearView.razor.css` and `BowlerTitlesModal.razor.css`:

```css
.type-pill {
    display: inline-flex; align-items: center; gap: 0.4rem;
    padding: 0.25rem 0.7rem;
    border-radius: 999px;
    font-family: 'Manrope', sans-serif;
    font-weight: 600; font-size: 0.72rem; letter-spacing: 0.02em;
    white-space: nowrap;
}
.type-pill::before {
    content: ''; width: 6px; height: 6px; border-radius: 50%;
    background: currentColor; opacity: 0.7;
}
.type-singles { background: var(--neba-blue-50);  color: var(--neba-blue-700); }
.type-doubles { background: #f4ecfb;              color: #7a3fb8; }
.type-trios   { background: #fff4e6;              color: #CC6600; }
.type-team    { background: #ecfdf5;              color: #047857; }
.type-senior  { background: #fffbeb;              color: #92400e; }
.type-women   { background: #fdf2f8;              color: #be185d; }
.type-special { background: var(--neba-gray-100); color: var(--neba-gray-700); }
```

---

## 11. ChampionsHelpers.cs

Put shared static helpers in `History/Champions/ChampionsHelpers.cs`. Both `YearView` and `BowlerTitlesModal` call these directly — no duplication.

```csharp
namespace Neba.Website.Server.History.Champions;

internal static class ChampionsHelpers
{
    public static string MonthAbbreviation(int month) => month switch
    {
        1  => "Jan", 2  => "Feb", 3  => "Mar",
        4  => "Apr", 5  => "May", 6  => "Jun",
        7  => "Jul", 8  => "Aug", 9  => "Sep",
        10 => "Oct", 11 => "Nov", 12 => "Dec",
        _  => string.Empty
    };

    public static string TypePillClass(string tournamentType)
    {
        var t = tournamentType.ToLowerInvariant();
        if (t.Contains("double"))  return "type-doubles";
        if (t.Contains("trio"))    return "type-trios";
        if (t.Contains("team"))    return "type-team";
        if (t.Contains("senior"))  return "type-senior";
        if (t.Contains("women"))   return "type-women";
        if (t.Contains("special")) return "type-special";
        return "type-singles";
    }
}
```

---

## 12. CSS Variables and Global Styles (app.css additions)

The existing `neba_theme.css` provides most `--neba-blue-*` and `--neba-gray-*` scale tokens. The following are **missing** and must be added to `app.css` (not a scoped `.razor.css` — they need to be globally accessible).

Add a block to `app.css`:

```css
/* ── Champions page tokens ──────────────────────────────────── */
:root {
    /* Blue scale additions (--neba-blue-brand already exists for #052767) */
    --neba-blue-900: var(--neba-blue-brand);  /* alias for champion page references */
    --neba-blue-50:  #EEF0FD;

    /* Ink / accent */
    --t-ink:  #0a0f2c;
    --t-gold: #B8860B;

    /* Tier accent tokens */
    --tier-elite-bg:   linear-gradient(135deg, #fde68a 0%, #f59e0b 100%);
    --tier-elite-ink:  #78350f;
    --tier-elite-ring: #f59e0b;

    --tier-mid-bg:   linear-gradient(135deg, #052767 0%, #3E4FD9 100%);
    --tier-mid-ink:  #ffffff;
    --tier-mid-ring: #3E4FD9;

    --tier-std-bg:   #ffffff;
    --tier-std-ink:  #0a0f2c;
    --tier-std-ring: #E5E5E5;

    /* Radius scale */
    --radius-md: 0.5rem;
    --radius-lg: 0.75rem;
    --radius-xl: 1rem;

    /* Shadow scale */
    --shadow-card: 0 1px 2px rgba(5,39,103,0.04), 0 1px 0 rgba(5,39,103,0.02);
    --shadow-lift: 0 14px 36px -16px rgba(5,39,103,0.28);
}

/* Material Symbols (Outlined variant — already loaded in App.razor) */
.msym {
    font-family: 'Material Symbols Outlined';
    font-weight: normal; font-style: normal;
    line-height: 1; letter-spacing: normal;
    text-transform: none; white-space: nowrap;
    display: inline-block;
    font-feature-settings: 'liga';
    -webkit-font-smoothing: antialiased;
}
```

> `Material Symbols Outlined` is already loaded in `App.razor`. Do **not** add a second font link.

---

## 13. Name Display Rule

**Bowler names must never be truncated.** Do not use `text-overflow: ellipsis`, `overflow: hidden`, `white-space: nowrap` (without wrapping), or any other CSS technique that clips a name. Cards, table cells, and modal headers must all expand to show the full name. Use `word-break: break-word` if long names need to wrap within a constrained container.

---

## 14. Verification Checklist

Before considering the implementation complete, confirm:

- [ ] `Champions.razor` renders without errors with empty collections (`Array.Empty`)
- [ ] Both API calls fire concurrently on init
- [ ] Switching segment tabs does not reset collapse state in the inactive view
- [ ] Expand All / Collapse All only affects the currently active view
- [ ] Clicking a bowler card in TitleCountView opens the modal with correct name / count / HOF
- [ ] Clicking a champion link in YearView opens the modal; if the bowler is in the summaries list the correct count appears immediately
- [ ] Modal shows spinner during load, error state + Retry on failure, table on success
- [ ] Retry re-fires the API call
- [ ] Tournament links in modal are `<a href="/tournaments/{TournamentId}">` — not JS `onclick`
- [ ] `TypePillClass` and `MonthAbbreviation` live in `ChampionsHelpers` — no duplication
- [ ] HOF badge is `<img src="/images/neba-hof.jpg" class="hof-badge" alt="Hall of Fame" />` everywhere — no text "HOF" spans
- [ ] **No bowler name is truncated** — no ellipsis, no overflow clipping on name elements
- [ ] No inline `style=""` attributes remain
- [ ] No bare `<` operators inside `@code` switch expressions
- [ ] All component parameters use `[Parameter, EditorRequired]` where non-optional
- [ ] CSS champion tokens are in `app.css` under `:root`, not in a scoped file
- [ ] `.msym` class is in `app.css` pointing to `Material Symbols Outlined`
- [ ] `NebaModal` has `HeaderContent` render fragment and it renders correctly alongside the close button
- [ ] `ITournamentApiService` has `GetTitleSummariesAsync` and `GetTitlesByYearAsync` and they are registered in DI
