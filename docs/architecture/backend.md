# NEBA Website - Architecture Reference

> **For Coding Agents**: This document defines architectural patterns and decisions for the NEBA Website API. Follow these guidelines when generating or modifying code.

---

This document serves as a reference for AI assistants (Claude, Copilot) working on the NEBA Website codebase. It captures architectural decisions, domain modeling patterns, and implementation guidelines established during design discussions.

---

## Agent Collaboration Protocol

**Critical**: This project requires a collaborative, question-driven workflow. The developer has strong software design vocabulary and can articulate requirements clearly. Leverage this.

### Before Writing Code

1. **Propose approaches and get confirmation** before implementing — don't assume
2. **Explain trade-offs** when multiple valid approaches exist
3. **Question your own recommendations** — if you're suggesting something, explain why it's the right choice for this context

### When Reviewing or Modifying Code

1. **Be critical of existing code** — identify violations of these architectural patterns
2. **Be critical of your own generated code** — review it against these guidelines before presenting
3. **Explain changes** — when modifying existing code, explain what's changing and why
4. **Ask clarifying questions** when requirements are ambiguous rather than making assumptions

### Communication Style

- State facts directly without unnecessary explanation
- The developer is proficient in .NET — don't over-explain fundamentals
- When asked, provide recommendations with reasoning
- Use precise terminology

---

## Overview

NEBA Website is a centralized platform for the New England Bowlers Association. It handles tournament operations, enforces NEBA and USBC rules, and manages governance and membership.

### Technology Stack

- **Runtime**: .NET 10
- **Backend**: ASP.NET Core Web API with Fast Endpoints
- **Frontend**: Blazor (single application with public/admin areas)
- **Database**: PostgreSQL (with consideration for document store as appropriate)
- **ORM**: Entity Framework Core with EF Core Identity
- **Local Development**: .NET Aspire
- **Production**: Azure (App Service, Monitor, Key Vault, Blob Storage, Maps)
- **Background Jobs**: Hangfire
- **API Documentation**: Scalar (via Fast Endpoints)

### Architecture Principles

- **Clean Architecture**: Domain at the center, dependencies point inward
- **Domain-Driven Design**: Tactical patterns (aggregates, entities, value objects, domain events)
- **CQRS**: Command/Query separation with distinct read and write models
- **No Modular Monolith**: Single domain/application/infrastructure - complexity not justified for current scale (~1k members, 1-2 tournaments/month, ~10k visits/month)
- **Feature Folders**: Organize by domain area within each layer, treating folders as if they were modules

---

## Project Structure

```
src/
├── Neba.Domain/
│   ├── Bowlers/
│   ├── BowlingCenters/
│   ├── Tournaments/
│   ├── Content/
│   └── SharedKernel/
├── Neba.Application/
│   ├── Bowlers/
│   │   ├── Commands/
│   │   └── Queries/
│   ├── BowlingCenters/
│   ├── Tournaments/
│   └── Common/
│       └── Behaviors/
├── Neba.Infrastructure/
├── Neba.Api/
│   ├── Tournaments/
│   │   ├── CreateTournament/
│   │   │   ├── CreateTournamentEndpoint.cs
│   │   │   └── CreateTournamentValidator.cs
│   │   ├── GetTournament/
│   │   └── ListTournaments/
│   ├── Squads/
│   ├── Bowlers/
│   └── BowlingCenters/
├── Neba.Api.Contracts/
│   ├── Tournaments/
│   │   ├── CreateTournamentInput.cs
│   │   ├── TournamentResponse.cs
│   │   └── TournamentListResponse.cs
│   ├── Squads/
│   ├── Bowlers/
│   └── BowlingCenters/
└── Neba.Website/
```

### Layer Responsibilities

| Layer | Responsibility |
|-------|----------------|
| `Neba.Domain` | Entities, aggregates, value objects, domain events, repository interfaces |
| `Neba.Application` | Commands, queries, handlers, application services, DTOs |
| `Neba.Infrastructure` | EF Core DbContext, repository implementations, external service clients |
| `Neba.Api` | Fast Endpoints, validators, real-time hubs (SSE/WebSocket) |
| `Neba.Api.Contracts` | Input records, response records, and Refit interfaces shared with Blazor |
| `Neba.Website` | Blazor Web App (Interactive Auto mode) |

