using Microsoft.AspNetCore.Authentication.Cookies;

namespace Neba.Website.Server.Account;

internal static class AccountConfiguration
{
    extension(IServiceCollection services)
    {
        public void AddAccountServices()
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
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                });

            services.AddCascadingAuthenticationState();
        }
    }
}