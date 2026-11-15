# Hall of Fame Inductee Profile

Clicking an inductee's photo (or initials) or name on `/hall-of-fame` opens a profile modal with a prestigious header (photo, class year, categories, Hall of Fame logo) and the same points and membership data the points page modal shows.

## Decisions locked in during scoping

- **Not critical.** NFR checkpoint skipped: public, read-only, names already public.
- **Permission:** anonymous. Reuses the anonymous `GET /hall-of-fame/points/{bowlerId}`. No new policy, no new role.
- **Who opens it:** every inductee, including Meritorious Service and Friend of NEBA. Those two have no points, so the body shows a short line instead of empty tables (a later enhancement can fill it).
- **No 36-point highlight** in the inductee modal. The points table is a plain history with a running total. The API still sends `ReachedThreshold`; the points page keeps its highlight.
- **No "of 36 / needed" targets** in the inductee stats: they are inducted, so the targets add nothing.
- **Banner:** the existing `/images/neba-hof-480.jpg` logo, small, in a dark blue and gold header.
- **Photo editing:** stays on the gallery card. The modal is read-only; no edit pencil in it.
- **Header needs no extra fetch.** Photo, focal point, class year, name and categories come from the inductions list the page already holds. Only the points body fetches.
- **Reuse, not duplicate.** Same `IHallOfFameApi.GetHallOfFamePointsDetailAsync` call and `HallOfFamePointsDetailViewModel`. The points page keeps its current modal.

## Phase 1: API

The one gap: `GET /hall-of-fame/inductions` returns the induction id but not the bowler id, and `GET /hall-of-fame/points/{bowlerId}` needs the bowler id. Phase 1 adds `BowlerId` to the inductions list. The detail route needs no change. The cache key stays as is; clear the cache by hand on deploy, because entries written before it lack `BowlerId`.

Paths relative to `src/` unless they start with `tests/`. All code is a draft to paste and adjust.

### Domain

No change. `HallOfFameInduction.BowlerId` already exists.

### Application

`Neba.Api/Features/HallOfFame/ListHallOfFameInductions/HallOfFameInductionDto.cs`: add after `Id`.

```csharp
    /// <summary>
    /// The ULID string identifying the inducted bowler.
    /// </summary>
    public required string BowlerId { get; init; }
```

`Neba.Api/Features/HallOfFame/ListHallOfFameInductions/ListHallOfFameInductionsQueryHandler.cs`: project the id and map it.

```csharp
            .Select(induction => new
            {
                induction.Id,
                induction.BowlerId,
                induction.Year,
                // ...unchanged
            })
```

```csharp
        return [.. rows.Select(row => new HallOfFameInductionDto
        {
            Id = row.Id.ToString(),
            BowlerId = row.BowlerId.ToString(),
            Year = row.Year,
            // ...unchanged
        })];
```

### API

`Neba.Api/Features/HallOfFame/ListHallOfFameInductions/ListHallOfFameInductionsEndpoint.cs`: route, auth and status codes stay the same.

```csharp
                .Select(i => new HallOfFameInductionResponse
                {
                    Id = i.Id,
                    BowlerId = i.BowlerId,
                    Year = i.Year,
                    // ...unchanged
                })],
```

### Contracts

`Neba.Api.Contracts/HallOfFame/ListHallOfFameInductions/HallOfFameInductionResponse.cs`: add after `Id`.

```csharp
    /// <summary>
    /// The ULID string identifying the inducted bowler. Use it to fetch the bowler's Hall of Fame points.
    /// </summary>
    public required string BowlerId { get; init; }
```

### Infrastructure and database

None. No schema change, no migration.

### Tests

`tests/Neba.TestFactory/HallOfFame/HallOfFameInductionDtoFactory.cs`

```csharp
    public const string ValidBowlerId = "01JZ8K3M4N5P6Q7R8S9T0V1W3Z";

    public static HallOfFameInductionDto Create(
        string? id = null,
        string? bowlerId = null,
        int? year = null,
        // ...unchanged parameters
        => new()
        {
            Id = id ?? ValidId,
            BowlerId = bowlerId ?? ValidBowlerId,
            // ...unchanged
        };
```

In `Bogus(int count, Faker faker)`, draw bowler ids from a separate randomizer so seeded values for the other fields and the existing snapshots keep their values:

```csharp
        var bowlerIdFaker = new Faker { Random = new Randomizer(poolSeed + 1) };
        // ...in the Select:
                Id = Ulid.BogusString(faker),
                BowlerId = Ulid.BogusString(bowlerIdFaker),
```

`tests/Neba.TestFactory/HallOfFame/HallOfFameInductionResponseFactory.cs`: same shape (`ValidBowlerId`, a `bowlerId` parameter on `Create()`, and a separate `bowlerIdFaker` in `Bogus()`; do not reuse `idFaker`, or the focal-point draws shift).

```csharp
    public const string ValidBowlerId = "01JZ8K3M4N5P6Q7R8S9T0V1W3Z";
    // Create(): BowlerId = bowlerId ?? ValidBowlerId,
    // Bogus():  var bowlerIdFaker = new Faker { Random = new Randomizer(poolSeed + 1) };
    //           BowlerId = Ulid.BogusString(bowlerIdFaker),
```

`tests/Neba.Api.Tests/Features/HallOfFame/ListHallOfFameInductions/ListHallOfFameInductionsQueryHandlerTests.cs`: assert the id in the existing fields test.

```csharp
        dto.BowlerId.ShouldBe(bowler.Id.ToString());
```

`tests/Neba.Api.Tests/Features/HallOfFame/ListHallOfFameInductions/ListHallOfFameInductionsEndpointTests.cs`: new test, and the existing snapshot (`...ShouldReturnOkWithMappedInductions_WhenQuerySucceeds.verified.txt`) gains a `BowlerId` line per item; regenerate it with Verify and review the diff.

```csharp
    [Fact(DisplayName = "HandleAsync should map the bowler id into the response")]
    public async Task HandleAsync_ShouldMapBowlerId_WhenInductionExists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var dto = HallOfFameInductionDtoFactory.Create(bowlerId: "01JZ8K3M4N5P6Q7R8S9T0V1W4A");

        var queryHandlerMock = new Mock<IQueryHandler<ListHallOfFameInductionsQuery, IReadOnlyCollection<HallOfFameInductionDto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(handler => handler.HandleAsync(It.IsAny<ListHallOfFameInductionsQuery>(), cancellationToken))
            .ReturnsAsync([dto]);

        var endpoint = Factory.Create<ListHallOfFameInductionsEndpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert
        endpoint.Response.Items.Single().BowlerId.ShouldBe("01JZ8K3M4N5P6Q7R8S9T0V1W4A");
    }
```

`tests/e2e/mock-api/mock-api-server.ts`: add `bowlerId` to the interface and every item. A missing required field fails Playwright every run. The ids below do not collide with the points standings ids (`...101` to `...104`); Phase 2 adds detail mock data for them.

