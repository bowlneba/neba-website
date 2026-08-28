using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Security.Authorization;

namespace Neba.Website.Server.Account;

internal static class AccountConfiguration
{
    // Shared with Neba.Api's SecurityConfiguration (SharedAuthCookieName) — same cookie name, and
    // (per SharedDataProtectionApplicationName below) the same Data Protection application name —
    // so a Webmaster's cookie-auth ticket can be decrypted by the API, letting them navigate
    // straight to /background-jobs without a separate token. Keep both in sync if either changes.
    private const string SharedAuthCookieName = ".Neba.Auth";

    // The parent domain both apps sit under in production (bowlneba.com / api.bowlneba.com — see
    // JwtSettings in Neba.Api/appsettings.json). Only applied outside local dev, where each app
    // instead runs on its own Aspire-assigned localhost port with no shared parent domain — setting
    // a Domain the browser can't match to the actual host would silently drop the cookie entirely.
    private const string SharedAuthCookieDomain = ".bowlneba.com";

    extension(IServiceCollection services)
    {
        public void AddAccountServices(IConfiguration configuration, IHostEnvironment environment)
        {
            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/account/login";
                    options.LogoutPath = "/account/logout";
                    options.AccessDeniedPath = "/account/access-denied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                    options.Cookie.Name = SharedAuthCookieName;
                    options.Cookie.HttpOnly = true;
                    // SameAsRequest (not Always) — local dev runs over plain HTTP, and CookieSecurePolicy.Always
                    // causes the browser to silently drop the auth cookie on an insecure connection.
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    // Lax (not Strict) — Strict drops the cookie on top-level navigations arriving
                    // from outside the site (e.g. an email link), forcing an extra login. CSRF
                    // protection for state-changing requests comes from UseAntiforgery(), not SameSite.
                    options.Cookie.SameSite = SameSiteMode.Lax;

                    if (environment.IsProduction())
                    {
                        options.Cookie.Domain = SharedAuthCookieDomain;
                    }
                });

            services
                .AddAuthorizationBuilder()
                    .AddNebaPolicies();

            services
                .AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>()
                .AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

            services.AddCascadingAuthenticationState();

            // Used only by the DEBUG-only "Log in as Admin" prefill on Login.razor.
            services.AddOptions<AdminLoginSettings>()
                .Bind(configuration.GetSection("Admin"));

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<AdminLoginSettings>>().Value);
        }
    }
}