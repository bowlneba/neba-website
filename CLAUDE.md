# Code Standards

Before implementing or reviewing code, read `.github/instructions/pull-request-review.instructions.md` for PR review guidelines that apply to all code in this repository.

For detailed architectural context:

- Backend: `docs/architecture/backend.md` (or wherever you put ARCHITECTURE.md)
- Blazor: `docs/architecture/blazor.md`

## Workflow Preferences

- **Show, don't insert**: When suggesting code changes, display the implementation in the response for review rather than directly inserting it into files. Only insert code directly when explicitly requested. Documentation files (`.md`, skill files, architecture docs) are exempt — insert those directly.

## Self-Maintenance

This file is a **living document** and should be kept current as the project evolves. Both Claude and GitHub Copilot can leverage these learnings to provide better assistance.

When you discover something important during a session, update this file to capture:

- **Learnings**: Project-specific patterns, conventions, or gotchas discovered during work
- **Common fixes**: Solutions to recurring issues or errors
- **Preferences**: User workflow preferences expressed during conversations

Before ending a session where significant discoveries were made, consider whether they should be documented here for future reference.

## Architecture Rules

### Layer Boundaries

- Domain folders (Bowlers, Tournaments, etc.) must NOT cross-reference each other's domain objects (aggregates, entities, value objects, domain services). Exception: importing a strongly-typed ID from another context (e.g., `BowlerId` in `HallOfFame`) is allowed — it's a typed foreign key, not a domain dependency.
- Commands return `ErrorOr<T>`, never throw for business rules
- Queries return DTOs, never domain entities
- Validators handle structural validation only (no DB lookups, no business rules)
- Use `Error.Validation` (422) when the input itself is wrong; use `Error.Conflict` (409) when the input is valid but the system's current state prevents the operation. Retry test: if the caller could resend the exact same payload and succeed after a state change, it's `Conflict`.

### Always-Valid Entities and Aggregate Assignment

Child entities owned by an aggregate use `internal static ErrorOr<T> Create(...)` factory methods that validate the entity's own structural invariants. The `internal` modifier ensures the entity can only be constructed through the owning aggregate (same assembly) — never directly from application or test code.

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

When an assign method's invariant depends on data owned by another aggregate, the application layer queries that data and passes it as a parameter. The aggregate enforces the rule; the application layer provides the facts.

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

// Formula is domain logic — lives on the aggregate, not the application layer
private int ComputeMinimumGames(int statEligibleTournaments) =>
    (int)Math.Floor(_minimumGamesMultiplier * statEligibleTournaments);
```

Application layer orchestrates — queries the cross-aggregate fact once, then drives the aggregate:

```csharp
var statEligibleCount = await _tournamentRepository.CountStatEligibleAsync(command.SeasonId, ct);
season.AssignHighAverageWinner(command.BowlerId, command.Average, command.Games,
    command.TournamentsParticipated, statEligibleCount);