### Namespace Boundaries

Domain folders should not reference each other directly. Cross-cutting needs go through:

- Shared IDs in `SharedKernel`
- Domain events
- Application layer orchestration

---

## Domain Modeling

### Ubiquitous Language: Bowler vs Member

**Bowler** is the core entity representing a person. **Member** is a status/relationship that a Bowler has with the organization.

Rationale:

- "There were 200 bowlers at the tournament" (not "200 members")
- "How many members we have" (bowlers with current membership)
- A person can bowl without being a member (pays higher fee)
- A person can be a member without bowling (though rare in practice)
- Membership is temporal - expires yearly, can lapse, can be renewed

```csharp
public class Bowler : AggregateRoot
{
    public BowlerId Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public DateOnly DateOfBirth { get; }
    public Address Address { get; }
    
    private readonly List<Membership> _memberships = [];
    public IReadOnlyList<Membership> Memberships => _memberships.AsReadOnly();
    
    public Membership? CurrentMembership => _memberships.FirstOrDefault(m => m.IsCurrent);
    public bool IsMember => CurrentMembership is not null;
    public bool IsGuest => !IsMember && !_memberships.Any(m => m.EndedWithinYears(2));
    
    public int Age => CalculateAge(DateOfBirth);
    public decimal DistanceFrom(BowlingCenter center) => /* calculation */;
}

public class Membership : Entity
{
    public BowlerId BowlerId { get; }
    public MembershipYear Year { get; }
    public MembershipType Type { get; }  // Standard, Military, etc.
    public DateOnly StartDate { get; }
    public DateOnly ExpirationDate { get; }
    public bool IsCurrent => /* date logic */;
}
```

### Entity Identity Strategy

From a DDD perspective, there is no difference between natural keys and surrogate keys - the identity is whatever the domain says it is.

**Surrogate Key (ULID)**:

- Used when no reliable natural key exists
- Example: `BowlerId` wraps a ULID

**Natural Key**:

- Used when a stable, always-present natural key exists
- Example: `BowlingCenterId` wraps a USBC certification number

Both patterns:

- Live in `SharedKernel` as cross-boundary reference types
- Are strongly-typed value objects
- Have factory methods for construction

```csharp
// SharedKernel - surrogate key
[StronglyTypedId("ulid-full)]
public record BowlerId;

// SharedKernel - natural key with synthetic fallback
public record BowlingCenterId
{
    public string Value { get; }
    
    private BowlingCenterId(string value) 
        => Value = value;
    
    public static BowlingCenterId FromCertification(string certNumber) 
        => new(certNumber);
        
    public static BowlingCenterId Synthetic() => new($"HISTORICAL-{Ulid.NewUlid()}");
    
    public bool IsSynthetic => Value.StartsWith("HISTORICAL-");
}
```

For `BowlingCenter`, the certification number IS the identity. The UI can expose it as "Certification Number" via a property while the domain uses `Id`:

```csharp
public class BowlingCenter 
    : AggregateRoot
{
    public BowlingCenterId Id { get; }
    public string Name { get; private set; }
    public Address Address { get; }
    public BowlingCenterStatus Status { get; private set; }
    public BowlingCenterSource Source { get; }
    public DateTime? LastUsbcSync { get; private set; }
}
```

### Aggregate Base Class

Minimal base class for domain event mechanics only. No identity property - each aggregate defines its own identity shape.

```csharp
public interface IAggregateRoot
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

public abstract class AggregateRoot : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];
    
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void AddDomainEvent(IDomainEvent domainEvent) => 
        _domainEvents.Add(domainEvent);
    
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

`AddDomainEvent` is `protected` - only the aggregate itself can raise events, maintaining encapsulation.

### Foreign Key Strategy

**Owned entities** (child entities within an aggregate):

- FK references parent's database PK (int, auto-generated, shadow property)
- Example: `TournamentChampion` → `Tournament`

**Aggregate references** (one aggregate pointing to another):

- FK references the entity ID (BowlerId, BowlingCenterId)
- Example: `Tournament.VenueId` → `BowlingCenter`

```csharp
public class Tournament 
    : AggregateRoot
{
    public TournamentId Id { get; }
    public BowlingCenterId VenueId { get; }  // Aggregate reference
    
    private readonly List<TournamentChampion> _champions = [];
    public IReadOnlyList<TournamentChampion> Champions => _champions.AsReadOnly();
}

