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
│   │   ├── CreateTournament/
│   │   │   ├── CreateTournamentRequest.cs
│   │   │   └── TournamentInput.cs
│   │   ├── GetTournament/
│   │   │   └── TournamentResponse.cs
│   │   ├── ListTournaments/
│   │   │   ├── ListTournamentsRequest.cs
│   │   │   └── TournamentSummaryResponse.cs
│   │   └── ITournamentsApi.cs
│   ├── Squads/
│   ├── Bowlers/
│   ├── BowlingCenters/
│   └── Common/
│       ├── CollectionResponse.cs
│       └── PaginationResponse.cs
└── Neba.Website/
```

### Layer Responsibilities

| Layer | Responsibility |
|-------|----------------|
| `Neba.Domain` | Entities, aggregates, value objects, domain events, repository interfaces |
| `Neba.Application` | Commands, queries, handlers, application services, DTOs |
| `Neba.Infrastructure` | EF Core DbContext, repository implementations, external service clients |
| `Neba.Api` | Fast Endpoints, validators, real-time hubs (SSE/WebSocket) |
| `Neba.Api.Contracts` | Request/Input/Response records and Refit interfaces shared with Blazor |
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

Each endpoint in its own use case folder with endpoint, summary, and validator:

```
Neba.Api/
├── Tournaments/
│   ├── TournamentEndpointGroup.cs
│   ├── CreateTournament/
│   │   ├── CreateTournamentEndpoint.cs
│   │   ├── CreateTournamentSummary.cs
│   │   └── CreateTournamentValidator.cs
│   ├── UpdateTournament/
│   ├── GetTournament/
│   ├── ListTournaments/
│   └── DeleteTournament/
├── Squads/
├── Bowlers/
└── BowlingCenters/
```

### Contracts Layer

Contracts follow a Request/Input/Response pattern organized by use case:

**Requests wrap Inputs** (for commands):

```csharp
namespace Neba.Api.Contracts.Tournaments.CreateTournament;

/// <summary>
/// Tournament details input
/// </summary>
public record TournamentInput
{
    /// <summary>
    /// The name of the tournament
    /// </summary>
    /// <example>Spring Classic 2026</example>
    public string TournamentName { get; init; } = string.Empty;

    /// <summary>
    /// Tournament start date
    /// </summary>
    public DateTime StartDate { get; init; }

    /// <summary>
    /// Tournament location
    /// </summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>
    /// Maximum number of participants
    /// </summary>
    public int MaxParticipants { get; init; }
}

/// <summary>
/// Request to create a new tournament
/// </summary>
public record CreateTournamentRequest
{
    /// <summary>
    /// Tournament details to create
    /// </summary>
    public TournamentInput Tournament { get; init; } = new();
}
```

**Responses**:

```csharp
namespace Neba.Api.Contracts.Tournaments.GetTournament;

