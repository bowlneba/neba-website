# New Endpoint

Scaffold a new Fast Endpoints use case following project conventions.

**Usage**: `/new-endpoint <Domain> <Action>` (e.g., `/new-endpoint Tournaments Create`)

## Files to Create

For `{Domain}/{Action}{Entity}`:

### 1. API Layer (`src/Neba.Api/{Domain}/{Action}{Entity}/`)

**{Action}{Entity}Endpoint.cs**:

```csharp
public sealed class {Action}{Entity}Endpoint : Endpoint<{Action}{Entity}Request, {Entity}Response>
{
    public override void Configure()
    {
        Post("/api/{domain}/{action}"); // or appropriate verb/route
        AllowAnonymous(); // or Roles("Admin"), Policies("RequireAdmin")
        Description(d => d
            .WithName("{Action}{Entity}")
            .Produces<{Entity}Response>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status404NotFound));
    }
}
```

**{Action}{Entity}Summary.cs**:

```csharp
public sealed class {Action}{Entity}Summary : Summary<{Action}{Entity}Endpoint>
{
    public {Action}{Entity}Summary()
    {
        Summary = "{Action} a {Entity}";
        Description = "Detailed description here";
        ExampleRequest = new {Action}{Entity}Request { /* example */ };
    }
}
```

**{Action}{Entity}Validator.cs**:

```csharp
public sealed class {Action}{Entity}Validator : Validator<{Action}{Entity}Request>
{
    public {Action}{Entity}Validator()
    {
        // Structural validation ONLY - no DB lookups, no business rules
        RuleFor(x => x.Input.Name).NotEmpty().MaximumLength(100);
    }
}
```

### 2. Contracts (`src/Neba.Api.Contracts/{Domain}/{Action}{Entity}/`)

**{Action}{Entity}Request.cs**:

```csharp
public sealed record {Action}{Entity}Request
{
    public required {Entity}Input Input { get; init; }
}
```

**{Entity}Input.cs** (for commands):

```csharp
public sealed record {Entity}Input
{
    public required string Name { get; init; }
    // ... other properties
}
```

**{Entity}Response.cs**:

```csharp
public sealed record {Entity}Response
{
    public required string Id { get; init; }
    // ... other properties
}
```

### 3. Application Layer (`src/Neba.Application/{Domain}/Commands/`)

**{Action}{Entity}Command.cs**:

```csharp
public sealed record {Action}{Entity}Command(/* parameters */) : IRequest<ErrorOr<{Entity}>>;
```

**{Action}{Entity}Handler.cs**:

```csharp
public sealed class {Action}{Entity}Handler : IRequestHandler<{Action}{Entity}Command, ErrorOr<{Entity}>>
{
    public async Task<ErrorOr<{Entity}>> Handle({Action}{Entity}Command request, CancellationToken ct)
    {
        // Business logic here - return errors, don't throw
    }
}
```

### 4. Tests

**Unit test** (`tests/Neba.Application.Tests/{Domain}/`):

```csharp
[Fact, UnitTest, Component("{Domain}")]
[DisplayName("{Action}{Entity} should succeed with valid input")]
public async Task {Action}{Entity}_WithValidInput_Succeeds()
{
    // Arrange using test factories
    // Act
    // Assert with Shouldly
}
```

**Integration test** (`tests/Neba.Api.Tests/{Domain}/`):

```csharp
[Fact, IntegrationTest, Component("{Domain}")]
[DisplayName("{Action}{Entity} endpoint should return 201 Created")]
public async Task {Action}{Entity}_Endpoint_Returns201()
{
    // Arrange
    // Act - call endpoint
    // Assert response
}
```

## Checklist

- [ ] Authorization explicitly configured
- [ ] All status codes documented with `Produces()`/`ProducesProblemDetails()`
- [ ] `WithName()` set in Description
- [ ] Validator only has structural validation
- [ ] Handler returns `ErrorOr<T>`
- [ ] Tests have all required traits