public class TournamentChampion  // Owned entity - FK to Tournament's DB PK
{
    public BowlerId BowlerId { get; }
    public ChampionshipType Type { get; }
}
```

### Domain Events

Still valuable without modules - they decouple concerns within the monolith:

- Side effects that shouldn't be in the aggregate (send email, update read model)
- Eventual consistency for non-critical operations
- Handlers are in-process, same transaction boundary available

Dispatch via `SaveChangesAsync` interceptor or pipeline behavior.

```csharp
public class Tournament 
    : AggregateRoot
{
    public void Complete(IReadOnlyList<BowlerId> championIds)
    {
        // Domain logic, validation...
        Status = TournamentStatus.Completed;
        
        AddDomainEvent(new TournamentCompleted(Id, championIds));
    }
}
```

---

## CQRS Implementation

### Commands

- Use domain entities
- Go through repositories
- Return `ErrorOr<T>` for Result pattern

### Queries

- Return DTOs directly from query repositories
- Projections happen in the repository (`.Select()`)
- No need to load full aggregates

### Repository Pattern

Separate repositories for commands and queries:

**Command/Entity Repositories**:

- Return fully hydrated aggregates
- Used by command handlers

**Query Repositories**:

- Return DTOs/projections
- Named methods communicate intent
- Mapping happens in the repository

```csharp
// Command repository
public interface ITournamentRepository
{
    Task<Tournament?> GetById(TournamentId id);
    Task Add(Tournament tournament);
    Task Update(Tournament tournament);
}

// Query repository - named methods for each use case
public interface ITournamentQueries
{
    Task<IEnumerable<TournamentListDto>> GetUpcoming();
    Task<TournamentDetailDto?> GetPublicDetail(TournamentId id);
    Task<IEnumerable<TournamentResultDto>> GetCompletedByYear(int year);
}
```

Query repository implementation with mapping inline:

```csharp
public async Task<TournamentDetailDto?> GetPublicDetail(TournamentId id)
{
    return await _context.Tournaments
        .Where(t => t.Id == id)
        .Select(t => new TournamentDetailDto
        {
            Id = t.Id.Value,
            Name = t.Name,
            Date = t.Date,
            VenueName = t.Venue.Name
        })
        .FirstOrDefaultAsync();
}
```

### DTOs and Contracts

- **DTOs**: Returned by query handlers, shaped for the use case
- **Inputs/Responses** (`Neba.Api.Contracts`): API contract shared with Blazor

Mapping:

- Query repository → DTO (inline in repository)
- DTO → Response (inline in endpoint, unless complex)

Start with fewer mapping layers. Add abstraction when there's a reason.

### API Clients (Refit)

Refit interfaces live in `Neba.Api.Contracts` alongside the Inputs/Responses they reference. This keeps the full API contract in one place.

```csharp
// Neba.Api.Contracts/Tournaments/ITournamentApi.cs
public interface ITournamentApi
{
    [Get("/tournaments")]
    Task<IReadOnlyList<TournamentListResponse>> GetAll();
    
    [Get("/tournaments/{id}")]
    Task<TournamentResponse> GetById(string id);
    
    [Post("/tournaments")]
    Task<TournamentResponse> Create([Body] CreateTournamentInput input);
}
```

**Registration** (in Blazor client):

```csharp
builder.Services
    .AddRefitClient<ITournamentApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https+http://api"));
```

The `https+http://api` URI uses Aspire's service discovery to resolve the API endpoint.

---

## API Design

### Fast Endpoints Structure

Each endpoint in its own folder with endpoint class and validator:

```
Neba.Api/
├── Tournaments/
│   ├── CreateTournament/
│   │   ├── CreateTournamentEndpoint.cs
│   │   └── CreateTournamentValidator.cs
```

