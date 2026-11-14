using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.RateLimiting;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.RateLimiting;

[UnitTest]
[Component("Api.RateLimiting")]
public sealed class BowlerSearchRateLimitValidationTests
{
    [Theory(DisplayName = "AddRateLimiting throws when a bowler search setting is zero or negative")]
    [InlineData("RateLimiting:BowlerSearchPermitLimit", "BowlerSearchPermitLimit")]
    [InlineData("RateLimiting:BowlerSearchWindowSeconds", "BowlerSearchWindowSeconds")]
    public void AddRateLimiting_ShouldThrow_WhenBowlerSearchSettingIsNotPositive(string key, string expectedInMessage)
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "10",
                ["RateLimiting:WindowSeconds"] = "60",
                [key] = "0",
            })
            .Build();
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddRateLimiting(config);

        // Assert
        act.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain(expectedInMessage);
    }
}

[IntegrationTest]
[Component("Api.RateLimiting")]
public sealed class BowlerSearchRateLimitTests : IAsyncLifetime
{
    private WebApplication? _app;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        // The public ceiling is high; the bowler search ceiling is 2 per window.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "100",
                ["RateLimiting:WindowSeconds"] = "60",
                ["RateLimiting:BowlerSearchPermitLimit"] = "2",
                ["RateLimiting:BowlerSearchWindowSeconds"] = "60",
            })
            .Build();

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");
        builder.Services.AddRateLimiting(config);

        _app = builder.Build();
        _app.UseRateLimiter();

        var group = _app.MapGroup(string.Empty).RequireRateLimiting(RateLimitingConfiguration.PublicPolicy);
        group.MapGet("/search", () => Results.Ok())
            .RequireRateLimiting(RateLimitingConfiguration.BowlerSearchPolicy);
        group.MapGet("/other", () => Results.Ok());

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

    [Fact(DisplayName = "Third bowler search returns 429 even though the public limit is higher")]
    public async Task BowlerSearch_ShouldReturn429_WhenItsOwnLimitIsExceeded()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var uri = new Uri("/search", UriKind.Relative);

        // Act
        var first = await _client.GetAsync(uri, ct);
        var second = await _client.GetAsync(uri, ct);
        var third = await _client.GetAsync(uri, ct);

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        third.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact(DisplayName = "Other endpoints in the same group keep the public limit when bowler search is exhausted")]
    public async Task PublicEndpoints_ShouldKeepPublicLimit_WhenBowlerSearchIsExhausted()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var search = new Uri("/search", UriKind.Relative);
        for (var i = 0; i < 3; i++)
        {
            await _client.GetAsync(search, ct);
        }

        // Act
        var other = await _client.GetAsync(new Uri("/other", UriKind.Relative), ct);

        // Assert
        other.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
