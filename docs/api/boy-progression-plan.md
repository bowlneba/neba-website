# Bowler of the Year Point Progression — Implementation Plan

## Goal

Replace the temporary `_boyProgression.cs` + per-season JSON files (`_boy{year}.json`) with a DB-backed calculation service that derives each bowler's cumulative point progression through the season from `HistoricalTournamentResult`. The output shape stays identical to the current `BowlerOfTheYearPointsRaceSeriesDto` so the rest of the stats pipeline is unchanged.

Eventually the service returns progressions for all six BOY race categories in a single call. This requires bowler demographic data (`DateOfBirth`, `Gender`) and a BOY category association on `SideCut` — neither exists yet — so the work is split into two phases.

---

## BOY Race Categories (existing `BowlerOfTheYearCategory` SmartEnum)

| Category | Bowler eligibility requirement | Notes |
|----------|-------------------------------|-------|
| Open | Any bowler | All stat-eligible tournaments |
| Senior | Age ≥ 50 **by each tournament's end date** | Stat-eligible + Senior/SeniorAndWomen tournaments |
| Super Senior | Age ≥ 60 **by each tournament's end date** | Stat-eligible + Senior/SeniorAndWomen tournaments |
| Woman | Gender = Female | Stat-eligible + Women/SeniorAndWomen tournaments |
| Rookie | First-year NEBA member | Stat-eligible tournaments; requires membership data — see Phase 2 notes |
| Youth | Age < 18 **by each tournament's end date** | Stat-eligible tournaments only |

> **Age eligibility is evaluated against `Tournament.EndDate`, not `StartDate`.** A bowler who turns 50 on the second day of a two-day tournament is eligible — they can bowl day 2 and earn Senior BOY points for that event. A bowler who turns 50 in June earns no Senior BOY points for tournaments that concluded before their birthday. The same rule applies to Super Senior (60) and Youth (under 18).

---

## Point Calculation Rules

These rules apply to a single `HistoricalTournamentResult` row when computing points for a target BOY race **R**:

### Tournament eligibility for race R

| Race | Eligible tournament criteria |
|------|-----------------------------|
| Open | `Tournament.StatsEligible = true` |
| Senior | `Tournament.StatsEligible = true` **OR** `TournamentType` ∈ {`Senior`, `SeniorAndWomen`} |
| Super Senior | `Tournament.StatsEligible = true` **OR** `TournamentType` ∈ {`Senior`, `SeniorAndWomen`} |
| Woman | `Tournament.StatsEligible = true` **OR** `TournamentType` ∈ {`Women`, `SeniorAndWomen`} |
| Rookie | `Tournament.StatsEligible = true` |
| Youth | `Tournament.StatsEligible = true` |

> Senior and Women-type tournaments are intentionally not stat-eligible for the Open race but do feed their respective specialty races.

### Points awarded per result row for race R

Given tournament is eligible for R and bowler is eligible for R:

| `SideCutId` | `SideCut.BowlerOfTheYearCategory` | Points for race R |
|-------------|----------------------------------|-------------------|
| `null` (main cut) | — | Listed `Points` from result row |
| not `null` | == R (this side cut targets race R) | Listed `Points` from result row |
| not `null` | != R (side cut targets a different race) | 5 (default entry points) |

If the tournament is not eligible for race R → **0 points** (row excluded).  
If the bowler is not eligible for race R → **0 points** (row excluded).

### Concrete example

> Senior tournament (`StatsEligible = false`), bowler finishes in the SuperSenior side cut (`SideCut.BowlerOfTheYearCategory = SuperSenior`), listed `Points = 40`.

| Race | Eligible? | Points |
|------|-----------|--------|
| Open | No — tournament not stat-eligible | 0 |
| Senior | Yes — Senior tournament | 5 — bowler is in a side cut whose category ≠ Senior |
| Super Senior | Yes — Senior tournaments count for the Super Senior race, and the side cut targets SuperSenior | **listed 40** |
| Woman | No — bowler presumed not female in this example | 0 |

> Simpler example: stat-eligible Singles tournament, bowler in a Senior side cut (`Points = 10`).

| Race | Points |
|------|--------|
| Open | 5 (in a side cut) |
| Senior | 10 (side cut targets Senior) |
| Super Senior | 5 (in a side cut, category ≠ SuperSenior) |
| Woman | 5 (in a side cut, category ≠ Woman) — only if bowler is female |

---

## Prerequisites

The following changes are required before Phase 2 can be implemented. They are **not** needed for Phase 1 (Open BOY only).

