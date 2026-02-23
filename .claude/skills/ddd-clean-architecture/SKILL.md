---
name: ddd-clean-architecture
description: Implement domain-driven design (DDD) and clean architecture patterns in ASP.NET Core Web APIs using Fast Endpoints and the REPR pattern. Use this skill when building or refactoring backend services that need strategic domain modeling, tactical DDD patterns (entities, value objects, aggregates, domain events, repositories), and clean architecture layers (domain, application, infrastructure, presentation). Follows principles from Eric Evans' "Domain-Driven Design" and Robert C. Martin's "Clean Architecture".
license: Complete terms in LICENSE.txt
---

This skill guides implementation of domain-driven design and clean architecture patterns in ASP.NET Core Web APIs, following the strategic and tactical patterns from Eric Evans' "Domain-Driven Design" and the architectural principles from Robert C. Martin's "Clean Architecture".

The user provides backend requirements: a feature, bounded context, aggregate, or architectural refactoring to implement. They may include domain context, business rules, or technical constraints.

## Strategic Design Principles

Before coding, understand the domain and establish clear boundaries:

- **Ubiquitous Language**: Identify key domain terms and use them consistently in code, comments, and conversations
- **Bounded Contexts**: Define clear boundaries where a domain model applies. Each bounded context has its own model and language
- **Context Mapping**: Understand relationships between bounded contexts (Shared Kernel, Customer-Supplier, Anti-Corruption Layer, etc.)
- **Core Domain**: Identify what makes the business unique and valuable. Invest the most effort here
- **Subdomains**: Distinguish between Core, Supporting, and Generic subdomains to allocate appropriate design effort

## Clean Architecture Layers

Organize code into layers with clear dependency rules (dependencies point inward):

### 1. Domain Layer (Innermost - No Dependencies)

- **Entities**: Objects with identity that persist through time and state changes
- **Value Objects**: Immutable objects defined by their attributes, not identity
- **Aggregates**: Cluster of entities and value objects with a root entity enforcing invariants
- **Domain Events**: Events that domain experts care about
- **Domain Services**: Operations that don't belong to a single entity
- **Specifications**: Business rule predicates that can be combined and reused
- **Enumerations**: Domain-specific enumeration types (use strongly-typed enums)
- **Repository Interfaces** (domain repos only): Defined here only when a domain service requires persistence. Domain repository interfaces always return entities/aggregates — never DTOs

### 2. Application Layer (Orchestration - Depends on Domain)

- **Command Handlers**: `ICommandHandler<TCommand, TResult>` implementations for write operations
- **Query Handlers**: `IQueryHandler<TQuery, TResult>` implementations returning DTOs
- **DTOs**: Data transfer objects for crossing layer boundaries
- **Repository Interfaces** (application repos): The most common location. Command-side interfaces return fully hydrated aggregates; query-side interfaces return DTOs/projections
- **External Service Interfaces**: Defined here, implemented in Infrastructure
- **Validators**: Input validation using FluentValidation (structural only — no business rules, no DB lookups)

### 3. Infrastructure Layer (External - Depends on Application & Domain)

- **Persistence**: EF Core DbContext, repository implementations
- **External Services**: API clients, message queues, email services
- **Identity & Security**: Authentication, authorization implementations
- **Caching**: Redis, in-memory cache implementations
- **File Storage**: Blob storage, file system operations
- **Configuration**: Options pattern implementations

### 4. Presentation Layer (API)

