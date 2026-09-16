using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;

using ErrorOr;

using FastEndpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Neba.Api.Contracts.Security;
using Neba.Api.Features.Cache.ClearCache;
using Neba.Api.Security;
using Neba.Api.Versioning;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

using Npgsql;

using NebaMessaging = Neba.Api.Messaging;
using SecurityRoles = Neba.Api.Security.Domain.Roles;

namespace Neba.Api.Tests.Features.Cache.ClearCache;

// Not running in Development: the endpoint's Configure() requires the Cache.Clear permission.
// See ClearCacheEndpointDevelopmentAuthorizationTests for the anonymous-in-Development branch.
[IntegrationTest]
[Component("Cache")]
[SuppressMessage("Design", "CA2213:Disposable fields should be disposed", Justification = "_app is intentionally never disposed - see DisposeAsync.")]
[Collection<SecurityDbContextFixture>]
public sealed class ClearCacheEndpointAuthorizationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = TestAccessTokenFactory.Settings.Issuer,
            ["JwtSettings:Audience"] = TestAccessTokenFactory.Settings.Audience,
            ["JwtSettings:SigningKey"] = TestAccessTokenFactory.Settings.SigningKey,
            ["WebsiteSettings:BaseUrl"] = "https://bowlneba.com",
        });

        // AddSecurity() registers SecurityDbContext against a NpgsqlDataSource, and
        // UseSecurityInfrastructureAsync() seeds roles/permission claims via RoleManager on
        // startup - both require a real Postgres connection, hence the Testcontainers-backed fixture.
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

        // See DeleteArticleEndpointAuthorizationTests for why UsePropertyNamingPolicy is disabled here -
        // MTP runs the whole test assembly in one shared process, and this static mutation would
        // otherwise leak into unrelated validator tests for the rest of the run.
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

    private async Task<HttpResponseMessage> SendDeleteAsync(string? accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/cache");

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await _client.SendAsync(request, ct);
    }

    [Fact(DisplayName = "DELETE /cache returns 401 when no access token is provided")]
    public async Task Delete_ShouldReturn401_WhenNoAccessTokenIsProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await SendDeleteAsync(accessToken: null, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "DELETE /cache returns 403 when the token lacks the Cache.Clear permission")]
    public async Task Delete_ShouldReturn403_WhenTokenLacksClearCachePermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var token = TestAccessTokenFactory.Create(permissions: [Permissions.GetUsers]);

        // Act
        using var response = await SendDeleteAsync(token, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "DELETE /cache returns 403 when the token has roles but no permission claims")]
    public async Task Delete_ShouldReturn403_WhenTokenHasRolesButNoPermissionClaims()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var token = TestAccessTokenFactory.Create(roles: [SecurityRoles.Admin]);

        // Act
        using var response = await SendDeleteAsync(token, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "DELETE /cache returns 204 when the token has the Cache.Clear permission")]
    public async Task Delete_ShouldReturn204_WhenTokenHasClearCachePermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var token = TestAccessTokenFactory.Create(permissions: [Permissions.ClearCache]);

        // Act
        using var response = await SendDeleteAsync(token, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}