### Validation Strategy

**FluentValidation** for input validation (via Fast Endpoints):

- Structural validation: required fields, formats, lengths
- Minimal business rules

**Domain entities** for business rule validation:

- Complex rules, cross-field validation
- Return `ErrorOr<T>` for failures

### Error Handling

Errors flow as `ErrorOr<T>` results from domain and application layers. The API translates these to ProblemDetails (RFC 9457):

- Domain/application errors → appropriate HTTP status code + ProblemDetails body
- Validation errors → 400 Bad Request with `errors` object containing field-level details
- Unexpected exceptions → 500 Internal Server Error (details hidden in production)

### Authentication & Authorization

- EF Core Identity
- Role-based authorization (Admin, Scorer, etc.)
- Day 1: Admin-only authentication
- Future: Public user registration (admins can still access admin areas)

Endpoints return what they return - not different data based on auth. Access is controlled at the endpoint level (e.g., `GET /tournaments` is public, `POST /tournaments` requires admin).

### Real-time Endpoints

Squad-level boundaries for live scoring:

- **SSE**: Public score viewing (`/squads/{id}/live-scores`)
- **WebSocket**: Score entry by operators (`/squads/{id}/scores`)

Squads run one at a time within a tournament (no overlap).

---

## External Integrations

### USBC API (Bowling Centers)

Monthly background job (Hangfire):

1. Query USBC API for New England centers
2. New certs → Create `BowlingCenter` with `Source = UsbcApi`
3. Existing certs → Update name only (not address - USBC data can be incorrect)
4. Missing from response → Mark as `Closed`

### Third-Party Registration

External vendor is source of truth (from their point of view) for registrations. They call our api endpoint when registration completes.

Domain language:

- **Reservation**: Intent to bowl, received from third party (may be prepaid or pay on-site)
- **Check-in**: Confirmation at the tournament site

### Other Integrations

- Google Docs (bylaws display)
- Azure Blob Storage (tournament documents, photos)
- Challonge (bracket management for finals)
- Azure Maps (distance calculations for priority registration)

---

## Stats Implementation

### The Challenge

| Period     | Data Available                                 |
|:----------:|:-----------------------------------------------|
| 2004-2018  | Summary only (aggregated stats per bowler)     |
| 2019+      | Granular (individual scores, can recalculate)  |

### Solution: Stats as a Read Model

`BowlerSeasonStats` entity with a source discriminator:

```csharp
public class BowlerSeasonStats 
    : Entity
{
    public BowlerId BowlerId { get; }
    public int Season { get; }
    public StatsSource Source { get; }  // Legacy | Calculated
    public decimal Average { get; }
    public int HighBlock { get; }
    public int TournamentCount { get; }
    public int TotalPins { get; }
    public int TotalGames { get; }
}

public enum StatsSource
{
    Legacy,     // Imported summary data, read-only
    Calculated  // Derived from granular tournament data
}
```

### Recalculation Flow

On tournament completion, recalculate stats for participants only:

```csharp
public class TournamentCompletedHandler 
    : IDomainEventHandler<TournamentCompleted>
{
    public async Task Handle(TournamentCompleted event)
    {
        var tournament = await _tournamentRepository.GetById(event.TournamentId);
        var participantIds = tournament.GetParticipantBowlerIds();
        
        foreach (var bowlerId in participantIds)
        {
            await _statsService.RecalculateForSeason(bowlerId, tournament.Season);
        }
    }
}

public class StatsService
{
    public async Task RecalculateForSeason(BowlerId bowlerId, int season)
    {
        var existingStats = await _statsRepository.GetByBowlerAndSeason(bowlerId, season);
        
        // Never touch legacy data
        if (existingStats?.Source == StatsSource.Legacy)
        {
            return;
        }
        
        var scores = await _scoreRepository.GetByBowlerAndSeason(bowlerId, season);
        
        var stats = new BowlerSeasonStats(
            bowlerId,
            season,
            StatsSource.Calculated,
            average: scores.Sum(s => s.TotalPins) / (decimal)scores.Sum(s => s.Games),
            highBlock: scores.Max(s => s.BlockTotal),
            tournamentCount: scores.Select(s => s.TournamentId).Distinct().Count(),
            totalPins: scores.Sum(s => s.TotalPins),
            totalGames: scores.Sum(s => s.Games)
        );
        
        await _statsRepository.Upsert(stats);
    }
}
```

