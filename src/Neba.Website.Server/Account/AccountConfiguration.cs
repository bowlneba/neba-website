using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

using Neba.Api.Contracts.Security.Authorization;

namespace Neba.Website.Server.Account;

internal static class AccountConfiguration
{
    extension(IServiceCollection services)
    {
        public void AddAccountServices(IConfiguration configuration)
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
                    options.Cookie.HttpOnly = true;
                    // SameAsRequest (not Always) — local dev runs over plain HTTP, and CookieSecurePolicy.Always
                    // causes the browser to silently drop the auth cookie on an insecure connection.
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    // Lax (not Strict) — Strict drops the cookie on top-level navigations arriving
                    // from outside the site (e.g. an email link), forcing an extra login. CSRF
                    // protection for state-changing requests comes from UseAntiforgery(), not SameSite.
                    options.Cookie.SameSite = SameSiteMode.Lax;
                });

            services.AddAuthorization();
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