### 1. `Bowler`: add `DateOfBirth` and `Gender`

The `Bowler` entity XML docs explicitly note these are deferred until the org management software migration. When added:

- `DateOfBirth: DateOnly?` — nullable until migrated
- `Gender: Gender?` — existing `Gender` SmartEnum; nullable until migrated

Add EF configuration column + migration. The progression service filters per-bowler per-tournament using `DateOfBirth` evaluated against `Tournament.StartDate` for age-gated races.

### 2. `SideCut` → `BowlerOfTheYearCategory` relationship

The known side cuts are Senior, Super Senior, and Woman. Because the set is small and fixed, the BOY category for a side cut is derived by name rather than adding a new DB column:

```csharp
private static BowlerOfTheYearCategory? DeriveBoyCategory(string sideCutName) => sideCutName switch
{
    "Senior" => BowlerOfTheYearCategory.Senior,
    "Super Senior" => BowlerOfTheYearCategory.SuperSenior,
    "Woman" or "Women" => BowlerOfTheYearCategory.Woman,
    _ => null
};
```

`null` means the side cut has no BOY race target → 5 default points for all applicable races.

If new side cut types are added in the future that don't follow a simple name convention, add a `BowlerOfTheYearCategory? BoyCategory` column to `SideCut` at that point and retire this derivation.

---

## Phase 1 — Open BOY (no prerequisites)

**Goal**: replace `_boyProgression.cs` and the JSON files for the Open BOY race.

### Data required per result row

From `HistoricalTournamentResult` joined to:
- `Tournament` → `StatsEligible`, `StartDate` (for tournament date ordering)
- `Bowler` → `BowlerId`, `Name`
- `SideCut` (nullable) → `Id` (only need to know if a side cut exists; category not yet available)

### Point logic for Open BOY (Phase 1 simplification)

- `Tournament.StatsEligible = false` → skip (0 points)
- `SideCutId == null` → listed `Points`
- `SideCutId != null` → 5 (default entry points, hardcoded)

### Output

`IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>` — identical shape to current JSON approach. One entry per bowler, results ordered by tournament date, cumulative points computed in the service.

### Service signature (Phase 1)

```csharp
// Application layer — Neba.Application/Stats/BoyProgression/
Task<IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>> GetOpenBoyProgressionAsync(
    SeasonId seasonId,
    CancellationToken cancellationToken);
```

### Query method on `ITournamentQueries` (or new `IBoyProgressionQueries`)

```csharp
Task<IReadOnlyCollection<BoyProgressionResultDto>> GetHistoricalResultsForSeasonAsync(
    SeasonId seasonId,
    CancellationToken cancellationToken);
```

`BoyProgressionResultDto` shape:

| Property | Type | Source |
|----------|------|--------|
| `BowlerId` | `BowlerId` | Bowler.Id |
| `BowlerName` | `Name` | Bowler.Name |
| `TournamentId` | `TournamentId` | Tournament.Id |
| `TournamentName` | `string` | Tournament.Name |
| `TournamentDate` | `DateOnly` | Tournament.StartDate |
| `StatsEligible` | `bool` | Tournament.StatsEligible |
| `TournamentType` | `TournamentType` | Tournament.TournamentType |
| `Points` | `int` | HistoricalTournamentResult.Points |
| `SideCutId` | `int?` | HistoricalTournamentResult.SideCutId |
| `SideCutBoyCategory` | `BowlerOfTheYearCategory?` | SideCut.BoyCategory (null in Phase 1 — field not yet on SideCut) |

### Integration

Update `GetSeasonStatsQueryHandler` (currently calls `_BowlerOfTheYearProgression.GetBowlerOfTheYearProgressionAsync`) to call the new service instead. Remove `_boyProgression.cs` and the JSON files once Phase 1 is validated.

---

## Phase 2 — All BOY Race Categories

**Requires**: both prerequisites above (Bowler demographics + SideCut BOY category).

### Service signature (Phase 2)

```csharp
Task<IReadOnlyDictionary<BowlerOfTheYearCategory, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>>>
    GetBoyProgressionAsync(SeasonId seasonId, CancellationToken cancellationToken);
```

Single call; returns one progression collection per category. The caller (stats handler) replaces its current single Open BOY call with this and distributes results by category.

### Additional data required (beyond Phase 1)

