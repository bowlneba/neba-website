using Microsoft.Extensions.Http.Resilience;
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
                    client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<DiscordSettings>>().Value.ApiLogsWebhookUrl))
                .AddStandardResilienceHandler(ConfigureResilience);

            builder.Services.AddHttpClient<IManualInterventionNotifier, ManualInterventionNotifier>((sp, client) =>
                    client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<DiscordSettings>>().Value.ManualInterventionWebhookUrl))
                .AddStandardResilienceHandler(ConfigureResilience);

            // No resilience handler: the standard one cannot disable retries (Polly needs
            // MaxRetryAttempts >= 1), and a retry after a timeout could post the announcement twice.
            builder.Services.AddHttpClient<INewsAnnouncementNotifier, NewsAnnouncementNotifier>((sp, client) =>
            {
                client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<DiscordSettings>>().Value.NewsWebhookUrl);
                client.Timeout = NewsAnnouncementTimeout;
            });

            return builder;
        }
    }

    private static readonly TimeSpan NewsAnnouncementTimeout = TimeSpan.FromSeconds(10);

    private static void ConfigureResilience(HttpStandardResilienceOptions options)
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.Retry.MaxRetryAttempts = 2;
    }
}