using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using ErrorOr;

using FastEndpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Tournaments.CreateTournament;
using Neba.Api.Contracts.Tournaments.GetTournament;
using Neba.Api.Features.Tournaments.CreateTournament;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.GetTournament;
using Neba.Api.Security;
using Neba.Api.Versioning;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Tournaments;

using Npgsql;

using NebaMessaging = Neba.Api.Messaging;
using SecurityRoles = Neba.Api.Security.Domain.Roles;

namespace Neba.Api.Tests.Features.Tournaments.CreateTournament;

[IntegrationTest]
[Component("Tournaments")]
[SuppressMessage("Design", "CA2213:Disposable fields should be disposed", Justification = "_app is intentionally never disposed - see DisposeAsync.")]
[Collection<SecurityDbContextFixture>]
public sealed class CreateTournamentEndpointAuthorizationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
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

        var createCommandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateTournamentCommand, TournamentId>>(MockBehavior.Strict);
        createCommandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateTournamentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TournamentId.New());
        builder.Services.AddSingleton(createCommandHandlerMock.Object);

        // GetTournamentEndpoint is registered alongside CreateTournamentEndpoint purely so
        // Send.CreatedAtAsync("GetTournament", ...) can resolve that route via LinkGenerator on the
        // success path - its query handler is never actually invoked by these tests.
        var getTournamentQueryHandlerMock = new Mock<NebaMessaging.IQueryHandler<GetTournamentQuery, ErrorOr<TournamentDetailDto>>>(MockBehavior.Strict);
        builder.Services.AddSingleton(getTournamentQueryHandlerMock.Object);

        builder.Services
            .AddFastEndpoints(options =>
            {
                options.Assemblies = [typeof(CreateTournamentEndpoint).Assembly];
                options.Filter = type => type == typeof(CreateTournamentEndpoint) || type == typeof(GetTournamentEndpoint);
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

    private async Task<HttpResponseMessage> SendCreateAsync(string? accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/tournaments")
        {
            Content = JsonContent.Create(new CreateTournamentRequest { Tournament = TournamentInputFactory.Create() })
        };

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await _client.SendAsync(request, ct);
    }

    [Fact(DisplayName = "POST /tournaments returns 401 when no access token is provided")]
    public async Task Create_ShouldReturn401_WhenNoAccessTokenIsProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await SendCreateAsync(accessToken: null, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /tournaments returns 403 when the token lacks the Tournaments.CreateTournament permission")]
    public async Task Create_ShouldReturn403_WhenTokenLacksCreateTournamentPermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var token = TestAccessTokenFactory.Create(permissions: [Permissions.CreateArticle]);

        // Act
        using var response = await SendCreateAsync(token, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "POST /tournaments returns 403 when the token has roles but no permission claims")]
    public async Task Create_ShouldReturn403_WhenTokenHasRolesButNoPermissionClaims()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var token = TestAccessTokenFactory.Create(roles: [SecurityRoles.Admin]);

        // Act
        using var response = await SendCreateAsync(token, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "POST /tournaments returns 201 when the token has the Tournaments.CreateTournament permission")]
    public async Task Create_ShouldReturn201_WhenTokenHasCreateTournamentPermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var token = TestAccessTokenFactory.Create(permissions: [Permissions.CreateTournament]);

        // Act
        using var response = await SendCreateAsync(token, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}