- `Bowler.DateOfBirth` — for Senior (≥50), SuperSenior (≥60), Youth (<18) eligibility per tournament date
- `Bowler.Gender` — for Woman eligibility
- `SideCut.BoyCategory` — for side cut point routing (full points to target race, 5 to others)
- Rookie eligibility: requires membership data (first-year member flag) — **defer until membership migration**; exclude Rookie from Phase 2 initially

### Bowler eligibility evaluation

Age-gated races evaluate eligibility against `Tournament.EndDate` (not `StartDate`, and not a fixed age at season start). A bowler who turns 50 mid-season earns Senior BOY points only for tournaments whose `EndDate` falls on or after their 50th birthday. This correctly handles multi-day tournaments: a bowler turning 50 on day 2 of a two-day event is eligible for that event.

### Per-race point computation

The full rules table from the "Point Calculation Rules" section above applies. The implementation loops over all races per result row and applies the three-way branch (0 / listed / 5) to each.

### Notes on Rookie

Rookie BOY requires knowing whether the bowler is in their first year of NEBA membership. This data lives in the org management software (not yet migrated). Rookie is excluded from Phase 2 and tracked as a separate backlog item.

---

## Implementation Order

### Phase 1

1. Add `IBoyProgressionQueries` interface + `GetHistoricalResultsForSeasonAsync` implementation in `TournamentQueries` (or new class)
2. `BoyProgressionResultDto`
3. `BowlerOfTheYearProgressionService` with Open BOY logic
4. Register service in DI
5. Update `GetSeasonStatsQueryHandler` to call the new service
6. Validate output matches existing JSON-derived results
7. Delete `_boyProgression.cs` and JSON files

### Phase 2 (after prerequisites land)

1. Add `DateOfBirth` + `Gender` to `Bowler` + migration
2. Add `BoyCategory` to `SideCut` + migration + seed existing side cuts with correct categories
3. Extend query to include `Bowler.DateOfBirth`, `Bowler.Gender`, `SideCut.BoyCategory`
4. Add per-race eligibility logic to `BowlerOfTheYearProgressionService`
5. Change return type to `IReadOnlyDictionary<BowlerOfTheYearCategory, ...>`
6. Update stats handler to consume all categories

---

## Files to Delete After Phase 1

- `src/Neba.Application/Stats/_boyProgression.cs`
- `src/Neba.Application/Stats/_boy{year}.json` (all seasons)

---

## Concrete Implementation Plan — Phase 1

This section captures all decisions made in conversation, is self-contained, and is the authoritative implementation reference. Treat the sections above as domain rules; treat this section as the build guide.

---

### Decisions Made

| Question | Decision | Rationale |
| --- | --- | --- |
| Where does the new query method live? | `IStatsQueries` + `StatsQueries` | The result is stats data used only for stats computation. No justification for a new single-method interface when `IStatsQueries` already covers this domain. |
| What happens to `ISeasonStatsService.GetBowlerOfTheYearRaceAsync`? | Remove it entirely | It was explicitly marked temporary. The handler injects the new dedicated service directly. `IBowlerQueries` can also be removed from the handler. |
| How is the result cached? | Single `HybridCache` entry per season inside `BowlerOfTheYearProgressionService` holds all races | One DB fetch populates all six races. 14-day expiration, same tag set as season stats. |
| Where does the new service live? | `Neba.Application/Stats/BoyProgression/` | Per the plan doc and consistent with Application layer service organization. |
| How many races does the service return? | All six (`Open`, `Senior`, `SuperSenior`, `Woman`, `Youth`, `Rookie`) | Structure is defined now; non-Open races return empty until Phase 2 prerequisites land. |
| Key `(TournamentId, BowlerId)` on `HistoricalTournamentResult` | One result row per bowler per tournament | A bowler is either in the main cut (`SideCutId = null`) or a specific side cut (`SideCutId != null`). No multiple rows per bowler per tournament. |

---

### Race Population Status — Phase 1

| Race | Phase 1 output | Blocked on |
| --- | --- | --- |
| Open | Fully computed from DB | Nothing |
| Senior | Empty | `Bowler.DateOfBirth` (Phase 2 prerequisite) |
| Super Senior | Empty | `Bowler.DateOfBirth` (Phase 2 prerequisite) |
| Woman | Empty | `Bowler.Gender` (Phase 2 prerequisite) |
| Youth | Empty | `Bowler.DateOfBirth` (Phase 2 prerequisite) |
| Rookie | Empty | Membership data (deferred past Phase 2) |

All six keys are always present in the returned dictionary. The UI renders a race tab as empty when its collection is empty.

---

### Data Shape