Career stats combine both sources transparently.

---

## Testing Strategy

### Test Project Structure

```
tests/
├── Neba.TestFactory/           # Shared test infrastructure (factories, fixtures, traits)
├── Neba.Api.Tests/             # API endpoint tests (unit + integration)
├── Neba.Application.Tests/     # Handler tests (unit + integration)
├── Neba.Domain.Tests/          # Domain logic tests (unit)
├── Neba.Infrastructure.Tests/  # Repository tests (unit + integration)
├── Neba.Website.Tests/         # UI tests (bUnit, services, JS interop)
├── e2e/                        # Playwright E2E tests (TypeScript)
└── js/                         # Jest tests for JS modules
```

All test projects reference `Neba.TestFactory` for shared factories, fixtures, and trait attributes.

### Test Traits

Tests are categorized using custom xUnit v3 trait attributes defined in `Neba.TestFactory`:

```csharp
// Category traits - Unit vs Integration
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class UnitTestAttribute : Attribute, ITraitAttribute
{
    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
        => [new("Category", "Unit")];
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class IntegrationTestAttribute : Attribute, ITraitAttribute
{
    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
        => [new("Category", "Integration")];
}

// Component trait - feature/functionality being tested
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class ComponentAttribute(string component) : Attribute, ITraitAttribute
{
    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
        => [new("Component", component)];
}
```

**Usage:**

```csharp
[UnitTest]
[Component("Tournaments.Registration")]
public class RegisterBowlerCommandTests
{
    [Fact]
    public void Should_Fail_When_Squad_At_Capacity() { }
}

[IntegrationTest]
[Component("Tournaments")]
public class TournamentRepositoryTests : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task GetById_Returns_Tournament_With_Squads() { }
}
```

**Component naming**: Use feature folder names (e.g., `Tournaments`, `Bowlers`). Add sub-component when testing specific functionality (e.g., `Tournaments.Registration`, `Tournaments.Scoring`).

### Filtering Tests

Filter by trait using `dotnet test` or `dotnet run`:

```bash
# Using dotnet test
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
dotnet test --filter "Component=Tournaments"
dotnet test --filter "Category=Unit&Component=Tournaments.Registration"

# Using dotnet run (note the -- to pass args to test runner)
dotnet run --project tests/Neba.Domain.Tests -- --filter "Category=Unit"

# Running the executable directly
./Neba.Domain.Tests --filter "Category=Unit"

# xUnit v3 query filter language
dotnet run -- --filter-query "[Category=Unit]"
```

**CI usage**: Run unit and integration tests in separate jobs for faster feedback:

```yaml
- name: Run Unit Tests
  run: dotnet test --filter "Category=Unit"

- name: Run Integration Tests
  run: dotnet test --filter "Category=Integration"
```

### Test Naming & Display Names

All tests must have explicit display names for clarity in test runners and CI output.

**Facts**: Use `DisplayName` parameter:

```csharp
[Fact(DisplayName = "Should fail when squad is at capacity")]
public void Should_Fail_When_Squad_At_Capacity() { }
```

**Theories**: Use `DisplayName` on the theory and descriptive `[InlineData]`:

```csharp
[Theory(DisplayName = "Should validate tournament type eligibility")]
[InlineData(TournamentType.Senior, 49, false, DisplayName = "Senior tournament rejects bowler under 50")]
[InlineData(TournamentType.Senior, 50, true, DisplayName = "Senior tournament accepts bowler at 50")]
[InlineData(TournamentType.Open, 25, true, DisplayName = "Open tournament accepts any age")]
public void Should_Validate_Eligibility(TournamentType type, int age, bool expected) { }
```

**MemberData/ClassData**: Include display name in the test data or use descriptive method names:

