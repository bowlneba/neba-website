---
applyTo: '**'
---

# Pull Request Review Guidelines

> **For GitHub Copilot**: Use these guidelines when reviewing pull requests. Flag violations as suggestions for refactoring, not blocking requests. The codebase follows Clean Architecture, DDD, and CQRS patterns.

---

## Architecture & Layer Boundaries

### Domain Layer (`Neba.Domain`)

**Treat each root-level namespace as a separate bounded context.** Folders like `Tournaments`, `Bowlers`, `BowlingCenters`, and `Membership` should never directly reference each other — review as if they were separate assemblies without project references.

Flag when:

- A domain folder imports types from another domain folder (e.g., `Tournaments` importing from `Bowlers`)
- Types that should live in `SharedKernel` are defined within a specific domain folder (cross-cutting IDs, shared value objects, domain event interfaces)
- Domain entities expose public setters or mutable collections
- Aggregates lack domain event support when state changes occur
- Business logic appears outside the domain layer

### Application Layer (`Neba.Application`)

**Commands must return `ErrorOr<T>`** — flag any command handler that throws exceptions for business rule violations or returns raw types.

**Cross-domain orchestration is allowed here**, but should occur through application services that call the "public API" of each domain (repository interfaces, domain services), not by directly manipulating another domain's internals.

Flag when:

- Command handlers don't return `ErrorOr<T>`
- Query handlers return domain entities instead of DTOs/response types
- Direct instantiation of another domain's aggregates (should go through that domain's factory or repository)
- Missing `CancellationToken` propagation in async methods

**Do NOT flag** query or command handlers that appear to be simple pass-throughs to an infrastructure service. All handlers are wrapped by `TracedQueryHandlerDecorator` / `TracedCommandHandlerDecorator`, which provides automatic telemetry (activity spans, duration tracking, structured error logging). Bypassing the handler pipeline would lose this observability. See [ADR-0003](../../docs/adr/0003-handler-decoration-over-direct-service-calls.md).

### Infrastructure Layer (`Neba.Infrastructure`)

Flag when:

- Repository implementations return domain entities for query operations (queries should return DTOs)
- Repository methods don't use `AsNoTracking()` for read-only queries
- Raw SQL or direct DbContext usage outside of repositories

### API Layer (`Neba.Api`)

**Structure**: Each endpoint lives in a use case folder with endpoint, summary, and validator:

```
Neba.Api/Tournaments/CreateTournament/
├── CreateTournamentEndpoint.cs
├── CreateTournamentSummary.cs
└── CreateTournamentValidator.cs
```

Flag when:

- Business logic appears in endpoints (should be in domain or application layer)
- Files in wrong folders (e.g., validator in Contracts project)
- Missing Summary class
- Mixing concerns (multiple use cases in one folder)

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
- ❌ Cross-property validation (belongs in Application layer)
- ❌ Database lookups (belongs in Application layer)
- ❌ Business rules (belongs in Domain/Application layer)

Flag when:

- Validator injects repositories or services
- Validator contains `MustAsync` with database queries
- Validation logic appears in endpoint handler instead of validator
- Missing validator when request has input to validate

#### Error Handling

All errors must return ProblemDetails (RFC 7807).

Flag when:

- Custom error responses instead of ProblemDetails
- Not handling all error cases from `ErrorOr<T>` result
- Using `SendAsync()` with custom error objects
- Missing error case handling (assuming success without checking `result.IsFailure`)

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

| Operation | Method | URL Pattern | Success Code |
|-----------|--------|-------------|--------------|
| List | GET | `/resources` | 200 |
| Get single | GET | `/resources/{id}` | 200 |
| Create | POST | `/resources` | 201 |
| Full update | PUT | `/resources/{id}` | 200 |
| Partial update | PATCH | `/resources/{id}` | 200 |
| Delete | DELETE | `/resources/{id}` | 204 |

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

**Mock verification must use expected values, not `IsAny<T>`:**

```csharp
// Correct - verify with expected values
const long startTimestamp = 1000;
stopwatchProviderMock.Setup(s => s.GetTimestamp()).Returns(startTimestamp);
stopwatchProviderMock.Setup(s => s.GetElapsedTime(startTimestamp)).Returns(TimeSpan.FromMilliseconds(42));

// ... execute code ...

stopwatchProviderMock.Verify(s => s.GetElapsedTime(startTimestamp), Times.Once);

// Correct - use IsAny<T> ONLY when verifying method was NOT called
stopwatchProviderMock.Verify(s => s.GetElapsedTime(It.IsAny<long>()), Times.Never);

// Incorrect - using IsAny<T> when you should verify expected value
stopwatchProviderMock.Verify(s => s.GetElapsedTime(It.IsAny<long>()), Times.Once);
```

**Key mock verification principle**: `IsAny<T>` should only be used to verify that a method was **not called** (with `Times.Never()`). When verifying that a method **was called**, always verify it was called with the expected arguments, not with `IsAny<T>`.

Flag when:

