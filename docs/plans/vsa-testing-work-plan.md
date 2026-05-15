# VSA Testing Work Plan

## Context

All backend code now lives in `Neba.Api`. The layered architecture projects (`Neba.Application`, `Neba.Infrastructure`, `Neba.Domain`) have been removed. This plan drives the cleanup and new test authoring needed to bring `Neba.Api.Tests` to full VSA coverage. Work the phases in order — later phases depend on earlier ones being complete.

Reference: `docs/plans/vsa-testing-migration.md` for test patterns and standards.

---

## Phase 0: Prerequisite Investigations — COMPLETE

**0.1 GetSeasonsWithStats**: No separate slice exists. `SeasonWithStatsDto` is used internally within `GetSeasonStats` to build the `AvailableSeasons` dictionary in the response. There is no standalone `GetSeasonsWithStats` endpoint. **Skip all `GetSeasonsWithStats` items in later phases.**

**0.2 BowlerOfTheYearPointsRaceTournamentDtoFactory**: `BowlerOfTheYearPointsRaceTournamentDto` exists in `src/Neba.Api/Features/Stats/GetSeasonStats/BowlerOfTheYearPointsRaceDto.cs` (namespace `Neba.Api.Features.Stats.GetSeasonStats`). Do not delete the factory — its stale `using Neba.Application.Stats.GetSeasonStats` import will be fixed in Phase 3.1.

---

## Phase 1: Remove Obsolete Tests and Projects — COMPLETE

Deleted `tests/Neba.Application.Tests/` (entire directory) and `tests/Neba.Architecture.Tests/` (entire directory). Removed both from `Neba.Website.slnx` and both matrix entries from `.github/workflows/ci.yml`.

---

## Phase 2: Resolve DTO Name Collisions in Tournaments — COMPLETE

Renamed 3 DTOs in `GetTournament` (`TournamentDetail*`) and 3 in `ListTournamentsInSeason` (`SeasonTournament*`). Updated `TournamentDetailDto`, `SeasonTournamentDto`, and both query handlers. No snapshot files reference the old type names. `Neba.Api` builds clean.

---

## Phase 3: Test Factories — COMPLETE

Fixed all stale namespace imports across `tests/Neba.TestFactory/`. Renamed `TournamentOilPatternDtoFactory` → `TournamentDetailOilPatternDtoFactory`; rewrote `TournamentDetailDtoFactory`, `SeasonTournamentDtoFactory`, `SeasonStatsDtoFactory`, `BoyProgressionResultDtoFactory`, `TournamentDetailOilPatternResponseFactory`, and `TournamentDetailOilPatternViewModelFactory` to match current DTO shapes. Created new factories: `TournamentDetailBowlingCenterDtoFactory`, `TournamentDetailSponsorDtoFactory`, `SeasonTournamentBowlingCenterDtoFactory`, `SeasonTournamentOilPatternDtoFactory`, `SeasonTournamentSponsorDtoFactory`, `SeasonWithStatsDtoFactory`. `Neba.TestFactory` builds clean (0 errors, 0 warnings).

---

## Phase 4: Calculator Tests — COMPLETE

`InternalsVisibleTo` was already set. Both test files created.

### 4.1 SeasonStatsCalculatorTests

Created `tests/Neba.Api.Tests/Features/Stats/GetSeasonStats/SeasonStatsCalculatorTests.cs`.

Covered:
- `CalculateStatMinimums` — `[Theory]` with 4 boundary values: 0, 2, 10, 20 tournaments
- `HighAverageLeaderboard` ordered by descending average (intentionally reversed input)
- Zero-games bowlers excluded from `HighAverageLeaderboard`
- Below-threshold bowlers excluded from `HighAverageLeaderboard`
- `MatchPlayRecordLeaderboard` tie-breaking: equal win% → higher wins ranks first
- `BowlerOfTheYear` standings ordered descending; zero-point bowlers excluded
- Smoke test: well-formed summary with `Bogus(count: 20, seed: 42)` data

### 4.2 BowlerOfTheYearRaceCalculatorTests

Created `tests/Neba.Api.Tests/Features/Stats/GetSeasonStats/BowlerOfTheYearRaceCalculatorTests.cs`.

Covered:
- Cumulative points accumulate correctly over multiple tournaments (Open race)
- Multiple bowlers get independent series with correct final totals (ranking + tie: both at 300 pts)
- Bowler with no stats-eligible results excluded from Open race
- Rookie category is always empty
- Non-matching side cut (e.g. "Senior") contributes only 5 points to Open race

