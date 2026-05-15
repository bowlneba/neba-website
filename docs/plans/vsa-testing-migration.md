# VSA Testing Migration Plan

## Context

The codebase is migrating from a layered architecture (Domain → Application → Infrastructure → API) to Vertical Slice Architecture (VSA), where each feature slice in `src/Neba.Api/Features/<Feature>/<UseCase>/` owns its query handler, DTOs, endpoint, validator, and summary. Query logic that previously lived behind interfaces (`IBowlingCenterQueries`, `ISponsorsQueries`, etc.) now lives directly inside the query handler using EF Core.

This document defines the testing standard for every slice post-VSA. Use it as input when asking the LLM to review a specific feature: *"Review the [FeatureName] feature and make sure it has been migrated according to the testing plan."*

---

## Current Migration State (as of 2026-05-14)

### Fully migrated to VSA in `Neba.Api`

| Feature | Slices |
|---|---|
| Awards | ListBowlerOfTheYearAwards, ListHighAverageAwards, ListHighBlockAwards |
| BowlingCenters | ListBowlingCenters |
| Documents | GetDocument, SyncDocument |
| HallOfFame | ListHallOfFameInductions |
| Seasons | ListSeasons |
| Sponsors | GetSponsorDetail, ListActiveSponsors |
| Stats | GetSeasonsWithStats |
| Tournaments | GetTournament, ListTournamentsInSeason |

### Not yet fully migrated

| Feature | Status |
|---|---|
| Stats / GetSeasonStats | Query handler still in `Neba.Application`. The endpoint and calculator are already in `Neba.Api`. The services (`SeasonStatsService`, `BowlerOfTheYearProgressionService`) and all DTOs are still in `Neba.Application.Stats`. These must be moved before writing tests. |

### Orphaned tests that must be deleted

`tests/Neba.Application.Tests/` contains handler tests that mock the old query/repository interfaces. These interfaces no longer exist outside Stats. Delete the entire test file (not just the class) for any handler that has been migrated to VSA:

- `BowlingCenters/ListBowlingCenters/ListBowlingCentersQueryHandlerTests.cs`
- `Awards/ListBowlerOfTheYearAwards/ListBowlerOfTheYearAwardsQueryHandlerTests.cs`
- `Awards/ListHighAverageAwards/ListHighAverageAwardsQueryHandlerTests.cs`
- `Awards/ListHighBlockAwards/ListHighBlockAwardsQueryHandlerTests.cs`
- `HallOfFame/ListHallOfFameInductions/ListHallOfFameInductionsQueryHandlerTests.cs`
- `HallOfFame/ListHallOfFameInductions/ListHallOfFameInductionsQueryTests.cs`
- `Seasons/ListSeasons/ListSeasonsQueryHandlerTests.cs`
- `Seasons/ListSeasons/ListSeasonsQueryTests.cs`
- `Sponsors/GetSponsorDetail/GetSponsorDetailQueryHandlerTests.cs`
- `Sponsors/ListActiveSponsors/ListActiveSponsorsQueryHandlerTests.cs`
- `Tournaments/GetTournament/GetTournamentQueryHandlerTests.cs`
- `Tournaments/ListTournamentsInSeason/ListTournamentsInSeasonQueryHandlerTests.cs`
- `Tournaments/TournamentsCacheDescriptorsTests.cs`
- `Stats/GetSeasonStats/GetSeasonStatsQueryHandlerTests.cs` — delete after Stats migration is complete
- `Stats/SeasonStatsServiceTests.cs` — delete after Stats migration is complete
- `Stats/BoyProgression/BowlerOfTheYearProgressionServiceTests.cs` — delete after Stats migration is complete
- `Stats/StatsCacheDescriptorsTests.cs` — evaluate whether this is still relevant post-migration

---

## What a Fully Migrated Slice Looks Like

### Source files (in `src/Neba.Api/Features/<Feature>/<UseCase>/`)

```
<UseCase>Query.cs              — ICachedQuery<T> with Cache key, tags, and Expiry
<UseCase>QueryHandler.cs       — injects AppDbContext, queries EF directly, returns DTO(s)
<UseCase>Endpoint.cs           — FastEndpoints endpoint; maps query result to response contract
<UseCase>Summary.cs            — OpenAPI summary / Produces declarations
<UseCase>Dto.cs                — one or more feature-local DTOs (not shared across features)
<UseCase>Request.cs            — request record (if the endpoint takes input)
<UseCase>RequestValidator.cs   — FluentValidation validator (if there is a Request)
```

Calculators and services that belong entirely to a single slice live in the same folder as `internal sealed` classes.

### Test files (in `tests/Neba.Api.Tests/Features/<Feature>/<UseCase>/`)