Each `HistoricalTournamentResult` row carries a single `SideCutId?`. For Open BOY:

- `StatsEligible = false` → skip (0 points)
- `StatsEligible = true`, `SideCutId == null` → main cut → use listed `Points`
- `StatsEligible = true`, `SideCutId != null` → side cut → 5 points (hardcoded for Phase 1)

`CumulativePoints` in the output DTO is the running total across all a bowler's included tournaments ordered by `TournamentDate`.

---

### Files to Create

#### `src/Neba.Application/Stats/BoyProgression/BoyProgressionResultDto.cs`

One row per `HistoricalTournamentResult` row. Shape serves the BOY progression computation only — no reuse intended.

```csharp
using Neba.Domain.Bowlers;
using Neba.Domain.Tournaments;

namespace Neba.Application.Stats.BoyProgression;

public sealed record BoyProgressionResultDto
{
    public required BowlerId BowlerId { get; init; }
    public required Name BowlerName { get; init; }
    public required TournamentId TournamentId { get; init; }
    public required string TournamentName { get; init; }
    public required DateOnly TournamentDate { get; init; }
    public required bool StatsEligible { get; init; }
    public required TournamentType TournamentType { get; init; }
    public required int Points { get; init; }
    public required int? SideCutId { get; init; }
}
```

#### `src/Neba.Application/Stats/BoyProgression/IBowlerOfTheYearProgressionService.cs`

```csharp
using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Seasons;

namespace Neba.Application.Stats.BoyProgression;

internal interface IBowlerOfTheYearProgressionService
{
    Task<IReadOnlyDictionary<BowlerOfTheYearCategory, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>>>
        GetAllProgressionsAsync(SeasonId seasonId, CancellationToken cancellationToken);
}
```

#### `src/Neba.Application/Stats/BoyProgression/BowlerOfTheYearProgressionService.cs`

```csharp
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using Neba.Application.Caching;
using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Seasons;

namespace Neba.Application.Stats.BoyProgression;

internal sealed class BowlerOfTheYearProgressionService(
    IStatsQueries statsQueries,
    HybridCache cache,
    ILogger<BowlerOfTheYearProgressionService> logger)
    : IBowlerOfTheYearProgressionService
{
    private readonly IStatsQueries _statsQueries = statsQueries;
    private readonly HybridCache _cache = cache;
    private readonly ILogger<BowlerOfTheYearProgressionService> _logger = logger;

    public async Task<IReadOnlyDictionary<BowlerOfTheYearCategory, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>>>
        GetAllProgressionsAsync(SeasonId seasonId, CancellationToken cancellationToken)
    {
        var cacheDescriptor = CacheDescriptors.Stats.BoyProgressions(seasonId);

        return await _cache.GetOrCreateAsync(
            key: cacheDescriptor.Key,
            factory: async (cancel) =>
            {
                _logger.LogCacheMiss(cacheDescriptor.Key);
                var results = await _statsQueries.GetBoyProgressionResultsForSeasonAsync(seasonId, cancel);
                return ComputeAllProgressions(results);
            },
            options: new HybridCacheEntryOptions { Expiration = TimeSpan.FromDays(14) },
            tags: cacheDescriptor.Tags,
            cancellationToken: cancellationToken);
    }

    internal static IReadOnlyDictionary<BowlerOfTheYearCategory, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>>
        ComputeAllProgressions(IReadOnlyCollection<BoyProgressionResultDto> results)
    {
        return new Dictionary<BowlerOfTheYearCategory, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>>
        {
            [BowlerOfTheYearCategory.Open] = ComputeOpenProgression(results),
            [BowlerOfTheYearCategory.Senior] = [],       // Phase 2: requires Bowler.DateOfBirth
            [BowlerOfTheYearCategory.SuperSenior] = [],  // Phase 2: requires Bowler.DateOfBirth
            [BowlerOfTheYearCategory.Woman] = [],        // Phase 2: requires Bowler.Gender
            [BowlerOfTheYearCategory.Youth] = [],        // Phase 2: requires Bowler.DateOfBirth
            [BowlerOfTheYearCategory.Rookie] = [],       // Deferred: requires membership data
        };
    }

    private static IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto> ComputeOpenProgression(
        IReadOnlyCollection<BoyProgressionResultDto> results)
    {
        var eligibleResults = results
            .Where(r => r.StatsEligible)
            .GroupBy(r => r.BowlerId);

        return [.. eligibleResults.Select(group =>
        {
            var cumulativePoints = 0;
            var tournamentResults = group
                .OrderBy(r => r.TournamentDate)
                .Select(r =>
                {
                    cumulativePoints += r.SideCutId.HasValue ? 5 : r.Points;
                    return new BowlerOfTheYearPointsRaceTournamentDto
                    {
                        TournamentName = r.TournamentName,
                        TournamentDate = r.TournamentDate,
                        CumulativePoints = cumulativePoints
                    };
                })
                .ToArray();

            return new BowlerOfTheYearPointsRaceSeriesDto
            {
                BowlerId = group.Key,
                BowlerName = group.First().BowlerName,
                Results = tournamentResults
            };
        })];
    }
}

internal static partial class BowlerOfTheYearProgressionServiceLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Cache miss for key '{CacheKey}', executing query handler")]
    public static partial void LogCacheMiss(
        this ILogger<BowlerOfTheYearProgressionService> logger,
        string cacheKey);
}
```