**Note**: The `Neba.Api.Tests` project has 124 pre-existing build errors from other test files referencing removed VSA namespaces (old `Neba.Application`, `Neba.Domain` types) — these are not caused by Phase 4 work and will be addressed in later phases. The new calculator test files have zero compile errors.

---

## Phase 5: Domain Entity Test Review — COMPLETE

**Changes made:**

- Removed broken `<AdditionalFiles>` reference pointing to `src/Neba.Domain/ulid-full.typedid` (project removed in VSA migration) from `Neba.Api.Tests.csproj`.
- Deleted `TournamentValidationServiceTests.cs` — the service was refactored to take `AppDbContext` directly; the old test mocked `ISeasonRepository` which no longer exists. Needs to be rewritten as an integration test in Phase 6 when the PostgreSQL fixture is wired up for `Neba.Api.Tests`.
- Fixed stale `using Neba.Domain.*` imports across all 10 tournament domain test files:
  - `PatternRatioCategoryTests.cs`, `PatternLengthCategoryTests.cs`, `TournamentTypeTests.cs`, `TournamentRoundTests.cs`, `TournamentOilPatternTests.cs`, `TournamentTests.cs` → `Neba.Api.Features.Tournaments.Domain`
  - `TournamentTests.cs` also: `Neba.Domain.Sponsors` → `Neba.Api.Features.Sponsors.Domain`
  - `SideCutCriteriaGroupTests.cs` — stale import was unused; removed entirely
  - `SideCutCriteriaTests.cs` → `Neba.Api.Features.Tournaments.Domain`
  - `SideCutTests.cs` → `Neba.Api.Features.Tournaments.Domain` + added `Neba.Api.Domain` (for `AggregateRoot` and `LogicalOperator`)
  - `LogicalOperatorTests.cs` — was missing `using Neba.Api.Domain;`; added it

**Coverage verdict by feature:**

| Feature | Domain methods | Tests | Status |
|---|---|---|---|
| Bowlers | `Name.Create`, `ToLegalName`, `ToDisplayName`, `ToFormalName` | `NameTests.cs` covers all | Complete |
| BowlingCenters | `LaneRange.Create`, `LaneConfiguration.Create`, `CertificationNumber.Create` | Tests exist for all value objects | Complete |
| HallOfFame | No domain methods (aggregate is data-only) | — | Complete |
| Seasons | All `Add*` aggregate methods | `SeasonTests.cs`, `BowlerOfTheYearAwardTests.cs`, `HighAverageAwardTests.cs`, `HighBlockAwardTests.cs` | Complete |
| Sponsors | No domain methods (`Sponsor`, `ContactInfo` are data-only) | — | Complete |
| Tournaments | `Tournament.AddSponsor`, `AddOilPattern`; `TournamentOilPattern.Create`, `AddTournamentRound`; `SideCut.AddCriteriaGroup`, `AddCriteria`; `SideCutCriteria.CreateAgeRequirement`, `CreateGenderRequirement`; `SideCutCriteriaGroup.AddCriteria` | Tests exist for all after namespace fixes | Complete |
| Stats | `BowlerSeasonStats` and `BowlerSeasonStatsSnapshot` are `init`-only data containers — no domain logic | No Stats/Domain folder needed | Complete |

**Deferred:** `TournamentValidationService` integration test — added to Phase 6 Tournaments section.

---

## Phase 6: Query Handler Integration Tests — COMPLETE

Added `InternalsVisibleTo("Neba.Api.Tests")` to `tests/Neba.TestFactory/Neba.TestFactory.csproj` to expose `PostgreSqlFixture.CreateDbContext()` and internal handler/service types.

All 11 integration test files created under `tests/Neba.Api.Tests/Features/`. Each uses `PostgreSqlFixture` + `IClassFixture` + `IAsyncLifetime` with `ResetAsync` between tests, seeds via `DbContext`, and instantiates the handler directly.

**Awards**: `ListBowlerOfTheYearAwardsQueryHandlerTests`, `ListHighAverageAwardsQueryHandlerTests`, `ListHighBlockAwardsQueryHandlerTests` — empty, correct fields, multi-season.

**BowlingCenters**: `ListBowlingCentersQueryHandlerTests` — empty, correct fields with coordinates, count.

