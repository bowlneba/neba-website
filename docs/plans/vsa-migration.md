# VSA Migration Plan

Migrate from clean architecture (Domain / Application / Infrastructure / Api as four separate projects) to vertical slice architecture with three runtime projects. DDD tactical patterns (aggregates, value objects, always-valid entities, ErrorOr) are kept; project layer boundaries are replaced by folder conventions.

---

## Before / After

### Projects (src/)

| Before | After | Note |
|---|---|---|
| `Neba.Domain` | **deleted** | code moves to `Neba.Api/Features/*/Domain/` and `Neba.Api/Features/Shared/` |
| `Neba.Application` | **deleted** | code moves to `Neba.Api` feature folders |
| `Neba.Infrastructure` | **deleted** | code moves to `Neba.Api/Data/` and `Neba.Api/Services/` |
| `Neba.Api` | **kept** | absorbs everything above |
| `Neba.Api.Contracts` | **kept, unchanged** | Refit typed client contracts for Website.Server |
| `Neba.Website.Server` | **kept, unchanged** | |
| `Neba.Website.Client` | **kept, unchanged** | |
| `Neba.AppHost` | **kept, updated** | drop project refs to deleted projects |
| `Neba.ServiceDefaults` | **kept, unchanged** | |

### Projects (tests/)

| Before | After | Note |
|---|---|---|
| `Neba.Domain.Tests` | **deleted** | tests move to `Neba.Api.Tests` |
| `Neba.Application.Tests` | **deleted** | tests move to `Neba.Api.Tests` |
| `Neba.Infrastructure.Tests` | **deleted** | tests move to `Neba.Api.Tests` |
| `Neba.Api.Tests` | **kept** | absorbs the three above |
| `Neba.Architecture.Tests` | **deleted** | layer rules no longer apply; delete |
| `Neba.TestFactory` | **kept, updated** | update project reference from Domain → Api |
| `Neba.Website.Tests` | **kept, unchanged** | |

### Target folder structure inside `Neba.Api`

```
Neba.Api/
  Features/
    Tournaments/
      Domain/                      ← Tournament, TournamentId, OilPattern, SideCut, TournamentRound, etc.
      GetTournament/
        Endpoint.cs, Request.cs, Validator.cs, Summary.cs
        Query.cs, Handler.cs, Dto files
      ListTournamentsInSeason/
        Endpoint.cs, Request.cs, Validator.cs, Summary.cs, Query.cs, Handler.cs, Dto files

    Seasons/
      Domain/                      ← Season, SeasonId, SeasonAwardId, BowlerOfTheYearAward, HighAverageAward, HighBlockAward
      ListSeasons/  ...

    Bowlers/
      Domain/                      ← Bowler, BowlerId, Gender, Name, NameSuffix
      (no endpoint features yet)

    BowlingCenters/
      Domain/                      ← BowlingCenter, BowlingCenterStatus, CertificationNumber, LaneRange, etc.
      ListBowlingCenters/  ...

    HallOfFame/
      Domain/                      ← HallOfFameInduction, HallOfFameId, HallOfFameCategory
      ListHallOfFameInductions/  ...

    Sponsors/
      Domain/                      ← Sponsor, SponsorId, SponsorCategory, SponsorTier, ContactInfo
      GetSponsorDetail/  ...
      ListActiveSponsors/  ...

    Stats/
      Domain/                      ← BowlerSeasonStats, BowlerSeasonStatsSnapshot
      GetSeasonStats/  ...

    Awards/                        ← no Domain/ here: award entities are owned by Season aggregate
      AwardsGroup.cs
      ListBowlerOfTheYearAwards/
        Endpoint.cs, Summary.cs
        Query.cs, Handler.cs, Dto files   ← from Application; Handler injects DbContext directly
      ListHighAverageAwards/  ...
      ListHighBlockAwards/  ...

    Documents/
      GetDocument/  ...
      SyncDocument/ ...

    Shared/
      Domain/                      ← AggregateRoot.cs, IDomainEvent.cs, LogicalOperator.cs, SmartFlagEnumJsonConverter.cs
      Contact/                     ← Address, EmailAddress, PhoneNumber, CanadianProvince, UsState, shared DTOs
      Geography/                   ← Coordinates, DistanceCalculator
      Storage/                     ← StoredFile, FileContent, IFileStorageService

  Data/                            ← all of Infrastructure/Database/
    AppDbContext.cs
    AppDbContextDesignTimeFactory.cs
    DatabaseConfiguration.cs
    Configurations/
    Converters/
    Entities/
    Interceptors/
    Migrations/
    Options/

  Services/                        ← non-DB infrastructure services
    BackgroundJobs/                ← Infrastructure/BackgroundJobs/
    Caching/                       ← Infrastructure/Caching/
    Clock/                         ← Infrastructure/Clock/
    Documents/                     ← Infrastructure/Documents/ (GoogleDriveService)
    Storage/                       ← Infrastructure/Storage/ (AzureBlobStorageService)

  Messaging/                       ← Application/Messaging/ (IQuery, ICachedQuery, IQueryHandler, ICommand, ICommandHandler)
  Telemetry/                       ← Infrastructure/Telemetry/
  ErrorHandling/                   (unchanged)
  OpenApi/                         (unchanged)
  Versioning/                      (unchanged)
  Program.cs
```

