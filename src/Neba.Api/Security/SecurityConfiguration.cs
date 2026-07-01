using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Neba.Api.Database;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Infrastructure.Authorization;

using Npgsql;

namespace Neba.Api.Security;

internal static class SecurityConfiguration
{
    // Used by endpoints that require any authenticated user (no specific role) — see
    // LogoutEndpoint and GetCurrentUserEndpoint, which call Policies(AuthenticatedPolicy).
    public const string AuthenticatedPolicy = "Authenticated";

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
                        ))
                    .UseSnakeCaseNamingConvention();
            });

            builder.Services
                .AddIdentity<ApplicationUser, ApplicationRole>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireNonAlphanumeric = false;
                    // Email confirmation is configured but intentionally bypassed for now:
                    // RegisterCommandHandler sets EmailConfirmed on creation so admin-created accounts
                    // are immediately active. Remove that bypass when the self-registration flow ships.
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

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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
                });

            builder.Services
                .AddAuthorizationBuilder()
                .AddPolicy(AuthenticatedPolicy, policy => policy.RequireAuthenticatedUser());

            builder.Services
                .AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>()
                .AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public WebApplication UseSecurityInfrastructure()
        {
            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }
    }
}