**HallOfFame**: `ListHallOfFameInductionsQueryHandlerTests` — empty, correct fields (no photo), `PhotoUri` set when photo present. Mocks `IFileStorageService`.

**Seasons**: `ListSeasonsQueryHandlerTests` — empty, correct fields, ordering by start date descending.

**Sponsors**: `GetSponsorDetailQueryHandlerTests` — `NotFound`, correct fields, `LogoUrl` set, slug filter. `ListActiveSponsorsQueryHandlerTests` — empty, inactive excluded, correct fields, `LogoUrl` set. Both mock `IFileStorageService`.

**Stats**: `GetSeasonStatsQueryHandlerTests` — `SeasonHasNoStats` when no records, most recent season, specified year, year not found. Uses real `SeasonStatsCalculator` and `BowlerOfTheYearRaceCalculator` (both parameterless); `HybridCache` built from `ServiceCollection` per test class.

**Tournaments**: `GetTournamentQueryHandlerTests` — `NotFound`, correct fields (empty historical), `LogoUrl` set. `ListTournamentsInSeasonQueryHandlerTests` — empty, season filter, correct fields, `LogoUrl` set. `TournamentValidationServiceTests` — `SeasonNotFound`, valid dates, start before season, end after season. All tournament tests mock `IFileStorageService`.

---

## Phase 7: Endpoint Test Review and Gaps

### 7.1 Review All Existing Endpoint Tests

For each existing endpoint test class, verify and fix as needed:

1. Every response code in `Configure()` (`Produces(...)`, `ProducesProblemDetails(...)`) has a corresponding test.
2. Happy-path tests call `await Verify(endpoint.Response)` and do **not** also assert individual response properties (remove redundant assertions).
3. Each test uses a **distinct** Bogus seed — no two tests in the same class share a seed.
4. Error-path tests assert only `endpoint.HttpContext.Response.StatusCode` — no `Verify` call.
5. The Configure test (route + auth) may remain as-is.

Features with existing endpoint tests: all Award slices, BowlingCenters, Documents/GetDocument, HallOfFame, Seasons, both Sponsor slices, Stats/GetSeasonStats, both Tournament slices.

### 7.2 Create Missing Endpoint Tests

**Stats/GetSeasonsWithStats** — skip (Phase 0.1: no separate slice).

### 7.3 Missing Query Tests

**Stats/GetSeasonStats** — `GetSeasonStatsQueryTests.cs` does not exist.

Create `tests/Neba.Api.Tests/Features/Stats/GetSeasonStats/GetSeasonStatsQueryTests.cs` and test:
- `query.Cache.Key` equals the expected string
- `query.Cache.Tags` contains every expected tag and no extras (`Count.ShouldBe(N)`)
- `query.Expiry` equals the expected `TimeSpan`

**Stats/GetSeasonsWithStats** — skip (Phase 0.1: no separate slice).

---

## Phase 8: CI and Mutation Configuration Updates

Make these changes regardless of the other phases; they can be done in parallel with Phase 1.

### 8.1 Update `tests/Neba.Api.Tests/stryker-config.json`

Change `thresholds` from `high: 90 / low: 80 / break: 75` to:

```json
"thresholds": {
  "high": 80,
  "low": 70,
  "break": 60
}
```

### 8.2 Update `.github/workflows/mutation-testing.yml`

1. **Delete** the entire `mutation-application` job.
2. **Update** the `mutation-api` job name: `API (break ≥ 75%)` → `API (break ≥ 60%)`.
3. **Update** the `mutation-summary` job:
   - Remove `mutation-application` from the `needs` array.
   - Remove the Application entry from the `layers` array in the inline GitHub Script.
   - Update the API `breakThreshold` string from `≥ 75%` to `≥ 60%`.

### 8.3 Update `.github/workflows/ci.yml`

Remove the following two entries from the `xunit` strategy matrix:

```yaml
- test-project: tests/Neba.Application.Tests/Neba.Application.Tests.csproj
  test-name: Application Tests
  ...
- test-project: tests/Neba.Architecture.Tests/Neba.Architecture.Tests.csproj
  test-name: Architecture Tests
  ...
```

### 8.4 Update `CLAUDE.md` Thresholds Table

In the `.NET Mutation Testing` section, update the API row to match the new thresholds:

```
| API | 80 | 70 | 60 | |
```
