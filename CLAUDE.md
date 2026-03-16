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

**Per-layer decisions** (make these explicitly for each new layer):

- `ignore-mutations: Linq` — keep for Domain/Application (logic layers); exclude for Infrastructure/API/Blazor (see rationale in learnings below)
- `mutate` exclusions — inspect actual files; exclude pure declarations (source-generated stubs, SmartEnum tables), not logic

**Thresholds by layer**:

| Layer          | high | low | break |
|----------------|------|-----|-------|
| Domain         | 95   | 90  | 85    |
| Application    | 95   | 90  | 85    |
| Infrastructure | 60   | 40  | 0     |
| API            | 80   | 60  | 75    |
| Blazor         | 80   | 60  | 70    |

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

When writing Configure tests with `Factory.Create<TEndpoint>()`, two categories of mutations are permanently unkillable:

1. **`Get(...)` calls** — FastEndpoints source generation pre-registers route templates at compile time via `SelfRegisteredExtensions.cs`. Even when `Get(...)` is removed from `Configure()`, `Definition.Routes` still contains the route template. Assert routes using `ShouldContain()`, but the route mutation will always survive.

2. **`Description(...)` and `Options(...)` calls** — Both store `Action<RouteHandlerBuilder>` delegates that are only invoked during real app startup (not in `Factory.Create<>()` unit tests). `EndpointMetadata` is always empty in unit tests, so `TagsAttribute` lookups return 0 items. `endpoint.Definition.Version.Current` is always 0 when using `FastEndpoints.AspVersioning` (not direct FastEndpoints `Version()` call). Add `"Description"` and `"Options"` to `ignore-methods` in the API layer stryker-config.json to skip these unkillable mutations.

3. **`return;` after `Send.NotFoundAsync()`** — FastEndpoints base class swallows exceptions thrown after the response has been set. Even if `result.Value` throws (ErrorOr v2), the 404 status remains and assertions pass. This mutation is unkillable in unit tests.

**Practical limit**: The API layer stryker break threshold is 75%. With `"Description"` and `"Options"` in `ignore-methods`, the score achieves 75% (12/16). The 4 unkillable survivors are: 3 × `Get()` route mutations + 1 × `return;` guard.

### FusionCache Deserialization Recovery

- Cached query DTOs should use serialization-safe types; do not store domain `SmartEnum` instances directly in cached DTO properties.
- Map SmartEnum values to primitives in query projections (for example, `Status.Name` as `string`) before caching.
- `CachedQueryHandlerDecorator` catches cache deserialization failures on plain cached queries, logs a warning, executes the inner handler, and rewrites the cache entry.
- Keep the cache key stable unless explicitly directed otherwise; deserialization fallback handles stale entry recovery.