**Note on `Awards/`:** `BowlerOfTheYearAward`, `HighAverageAward`, and `HighBlockAward` are child entities owned by the `Season` aggregate. They live in `Features/Seasons/Domain/`. The `Awards/` feature slice has no domain of its own — it queries into Season's child collections. This is intentional and honest about domain ownership.

---

## Key Decisions

### 1. Namespaces — keep as-is

Do **not** rename namespaces to match the new project. `Neba.Domain.Tournaments.Tournament` stays `Neba.Domain.Tournaments.Tournament`; it just lives inside `Neba.Api`. This avoids thousands of `using` change diffs across Website.Server, Contracts, TestFactory, and all test projects.

### 2. Keep IQueryHandler / ICommandHandler dispatch

The `CachedQueryHandlerDecorator` and `TracedQueryHandlerDecorator` both wrap `IQueryHandler<TQuery, TResponse>`. Both are non-trivial (deserialization fallback, ErrorOr unwrapping, distributed tracing, activity source). Keep them.

What **is** removed: the intermediate `I*Queries` interfaces (`IAwardQueries`, `ISeasonQueries`, `IBowlerQueries`, `IBowlerOfTheYearProgressionService`… etc.) and `ISeasonRepository`. The concrete query implementations that lived in `Infrastructure/Database/Queries/*.cs` are inlined directly into their respective query handlers, which now inject `AppDbContext` directly.

### 3. Remove I*Queries and ISeasonRepository — use DbContext directly

Before:
```csharp
// Application handler
public async Task<ListBowlerOfTheYearAwardsResponse> HandleAsync(
    ListBowlerOfTheYearAwardsQuery query, CancellationToken ct)
    => await _awardQueries.ListBowlerOfTheYearAwardsAsync(query.SeasonId, ct);

// Infrastructure implementation
public Task<ListBowlerOfTheYearAwardsResponse> ListBowlerOfTheYearAwardsAsync(...)
    => _dbContext.Set<BowlerOfTheYearAward>()
        .Where(...)
        .Select(...)
        .ToListAsync(ct);
```

After (one file, no interface):
```csharp
public async Task<ListBowlerOfTheYearAwardsResponse> HandleAsync(
    ListBowlerOfTheYearAwardsQuery query, CancellationToken ct)
    => await _dbContext.Set<BowlerOfTheYearAward>()
        .Where(...)
        .Select(...)
        .ToListAsync(ct);
```

`ISeasonRepository` (only repository) follows the same pattern — `SeasonRepository.cs` is deleted; command handlers that previously called `_repository.SaveAsync(ct)` now call `_dbContext.SaveChangesAsync(ct)` directly. The only repository method beyond save was `GetByIdAsync`; that becomes a direct `_dbContext.Seasons.FindAsync(id, ct)` call.

### 4. `internal` factory method boundary widens

Currently `internal static ErrorOr<T> Create(...)` on domain entities is enforced by the assembly boundary of `Neba.Domain`. After the migration, `internal` means "anywhere in `Neba.Api`", which includes handlers and endpoints. The compiler no longer enforces this — it becomes a convention. Accept this tradeoff; add a note to CLAUDE.md.

