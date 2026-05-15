---
applyTo: "**"
---

# Pull Request Review Guidelines

> **For GitHub Copilot**: Use these guidelines when reviewing pull requests. Flag violations as suggestions for refactoring, not blocking requests. The codebase follows Vertical Slice Architecture, DDD, and CQRS patterns.

---

## Architecture & Feature Boundaries

### Domain (Feature Domain Types in `Features/*/Domain/`)

**Treat each feature domain namespace as a separate bounded context.** Types under `Features/Tournaments/Domain`, `Features/Bowlers/Domain`, `Features/BowlingCenters/Domain`, etc. should never directly reference each other's domain objects — review as if they were separate assemblies without project references.

**Exception — strongly-typed IDs only**: A feature domain may import a strongly-typed ID from another feature's domain (e.g., `HallOfFame` importing `BowlerId` from `Features.Bowlers.Domain`) when it needs to record a cross-context foreign key relationship. This is analogous to a database FK: the context stores the _identifier_, not the object. Importing the full aggregate, entity, value objects, or domain services of another feature is still prohibited.

**Do NOT flag** `internal` navigation properties on domain aggregates. These are intentional: EF Core entity configurations in `Neba.Api/Database/` live in the same assembly (`Neba.Api`) and rely on `internal` access for mapping and query projections. The `internal` modifier is what prevents external assemblies (Contracts, Website) from using these properties. Flagging them as an accessibility problem misreads the intent.

Flag when:

- A feature domain folder imports domain objects from another feature domain folder (aggregates, entities, value objects, domain services, or enums — e.g., `Tournaments/Domain` importing `Bowler`, not `BowlerId`)
- Cross-cutting domain base types (`AggregateRoot`, `IDomainEvent`) are duplicated inside a feature folder instead of using the shared `Neba.Api/Domain/` types
- Domain entities expose public setters or mutable collections
- Aggregates lack domain event support when state changes occur
- Business logic appears outside the domain layer
- A child entity owned by an aggregate does **not** have an `internal static ErrorOr<T> Create(...)` factory — every child entity must own its structural invariants through this factory, even if validation is minimal (e.g., non-negative amount). The `internal` modifier ensures construction is only possible from the same assembly (the aggregate root or `InternalsVisibleTo` test helpers)
- A child entity owned by an aggregate has a `public static Create(...)` factory — it should be `internal` so construction is only possible through the aggregate root (by convention — both live in `Neba.Api`)
- An aggregate's assign/add method validates child entity invariants directly (e.g., checking `blockScore > 0` on `Season`) instead of delegating to the child entity's `internal static Create(...)` factory
- A child entity is instantiated directly via `new` outside the aggregate root — application or test code must go through the aggregate's assign methods
- An application handler computes a domain formula and passes the derived result to an aggregate — raw input data should be passed instead; the formula belongs in the domain (e.g., computing `minimumGames = floor(4.5 × count)` in a handler rather than passing `statEligibleTournamentCount` to the aggregate)
- A new aggregate, entity, or value object is introduced without a corresponding entry in `docs/ubiquitous-language.md` — every new domain type needs a UL definition so the vocabulary stays shared across code, docs, and conversation
- A new aggregate, entity, or value object has an XML `<summary>` comment that contradicts or omits the purpose described in the UL — comments don't need to be word-for-word matches, but must convey the same concept to an engineer reading the code cold
- An existing domain type is touched and its XML `<summary>` is absent or misleading relative to its UL entry — take a quick pass over the UL when reviewing domain changes and flag any pre-existing gaps encountered along the way

### Handlers (Feature Slice Use Case Folders)

Handlers live at `Features/{Feature}/{UseCase}/`, co-located with their command/query type and DTO. Each handler implements `IQueryHandler<,>` or `ICommandHandler<,>` and injects `AppDbContext` directly.

**Commands must return `ErrorOr<T>`** — flag any command handler that throws exceptions for business rule violations or returns raw types.

**Cross-feature data access is allowed in handlers** — a handler may query data from multiple feature domains inline (e.g., counting stat-eligible tournaments when assigning a season award). The handler provides facts to the aggregate; the aggregate enforces rules.