```
<UseCase>QueryTests.cs            — unit tests for cache key, tags, expiry
<UseCase>QueryHandlerTests.cs     — integration tests against a real Postgres via PostgreSqlFixture
<UseCase>EndpointTests.cs         — unit tests focused on the JSON contract (Verify snapshots)
<UseCase>RequestValidatorTests.cs — unit tests for the validator (if a validator exists)
<UseCase>CalculatorTests.cs       — unit tests for any inline calculator (if one exists)
```

---

## Test Standards by File Type

### 1. Query tests (`<UseCase>QueryTests.cs`) — Unit

These are lightweight and already exist for most slices. Keep them. They verify:

- `query.Cache.Key` is the expected string
- `query.Cache.Tags` contains every expected tag and no extra tags (`Count.ShouldBe(N)`)
- `query.Expiry` is the expected `TimeSpan`

No changes needed unless the query definition itself changed.

---

### 2. Query handler tests (`<UseCase>QueryHandlerTests.cs`) — Integration

The old Application.Tests handler tests mocked `IBowlingCenterQueries` etc. — delete them. The new tests must exercise the handler against a real Postgres database using the existing `PostgreSqlFixture` from `Neba.TestFactory.Infrastructure`.

**Pattern:**

```csharp
[IntegrationTest]
[Component("<Feature>")]
public sealed class <UseCase>QueryHandlerTests(PostgreSqlFixture fixture)
    : IClassFixture<PostgreSqlFixture>
{
    [Fact(DisplayName = "...")]
    public async Task HandleAsync_Should<Result>_When<Condition>()
    {
        // Arrange — seed via DbContext
        await using var db = fixture.CreateDbContext();
        // ... seed domain entities

        // Act
        var handler = new <UseCase>QueryHandler(db);
        var result = await handler.HandleAsync(new <UseCase>Query { ... }, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(...);
        // etc.
    }
}
```

**Required test scenarios per handler:**

- Returns the expected data when matching records exist
- Returns empty / `Error.NotFound` when no matching records exist
- Filters correctly (e.g., by season, by active status, by ID) when the query has parameters
- Returns records in the expected order when ordering is applied

---

### 3. Endpoint tests (`<UseCase>EndpointTests.cs`) — Unit