### 5. Application services (`ISeasonStatsService`, `IBowlerOfTheYearProgressionService`)

These are domain-logic services, not infrastructure. They stay as concrete classes and their interface is deleted or kept depending on whether testing requires it. Check each:
- `SeasonStatsService` — has meaningful logic, keep as a registered service injected by handlers
- `BowlerOfTheYearProgressionService` — same

Keep the concrete classes; remove the interfaces if they have no value beyond DI. Alternatively, keep the interfaces for testability on `GetSeasonStats` which is the most complex handler. **Decide per service when you get there.**

---

## Phase-by-Phase Implementation

### Phase 1 — Prepare Neba.Api to receive code

1. Create the following folder skeleton inside `src/Neba.Api/`:
   - `Features/Tournaments/Domain/`
   - `Features/Seasons/Domain/`
   - `Features/Bowlers/Domain/`
   - `Features/BowlingCenters/Domain/`
   - `Features/HallOfFame/Domain/`
   - `Features/Sponsors/Domain/`
   - `Features/Stats/Domain/`
   - `Features/Awards/`
   - `Features/Documents/`
   - `Features/Shared/Domain/`
   - `Features/Shared/Contact/`
   - `Features/Shared/Geography/`
   - `Features/Shared/Storage/`
   - `Data/`
   - `Services/BackgroundJobs/`
   - `Services/Caching/`
   - `Services/Clock/`
   - `Services/Documents/`
   - `Services/Storage/`
   - `Messaging/`
2. Add `<ProjectReference>` to `Neba.Api.csproj` for all packages currently in `Neba.Domain.csproj`, `Neba.Application.csproj`, and `Neba.Infrastructure.csproj` that aren't already there. Use `Directory.Packages.props` — do not pin versions.
3. Verify `dotnet build` still passes before moving any files.

### Phase 2 — Move Neba.Domain → Neba.Api/Features/

Move each domain folder to its matching feature slice. No namespace changes.

| From (`src/Neba.Domain/`) | To (`src/Neba.Api/Features/`) |
|---|---|
| `AggregateRoot.cs`, `IDomainEvent.cs`, `LogicalOperator.cs`, `SmartFlagEnumJsonConverter.cs` | `Shared/Domain/` |
| `Bowlers/` | `Bowlers/Domain/` |
| `BowlingCenters/` | `BowlingCenters/Domain/` |
| `Contact/` | `Shared/Contact/` |
| `Geography/` | `Shared/Geography/` |
| `HallOfFame/` | `HallOfFame/Domain/` |
| `Seasons/` | `Seasons/Domain/` |
| `Sponsors/` | `Sponsors/Domain/` |
| `Stats/` | `Stats/Domain/` |
| `Storage/` | `Shared/Storage/` |
| `Tournaments/` | `Tournaments/Domain/` |

After moving:
1. Remove `<ProjectReference Include="..\Neba.Domain\Neba.Domain.csproj" />` from `Neba.Api.csproj`.
2. Remove `Neba.Domain` project reference from every other project that had it (`Neba.Application`, `Neba.Infrastructure`, `Neba.Api.Tests`, `Neba.TestFactory`, `Neba.Architecture.Tests`) — add `Neba.Api` reference where needed.
3. `dotnet build` must pass before moving on.

Note: `ISeasonRepository.cs` is in `Neba.Domain/Seasons/` — it moves to `Features/Seasons/Domain/` now and is deleted in Phase 6.

### Phase 3 — Move Infrastructure/Database → Neba.Api/Data/

Move every file under `src/Neba.Infrastructure/Database/` to `src/Neba.Api/Data/`, preserving sub-folder structure. No namespace changes.

Files to move: `AppDbContext.cs`, `AppDbContextDesignTimeFactory.cs`, `DatabaseConfiguration.cs`, all `Configurations/`, all `Converters/`, all `Entities/`, all `Interceptors/`, all `Migrations/`, `Options/`.

After moving:
1. Update `Neba.Api.csproj` — add EF Core, Npgsql packages if not already present.
2. Update `AppDbContextDesignTimeFactory.cs` — the design-time factory may have path-relative assumptions; verify it still resolves correctly.
3. Verify `dotnet ef dbcontext info` resolves cleanly.