Flag when:

- Command handlers don't return `ErrorOr<T>`
- Query handlers return domain entities instead of DTOs/response types
- Direct instantiation of another feature's aggregates (should go through the aggregate's own factory methods)
- Missing `CancellationToken` propagation in async methods
- Query handlers use `.AsTracking()` or omit `.AsNoTracking()` for read-only operations

**Do NOT flag** query or command handlers that appear to be simple pass-throughs. All handlers are wrapped by `TracedQueryHandlerDecorator` / `TracedCommandHandlerDecorator`, which provides automatic telemetry (activity spans, duration tracking, structured error logging). Bypassing the handler pipeline would lose this observability. See [ADR-0003](../../docs/adr/0003-handler-decoration-over-direct-service-calls.md).

### Infrastructure (`Neba.Api/Database/`, `Neba.Api/Caching/`, etc.)

Infrastructure concerns live in dedicated folders within `Neba.Api`. There is no repository abstraction — handlers inject `AppDbContext` directly.

Flag when:

- EF Core entity configurations expose domain-computed properties to the persistence layer (EF configurations should only map columns, not derive business values)
- Raw SQL bypasses EF Core without a clear documented reason
- Feature domain types directly reference EF Core attributes or types (domain types should stay framework-free)

### API Layer (`Neba.Api`)

**Structure**: Each endpoint lives in a use case folder with endpoint, summary, validator, command/query, and handler:

```
Neba.Api/Features/Tournaments/CreateTournament/
├── CreateTournamentEndpoint.cs
├── CreateTournamentSummary.cs
├── CreateTournamentValidator.cs
├── CreateTournamentCommand.cs
└── CreateTournamentCommandHandler.cs
```

Flag when:

- Business logic appears in endpoints (should be in domain types or handler)
- Files in wrong folders (e.g., validator in Contracts project, handler outside its use case folder)
- Missing Summary class
- Mixing concerns (multiple use cases in one folder)
- Handler or DTO defined outside the use case folder it belongs to

#### Endpoint Configuration

Every endpoint's `Configure()` method must include:

- HTTP verb and RESTful route
- `Group<TEndpointGroup>()` configured
- `Version()` explicitly specified (even if defaulting to 1)
- Authorization **explicitly** configured (`AllowAnonymous()`, `Roles()`, or `Policies()`)
- `Tags()` with domain and visibility (e.g., `"Tournaments", "Authenticated"`)
- `Description()` with `WithName()` (required for OpenAPI)
- `Produces()`/`ProducesProblemDetails()` for all status codes

Flag when:

- Authorization not explicitly configured (implicit defaults are not allowed)
- Missing `WithName()` in Description
- Visibility tag doesn't match authorization (e.g., `AllowAnonymous()` with `"Authenticated"` tag)
- Action-based routes (`/api/tournaments/create` instead of `/api/tournaments`)
- Missing status code documentation

#### Validation

Validators should only contain structural validation:

- ✅ Required fields, length constraints, range validation, format validation
- ❌ Cross-property validation (belongs in handler)
- ❌ Database lookups (belongs in handler)
- ❌ Business rules (belongs in domain or handler)

Flag when:

- Validator injects repositories or services
- Validator contains `MustAsync` with database queries
- Validation logic appears in endpoint handler instead of validator
- Missing validator when request has input to validate

#### Error Handling

All errors must return ProblemDetails (RFC 9457) via FastEndpoints' built-in `UseProblemDetails()`. Use `AddError()` + `Send.ErrorsAsync(statusCode)` to return `ErrorOr<T>` errors with the appropriate HTTP status code.

**Exception**: A bare `Send.NotFoundAsync()` (HTTP 404 with no body) is acceptable when the 404 status code itself is sufficient documentation of the error — e.g. a simple "document not found" GET endpoint where the caller only needs to know the resource doesn't exist. Do NOT flag this pattern.

Flag when:

- Custom error response bodies used instead of ProblemDetails (for errors other than bare 404)
- Not handling all error cases from `ErrorOr<T>` result
- Using `SendAsync()` with custom error objects
- Missing error case handling (assuming success without checking `result.IsError`)
- Using `result.IsFailure` or `result.Error` instead of `result.IsError` / `result.FirstError` (ErrorOr API)

