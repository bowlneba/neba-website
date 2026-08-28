using System.Text;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Security.Authorization;
using Neba.Api.Database;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Infrastructure;

using Npgsql;

namespace Neba.Api.Security;

internal static class SecurityConfiguration
{
    // Used by endpoints that require any authenticated user (no specific role) — see
    // LogoutEndpoint and GetCurrentUserEndpoint, which call Policies(AuthenticatedPolicy).
    public const string AuthenticatedPolicy = "Authenticated";

    // The Hangfire dashboard is a browser-navigated page, not an API call — see the two auth
    // fallbacks wired below (query-string token, cookie scheme) that exist only to make it usable
    // in a browser.
    private const string BackgroundJobsDashboardPath = "/background-jobs";
    private const string AccessTokenQueryParameterName = "access_token";

    // Combines JWT bearer (used for normal API calls) and cookie auth (recognizes a Webmaster
    // already signed in to the website, see Neba.Website.Server's AddAccountServices) behind one
    // default scheme, since the Hangfire dashboard filter just reads HttpContext.User.
    private const string CombinedAuthenticationScheme = "JwtOrCookie";

    // Shared with Neba.Website.Server's AddAccountServices — same cookie name (and, per
    // StorageConfiguration.DataProtectionApplicationName, the same Data Protection application
    // name) so a ticket the website encrypts can be decrypted here. Keep both in sync if either
    // changes.
    internal const string SharedAuthCookieName = ".Neba.Auth";

    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddSecurity()
        {
            builder.Services.AddDbContext<SecurityDbContext>((sp, options) =>
            {
                var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
                options
                    .UseNpgsql(dataSource, npgsql => npgsql
                        .MigrationsHistoryTable(
                            SecurityDbContext.MigrationsHistoryTableName,
                            SecurityDbContext.Schema
                        )
                        .EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null))
                    .UseSnakeCaseNamingConvention();
            });

            builder.Services
                .AddIdentity<ApplicationUser, ApplicationRole>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireNonAlphanumeric = false;
                    options.SignIn.RequireConfirmedEmail = true;
                    options.User.RequireUniqueEmail = true;
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                })
                .AddEntityFrameworkStores<SecurityDbContext>()
                .AddDefaultTokenProviders();

            var jwtSettings = builder.Configuration
                .GetSection("JwtSettings")
                .Get<JwtSettings>()
                ?? throw new InvalidOperationException("JwtSettings configuration section is missing.");

            if (string.IsNullOrWhiteSpace(jwtSettings.SigningKey))
                throw new InvalidOperationException("JwtSettings:SigningKey must not be empty.");

            if (Encoding.UTF8.GetByteCount(jwtSettings.SigningKey) < 32)
                throw new InvalidOperationException("JwtSettings:SigningKey must be at least 32 bytes (256 bits) for HMAC-SHA256.");

            builder.Services.AddSingleton(jwtSettings);
            builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

            var websiteSettings = builder.Configuration
                .GetSection("WebsiteSettings")
                .Get<WebsiteSettings>()
                ?? throw new InvalidOperationException("WebsiteSettings configuration section is missing.");

            if (string.IsNullOrWhiteSpace(websiteSettings.BaseUrl))
                throw new InvalidOperationException("WebsiteSettings:BaseUrl must not be empty.");

            builder.Services.AddSingleton(websiteSettings);

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CombinedAuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddPolicyScheme(CombinedAuthenticationScheme, "JWT or Cookie", options =>
                {
                    // Every normal API call carries either an Authorization header or the
                    // query-string fallback above; a plain browser navigation to the dashboard
                    // with neither present falls back to the website's auth cookie, if any.
                    options.ForwardDefaultSelector = context =>
                        context.Request.Headers.ContainsKey("Authorization")
                            || context.Request.Query.ContainsKey(AccessTokenQueryParameterName)
                                ? JwtBearerDefaults.AuthenticationScheme
                                : CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    // Same scheme name, cookie name, and Data Protection application name
                    // (StorageConfiguration.DataProtectionApplicationName) as Neba.Website.Server's
                    // AddAccountServices, so a ticket the website encrypts can be decrypted here —
                    // this scheme is never challenged directly (see ForwardDefaultSelector above),
                    // it only ever reads a cookie the website already issued.
                    options.Cookie.Name = SharedAuthCookieName;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwtSettings.SigningKey))
                    };

                    // The Hangfire dashboard (/background-jobs) is a plain browser-navigated page,
                    // not an API call the website's Refit clients attach a bearer token to, so it
                    // has no other way to authenticate a linked-to visit. Scoped narrowly to that
                    // one path so token-in-URL isn't accepted anywhere else in the API (query
                    // strings end up in server logs/browser history, unlike an Authorization header).
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (context.Request.Path.StartsWithSegments(BackgroundJobsDashboardPath, StringComparison.Ordinal))
                            {
                                var token = context.Request.Query[AccessTokenQueryParameterName];
                                if (!string.IsNullOrEmpty(token))
                                {
                                    context.Token = token;
                                }
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services
                .AddAuthorizationBuilder()
                .AddPolicy(AuthenticatedPolicy, policy => policy.RequireAuthenticatedUser())
                .AddNebaPolicies();

            builder.Services
                .AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>()
                .AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public async Task<WebApplication> UseSecurityInfrastructureAsync()
        {
            app.UseAuthentication();
            app.UseAuthorization();

            // Keeps each role's permission claims (AspNetRoleClaims) in sync with the RolePermissions
            // mapping in SecurityRoleSeeder, so adding a new Permissions value takes effect for existing
            // roles/users on the next app restart without a manual DB edit.
            await using (var seedScope = app.Services.CreateAsyncScope())
            {
                var roleManager = seedScope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                var seedLogger = seedScope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Neba.Api.Security.Infrastructure.SecurityRoleSeeder");
                await SecurityRoleSeeder.SeedAsync(roleManager, seedLogger);
            }

            return app;
        }
    }
}