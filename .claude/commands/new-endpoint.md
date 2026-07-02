# New Endpoint

Scaffold a new Fast Endpoints use case following project conventions.

**Usage**: `/new-endpoint <description>` — describe the endpoint you want, including the domain, HTTP verb, route, and any relevant context files.

## Files to Create

For `{Domain}/{Action}{Entity}`:

### 1. API Layer (`src/Neba.Api/Features/{Domain}/`)

**`{Domain}EndpointGroup.cs`** (if it doesn't already exist):

```csharp
internal sealed class {Domain}EndpointGroup : SubGroup<BaseEndpointGroup>
{
    public {Domain}EndpointGroup()
    {
        VersionSets.CreateApi("{Domain}", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("{route-prefix}", endpoint => endpoint
            .Description(description => description
                .WithTags("{Domain}")
                .ProducesProblemDetails(500)));
    }
}
```

**`{Action}{Entity}Endpoint.cs`**:

```csharp
internal sealed class {Action}{Entity}Endpoint(IQueryHandler<{Action}{Entity}Query, {Result}> queryHandler)
    : EndpointWithoutRequest<{Response}>  // or Endpoint<{Request}, {Response}> for endpoints with input
{
    public override void Configure()
    {
        Get("{route}");  // or Post/Put/Delete as appropriate
        Group<{Domain}EndpointGroup>();

        Options(options => options
            .WithVersionSet("{Domain}")
            .MapToApiVersion(new ApiVersion(1, 0)));

        AllowAnonymous();  // or Roles("Admin") / Policies("RequireAdmin")

        Description(description => description
            .WithName("{Action}{Entity}")
            .WithTags("Public")
            .Produces<{Response}>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(CancellationToken ct)  // or HandleAsync(TRequest req, CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new {Action}{Entity}Query(), ct);

        var response = new {Response}
        {
            // map from result/dto to response
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}
```

**`{Action}{Entity}EndpointSummary.cs`**:

```csharp
internal sealed class {Action}{Entity}Summary : Summary<{Action}{Entity}Endpoint>
{
    public {Action}{Entity}Summary()
    {
        Summary = "{One-line description}.";
        Description = "Detailed description of what this endpoint does.";

#pragma warning disable S1075 // URIs should not be hardcoded
        Response(200, "Description of the 200 response.",
            contentType: MediaTypeNames.Application.Json,
            example: new {Response}
            {
                // example values using factory constants where possible
            });
#pragma warning restore S1075 // URIs should not be hardcoded
    }
}
```

**`{Action}{Entity}RequestValidator.cs`** (for endpoints with a request type, structural validation only):

```csharp
internal sealed class {Action}{Entity}RequestValidator : Validator<{Action}{Entity}Request>
{
    public {Action}{Entity}RequestValidator()
    {
        // Structural validation ONLY — no DB lookups, no business rules
        RuleFor(r => r.Id)
            .NotEmpty()
            .WithErrorCode("{Action}{Entity}Request.IdRequired")
            .WithMessage("Id is required.");
    }
}
```

**`{Action}{Entity}Request.cs`** (for GET endpoints with query params — internal to the API, not in Contracts):

```csharp
internal sealed class {Action}{Entity}Request
{
    [BindFrom("page")]    // always use [BindFrom] to make the query-string key explicit
    public int Page { get; set; } = 1;

    [BindFrom("pageSize")]
    public int PageSize { get; set; } = 10;
}
```

### 2. Contracts (`src/Neba.Api.Contracts/{Domain}/`)

**`{Entity}Response.cs`**:

```csharp
/// <summary>
/// Represents a ... for display in ...
/// </summary>
public sealed record {Entity}Response
{
    /// <summary>...</summary>
    public required string Id { get; init; }
    // ... other properties with XML docs
}
```

**`I{Domain}Api.cs`** (Refit contract, if it doesn't already exist):

```csharp
public interface I{Domain}Api
{
    [Get("/{route}")]
    Task<IApiResponse<{Response}>> {Action}{Entity}Async(CancellationToken cancellationToken = default);
}
```

### 3. Test Factories (`tests/Neba.TestFactory/{Domain}/`)

Create a factory for **every new type** introduced: internal DTOs, public response contracts, and request types.

**`{Entity}DtoFactory.cs`** (for internal `{Entity}Dto` used by query handlers):

```csharp
public static class {Entity}DtoFactory
{
    public const string ValidSlug = "test-item";  // named constants for defaults
    public const string ValidTitle = "Test Item";
    public static readonly DateTimeOffset ValidDate = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static {Entity}Dto Create(
        string? slug = null,
        string? title = null,
        DateTimeOffset? date = null)
        => new()
        {
            Slug = slug ?? ValidSlug,
            Title = title ?? ValidTitle,
            Date = date ?? ValidDate
        };

    public static IReadOnlyCollection<{Entity}Dto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<{Entity}Dto>()
            .CustomInstantiator(f => new()
            {
                Slug = f.Lorem.Slug(),
                Title = f.Random.Words(3),
                Date = f.Date.PastOffset(2)
            });

        if (seed.HasValue) faker.UseSeed(seed.Value);
        return faker.Generate(count);
    }
}
```

**`{Entity}ResponseFactory.cs`** (for the public `{Entity}Response` in Contracts):

```csharp
public static class {Entity}ResponseFactory
{
    public const string ValidSlug = "test-item";
    public const string ValidTitle = "Test Item";

    public static {Entity}Response Create(
        string? slug = null,
        string? title = null)
        => new()
        {
            Slug = slug ?? ValidSlug,
            Title = title ?? ValidTitle
        };

    public static IReadOnlyCollection<{Entity}Response> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<{Entity}Response>()
            .CustomInstantiator(f => new()
            {
                Slug = f.Lorem.Slug(),
                Title = f.Random.Words(3)
            });

        if (seed.HasValue) faker.UseSeed(seed.Value);
        return faker.Generate(count);
    }
}
```

### 4. Endpoint Tests (`tests/Neba.Api.Tests/Features/{Domain}/{Action}{Entity}/`)

Endpoint tests are **unit tests** using `Factory.Create<TEndpoint>()` from FastEndpoints and **Verify** (VerifyXunit) for snapshot assertions on the mapped response shape.

**`{Action}{Entity}EndpointTests.cs`**:

```csharp
[UnitTest]
[Component("{Domain}")]
public sealed class {Action}{Entity}EndpointTests
{
    [Fact(DisplayName = "HandleAsync should return OK with mapped {entities} when query succeeds")]
    public async Task HandleAsync_ShouldReturnOkWithMapped{Entities}_WhenQuerySucceeds()
    {
        // Arrange
        var dtos = {Entity}DtoFactory.Bogus(3, 42);  // always use a seed for snapshot tests
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<{Action}{Entity}Query, IReadOnlyCollection<{Entity}Dto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<{Action}{Entity}Query>(), cancellationToken))
            .ReturnsAsync(dtos);

        var endpoint = Factory.Create<{Action}{Entity}Endpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);
        // For endpoints with a request: await endpoint.HandleAsync(new {Action}{Entity}Request { ... }, cancellationToken);

        // Assert — Verify snapshots the full response payload structure
        await Verify(endpoint.Response);
    }

    [Fact(DisplayName = "HandleAsync should return OK with empty collection when no {entities} exist")]
    public async Task HandleAsync_ShouldReturnOkWithEmpty{Entities}_WhenNo{Entities}Exist()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var queryHandlerMock = new Mock<IQueryHandler<{Action}{Entity}Query, IReadOnlyCollection<{Entity}Dto>>>(MockBehavior.Strict);
        queryHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<{Action}{Entity}Query>(), cancellationToken))
            .ReturnsAsync([]);

        var endpoint = Factory.Create<{Action}{Entity}Endpoint>(queryHandlerMock.Object);

        // Act
        await endpoint.HandleAsync(cancellationToken);

        // Assert — explicit for empty/edge cases; no Verify needed
        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Items.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Configure should register anonymous GET route under /{domain}")]
    public void Configure_ShouldRegisterAnonymousGetRoute_Under{Domain}Path()
    {
        // Arrange
        var queryHandlerMock = new Mock<IQueryHandler<{Action}{Entity}Query, IReadOnlyCollection<{Entity}Dto>>>(MockBehavior.Strict);
        var endpoint = Factory.Create<{Action}{Entity}Endpoint>(queryHandlerMock.Object);

        // Assert
        endpoint.Definition.Verbs.ShouldContain("GET");
        endpoint.Definition.Routes.ShouldContain(r => r.Contains("{domain}"), "should be under the /{domain} path");
        endpoint.Definition.AnonymousVerbs.ShouldNotBeEmpty();
    }
}
```

**Important Verify rules**:
- Always use a fixed seed (`Bogus(count, seed)`) in snapshot tests so the output is deterministic.
- The snapshot file (`*.verified.txt`) is created on the first run. Run the test, confirm the received output is correct, then accept it by copying `.received.txt` → `.verified.txt`.
- Use `await Verify(endpoint.Response)` to snapshot the **full response payload**, validating the entire field mapping in one assertion.
- Use Verify for happy-path structure checks. Use explicit Shouldly assertions for edge cases (empty, null, error status codes) where the exact shape doesn't need snapshotting.
- **Do NOT use** `Verify` from Moq — this is VerifyXunit (`global using static VerifyXunit.Verifier`).

**For endpoints that return `ErrorOr<T>`** (commands/queries that can fail), add tests for each error path:

```csharp
[Fact(DisplayName = "HandleAsync should return 404 when {entity} does not exist")]
public async Task HandleAsync_ShouldReturn404_When{Entity}DoesNotExist()
{
    // Arrange
    var cancellationToken = TestContext.Current.CancellationToken;
    var queryHandlerMock = new Mock<IQueryHandler<...>>(MockBehavior.Strict);
    queryHandlerMock
        .Setup(h => h.HandleAsync(It.IsAny<...>(), cancellationToken))
        .ReturnsAsync(Error.NotFound());

    var endpoint = Factory.Create<{Action}{Entity}Endpoint>(queryHandlerMock.Object);

    // Act
    await endpoint.HandleAsync(new {Action}{Entity}Request { ... }, cancellationToken);

    // Assert
    endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
}
```

**For paginated endpoints** (using `PagedResult<T>` / `PaginationResponse<T>`), verify pagination field mapping explicitly and in the snapshot:

```csharp
[Fact(DisplayName = "HandleAsync should map pagination fields correctly from request and result")]
public async Task HandleAsync_ShouldMapPaginationFields_CorrectlyFromRequestAndResult()
{
    // Arrange
    var dtos = {Entity}DtoFactory.Bogus(3, 42);
    var pagedResult = dtos.WithTotalItems(15);  // PagedResultExtensions helper
    // ...mock setup...

    // Act
    await endpoint.HandleAsync(new {Action}{Entity}Request { Page = 3, PageSize = 5 }, cancellationToken);

    // Assert
    endpoint.Response.PageNumber.ShouldBe(3);
    endpoint.Response.PageSize.ShouldBe(5);
    endpoint.Response.TotalItems.ShouldBe(15);
    // TotalPages, HasNextPage, HasPreviousPage are computed on PaginationResponse — covered by snapshot
}
```

### 5. Blazor Consumer Tests (`tests/Neba.Website.Tests/{Domain}/`, if a page/component calls `I{Domain}Api`)

When a Blazor page or component calls the new `I{Domain}Api` method (via `ApiExecutor`), mock the Refit interface with Moq as usual, but build the `IApiResponse<T>` it returns with **`StubApiResponse<T>`** from `Refit.Testing` — not `Mock<IApiResponse<T>>`. `StubApiResponse<T>` is a hand-written `IApiResponse<T>` with init-only properties, purpose-built for this exact case (testing code that consumes `IApiResponse<T>` directly, without going through HTTP).

```csharp
using Refit;
using Refit.Testing;

[UnitTest]
[Component("Website.{Domain}")]
public sealed class {Entity}PageTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<I{Domain}Api> _mockApi;

    public {Entity}PageTests()
    {
        _mockApi = new Mock<I{Domain}Api>(MockBehavior.Strict);

        var mockStopwatch = new Mock<IStopwatchProvider>(MockBehavior.Strict);
        mockStopwatch.Setup(x => x.GetTimestamp()).Returns(0L);
        mockStopwatch.Setup(x => x.GetElapsedTime(It.IsAny<long>())).Returns(TimeSpan.Zero);

        _ctx = new BunitContext();
        _ctx.Services.AddSingleton(_mockApi.Object);
        _ctx.Services.AddSingleton(new ApiExecutor(mockStopwatch.Object, NullLogger<ApiExecutor>.Instance));
    }

    public void Dispose() => _ctx.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetupSuccessResponse(IReadOnlyCollection<{Entity}Response> items)
    {
        using var response = new StubApiResponse<CollectionResponse<{Entity}Response>>
        {
            IsSuccessStatusCode = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new CollectionResponse<{Entity}Response> { Items = items }
        };

        _mockApi
            .Setup(x => x.{Action}{Entity}Async(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private void SetupFailureResponse(System.Net.HttpStatusCode statusCode)
    {
        using var response = new StubApiResponse<CollectionResponse<{Entity}Response>>
        {
            IsSuccessStatusCode = false,
            StatusCode = statusCode,
            Content = null
        };

        _mockApi
            .Setup(x => x.{Action}{Entity}Async(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }
}
```

**Rules**:
- `using var response = new StubApiResponse<T> { ... };` — `StubApiResponse<T>.Dispose()` is a documented no-op, but Roslyn's `CA2000` still flags direct construction of an `IDisposable`, so wrap it in `using` to satisfy the analyzer.
- Pass the stub directly to `.ReturnsAsync(response)` — no `.Object` (unlike `Mock<T>`, `StubApiResponse<T>` *is* the `IApiResponse<T>`, not a wrapper).
- If the stub is captured inside a lambda passed through `Task.FromResult(...)` (e.g. testing `ApiExecutor` itself rather than a page), `Task<T>` is invariant, so `Task.FromResult(response)` infers `Task<StubApiResponse<T>>`, not `Task<IApiResponse<T>>`. Use `Task.FromResult<IApiResponse<T>>(response)` explicitly, or declare `IApiResponse<T> response = new StubApiResponse<T> { ... };` so the static type is already the interface. In that scenario, prefer `#pragma warning disable CA2000` with a rationale comment over `using var`, since wrapping in `using` alongside `Task.FromResult` trips `CA2025` (disposal-before-task-completion) as a false positive.
- Reserve `Mock<I{Domain}Api>` for the Refit interface itself — only the `IApiResponse<T>` it returns should be a `StubApiResponse<T>`.

## Checklist

- [ ] `{Domain}EndpointGroup` exists (create if missing)
- [ ] Authorization explicitly configured (`AllowAnonymous()`, `Roles()`, or `Policies()`)
- [ ] All status codes documented with `Produces()`/`ProducesProblemDetails()`
- [ ] `WithName()` and `WithTags()` set in `Description`
- [ ] `[BindFrom("key")]` on every query-param property in request classes
- [ ] Validator has structural validation only (no DB lookups, no business rules)
- [ ] Response contract lives in `Neba.Api.Contracts/{Domain}/`
- [ ] `I{Domain}Api` Refit interface updated with the new method
- [ ] `{Entity}DtoFactory` exists in `Neba.TestFactory/{Domain}/`
- [ ] `{Entity}ResponseFactory` exists in `Neba.TestFactory/{Domain}/`
- [ ] Endpoint tests: snapshot test (Verify), empty/edge case (Shouldly), Configure (route + anon)
- [ ] Snapshot `.verified.txt` accepted after first run
- [ ] Blazor consumer tests (if applicable): `IApiResponse<T>` built with `StubApiResponse<T>` from `Refit.Testing`, not `Mock<IApiResponse<T>>`
