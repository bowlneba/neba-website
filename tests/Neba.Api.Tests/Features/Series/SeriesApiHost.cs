using System.Net.Http.Headers;
using System.Text.Json;

using FastEndpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Neba.Api.BackgroundJobs;
using Neba.Api.Contracts.Security;
using Neba.Api.ErrorHandling;
using Neba.Api.Features.Series;
using Neba.Api.Messaging;
using Neba.Api.Security;
using Neba.Api.Storage;
using Neba.Api.Uploads;
using Neba.Api.Versioning;
using Neba.TestFactory.Infrastructure;
using Neba.TestFactory.Storage;

using Npgsql;

using SecurityRoles = Neba.Api.Security.Domain.Roles;

namespace Neba.Api.Tests.Features.Series;

/// <summary>
/// A minimal real host with the Series endpoints, the real handlers, a real AppDbContext and the real JWT
/// pipeline, so tests see the actual 401, 403, 422 and 409 behavior.
/// </summary>
internal sealed class SeriesApiHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    public HttpClient Client { get; }

    public Mock<IBackgroundJobScheduler> Scheduler { get; }

    private SeriesApiHost(WebApplication app, HttpClient client, Mock<IBackgroundJobScheduler> scheduler)
    {
        _app = app;
        Client = client;
        Scheduler = scheduler;
    }

    public static string ManagerToken()
        => TestAccessTokenFactory.Create(
            roles: [SecurityRoles.Manager],
            permissions: [Permissions.CreateSeries, Permissions.EditSeries, Permissions.DeleteSeries, Permissions.EditSeriesLogo]);

    public static string AdminToken()
        => TestAccessTokenFactory.Create(roles: [SecurityRoles.Admin], permissions: [.. Permissions.List]);

    public static string MemberToken()
        => TestAccessTokenFactory.Create(roles: [SecurityRoles.Member]);

    public static async Task<SeriesApiHost> StartAsync(
        AppDbContextFixture appFixture, SecurityDbContextFixture securityFixture, Mock<IKeyedLockProvider>? keyedLocks = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://127.0.0.1:0");

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = TestAccessTokenFactory.Settings.Issuer,
            ["JwtSettings:Audience"] = TestAccessTokenFactory.Settings.Audience,
            ["JwtSettings:SigningKey"] = TestAccessTokenFactory.Settings.SigningKey,
            ["WebsiteSettings:BaseUrl"] = "https://bowlneba.com",
        });

        builder.Services.AddSingleton(new NpgsqlDataSourceBuilder(securityFixture.ConnectionString).Build());
        builder.Services.AddScoped(_ => appFixture.CreateDbContext());
        builder.Services.AddSingleton(TimeProvider.System);

        var storage = new Mock<IFileStorageService>(MockBehavior.Strict);
        storage.Setup(s => s.GetBlobUri(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string container, string path) => new Uri($"https://storage.example.com/{container}/{path}"));
        builder.Services.AddSingleton(storage.Object);

        var staging = new Mock<IUploadStagingService>(MockBehavior.Strict);
        staging.Setup(s => s.StageUploadAsync(It.IsAny<IFormFile>(), "bowlneba-public", "series/logo", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StoredFileFactory.Create(container: "bowlneba-public", path: "series/logo/staged.png", contentType: "image/png"));
        staging.Setup(s => s.StageUploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), "bowlneba-public", "series/logo", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(StoredFileFactory.Create(container: "bowlneba-public", path: "series/logo/staged.png", contentType: "image/png"));
        builder.Services.AddSingleton(staging.Object);

        var resizer = new Mock<IImageResizer>(MockBehavior.Strict);
        resizer.Setup(r => r.ResizeLogoAsync(It.IsAny<Stream>(), It.IsAny<int>(), It.IsAny<IReadOnlySet<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResizedImage { Content = [1, 2, 3], ContentType = "image/png", FileExtension = "png" });
        builder.Services.AddSingleton(resizer.Object);

        // The only job the Series endpoints enqueue is the delete of a replaced or removed logo.
        var scheduler = new Mock<IBackgroundJobScheduler>(MockBehavior.Strict);
        scheduler.Setup(s => s.Enqueue(It.IsAny<DeleteStoredFilesJob>())).Returns("job-id");
        builder.Services.AddSingleton(scheduler.Object);

        // The advancement write endpoints run through SeriesAdvancement. The lock is a grant-anything double unless a test
        // passes its own: the real provider needs Hangfire storage, which has no Postgres test pattern here.
        builder.Services.AddSingleton((keyedLocks ?? KeyedLockDoubles.GrantingAny().Provider).Object);
        builder.Services.AddScoped<SeriesAdvancement>();
        builder.Services.AddScoped<ISeriesAdvancementFactsReader, SeriesAdvancementFactsReader>();

        MessagingConfiguration.AddMessaging(builder.Services);

        builder.Services
            .AddFastEndpoints(options =>
            {
                options.Assemblies = [typeof(SeriesEndpointGroup).Assembly];
                options.Filter = type => type.Namespace?.StartsWith("Neba.Api.Features.Series", StringComparison.Ordinal) == true;
            })
            .AddVersioning();

        builder.Services.AddFusionCache();
        builder.AddSecurity();

        var app = builder.Build();
        await app.UseSecurityInfrastructureAsync();

        // UsePropertyNamingPolicy is off for the same reason as the other endpoint tests: the setting is
        // process-wide static state that would otherwise leak into unrelated validator tests.
        app.UseFastEndpoints(config =>
        {
            config.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            config.Binding.UsePropertyNamingPolicy = false;
            config.Validation.UsePropertyNamingPolicy = false;

            config.Errors.ConfigureErrorHandling();
        });

        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        var client = new HttpClient { BaseAddress = new Uri(address) };

        return new SeriesApiHost(app, client, scheduler);
    }

    public static HttpRequestMessage Request(HttpMethod method, string url, string? token, object? body = null)
    {
        var request = new HttpRequestMessage(method, new Uri(url, UriKind.Relative));

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = System.Net.Http.Json.JsonContent.Create(body);
        }

        return request;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();

        // Deliberately not disposing _app: UseFastEndpoints() points FastEndpoints' process-wide static
        // service resolver at this host's provider (see UploadTournamentLogoEndpointAuthorizationTests).
    }
}