```typescript
interface MockInduction {
  id: string;
  bowlerId: string;
  // ...unchanged
}

    { id: '01JX0000000000000000000001', bowlerId: '01JX0000000000000000000201', year: 2024, bowlerName: 'Jane Smith', ... },
    { id: '01JX0000000000000000000002', bowlerId: '01JX0000000000000000000202', year: 2024, bowlerName: 'Bob Johnson', ... },
    { id: '01JX0000000000000000000003', bowlerId: '01JX0000000000000000000203', year: 2023, bowlerName: 'Alice Williams', ... },
    { id: '01JX0000000000000000000004', bowlerId: '01JX0000000000000000000204', year: 2023, bowlerName: 'Tom Davis', ... },
```

`tests/Neba.Website.Tests/HallOfFame/HallOfFameMappingExtensionsTests.cs` and `HallOfFameTests.cs` use the response factory, so they pick up the default with no edit; Phase 2 adds their assertions.

### Deferred to Phase 2 or out of scope

- The view model field, the modal markup and styling, and click handling (Phase 2).
- A new endpoint or DTO for the profile: none needed.
- Profile content for Meritorious Service and Friend of NEBA inductees (a later enhancement).

## Phase 2: UI

Paths relative to `src/Neba.Website.Server/HallOfFame/` unless they start with `tests/`. All code is a draft to paste and adjust. Class names keep the existing `pm-` prefix on the shared pieces so current selectors (bUnit and Playwright) still match.

One change from the functional draft: the `ShowPointValues` bool became a `HallOfFamePointsDetailLayout` enum (`Standing`, `Profile`). The inductee layout differs in more than one way (plain list, stat order, centered stats with gold rules), so one named layout reads better than a bool.

### API client

No change. `IHallOfFameApi.GetHallOfFamePointsDetailAsync` already exists and works for inducted bowlers.

### View model and mapping

`HallOfFameInductionViewModel.cs`: add after `Id`.

```csharp
    /// <summary>
    /// The ULID string identifying the inducted bowler. Used to load the bowler's Hall of Fame points.
    /// </summary>
    public required string BowlerId { get; init; }
```

`HallOfFameMappingExtensions.cs`: in the inductions `ToViewModel()`.

```csharp
                Id = dto.Id,
                BowlerId = dto.BowlerId,
                BowlerName = dto.BowlerName,
```

`tests/Neba.TestFactory/HallOfFame/HallOfFameInductionViewModelFactory.cs`: same shape as Phase 1. A separate randomizer keeps seeded values for the other fields.

```csharp
    public const string ValidBowlerId = "01JZ8K3M4N5P6Q7R8S9T0V1W3Z";

    public static HallOfFameInductionViewModel Create(
        string? id = null,
        string? bowlerId = null,
        string? bowlerName = null,
        // ...unchanged parameters
        => new()
        {
            Id = id ?? ValidId,
            BowlerId = bowlerId ?? ValidBowlerId,
            // ...unchanged
        };

    // In Bogus(int count, Faker faker):
    //   var bowlerIdFaker = new Faker { Random = new Randomizer(faker.Random.Int()) };   // draw once, before the Select
    //   BowlerId = Ulid.BogusString(bowlerIdFaker),
```

(No new factory for the detail view model: the new tests build it with `HallOfFamePointsDetailResponseFactory.Create(...).ToViewModel()`, as `HallOfFameMappingExtensionsTests` already does.)

### Components

`HallOfFamePointsDetailLayout.cs` (new)

```csharp
namespace Neba.Website.Server.HallOfFame;

/// <summary>
/// How <see cref="HallOfFamePointsDetailView"/> lays out one bowler's points.
/// </summary>
public enum HallOfFamePointsDetailLayout
{
    /// <summary>The points-page modal: left-aligned stats, a points table with running totals, and the threshold highlight.</summary>
    Standing,

    /// <summary>The inductee profile: centered stats, and plain two-column lists with no point values.</summary>
    Profile
}
```

`HallOfFamePointsDetailView.razor` (new; the markup moves out of `HallOfFamePointsModal.razor`)

```razor
@using System.Globalization
@using Humanizer

<div class="pm-body">
    <div class="@(Layout == HallOfFamePointsDetailLayout.Profile ? "pm-stats pm-stats--profile" : "pm-stats")">
        @foreach (var stat in Stats())
        {
            <div class="pm-stat">
                <b>@stat.Value</b>
                <span>@stat.Label</span>
            </div>
        }
    </div>

    <h3>Points earned</h3>
    @if (Detail.Breakdown.Count == 0)
    {
        <p class="pm-muted">No points yet.</p>
    }
    else if (Layout == HallOfFamePointsDetailLayout.Profile)
    {
        <ul class="pm-years">
            @foreach (var entry in Detail.Breakdown)
            {
                <li><span>@entry.Year</span><span>@entry.Source</span></li>
            }
        </ul>
    }
    else
    {
        <table class="pm-table">
            <thead>
                <tr><th scope="col">Year</th><th scope="col">Title or award</th><th scope="col" class="pm-r">Points</th><th scope="col" class="pm-r">Total</th></tr>
            </thead>
            <tbody>
                @foreach (var entry in Detail.Breakdown)
                {
                    <tr class="@(entry.ReachedThreshold ? "pm-reached" : null)">
                        <td>@entry.Year</td>
                        <td>
                            @entry.Source
                            @if (entry.ReachedThreshold)
                            {
                                <span class="pm-reached-tag">@(Requirements is null ? "Reached the points threshold" : "Reached " + Requirements.Points + " points")</span>
                            }
                        </td>
                        <td class="pm-r">@entry.Points</td>
                        <td class="pm-r">@entry.RunningTotal</td>
                    </tr>
                }
            </tbody>
            <tfoot>
                <tr><td></td><td>Total</td><td class="pm-r">@Detail.TotalPoints</td><td></td></tr>
            </tfoot>
        </table>
    }

    <h3>Membership years</h3>
    @if (Detail.MembershipYears.Count == 0)
    {
        <p class="pm-muted">No counted membership years. Guest and non-member entries do not count.</p>
    }
    else
    {
        <ul class="pm-years">
            @foreach (var year in Detail.MembershipYears)
            {
                <li><span>@year.Year</span><span>@year.MembershipType</span></li>
            }
        </ul>
    }

    @if (!Detail.MembershipHistoryComplete)
    {
        <p class="pm-note">
            This bowler's first membership is not on file. Records start in @(Requirements?.MembershipDataStartYear.ToString(CultureInfo.InvariantCulture) ?? "1999"), so the real count may be higher.
        </p>
    }
</div>

@code {
    [Parameter, EditorRequired]
    public HallOfFamePointsDetailViewModel Detail { get; set; } = default!;

    /// <summary>The thresholds, when the caller has them. Null drops the "of 36" and "needed" text.</summary>
    [Parameter]
    public HallOfFamePointRequirements? Requirements { get; set; }

    [Parameter]
    public HallOfFamePointsDetailLayout Layout { get; set; }

    // Standing order: points, seasons, membership. Profile order: membership, points, seasons.
    private IEnumerable<(string Value, string Label)> Stats()
    {
        var years = Detail.MembershipYears.Count;
        var points = (
            Value: Detail.TotalPoints.ToString(CultureInfo.InvariantCulture),
            Label: Requirements is null ? "points" : "of " + Requirements.Points + " points");
        var seasons = (
            Value: Detail.SeasonsWithPoints.ToString(CultureInfo.InvariantCulture),
            Label: "season".ToQuantity(Detail.SeasonsWithPoints, ShowQuantityAs.None) + " with points"
                + (Requirements is null ? null : " (" + Requirements.Seasons + " needed)"));
        var membership = (
            Value: years.ToString(CultureInfo.InvariantCulture) + (Detail.MembershipHistoryComplete ? string.Empty : "*"),
            Label: "membership " + "year".ToQuantity(years, ShowQuantityAs.None)
                + (Requirements is null ? null : " (" + Requirements.MembershipYears + " needed)"));

        return Layout == HallOfFamePointsDetailLayout.Profile
            ? [membership, points, seasons]
            : [points, seasons, membership];
    }
}
```

