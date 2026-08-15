using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Neba.Api.Legacy;
using Neba.TestFactory.Attributes;

namespace Neba.Api.Tests.Legacy;

// Mirrors the single-file shape of the production source (Legacy/Health.cs) so this test file is
// removed alongside it at sunset.

[IntegrationTest]
[Component("Legacy")]
public sealed class HealthEndpointTests : IAsyncLifetime
{
    private const string ValidApiKey = "test-legacy-api-key";

    private WebApplication _app = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton(Options.Create(new LegacySettings { ApiKey = ValidApiKey }));

        _app = builder.Build();

        // Health.cs is deliberately mapped outside MapLegacyGroup()'s LegacyApiKeyFilter, so there
        // is no group filter here to prove is wired - map it directly, as it's mapped in Program.cs.
        _app.MapLegacyHealth();

        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync(TestContext.Current.CancellationToken);
        await _app.DisposeAsync();
    }

    [Fact(DisplayName = "GET /legacy/health returns 403 when the X-Api-Key header is missing")]
    public async Task Get_ShouldReturn403_WhenApiKeyHeaderIsMissing()
    {
        // Arrange
        using var client = _app.GetTestClient();

        // Act
        using var response = await client.GetAsync(new Uri("/legacy/health", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "GET /legacy/health returns 403 when the X-Api-Key header is wrong")]
    public async Task Get_ShouldReturn403_WhenApiKeyHeaderIsWrong()
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        // Act
        using var response = await client.GetAsync(new Uri("/legacy/health", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "GET /legacy/health returns 204 when the X-Api-Key header is valid")]
    public async Task Get_ShouldReturn204_WhenApiKeyHeaderIsValid()
    {
        // Arrange
        using var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ValidApiKey);

        // Act
        using var response = await client.GetAsync(new Uri("/legacy/health", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}