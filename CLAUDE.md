# Code Standards

Before implementing or reviewing code, read `.github/instructions/pull-request-review.instructions.md` for PR review guidelines that apply to all code in this repository.

For detailed architectural context:

- Backend: `docs/architecture/backend.md` (or wherever you put ARCHITECTURE.md)
- Blazor: `docs/architecture/blazor.md`

## Self-Maintenance

This file is a **living document** and should be kept current as the project evolves. Both Claude and GitHub Copilot can leverage these learnings to provide better assistance.

When you discover something important during a session, update this file to capture:

- **Learnings**: Project-specific patterns, conventions, or gotchas discovered during work
- **Common fixes**: Solutions to recurring issues or errors
- **Preferences**: User workflow preferences expressed during conversations

Before ending a session where significant discoveries were made, consider whether they should be documented here for future reference.

## Architecture Rules

### Feature Boundaries

- Feature domain folders (`Features/Bowlers/Domain`, `Features/Tournaments/Domain`, etc.) must NOT cross-reference each other's domain objects (aggregates, entities, value objects, domain services). Exception: importing a strongly-typed ID from another feature's domain (e.g., `BowlerId` from `Neba.Api.Features.Bowlers.Domain` in `HallOfFame`) is allowed — it's a typed foreign key, not a domain dependency.
- Commands return `ErrorOr<T>`, never throw for business rules
- Queries return DTOs, never domain entities
- Validators handle structural validation only (no DB lookups, no business rules)
- Use `Error.Validation` (422) when the input itself is wrong; use `Error.Conflict` (409) when the input is valid but the system's current state prevents the operation. Retry test: if the caller could resend the exact same payload and succeed after a state change, it's `Conflict`.
- Methods returning collections — whether directly (`List<T>`, `IEnumerable<T>`, etc.) or wrapped (`Task<List<T>>`) — must never return `null`. Return an empty collection instead. Nullable collection return types (`List<T>?`, `IEnumerable<T>?`, etc.) are not permitted unless there is an explicit, documented reason why `null` is semantically distinct from empty for that method.

### Always-Valid Entities and Aggregate Assignment

Child entities owned by an aggregate use `internal static ErrorOr<T> Create(...)` factory methods that validate the entity's own structural invariants. The `internal` modifier restricts construction to the same assembly (`Neba.Api`); by convention, only the owning aggregate root calls these factories — never handler or test code directly.

The aggregate root's assign methods take raw properties, call the internal factory, enforce aggregate-level invariants (e.g., `Complete == true`), and return a single `ErrorOr<Success>` to the caller:

```csharp
// Child entity owns its own invariants — internal so only Season can construct it
internal static ErrorOr<HighBlockAward> Create(BowlerId bowlerId, int blockScore)
{
    if (blockScore <= 0)
        return Error.Validation("HighBlockAward.BlockScore", "Block score must be greater than zero.");
    return new HighBlockAward { Id = SeasonAwardId.New(), BowlerId = bowlerId, BlockScore = blockScore };
}

// Aggregate enforces its own invariant and delegates entity validation to the entity
public ErrorOr<Success> AssignHighBlockAward(BowlerId bowlerId, int blockScore)
{
    if (!Complete)
        return Error.Conflict("Season.NotComplete", "Awards may only be assigned to a completed season.");
    var award = HighBlockAward.Create(bowlerId, blockScore);
    if (award.IsError) return award.Errors;
    _highBlockAwards.Add(award.Value);
    return Result.Success;
}
```

**Why this matters**: If entity validation lived on the aggregate, the aggregate would absorb invariants that have nothing to do with it. If `Create()` were public, the entity could be constructed in an invalid state outside the aggregate. The internal factory gives call-site simplicity (single `ErrorOr` chain) while keeping each invariant owned by the right type.

### Aggregate Invariants Requiring Cross-Aggregate Data

When an assign method's invariant depends on data owned by another aggregate, the handler queries that data and passes it as a parameter. The aggregate enforces the rule; the handler provides the facts.

**The deciding factor — persist on aggregate vs. pass as parameter**:

