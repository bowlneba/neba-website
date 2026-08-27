# Bowler of the Year Progression — Cover Current-Season Tournaments + Rookie Race

## Problem

`GetSeasonStatsQueryHandler` builds the BOY points-progression chart from **`HistoricalTournamentResult`** only (`GetSeasonStatsQueryHandler.cs:121-143`). That table is a one-time snapshot written by the legacy data migration (`data-migration/Website Port.linq`, `MigrateHistoricalTournamentResults`) covering seasons 2019–2025. Nothing writes to it going forward — `SyncTournamentResultsJob` (the job that keeps the *current* season's results up to date) only writes to `TournamentResult`, a separate table with a separate shape.

Net effect: the progression chart works for 2025 and earlier, and will always be empty for 2026 and later, regardless of whether `BowlerSeasonStats`/`TournamentResult` are otherwise populated correctly for the current season.

Separately, the Rookie race is stubbed out entirely today:

```csharp
[BowlerOfTheYearCategory.Rookie.Value] = [],  // Deferred: requires membership data
```

(`BowlerOfTheYearRaceCalculator.cs:18`) — even for 2019–2025, where the data to compute it already exists.

This plan covers both, since fixing the first properly (adding a second result source) is the natural point to also plug the eligibility gap that's been blocking the second.

## Goal

1. The progression chart (and any other consumer of `BoyProgressionResultDto`) draws from **both** `HistoricalTournamentResult` and `TournamentResult`, concatenated — no year/season cutoff check. A season simply has rows in whichever table(s) apply to it; in practice historical seasons only ever populate one table and current seasons the other, but the query doesn't need to know or care which.
2. Rookie-of-the-year progression is computed the same way historical Rookie standings already are (`SeasonStatsCalculator.ComputeBotyStandings(bs => bs.BowlerOfTheYearPoints, bs => bs.IsRookie)`), for both historical and current seasons.

## Why this wasn't already possible: the two tables don't line up

| Field | `HistoricalTournamentResult` | `TournamentResult` |
|---|---|---|
| Bowler key | `int BowlerId` | `BowlerId BowlerId` (strong ID) |
| Tournament key | `int TournamentId` | *(none — reached only via `Tournament` nav from `Season`/join)* |
| Place | `int? Place` | `int Place` (never null — see `TournamentResult.cs:29-35`, DNF bowlers still ranked) |
| Points | `int Points` | `int Points` |
| Side cut | `int? SideCutId` / `SideCut? SideCut` | **none** |

The side-cut gap already exists elsewhere and is already handled by precedent: `GetTournamentQueryHandler.cs:134-138` builds a `TournamentResultDto` for *current* (`TournamentResult`-backed) rows with `SideCutName = null, SideCutIndicator = null` unconditionally, then concatenates with the historical rows (which do populate side cut) — see lines 146-168 there for the exact "historical ∪ recorded" pattern this plan re-applies to `BoyProgressionResultDto`.

That's an acceptable, already-established simplification: recorded (current-season) results don't yet carry side-cut assignment at all, so `PointsForRace`'s side-cut-category logic (`BowlerOfTheYearRaceCalculator.cs:151-167`) will simply treat every current-season result as a main-cut result (`SideCutId == null` → full `Points`, no `5`-point floor). This matches today's real behavior for current tournaments (side cuts aren't assigned to `TournamentResult` at all yet), so it's not a regression — just something to note if/when side-cut tracking is added to `TournamentResult` in the future.

## Design

### 1. Extend `BoyProgressionResultDto` sourcing to union both tables

In `GetSeasonStatsQueryHandler.ComputeSeasonStatsAsync`, replace the single `_historicalTournamentResults` query (lines 121-143) with two projections into the same `BoyProgressionResultDto` shape, unioned:

```csharp
private readonly IQueryable<TournamentResult> _tournamentResults
    = appDbContext.TournamentResults.AsNoTracking();
```

```csharp
var historicalProgressionResults = _historicalTournamentResults
    .Where(result => result.Tournament.SeasonId == seasonId)
    .Select(result => new BoyProgressionResultDto
    {
        BowlerId = result.Bowler.Id,          // existing mapping unchanged
        // ... unchanged, same as today
        SideCutId = result.SideCutId,
        SideCutName = result.SideCut != null ? result.SideCut.Name : null
    });

var currentProgressionResults = _tournamentResults
    .Where(result => result.Tournament.SeasonId == seasonId)
    .Select(result => new BoyProgressionResultDto
    {
        BowlerId = result.BowlerId,
        BowlerName = result.Bowler.Name,
        BowlerDateOfBirth = result.Bowler.DateOfBirth,
        BowlerGender = result.Bowler.Gender == null ? null : result.Bowler.Gender.Value,
        TournamentId = result.Tournament.Id,
        TournamentName = result.Tournament.Name,
        TournamentDate = result.Tournament.StartDate,
        TournamentEndDate = result.Tournament.EndDate,
        StatsEligible = result.Tournament.StatsEligible,
        TournamentType = result.Tournament.TournamentType.Value,
        Points = result.Points,
        SideCutId = null,
        SideCutName = null
    });

var bowlerOfTheYearProgressions = await historicalProgressionResults
    .Concat(currentProgressionResults)
    .OrderBy(result => result.TournamentDate)
    .ToListAsync(cancellationToken);
```

Notes:
- `TournamentResult` today is only ever reached through `Tournament._results` (owned-collection style, per `Tournament.AddResult`) — there's no `DbSet<TournamentResult>` exposed on `AppDbContext` yet, so `_tournamentResults` as sketched above doesn't exist as a directly queryable source. Since this is read access for a query handler, not a write path through the aggregate, adding `public DbSet<TournamentResult> TournamentResults => Set<TournamentResult>();` to `AppDbContext` is fine here — it doesn't bypass any aggregate invariant (nothing is being created/mutated, only read and projected), it just needs a `Tournament` nav (already configured, since EF must already map the FK back to the owning `Tournament` for `TournamentResultConfiguration` to work) to reach `SeasonId`/`Name`/dates/`TournamentType`/`StatsEligible`. Add the `DbSet` alongside the other read-oriented sets on `AppDbContext` rather than routing this through `_tournaments.SelectMany(t => t.Results, ...)`.
- Two separate `IQueryable<T>.Select(...)` projections into the same anonymous/record shape, `Concat`ed, then `OrderBy` + single `ToListAsync` — EF Core translates `Concat` over two queries against different tables fine (it's a `UNION ALL` when both come from provider-translatable sources); no in-memory merge needed unless the second source (`Tournament.Results` navigation) turns out not to be directly queryable, in which case one side may need `ToListAsync` first and the union done client-side. Either way, the season's dataset is small (single-season tournament count), so a client-side merge is not a performance concern if the clean provider-level `Concat` isn't feasible.
- No year/date branching anywhere — a season's stats are simply whatever's in the two tables for that `SeasonId`. If a season somehow had rows in both tables (shouldn't happen given the migration is one-time and complete-tournament-sync is ongoing, but not structurally prevented), both would show up; that's fine and matches "just check both tables."

### 2. Rookie progression — add IsRookie eligibility, sourced from `BowlerSeasonStats`

`IsBowlerEligibleForRace` (`BowlerOfTheYearRaceCalculator.cs:115-137`) currently has no case for `Rookie` at all — it falls through to the final `Youth` branch's `&&`, which for `Rookie` evaluates to `false` (category != Youth), so `Rookie` never reaches this method anyway because `CalculateAllProgressions` hardcodes `[]` for it (line 18) rather than calling `ComputeRaceProgression`.

Unlike Senior/SuperSenior/Youth/Woman, Rookie status isn't derivable from a single result row's bowler fields (DOB, gender) — it's a **per-bowler-per-season** designation computed by `LegacySeasonStatsCalculator.ComputeMembershipStatus` (`LegacySeasonStatsCalculator.cs:226`) and persisted on `BowlerSeasonStats.IsRookie`. This is exactly why it was deferred: `BoyProgressionResultDto` had no way to carry it, since the historical-result query never joined to `BowlerSeasonStats`.

Fix:

1. Add `bool IsRookie` to `BoyProgressionResultDto`.
2. In `GetSeasonStatsQueryHandler`, after loading `bowlerStats` (already queried at line 80-119 for the exact same `seasonId`), build a `BowlerId → IsRookie` lookup from it and use it when projecting both the historical and current progression queries — e.g. materialize the union of raw rows first, then a client-side `Select` to attach `IsRookie` from the lookup (simpler than joining `BowlerSeasonStats` a second time inside each EF query, and `bowlerStats` is already loaded in memory for this same request).
3. In `CalculateAllProgressions`, replace the hardcoded `[]` with `ComputeRaceProgression(results, BowlerOfTheYearCategory.Rookie)`.
4. In `IsTournamentEligibleForRace`, Rookie already falls into the first branch (`Open || Youth || Rookie` → `result.StatsEligible`) — no change needed there; per `AssignRookieBowlerOfTheYearAwardJob`'s comment ("uses BowlerOfTheYearPoints as Open, filtered to IsRookie"), Rookie should use the same tournament-eligibility and points rules as Open, just bowler-filtered.
5. In `IsBowlerEligibleForRace`, add: `if (category == BowlerOfTheYearCategory.Rookie) return result.IsRookie;`

This makes Rookie progression work identically for historical and current seasons, with no special-casing — it rides the same union built in step 1.

## Bonus fix (ship before dark release): `GetSeasonStatsQueryHandler` caches on the wrong stack

While re-reading this handler for the union work above, found a cache-invalidation mismatch — same bug class as a prior `DeleteArticleCommandHandler` bug in this codebase (fixed by switching that handler from `HybridCache` to `IFusionCache`).

- `GenerateSeasonStatsJob.cs:28,121` invalidates via **`IFusionCache.RemoveByTagAsync("neba:stats:seasons:{seasonId}")`** after regenerating `BowlerSeasonStats`.
- `GetSeasonStatsQueryHandler.cs:20,62-66` reads/writes through **Microsoft's `HybridCache`** (`CacheDescriptors.Stats.BowlerSeasonStats(season.Id)`).
- `CachingConfiguration.cs` registers these as two entirely separate, unbridged cache stacks (`AddHybridCache(...)` and `AddFusionCache()...WithRegisteredDistributedCache()`, no `.AsHybridCache()` bridge). The job's invalidation call never reaches the entry the query handler actually serves.

**Practical effect**: if the stats page is ever hit for a season *before* `GenerateSeasonStatsJob` has (re)populated `BowlerSeasonStats` — e.g. a `SeasonHasNoStats` error, or a stale/empty result — that response can sit in `HybridCache` indefinitely. The job's own cache-clear is a no-op against it, since it's clearing a different cache instance. This is a plausible contributor to "why does 2026 still show nothing" even after the underlying data/job issue is fixed.

**Fix**: change `GetSeasonStatsQueryHandler` to inject and use `IFusionCache` instead of `HybridCache`, matching `CachedQueryHandlerDecorator` (the pipeline every other cached query goes through) and matching what `GenerateSeasonStatsJob` already invalidates against. Confirm `CacheDescriptors.Stats.BowlerSeasonStats(...)`'s tag matches (or is reconciled with) the literal `"neba:stats:seasons:{seasonId}"` string the job uses today.

**Tests**: per this codebase's existing guidance, a test asserting cache invalidation must register a real `IFusionCache` (`services.AddFusionCache()...`) in the test `ServiceCollection`, not `HybridCache` — a test built against the wrong cache type will pass even when the invalidation logic is broken (this is exactly what masked the earlier `DeleteArticleCommandHandler` bug for a while).

This fix is independent of the historical/current union and Rookie-progression work above and can ship on its own branch first.

## Summary of file-level changes

- `Features/Stats/GetSeasonStats/BoyProgressionResultDto.cs` — add `IsRookie`.
- `Features/Stats/GetSeasonStats/GetSeasonStatsQueryHandler.cs` — add `_tournamentResults` (or equivalent) query source; replace the single historical-only progression query with the historical+current union; attach `IsRookie` from the already-loaded `bowlerStats`; switch from `HybridCache` to `IFusionCache` (see bonus fix above).
- `Features/Stats/GetSeasonStats/BowlerOfTheYearRaceCalculator.cs` — un-defer Rookie in `CalculateAllProgressions`; add the `Rookie` case to `IsBowlerEligibleForRace`.

## Testing

- Unit test `GetSeasonStatsQueryHandler` (or an integration test, matching existing coverage style for this handler) for a season with rows in `TournamentResult` only (no `HistoricalTournamentResult` rows) — confirm progressions are non-empty and correct.
- Unit test a season with rows in both tables — confirm both contribute to the same bowler's cumulative series in chronological order (tests the `Concat` + `OrderBy` ordering doesn't silently drop or duplicate one side).
- Unit test `BowlerOfTheYearRaceCalculator.ComputeRaceProgression` for `Rookie`: a mix of rookie and non-rookie bowlers across eligible tournaments, confirming only rookie bowlers appear in the series and non-rookies are excluded — mirroring existing per-category tests for Senior/SuperSenior/Youth/Woman.
- Regression: confirm existing historical-only progression tests (Open/Senior/SuperSenior/Woman/Youth for pre-2026 seasons) still pass unchanged after the union is introduced.