> **Note on `internal static`**: `ComputeAllProgressions` and `ComputeOpenProgression` are `internal` / `private static` so unit tests can call `ComputeAllProgressions` directly without hitting the cache or the DB.

---

### Files to Modify

#### `src/Neba.Application/Stats/GetSeasonStats/SeasonStatsDto.cs`

```csharp
// Before
public required IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto> BowlerOfTheYearRace { get; init; }

// After — rename and change type; caller must be updated
public required IReadOnlyDictionary<BowlerOfTheYearCategory, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>> BowlerOfTheYearRaces { get; init; }
```

Add `using Neba.Domain.Seasons;` if not already present.

#### `src/Neba.Application/Stats/IStatsQueries.cs`

Add to the interface:

```csharp
/// <summary>
/// Retrieves all historical tournament result rows for the given season, joined to Tournament and Bowler.
/// Used exclusively to compute BOY point progressions.
/// </summary>
Task<IReadOnlyCollection<BoyProgressionResultDto>> GetBoyProgressionResultsForSeasonAsync(
    SeasonId seasonId,
    CancellationToken cancellationToken);
```

Add `using Neba.Application.Stats.BoyProgression;` to the usings.

#### `src/Neba.Infrastructure/Database/Queries/StatsQueries.cs`

Add a second `IQueryable` field:

```csharp
private readonly IQueryable<HistoricalTournamentResult> _historicalTournamentResults
    = appDbContext.HistoricalTournamentResults.AsNoTracking();
```

Add the implementation:

```csharp
public async Task<IReadOnlyCollection<BoyProgressionResultDto>> GetBoyProgressionResultsForSeasonAsync(
    SeasonId seasonId,
    CancellationToken cancellationToken)
    => await _historicalTournamentResults
        .Where(r => r.Tournament.SeasonId == seasonId)
        .OrderBy(r => r.Tournament.StartDate)
        .Select(r => new BoyProgressionResultDto
        {
            BowlerId = r.Bowler.Id,
            BowlerName = r.Bowler.Name,
            TournamentId = r.Tournament.Id,
            TournamentName = r.Tournament.Name,
            TournamentDate = r.Tournament.StartDate,
            StatsEligible = r.Tournament.StatsEligible,
            TournamentType = r.Tournament.TournamentType,
            Points = r.Points,
            SideCutId = r.SideCutId
        })
        .ToListAsync(cancellationToken);
```

Add required usings: `Neba.Application.Stats.BoyProgression`, `Neba.Infrastructure.Database.Entities`.

#### `src/Neba.Application/Stats/SeasonStatsService.cs`

- Remove `GetBowlerOfTheYearRaceAsync` method and its `ISeasonStatsService` declaration.
- Remove the `IBowlerQueries` constructor parameter and field.
- Remove the XML doc comment referencing the temporary state.

#### `src/Neba.Application/Stats/GetSeasonStats/GetSeasonStatsQueryHandler.cs`

- Remove `IBowlerQueries bowlerQueries` constructor parameter and `_bowlerQueries` field.
- Add `IBowlerOfTheYearProgressionService boyProgressionService` constructor parameter and field.
- Replace the call:

```csharp
// Before
var bowlerOfTheYearRace = await _seasonStatsService.GetBowlerOfTheYearRaceAsync(season, _bowlerQueries, cancellationToken);

// After
var bowlerOfTheYearRaces = await _boyProgressionService.GetAllProgressionsAsync(season.Id, cancellationToken);
```

- Update `SeasonStatsDto` initializer: `BowlerOfTheYearRaces = bowlerOfTheYearRaces`.
- Remove the "temporary" XML doc comment.

