using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.RateLimiting;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.RateLimiting;

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
        values!.Single().ShouldNotBeNullOrWhiteSpace();
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