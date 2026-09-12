using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Neba.Api.RateLimiting;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.RateLimiting;

[UnitTest]
[Component("Api.RateLimiting")]
public sealed class RateLimitingConfigurationValidationTests
{
    [Fact(DisplayName = "AddRateLimiting throws when PermitLimit is zero or negative")]
    public void AddRateLimiting_ShouldThrow_WhenPermitLimitIsNotPositive()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "0",
                ["RateLimiting:WindowSeconds"] = "60",
            })
            .Build();
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddRateLimiting(config);

        // Assert
        act.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("PermitLimit");
    }

    [Fact(DisplayName = "AddRateLimiting throws when WindowSeconds is zero or negative")]
    public void AddRateLimiting_ShouldThrow_WhenWindowSecondsIsNotPositive()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "10",
                ["RateLimiting:WindowSeconds"] = "0",
            })
            .Build();
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddRateLimiting(config);

        // Assert
        act.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("WindowSeconds");
    }

    [Fact(DisplayName = "AddRateLimiting throws when AuthenticatedPermitLimit is zero or negative")]
    public void AddRateLimiting_ShouldThrow_WhenAuthenticatedPermitLimitIsNotPositive()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "10",
                ["RateLimiting:WindowSeconds"] = "60",
                ["RateLimiting:AuthenticatedPermitLimit"] = "0",
                ["RateLimiting:AuthenticatedWindowSeconds"] = "60",
            })
            .Build();
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddRateLimiting(config);

        // Assert
        act.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("AuthenticatedPermitLimit");
    }

    [Fact(DisplayName = "AddRateLimiting throws when AuthenticatedWindowSeconds is zero or negative")]
    public void AddRateLimiting_ShouldThrow_WhenAuthenticatedWindowSecondsIsNotPositive()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "10",
                ["RateLimiting:WindowSeconds"] = "60",
                ["RateLimiting:AuthenticatedPermitLimit"] = "10",
                ["RateLimiting:AuthenticatedWindowSeconds"] = "0",
            })
            .Build();
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddRateLimiting(config);

        // Assert
        act.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("AuthenticatedWindowSeconds");
    }

    [Fact(DisplayName = "AddRateLimiting configures ForwardedHeaders with XForwardedFor and RFC 1918 networks")]
    public void AddRateLimiting_ShouldConfigureForwardedHeaders_WithRfc1918KnownNetworks()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "10",
                ["RateLimiting:WindowSeconds"] = "60",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddRateLimiting(config);

        // Act
        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>()
            .Value;

        // Assert
        options.ForwardedHeaders.ShouldBe(ForwardedHeaders.XForwardedFor);
        options.KnownIPNetworks.ShouldContain(n =>
            n.BaseAddress.Equals(IPAddress.Parse("10.0.0.0")) && n.PrefixLength == 8);
        options.KnownIPNetworks.ShouldContain(n =>
            n.BaseAddress.Equals(IPAddress.Parse("172.16.0.0")) && n.PrefixLength == 12);
        options.KnownIPNetworks.ShouldContain(n =>
            n.BaseAddress.Equals(IPAddress.Parse("192.168.0.0")) && n.PrefixLength == 16);
    }
}

[IntegrationTest]
[Component("Api.RateLimiting")]
public sealed class RateLimitingConfigurationTests : IAsyncLifetime
{
    private WebApplication? _app;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "1",
                ["RateLimiting:WindowSeconds"] = "60",
            })
            .Build();

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");
        builder.Services.AddRateLimiting(config);

        _app = builder.Build();
        _app.UseRateLimiter();
        _app.MapGet("/probe", () => Results.Ok())
            .RequireRateLimiting(RateLimitingConfiguration.PublicPolicy);

        await _app.StartAsync();

        var addresses = _app.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses;

        _client = new HttpClient { BaseAddress = new Uri(addresses.First()) };
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
    }

    [Fact(DisplayName = "Second request returns 429 when permit limit is 1")]
    public async Task RateLimit_ShouldReturn429_WhenLimitExceeded()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        var first = await _client.GetAsync(new Uri("/probe", UriKind.Relative), ct);
        var second = await _client.GetAsync(new Uri("/probe", UriKind.Relative), ct);

        // Assert
        first.StatusCode.ShouldNotBe(HttpStatusCode.TooManyRequests);
        second.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact(DisplayName = "429 response includes Retry-After header")]
    public async Task RateLimit_ShouldIncludeRetryAfterHeader_On429()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        await _client.GetAsync(new Uri("/probe", UriKind.Relative), ct);
        var response = await _client.GetAsync(new Uri("/probe", UriKind.Relative), ct);

        // Assert
        response.Headers.TryGetValues("Retry-After", out var values).ShouldBeTrue();
        values.Single().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "429 response body is ProblemDetails with status 429")]
    public async Task RateLimit_ShouldReturnProblemDetails_On429()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        await _client.GetAsync(new Uri("/probe", UriKind.Relative), ct);
        var response = await _client.GetAsync(new Uri("/probe", UriKind.Relative), ct);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);

        // Assert
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe(StatusCodes.Status429TooManyRequests);
        problem.Title.ShouldBe("Too Many Requests");
    }
}