### Phase 4 — Move remaining Infrastructure → Neba.Api/Services/ and Neba.Api/Telemetry/

Move file by file:

| From | To |
|---|---|
| `Infrastructure/BackgroundJobs/` | `Api/Services/BackgroundJobs/` |
| `Infrastructure/Caching/` | `Api/Services/Caching/` |
| `Infrastructure/Clock/` | `Api/Services/Clock/` |
| `Infrastructure/Documents/` | `Api/Services/Documents/` |
| `Infrastructure/Storage/` | `Api/Services/Storage/` |
| `Infrastructure/Telemetry/` | `Api/Telemetry/` |
| `Infrastructure/InfrastructureConfiguration.cs` | delete (inline into `Api/Program.cs` or a new `ApiConfiguration.cs`) |
| `Infrastructure/IInfrastructureAssemblyMarker.cs` | delete |

Update `InfrastructureConfiguration` extension methods — consolidate into `Program.cs` or a new `src/Neba.Api/ApiConfiguration.cs`. The Scrutor scans that currently reference `typeof(IApplicationAssemblyMarker).Assembly` or `typeof(IInfrastructureAssemblyMarker).Assembly` should now reference `typeof(IApiAssemblyMarker).Assembly` (already exists in Neba.Api).

Delete `Neba.Infrastructure` project and its `.csproj`.

### Phase 5 — Move Application/Messaging and BackgroundJob abstractions → Neba.Api/

| From (`src/Neba.Application/`) | To (`src/Neba.Api/`) |
|---|---|
| `Messaging/IQuery.cs` | `Messaging/` |
| `Messaging/ICachedQuery.cs` | `Messaging/` |
| `Messaging/IQueryHandler.cs` | `Messaging/` |
| `Messaging/ICommand.cs` | `Messaging/` |
| `Messaging/ICommandHandler.cs` | `Messaging/` |
| `Messaging/MessagingConfiguration.cs` | `Messaging/` |
| `Caching/CacheDescriptor.cs` | `Services/Caching/` |
| `Caching/CacheDescriptors.cs` | `Services/Caching/` |
| `Clock/IStopwatchProvider.cs` | `Services/Clock/` |
| `BackgroundJobs/IBackgroundJob.cs` | `Services/BackgroundJobs/` |
| `BackgroundJobs/IBackgroundJobHandler.cs` | `Services/BackgroundJobs/` |
| `BackgroundJobs/IBackgroundJobScheduler.cs` | `Services/BackgroundJobs/` |
| `BackgroundJobs/IDomainEventJob.cs` | `Services/BackgroundJobs/` |

After moving:
- Update `MessagingConfiguration` Scrutor scans to use `typeof(IApiAssemblyMarker).Assembly`.
- Update `BackgroundJobConfiguration` Scrutor scan (currently uses `typeof(IApplicationAssemblyMarker).Assembly`) to use `typeof(IApiAssemblyMarker).Assembly`.
- Delete `Neba.Application/ApplicationConfiguration.cs` and `IApplicationAssemblyMarker.cs` once all files are moved out.

### Phase 6 — Move Application handlers into feature folders; remove I*Queries

This is the largest phase. For each feature:

**Per-feature steps:**

1. Move `Application/{Feature}/{Action}/` DTOs and Query record into `Api/Features/{Feature}/{Action}/`.
2. Move the `QueryHandler.cs` into `Api/Features/{Feature}/{Action}/`.
3. Open the handler — find the injected `I*Queries` interface call.
4. Find the corresponding implementation in `Infrastructure/Database/Queries/{Feature}Queries.cs`.
5. Replace the interface injection with `AppDbContext` injection. Inline the EF query directly into `HandleAsync`.
6. Delete the corresponding method from the `I*Queries` interface and its implementation file. When the implementation file is empty, delete it.
7. When all methods on an `I*Queries` interface are gone, delete the interface file from `Application/{Feature}/`.

Move error files (e.g., `Application/Sponsors/SponsorErrors.cs`) into the matching `Api/Features/{Feature}/` folder.