- Tests manually instantiate domain entities instead of using factories
- Tests don't follow the Arrange-Act-Assert pattern
- Integration tests don't use Bogus factories with seeds for reproducibility
- Missing Verify (snapshot) tests for mapping operations
- Tests missing `[UnitTest]` or `[IntegrationTest]` trait attribute
- Tests missing `[Component]` trait attribute
- Facts or Theories missing `DisplayName` parameter
- InlineData missing `TestDisplayName` for theory test cases
- Test method names not following `<MethodName>_Should<ExpectedOutcome>_When<Condition>` pattern
- Mocking `ILogger<T>` instead of using `NullLogger<T>.Instance`
- Using `new Mock<T>()` without `MockBehavior.Strict` parameter
- Using `null!` instead of `#nullable disable`/`#nullable enable` for null testing
- Mock `.Verify()` calls using `IsAny<T>` when verifying method was called with specific arguments

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

| Layer | Required Tests |
|-------|----------------|
| Services | Mock Refit interface, verify ErrorOr mapping, error handling |
| Command handlers | Business rule enforcement, domain event raising, error cases |
| Query handlers | Correct DTO mapping (use Verify snapshots) |
| Complex components | bUnit tests for interaction logic, conditional rendering |
| JS modules | Jest tests for function behavior |

**Logging in tests**: Use `NullLogger<T>.Instance` instead of mocking `ILogger<T>`. Mocking ILogger adds complexity with no benefit — the null logger discards output silently.

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

| Anti-Pattern | Correct Approach |
|--------------|------------------|
| Throwing exceptions for business rule violations | Return `ErrorOr<T>` with typed errors |
| Domain entity in API response | Map to response DTO |
| Service injected into component | Pass data via parameters from page |
| `async void` methods | `async Task` with proper error handling |
| Catching generic `Exception` | Catch specific exceptions or use ErrorOr |
| Magic strings for routes/keys | Constants or strongly-typed alternatives |
| Public setters on entities | Private setters with behavior methods |
| `DateTime.Now` in domain logic | Inject `TimeProvider` |
| Legacy extension method syntax (`this` parameter) | Use `extension()` blocks (C# 14) |
| Custom error response in endpoint | Use `ProblemDetails` via `SendProblemDetailsAsync()` |
| Implicit endpoint authorization | Explicit `AllowAnonymous()`, `Roles()`, or `Policies()` |
| Validation in endpoint handler | Create separate `Validator<TRequest>` class |
| Database lookup in validator | Move to Application layer handler |
| Request properties without Input wrapper | Wrap in `TournamentInput` (for commands) |
| Separate mapper classes for endpoints | Inline mapping in endpoint |
| URL-based API versioning (`/api/v1/...`) | Header-based versioning (`X-Api-Version`) |
| Direct use of Newtonsoft.Json (`JsonConvert`, `JObject`) | System.Text.Json with source generators |
| AutoMapper, Mapster, or similar mapping libraries | Explicit inline mapping |
| Unsealed classes without justification | Seal classes by default |
| Value objects as mutable class | Use `sealed record class` (EF persisted) or `readonly record struct` (transient) |
| Unbounded database queries | Always use `.Take()` with enforced maximum limits |

### Banned Libraries

The following libraries are explicitly prohibited from direct use in application code:

| Library                                            | Reason                                                                 | Alternative                              |
| -------------------------------------------------- | ---------------------------------------------------------------------- | ---------------------------------------- |
| **AutoMapper**, **Mapster**, **ExpressMapper**     | Runtime reflection, hidden mappings, hard to debug, breaks compile-time safety | Explicit mapping methods                 |
| **Newtonsoft.Json** (`JsonConvert`, `JObject`)     | Reflection-based, not AOT-compatible, legacy                           | System.Text.Json with source generators  |
| **BinaryFormatter**                                | Security vulnerabilities, deprecated                                   | System.Text.Json, MessagePack, Protobuf  |

**Note on transitive dependencies**: Some packages (e.g., Hangfire) have transitive dependencies on Newtonsoft.Json. The package may exist in the dependency graph, but **direct usage in our code is prohibited**. Flag any `using Newtonsoft.Json` statements or direct calls to `JsonConvert`.

---

## Review Checklist

When reviewing, verify:

### Architecture & Code Quality

- [ ] Layer boundaries respected (no cross-domain references)
- [ ] Commands return `ErrorOr<T>`
- [ ] Queries return DTOs, not entities
- [ ] Extension methods use `extension()` block syntax, not legacy `this` parameter

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
- [ ] Tests have `[UnitTest]` or `[IntegrationTest]` trait
- [ ] Tests have `[Component]` trait
- [ ] Tests have `DisplayName` on Facts and Theories
- [ ] New code has corresponding tests
- [ ] API endpoint integration tests cover success, validation failure, and auth failure

### Observability

- [ ] Logging present with appropriate levels
- [ ] Spans added for business operations
- [ ] No sensitive data logged

### Blazor

- [ ] Blazor components don't fetch data directly