#### `src/Neba.Api/Stats/GetSeasonStats/GetSeasonStatsEndpoint.cs` and response type

Replace the single `BowlerOfTheYearPointsRace` property with six named properties (one per race). Named properties avoid magic strings at the API boundary:

```csharp
// In the response record/class
public required IReadOnlyCollection<PointsRaceSeriesResponse> OpenPointsRace { get; init; }
public required IReadOnlyCollection<PointsRaceSeriesResponse> SeniorPointsRace { get; init; }
public required IReadOnlyCollection<PointsRaceSeriesResponse> SuperSeniorPointsRace { get; init; }
public required IReadOnlyCollection<PointsRaceSeriesResponse> WomenPointsRace { get; init; }
public required IReadOnlyCollection<PointsRaceSeriesResponse> YouthPointsRace { get; init; }
public required IReadOnlyCollection<PointsRaceSeriesResponse> RookiePointsRace { get; init; }
```

In the endpoint mapping, project from the dictionary:

```csharp
static IReadOnlyCollection<PointsRaceSeriesResponse> MapRace(
    IReadOnlyDictionary<BowlerOfTheYearCategory, IReadOnlyCollection<BowlerOfTheYearPointsRaceSeriesDto>> races,
    BowlerOfTheYearCategory category)
    => [.. races[category].Select(race => new PointsRaceSeriesResponse
    {
        BowlerId = race.BowlerId.Value.ToString(),
        BowlerName = race.BowlerName.ToString(),
        Results = [.. race.Results.Select(r => new PointsRaceTournamentResponse
        {
            TournamentName = r.TournamentName,
            TournamentDate = r.TournamentDate,
            CumulativePoints = r.CumulativePoints
        })]
    })];
```

#### `src/Neba.Application/ApplicationConfiguration.cs`

In `AddServices()`:

```csharp
services.AddScoped<IBowlerOfTheYearProgressionService, BowlerOfTheYearProgressionService>();
```

#### `src/Neba.Application/Caching/CacheDescriptors.cs`

Inside the `Stats` class, add:

```csharp
public static CacheDescriptor BoyProgressions(SeasonId seasonId)
    => new()
    {
        Key = $"neba:stats:seasons:{seasonId}:boy-progressions",
        Tags = ["neba", "neba:stats", "neba:stats:seasons", $"neba:stats:seasons:{seasonId}"]
    };
```

---

### UI Changes

#### Context

The current sidebar chart widget (`stats-points-race-card`) shows a compact Open BOY chart (top 3 bowlers) that opens a modal with top 10. This widget is removed entirely. The chart is replaced by a "Points Progression" link rendered inline next to the section header for each BOY standings category. Clicking the link opens a modal containing a race selector (tab row) and the full-width chart for the selected race.

#### Race labels

The UI label for the Open category is "Bowler of the Year", not "Open" (existing convention — see memory note).

| Category | UI label |
| --- | --- |
| Open | Bowler of the Year |
| Senior | Senior |
| Super Senior | Super Senior |
| Woman | Women |
| Youth | Youth |
| Rookie | Rookie |

#### `StatsPageViewModel` changes

Replace:

```csharp
public required IReadOnlyCollection<PointsRaceSeriesViewModel> BowlerOfTheYearPointsRace { get; init; }
```

With six named properties (parallel to the API response names):

```csharp
public required IReadOnlyCollection<PointsRaceSeriesViewModel> OpenPointsRace { get; init; }
public required IReadOnlyCollection<PointsRaceSeriesViewModel> SeniorPointsRace { get; init; }
public required IReadOnlyCollection<PointsRaceSeriesViewModel> SuperSeniorPointsRace { get; init; }
public required IReadOnlyCollection<PointsRaceSeriesViewModel> WomenPointsRace { get; init; }
public required IReadOnlyCollection<PointsRaceSeriesViewModel> YouthPointsRace { get; init; }
public required IReadOnlyCollection<PointsRaceSeriesViewModel> RookiePointsRace { get; init; }
```

Update `StatsApiService` to map the six API response properties to these six viewmodel properties.

#### `SeasonStats.razor` — main stats page changes

**Remove** the entire `stats-points-race-card` sidebar widget block and its associated `OpenPointsRaceModal` / `OnPointsRaceWidgetKeyDown` methods.

**Add** a "Points Progression" link (button styled as a text link) next to the section header for each BOY standings group (Bowler of the Year, Senior, Super Senior, Women, Youth, Rookie). Each link opens the shared progression modal pre-selected on that race.