Move shared DTOs that don't belong to a single action (e.g., `Application/Contact/AddressDto.cs`, `Application/Tournaments/OilPatternDto.cs`) into the corresponding `Features/Shared/` or `Features/{Feature}/` folder.

**Feature checklist:**

- [ ] **Awards** — `IAwardQueries` (3 handlers: BOY, HighAverage, HighBlock)
  - Handlers move to `Features/Awards/List*/`
  - No domain folder — award entities live in `Features/Seasons/Domain/`
- [ ] **BowlingCenters** — `IBowlingCenterQueries`
  - Handler moves to `Features/BowlingCenters/ListBowlingCenters/`
- [ ] **Documents** — `IDocumentsService` is an external service (Google Drive); **keep the interface**
  - `GetDocumentQueryHandler` moves to `Features/Documents/GetDocument/`
  - `SyncDocumentToStorageJob`, `SyncDocumentToStorageJobHandler`, `SyncDocumentToStorageMetrics` move to `Features/Documents/SyncDocument/`
  - `IDocumentsService`, `DocumentDto`, `DocumentErrors` move to `Features/Documents/`
- [ ] **HallOfFame** — `IHallOfFameQueries`
  - Handler moves to `Features/HallOfFame/ListHallOfFameInductions/`
- [ ] **Seasons** — `ISeasonQueries`
  - Handler moves to `Features/Seasons/ListSeasons/`
- [ ] **Sponsors** — `ISponsorQueries`
  - Handlers move to `Features/Sponsors/GetSponsorDetail/` and `Features/Sponsors/ListActiveSponsors/`
- [ ] **Stats** — `IStatsQueries`; see note on `ISeasonStatsService` below
  - Handler moves to `Features/Stats/GetSeasonStats/`
- [ ] **Tournaments** — `ITournamentQueries`
  - Handlers move to `Features/Tournaments/GetTournament/` and `Features/Tournaments/ListTournamentsInSeason/` (already exist in Api)
- [ ] **Bowlers** — `IBowlerQueries` (no dedicated handler; used by Stats handler — inline into Stats handler)

**`ISeasonStatsService` and `IBowlerOfTheYearProgressionService` (deferred decision):**

These are domain-logic computation services, not DB query abstractions. Do not remove their interfaces in this phase. Move the interfaces and concrete classes into `Features/Stats/` alongside the handler. Decide whether to keep or delete the interfaces based on whether `GetSeasonStats` handler tests benefit from mocking them. This decision is deferred to the Stats handler migration.

**ISeasonRepository (only repo):**

`SeasonRepository` has `GetByIdAsync` and wraps `SaveChangesAsync`. Replace every caller:
- `GetByIdAsync(id, ct)` → direct EF lookup on `_dbContext` (check whether Season uses a shadow int PK or the domain ULID for lookup; match the existing `SeasonRepository` query exactly)
- `SaveAsync(ct)` → `_dbContext.SaveChangesAsync(ct)`

Delete `Features/Seasons/Domain/ISeasonRepository.cs` and `Infrastructure/Database/Repositories/SeasonRepository.cs`.

After all handlers are moved and all `I*Queries` files are empty/deleted, delete:
- `Neba.Application` project and its `.csproj`
- All `Infrastructure/Database/Queries/` files (now empty)
- `Infrastructure/Database/Repositories/` (now empty)

### Phase 7 — Consolidate test projects

**For each test project being deleted (`Neba.Domain.Tests`, `Neba.Application.Tests`, `Neba.Infrastructure.Tests`):**

1. Move all test files into `Neba.Api.Tests/` mirroring the new `Features/` structure:
   - `Domain.Tests/Tournaments/` → `Api.Tests/Features/Tournaments/Domain/`
   - `Domain.Tests/Seasons/` → `Api.Tests/Features/Seasons/Domain/`
   - `Domain.Tests/BowlingCenters/` → `Api.Tests/Features/BowlingCenters/Domain/`
   - `Application.Tests/Awards/` → `Api.Tests/Features/Awards/`
   - `Application.Tests/Seasons/` → `Api.Tests/Features/Seasons/`
   - `Application.Tests/Tournaments/` → `Api.Tests/Features/Tournaments/`
   - `Infrastructure.Tests/Database/` → `Api.Tests/Data/`
   - (follow the same pattern for all other feature areas)