```

**Anti-pattern**: Computing a domain formula in the application handler and passing the derived result (e.g., pre-computing `minimumGames` and passing it in). When the formula changes, the fix belongs in the domain — not scattered across handlers.

### Testing Requirements

#### JavaScript Mutation Testing

- Uses **Stryker v9** with `@stryker-mutator/jest-runner`; config: `stryker.config.json`
- Mutates all `src/Neba.Website.Server/**/*.js` (excludes `.tests.js` files)
- A mutation is **killed** when at least one test *fails* on the mutated code
- **"Not covered"** → needs a new test that exercises the code path
- **"Covered, survived"** → test reaches the code but assertions aren't specific enough; sharpen them
- Be pragmatic: `StringLiteral` mutations inside `console.log/warn/error` are low-value
- Arithmetic mutations where one operand is `0` are ambiguous (`a - 0 === a + 0`) — ensure test data uses non-zero baseline values for timing, distances, and similar calculations
- `BlockStatement` mutations (emptying a function body to `{}`) are high-value — they reveal entirely untested code paths and should be prioritized

#### .NET Mutation Testing

- Uses **dotnet-stryker** (global install: `dotnet tool install --global dotnet-stryker`)
- Config per layer: `tests/<Layer>.Tests/stryker-config.json`
- Run from the test project directory: `cd tests/Neba.Domain.Tests && dotnet stryker`
- Diff run (PR): `dotnet stryker --since origin/main`
- Reports land in `tests/<Layer>.Tests/StrykerOutput/`

**New layer checklist** — when adding a stryker-config.json for a new layer, every config must have:

1. `"test-runner": "mtp"` — **required** for xUnit v3; without it all mutants show as Survived (xUnit v3 runs tests out-of-process; Stryker's VSTest extension never receives events). Shipped in Stryker.NET 4.13.
2. `"project-info"` (not `"dashboard"`) — the .NET config key for dashboard reporting; `"dashboard"` is JS-only and will throw an unknown key error.
3. `"reporters": ["html", "json", "progress"]` locally — omit `"dashboard"` from the config; pass `--reporter dashboard` as a CLI flag in CI only (having it in the config requires the API key even locally).
4. `"ignore-mutations": ["string", "Update"]` — always exclude string literals (noise) and update expressions (`i++`/`i--`). The MTP runner in Stryker 4.13 does **not** respect `additional-timeout`, so `i++` → `i--` mutations in `for` loops create infinite loops that hang the entire run. `UpdateExpression` mutations are low-value anyway — loop control infrastructure, not business logic.

**Coverage analysis note**: MTP coverage is partially implemented in 4.13 — uncovered mutants are filtered out, but per-mutant test selection is not yet available. This means runs are slower than they will eventually be (all tests run per mutant), but results are accurate.

**StronglyTypedId + Stryker limitation** — Stryker's in-memory Roslyn compilation invokes source generators but does not pass `AdditionalFiles` (e.g., `ulid-full.typedid`) to them. The `StronglyTypedIds` generator therefore produces no output, causing compile errors when domain source files reference any member that was previously template-generated. Fix: remove `Value { get; }`, the private `(Ulid value)` constructor, and `New()` from the template and define all three explicitly in each ID's partial struct body. Every `[StronglyTypedId("ulid-full")]` type must declare this trio — including test helper structs, not just domain IDs. See [ADR-0006](docs/adr/0006-explicit-new-on-stronglytypedid-partial-structs.md).

**Per-layer decisions** (make these explicitly for each new layer):

- `ignore-mutations: Linq` — keep for Domain/Application (logic layers); exclude for Infrastructure/API/Blazor (see rationale in learnings below)
- `mutate` exclusions — inspect actual files; exclude pure declarations (source-generated stubs, SmartEnum tables), not logic

**Thresholds by layer**:

| Layer          | high | low | break | Notes                                                                                  |
|----------------|------|-----|-------|----------------------------------------------------------------------------------------|
| Domain         | 95   | 90  | 85    |                                                                                        |
| Application    | 95   | 90  | 85    |                                                                                        |
| Infrastructure | 75   | 65  | 0     | Local only — Testcontainers integration tests crash the MTP runner; not wired into CI  |
| API            | 80   | 60  | 75    |                                                                                        |
| Blazor         | 85   | 70  | 65    |                                                                                        |

- A mutation is **killed** when at least one test *fails* on the mutated code
- **"Not covered"** → needs a new test exercising the code path
- **"Covered, survived"** → assertions aren't specific enough; sharpen them

#### .NET Testing Requirements

- All tests need `[UnitTest]` or `[IntegrationTest]` trait
- All tests need `[Component("FeatureName")]` trait
- All Facts/Theories need `DisplayName`
- Use `MockBehavior.Strict` for all mocks
- Use `NullLogger<T>.Instance`, never mock ILogger
- Use test factories from `Neba.TestFactory`, never manual entity instantiation
- Test factories follow a consistent pattern: `Create()` with nullable params (const defaults), `Bogus(int count, int? seed)` for collection
- Use a seed with `Bogus` only when the specific data values matter to the assertion (e.g., snapshot tests, integration tests for reproducibility). Omit the seed when only shape/count/type matters — the test is clearer without it
- When seeds are used, each test should use a distinct seed value — don't reuse the same seed across multiple tests
- Infrastructure services wrapping external SDKs (e.g., Azure Blob Storage) use Testcontainers for integration tests, not mocks
- Use **Shouldly** for assertions, NOT FluentAssertions
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
2. Choose test type based on layer:
   - Domain entity/aggregate → Unit test in `Neba.Domain.Tests`
   - Application handler → Unit test in `Neba.Application.Tests`
   - Infrastructure/EF Core → Integration test in `Neba.Infrastructure.Tests`
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
- **JS mutation tests (full run + summary)**: `npm run mutation:ai`
- **JS mutation report for one file**: `npm run mutation:ai:file -- <FileName>` (e.g. `-- NavMenu`)
- **.NET mutation tests — Domain**: `cd tests/Neba.Domain.Tests && dotnet stryker`
- **.NET mutation tests — Application**: `cd tests/Neba.Application.Tests && dotnet stryker`
- **.NET mutation tests — Infrastructure**: `cd tests/Neba.Infrastructure.Tests && dotnet stryker`
- **.NET mutation tests — API**: `cd tests/Neba.Api.Tests && dotnet stryker`
- **.NET mutation summary**: `npm run mutation:ai:dotnet -- Domain`
- **.NET mutation detail for one file**: `npm run mutation:ai:dotnet -- Domain <FileName>` (e.g. `-- Domain LaneRange`)
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

### Blazor Layer Mutation Testing — C# 14 Extension Block Limitation

Stryker 4.14.1 (latest as of April 2026) **cannot run mutation tests on `Neba.Website.Server`** because its internal Roslyn version does not support C# 14 `extension` block syntax (`extension(T t) { ... }` inside a `static class`). The compile error is a rollback failure after Stryker tries to include these files in compilation.

**Root cause**: Stryker packages its own Roslyn. The C# 14 `extension` block feature requires a Roslyn version newer than Stryker ships.

**Affected files** (use `extension(` blocks):

- `BowlingCenters/BowlingCenterMappingExtensions.cs`
- `HallOfFame/HallOfFameMappingExtensions.cs`
- `History/Awards/BowlerOfTheYearMappingExtensions.cs`
- `History/Awards/HighAverageMappingExtensions.cs`
- `History/Awards/HighBlockMappingExtensions.cs`
- `Sponsors/SponsorMappingExtensions.cs`
- `Services/ApiServicesConfiguration.cs`
- `Maps/MapsConfiguration.cs`

**Workaround**: None until Stryker updates its Roslyn package. Do not attempt to rewrite these files to traditional extension methods — the `extension` block syntax is the project convention per Architecture Patterns.

**What to do**: Skip the Blazor mutation run. When Stryker ships a version that supports C# 14 extension blocks, run `cd tests/Neba.Website.Tests && dotnet stryker` to get the baseline score.

### FusionCache Deserialization Recovery

- Cached query DTOs should use serialization-safe types; do not store domain `SmartEnum` instances directly in cached DTO properties.
- Map SmartEnum values to primitives in query projections (for example, `Status.Name` as `string`) before caching.
- `CachedQueryHandlerDecorator` catches cache deserialization failures on plain cached queries, logs a warning, executes the inner handler, and rewrites the cache entry.
- Keep the cache key stable unless explicitly directed otherwise; deserialization fallback handles stale entry recovery.

### Razor @code Block — Parser Limitations

Two patterns that break Razor's lexer even inside `@code { }` blocks:

1. **Relational patterns `< N =>` in switch expressions** — `<` followed by a space then a digit is misread as an HTML tag start, causing the brace tracker to prematurely close the `@code` block. Use `if`/`else` with `>=` instead (e.g., `if (pct >= 90) return "full";`). `<=` (less-than-or-equal) is NOT affected — only bare `<` followed by a space.

2. **String interpolation with `{}` inside @code** — `$"prefix:{expr}suffix"` braces inside string interpolations in `@code` can confuse the Razor brace counter. Use string concatenation instead: `"prefix:" + expr + "suffix"`.

3. **Component attribute values always need `@` for C# expressions** — `Foo="fieldName"` passes the literal string `"fieldName"`, not the field's value. Always write `Foo="@fieldName"` for fields/properties, `Foo="@(expr)"` for expressions with operators (e.g. null-forgiving `!`, null-coalescing `??`).

4. **Blazor parameters use `[EditorRequired]` not C# `required`** — the `required` keyword on Blazor `[Parameter]` properties causes compile errors (CS0246/CS7014). Always use `[Parameter, EditorRequired]` with a default initializer (`= default!;`, `= string.Empty;`, `= [];`).
