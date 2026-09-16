using System.Diagnostics.CodeAnalysis;
using System.Net;

using ErrorOr;

using FastEndpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Neba.Api.Features.Cache.ClearCache;
using Neba.Api.Security;
using Neba.Api.Versioning;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

using Npgsql;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Features.Cache.ClearCache;

// Running in Development: the endpoint's Configure() allows anonymous access instead of requiring
// the Cache.Clear permission. See ClearCacheEndpointAuthorizationTests for the permission-gated
// non-Development branch.
[IntegrationTest]
[Component("Cache")]
[SuppressMessage("Design", "CA2213:Disposable fields should be disposed", Justification = "_app is intentionally never disposed - see DisposeAsync.")]
[Collection<SecurityDbContextFixture>]
public sealed class ClearCacheEndpointDevelopmentAuthorizationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = TestAccessTokenFactory.Settings.Issuer,
            ["JwtSettings:Audience"] = TestAccessTokenFactory.Settings.Audience,
            ["JwtSettings:SigningKey"] = TestAccessTokenFactory.Settings.SigningKey,
            ["WebsiteSettings:BaseUrl"] = "https://bowlneba.com",
            // WebApplication.CreateBuilder() picks up Neba.Api's appsettings.Development.json
            // (copied into this test project's output directory via the ProjectReference) once
            // EnvironmentName is "Development", and that file restricts AllowedHosts to
            // "localhost" - which the test client's 127.0.0.1 loopback address doesn't match.
            // Override it back to "*" so the request isn't rejected by host filtering.
            ["AllowedHosts"] = "*",
        });

        builder.Services.AddSingleton(new NpgsqlDataSourceBuilder(fixture.ConnectionString).Build());

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<ClearCacheCommand>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<ClearCacheCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);
        builder.Services.AddSingleton(commandHandlerMock.Object);

        builder.Services
            .AddFastEndpoints(options =>
            {
                options.Assemblies = [typeof(ClearCacheEndpoint).Assembly];
                options.Filter = type => type == typeof(ClearCacheEndpoint);
            })
            .AddVersioning();

        builder.AddSecurity();

        _app = builder.Build();
        await _app.UseSecurityInfrastructureAsync();

        // See DeleteArticleEndpointAuthorizationTests for why UsePropertyNamingPolicy is disabled here.
        _app.UseFastEndpoints(c => c.Validation.UsePropertyNamingPolicy = false);

        await _app.StartAsync();

        var address = _app.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses
            .First();

        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();

        // Deliberately not disposing _app - see DeleteArticleEndpointAuthorizationTests for why.
    }

    [Fact(DisplayName = "DELETE /cache returns 204 with no access token when running in Development")]
    public async Task Delete_ShouldReturn204_WithNoAccessToken_WhenDevelopment()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/cache");

        // Act
        using var response = await _client.SendAsync(request, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}