**Add** a new `RenderProgressionModal(string raceLabel)` render fragment containing:

- A `_selectedProgressionRace` local field (default `"Open"` or `"Bowler of the Year"` label — whichever matches the link that was clicked).
- A tab row with one tab button per race label. Clicking a tab updates `_selectedProgressionRace`.
- Below the tabs: if the selected race collection is non-empty, render `<PointsRaceChart>` with `ShowCategoryLabels="true"` and **top 10 bowlers by final cumulative points** (sort the collection by last `CumulativePoints` descending, take 10). If empty, render a `<p>No data available for this race yet.</p>` message.

The modal is opened via the existing `OpenModal(title, content, isWide: true)` helper. Title: `"Points Race"`.

**Result**: No persistent chart on the stats page. The chart is one click away from the standings section that users are already reading.

---

#### Individual stats page changes

The individual stats page is populated client-side within `StatsApiService.MapToIndividualStatsPageViewModel` — it filters the same `GetSeasonStatsResponse` that the main stats page receives. No new API endpoint or query is needed.

Currently `IndividualStatsPageViewModel` carries a single nullable `BowlerOfTheYearPointsRace` (Open only). This is replaced by a collection of per-race progressions that includes only races where this bowler has data.

##### `IndividualStatsPageViewModel` changes

Remove:

```csharp
public PointsRaceSeriesViewModel? BowlerOfTheYearPointsRace { get; init; }
```

Add:

```csharp
public IReadOnlyCollection<IndividualBoyProgressionViewModel> BoyProgressions { get; init; } = [];
```

New supporting record (new file `IndividualBoyProgressionViewModel.cs`):

```csharp
namespace Neba.Website.Server.Stats;

public sealed record IndividualBoyProgressionViewModel
{
    public required string RaceLabel { get; init; }
    public required PointsRaceSeriesViewModel BowlerSeries { get; init; }
    public required PointsRaceSeriesViewModel? LeaderSeries { get; init; }
}
```

`LeaderSeries` is `null` when the bowler IS the race leader. The chart then renders just one series. When non-null, the chart renders both series (bowler + leader) so the bowler can see how they compare.

##### `StatsApiService` mapping changes

In `MapToIndividualStatsPageViewModel`, replace the `BowlerOfTheYearPointsRace` mapping with:

```csharp
BoyProgressions = BuildIndividualBoyProgressions(response, bowlerId),
```

Add a private static helper:

```csharp
private static IReadOnlyCollection<IndividualBoyProgressionViewModel> BuildIndividualBoyProgressions(
    GetSeasonStatsResponse response, string bowlerId)
{
    var result = new List<IndividualBoyProgressionViewModel>();

    TryAddRace(response.OpenPointsRace,     response.BowlerOfTheYear,     "Bowler of the Year", bowlerId, result);
    TryAddRace(response.SeniorPointsRace,   response.SeniorOfTheYear,     "Senior",             bowlerId, result);
    TryAddRace(response.SuperSeniorPointsRace, response.SuperSeniorOfTheYear, "Super Senior",   bowlerId, result);
    TryAddRace(response.WomenPointsRace,    response.WomanOfTheYear,      "Women",              bowlerId, result);
    TryAddRace(response.YouthPointsRace,    response.YouthOfTheYear,      "Youth",              bowlerId, result);
    TryAddRace(response.RookiePointsRace,   response.RookieOfTheYear,     "Rookie",             bowlerId, result);

    return result;
}

private static void TryAddRace(
    IReadOnlyCollection<PointsRaceSeriesResponse> allSeries,
    IReadOnlyCollection<BowlerOfTheYearStandingResponse> standings,
    string raceLabel,
    string bowlerId,
    List<IndividualBoyProgressionViewModel> result)
{
    var bowlerRaw = allSeries.FirstOrDefault(r => r.BowlerId == bowlerId);
    if (bowlerRaw is null)
        return;

    var leaderId = standings.FirstOrDefault()?.BowlerId;
    var leaderRaw = leaderId is not null && leaderId != bowlerId
        ? allSeries.FirstOrDefault(r => r.BowlerId == leaderId)
        : null;

    result.Add(new IndividualBoyProgressionViewModel
    {
        RaceLabel = raceLabel,
        BowlerSeries = MapSeries(bowlerRaw),
        LeaderSeries = leaderRaw is not null ? MapSeries(leaderRaw) : null,
    });
}

private static PointsRaceSeriesViewModel MapSeries(PointsRaceSeriesResponse raw) =>
    new()
    {
        BowlerId = raw.BowlerId,
        BowlerName = raw.BowlerName,
        Results = [.. raw.Results.Select(t => new PointsRaceTournamentViewModel
        {
            TournamentName = t.TournamentName,
            TournamentDate = t.TournamentDate,
            CumulativePoints = t.CumulativePoints
        })]
    };
```