/// <summary>
/// Full tournament details response
/// </summary>
public record TournamentResponse
{
    public Guid TournamentId { get; init; }
    public string TournamentName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public string Location { get; init; } = string.Empty;
    public int MaxParticipants { get; init; }
    public int CurrentParticipants { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
```

**Common Response Wrappers** (in `Neba.Api.Contracts.Common`):

```csharp
public record CollectionResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
}

public record PaginationResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

**Refit Interfaces**:

```csharp
namespace Neba.Api.Contracts.Tournaments;

public interface ITournamentsApi
{
    [Post("/api/tournaments")]
    Task<TournamentResponse> CreateTournamentAsync(
        [Body] CreateTournamentRequest request,
        CancellationToken cancellationToken = default);

    [Get("/api/tournaments/{id}")]
    Task<TournamentResponse> GetTournamentAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    [Get("/api/tournaments")]
    Task<PaginationResponse<TournamentSummaryResponse>> ListTournamentsAsync(
        [Query] ListTournamentsRequest request,
        CancellationToken cancellationToken = default);

    [Delete("/api/tournaments/{id}")]
    Task DeleteTournamentAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
```

### Endpoint Implementation

```csharp
namespace Neba.Api.Tournaments.CreateTournament;

public class CreateTournamentEndpoint
    : Endpoint<CreateTournamentRequest, TournamentResponse>
{
    private readonly ICommandHandler<CreateTournamentCommand, TournamentDto> _handler;

    public CreateTournamentEndpoint(ICommandHandler<CreateTournamentCommand, TournamentDto> handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/tournaments");
        Group<TournamentEndpointGroup>();
        Version(1);
        Roles("TournamentManager", "Admin");
        Tags("Tournaments", "Authenticated");

        Description(b => b
            .WithName("CreateTournament")
            .Produces<TournamentResponse>(201, "application/json")
            .ProducesProblemDetails(400, "application/problem+json")
            .ProducesProblemDetails(409, "application/problem+json"));
    }

    public override async Task HandleAsync(
        CreateTournamentRequest req,
        CancellationToken ct)
    {
        // Map Request -> Command
        var command = new CreateTournamentCommand(
            req.Tournament.TournamentName,
            req.Tournament.StartDate,
            req.Tournament.Location,
            req.Tournament.MaxParticipants);

        var result = await _handler.HandleAsync(command, ct);

        // Handle errors
        if (result.IsFailure)
        {
            await this.SendProblemDetailsAsync(result.Error, ct);
            return;
        }

        // Map DTO -> Response
        var response = new TournamentResponse
        {
            TournamentId = result.Value.TournamentId,
            TournamentName = result.Value.TournamentName,
            // ... map remaining properties
        };

        await SendCreatedAtAsync<GetTournamentEndpoint>(
            new { id = response.TournamentId },
            response,
            cancellation: ct);
    }
}
```

### Endpoint Groups

```csharp
namespace Neba.Api.Tournaments;

public class TournamentEndpointGroup : Group
{
    public TournamentEndpointGroup()
    {
        Configure("api/tournaments", ep =>
        {
            ep.Description(x => x
                .ProducesProblemDetails(401, "application/problem+json")
                .ProducesProblemDetails(403, "application/problem+json")
                .ProducesProblemDetails(429, "application/problem+json"));
        });
    }
}
```

### Summary Classes

Summary classes provide OpenAPI documentation and live alongside the endpoint:

```csharp
namespace Neba.Api.Tournaments.CreateTournament;

public class CreateTournamentSummary : Summary<CreateTournamentEndpoint>
{
    public CreateTournamentSummary()
    {
        Summary = "Create a new bowling tournament";

        Description = """
            Creates a new tournament in the NEBA system.

            The tournament will be validated against business rules:
            - Tournament name must be unique
            - Start date must be in the future
            - Max participants must be between 8 and 256
            """;

        ExampleRequest = new CreateTournamentRequest
        {
            Tournament = new TournamentInput
            {
                TournamentName = "Spring Classic 2026",
                StartDate = new DateTime(2026, 4, 15),
                Location = "Boston, MA",
                MaxParticipants = 128
            }
        };

        Response<TournamentResponse>(201, "Tournament created successfully", example: new()
        {
            TournamentId = Guid.Parse("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"),
            TournamentName = "Spring Classic 2026",
            // ... example values
        });

        Response<ProblemDetails>(400, "Validation errors occurred");
        Response<ProblemDetails>(409, "Business rule conflict");
    }
}
```

### Validation Strategy

**FluentValidation** (via Fast Endpoints) for structural validation only:

```csharp
public class CreateTournamentValidator : Validator<CreateTournamentRequest>
{
    public CreateTournamentValidator()
    {
        RuleFor(x => x.Tournament.TournamentName)
            .NotEmpty()
            .WithMessage("Tournament name is required")
            .MaximumLength(200);

        RuleFor(x => x.Tournament.StartDate)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Start date must be in the future");

        RuleFor(x => x.Tournament.MaxParticipants)
            .InclusiveBetween(8, 256);
    }
}
```

**Validation scope**:

- ✅ Required fields, length constraints, range validation, format validation
- ❌ Cross-property validation (Application layer)
- ❌ Database lookups (Application layer)
- ❌ Business rules (Domain/Application layer)

**Domain entities** handle business rule validation and return `ErrorOr<T>` for failures.

### Query Parameter Handling

```csharp
public record ListTournamentsRequest
{
    [QueryParam, BindFrom("location")]
    public string? Location { get; init; }

    [QueryParam, BindFrom("startDateFrom")]
    public DateTime? StartDateFrom { get; init; }

    [QueryParam, BindFrom("page")]
    public int Page { get; init; } = 1;

    [QueryParam, BindFrom("pageSize")]
    public int PageSize { get; init; } = 20;
}
```

Rules:

- Use flat query strings (not nested objects)
- Use camelCase for parameter names
- Use `[QueryParam]` and `[BindFrom]` attributes

### Error Handling

Errors flow as `ErrorOr<T>` from domain/application layers. The API translates to ProblemDetails (RFC 9457):

```csharp
namespace Neba.Api.Extensions;

public static class ProblemDetailsExtensions
{
    public static async Task SendProblemDetailsAsync(
        this IEndpoint endpoint,
        Error error,
        CancellationToken ct)
    {
        var httpContext = endpoint.HttpContext;

        var problemDetails = new ProblemDetails
        {
            Type = "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.1",
            Title = error.Type switch
            {
                ErrorType.Validation => "Validation Error",
                ErrorType.Conflict => "Conflict",
                ErrorType.NotFound => "Not Found",
                _ => "An error occurred"
            },
            Status = error.Type switch
            {
                ErrorType.Validation => 400,
                ErrorType.Conflict => 409,
                ErrorType.NotFound => 404,
                _ => 500
            },
            Detail = error.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions.Add("traceId", httpContext.TraceIdentifier);

        if (!string.IsNullOrEmpty(error.Code))
            problemDetails.Extensions.Add("errorCode", error.Code);

        if (error.Context?.Any() == true)
            problemDetails.Extensions.Add("context", error.Context);

        await endpoint.SendAsync(problemDetails, problemDetails.Status!.Value, ct);
    }
}
```

**Error response rules**:

- Always return ProblemDetails for any error (400, 401, 403, 404, 409, 500, etc.)
- Use Fast Endpoints methods when appropriate (`SendNotFoundAsync()`, `SendUnauthorizedAsync()`, `SendForbiddenAsync()`)
- Use `SendProblemDetailsAsync()` extension for Application layer errors
- Validation errors always return 400
- Include `traceId` in all error responses
- Include `errorCode` for domain/business errors
- Include `context` for additional error details when helpful

### Authentication & Authorization

All endpoints must explicitly configure authorization:

```csharp
public override void Configure()
{
    Post("/api/tournaments");

    // Explicitly configure auth - one of:
    AllowAnonymous();
    Roles("TournamentManager", "Admin");
    Policies("CanManageTournaments");

    // Never leave auth unspecified
}
```

**Tags for visibility**:

```csharp
// Public endpoint
Tags("Tournaments", "Public");
AllowAnonymous();

// Authenticated endpoint
Tags("Tournaments", "Authenticated");
Roles("TournamentManager");

// Admin endpoint
Tags("Tournaments", "Admin");
Roles("Admin");
```

- EF Core Identity for user management
- Role-based authorization (Admin, Scorer, etc.)
- Day 1: Admin-only authentication
- Future: Public user registration

Endpoints return what they return - not different data based on auth. Access is controlled at the endpoint level.

### Rate Limiting

Configuration via appsettings.json:

```json
{
  "RateLimiting": {
    "General": {
      "Anonymous": { "PermitLimit": 100, "Window": "00:01:00" },
      "Authenticated": { "PermitLimit": 1000, "Window": "00:01:00" }
    },
    "PerEndpoint": {
      "CreateTournament": { "PermitLimit": 10, "Window": "00:01:00" }
    }
  }
}
```

Rules:

- Rate limit by IP for anonymous requests
- Rate limit by authenticated user for authenticated requests
- Per-endpoint limits use the endpoint's `WithName()` value
- ProblemDetails returned for 429

### Versioning

Header-based versioning via `X-Api-Version`:

```csharp
// V1 (default - no header required)
public class CreateTournamentEndpoint : Endpoint<CreateTournamentRequest, TournamentResponse>
{
    public override void Configure()
    {
        Post("/api/tournaments");
        Version(1);
    }
}

// V2 (requires X-Api-Version: 2 header)
public class CreateTournamentEndpointV2 : Endpoint<CreateTournamentRequestV2, TournamentResponse>
{
    public override void Configure()
    {
        Post("/api/tournaments");
        Version(2);
    }
}
```

Versioning rules:

- V1 is default (no header required)
- All other versions require explicit header
- When V1 is sunset, rename `CreateTournamentEndpointV2` → `CreateTournamentEndpoint` but keep `Version(2)`

**Deprecation**:

```csharp
public override void Configure()
{
    Post("/api/tournaments");
    Version(1);
    Tags("Tournaments", "Authenticated", "Deprecated");
    Deprecate(removedOn: new DateTime(2027, 1, 1));
}
```

### Response Caching

Response caching is handled via **HybridCache in the Application layer**, not at the API endpoint level. Do not use Fast Endpoints' `ResponseCache()` method.

### Idempotency

Not currently implemented for POST commands. When implementing in the future, Fast Endpoints supports idempotency via headers if needed.

### Real-time Endpoints

Squad-level boundaries for live scoring:

- **SSE**: Public score viewing (`/squads/{id}/live-scores`)
- **WebSocket**: Score entry by operators (`/squads/{id}/scores`)

Squads run one at a time within a tournament (no overlap).

### XML Documentation

Enable in .csproj:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

All public contracts (requests, responses, inputs) must have XML comments with `<summary>` and `<example>` tags.

### Endpoint Checklist

When creating a new endpoint:

- [ ] Use case folder created with all files (Endpoint, Summary, Validator if needed)
- [ ] Contracts created in Api.Contracts (Request/Input, Response)
- [ ] Request wraps Input (for commands)
- [ ] All contracts have XML documentation comments
- [ ] Endpoint inherits from `Endpoint<TRequest, TResponse>`
- [ ] `Configure()` method includes:
  - [ ] HTTP verb and route (RESTful)
  - [ ] Group configured
  - [ ] Version specified (defaults to 1)
  - [ ] Authorization explicit (AllowAnonymous, Roles, or Policies)
  - [ ] Tags with domain and visibility
  - [ ] Description with `WithName()` (required)
  - [ ] Produces/ProducesProblemDetails for all status codes
- [ ] `HandleAsync()` maps Request → Command/Query → Response
- [ ] Errors use `SendProblemDetailsAsync()` extension or appropriate Fast Endpoints methods
- [ ] Validator only validates structural rules (no business logic)
- [ ] Summary class with examples for request and all responses
- [ ] Refit interface updated
- [ ] Integration tests written

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

All tests must have explicit display names for clarity in test runners and CI output. Method names follow the pattern: `<MethodName>_Should<ExpectedOutcome>_When<Condition>`

**Facts**: Use `DisplayName` parameter:

```csharp
[Fact(DisplayName = "Should fail when squad is at capacity")]
public void RegisterBowler_ShouldFail_WhenSquadAtCapacity() { }
```

**Theories**: Use `DisplayName` on the theory and `TestDisplayName` on each `[InlineData]`:

```csharp
[Theory(DisplayName = "Should validate tournament type eligibility")]
[InlineData(TournamentType.Senior, 49, false, TestDisplayName = "Senior tournament rejects bowler under 50")]
[InlineData(TournamentType.Senior, 50, true, TestDisplayName = "Senior tournament accepts bowler at 50")]
[InlineData(TournamentType.Open, 25, true, TestDisplayName = "Open tournament accepts any age")]
public void ValidateEligibility_ShouldVerifyAge_WhenTournamentTypeRequiresAge(TournamentType type, int age, bool expected) { }
```

**MemberData/ClassData**: Include display name in the test data or use descriptive method names:

```csharp
[Theory(DisplayName = "Should calculate handicap correctly")]
[MemberData(nameof(HandicapTestCases))]
public void CalculateHandicap_ShouldComputeCorrectValue_WhenGivenAverageAndBasis(int average, int basis, decimal factor, int expected) { }

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
- Use `NullLogger<T>` instead of mocking `ILogger<T>` — simpler and avoids mock setup complexity
- **Always use `MockBehavior.Strict`** — fails fast on unexpected calls, making tests more reliable

```csharp
// Correct - use NullLogger and MockBehavior.Strict
var mockApi = new Mock<IApiClient>(MockBehavior.Strict);
var sut = new TournamentService(
    mockApi.Object,
    NullLogger<TournamentService>.Instance);

// Incorrect - don't mock ILogger
var mockLogger = new Mock<ILogger<TournamentService>>();
var sut = new TournamentService(mockApi.Object, mockLogger.Object);

// Incorrect - missing MockBehavior.Strict
var mockApi = new Mock<IApiClient>();
```

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

**API Integration Test Pattern**:

```csharp
[IntegrationTest]
[Component("Tournaments")]
public class TournamentEndpointTests : IClassFixture<AppFixture>, IAsyncLifetime
{
    private readonly AppFixture _fixture;