`HallOfFamePointsDetailView.razor.css` (new; the rules move from `HallOfFamePointsModal.razor.css`, plus the Profile rules)

```css
.pm-body h3 { font-family: var(--neba-font-display); font-size: 1rem; margin: 1.25rem 0 .5rem; color: var(--neba-gray-700); }
.pm-stats { display: flex; gap: 2rem; margin-bottom: 1.25rem; }
.pm-stat b { display: block; font-family: var(--neba-font-display); font-size: 1.75rem; line-height: 1.1; color: var(--neba-blue-700); }
.pm-stat span { font-size: .8125rem; color: var(--neba-gray-600); }
.pm-table { width: 100%; border-collapse: collapse; font-size: .9rem; }
.pm-table th { text-align: left; font-weight: 600; font-size: .8125rem; color: var(--neba-gray-600); padding: .4rem .5rem; border-bottom: 1px solid var(--neba-gray-300); }
.pm-table td { padding: .4rem .5rem; border-bottom: 1px solid var(--neba-gray-200); }
.pm-table tfoot td { font-weight: 700; border-bottom: 0; }
.pm-table tr.pm-reached td { background: #FFF8E6; font-weight: 600; }
.pm-table tr.pm-reached td:first-child { box-shadow: inset 4px 0 0 var(--neba-gold); }
.pm-reached-tag { display: inline-block; margin-left: .5rem; padding: .1rem .5rem; border-radius: 999px; background: var(--neba-blue-brand); color: var(--neba-gold); font-size: .6875rem; font-weight: 700; white-space: nowrap; }
.pm-r { text-align: right; font-variant-numeric: tabular-nums; }
.pm-years { columns: 2; column-gap: 1.5rem; margin: 0; padding: 0; list-style: none; font-size: .9rem; }
.pm-years li { display: flex; justify-content: space-between; gap: .75rem; padding: .25rem 0; border-bottom: 1px solid var(--neba-gray-200); break-inside: avoid; }
.pm-years span:last-child { color: var(--neba-gray-600); }
.pm-muted { color: var(--neba-gray-600); font-size: .9rem; margin: 0; }
.pm-note { margin: .75rem 0 0; padding: .625rem .875rem; background: #FFF8E6; border-left: 4px solid var(--neba-gold); font-size: .85rem; border-radius: 0 var(--neba-radius) var(--neba-radius) 0; }

/* Profile layout: three stats across (left edge, middle, right edge), each centered under a gold rule. */
.pm-stats--profile { display: grid; grid-template-columns: repeat(3, 1fr); gap: .75rem; }
.pm-stats--profile .pm-stat { min-width: 6rem; padding-top: .5rem; border-top: 3px solid var(--neba-gold); text-align: center; }
.pm-stats--profile .pm-stat:nth-child(1) { justify-self: start; }
.pm-stats--profile .pm-stat:nth-child(2) { justify-self: center; }
.pm-stats--profile .pm-stat:nth-child(3) { justify-self: end; }
.pm-stats--profile .pm-stat b { font-weight: 800; color: var(--neba-blue-brand); }

@media (max-width: 520px) {
    .pm-years { columns: 1; }
    .pm-stats { gap: 1.25rem; }
    .pm-stats--profile { gap: .75rem; }
    .pm-stats--profile .pm-stat { min-width: 0; }
    .pm-stats--profile .pm-stat b { font-size: 1.6rem; }
    .pm-stats--profile .pm-stat span { font-size: .75rem; line-height: 1.25; }
}
```

`HallOfFamePointsDetailLoader.razor` (new; the load, state and retry code moves out of `HallOfFamePointsModal.razor`)

```razor
@using Neba.Api.Contracts.HallOfFame
@using Neba.Website.Server.Services

@inject ApiExecutor ApiExecutor
@inject IHallOfFameApi HallOfFameApi

@if (_state == LoaderState.Loading)
{
    <div class="pm-state">
        <NebaLoadingIndicator />
        <div>Loading points…</div>
    </div>
}
else if (_state == LoaderState.Error)
{
    <div class="pm-state">
        <h3>Could not load points</h3>
        <p>Something went wrong fetching this bowler's record.</p>
        <button type="button" class="pm-retry" @onclick="RetryAsync">Retry</button>
    </div>
}
else if (_data is not null)
{
    if (EmptyContent is not null && _data.Breakdown.Count == 0 && _data.MembershipYears.Count == 0)
    {
        @EmptyContent
    }
    else
    {
        <HallOfFamePointsDetailView Detail="@_data" Requirements="@Requirements" Layout="@Layout" />
    }
}

@code {
    [Parameter, EditorRequired]
    public string BowlerId { get; set; } = string.Empty;

    /// <summary>The thresholds, when the caller has them. Null drops the "of 36" and "needed" text.</summary>
    [Parameter]
    public HallOfFamePointRequirements? Requirements { get; set; }

    [Parameter]
    public HallOfFamePointsDetailLayout Layout { get; set; }

    /// <summary>Shown instead of the data view when the bowler has no points and no membership years.</summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>Raised after each successful load, so a caller can read the detail (for example the inducted line).</summary>
    [Parameter]
    public EventCallback<HallOfFamePointsDetailViewModel> OnLoaded { get; set; }

    private enum LoaderState { Loading, Error, Data }

    private LoaderState _state = LoaderState.Loading;
    private HallOfFamePointsDetailViewModel? _data;
    private string? _lastLoadedBowlerId;

    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrEmpty(BowlerId) || BowlerId == _lastLoadedBowlerId)
        {
            return;
        }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _lastLoadedBowlerId = BowlerId;
        _state = LoaderState.Loading;
        _data = null;
        StateHasChanged();

        var result = await ApiExecutor.ExecuteAsync(
            "HallOfFame",
            "GetHallOfFamePointsDetail",
            ct => HallOfFameApi.GetHallOfFamePointsDetailAsync(BowlerId, ct));

        if (result.IsError)
        {
            _state = LoaderState.Error;
            return;
        }

        _data = result.Value.ToViewModel();
        _state = LoaderState.Data;
        await OnLoaded.InvokeAsync(_data);
    }

    private Task RetryAsync()
    {
        _lastLoadedBowlerId = null;
        return LoadAsync();
    }
}
```