[IntegrationTest]
[Component("Api.RateLimiting")]
public sealed class RateLimitingConfigurationAuthenticatedPartitionTests : IAsyncLifetime
{
    private const string AuthenticateHeaderName = "X-Test-Authenticate-As";
    private const string AuthenticateNoClaimHeaderName = "X-Test-Authenticate-NoNameIdentifier";

    private WebApplication? _app;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        // Anonymous stays tightly limited (1/window); authenticated gets a much higher ceiling
        // (10/window) and is partitioned per-user rather than sharing the anonymous IP bucket.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "1",
                ["RateLimiting:WindowSeconds"] = "60",
                ["RateLimiting:AuthenticatedPermitLimit"] = "10",
                ["RateLimiting:AuthenticatedWindowSeconds"] = "60",
            })
            .Build();

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");
        builder.Services.AddRateLimiting(config);

        _app = builder.Build();

        // Stands in for the real JWT/cookie authentication middleware, which must run before the
        // rate limiter so its partition selector can read context.User (see Program.cs).
        _app.Use(async (context, next) =>
        {
            var userId = context.Request.Headers[AuthenticateHeaderName].FirstOrDefault();
            if (!string.IsNullOrEmpty(userId))
            {
                context.User = new ClaimsPrincipal(
                    new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "Test"));
            }
            else if (context.Request.Headers.ContainsKey(AuthenticateNoClaimHeaderName))
            {
                // Authenticated (has an identity marked IsAuthenticated) but missing the
                // NameIdentifier claim the partition selector reads - exercises the fallback
                // to the anonymous per-IP partition in RateLimitingConfiguration.
                context.User = new ClaimsPrincipal(new ClaimsIdentity("Test"));
            }

            await next(context);
        });

        _app.UseRateLimiter();
        _app.MapGet("/probe", () => Results.Ok())
            .RequireRateLimiting(RateLimitingConfiguration.PublicPolicy);

        await _app.StartAsync();

        var addresses = _app.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses;

        _client = new HttpClient { BaseAddress = new Uri(addresses.First()) };
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
    }

    [Fact(DisplayName = "Authenticated caller keeps succeeding past the anonymous permit limit")]
    public async Task RateLimit_ShouldNotReject_WhenAuthenticatedCallerExceedsAnonymousLimit()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var request1 = new HttpRequestMessage(HttpMethod.Get, "/probe");
        request1.Headers.Add(AuthenticateHeaderName, "user-1");
        using var request2 = new HttpRequestMessage(HttpMethod.Get, "/probe");
        request2.Headers.Add(AuthenticateHeaderName, "user-1");

        // Act
        var first = await _client.SendAsync(request1, ct);
        var second = await _client.SendAsync(request2, ct);

        // Assert - anonymous permit limit is 1, so a second request from the same identity only
        // succeeds because it's on the higher authenticated ceiling, not the anonymous one.
        first.StatusCode.ShouldNotBe(HttpStatusCode.TooManyRequests);
        second.StatusCode.ShouldNotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact(DisplayName = "Different authenticated users are partitioned independently")]
    public async Task RateLimit_ShouldPartitionIndependently_ForDifferentAuthenticatedUsers()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var requestUser1 = new HttpRequestMessage(HttpMethod.Get, "/probe");
        requestUser1.Headers.Add(AuthenticateHeaderName, "user-1");
        using var requestUser2 = new HttpRequestMessage(HttpMethod.Get, "/probe");
        requestUser2.Headers.Add(AuthenticateHeaderName, "user-2");

        // Act
        var responseUser1 = await _client.SendAsync(requestUser1, ct);
        var responseUser2 = await _client.SendAsync(requestUser2, ct);

        // Assert
        responseUser1.StatusCode.ShouldNotBe(HttpStatusCode.TooManyRequests);
        responseUser2.StatusCode.ShouldNotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact(DisplayName = "Authenticated caller with no NameIdentifier claim falls back to the anonymous per-IP limit")]
    public async Task RateLimit_ShouldFallBackToAnonymousLimit_WhenAuthenticatedCallerHasNoNameIdentifierClaim()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var request1 = new HttpRequestMessage(HttpMethod.Get, "/probe");
        request1.Headers.Add(AuthenticateNoClaimHeaderName, "true");
        using var request2 = new HttpRequestMessage(HttpMethod.Get, "/probe");
        request2.Headers.Add(AuthenticateNoClaimHeaderName, "true");

        // Act
        var first = await _client.SendAsync(request1, ct);
        var second = await _client.SendAsync(request2, ct);

        // Assert - anonymous permit limit is 1; a caller lacking the NameIdentifier claim never
        // reaches the higher authenticated ceiling, so the second request is rejected just like
        // a genuinely anonymous caller.
        first.StatusCode.ShouldNotBe(HttpStatusCode.TooManyRequests);
        second.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact(DisplayName = "Unauthenticated caller still hits the tighter anonymous limit")]
    public async Task RateLimit_ShouldStillReject_WhenAnonymousCallerExceedsAnonymousLimit()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        var first = await _client.GetAsync(new Uri("/probe", UriKind.Relative), ct);
        var second = await _client.GetAsync(new Uri("/probe", UriKind.Relative), ct);

        // Assert
        first.StatusCode.ShouldNotBe(HttpStatusCode.TooManyRequests);
        second.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}