- **Live data owned by another aggregate** → pass as a parameter. The other aggregate remains the single source of truth. Duplicating it creates redundancy. Example: `statEligibleTournamentCount` for `AssignHighAverageWinner` — tournaments own this fact, not Season.
- **Per-instance formula coefficients** → persist on the aggregate, set at a lifecycle transition. The formula belongs in the domain; the coefficient may legitimately vary per instance and must be frozen with the aggregate's closed state. Example: `_minimumGamesMultiplier` is set at `Season.Close()` because an abbreviated season might use a different threshold than a regular season.

```csharp
// Application layer provides the cross-aggregate fact; aggregate enforces the rule
public ErrorOr<Success> AssignHighAverageWinner(
    BowlerId bowlerId, decimal average, int games, int? tournamentsParticipated,
    int statEligibleTournamentCount)
{
    if (!Complete)
        return SeasonErrors.SeasonNotComplete;

    var minimumGames = ComputeMinimumGames(statEligibleTournamentCount);
    if (games < minimumGames)
        return SeasonErrors.InsufficientGames(games, minimumGames);

    var award = HighAverageAward.Create(bowlerId, average, games, tournamentsParticipated);
    if (award.IsError) return award.Errors;
    _highAverageAwards.Add(award.Value);
    return Result.Success;
}

// Formula is domain logic — lives on the aggregate, not the handler
private int ComputeMinimumGames(int statEligibleTournaments) =>
    (int)Math.Floor(_minimumGamesMultiplier * statEligibleTournaments);
```

The handler orchestrates — queries the cross-aggregate fact once, then drives the aggregate:

```csharp
var statEligibleCount = await appDbContext.Tournaments
    .CountAsync(t => t.SeasonId == command.SeasonId && t.StatEligible, ct);
season.AssignHighAverageWinner(command.BowlerId, command.Average, command.Games,
    command.TournamentsParticipated, statEligibleCount);
```

**Anti-pattern**: Computing a domain formula in the handler and passing the derived result (e.g., pre-computing `minimumGames` and passing it in). When the formula changes, the fix belongs in the domain — not scattered across handlers.

### Testing Requirements

#### Mutation Testing

Mutation testing (Stryker) is **not currently in the CI pipeline** — removed May 2026. Stryker configs (`stryker-config.json`) and local tooling remain in place for manual runs. See the `## Learnings` section below for notes on known Stryker limitations.


#### .NET Testing Requirements

- All tests need `[UnitTest]` or `[IntegrationTest]` trait
- All tests need `[Component("FeatureName")]` trait
- All Facts/Theories need `DisplayName`
- All test methods must include explicit AAA section comments: `// Arrange`, `// Act`, `// Assert`
- Use `MockBehavior.Strict` for all mocks
- Use `NullLogger<T>.Instance`, never mock ILogger
- Use test factories from `Neba.TestFactory`, never manual entity instantiation
- Test factories follow a consistent pattern: `Create()` with nullable params (const defaults), `Bogus(int count, int? seed)` for collection
- **`Create()` must always produce a persistable entity with no arguments** — every default must satisfy all domain invariants and EF constraints (e.g., required complex properties). If a test fails because `Create()` produces an invalid entity when called with no arguments, fix the factory default rather than patching the test. Example: `AddressFactory.CreateUsAddress()` passes `null` coordinates, but `BowlingCenterFactory.Create()` must call it with `coordinates: AddressFactory.ValidCoordinates` so the default address satisfies EF's non-nullable `Coordinates` constraint.
- Use a seed with `Bogus` only when the specific data values matter to the assertion (e.g., snapshot tests, integration tests for reproducibility). Omit the seed when only shape/count/type matters — the test is clearer without it
- When seeds are used, each test should use a distinct seed value — don't reuse the same seed across multiple tests
- Infrastructure services wrapping external SDKs (e.g., Azure Blob Storage) use Testcontainers for integration tests, not mocks
- Use **Shouldly** for assertions only; do not use FluentAssertions
- When testing null inputs on non-nullable parameters (nullable reference types are enabled project-wide), wrap the test method with `#nullable disable` / `#nullable enable` instead of using `null!`:

  ```csharp
  #nullable disable
  [Fact(DisplayName = "...")]
  public void Method_ShouldReturnError_WhenInputIsNull()
  {
      var result = SomeMethod(null);
      // assertions
  }
  #nullable enable
  ```