`HallOfFamePointsDetailLoader.razor.css` (new)

```css
.pm-state { padding: 2.5rem 1rem; text-align: center; color: var(--neba-gray-600); }
.pm-state h3 { color: #1f2937; margin: .5rem 0 .25rem; }
.pm-retry { margin-top: .75rem; padding: .5rem 1rem; border: 1px solid var(--neba-blue-700); background: #fff; color: var(--neba-blue-700); border-radius: var(--neba-radius); cursor: pointer; font-weight: 600; }
```

`HallOfFamePointsModal.razor` (edit: the body and the load code leave; the header stays)

```razor
@using Neba.Api.Contracts.HallOfFame

<NebaModal IsOpen="@IsOpen" OnClose="@OnClose" MaxWidth="640px" AriaLabel="@("Hall of Fame points for " + BowlerName)">
    <HeaderContent>
        <div class="pm-title-wrap">
            <h2 class="pm-title">@BowlerName</h2>
            @if (_inductedLine is not null)
            {
                <span class="pm-flag">@_inductedLine</span>
            }
        </div>
    </HeaderContent>
    <ChildContent>
        <HallOfFamePointsDetailLoader BowlerId="@BowlerId"
                                      Requirements="@Requirements"
                                      OnLoaded="HandleLoaded" />
    </ChildContent>
</NebaModal>

@code {
    [Parameter, EditorRequired]
    public bool IsOpen { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnClose { get; set; }

    [Parameter, EditorRequired]
    public string BowlerId { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string BowlerName { get; set; } = string.Empty;

    /// <summary>The thresholds, when the caller has them. Null drops the "of 36" and "needed" text.</summary>
    [Parameter]
    public HallOfFamePointRequirements? Requirements { get; set; }

    private string? _inductedLine;

    protected override void OnParametersSet()
    {
        // The loader is gone while the modal is closed; drop the last bowler's flag so it does not flash on the next open.
        if (!IsOpen)
        {
            _inductedLine = null;
        }
    }

    private void HandleLoaded(HallOfFamePointsDetailViewModel detail)
        => _inductedLine = detail.InductedLine;
}
```

`HallOfFamePointsModal.razor.css` (edit: only the header rules stay)

```css
.pm-title { font-family: var(--neba-font-display); font-size: 1.375rem; font-weight: 800; margin: 0; }
.pm-flag { display: inline-flex; margin-top: .5rem; padding: .2rem .6rem; border-radius: 999px; background: var(--neba-blue-brand); color: var(--neba-gold); font-size: .75rem; font-weight: 600; }
```

`HallOfFameInducteeModal.razor` (new)

```razor
<NebaModal IsOpen="@IsOpen"
           OnClose="@OnClose"
           MaxWidth="640px"
           CompactSize="@true"
           AriaLabelledById="ip-title">
    <HeaderContent>
        @if (Inductee is not null)
        {
            <div class="ip-band">
                <img class="ip-logo" src="/images/neba-hof-480.jpg" alt="NEBA Hall of Fame" width="480" height="436" />
            </div>
            <div class="ip-id">
                <HallOfFameAvatar BowlerName="@Inductee.BowlerName"
                                  Ringed="@true"
                                  PhotoUri="@Inductee.PhotoUri"
                                  FocalX="@Inductee.PhotoFocalX"
                                  FocalY="@Inductee.PhotoFocalY"
                                  Zoom="@Inductee.PhotoZoom" />
                <h2 id="ip-title" class="ip-name">@Inductee.BowlerName</h2>
                <p class="ip-class">Class of @Inductee.InductionYear</p>
                <ul class="ip-chips">
                    @foreach (var category in Inductee.Categories)
                    {
                        <li><span class="ip-dot @HallOfFameCategories.DotClass(category)"></span>@category</li>
                    }
                </ul>
            </div>
        }
    </HeaderContent>
    <ChildContent>
        @if (Inductee is not null)
        {
            <HallOfFamePointsDetailLoader BowlerId="@Inductee.BowlerId"
                                          Layout="@HallOfFamePointsDetailLayout.Profile">
                <EmptyContent>
                    <p class="ip-empty">Points and membership details for Meritorious Service and Friend of NEBA inductees are not shown yet.</p>
                </EmptyContent>
            </HallOfFamePointsDetailLoader>
        }
    </ChildContent>
</NebaModal>

@code {
    [Parameter, EditorRequired]
    public bool IsOpen { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnClose { get; set; }

    /// <summary>The inductee to show. Null while the modal is closed.</summary>
    [Parameter]
    public HallOfFameInductionViewModel? Inductee { get; set; }
}
```

`HallOfFameInducteeModal.razor.css` (new)

