using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

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
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                });

            services.AddCascadingAuthenticationState();

            // Used only by the DEBUG-only "Log in as Admin" prefill on Login.razor.
            services.AddOptions<AdminLoginSettings>()
                .Bind(configuration.GetSection("Admin"));

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<AdminLoginSettings>>().Value);
        }
    }
}