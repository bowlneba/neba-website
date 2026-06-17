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

            var mailpitConnectionString = builder.Configuration.GetConnectionString("mailpit");

            if (mailpitConnectionString is not null)
            {
                var endpoint = new Uri(mailpitConnectionString.Replace("Endpoint=", string.Empty, StringComparison.CurrentCulture));
                builder.Services.PostConfigure<EmailSettings>(settings =>
                {
                    settings.Host = endpoint.Host;
                    settings.Port = endpoint.Port;
                    settings.UserName = string.Empty;
                    settings.AppPassword = string.Empty;
                    settings.TlsMode = MailKit.Security.SecureSocketOptions.None;
                });
            }
        }
    }
}