```css
/* NebaModal's header wrapper: the band runs edge to edge and the close button floats over it. */
::deep .neba-modal-header { display: block; padding: 0; border-bottom: 0; position: relative; }
::deep .neba-modal-close { position: absolute; top: .75rem; right: .75rem; z-index: 2; width: 2rem; height: 2rem; border: 1px solid rgba(224, 180, 84, .55); border-radius: 50%; background: rgba(3, 26, 71, .55); color: var(--neba-gold); font-size: 1rem; }
::deep .neba-modal-close:hover { background: rgba(3, 26, 71, .8); color: var(--neba-gold); }
::deep .neba-modal-close:focus-visible { outline: 3px solid var(--neba-gold); outline-offset: 2px; }
::deep .neba-modal-body { padding: 1.25rem 1.5rem 1.75rem; }

.ip-band { position: relative; height: 96px; border-radius: .75rem .75rem 0 0; border-bottom: 4px solid var(--neba-gold); background: linear-gradient(160deg, var(--neba-blue-brand) 0%, #031A47 100%); }
.ip-logo { position: absolute; left: 1.25rem; top: .9rem; width: 64px; height: auto; padding: .3rem; border-radius: .5rem; background: #fff; }

.ip-id { --avatar-ring-gap: #fff; position: relative; display: flex; flex-direction: column; align-items: center; margin-top: -64px; padding: 0 1.5rem 1.25rem; border-bottom: 1px solid var(--neba-gray-200); text-align: center; }
.ip-id ::deep .inductee-photo { width: 128px; height: 128px; font-size: 2.9rem; box-shadow: 0 0 0 4px #fff, 0 0 0 7px var(--neba-gold), 0 8px 16px rgba(5, 39, 103, .3); }
.ip-name { margin: .25rem 0 .15rem; font-family: var(--neba-font-display); font-size: 1.75rem; font-weight: 800; color: var(--neba-blue-brand); }
.ip-class { margin: 0 0 .75rem; font-size: 1.05rem; font-weight: 700; color: var(--neba-gold-deep); }

.ip-chips { display: flex; flex-wrap: wrap; justify-content: center; gap: .35rem .5rem; margin: 0; padding: 0; list-style: none; font-size: .78rem; color: var(--neba-blue-brand); }
.ip-chips li { display: inline-flex; align-items: center; padding: .15rem .6rem; border: 1px solid rgba(5, 39, 103, .25); border-radius: 999px; }
.ip-dot { display: inline-block; width: .55em; height: .55em; margin-right: .4em; border-radius: 50%; background: var(--neba-gray-500); }
.ip-dot.hof-dot--superior { background: var(--neba-gold-deep); }
.ip-dot.hof-dot--meritorious { background: var(--neba-blue-700); }
.ip-dot.hof-dot--friend { background: #2F9E5B; }

.ip-empty { max-width: 34ch; margin: 0 auto; padding: 2rem 1rem; text-align: center; font-size: .95rem; color: var(--neba-gray-600); }

@media (max-width: 520px) {
    ::deep .neba-modal-body { padding: 1rem 1rem 1.5rem; }
    .ip-id ::deep .inductee-photo { width: 104px; height: 104px; font-size: 2.3rem; }
    .ip-name { font-size: 1.45rem; }
}
```

`HallOfFameAvatar.razor` (edit: the circle becomes a button when `OnOpen` is set)

```razor
@{
    RenderFragment portrait = @<text>
        @if (PhotoUri is not null)
        {
            <img class="inductee-photo__img" src="@PhotoUri.AbsoluteUri" alt="" style="@ImageStyle" loading="lazy" />
        }
        else
        {
            @GetInitials(BowlerName)
        }
    </text>;
}

<div class="avatar-wrap">
    @if (OnOpen.HasDelegate)
    {
        <button type="button"
                class="@(PhotoClass + " inductee-photo--button")"
                aria-label="@("View " + BowlerName + "'s Hall of Fame profile")"
                @onclick="OnOpen">
            @portrait
        </button>
    }
    else
    {
        <div class="@PhotoClass">
            @portrait
        </div>
    }

    @if (OnEdit.HasDelegate)
    {
        <HallOfFamePhotoEditButton BowlerName="@BowlerName" OnClick="OnEdit" />
    }
</div>

@code {
    // ...existing parameters unchanged, then:

    /// <summary>
    /// When set, the portrait is a button that raises this callback (opens the inductee's profile).
    /// </summary>
    [Parameter]
    public EventCallback OnOpen { get; set; }

    private string PhotoClass
        => Ringed ? "inductee-photo inductee-photo--ringed" : "inductee-photo";

    // ...ImageStyle and GetInitials unchanged
}
```

`HallOfFameAvatar.razor.css` (edit: add)

```css
/* The portrait as a button: drop the browser's button look, keep the circle. */
.inductee-photo--button { padding: 0; border: 0; appearance: none; font-family: inherit; cursor: pointer; }
.inductee-photo--button:focus-visible { outline: 3px solid var(--neba-gold); outline-offset: 6px; }
```

`HallOfFame.razor` (edit)

```razor
<HallOfFameAvatar BowlerName="@inductee.BowlerName"
                  Ringed="@true"
                  PhotoUri="@inductee.PhotoUri"
                  FocalX="@inductee.PhotoFocalX"
                  FocalY="@inductee.PhotoFocalY"
                  Zoom="@inductee.PhotoZoom"
                  OnOpen="@OpenCallbackFor(inductee)"
                  OnEdit="@EditCallbackFor(inductee)" />
<button type="button" class="inductee-name" @onclick="@(() => OpenProfile(inductee))">@inductee.BowlerName</button>
```

```razor
    <HallOfFameInducteeModal IsOpen="@(_viewing is not null)"
                             Inductee="@_viewing"
                             OnClose="CloseProfile" />

    <HallOfFamePhotoEditor ... />   @* unchanged *@
```

```csharp
    private HallOfFameInductionViewModel? _viewing;

    private EventCallback OpenCallbackFor(HallOfFameInductionViewModel inductee)
        => EventCallback.Factory.Create(this, () => OpenProfile(inductee));

    private void OpenProfile(HallOfFameInductionViewModel inductee)
        => _viewing = inductee;

    private void CloseProfile()
        => _viewing = null;
```

`HallOfFame.razor.css` (edit: the name plate is now a `<button>`; add three lines to the existing `.inductee-name` rule and two new rules)

```css
.inductee-name {
    /* ...existing declarations... */
    border: 0;
    font-family: inherit;
    cursor: pointer;
}

.inductee-name:hover { filter: brightness(1.06); }
.inductee-name:focus-visible { outline: 3px solid var(--hof-gold); outline-offset: 2px; }
```

`HallOfFame.razor.js` and `HallOfFamePoints.razor`: no change. The filter script reads `data-` attributes only, and the points page keeps its modal call as is.

### Tests

bUnit, `tests/Neba.Website.Tests/HallOfFame/` (UnitTest and Component traits, DisplayName, AAA comments, strict mocks, as in the existing files).

`HallOfFamePointsDetailViewTests.cs` (new; `[Component("Website.HallOfFame.HallOfFamePointsDetailView")]`)