#### Error Codes

Error codes must follow the `Entity.ErrorCode` convention (PascalCase, dot-separated). See [ADR-0004](../../docs/adr/0004-error-code-naming-convention.md).

#### Error Types (`Error.Validation` vs `Error.Conflict`)

Use the **retry test** to choose between them: if the caller could retry the exact same request and succeed — without changing their payload — it is a state conflict, not a validation failure.

- `Error.Validation` (422) — the input itself is structurally wrong; the caller must change their payload to fix it (e.g., a score of 0, a missing required field)
- `Error.Conflict` (409) — the input is valid but the system's current state prevents the operation (e.g., season not yet closed, bowler already registered)

Flag when:

- Error codes don't follow `Entity.ErrorCode` pattern (e.g., `"documentNotFound"` instead of `"Document.NotFound"`)
- Error codes use lowercase or camelCase instead of PascalCase
- Error codes are missing (empty string or generic code)
- Application error classes are not named `{Entity}Errors` or are not `internal static`
- `Error.Validation` is used for a state/precondition failure where the caller could retry unchanged (should be `Error.Conflict`)
- `Error.Conflict` is used for a structural input problem (should be `Error.Validation`)

#### Summary Classes

Every endpoint needs a Summary class with:

- Short summary and detailed description
- `ExampleRequest` with realistic data
- `Response<T>()` examples for all status codes (200/201, 400, 404, 409, etc.)

Flag when:

- Missing Summary class
- Summary too brief (e.g., "Create tournament" instead of full description)
- Missing or unrealistic example data
- Missing response examples for error cases

#### Mapping

Mapping should be inline in the endpoint (Request → Command, DTO → Response).

Flag when:

- Separate mapper classes created
- AutoMapper or similar libraries used
- Mapping logic is overly complex (may indicate wrong abstraction level)

### Contracts Layer (`Neba.Api.Contracts`)

**Structure**: Contracts organized by use case folders:

```
Neba.Api.Contracts/Tournaments/CreateTournament/
├── CreateTournamentRequest.cs
└── TournamentInput.cs
```

**Request wraps Input** for commands:

```csharp
public record CreateTournamentRequest
{
    public TournamentInput Tournament { get; init; } = new();
}
```

Flag when:

- Request doesn't wrap Input (properties directly on request)
- Contracts organized by type (`Requests/`, `Responses/`) instead of use case
- Using `{ get; set; }` instead of `{ get; init; }`
- Missing XML documentation (`<summary>`, `<example>` tags)
- Refit interface not updated with new endpoint method

### Blazor (`Neba.Website.Server` / `Neba.Website.Client`)

Flag when:

- Pages contain business logic (pages should be thin orchestrators)
- Components fetch data directly (should receive via parameters)
- Services don't return `ErrorOr<T>`
- Components inject services other than UI services (notifications, navigation)
- Feature-specific components placed in generic `Components/` folder
- Missing loading state handling
- Components placed in Client project without clear justification (offline, browser APIs, latency-sensitive)

---

## REST API Conventions

### URL Structure

- **Plural nouns only**: `/tournaments`, `/bowlers`, `/bowling-centers`
- **No verbs in URLs**: `GET /tournaments/{id}` not `GET /getTournament/{id}`
- **Kebab-case for multi-word resources**: `/bowling-centers` not `/bowlingCenters`
- **Nested resources for relationships**: `/tournaments/{id}/squads`

### HTTP Methods

| Operation      | Method | URL Pattern       | Success Code |
| -------------- | ------ | ----------------- | ------------ |
| List           | GET    | `/resources`      | 200          |
| Get single     | GET    | `/resources/{id}` | 200          |
| Create         | POST   | `/resources`      | 201          |
| Full update    | PUT    | `/resources/{id}` | 200          |
| Partial update | PATCH  | `/resources/{id}` | 200          |
| Delete         | DELETE | `/resources/{id}` | 204          |

### Query Parameters