```csharp
[Theory(DisplayName = "Should calculate handicap correctly")]
[MemberData(nameof(HandicapTestCases))]
public void Should_Calculate_Handicap(int average, int basis, decimal factor, int expected) { }

public static TheoryData<int, int, decimal, int> HandicapTestCases => new()
{
    { 180, 220, 0.8m, 32 },  // (220 - 180) * 0.8 = 32
    { 200, 220, 0.8m, 16 },  // (220 - 200) * 0.8 = 16
    { 220, 220, 0.8m, 0 },   // No handicap at basis
};
```

**Why display names matter**:

- Test output is readable without parsing method names
- CI logs clearly show what failed
- Non-technical stakeholders can understand test coverage

### Unit Tests

- xUnit v3, Moq, Shouldly
- Domain logic and business rules
- Command/query handlers
- Use `Factory.Create()` for test data
- Mark with `[UnitTest]` trait

### Test Data Factories

Factories live in `Neba.TestFactory` and provide two approaches:

- **`Create`**: For unit tests. Pass only the properties relevant to the test - everything else gets valid defaults. This makes tests self-documenting.
- **`Bogus`**: For integration tests. Generates realistic random data using the Bogus library.

```csharp
public static class TournamentFactory
{
    // Valid defaults
    public const string ValidName = "Spring Classic";
    public static readonly DateOnly ValidDate = new(2025, 6, 15);
    public static readonly TournamentType ValidType = TournamentType.Open;
    
    public static Tournament Create(
        TournamentId? id = null,
        string? name = null,
        DateOnly? date = null,
        TournamentType? type = null,
        BowlingCenter? venue = null,
        IReadOnlyList<Squad>? squads = null)
    {
        return new Tournament(
            id ?? TournamentId.New(),
            name ?? ValidName,
            date ?? ValidDate,
            type ?? ValidType,
            venue ?? BowlingCenterFactory.Create(),
            squads ?? SquadFactory.Bogus(3)
        );
    }
    
    public static IReadOnlyList<Tournament> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<Tournament>()
            .CustomInstantiator(f => new Tournament(
                TournamentId.New(),
                f.Commerce.ProductName(),
                f.Date.FutureDateOnly(),
                f.PickRandom<TournamentType>(),
                BowlingCenterFactory.Bogus(seed: f.Random.Int()).First(),
                SquadFactory.Bogus(f.Random.Int(1, 5), seed: f.Random.Int())
            ));
        
        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }
        
        return faker.Generate(count);
    }
    
    public static Tournament Bogus(int? seed = null) => Bogus(1, seed).First();
}
```

**Factory Guidelines**:

- Every property on `Create` is nullable with a default of `null`
- Unspecified properties get valid defaults (constants defined in the factory)
- Related entities use their corresponding factory's `Create` method
- Collections default to 3 items
- Use `CustomInstantiator` to avoid requiring public default constructors

**Unit Test Usage** - Only pass what matters for the test:

```csharp
// Testing senior tournament eligibility - only type matters
var tournament = TournamentFactory.Create(type: TournamentType.Senior);

// Testing tournament naming rules - only name matters
var tournament = TournamentFactory.Create(name: "");

// Testing venue assignment - only venue matters
var tournament = TournamentFactory.Create(
    venue: BowlingCenterFactory.Create(status: BowlingCenterStatus.Closed)
);
```

**Integration Test Usage** - Generate realistic data:

```csharp
// Random single item
var tournament = TournamentFactory.Bogus();

// Multiple random items
var tournaments = TournamentFactory.Bogus(count: 10);

// Reproducible random data for consistent test runs
var tournaments = TournamentFactory.Bogus(count: 10, seed: 12345);
```

### Mapping Tests with Verify

Use Verify (snapshot testing) to catch unmapped properties in query repositories and endpoint responses:

```csharp
[IntegrationTest]
[Component("Tournaments")]
public class TournamentQueryTests : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task GetPublicDetail_MapsAllFields()
    {
        var result = await _repository.GetPublicDetail(knownTournamentId);

        await Verify(result);  // Fails if shape changes
    }
}
```

Workflow:

1. First run creates `.verified.txt` file alongside the test
2. New/removed property → test fails, creates `.received.txt`
3. Review diff and accept (`dotnet verify accept`) or fix mapping
4. Commit updated `.verified.txt`

### Integration Tests