```csharp
    private IRenderedComponent<HallOfFamePointsDetailView> Render(
        HallOfFamePointsDetailLayout layout,
        HallOfFamePointRequirements? requirements = null,
        HallOfFamePointsDetailResponse? response = null)
        => _ctx.Render<HallOfFamePointsDetailView>(p => p
            .Add(x => x.Detail, (response ?? HallOfFamePointsDetailResponseFactory.Create()).ToViewModel())
            .Add(x => x.Layout, layout)
            .Add(x => x.Requirements, requirements));

    [Fact(DisplayName = "Standing layout shows the points table with running totals and the threshold row")]
    public void Render_ShouldShowTable_WhenStandingLayout()
    {
        // Arrange
        var response = HallOfFamePointsDetailResponseFactory.Create(
            totalPoints: 42,
            breakdown:
            [
                new HallOfFamePointsEntryResponse { Source = "Singles Title", Date = new DateOnly(2018, 3, 1), Points = 30, RunningTotal = 30, ReachedThreshold = false },
                new HallOfFamePointsEntryResponse { Source = "Bowler of the Year", Date = new DateOnly(2019, 12, 31), Points = 12, RunningTotal = 42, ReachedThreshold = true }
            ]);

        // Act
        var cut = Render(HallOfFamePointsDetailLayout.Standing, response: response);

        // Assert
        cut.FindAll(".pm-table tbody tr").Count.ShouldBe(2);
        cut.FindAll(".pm-table tr.pm-reached").ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "Profile layout lists year and title with no point values, total, or threshold row")]
    public void Render_ShouldShowPlainList_WhenProfileLayout()
    {
        // Arrange
        // Act
        var cut = Render(HallOfFamePointsDetailLayout.Profile);

        // Assert
        cut.FindAll(".pm-table").ShouldBeEmpty();
        cut.FindAll(".pm-reached").ShouldBeEmpty();
        var items = cut.FindAll(".pm-years")[0].QuerySelectorAll("li");
        items.Select(li => li.TextContent).ShouldBe(["2017Bowler of the Year", "2019Singles Title"]);
    }

    [Fact(DisplayName = "Profile layout orders the stats membership years, points, seasons")]
    public void Render_ShouldOrderStats_WhenProfileLayout()
    {
        // Arrange
        var response = HallOfFamePointsDetailResponseFactory.Create(totalPoints: 43, seasonsWithPoints: 8);

        // Act
        var cut = Render(HallOfFamePointsDetailLayout.Profile, response: response);

        // Assert
        cut.FindAll(".pm-stat span").Select(span => span.TextContent)
            .ShouldBe(["membership years", "points", "seasons with points"]);
    }

    [Fact(DisplayName = "Standing layout orders the stats points, seasons, membership years")]
    public void Render_ShouldOrderStats_WhenStandingLayout() { /* same shape; expects points first */ }

    [Fact(DisplayName = "Shows the thresholds only when the caller passes requirements")]
    public void Render_ShouldShowThresholds_WhenRequirementsPassed() { /* of 36 points, (3 needed), (10 needed); none when null */ }

    [Fact(DisplayName = "Shows the membership note and asterisk only when the history is incomplete")]
    public void Render_ShouldShowMembershipNote_WhenHistoryIncomplete() { /* membershipHistoryComplete: false */ }
```

`HallOfFamePointsDetailLoaderTests.cs` (new; same setup as `HallOfFamePointsModalTests`: strict `IHallOfFameApi`, `ApiExecutor`, loose JS interop)

```csharp
    [Fact(DisplayName = "Fetches the detail for the bowler id")]
    public void Render_ShouldFetchDetail_ForBowlerId() { /* Verify GetHallOfFamePointsDetailAsync(BowlerId) Times.Once */ }

    [Fact(DisplayName = "Shows the loading state until the API answers")]
    public void Render_ShouldShowLoading_BeforeApiAnswers() { /* TaskCompletionSource, as in the modal tests */ }

    [Fact(DisplayName = "Shows an error with Retry when the API fails, and Retry loads the data")]
    public void Render_ShouldShowErrorAndRetry_WhenApiFails() { /* Times.Exactly(2) */ }

    [Fact(DisplayName = "Fetches again when the bowler id changes")]
    public void SetParameters_ShouldRefetch_WhenBowlerIdChanges() { /* cut.Render(p => p.Add(x => x.BowlerId, other)) */ }

    [Fact(DisplayName = "Raises OnLoaded with the detail after a successful load")]
    public void Render_ShouldRaiseOnLoaded_WhenLoaded() { /* capture the callback argument; InductedLine matches */ }

    [Fact(DisplayName = "Renders EmptyContent when it is set and the bowler has no points and no membership years")]
    public void Render_ShouldRenderEmptyContent_WhenDetailIsEmpty()
    {
        // Arrange
        SetupDetail(HallOfFamePointsDetailResponseFactory.Create(
            totalPoints: 0, seasonsWithPoints: 0, breakdown: [], membershipYears: []));

        // Act
        var cut = _ctx.Render<HallOfFamePointsDetailLoader>(p => p
            .Add(x => x.BowlerId, BowlerId)
            .Add(x => x.EmptyContent, builder => builder.AddMarkupContent(0, "<p class=\"empty\">Nothing here</p>")));

        // Assert
        cut.Find(".empty").TextContent.ShouldBe("Nothing here");
        cut.FindAll(".pm-stat").ShouldBeEmpty();
    }

    [Fact(DisplayName = "Renders the data view when EmptyContent is not set and the detail is empty")]
    public void Render_ShouldRenderDataView_WhenNoEmptyContent() { /* "No points yet." */ }
```

`HallOfFameInducteeModalTests.cs` (new; `[Component("Website.HallOfFame.HallOfFameInducteeModal")]`)

```csharp
    [Fact(DisplayName = "Renders nothing while closed")]
    public void Render_ShouldRenderNothing_WhenClosed() { /* isOpen: false, Inductee null; Markup.Trim() empty; no API call */ }

    [Fact(DisplayName = "Header shows name, class year, categories, and initials when there is no photo")]
    public void Render_ShouldShowHeader_WhenOpen()
    {
        // Arrange
        SetupDetail(HallOfFamePointsDetailResponseFactory.Create());
        var inductee = HallOfFameInductionViewModelFactory.Create(
            bowlerName: "Jane Smith",
            inductionYear: 2024,
            categories: [HallOfFameCategory.SuperiorPerformance, HallOfFameCategory.MeritoriousService]);

        // Act
        var cut = RenderModal(inductee);

        // Assert
        cut.Find(".ip-name").TextContent.ShouldBe("Jane Smith");
        cut.Find(".ip-class").TextContent.ShouldBe("Class of 2024");
        cut.FindAll(".ip-chips li").Select(li => li.TextContent).ShouldBe(["Superior Performance", "Meritorious Service"]);
        cut.Find(".inductee-photo").TextContent.Trim().ShouldBe("JS");
        cut.Find("img.ip-logo").GetAttribute("alt").ShouldBe("NEBA Hall of Fame");
    }

    [Fact(DisplayName = "Header shows the photo when the inductee has one")]
    public void Render_ShouldShowPhoto_WhenInducteeHasPhoto() { /* photoUri set; img.inductee-photo__img src */ }

    [Fact(DisplayName = "Loads the points by the inductee's bowler id")]
    public void Render_ShouldLoadPoints_ByBowlerId() { /* Verify GetHallOfFamePointsDetailAsync(inductee.BowlerId) */ }

    [Fact(DisplayName = "Shows the points as a plain list with no threshold row and no 'of 36' text")]
    public void Render_ShouldShowProfileLayout_WhenDataLoaded() { /* .pm-table empty, .pm-reached empty, Markup not contains " of 36" */ }

    [Fact(DisplayName = "Shows the empty message for an inductee with no points and no membership years")]
    public void Render_ShouldShowEmptyMessage_WhenNoData() { /* ".ip-empty" contains "not shown yet"; no ".pm-stat" */ }

    [Fact(DisplayName = "Shows the data for a Meritorious Service inductee who has points")]
    public void Render_ShouldShowData_WhenMeritoriousInducteeHasPoints() { /* categories: [MeritoriousService]; detail has points; no ".ip-empty" */ }
```

