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

- Uses **dotnet-stryker** (global install); config: `tests/<Layer>.Tests/stryker-config.json`
- Run from the test project directory: `cd tests/Neba.Domain.Tests && dotnet stryker`
- Diff run (PR): `dotnet stryker --since origin/main`
- Reports land in `tests/<Layer>.Tests/StrykerOutput/`
- **Thresholds — Domain**: high 95 / low 90 / break 85
- **Excluded from mutation** — files with no logic worth mutating:
  - `**/AssemblyInfo.cs` — source generator attribute
  - `**/*Id.cs` — `[StronglyTypedId]` empty partial structs (generated logic)
  - SmartEnum/SmartFlagEnum declaration files (pure lookup tables, private constructors delegate to base): review new files as they are added to decide whether to exclude them using the same criteria
- `ignore-mutations`: `string` (Linq is intentionally kept — LINQ operator mutations in domain logic are high-signal)
- `ignore-methods`: `ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrEmpty`, `Guard.*`, `Log.*`, `Logger.*`
- **High-value mutation targets**: aggregates with business logic, value objects with validation, `DistanceCalculator`, `SmartFlagEnumJsonConverter`
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
- **CI status**: `gh run list --limit 5`
- **CI failure details**: `gh run view <run-id> --log-failed`

## Learnings

### API Route Conventions

- **No `/api` prefix** — the API is served from `api.bowlneba.com`, so routes start directly with the resource (e.g. `/documents/{DocumentName}`, not `/api/documents/{DocumentName}`)
- **No version in path** — API versioning is handled via request headers, not URL segments (no `/v1/`, `/api/v1/`, etc.)

### FusionCache Deserialization Recovery

- Cached query DTOs should use serialization-safe types; do not store domain `SmartEnum` instances directly in cached DTO properties.
- Map SmartEnum values to primitives in query projections (for example, `Status.Name` as `string`) before caching.
- `CachedQueryHandlerDecorator` catches cache deserialization failures on plain cached queries, logs a warning, executes the inner handler, and rewrites the cache entry.
- Keep the cache key stable unless explicitly directed otherwise; deserialization fallback handles stale entry recovery.