### API Endpoint Checklist

- Use case folder structure: Endpoint + Summary + Validator
- Authorization explicitly configured (never implicit) - use `AllowAnonymous()`, `Roles()`, or `Policies()`
- `WithName()` in Description for OpenAPI
- `Produces()`/`ProducesProblemDetails()` for all status codes
- Request wraps Input for commands

### Bug Fixing (TDD Approach)

1. Write a failing test that demonstrates the bug FIRST
2. Choose test project based on what's broken:
   - Domain entity/aggregate (in `Features/*/Domain/`) → Unit test in `Neba.Api.Tests`
   - Handler (in `Features/*/`) → Unit test in `Neba.Api.Tests`
   - EF Core / Database (in `Database/`) → Integration test in `Neba.Api.Tests`
   - API endpoint → Integration test in `Neba.Api.Tests`
   - Blazor component → bUnit test in `Neba.Website.Tests`
   - UI interaction/flow → E2E test in `tests/e2e/`
3. Verify the test fails (proves it catches the bug)
4. Make minimal code change to fix
5. Verify test passes
6. Run full test suite for regressions

## Workflow Commands

- **Full stack**: `aspire run`
- **Unit tests**: `dotnet test --filter "Category=Unit"`
- **Integration tests**: `dotnet test --filter "Category=Integration"`
- **Specific component**: `dotnet test --filter "Component=Tournaments"`
- **E2E tests**: `npm run test:e2e`
- **CI status**: `gh run list --limit 5`
- **CI failure details**: `gh run view <run-id> --log-failed`

## Learnings

### API Route Conventions

- **No `/api` prefix** — the API is served from `api.bowlneba.com`, so routes start directly with the resource (e.g. `/documents/{DocumentName}`, not `/api/documents/{DocumentName}`)
- **No version in path** — API versioning is handled via request headers, not URL segments (no `/v1/`, `/api/v1/`, etc.)

### API Layer Mutation Testing — FastEndpoints Unit Test Limitations

When writing Configure tests with `Factory.Create<TEndpoint>()`, several categories of mutations are permanently unkillable:

1. **`Get(...)` calls** — FastEndpoints source generation pre-registers route templates at compile time via `SelfRegisteredExtensions.cs`. Even when `Get(...)` is removed from `Configure()`, `Definition.Routes` still contains the route template. Assert routes using `ShouldContain()`, but the route mutation will always survive. Add `"Get"` to `ignore-methods`.

2. **`Version(...)` calls** — `endpoint.Definition.Version.Current` is always 0 when using `FastEndpoints.AspVersioning` (version is applied via `MapToApiVersion` in an `Options()` delegate, not via direct `Version()` call). Add `"Version"` to `ignore-methods`.

3. **`Description(...)` and `Options(...)` calls** — Both store `Action<RouteHandlerBuilder>` delegates that are only invoked during real app startup (not in `Factory.Create<>()` unit tests). `EndpointMetadata` is always empty in unit tests, so `TagsAttribute` lookups return 0 items. Add `"Description"` and `"Options"` to `ignore-methods`.

4. **`return;` after `Send.NotFoundAsync()`** — FastEndpoints base class swallows exceptions thrown after the response has been set. Even if `result.Value` throws (ErrorOr v2), the 404 status remains and assertions pass. Use `// Stryker disable once Statement` before these `return;` guards.

5. **`await Send.OkAsync(...)` at the end of `HandleAsync`** — When this is the last statement, removing it is equivalent (no assertion fails on a void-like call with no state side-effects visible to unit tests). Use `// Stryker disable once Statement` before the final `Send.OkAsync` call.

