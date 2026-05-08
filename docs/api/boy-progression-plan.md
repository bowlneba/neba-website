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