`HallOfFameAvatarTests.cs` (edit: add)

```csharp
    [Fact(DisplayName = "Should render the portrait as a labelled button and raise OnOpen when clicked")]
    public void Render_ShouldRenderButton_WhenOnOpenIsSet()
    {
        // Arrange
        var opened = false;
        var cut = Render(p => p.Add(x => x.OnOpen, EventCallback.Factory.Create(this, () => opened = true)));

        // Act
        var button = cut.Find("button.inductee-photo");
        button.Click();

        // Assert
        button.GetAttribute("aria-label").ShouldBe("View Jane Smith's Hall of Fame profile");
        opened.ShouldBeTrue();
    }

    [Fact(DisplayName = "Should render the portrait as a plain element when OnOpen is not set")]
    public void Render_ShouldRenderPlainElement_WhenOnOpenIsNotSet()
    {
        // Arrange
        // Act
        var cut = Render();

        // Assert
        cut.FindAll("button.inductee-photo").ShouldBeEmpty();
        cut.Find("div.inductee-photo").ShouldNotBeNull();
    }
```

`HallOfFameTests.cs` (edit: add; the page tests already set up the strict API mock and `SetupSuccessResponse`)

```csharp
    [Fact(DisplayName = "Should open the inductee profile when the name is clicked")]
    public void Click_ShouldOpenProfile_WhenNameClicked()
    {
        // Arrange
        var induction = HallOfFameInductionResponseFactory.Create(bowlerId: "01JZ8K3M4N5P6Q7R8S9T0V1W4B", bowlerName: "Jane Smith", year: 2019);
        SetupSuccessResponse([induction]);
        _mockApi
            .Setup(x => x.GetHallOfFamePointsDetailAsync("01JZ8K3M4N5P6Q7R8S9T0V1W4B", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponseFactory.Ok(HallOfFamePointsDetailResponseFactory.Create(bowlerId: "01JZ8K3M4N5P6Q7R8S9T0V1W4B")));   // use the file's existing helper for a Refit OK response
        var cut = _ctx.Render<HallOfFamePage>();

        // Act
        cut.Find("button.inductee-name").Click();

        // Assert
        cut.Find(".ip-class").TextContent.ShouldBe("Class of 2019");
    }

    [Fact(DisplayName = "Should open the inductee profile when the photo is clicked")]
    public void Click_ShouldOpenProfile_WhenPhotoClicked() { /* same setup; cut.Find("button.inductee-photo").Click() */ }

    [Fact(DisplayName = "Should close the inductee profile when the modal close button is clicked")]
    public void Click_ShouldCloseProfile_WhenCloseClicked() { /* cut.Find(".neba-modal-close").Click(); ".ip-name" absent */ }

    [Fact(DisplayName = "Should open the photo editor and not the profile when the edit button is clicked")]
    public void Click_ShouldOpenEditorOnly_WhenEditClicked() { /* editor authorization as in the existing edit tests; ".ip-name" absent */ }
```

`HallOfFameMappingExtensionsTests.cs` and `HallOfFameMappingExtensionsTests.ToViewModel_ShouldMapAllFields.verified.txt`: the snapshot gains a `BowlerId` line; regenerate with Verify and review the diff.

`HallOfFamePointsModalTests.cs`: no edit expected. If a test only covers loader behavior (error and Retry, loading), it may move to the loader tests; keep one modal test that the header flag shows after load and clears on close.

Playwright, `tests/e2e/mock-api/mock-api-server.ts` (add; `...201` and `...202` are the bowler ids Phase 1 gave Jane Smith and Bob Johnson; `...203` is left out on purpose so it returns 404 for the error test)

```typescript
// Detail for inducted bowlers. 203 (Alice Williams) is missing on purpose: the detail route returns 404 for her.
const MOCK_INDUCTEE_DETAILS = new Map<string, object>([
  ['01JX0000000000000000000201', {
    bowlerId: '01JX0000000000000000000201',
    bowlerName: 'Jane Smith',
    inductedCategories: ['Superior Performance'],
    totalPoints: 43,
    seasonsWithPoints: 3,
    membershipHistoryComplete: true,
    breakdown: [
      { source: 'Bowler of the Year', date: '2003-12-31', points: 12, runningTotal: 12, reachedThreshold: false },
      { source: 'Singles Title', date: '2009-03-10', points: 6, runningTotal: 18, reachedThreshold: false },
      { source: 'NEBA Masters Title', date: '2014-09-11', points: 6, runningTotal: 24, reachedThreshold: false },
    ],
    membershipYears: [
      { year: 1999, membershipType: 'New' },
      { year: 2000, membershipType: 'Renewal' },
    ],
  }],
  ['01JX0000000000000000000202', {
    bowlerId: '01JX0000000000000000000202',
    bowlerName: 'Bob Johnson',
    inductedCategories: ['Meritorious Service'],
    totalPoints: 0,
    seasonsWithPoints: 0,
    membershipHistoryComplete: true,
    breakdown: [],
    membershipYears: [],
  }],
]);
```

```typescript
  if (pathname.startsWith('/hall-of-fame/points/')) {
    const bowlerId = pathname.slice('/hall-of-fame/points/'.length);
    return MOCK_HALL_OF_FAME_POINTS_DETAIL(bowlerId) ?? MOCK_INDUCTEE_DETAILS.get(bowlerId) ?? null;
  }
```

`tests/e2e/HallOfFame.spec.ts` (add)

```typescript
  test.describe('inductee profile', () => {
    test.use({ viewport: { width: 1200, height: 800 } });

    test.beforeEach(async ({ page }) => {
      await page.goto('/hall-of-fame');
      await page.waitForSelector('.inductee-item');
    });

    test('opens from the name with the header and a plain points list', async ({ page }) => {
      await page.locator('.inductee-name', { hasText: 'Jane Smith' }).click();

      const dialog = page.getByRole('dialog', { name: 'Jane Smith' });
      await expect(dialog.getByText('Class of 2024')).toBeVisible();
      await expect(dialog.locator('.ip-chips li')).toHaveText(['Superior Performance']);
      await expect(dialog.locator('.pm-stat')).toHaveCount(3);
      await expect(dialog.getByText('Singles Title')).toBeVisible();
      await expect(dialog.locator('.pm-reached')).toHaveCount(0);
      await expect(dialog).not.toContainText('of 36');
    });

    test('opens from the photo circle', async ({ page }) => {
      await page.getByRole('button', { name: "View Jane Smith's Hall of Fame profile" }).click();
      await expect(page.getByRole('dialog', { name: 'Jane Smith' })).toBeVisible();
    });

    test('closes with Escape', async ({ page }) => {
      await page.locator('.inductee-name', { hasText: 'Jane Smith' }).click();
      const dialog = page.getByRole('dialog', { name: 'Jane Smith' });
      await expect(dialog).toBeVisible();

      await page.keyboard.press('Escape');

      await expect(dialog).toBeHidden();
    });

    test('shows a message for an inductee with no points', async ({ page }) => {
      await page.locator('.inductee-name', { hasText: 'Bob Johnson' }).click();
      await expect(page.getByRole('dialog', { name: 'Bob Johnson' })).toContainText('not shown yet');
    });

    test('shows an error with Retry when the points cannot load', async ({ page }) => {
      await page.locator('.inductee-name', { hasText: 'Alice Williams' }).click();
      await expect(page.getByRole('dialog', { name: 'Alice Williams' })).toContainText('Could not load points');
      await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible();
    });
  });
```

