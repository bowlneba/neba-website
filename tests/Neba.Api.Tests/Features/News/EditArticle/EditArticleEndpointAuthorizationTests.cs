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
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.News.EditArticle;
using Neba.Api.Security;
using Neba.Api.Versioning;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.News;

using Npgsql;

using NebaMessaging = Neba.Api.Messaging;
using SecurityRoles = Neba.Api.Security.Domain.Roles;
using WireEditArticleRequest = Neba.Api.Contracts.News.EditArticle.EditArticleRequest;

namespace Neba.Api.Tests.Features.News.EditArticle;

[IntegrationTest]
[Component("News")]
[SuppressMessage("Design", "CA2213:Disposable fields should be disposed", Justification = "_app is intentionally never disposed - see DisposeAsync.")]
[Collection<SecurityDbContextFixture>]
public sealed class EditArticleEndpointAuthorizationTests(SecurityDbContextFixture fixture)
    : IClassFixture<SecurityDbContextFixture>, IAsyncLifetime
{
    private const string ValidArticleId = "01000000000000000000000001";

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
            ["WebsiteSettings:BaseUrl"] = "https://bowlneba.com",
        });

        // AddSecurity() registers SecurityDbContext against a NpgsqlDataSource, and
        // UseSecurityInfrastructureAsync() seeds roles/permission claims via RoleManager on
        // startup - both require a real Postgres connection, hence the Testcontainers-backed fixture.
        builder.Services.AddSingleton(new NpgsqlDataSourceBuilder(fixture.ConnectionString).Build());

        var commandHandlerMock = new Mock<NebaMessaging.ICommandHandler<EditArticleCommand, Updated>>(MockBehavior.Strict);
        commandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<EditArticleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Updated);
        builder.Services.AddSingleton(commandHandlerMock.Object);

        builder.Services
            .AddFastEndpoints(options =>
            {
                options.Assemblies = [typeof(EditArticleEndpoint).Assembly];
                options.Filter = type => type == typeof(EditArticleEndpoint);
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

    private async Task<HttpResponseMessage> SendEditAsync(string? accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/news/{ValidArticleId}")
        {
            Content = JsonContent.Create(new WireEditArticleRequest { Id = ValidArticleId, Article = EditArticleInputFactory.Create() })
        };

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await _client.SendAsync(request, ct);
    }

    [Fact(DisplayName = "PUT /news/{id} returns 401 when no access token is provided")]
    public async Task Edit_ShouldReturn401_WhenNoAccessTokenIsProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await SendEditAsync(accessToken: null, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "PUT /news/{id} returns 403 when the token lacks the News.EditArticle permission")]
    public async Task Edit_ShouldReturn403_WhenTokenLacksEditArticlePermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var token = TestAccessTokenFactory.Create(permissions: [Permissions.CreateArticle]);

        // Act
        using var response = await SendEditAsync(token, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "PUT /news/{id} returns 403 when the token has roles but no permission claims")]
    public async Task Edit_ShouldReturn403_WhenTokenHasRolesButNoPermissionClaims()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var token = TestAccessTokenFactory.Create(roles: [SecurityRoles.Admin]);

        // Act
        using var response = await SendEditAsync(token, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "PUT /news/{id} returns 204 when the token has the News.EditArticle permission")]
    public async Task Edit_ShouldReturn204_WhenTokenHasEditArticlePermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var token = TestAccessTokenFactory.Create(permissions: [Permissions.EditArticle]);

        // Act
        using var response = await SendEditAsync(token, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}