**`ignore-methods` for API layer** (all five categories above): `"Description"`, `"Options"`, `"Get"`, `"Version"` — add all four to stryker-config.json. Use `// Stryker disable once Statement` inline for the `return;` and `Send.OkAsync` guards.

### API Layer Mutation Testing — `static readonly Lazy<>` Limitation

When a class uses `private static readonly Lazy<T>` (e.g., for a cached dictionary built via reflection), the MTP runner shares the same process across mutant runs. Once the `Lazy<>` is initialized by the first mutant run, subsequent mutant runs for methods that only execute during initialization never re-trigger the factory. All mutations inside those methods survive regardless of test quality.

**Fix — two-step inline disable required per init-only method**:

1. `// Stryker disable all` **inside** the method body (as its first statement) — disables all inline mutations (logical, equality, boolean, statement, bitwise, etc.) scoped to that method body. Place in EVERY init-only method.
2. `// Stryker disable once Block` **before** the method declaration — disables the block removal mutation, which operates at the declaration level and is NOT covered by the body-level disable. Place before EVERY init-only method declaration.

```csharp
// Stryker disable once Block : see BuildEnumNamesByTypeName
private static IEnumerable<Assembly> GetDomainAssemblies()
{
    // Stryker disable all : see BuildEnumNamesByTypeName
    EnsureNebaAssembliesLoaded();
    return AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => a.GetName().Name?.StartsWith("Neba.Domain", StringComparison.Ordinal) == true);
}
```

**Scope rules for inline disable comments** (confirmed empirically):

- `// Stryker disable all` inside a method body: scoped to that method body only. Does NOT span to sibling methods even if `// Stryker restore all` is in a later method. Both disable and restore must be in the SAME method body, or disable placed before a method declaration only covers that immediate method.
- `// Stryker disable once Block` before a method declaration: covers the NEXT block mutation (the method body block removal). One comment = one method.
- `// Stryker disable all` / `// Stryker restore all` at CLASS scope (between method declarations): only covers the immediately following method declaration, NOT a range.

This applies to any `ISchemaProcessor`, startup-cached registries, or other static initialization patterns.

### Log-Content Testing with FakeLogger

- Use `FakeLogger<T>` from the `Microsoft.Extensions.Diagnostics.Testing` NuGet package (version `9.0.0` in `Directory.Packages.props`) when a class's primary behavior involves logging and you need to assert on log level, message content, or structured attributes.
- Add `using Microsoft.Extensions.Logging.Testing;` — that is the namespace `FakeLogger<T>` lives in (the NuGet package name and the namespace differ).
- `FakeLogger<T>` is a real `ILogger<T>` implementation — not a mock — so it satisfies the "never mock ILogger" rule.
- Assert via `logger.Collector.GetSnapshot()` which returns `IReadOnlyList<FakeLogRecord>`, each with `.Level` and `.Message`.
- Each test project that uses `FakeLogger<T>` needs `<PackageReference Include="Microsoft.Extensions.Diagnostics.Testing" />` in its `.csproj`.

All classes that use `[LoggerMessage]` source-generated log methods have dedicated log-assertion tests using `FakeLogger<T>`. When adding a new class that logs, add `Microsoft.Extensions.Diagnostics.Testing` to its test project (if not already present) and add log-assertion tests covering every log level/path.

### FusionCache Deserialization Recovery

- Cached query DTOs should use serialization-safe types; do not store domain `SmartEnum` instances directly in cached DTO properties.
- Map SmartEnum values to primitives in query projections (for example, `Status.Name` as `string`) before caching.
- `CachedQueryHandlerDecorator` catches cache deserialization failures on plain cached queries, logs a warning, executes the inner handler, and rewrites the cache entry.
- Keep the cache key stable unless explicitly directed otherwise; deserialization fallback handles stale entry recovery.

### EF Core Navigation Fixup — `= []` Collection Initializers Cause `Collection was of a fixed size`