2. Update `using` directives and namespaces in moved test files.
3. Move any shared test fixtures/base classes into `Api.Tests/`.
4. Delete the old `.csproj` and folder.

**Update `Neba.Api.Tests.csproj`:**
- Absorb all NuGet packages from the three deleted test projects.
- Replace project references to Domain/Application/Infrastructure with `Neba.Api`.

**Update `Neba.TestFactory.csproj`:**
- Remove reference to `Neba.Domain`; add reference to `Neba.Api`.

**Delete `Neba.Architecture.Tests`** — the layer rules it enforces no longer exist.

### Phase 8 — Update stryker config

The four per-layer `stryker-config.json` files collapse to one in `tests/Neba.Api.Tests/`.

New config should:
- Use the single `Neba.Api` project
- Set threshold targets (suggest starting at API-layer values: high=80, low=60, break=75)
- Carry forward `ignore-mutations` for `string`, `Update`, `Description`, `Options`, `Get`, `Version` (all still applicable for FastEndpoints)
- Keep `"test-runner": "mtp"`

Delete stryker configs from `Domain.Tests/`, `Application.Tests/`, `Infrastructure.Tests/` during Phase 7.

### Phase 9 — Update solution file and AppHost

1. Remove deleted projects from `neba-website.sln`.
2. Update `Neba.AppHost.csproj` — remove references to `Neba.Domain`, `Neba.Application`, `Neba.Infrastructure`.
3. Update `Neba.AppHost/Program.cs` if it references any assembly marker types from the deleted projects.

### Phase 10 — Update CLAUDE.md and CI

**CLAUDE.md changes:**
- Remove "Layer Boundaries" rules that reference separate projects
- Replace with folder-convention rules:
  - Domain types live in `Neba.Api/Features/{BoundedContext}/Domain/`; cross-cutting primitives live in `Features/Shared/`
  - Feature handlers must not reference domain types from a sibling bounded context's `Domain/` folder directly — go through the owning feature's public types
  - `internal` on entity factory methods is enforced by convention, not assembly boundary (see Key Decision 4)
- Remove per-layer mutation thresholds table; replace with single API-layer threshold
- Remove `Neba.Domain.Tests`, `Neba.Application.Tests`, `Neba.Infrastructure.Tests` from workflow commands
- Add note that Architecture.Tests has been deleted
- Update `dotnet stryker` commands to reference single test project

**CI (`.github/workflows/*.yml`):**
- Remove any steps that run tests against deleted projects by name
- Update mutation test commands

---

## Testing Strategy Post-Migration

| Test type | Location | Notes |
|---|---|---|
| Domain entity / aggregate unit tests | `Neba.Api.Tests/Domain/` | Keep as unit tests; no EF needed |
| Handler unit tests (query handlers) | `Neba.Api.Tests/{Feature}/` | Now inject AppDbContext; become integration tests using Testcontainers |
| Handler unit tests (command handlers) | `Neba.Api.Tests/{Feature}/` | Same — real DB via Testcontainers |
| Endpoint tests (FastEndpoints Configure) | `Neba.Api.Tests/{Feature}/` | Unchanged — Factory.Create<T>() still works |
| Infrastructure integration tests | `Neba.Api.Tests/Data/` | Testcontainers, AzuriteFixture — unchanged |
| Blazor component tests | `Neba.Website.Tests/` | Unchanged |
| E2E tests | `tests/e2e/` | Unchanged |

The shift: handler tests that previously used `MockBehavior.Strict` mocks on `I*Queries` become Testcontainers integration tests. This is higher-confidence anyway — mock tests couldn't catch EF query bugs.

---

## Validation Checkpoints

After each phase, verify before moving on:

```bash
dotnet build                                    # must pass
dotnet test --filter "Category=Unit"            # domain entity tests
dotnet test --filter "Category=Integration"     # after Phase 7
aspire run                                      # full stack smoke test
```

After Phase 7:
```bash
dotnet ef dbcontext info --project src/Neba.Api # migrations still resolve
```

After Phase 10:
```bash
cd tests/Neba.Api.Tests && dotnet stryker       # single stryker run
```