- WebApplicationFactory for API testing
- Test containers for PostgreSQL
- Respawn for database reset between tests
- Authentication/authorization flow testing
- Mark with `[IntegrationTest]` trait

**Integration Test Pattern**:

```csharp
[IntegrationTest]
[Component("Tournaments")]
public class TournamentEndpointTests : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client;

    public TournamentEndpointTests(ApiFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task GetTournament_ReturnsTournament_WhenExists()
    {
        // Arrange - seed with Bogus data
        var tournament = TournamentFactory.Bogus(seed: 12345);
        await _fixture.SeedAsync(tournament);

        // Act
        var response = await _client.GetAsync($"/tournaments/{tournament.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TournamentResponse>();
        await Verify(result);
    }
}
```

**Database Reset**: Use Respawn to reset database state between tests rather than recreating the container.

---

## Migration Strategy

### Phased Approach

1. **Website migrates first** as the priority
2. **WinForms application continues running** in parallel
3. **WinForms pushes data to new schema** during transition
4. **Entities start minimal**, enriched as app features migrate

### Data Flow

Data flows one direction during migration - from WinForms into the new system. The new schema becomes the source of truth as soon as the website goes live.

### Entity Evolution

Example: `Tournament` starts with basic public-facing fields (date, type, venue, results). Later gains full operational attributes (round formats, advancement rules, squad configurations) when that functionality moves from WinForms.

### Historical Data

- Tournaments, bowling centers, bowler stats ported from both existing website database and WinForms database
- Manual reconciliation for items like bowling center matching where data quality varies
- Bowling centers with unknown certification numbers get synthetic IDs (`HISTORICAL-{ulid}`)

---

## Observability

### Principles

Be intentional about observability from the start - don't bolt it on later. Every feature implementation should consider:

- **What needs to be traced?** - Operations that cross boundaries (HTTP calls, database queries, external APIs)
- **What metrics matter?** - Business metrics (tournaments created, scores recorded) and technical metrics (response times, error rates)
- **What should be logged?** - Decisions, failures, and context - not noise

### Implementation

**Structured Logging**:

- `ILogger<T>` everywhere
- Log at appropriate levels (Debug for development context, Information for business events, Warning for recoverable issues, Error for failures)
- Include correlation IDs and relevant entity IDs in log scopes
- Never log sensitive data (PII, credentials)

**Distributed Tracing**:

- OpenTelemetry for trace propagation
- Custom spans for significant business operations (e.g., tournament completion, stats recalculation)
- Tag spans with relevant context (tournament ID, bowler ID, operation type)

**Metrics**:

- Track business KPIs (registrations per tournament, average response times)
- Track technical health (error rates, queue depths, background job durations)
- Use counters, histograms, and gauges appropriately

### Infrastructure

- **Local Development**: .NET Aspire Dashboard (traces, logs, metrics in one place)
- **Production**: Azure Monitor (Application Insights for traces/logs, Azure Monitor Metrics)

### Design Considerations

When implementing a feature, ask:

1. How will we know if this is working correctly in production?
2. How will we debug issues without access to the user's session?
3. What business questions might stakeholders ask that telemetry could answer?
4. What's the first thing we'd look for if this feature causes an incident?

---

## Coding Standards

Coding standards are defined in `.editorconfig` at the repository root. All code must conform to these rules.

Agents should:

- Follow the `.editorconfig` settings
- Run code cleanup / formatting before presenting code
- Not override or ignore analyzer warnings without explicit approval

---

## Backlog

- **Architecture Tests**: NetArchTest to enforce layer/folder boundaries (deferred - rely on discipline for now)

---

## Libraries & Packages

| Purpose | Library |
| --------- | --------- |
| Result Pattern | ErrorOr |
| Validation | FluentValidation (via Fast Endpoints) |
| API Framework | Fast Endpoints |
| API Documentation | Scalar |
| Background Jobs | Hangfire |
| Snapshot Testing | Verify |
| Unit Testing | xUnit, Moq, Shouldly |
| Test Data Generation | Bogus |
| Database Reset (Tests) | Respawn |
| Test Containers | Testcontainers |
| HTTP Client | Refit |