- **camelCase**: `pageSize`, `sortBy`, `includeInactive`
- **Pagination**: `page` (1-indexed), `pageSize`
- **Filtering**: Use resource attribute names (`status=active`, `type=senior`)

### Response Envelopes

**Single item:**

```json
{
  "data": { "id": "...", "name": "..." }
}
```

**Collection:**

```json
{
  "items": [...],
  "totalCount": 100
}
```

Collection types should be `IReadOnlyCollection<T>`.

**Paginated collection:**

```json
{
  "items": [...],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20
}
```

**Error responses:** Must use Problem Details format (RFC 7807) for all error status codes.

Flag when:

- Response shapes don't match these envelopes
- Collections use `List<T>` or `IEnumerable<T>` instead of `IReadOnlyCollection<T>` in public API contracts
- Error responses don't use Problem Details
- Pagination uses 0-indexed pages

---

## Observability

### Logging

Every service and handler should use `ILogger<T>`. Flag when:

- New services or handlers lack logger injection
- Logging sensitive data (PII, credentials, tokens)
- Missing correlation IDs in log scopes for operations that span multiple steps
- Using wrong log levels:
  - `Debug`: Development context, verbose details
  - `Information`: Business events (tournament created, score recorded)
  - `Warning`: Recoverable issues, degraded functionality
  - `Error`: Failures requiring attention

### Distributed Tracing

Flag when:

- New command handlers lack activity spans for business operations
- Spans missing relevant tags (entity IDs, operation type)
- Cross-boundary operations (HTTP, database, external APIs) aren't traced

Suggest spans for:

- Business-critical operations (score submission, registration, tournament completion)
- Operations involving multiple steps
- Operations where timing breakdown aids debugging

### Metrics

Look for opportunities to suggest metrics:

- **Counters**: Business events (registrations, scores recorded), errors by type
- **Histograms**: Operation durations, response times

Flag when:

- Critical business operations lack metric instrumentation
- Metrics capture high-cardinality dimensions (user IDs, timestamps)

---

## Testing

### Architecture Tests

Architecture rules are enforced automatically by `Neba.Architecture.Tests` (ArchUnitNET). These run in CI before unit tests and fail the build on violations — no manual review needed for what they cover.

Flag when:

- A new feature with a domain namespace (e.g., `Neba.Api.Features.NewFeature.Domain`) is added but `BoundedContextNamespaces` in `DomainBoundaryTests.cs` is not updated. This is the **only file that needs updating** when a new feature domain is introduced.
- A new handler interface type is introduced (beyond `ICommandHandler`, `IQueryHandler`, `IBackgroundJobHandler`) without corresponding naming, visibility, and colocation tests added to `Neba.Architecture.Tests`.

### Required Coverage

New code should maintain 80%+ coverage (enforced by SonarQube). Flag when:

- New services lack corresponding unit tests
- New command/query handlers lack tests
- Complex components lack bUnit tests
- Critical user flows lack E2E test consideration

### Test Patterns

**Unit tests must use factory methods for entity creation:**

```csharp
// Correct - uses factory, only specifies what matters for test
var tournament = TournamentFactory.Create(type: TournamentType.Senior);

// Incorrect - manual instantiation
var tournament = new Tournament("Test", DateTime.Now, TournamentType.Senior, ...);
```

**Tests must have trait attributes and display names:**

```csharp
// Correct - has traits and display name
[UnitTest]
[Component("Tournaments.Registration")]
public class RegisterBowlerTests
{
    [Fact(DisplayName = "Should fail when squad is at capacity")]
    public void Should_Fail_When_Squad_At_Capacity() { }

    [Theory(DisplayName = "Should validate age eligibility")]
    [InlineData(49, false, DisplayName = "Under 50 rejected for senior")]
    [InlineData(50, true, DisplayName = "At 50 accepted for senior")]
    public void Should_Validate_Age(int age, bool expected) { }
}

// Incorrect - missing traits and display names
public class RegisterBowlerTests
{
    [Fact]
    public void Should_Fail_When_Squad_At_Capacity() { }
}
```

**`MockBehavior.Strict` eliminates the need for `.Verify()` calls:**

