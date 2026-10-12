using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using ErrorOr;

using FastEndpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Security.Login;
using Neba.Api.Security;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Login;
using Neba.Api.Security.UpdateUser;
using Neba.Api.Versioning;
using Neba.TestFactory.Infrastructure;

using Npgsql;

using NebaMessaging = Neba.Api.Messaging;

namespace Neba.Api.Tests.Security;

/// <summary>
/// A real host (JWT auth, permission policies, Identity on the Testcontainers Postgres, seeded roles)
/// serving the security endpoints that permission overrides touch: <c>PUT /security/users/{id}</c> and
/// <c>POST /security/login</c>. Handlers are the production ones, not mocks.
/// </summary>
internal sealed class SecurityApiHost
{
    private static readonly Type[] HostedTypes =
    [
        typeof(UpdateUserEndpoint),
        typeof(UpdateUserRequestValidator),
        typeof(LoginEndpoint),
        typeof(LoginRequestValidator)
    ];

    private SecurityApiHost(WebApplication app, HttpClient client)
    {
        App = app;
        Client = client;
    }

    public WebApplication App { get; }

    public HttpClient Client { get; }

    public static async Task<SecurityApiHost> StartAsync(
        string connectionString, Action<IServiceCollection>? configureServices = null)
    {
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

        builder.Services.AddSingleton(new NpgsqlDataSourceBuilder(connectionString).Build());
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<NebaMessaging.ICommandHandler<UpdateUserCommand, Updated>, UpdateUserCommandHandler>();
        builder.Services.AddScoped<NebaMessaging.ICommandHandler<LoginCommand, LoginDto>, LoginCommandHandler>();

        configureServices?.Invoke(builder.Services);

        builder.Services
            .AddFastEndpoints(options =>
            {
                options.Assemblies = [typeof(UpdateUserEndpoint).Assembly];
                options.Filter = type => HostedTypes.Contains(type);
            })
            .AddVersioning();

        builder.Services.AddFusionCache();
        builder.AddSecurity();

        var app = builder.Build();
        await app.UseSecurityInfrastructureAsync();

        // See DeleteArticleEndpointAuthorizationTests for why UsePropertyNamingPolicy is disabled here.
        app.UseFastEndpoints(c => c.Validation.UsePropertyNamingPolicy = false);

        await app.StartAsync();

        var address = app.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses
            .First();

        return new SecurityApiHost(app, new HttpClient { BaseAddress = new Uri(address) });
    }

    /// <summary>Stops the host. Deliberately not disposed - see DeleteArticleEndpointAuthorizationTests.</summary>
    public Task StopAsync()
    {
        Client.Dispose();
        return App.StopAsync();
    }

    public async Task<HttpResponseMessage> PutOverridesAsync(
        string accessToken,
        Ulid targetUserId,
        IReadOnlyCollection<string> added,
        IReadOnlyCollection<string> removed,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/security/users/{targetUserId}")
        {
            // UserId is a required property of the request, so the body carries it as well as the route.
            Content = JsonContent.Create(new
            {
                userId = targetUserId.ToString(),
                user = new { addedPermissions = added, removedPermissions = removed }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await Client.SendAsync(request, ct);
    }

    public async Task<IReadOnlyCollection<string>> LoginPermissionsAsync(string email, string password, CancellationToken ct)
    {
        using var response = await Client.PostAsJsonAsync("/security/login", new { email, password }, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(ct)
            ?? throw new InvalidOperationException("Login returned an empty body.");
        var token = new JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken);

        return [.. token.Claims.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value)];
    }

    /// <summary>The permission values the seeded <paramref name="roleName"/> role grants, as the real seeder wrote them.</summary>
    public async Task<IReadOnlyCollection<Permissions>> SeededRolePermissionsAsync(string roleName)
    {
        await using var scope = App.Services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<ApplicationRole>>();

        var role = await roleManager.FindByNameAsync(roleName)
            ?? throw new InvalidOperationException($"Role '{roleName}' was not seeded.");
        var claims = await roleManager.GetClaimsAsync(role);

        return [.. claims.Where(c => c.Type == Permissions.ClaimType).Select(c => Permissions.FromValue(c.Value))];
    }
}