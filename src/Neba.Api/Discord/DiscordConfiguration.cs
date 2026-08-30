using Microsoft.Extensions.Options;

namespace Neba.Api.Discord;

internal static class DiscordConfiguration
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddDiscord()
        {
            builder.Services.AddOptions<DiscordSettings>()
                .Bind(builder.Configuration.GetSection(DiscordSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddHttpClient<IDiscordNotifier, DiscordNotifier>((sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<DiscordSettings>>().Value;
                client.BaseAddress = new Uri(settings.WebhookUrl);
            })
                .AddStandardResilienceHandler(options =>
                {
                    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
                    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
                    options.Retry.MaxRetryAttempts = 2;
                });

            return builder;
        }
    }
}