Implemented using **Fast Endpoints** with the REPR (Request-Endpoint-Response) pattern. Each use case gets its own folder containing an endpoint, summary, and validator. See [REPR Pattern with Fast Endpoints](#repr-pattern-with-fast-endpoints).

## Tactical DDD Patterns Implementation

### Entities

- Entities have identity that persists through time and state changes
- Follow the entity base class pattern established in the project
- Implement equality based on identity (Id property)
- Encapsulate business rules and invariants within entities

### Value Objects

Value object representation depends on whether EF Core needs to materialize the type.

#### Persisted Value Objects (EF Core Owned/Complex Types)

Use `sealed record class` for value objects stored in the database. EF Core can bind to private constructors on classes, enabling the factory method pattern while preserving invariants during materialization.

```csharp
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
        => (Amount, Currency) = (amount, currency);

    public static ErrorOr<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
            return Error.Validation("Money.Amount.Negative", "Amount cannot be negative");
        if (string.IsNullOrWhiteSpace(currency))
            return Error.Validation("Money.Currency.Required", "Currency is required");

        return new Money(amount, currency);
    }
}
```

Configure as owned type in EF Core:

```csharp
modelBuilder.Entity<Tournament>().OwnsOne(t => t.EntryFee);
```

#### Non-Persisted Value Objects

Use `readonly record struct` for value objects that don't need EF Core materialization (DTOs, command parameters, transient calculations). This provides stack allocation and avoids defensive copies.

```csharp
public readonly record struct DateRange
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    private DateRange(DateOnly start, DateOnly end)
        => (Start, End) = (start, end);

    public static ErrorOr<DateRange> Create(DateOnly start, DateOnly end)
    {
        if (end < start)
            return Error.Validation("DateRange.End.BeforeStart", "End date must be after start date");

        return new DateRange(start, end);
    }
}
```

**Why the distinction**: EF Core materializes entities by calling constructors and setting properties. For structs, it uses `default(T)` which bypasses any private constructor. Classes allow EF Core to bind to private constructors, preserving invariants during database reads.

| Use `sealed record class`                  | Use `readonly record struct`                |
| ------------------------------------------ | ------------------------------------------- |
| Persisted by EF Core (owned/complex types) | Not persisted (DTOs, commands, transient)   |
| Needs private constructor + factory method | Simple value types without complex creation |
| Larger, heap-appropriate objects           | Small objects (≤ 16 bytes)                  |

### Aggregates

- Identify aggregate boundaries based on transactional consistency needs
- Ensure one root entity per aggregate
- Enforce invariants in the aggregate root
- Reference other aggregates by identity only
- Keep aggregates small and focused

### Aggregate Base Class

Minimal base class for domain event mechanics only. No identity property — each aggregate defines its own identity shape.

```csharp
public abstract class AggregateRoot : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

`AddDomainEvent` is `protected` — only the aggregate itself can raise events.

### Domain Events

- Define domain events for important business occurrences
- Raise events within entities/aggregates when state changes occur
- Handle events at the application layer for cross-aggregate coordination
- Dispatch via `SaveChangesAsync` interceptor or pipeline behavior

```csharp
public class Tournament : AggregateRoot
{
    public void Complete(IReadOnlyList<BowlerId> championIds)
    {
        Status = TournamentStatus.Completed;
        AddDomainEvent(new TournamentCompleted(Id, championIds));
    }
}
```

### Repository Pattern

Repositories are split by concern. Placement depends on who needs the interface:

**Application Repository Interfaces** (most common):

```csharp
// Used by command handlers — returns fully hydrated aggregate
public interface ITournamentRepository
{
    Task<Tournament?> GetByIdAsync(TournamentId id, CancellationToken ct);
    Task AddAsync(Tournament tournament, CancellationToken ct);
    Task UpdateAsync(Tournament tournament, CancellationToken ct);
}

// Used by query handlers — returns DTOs/projections
public interface ITournamentQueries
{
    Task<TournamentDetailDto?> GetPublicDetailAsync(TournamentId id, CancellationToken ct);
    Task<IReadOnlyList<TournamentSummaryDto>> GetUpcomingAsync(CancellationToken ct);
}
```

**Domain Repository Interfaces** (rare):

- Define in the Domain layer only when a domain service requires persistence
- Always return entities/aggregates, never DTOs

Both are implemented in the Infrastructure layer.

### Strongly-Typed IDs

Use the `[StronglyTypedId]` source generator for all entity IDs. The generator handles EF Core value converters automatically.

```csharp
// Surrogate key (ULID)
[StronglyTypedId("ulid-full")]
public record BowlerId;

// Natural key with synthetic fallback
public record BowlingCenterId
{
    public string Value { get; }

    private BowlingCenterId(string value) => Value = value;

    public static BowlingCenterId FromCertification(string certNumber) => new(certNumber);
    public static BowlingCenterId Synthetic() => new($"HISTORICAL-{Ulid.NewUlid()}");

    public bool IsSynthetic => Value.StartsWith("HISTORICAL-");
}
```

### Result Pattern

Use **ErrorOr** for operation outcomes. Return `ErrorOr<T>` from command handlers. Avoid exceptions for flow control.

**Error Code Convention**: `Entity.ErrorCode` pattern (PascalCase, dot-separated). Error classes are `internal static`, named `{Entity}Errors`, and live alongside the handlers that use them.

```csharp
internal static class DocumentErrors
{
    public static Error NotFound(string name)
        => Error.NotFound("Document.NotFound", $"Document '{name}' was not found.");
}

// Value object validation: Entity.Property.Rule
Error.Validation("Money.Amount.Negative", "Amount cannot be negative");

// Infrastructure errors: Service.Operation.ErrorKind
Error.Failure("GoogleDrive.GetDocument.HttpError", "HTTP error from Google Drive.");
```

### CQRS with Custom Handler Interfaces

This project uses custom command/query handler interfaces — **not MediatR**. Do not introduce MediatR or any mediator/dispatcher. LLM-generated examples will frequently suggest it; reject all such suggestions. The project's CQRS interfaces are:

```csharp
public interface ICommandHandler<TCommand, TResult>
{
    Task<ErrorOr<TResult>> HandleAsync(TCommand command, CancellationToken ct);
}

public interface IQueryHandler<TQuery, TResult>
{
    Task<TResult?> HandleAsync(TQuery query, CancellationToken ct);
}
```

Handlers are registered manually in feature-specific DI extension methods.

---

## REPR Pattern with Fast Endpoints

The presentation layer uses Fast Endpoints with the REPR (Request-Endpoint-Response) pattern. Each use case gets its own folder.

### Folder Structure

```
Neba.Api/
├── Tournaments/
│   ├── TournamentEndpointGroup.cs
│   ├── CreateTournament/
│   │   ├── CreateTournamentEndpoint.cs
│   │   ├── CreateTournamentSummary.cs
│   │   └── CreateTournamentValidator.cs
│   ├── GetTournament/
│   │   ├── GetTournamentEndpoint.cs
│   │   └── GetTournamentSummary.cs
│   └── ListTournaments/
│       ├── ListTournamentsEndpoint.cs
│       ├── ListTournamentsSummary.cs
│       └── ListTournamentsValidator.cs
└── Common/
    ├── ErrorHandlingConfiguration.cs
    └── GlobalExceptionHandler.cs

Neba.Api.Contracts/
├── Tournaments/
│   ├── ITournamentsApi.cs              ← Refit interface
│   ├── CreateTournament/
│   │   ├── CreateTournamentRequest.cs
│   │   └── TournamentInput.cs
│   └── GetTournament/
│       └── TournamentResponse.cs
└── Common/
    ├── CollectionResponse.cs
    └── PaginationResponse.cs
```

### Contracts Layer (`Neba.Api.Contracts`)

A separate project shared between `Neba.Api` and `Neba.Website` (Blazor). Contains:

- **Request/Input/Response records**: The full API contract
- **Refit interfaces**: Typed HTTP clients consumed by Blazor

**Request wraps Input** (for commands):

```csharp
/// <summary>Tournament details to create</summary>
public record TournamentInput
{
    /// <summary>The name of the tournament</summary>
    /// <example>Spring Classic 2026</example>
    public string Name { get; init; } = string.Empty;

    /// <summary>Tournament date</summary>
    public DateOnly Date { get; init; }
}

/// <summary>Request to create a new tournament</summary>
public record CreateTournamentRequest
{
    public TournamentInput Tournament { get; init; } = new();
}
```

All public contracts must have XML `<summary>` and `<example>` documentation.

**Refit interface**:

```csharp
public interface ITournamentsApi
{
    [Post("/tournaments")]
    Task<TournamentResponse> CreateAsync([Body] CreateTournamentRequest request, CancellationToken ct = default);

    [Get("/tournaments/{id}")]
    Task<TournamentResponse> GetAsync(string id, CancellationToken ct = default);
}
```

### Endpoint Implementation

```csharp
public sealed class CreateTournamentEndpoint
    : Endpoint<CreateTournamentRequest, TournamentResponse>
{
    private readonly ICommandHandler<CreateTournamentCommand, TournamentDto> _handler;

    public CreateTournamentEndpoint(ICommandHandler<CreateTournamentCommand, TournamentDto> handler)
        => _handler = handler;

    public override void Configure()
    {
        Post("/tournaments");
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

    public override async Task HandleAsync(CreateTournamentRequest req, CancellationToken ct)
    {
        var command = new CreateTournamentCommand(req.Tournament.Name, req.Tournament.Date);
        var result = await _handler.HandleAsync(command, ct);

        if (result.IsError)
        {
            AddError(result.FirstError.Description, result.FirstError.Code);
            await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
            return;
        }

        await SendCreatedAtAsync<GetTournamentEndpoint>(
            new { id = result.Value.Id },
            MapToResponse(result.Value),
            cancellation: ct);
    }

    private static TournamentResponse MapToResponse(TournamentDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name
    };
}
```

**Endpoint rules**:

- Routes start directly with the resource — no `/api` prefix (the API is served from its own subdomain), no version in the path
- Authorization is always explicit: `AllowAnonymous()`, `Roles()`, or `Policies()` — never unspecified
- `WithName()` is required for `SendCreatedAtAsync` and OpenAPI
- All status codes declared with `Produces`/`ProducesProblemDetails`
- Use `AddError()` + `Send.ErrorsAsync(statusCode)` to flow `ErrorOr<T>` failures through the ProblemDetails pipeline

### Endpoint Groups

```csharp
public sealed class TournamentEndpointGroup : Group
{
    public TournamentEndpointGroup()
    {
        Configure("tournaments", ep =>
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

```csharp
public sealed class CreateTournamentSummary : Summary<CreateTournamentEndpoint>
{
    public CreateTournamentSummary()
    {
        Summary = "Create a new bowling tournament";
        Description = "Creates a new tournament. Name must be unique and date must be in the future.";

        ExampleRequest = new CreateTournamentRequest
        {
            Tournament = new TournamentInput { Name = "Spring Classic 2026", Date = new DateOnly(2026, 4, 15) }
        };

        Response<TournamentResponse>(201, "Tournament created successfully");
        Response<ProblemDetails>(400, "Validation errors");
        Response<ProblemDetails>(409, "Business rule conflict");
    }
}
```

### Validators

FluentValidation for **structural validation only** — no business rules, no DB lookups:

```csharp
public sealed class CreateTournamentValidator : Validator<CreateTournamentRequest>
{
    public CreateTournamentValidator()
    {
        RuleFor(x => x.Tournament.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Tournament.Date).GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
```

### Endpoint Checklist

- [ ] Use case folder with Endpoint, Summary, Validator (if needed)
- [ ] Contracts in `Neba.Api.Contracts` (Request/Input, Response, Refit interface updated)
- [ ] Request wraps Input for commands
- [ ] Endpoint is `sealed`
- [ ] `Configure()`: HTTP verb + route (no `/api` prefix), group, version, explicit auth, tags, `WithName()`, all status codes declared
- [ ] Errors use `AddError()` + `Send.ErrorsAsync(statusCode)` for ProblemDetails
- [ ] Validator covers structural rules only
- [ ] Summary class with request/response examples
- [ ] Integration tests written

---

## Dependency Injection

Each layer exposes a single `Add{Layer}Services()` extension method using C# 14 `extension` syntax. Feature-specific registrations are grouped into internal `Add{Feature}Services()` methods co-located with the feature.

```csharp
// Neba.Application/DependencyInjection.cs
public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplicationServices()
        {
            services.AddTournamentServices();
            services.AddBowlerServices();

            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            return services;
        }
    }
}

// Neba.Application/Tournaments/TournamentServices.cs
internal static class TournamentServices
{
    extension(IServiceCollection services)
    {
        internal IServiceCollection AddTournamentServices()
        {
            services.AddScoped<ICommandHandler<CreateTournamentCommand, TournamentDto>,
                CreateTournamentHandler>();
            services.AddScoped<IQueryHandler<GetTournamentQuery, TournamentDto>,
                GetTournamentHandler>();
            return services;
        }
    }
}
```

Infrastructure uses **Scrutor** for assembly scanning where appropriate (e.g., background job handlers). Manual registration is preferred for command/query handlers to keep registrations explicit and co-located with the feature.

---

## Type Design Standards

### Sealed Classes

Seal all classes and records by default unless explicitly designed for inheritance.

```csharp
public sealed class TournamentService { }       // Correct
public sealed record TournamentDto { }          // Correct — seal records too
public abstract class AggregateRoot { }         // Correct — designed for inheritance
public class TournamentService { }              // Incorrect — unsealed without reason
```

### DateTime vs DateTimeOffset

Use `DateTimeOffset` for all points in time. It carries the UTC offset explicitly, eliminating ambiguity during serialization and persistence.

```csharp
public DateTimeOffset CreatedAt { get; }   // Correct
public DateTime CreatedAt { get; }         // Incorrect
```

Use `DateOnly`/`TimeOnly` when only a date or time component is needed.

### IDateTimeProvider

Never call `DateTime.UtcNow` or `DateTimeOffset.UtcNow` directly in application or domain code. Inject `IDateTimeProvider` for testable time.

```csharp
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
```

### Immutable Collections for Static Data

Use `FrozenDictionary<TKey, TValue>` and `FrozenSet<T>` for lookup data initialized once at startup.

```csharp
private static readonly FrozenDictionary<string, TournamentType> TypesByCode =
    new Dictionary<string, TournamentType>
    {
        ["O"] = TournamentType.Open,
        ["S"] = TournamentType.Senior,
    }.ToFrozenDictionary();
```

---

## Essential NuGet Packages

| Purpose | Package |
| ------- | ------- |
| Result pattern | ErrorOr |
| API framework | FastEndpoints |
| API documentation | Scalar.AspNetCore |
| Validation | FluentValidation (via Fast Endpoints) |
| Background jobs | Hangfire, Hangfire.PostgreSql |
| ORM | Microsoft.EntityFrameworkCore |
| DI scanning | Scrutor |
| Typed HTTP clients | Refit |
| Strongly-typed IDs | StronglyTypedId (source generator) |
| Unit testing | xUnit v3, Moq, Shouldly |
| Test data | Bogus |
| Snapshot testing | Verify |
| Testcontainers | Testcontainers.PostgreSql, Testcontainers.Azurite |
| Database reset | Respawn |
| Aspire integration testing | Aspire.Hosting.Testing |

---

## Project Structure

```
src/
├── Neba.Domain/
│   ├── SharedKernel/          ← Base classes, strongly-typed IDs, cross-boundary types
│   ├── Bowlers/
│   ├── BowlingCenters/
│   ├── Tournaments/
│   └── Content/
├── Neba.Application/
│   ├── Common/
│   │   └── Behaviors/         ← Pipeline behaviors (validation, logging)
│   ├── Bowlers/
│   │   ├── Commands/
│   │   └── Queries/
│   ├── Tournaments/
│   └── Storage/               ← IFileStorageService, StoredFile DTO
├── Neba.Infrastructure/
│   ├── Persistence/           ← EF Core DbContext, entity configs, migrations
│   ├── Repositories/          ← Repository implementations
│   ├── Storage/               ← Azure Blob Storage implementation
│   └── ExternalServices/      ← Google Drive, USBC API, etc.
├── Neba.Api/
│   ├── Tournaments/
│   │   ├── TournamentEndpointGroup.cs
│   │   ├── CreateTournament/
│   │   │   ├── CreateTournamentEndpoint.cs
│   │   │   ├── CreateTournamentSummary.cs
│   │   │   └── CreateTournamentValidator.cs
│   │   └── GetTournament/
│   └── Common/
│       ├── ErrorHandlingConfiguration.cs
│       └── GlobalExceptionHandler.cs
├── Neba.Api.Contracts/
│   ├── Tournaments/
│   │   ├── ITournamentsApi.cs
│   │   ├── CreateTournament/
│   │   └── GetTournament/
│   └── Common/
│       ├── CollectionResponse.cs
│       └── PaginationResponse.cs
└── Neba.Website/              ← Blazor Web App (Interactive Auto mode)

tests/
├── Neba.TestFactory/          ← Shared factories, fixtures, trait attributes
├── Neba.Domain.Tests/
├── Neba.Application.Tests/
├── Neba.Infrastructure.Tests/
├── Neba.Api.Tests/
├── Neba.Website.Tests/
└── e2e/                       ← Playwright (TypeScript)
```

Namespace boundaries: Domain folders must not reference each other. Cross-cutting needs go through `SharedKernel`, domain events, or application layer orchestration.

---

## Testing

### Testing Stack

| Purpose | Library |
| ------- | ------- |
| Test framework | xUnit v3 |
| Mocking | Moq (always `MockBehavior.Strict`) |
| Assertions | **Shouldly** |
| Test data | Bogus |
| Snapshot testing | Verify |
| Testcontainers | Testcontainers.PostgreSql, Testcontainers.Azurite |
| Database reset | Respawn |

**Never use FluentAssertions** — this project uses Shouldly for all assertions.

### Test Traits

Every test class must have a category trait and a component trait. These are defined in `Neba.TestFactory`.

```csharp
[UnitTest]
[Component("Tournaments")]
public sealed class CreateTournamentHandlerTests { }

[IntegrationTest]
[Component("Tournaments")]
public sealed class TournamentRepositoryTests : IClassFixture<DatabaseFixture> { }
```

**Component naming**: Use feature folder names (`Tournaments`, `Bowlers`). Add sub-component for specific functionality (`Tournaments.Registration`, `Tournaments.Scoring`).

Run tests by trait:

```bash
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
dotnet test --filter "Component=Tournaments"
dotnet test --filter "Category=Unit&Component=Tournaments"
```

### Test Naming

Method names: `<MethodName>_Should<Outcome>_When<Condition>`. Always use explicit display names.

```csharp
[Fact(DisplayName = "Should fail when squad is at capacity")]
public void RegisterBowler_ShouldFail_WhenSquadAtCapacity() { }

[Theory(DisplayName = "Should validate tournament type eligibility")]
[InlineData(TournamentType.Senior, 49, false, TestDisplayName = "Senior tournament rejects bowler under 50")]
[InlineData(TournamentType.Senior, 50, true,  TestDisplayName = "Senior tournament accepts bowler at 50")]
[InlineData(TournamentType.Open,   25, true,  TestDisplayName = "Open tournament accepts any age")]
public void ValidateEligibility_ShouldVerifyAge(TournamentType type, int age, bool expected) { }
```

### Mock Rules

- **Always `MockBehavior.Strict`** — fails fast on unexpected calls
- **Never mock `ILogger<T>`** — use `NullLogger<T>.Instance`

```csharp
var mockRepo = new Mock<ITournamentRepository>(MockBehavior.Strict);
var sut = new CreateTournamentHandler(
    mockRepo.Object,
    NullLogger<CreateTournamentHandler>.Instance);
```

`MockBehavior.Strict` setups act as implicit verification — no explicit `.Verify()` calls needed.

### Test Data Factories

Factories live in `Neba.TestFactory`. Two creation approaches per factory:

- **`Create()`**: For unit tests. All parameters nullable with valid constant defaults. Pass only what matters for the test.
- **`Bogus(int? seed)`** / **`Bogus(int count, int? seed)`**: For integration tests. Realistic random data via Bogus. Never instantiate entities manually in tests.

```csharp
public static class TournamentFactory
{
    public const string ValidName = "Spring Classic";
    public static readonly DateOnly ValidDate = new(2025, 6, 15);
    public static readonly TournamentType ValidType = TournamentType.Open;

    public static Tournament Create(
        TournamentId? id = null,
        string? name = null,
        DateOnly? date = null,
        TournamentType? type = null)
        => new(id ?? TournamentId.New(), name ?? ValidName, date ?? ValidDate, type ?? ValidType);

    public static Tournament Bogus(int? seed = null) => Bogus(1, seed).First();

    public static IReadOnlyList<Tournament> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<Tournament>()
            .UseSeed(seed ?? Random.Shared.Next())
            .CustomInstantiator(f => new Tournament(
                TournamentId.New(),
                f.Commerce.ProductName(),
                f.Date.FutureDateOnly(),
                f.PickRandom<TournamentType>()));
        return faker.Generate(count);
    }
}
```

Unit test usage — only pass what matters for the scenario:

```csharp
var tournament = TournamentFactory.Create(type: TournamentType.Senior);  // Testing senior rules
var tournament = TournamentFactory.Create(name: "");                      // Testing name validation
```

### Unit Test Pattern

```csharp
[UnitTest]
[Component("Tournaments")]
public sealed class CreateTournamentHandlerTests
{
    private readonly Mock<ITournamentRepository> _repository;
    private readonly CreateTournamentHandler _sut;

    public CreateTournamentHandlerTests()
    {
        _repository = new Mock<ITournamentRepository>(MockBehavior.Strict);
        _sut = new CreateTournamentHandler(
            _repository.Object,
            NullLogger<CreateTournamentHandler>.Instance);
    }

    [Fact(DisplayName = "Should return created tournament when command is valid")]
    public async Task HandleAsync_ShouldReturnDto_WhenCommandValid()
    {
        var command = new CreateTournamentCommand("Spring Classic", new DateOnly(2026, 4, 15));

        _repository
            .Setup(r => r.AddAsync(It.IsAny<Tournament>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.IsError.ShouldBeFalse();
        result.Value.Name.ShouldBe("Spring Classic");
    }
}
```

### Integration Test Pattern

```csharp
[IntegrationTest]
[Component("Tournaments")]
public sealed class TournamentEndpointTests : IClassFixture<AppFixture>, IAsyncLifetime
{
    private readonly AppFixture _fixture;

    public TournamentEndpointTests(AppFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _fixture.ResetDatabaseAsync();

    [Fact(DisplayName = "Should return 201 and location header when request is valid")]
    public async Task CreateTournament_ShouldReturn201_WhenValid()
    {
        var client = _fixture.CreateAuthenticatedClient("TournamentManager");
        var request = new CreateTournamentRequest
        {
            Tournament = new TournamentInput { Name = "Test Tournament", Date = new DateOnly(2026, 4, 15) }
        };

        var (response, result) = await client
            .POSTAsync<CreateTournamentEndpoint, CreateTournamentRequest, TournamentResponse>(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        result.Name.ShouldBe("Test Tournament");
        response.Headers.Location.ShouldNotBeNull();
    }
}
```

### Infrastructure Integration Tests

Services wrapping external SDKs use Testcontainers, not mocks.

```csharp
[IntegrationTest]
[Component("Storage")]
public sealed class AzureBlobStorageServiceTests : IClassFixture<AzuriteFixture>
{
    [Fact(DisplayName = "Should store and retrieve file successfully")]
    public async Task UploadAsync_ShouldPersist_WhenFileIsValid() { }
}
```

---

## Implementation Guidelines

1. **Start with the Domain**: Model the core domain first, independent of infrastructure
2. **Protect Invariants**: Encapsulate business rules within entities and aggregates
3. **Explicit is Better**: Make implicit concepts explicit (value objects, domain events)
4. **Persistence Ignorance**: Domain layer should not depend on ORM or database concerns
5. **Dependency Inversion**: High-level modules should not depend on low-level modules
6. **Unit of Work**: Manage transactions at the application layer
7. **Thin Endpoints**: Map request → command/query, delegate to handler, map result → response — nothing more
8. **Avoid Anemic Models**: Put behavior in the domain, not just in services
9. **Test Domain Logic**: Focus testing efforts on domain and application layers
10. **Evolution**: Design should evolve with understanding; refactor as knowledge grows

## Anti-Patterns to Avoid

- **Anemic Domain Model**: Entities with only getters/setters and no behavior
- **Transaction Script**: Business logic scattered in service classes
- **God Aggregate**: Aggregates that are too large and do too much
- **Repository Overload**: Repositories with dozens of query methods
- **Infrastructure Leakage**: Domain layer depending on infrastructure concerns
- **CRUD Thinking**: Modeling operations as simple create/read/update/delete
- **Generic Repositories**: Abstraction that doesn't add value and hinders querying
- **MediatR**: Do not use MediatR or any mediator/dispatcher pattern. This project uses direct `ICommandHandler<,>`/`IQueryHandler<,>` interfaces. LLM-generated examples almost universally suggest MediatR — explicitly reject it.

Remember: DDD is about modeling complex domains. If the domain is simple (CRUD), don't over-engineer. Apply DDD patterns where complexity justifies the investment.