The existing `HallOfFame.spec.ts` selectors (`.inductee-name`, `.inductee-item`, `.inductee-category`) still match because the name keeps its class and the card keeps its structure. `HallOfFamePoints.spec.ts` (modal opens from the points page) must pass unchanged; it checks the extraction.

JS tests: none. No JS module changes.

### Docs

No help doc. Viewing a profile is public, and the editor help (`edit-hall-of-fame-photo`) is unchanged because the pencil stays on the card.

### Mockups

Chosen design: `docs/plans/mockups/hall-of-fame-inductee-profile/inductee-profile.html` (the "Portrait" option; the other two were dropped). A slim navy band holds a small logo tile. The large gold-ringed photo or initials overlap the band onto a white, centered block with the name, "Class of {year}" in gold, and category chips. The body has three stats in a three-column grid: membership years at the left edge, points in the middle, seasons with points at the right edge, each with its number and label centered under its gold rule. Then "Points earned" and "Membership years", each as a two-column list on desktop (year left, title or years' membership type right) and one column on phone. There are no point values, running totals, total row, or 36-point highlight. On phone the stats stay on one row in the same arrangement ("seasons with points" wraps to two lines). The real CSS uses a `@media (max-width: 520px)` rule for the one-column lists, as the points modal does today; the mockup uses a container query only so its Phone switch works. The bar at the top of the file switches Superior Performance data, the Meritorious empty state, loading, error, initials or photo, and desktop or phone width.

### Deferred or out of scope

- Content for Meritorious Service and Friend of NEBA profiles.
- Deep links to a profile (no `/hall-of-fame/{id}` route).

## Commit plan

Seven commits, in order. Each builds and passes its own tests; stage only that commit's files by name. Every message ends with `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`. Paths under `src/Neba.Website.Server/HallOfFame/` are shortened to `HallOfFame/`; the same for `tests/Neba.Website.Tests/HallOfFame/` as `Website.Tests/`.

1. **Add Hall of Fame inductee profile plan**
   - Files: `docs/plans/hall-of-fame-inductee-profile.md`, `docs/plans/mockups/hall-of-fame-inductee-profile/inductee-profile.html`.
   - Why: the plan and the confirmed mockup travel with the work they describe.

2. **Return the bowler id from the inductions list**
   - Files: `src/Neba.Api/Features/HallOfFame/ListHallOfFameInductions/` (`HallOfFameInductionDto.cs`, `ListHallOfFameInductionsQueryHandler.cs`, `ListHallOfFameInductionsEndpoint.cs`), `src/Neba.Api.Contracts/HallOfFame/ListHallOfFameInductions/HallOfFameInductionResponse.cs`, `tests/Neba.TestFactory/HallOfFame/HallOfFameInductionDtoFactory.cs`, `tests/Neba.TestFactory/HallOfFame/HallOfFameInductionResponseFactory.cs`, `tests/Neba.Api.Tests/Features/HallOfFame/ListHallOfFameInductions/` (`ListHallOfFameInductionsQueryHandlerTests.cs`, `ListHallOfFameInductionsEndpointTests.cs`, the endpoint `.verified.txt` snapshot), `tests/e2e/mock-api/mock-api-server.ts` (`bowlerId` on the interface and the four inductions).
   - Why: the profile needs the bowler id to load points, and the e2e mock fails every run without the new required field. Clear the cache by hand on deploy.

3. **Carry the bowler id into the inductee view model**
   - Files: `HallOfFame/HallOfFameInductionViewModel.cs`, `HallOfFame/HallOfFameMappingExtensions.cs`, `tests/Neba.TestFactory/HallOfFame/HallOfFameInductionViewModelFactory.cs`, `Website.Tests/HallOfFameMappingExtensionsTests.cs` and its `ToViewModel_ShouldMapAllFields.verified.txt`.
   - Why: the website side of the id, kept apart from the API change so each commit stays small.

4. **Extract the points detail view and loader from the points modal**
   - Files: `HallOfFame/HallOfFamePointsDetailView.razor` and `.razor.css` (new, table layout only), `HallOfFame/HallOfFamePointsDetailLoader.razor` and `.razor.css` (new), `HallOfFame/HallOfFamePointsModal.razor` and `.razor.css` (header only, `OnLoaded` for the flag), `Website.Tests/HallOfFamePointsDetailViewTests.cs` and `HallOfFamePointsDetailLoaderTests.cs` (new), `Website.Tests/HallOfFamePointsModalTests.cs` (only if loader-only tests move).
   - Why: one load and display path for both modals, so the next commit adds no duplicate code. The points page behaves as before (`HallOfFamePoints.spec.ts` and the modal tests pass unchanged, apart from tests that move).

5. **Add a profile layout to the points detail view**
   - Files: `HallOfFame/HallOfFamePointsDetailLayout.cs` (new), `HallOfFame/HallOfFamePointsDetailView.razor` and `.razor.css`, `HallOfFame/HallOfFamePointsDetailLoader.razor` (`Layout` and `EmptyContent` pass-through), `Website.Tests/HallOfFamePointsDetailViewTests.cs` and `HallOfFamePointsDetailLoaderTests.cs` (profile and empty-content cases).
   - Why: the plain two-column list, centered stats and empty-content swap, tested before any caller uses them.

6. **Add the Hall of Fame inductee profile modal**
   - Files: `HallOfFame/HallOfFameInducteeModal.razor` and `.razor.css` (new), `Website.Tests/HallOfFameInducteeModalTests.cs` (new).
   - Why: the header (logo band, ringed photo, class year, categories) and the profile body, tested on their own before the gallery opens them.

7. **Open the inductee profile from the Hall of Fame gallery**
   - Files: `HallOfFame/HallOfFameAvatar.razor` and `.razor.css` (`OnOpen`), `HallOfFame/HallOfFame.razor` and `.razor.css` (name button, `_viewing`, modal), `Website.Tests/HallOfFameAvatarTests.cs`, `Website.Tests/HallOfFameTests.cs`, `tests/e2e/mock-api/mock-api-server.ts` (inductee detail mock), `tests/e2e/HallOfFame.spec.ts`.
   - Why: the last wiring step, so the feature turns on in one reviewable change, with the page and browser tests.

If the work drifts from this list, update the list instead of merging commits.
