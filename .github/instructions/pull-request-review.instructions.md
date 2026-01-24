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

### Infrastructure Layer (`Neba.Infrastructure`)

Flag when:

- Repository implementations return domain entities for query operations (queries should return DTOs)
- Repository methods don't use `AsNoTracking()` for read-only queries
- Raw SQL or direct DbContext usage outside of repositories

### API Layer (`Neba.Api`)

Flag when:

- Business logic appears in endpoints (should be in domain or application layer)
- Endpoints don't use validators for input validation
- Missing authorization attributes on admin endpoints
- Inconsistent with REST conventions (see below)

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

Flag when:

- Tests manually instantiate domain entities instead of using factories
- Tests don't follow the Arrange-Act-Assert pattern
- Integration tests don't use Bogus factories with seeds for reproducibility
- Missing Verify (snapshot) tests for mapping operations

### What to Test

| Layer | Required Tests |
|-------|----------------|
| Services | Mock Refit interface, verify ErrorOr mapping, error handling, logging |
| Command handlers | Business rule enforcement, domain event raising, error cases |
| Query handlers | Correct DTO mapping (use Verify snapshots) |
| Complex components | bUnit tests for interaction logic, conditional rendering |
| JS modules | Jest tests for function behavior |

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

Flag when:

- Input/response records are mutable (should be `record` types or have `init` setters)
- Missing XML documentation on public contract types
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

---

## Review Checklist

When reviewing, verify:

- [ ] Layer boundaries respected (no cross-domain references)
- [ ] Commands return `ErrorOr<T>`
- [ ] Queries return DTOs, not entities
- [ ] REST conventions followed
- [ ] Response envelopes consistent
- [ ] Tests use factories, not manual instantiation
- [ ] New code has corresponding tests
- [ ] Logging present with appropriate levels
- [ ] Spans added for business operations
- [ ] No sensitive data logged
- [ ] Blazor components don't fetch data directly
- [ ] Authorization attributes on admin endpoints
- [ ] Extension methods use `extension()` block syntax, not legacy `this` parameter
