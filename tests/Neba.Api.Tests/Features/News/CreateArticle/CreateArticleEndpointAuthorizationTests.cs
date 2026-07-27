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

using Neba.Api.Contracts.News.CreateArticle;
using Neba.Api.Contracts.Security;
using Neba.Api.Features.News.CreateArticle;
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.News.GetArticle;
using Neba.Api.Security;
using Neba.Api.Versioning;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.News;

using Npgsql;

using NebaMessaging = Neba.Api.Messaging;
using SecurityRoles = Neba.Api.Security.Domain.Roles;

namespace Neba.Api.Tests.Features.News.CreateArticle;

[IntegrationTest]
[Component("News")]
[SuppressMessage("Design", "CA2213:Disposable fields should be disposed", Justification = "_app is intentionally never disposed - see DisposeAsync.")]
[Collection<SecurityDbContextFixture>]
public sealed class CreateArticleEndpointAuthorizationTests(SecurityDbContextFixture fixture)
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
            ["WebsiteSettings:BaseUrl"] = "https://bowlneba.com",
        });

        // AddSecurity() registers SecurityDbContext against a NpgsqlDataSource, and
        // UseSecurityInfrastructureAsync() seeds roles/permission claims via RoleManager on
        // startup - both require a real Postgres connection, hence the Testcontainers-backed fixture.
        builder.Services.AddSingleton(new NpgsqlDataSourceBuilder(fixture.ConnectionString).Build());

        var createCommandHandlerMock = new Mock<NebaMessaging.ICommandHandler<CreateArticleCommand, CreatedArticle>>(MockBehavior.Strict);
        createCommandHandlerMock
            .Setup(h => h.HandleAsync(It.IsAny<CreateArticleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatedArticleFactory.Create());
        builder.Services.AddSingleton(createCommandHandlerMock.Object);

        // GetArticleEndpoint is registered alongside CreateArticleEndpoint purely so
        // Send.CreatedAtAsync("GetArticle", ...) can resolve that route via LinkGenerator on the
        // success path - its query handler is never actually invoked by these tests.
        var getArticleQueryHandlerMock = new Mock<NebaMessaging.IQueryHandler<GetArticleQuery, ErrorOr<ArticleDetailDto>>>(MockBehavior.Strict);
        builder.Services.AddSingleton(getArticleQueryHandlerMock.Object);

        builder.Services
            .AddFastEndpoints(options =>
            {
                options.Assemblies = [typeof(CreateArticleEndpoint).Assembly];
                options.Filter = type => type == typeof(CreateArticleEndpoint) || type == typeof(GetArticleEndpoint);
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
        using var request = new HttpRequestMessage(HttpMethod.Post, "/news")
        {
            Content = JsonContent.Create(new CreateArticleRequest { Article = ArticleInputFactory.Create() })
        };

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await _client.SendAsync(request, ct);
    }

    [Fact(DisplayName = "POST /news returns 401 when no access token is provided")]
    public async Task Create_ShouldReturn401_WhenNoAccessTokenIsProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await SendCreateAsync(accessToken: null, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /news returns 403 when the token lacks the News.CreateArticle permission")]
    public async Task Create_ShouldReturn403_WhenTokenLacksCreateArticlePermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var token = TestAccessTokenFactory.Create(permissions: [Permissions.EditArticle]);

        // Act
        using var response = await SendCreateAsync(token, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "POST /news returns 403 when the token has roles but no permission claims")]
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

    [Fact(DisplayName = "POST /news returns 201 when the token has the News.CreateArticle permission")]
    public async Task Create_ShouldReturn201_WhenTokenHasCreateArticlePermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var token = TestAccessTokenFactory.Create(permissions: [Permissions.CreateArticle]);

        // Act
        using var response = await SendCreateAsync(token, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}