With `MockBehavior.Strict`, any call without a matching `Setup` throws immediately. This means:

- **"Was called with expected args"** — redundant; the `Setup` with specific args already enforces this. If the code calls the method with wrong args (or doesn't call it), the test fails.
- **"Was not called"** — redundant; if no `Setup` exists for a method, `Strict` throws on any invocation.

```csharp
// Correct - Strict setup IS the verification; no .Verify() needed
_storageServiceMock
    .Setup(s => s.UploadFileAsync(
        "documents",
        query.DocumentName,
        expectedDocument.Content,
        expectedDocument.ContentType,
        It.Is<IDictionary<string, string>>(m =>
            m["source-document-id"] == expectedDocument.Id),
        TestContext.Current.CancellationToken))
    .Returns(Task.CompletedTask);

// ... execute code ...

// Assert on the result — no .Verify() calls needed
result.IsError.ShouldBeFalse();

// Incorrect - redundant .Verify() when using MockBehavior.Strict
_storageServiceMock.Verify(
    s => s.UploadFileAsync(It.IsAny<string>(), ...), Times.Once);
```

**Key principle**: With `MockBehavior.Strict`, `Setup` declarations define the expected interaction contract. The test fails immediately if the code deviates from that contract — no explicit `.Verify()` needed.

Flag when:

- Tests manually instantiate domain entities instead of using factories
- New entity, value object, DTO, or response type is added without a corresponding factory class in `Neba.TestFactory` (excludes SmartEnums, strongly-typed IDs, and command/query/job input objects — those don't need factories)
- A new `[StronglyTypedId("ulid-full")]` type is added without an explicit `New()` factory method in its partial struct body (source generators don't run in Stryker's Roslyn compilation; `New()` must be in real source — see [ADR-0006](../../docs/adr/0006-explicit-new-on-stronglytypedid-partial-structs.md))
- Tests don't follow the Arrange-Act-Assert pattern
- Integration tests don't use Bogus factories with seeds for reproducibility
- Missing Verify (snapshot) tests for mapping operations
- Tests missing `[UnitTest]` or `[IntegrationTest]` trait attribute
- Tests missing `[Component]` trait attribute
- Facts or Theories missing `DisplayName` parameter
- InlineData missing `TestDisplayName` for theory test cases
- Test method names not following `<MethodName>_Should<ExpectedOutcome>_When<Condition>` pattern
- Mocking `ILogger<T>` instead of using `NullLogger<T>.Instance` (when log content doesn't matter) or `FakeLogger<T>` from `Microsoft.Extensions.Logging.Testing` (when asserting on log output)
- Using `new Mock<T>()` without `MockBehavior.Strict` parameter
- Using `null!` instead of `#nullable disable`/`#nullable enable` for null testing
- Using `.Verify()` calls when `MockBehavior.Strict` already enforces the interaction contract via `Setup`

**Null testing pattern**: When testing methods that don't accept nullable references but need null passed:

```csharp
[Fact]
public void Method_ShouldThrow_WhenNull()
{
#nullable disable
    string value = null;

    Should.Throw<ArgumentNullException>(() => SomeMethod(value));
#nullable enable
}
```

### What to Test

| What                | Required Tests                                               |
| ------------------- | ------------------------------------------------------------ |
| Blazor services     | Mock Refit interface, verify ErrorOr mapping, error handling |
| Command handlers    | Business rule enforcement, domain event raising, error cases |
| Query handlers      | Correct DTO mapping (use Verify snapshots)                   |
| Domain aggregates   | Invariant enforcement, state transitions, error cases        |
| Complex components  | bUnit tests for interaction logic, conditional rendering     |
| JS modules          | Jest tests for function behavior                             |

**Logging in tests**: Never mock `ILogger<T>`. Use `NullLogger<T>.Instance` when you don't need to assert on log output. Use `FakeLogger<T>` from `Microsoft.Extensions.Logging.Testing` (namespace inside the `Microsoft.Extensions.Diagnostics.Testing` NuGet package) when you need to assert on log level, message content, or structured attributes — it's a real `ILogger<T>` implementation, not a mock. Assert via `logger.Collector.GetSnapshot()`, which returns `IReadOnlyList<FakeLogRecord>` with `.Level` and `.Message` on each entry.

### E2E Consideration

Suggest E2E tests for:

- Multi-step user flows
- Authentication/authorization boundaries
- Critical business operations
- Complex form validation with error recovery

---

## Code Style & Conventions

### C# Language Features

**Use extension members (C# 14) instead of extension methods:**

```csharp
// Correct - extension member syntax
public static class ServiceExtensions
{
    extension(WebApplication app)
    {
        public WebApplication MapDefaultEndpoints()
        {
            // implementation
            return app;
        }
    }
}

// Incorrect - legacy extension method syntax
public static class ServiceExtensions
{
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // implementation
        return app;
    }
}
```

Flag when:

- Extension methods use the legacy `this` parameter syntax instead of `extension()` blocks
- Multiple extension methods for the same type aren't grouped in a single `extension()` block

**Exception**: `[LoggerMessage]` partial methods must use the legacy `this` parameter syntax as required by the source generator.

### Naming

- **Files**: Match type name (`CreateTournamentEndpoint.cs` contains `CreateTournamentEndpoint`)
- **Feature folders**: Plural (`Tournaments/`, `Bowlers/`)
- **Routes**: Lowercase, plural (`/tournaments/{id}`)

### Project Structure

Flag when:

- Feature-specific components not alongside their pages
- Generic components contain domain knowledge
- Files in wrong project (Client vs Server without justification)

### Contracts (`Neba.Api.Contracts`)

See detailed criteria in **API Layer** section above. Additionally flag when:

- Contract types contain logic beyond simple computed properties

---

## Common Anti-Patterns to Flag

| Anti-Pattern                                             | Correct Approach                                                                                                                                 |
| -------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| Throwing exceptions for business rule violations         | Return `ErrorOr<T>` with typed errors                                                                                                            |
| Domain entity in API response                            | Map to response DTO                                                                                                                              |
| Service injected into component                          | Pass data via parameters from page                                                                                                               |
| `async void` methods                                     | `async Task` with proper error handling                                                                                                          |
| Catching generic `Exception`                             | Catch specific exceptions or use ErrorOr                                                                                                         |
| Magic strings for routes/keys                            | Constants or strongly-typed alternatives                                                                                                         |
| Public setters on entities                               | Private setters with behavior methods                                                                                                            |
| `DateTime.Now` / `DateTime.UtcNow` in domain logic       | Inject `IDateTimeProvider` / `TimeProvider`                                                                                                      |
| `DateTime` for representing points in time               | Use `DateTimeOffset` — unambiguous UTC offset, cleaner serialization                                                                             |
| Legacy extension method syntax (`this` parameter)        | Use `extension()` blocks (C# 14)                                                                                                                 |
| Custom error response body in endpoint                   | Use `AddError()` + `Send.ErrorsAsync(statusCode)` for ProblemDetails (bare `Send.NotFoundAsync()` is acceptable when status alone is sufficient) |
| Implicit endpoint authorization                          | Explicit `AllowAnonymous()`, `Roles()`, or `Policies()`                                                                                          |
| Validation in endpoint handler                           | Create separate `Validator<TRequest>` class                                                                                                      |
| Database lookup in validator                             | Move to Application layer handler                                                                                                                |
| Request properties without Input wrapper                 | Wrap in `TournamentInput` (for commands)                                                                                                         |
| Separate mapper classes for endpoints                    | Inline mapping in endpoint                                                                                                                       |
| URL-based API versioning (`/api/v1/...`)                 | Header-based versioning (`X-Api-Version`)                                                                                                        |
| Direct use of Newtonsoft.Json (`JsonConvert`, `JObject`) | System.Text.Json with source generators                                                                                                          |
| AutoMapper, Mapster, or similar mapping libraries        | Explicit inline mapping                                                                                                                          |
| Unsealed classes without justification                   | Seal classes by default                                                                                                                          |
| Value objects as mutable class                           | Use `sealed record class` (EF persisted) or `readonly record struct` (transient)                                                                 |
| Unbounded database queries                               | Always use `.Take()` with enforced maximum limits                                                                                                |
| Inconsistent or missing error codes                      | Follow `Entity.ErrorCode` convention ([ADR-0004](../../docs/adr/0004-error-code-naming-convention.md))                                           |

### Banned Libraries

The following libraries are explicitly prohibited from direct use in application code:

| Library                                        | Reason                                                                         | Alternative                             |
| ---------------------------------------------- | ------------------------------------------------------------------------------ | --------------------------------------- |
| **AutoMapper**, **Mapster**, **ExpressMapper** | Runtime reflection, hidden mappings, hard to debug, breaks compile-time safety | Explicit mapping methods                |
| **Newtonsoft.Json** (`JsonConvert`, `JObject`) | Reflection-based, not AOT-compatible, legacy                                   | System.Text.Json with source generators |
| **BinaryFormatter**                            | Security vulnerabilities, deprecated                                           | System.Text.Json, MessagePack, Protobuf |

**Note on transitive dependencies**: Some packages (e.g., Hangfire) have transitive dependencies on Newtonsoft.Json. The package may exist in the dependency graph, but **direct usage in our code is prohibited**. Flag any `using Newtonsoft.Json` statements or direct calls to `JsonConvert`.

---

## Review Checklist

When reviewing, verify:

### Architecture & Code Quality

- [ ] Feature boundaries respected (no cross-feature domain references)
- [ ] Commands return `ErrorOr<T>`
- [ ] Queries return DTOs, not entities
- [ ] Extension methods use `extension()` block syntax, not legacy `this` parameter
- [ ] `DateTimeOffset` used instead of `DateTime` for points in time

### Ubiquitous Language

- [ ] Every new aggregate, entity, and value object has an entry in `docs/ubiquitous-language.md`
- [ ] XML `<summary>` comments on new domain types convey the same concept as their UL entry (not word-for-word, but purpose-aligned)
- [ ] A quick scan of existing UL entries and XML comments for domain types touched in this PR — flag any pre-existing gaps or contradictions found in passing

### API Endpoints

- [ ] Use case folder structure followed (Endpoint, Summary, Validator)
- [ ] Authorization **explicitly** configured (`AllowAnonymous()`, `Roles()`, or `Policies()`)
- [ ] `WithName()` present in Description
- [ ] Tags match authorization (Public/Authenticated/Admin)
- [ ] All status codes documented with `Produces()`/`ProducesProblemDetails()`
- [ ] Validator present (if request has input to validate)
- [ ] Validator contains only structural validation (no DB lookups, no business rules)
- [ ] All errors return ProblemDetails
- [ ] Summary class with realistic examples
- [ ] Inline mapping (no separate mapper classes)

### Contracts

- [ ] Request wraps Input for commands
- [ ] XML documentation on all public types and properties
- [ ] Using `{ get; init; }` not `{ get; set; }`
- [ ] Refit interface updated

### REST Conventions

- [ ] REST conventions followed (plural nouns, no verbs in URLs)
- [ ] Response envelopes consistent

### Testing

- [ ] Tests use factories, not manual instantiation
- [ ] New entity/value object/DTO/response has a corresponding factory in `Neba.TestFactory` (SmartEnums, strongly-typed IDs, and input objects are exempt)
- [ ] New `[StronglyTypedId("ulid-full")]` type has an explicit `New()` factory method in its partial struct body (not relying solely on source generation — see [ADR-0006](../../docs/adr/0006-explicit-new-on-stronglytypedid-partial-structs.md))
- [ ] Tests have `[UnitTest]` or `[IntegrationTest]` trait
- [ ] Tests have `[Component]` trait
- [ ] Tests have `DisplayName` on Facts and Theories
- [ ] New code has corresponding tests
- [ ] API endpoint integration tests cover success, validation failure, and auth failure
- [ ] New feature domain namespace added to `BoundedContextNamespaces` in `DomainBoundaryTests.cs`

### Observability

- [ ] Logging present with appropriate levels
- [ ] Spans added for business operations
- [ ] No sensitive data logged

### Blazor

- [ ] Blazor components don't fetch data directly