When a domain entity initializes a collection navigation property with `= []` (C# 12 collection expression), the CLR resolves `IReadOnlyCollection<T> Prop { get; init; } = []` to `T[]` (a fixed-size array). EF Core 10's `ClrCollectionAccessorFactory` picks up this array type as `TCollection`, and when navigation fixup tries to call `AddStandalone(array, value)`, it hits `SZArrayHelper.Add` which throws `System.NotSupportedException: Collection was of a fixed size`.

This affects **both sides** of a relationship: adding a `TournamentSponsor` with a concrete `SponsorId` set causes EF to fix up `Sponsor.TournamentsSponsored` (also `= []`), even if you never set `Tournament = tournament` on the dependent.

**Symptom**: `NotSupportedException: Collection was of a fixed size` in the EF Core navigation fixup stack during integration test seeding.

**Fix in tests**: After saving the principal entities, call `_dbContext.ChangeTracker.Clear()` before adding dependent entities. With no tracked principals in the change tracker, EF has nothing to fixup against.

```csharp
await _dbContext.SaveChangesAsync(ct);

var tournamentDbId = _dbContext.Entry(tournament)
    .Property<int>(ShadowIdConfiguration.DefaultPropertyName).CurrentValue;

_dbContext.ChangeTracker.Clear(); // prevents fixup against tracked sponsors/tournaments

var ts = _dbContext.Set<TournamentSponsor>().Add(new TournamentSponsor { SponsorId = sponsorId, ... });
ts.Property<int>(TournamentConfiguration.ForeignKeyName).CurrentValue = tournamentDbId;

await _dbContext.SaveChangesAsync(ct);
```

**Note**: `PropertyAccessMode.Field` / `Navigation().HasField("_sponsors")` does NOT help — EF still determines `TCollection` from the property type, not the backing field type.

**Ordering constraint when combining TournamentSponsors + other dependents in the same test**: Any entities added via navigation properties to already-saved aggregates (e.g. `HistoricalTournamentChampion { Tournament = tournament }`) must be added and saved **before** `ChangeTracker.Clear()`. After the clear, detached entities passed as navigation properties are re-tracked as `Added`, causing a unique constraint violation on re-insert. The required save order for a fully-populated tournament test is:

1. Save all principals (season, bowling center, tournament, sponsors, bowlers)
2. Add `HistoricalTournamentChampion` entries (tournament + bowlers still tracked) → `SaveChangesAsync`
3. Read `tournamentDbId` from shadow property
4. `ChangeTracker.Clear()`
5. Add `TournamentSponsor` entries via shadow FK → `SaveChangesAsync`

**Stable Verify snapshots for tournaments**: Use explicit IDs via the source-generated `TournamentId(string)` constructor (the `ulid-full.typedid` template generates `public PLACEHOLDERID(string value)`). All-numeric ULID strings are valid (e.g. `"01000000000000000000000001"`). Apply the same to `SeasonId`, `BowlerId`, `SponsorId` — any ID that will appear in the snapshot output.

### Razor @code Block — Parser Limitations

Two patterns that break Razor's lexer even inside `@code { }` blocks:

1. **Relational patterns `< N =>` in switch expressions** — `<` followed by a space then a digit is misread as an HTML tag start, causing the brace tracker to prematurely close the `@code` block. Use `if`/`else` with `>=` instead (e.g., `if (pct >= 90) return "full";`). `<=` (less-than-or-equal) is NOT affected — only bare `<` followed by a space.

2. **String interpolation with `{}` inside @code** — `$"prefix:{expr}suffix"` braces inside string interpolations in `@code` can confuse the Razor brace counter. Use string concatenation instead: `"prefix:" + expr + "suffix"`.

3. **Component attribute values always need `@` for C# expressions** — `Foo="fieldName"` passes the literal string `"fieldName"`, not the field's value. Always write `Foo="@fieldName"` for fields/properties, `Foo="@(expr)"` for expressions with operators (e.g. null-forgiving `!`, null-coalescing `??`).

4. **Blazor parameters use `[EditorRequired]` not C# `required`** — the `required` keyword on Blazor `[Parameter]` properties causes compile errors (CS0246/CS7014). Always use `[Parameter, EditorRequired]` with a default initializer (`= default!;`, `= string.Empty;`, `= [];`).
