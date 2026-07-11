using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;

using FastEndpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.Contracts.Security;
using Neba.Api.Features.News.UploadArticleAttachment;
using Neba.Api.Security;
using Neba.Api.Uploads;
using Neba.Api.Versioning;
using Neba.TestFactory.Attributes;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Storage;

using Npgsql;

using SecurityRoles = Neba.Api.Security.Domain.Roles;

namespace Neba.Api.Tests.Features.News.UploadArticleAttachment;

[IntegrationTest]
[Component("News")]
[SuppressMessage("Design", "CA2213:Disposable fields should be disposed", Justification = "_app is intentionally never disposed - see DisposeAsync.")]
[Collection<SecurityDbContextFixture>]
public sealed class UploadArticleAttachmentEndpointAuthorizationTests(SecurityDbContextFixture fixture)
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

        var stagingServiceMock = new Mock<IUploadStagingService>(MockBehavior.Strict);
        stagingServiceMock
            .Setup(s => s.StageUploadAsync(
                It.IsAny<IFormFile>(),
                "news",
                "attachments",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(StoredFileFactory.Create());
        builder.Services.AddSingleton(stagingServiceMock.Object);

        builder.Services
            .AddFastEndpoints(options =>
            {
                options.Assemblies = [typeof(UploadArticleAttachmentEndpoint).Assembly];
                options.Filter = type => type == typeof(UploadArticleAttachmentEndpoint);
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

    private async Task<HttpResponseMessage> SendUploadAsync(string? accessToken, CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent([1, 2, 3, 4]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "File", "bracket.pdf");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/news/attachments")
        {
            Content = content
        };

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await _client.SendAsync(request, ct);
    }

    [Fact(DisplayName = "POST /news/attachments returns 401 when no access token is provided")]
    public async Task Upload_ShouldReturn401_WhenNoAccessTokenIsProvided()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await SendUploadAsync(accessToken: null, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "POST /news/attachments returns 403 when the token lacks the News.CreateArticle permission")]
    public async Task Upload_ShouldReturn403_WhenTokenLacksCreateArticlePermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var token = TestAccessTokenFactory.Create(permissions: [Permissions.Read]);

        // Act
        using var response = await SendUploadAsync(token, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "POST /news/attachments returns 403 when the token has roles but no permission claims")]
    public async Task Upload_ShouldReturn403_WhenTokenHasRolesButNoPermissionClaims()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var token = TestAccessTokenFactory.Create(roles: [SecurityRoles.Admin]);

        // Act
        using var response = await SendUploadAsync(token, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "POST /news/attachments returns 200 when the token has the News.CreateArticle permission")]
    public async Task Upload_ShouldReturn200_WhenTokenHasCreateArticlePermission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var token = TestAccessTokenFactory.Create(permissions: [Permissions.CreateArticle]);

        // Act
        using var response = await SendUploadAsync(token, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}