**Delete** all existing endpoint tests and rewrite from scratch. The only concern of endpoint tests is the **JSON contract** the endpoint produces. They do not test EF queries (that is the handler's job).

**The test class must cover every documented response code.** Determine which codes to test from the `Produces(...)` and `ProducesProblemDetails(...)` declarations in the endpoint's `Configure()` method, plus any explicit `Send.NotFoundAsync` / `Send.ErrorsAsync` / `Send.OkAsync` calls in `HandleAsync`.

**Response code → test type mapping:**

| Response code | What to assert |
|---|---|
| 200 OK | `await Verify(endpoint.Response)` — Verify snapshot of the full response shape |
| 404 Not Found | `endpoint.HttpContext.Response.StatusCode.ShouldBe(404)` |
| 422 Unprocessable | Not tested in endpoint tests — covered by RequestValidator tests |
| 500 Internal | `endpoint.HttpContext.Response.StatusCode.ShouldBe(500)` |

Every endpoint documents `500` implicitly (global exception handler). Unless the endpoint has an explicit null-guard → 500 path (like `GetSeasonStats`), the 500 path test is usually not needed because there is no code path to exercise it in a unit test. Only add it if the endpoint has an explicit `Send.ErrorsAsync(500, ...)` call.

**Happy-path test with Verify:**

```csharp
[Fact(DisplayName = "HandleAsync should return OK with correct response shape")]
public async Task HandleAsync_ShouldReturnOk_WithCorrectResponseShape()
{
    // Arrange
    var dto = <Dto>Factory.Bogus(3, seed: 42);  // always use a fixed seed for Verify tests
    var ct = TestContext.Current.CancellationToken;

    var handlerMock = new Mock<IQueryHandler<..., ...>>(MockBehavior.Strict);
    handlerMock.Setup(h => h.HandleAsync(It.IsAny<...>(), ct)).ReturnsAsync(dto);

    var endpoint = Factory.Create<...Endpoint>(handlerMock.Object);

    // Act
    await endpoint.HandleAsync(ct);  // or HandleAsync(request, ct) if the endpoint takes input

    // Assert
    endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
    await Verify(endpoint.Response);
}
```

**Additional constraints:**

- Each test in the same class must use a **distinct seed** so snapshots are independent.
- The Configure test (route + auth check) may be kept as-is — it is not being replaced.
- Remove any manual property assertions that Verify already captures (e.g., `endpoint.Response.TotalItems.ShouldBe(...)` is redundant when there is a Verify call in the same test).
- Do not add a Verify call to error-path tests — the response body on error paths is not meaningful in unit tests.

---

### 4. Request validator tests (`<UseCase>RequestValidatorTests.cs`) — Unit

No changes unless the validator changed. Keep these as-is.

---

### 5. Calculator tests (`<UseCase>CalculatorTests.cs`) — Unit

Any `internal sealed` calculator or service co-located in a feature folder needs its own unit test file. The test class lives in `tests/Neba.Api.Tests/Features/<Feature>/<UseCase>/`.

Because the calculator is `internal`, the test project must add `InternalsVisibleTo("Neba.Api.Tests")` if not already present — check `Neba.Api.csproj` before writing tests.

**Current calculators requiring tests:**

- `SeasonStatsCalculator` in `Features/Stats/GetSeasonStats/` — has two public-surface methods:
  - `CalculateStatMinimums(int tournamentCount)` — pure arithmetic, table-test friendly
  - `CalculateSeasonStatsSummary(...)` — complex calculation; use `BowlerSeasonStatsDtoFactory.Bogus()` to seed realistic data, then assert on specific output fields (leaderboard ordering, tie-breaking, zero-game exclusion, etc.)

---

## Test Factories

For every DTO owned by a VSA slice (`Neba.Api.Features.*`), a test factory must exist in `tests/Neba.TestFactory/`. If a factory references an old `Neba.Application.*` namespace for a type that has moved to `Neba.Api.Features.*`, the factory must be updated.

**Process per slice review:**

1. Identify all DTOs in the feature folder.
2. For each DTO, check whether a factory exists in `tests/Neba.TestFactory/`.
3. If no factory exists → run `/test-factory-generator <DtoTypeName>`.
4. If a factory exists but its namespace imports reference `Neba.Application.*` for a type that has moved → update the factory to point to the new `Neba.Api.Features.*` namespace, then re-verify the `Create()` and `Bogus()` defaults still match the current DTO definition.

**Known factories that need namespace updates** (reference `Neba.Application.*` for types that have moved):

- `TournamentDetailDtoFactory` — references `Neba.Application.Tournaments.GetTournament.*`
- `SeasonTournamentDtoFactory` — references `Neba.Application.Tournaments.ListTournamentsInSeason.*`
- `BowlingCenterSummaryDtoFactory` — references `Neba.Application.BowlingCenters.ListBowlingCenters.*`
- `SeasonDtoFactory` — references `Neba.Application.Seasons.*`
- `SponsorDetailDtoFactory`, `SponsorSummaryDtoFactory` — reference `Neba.Application.Sponsors.*`
- All Award DTO factories — reference `Neba.Application.*Awards.*`
- `HallOfFameInductionDtoFactory` — references old namespace

**New DTOs that likely have no factory yet:**

- `SeasonWithStatsDto` (`Features/Stats/GetSeasonsWithStats/`)
- `TournamentBowlingCenterDto` (both GetTournament and ListTournamentsInSeason have their own — separate factories needed)
- `TournamentOilPatternDto` (same — both features have one)
- `TournamentSponsorDto` (same)
- `TournamentResultDto` (GetTournament)
- `ContactInfoDto` (GetSponsorDetail)
- `BowlerSeasonStatsDto` (`Features/Stats/GetSeasonStats/`) — check if existing `BowlerSeasonStatsDtoFactory` points to the right namespace
- All inner DTO types for Stats leaderboards if they moved namespaces

---

## Stats / GetSeasonStats Migration (prerequisite)

Before writing tests for this slice, complete the VSA migration:

1. Move `GetSeasonStatsQuery.cs` → `src/Neba.Api/Features/Stats/GetSeasonStats/`
2. Move all DTOs from `src/Neba.Application/Stats/GetSeasonStats/` → same folder (or inline them into the new handler if they are only used there)
3. Move `SeasonStatsService` → inline the DB queries it wraps into a new `GetSeasonStatsQueryHandler` in the Api (following the same pattern as `GetSeasonsWithStatsQueryHandler`: inject `AppDbContext`, query EF directly)
4. Move `BowlerOfTheYearProgressionService` → inline into the handler or extract to a co-located service in `Features/Stats/GetSeasonStats/`
5. Delete `src/Neba.Application/Stats/` once the migration is complete
6. Update factory namespaces for any Stats DTOs that moved
7. Write tests per the standards above

The `SeasonStatsCalculator` is already in the right place — do not move it.

---

## How to Use This Document

For each feature review session, prompt the LLM with:

> "Review the **[FeatureName/UseCase]** feature and make sure it has been migrated according to `docs/plans/vsa-testing-migration.md`. The feature folder is `src/Neba.Api/Features/[FeatureName]/[UseCase]/`. Start by reading the source files, then check factories, delete old tests, and write new ones."

Run the final API review once all slices are done:

> "All VSA slices have been reviewed. Do a final pass across `src/Neba.Api/Features/` to confirm every slice has: a query handler integration test, endpoint tests with Verify snapshots, factories for all DTOs, and that `tests/Neba.Application.Tests/` contains no orphaned handler tests."
