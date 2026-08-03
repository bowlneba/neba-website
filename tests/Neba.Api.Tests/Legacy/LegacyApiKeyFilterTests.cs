using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

using Neba.Api.Legacy;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy;

[UnitTest]
[Component("Legacy")]
public sealed class LegacyApiKeyFilterTests
{
    private const string ValidApiKey = "test-legacy-api-key";
    private const string HeaderName = "X-Api-Key";

    [Fact(DisplayName = "InvokeAsync should return Unauthorized when the header is missing")]
    public async Task InvokeAsync_ShouldReturnUnauthorized_WhenHeaderIsMissing()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateInvocationContext(headerValue: null);

        // Act
        var result = await filter.InvokeAsync(context, NextReturnsOk);

        // Assert
        result.ShouldBeOfType<UnauthorizedHttpResult>();
    }

    [Fact(DisplayName = "InvokeAsync should return Unauthorized when the header is empty")]
    public async Task InvokeAsync_ShouldReturnUnauthorized_WhenHeaderIsEmpty()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateInvocationContext(string.Empty);

        // Act
        var result = await filter.InvokeAsync(context, NextReturnsOk);

        // Assert
        result.ShouldBeOfType<UnauthorizedHttpResult>();
    }

    [Fact(DisplayName = "InvokeAsync should return Unauthorized when the key does not match")]
    public async Task InvokeAsync_ShouldReturnUnauthorized_WhenKeyDoesNotMatch()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateInvocationContext("wrong-key");

        // Act
        var result = await filter.InvokeAsync(context, NextReturnsOk);

        // Assert
        result.ShouldBeOfType<UnauthorizedHttpResult>();
    }

    [Fact(DisplayName = "InvokeAsync should return Unauthorized when the key matches a prefix but not the full value")]
    public async Task InvokeAsync_ShouldReturnUnauthorized_WhenKeyIsPartialMatch()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateInvocationContext(ValidApiKey[..^1]);

        // Act
        var result = await filter.InvokeAsync(context, NextReturnsOk);

        // Assert
        result.ShouldBeOfType<UnauthorizedHttpResult>();
    }

    [Fact(DisplayName = "InvokeAsync should call next when the key matches")]
    public async Task InvokeAsync_ShouldCallNext_WhenKeyMatches()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateInvocationContext(ValidApiKey);

        // Act
        var result = await filter.InvokeAsync(context, NextReturnsOk);

        // Assert
        result.ShouldBeOfType<Ok>();
    }

    [Fact(DisplayName = "InvokeAsync should stamp a NameIdentifier claim with LegacyActor.Id when the key matches")]
    public async Task InvokeAsync_ShouldStampLegacyActorClaim_WhenKeyMatches()
    {
        // Arrange
        var filter = CreateFilter();
        var context = CreateInvocationContext(ValidApiKey);

        // Act
        await filter.InvokeAsync(context, NextReturnsOk);

        // Assert
        context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier).ShouldBe(LegacyActor.Id);
    }

    [Fact(DisplayName = "InvokeAsync should log a warning when the key is rejected")]
    public async Task InvokeAsync_ShouldLogWarning_WhenKeyIsRejected()
    {
        // Arrange
        var fakeLogger = new FakeLogger<LegacyApiKeyFilter>();
        var filter = CreateFilter(fakeLogger);
        var context = CreateInvocationContext("wrong-key");

        // Act
        await filter.InvokeAsync(context, NextReturnsOk);

        // Assert
        var record = fakeLogger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("/legacy/bowlers/new");
    }

    private static ValueTask<object?> NextReturnsOk(EndpointFilterInvocationContext _) =>
        ValueTask.FromResult<object?>(Results.Ok());

    private static LegacyApiKeyFilter CreateFilter(ILogger<LegacyApiKeyFilter>? logger = null) =>
        new(Options.Create(new LegacySettings { ApiKey = ValidApiKey }), logger ?? NullLogger<LegacyApiKeyFilter>.Instance);

    private static EndpointFilterInvocationContext CreateInvocationContext(string? headerValue)
    {
        var httpContext = new DefaultHttpContext
        {
            Request = { Path = "/legacy/bowlers/new" }
        };

        if (headerValue is not null)
        {
            httpContext.Request.Headers[HeaderName] = headerValue;
        }

        return EndpointFilterInvocationContext.Create(httpContext);
    }
}