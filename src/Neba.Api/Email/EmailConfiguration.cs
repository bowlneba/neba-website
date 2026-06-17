using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

using Neba.Api.Security.Domain;

namespace Neba.Api.Email;

internal static class EmailConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public void AddEmail()
        {
            builder.Services
                .Configure<EmailSettings>(options => builder.Configuration.GetSection(EmailSettings.SectionName).Bind(options))
                .AddTransient<IEmailSender, GoogleWorkspaceEmailSender>()
                .AddTransient<IEmailSender<ApplicationUser>, IdentityEmailSenderAdapter>();

            builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<EmailSettings>>().Value);
        }
    }
}