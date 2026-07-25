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

using Neba.Api.Contracts.Security;
using Neba.Api.Features.Tournaments.DeleteTournament;
using Neba.Api.Security;
using Neba.Api.Versioning;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;

using Npgsql;

using NebaMessaging = Neba.Api.Messaging;
using SecurityRoles = Neba.Api.Security.Domain.Roles;

namespace Neba.Api.Tests.Features.Tournaments.DeleteTournament;

[IntegrationTest]
[Component("Tournaments")]
[SuppressMessage("Design", "CA2213:Disposable fields should be disposed", Justification = "_app is intentionally never disposed - see DisposeAsync.")]
[Collection<SecurityDbContextFixture>]
public sealed class DeleteTournamentEndpointAuthorizationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private const string ValidTournamentId = "01000000000000000000000001";

    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        await fixture.ResetAsync();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = TestAccessTokenFactory.Settings.Issuer,
            ["JwtSettings:Audience"] = TestAccessTokenFactory.Settings.Audience,
            ["JwtSettings:SigningKey"] = TestAccessTokenFactory.Settings.SigningKey,
        });

        // AddSecurity() registers SecurityDbContext against a NpgsqlDataSource, and
        // UseSecurityInfrastructureAsync() seeds roles/permission claims via RoleManager on
        // startup - both require a real Postgres connection, hence the Testcontainers-backed fixture.
        builder.Services.AddSingleton(new NpgsqlDataSourceBuilder(fixture.ConnectionString).Build());

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<DeleteTournamentCommand, Deleted>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<DeleteTournamentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Deleted);
        builder.Services.AddSingleton(commandHandlerMock.Object);

        builder.Services
            .AddFastEndpoints(options =>
            {
                options.Assemblies = [typeof(DeleteTournamentEndpoint).Assembly];
                options.Filter = type => type == typeof(DeleteTournamentEndpoint);
            })
            .AddVersioning();

        builder.AddSecurity();

        _app = builder.Build();
        await _app.UseSecurityInfrastructureAsync();

        // See DeleteArticleEndpointAuthorizationTests for why UsePropertyNamingPolicy is disabled
        // here: FastEndpoints' ValidatorOptions.Global mutation otherwise leaks into every other
        // validator test for the rest of the shared-process test run.
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

        // Deliberately not disposing _app: UseFastEndpoints() points FastEndpoints' process-wide
        // static service resolver at this host's IServiceProvider. Disposing it here would leave
        // that global resolver pointing at a disposed provider for the rest of the test run,
        // breaking every unrelated Factory.Create<TEndpoint>() call (unit tests in other classes)
        // that happens to execute afterward with an ObjectDisposedException.
    }

    private async Task<HttpResponseMessage> SendDeleteAsync(string? accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/tournaments/{ValidTournamentId}");

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await _client.SendAsync(request, ct);
    }

    [Fact(DisplayName = "DELETE /tournaments/{id} returns 401 when no access token is provided")]
    public async Task Delete_ShouldReturn401_WhenNoAccessTokenIsProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await SendDeleteAsync(accessToken: null, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "DELETE /tournaments/{id} returns 403 when the token lacks the Tournaments.DeleteTournament permission")]
    public async Task Delete_ShouldReturn403_WhenTokenLacksDeleteTournamentPermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var token = TestAccessTokenFactory.Create(permissions: [Permissions.CreateTournament]);

        // Act
        using var response = await SendDeleteAsync(token, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "DELETE /tournaments/{id} returns 403 when the token has roles but no permission claims")]
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

    [Fact(DisplayName = "DELETE /tournaments/{id} returns 204 when the token has the Tournaments.DeleteTournament permission")]
    public async Task Delete_ShouldReturn204_WhenTokenHasDeleteTournamentPermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var token = TestAccessTokenFactory.Create(permissions: [Permissions.DeleteTournament]);

        // Act
        using var response = await SendDeleteAsync(token, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}