    public TournamentEndpointTests(AppFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync() { }

    public async Task DisposeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    [Fact]
    public async Task CreateTournament_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var client = _fixture.CreateAuthenticatedClient("TournamentManager");

        var request = new CreateTournamentRequest
        {
            Tournament = new TournamentInput
            {
                TournamentName = "Test Tournament",
                StartDate = DateTime.UtcNow.AddMonths(1),
                Location = "Boston, MA",
                MaxParticipants = 128
            }
        };

        // Act
        var (response, result) = await client
            .POSTAsync<CreateTournamentEndpoint, CreateTournamentRequest, TournamentResponse>(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        result.TournamentName.Should().Be("Test Tournament");
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateTournament_InvalidRequest_ReturnsBadRequest()
    {
        var client = _fixture.CreateAuthenticatedClient("TournamentManager");

        var request = new CreateTournamentRequest
        {
            Tournament = new TournamentInput { TournamentName = "" }  // Invalid
        };

        var (response, result) = await client
            .POSTAsync<CreateTournamentEndpoint, CreateTournamentRequest, ProblemDetails>(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Status.Should().Be(400);
        result.Title.Should().Be("Validation Error");
    }
}
```

**AppFixture**:

```csharp
public class AppFixture : WebApplicationFactory<Program>
{
    public string ConnectionString { get; private set; }

    public HttpClient CreateAuthenticatedClient(params string[] roles)
    {
        var client = CreateClient();
        var token = GenerateTestToken(roles);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task ResetDatabaseAsync()
    {
        // Use Respawn for fast database reset
    }

    private string GenerateTestToken(string[] roles)
    {
        // Test-only JWT generation with provided roles
    }
}
```

**Testing rules**:

- Use `WebApplicationFactory<Program>` via `AppFixture`
- Database cleanup after each test using Respawn
- Use `CreateAuthenticatedClient(roles)` for authenticated endpoints
- Use Fast Endpoints testing helpers for type-safe requests
- Each layer has its own test project

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