> The standings collections are already ordered by points descending (the API renders them that way), so `standings.FirstOrDefault()` is always the current leader.

##### `IndividualStats.razor` changes

Replace the existing single BOY points race section with a loop over `_model.BoyProgressions`. For each entry:

- Render a labelled section header (`{entry.RaceLabel} Points Progression`).
- Render `<PointsRaceChart>` with `ShowCategoryLabels="true"`.
- `Series` = `entry.LeaderSeries is not null ? [entry.BowlerSeries, entry.LeaderSeries] : [entry.BowlerSeries]`.
- When `LeaderSeries` is non-null, a legend note clarifies which line is the bowler vs. the leader (the chart legend already labels by `BowlerName`, so this is automatic).

Only races present in `BoyProgressions` are rendered — no "no data" message needed since absent races simply don't appear.

---

### Files to Delete

- `src/Neba.Application/Stats/_boyProgression.cs`
- `src/Neba.Application/Stats/_boy2019.json`
- `src/Neba.Application/Stats/_boy2021.json`
- `src/Neba.Application/Stats/_boy2022.json`
- `src/Neba.Application/Stats/_boy2023.json`
- `src/Neba.Application/Stats/_boy2024.json`
- `src/Neba.Application/Stats/_boy2025.json`

> **No year 2020** — pandemic year, season did not run.

---

### DI Registration Summary

No new Infrastructure registrations needed — `IStatsQueries` is already registered as `StatsQueries` in `DatabaseConfiguration.AddQueries()`. Only Application needs updating:

```csharp
// ApplicationConfiguration.cs — AddServices()
services.AddScoped<ISeasonStatsService, SeasonStatsService>();
services.AddScoped<IBowlerOfTheYearProgressionService, BowlerOfTheYearProgressionService>(); // add this
```

---

### Unit Tests — `BowlerOfTheYearProgressionService`

File: `tests/Neba.Application.Tests/Stats/BoyProgression/BowlerOfTheYearProgressionServiceTests.cs`

Test `ComputeAllProgressions` directly (it is `internal static` — accessible from the test project via `InternalsVisibleTo`).

All tests: `[UnitTest]`, `[Component("Stats")]`, `DisplayName`, use Shouldly, use test factories.

Required test cases:

| Case | What to assert |
| --- | --- |
| Empty input | Open race empty; all other race keys present and empty |
| Single bowler, single main-cut result, stat-eligible | Open: `CumulativePoints == result.Points`; one series entry |
| Single bowler, single side-cut result, stat-eligible | Open: `CumulativePoints == 5` regardless of `Points` value |
| Non-stat-eligible tournament | Open: bowler absent from series |
| Single bowler, three tournaments in date order | Open: cumulative total is correct running sum at each step |
| Single bowler, tournaments arriving out of date order in input | Open: results still ordered by date; cumulative correct |
| Multiple bowlers | Open: each gets their own series; totals independent |
| Mix of main-cut and side-cut results across tournaments | Open: each row uses correct point rule; cumulative accurate |
| All races other than Open | Always return empty collection regardless of input (Phase 1) |

**Test factory** — create `BoyProgressionResultDtoFactory` in `Neba.TestFactory` with a `Create()` method (nullable params, sensible const defaults) and a `Bogus(int count, int? seed)` method.

---

### Infrastructure Integration Test

File: `tests/Neba.Infrastructure.Tests/Stats/BoyProgressionQueriesTests.cs`

Test `StatsQueries.GetBoyProgressionResultsForSeasonAsync` against the real database (Testcontainers). Seed:

- A season
- Two tournaments in that season — one `StatsEligible = true`, one `StatsEligible = false`
- Two bowlers with results in each tournament (one main-cut, one side-cut)

Assert:

- All four result rows are returned (query does not filter on `StatsEligible` — that is the service's responsibility)
- `BowlerId`, `TournamentId`, `Points`, `SideCutId`, `StatsEligible`, `TournamentType`, `TournamentDate` are mapped correctly
- Results are ordered by `TournamentDate` ascending
