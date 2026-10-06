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

**Keep this file short (about 200 lines).** It loads into every session. Put the full write-up of a learning in the matching `docs/learnings/*.md` file and add at most a one-line rule here. If a rule can be enforced by an analyzer, `.editorconfig`, or an architecture test, enforce it there instead of writing it here.

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
- **Unit tests**: `dotnet test --filter-trait "Category=Unit"`
- **Integration tests**: `dotnet test --filter-trait "Category=Integration"`
- **Specific component**: `dotnet test --filter-trait "Component=Tournaments"`
- Since the `xunit.v3` 4.0.0 upgrade, `dotnet test` runs via the MTP `dotnet test` mode (`global.json`'s `test.runner`), which uses xunit's own `--filter-trait`/`--filter-class`/`--filter-query` syntax instead of VSTest's `--filter`. A project with zero tests matching the trait now exits non-zero ("Zero tests ran", exit code 8) instead of silently succeeding — expected when filtering a solution where only some projects carry a given trait; check each project's summary line, not just the overall exit code.
- **E2E tests**: `npm run test:e2e`
- **CI status**: `gh run list --limit 5`
- **CI failure details**: `gh run view <run-id> --log-failed`

## Learnings

Detailed write-ups live in `docs/learnings/`. Read the matching file before working in that area. The one-line rules below always apply.

### API route conventions

- **No `/api` prefix** — the API is served from `api.bowlneba.com`, so routes start directly with the resource (e.g. `/documents/{DocumentName}`, not `/api/documents/{DocumentName}`)
- **No version in path** — API versioning is handled via request headers, not URL segments (no `/v1/`, `/api/v1/`, etc.)

### Blazor and UI — `docs/learnings/blazor-ui.md`

- Per-keystroke input behavior (auto-advance, key filtering) lives in a colocated `.razor.js` module, never in C# `@onkeydown` handlers (SignalR round-trips race).
- Every data-entry page uses `DirtyFormGuard`; the page owns dirty tracking and calls `MarkDirty()` for anything not bound through an `InputBase`.
- Mark required fields with `FormLabel` ("(required)"), not optional ones. Login forms are exempt.
- Pickers over ~20 items use `NebaAutocomplete`, not `InputSelect`.
- Admin list pages add items through `FabCreateButton`.
- Every routable page has `<PageTitle>` (`{Page} - BowlNEBA`) and `@rendermode InteractiveServer` (not `Login`/`Logout`).
- Razor `@code`: no `< N =>` patterns, no `$"{x}"` interpolation, `@` on component attribute values, `[Parameter, EditorRequired]` instead of `required`.

### Testing — `docs/learnings/testing.md`

- Never mutate process-wide static state in an integration test (`UseFastEndpoints()` needs `UsePropertyNamingPolicy = false`; reset Hangfire statics in `DisposeAsync`; stop, never dispose, a `WebApplication`).
- Polling helpers must pick the latest event and tolerate a torn read on a live collection.
- Assert log content with `FakeLogger<T>`; assert redaction through a DI container with `EnableRedaction()`.
- FastEndpoints mutation testing has unkillable mutants; see the file for `ignore-methods` and Stryker disable comments.

### Backend — `docs/learnings/backend.md`

- Reuse an existing query and project down before adding a lightweight one. Name the DTO `*Summary`, not the query or route.
- Put `[PersonalData]`/`[PrivateData]` on any `[LoggerMessage]` parameter carrying PII. `AddRedaction()` needs `EnableRedaction()` too.
- Cached query DTOs hold primitives, never SmartEnum instances.
- Never initialize an EF collection navigation with `= []` on an `IReadOnlyCollection<T>` auto-property (it becomes a fixed-size array); use a `List<T>` backing field. Suppress IDE0305 and IDE0028 where you must use `new List<T>()`, and add an `ICollection<T>.Add` regression test.
- Emails: inline styles, `<table>` layout, hosted logo URL, `WebUtility.HtmlEncode` on user values.

### Hangfire PostgreSql — `docs/learnings/hangfire-postgres.md`

- `EnableTransactionScopeEnlistment` stays `false`. Dev runs one worker, so one wedged job stops all local jobs.
