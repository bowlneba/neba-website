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
                    client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<DiscordSettings>>().Value.WebhookUrl))
                .AddStandardResilienceHandler(ConfigureResilience);

            builder.Services.AddHttpClient<IManualInterventionNotifier, DiscordNotifier>((sp, client) =>
                    client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<DiscordSettings>>().Value.ManualInterventionWebhookUrl))
                .AddStandardResilienceHandler(ConfigureResilience);

            return builder;
        }
    }

    private static void ConfigureResilience(HttpStandardResilienceOptions options)
    {
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.Retry.MaxRetryAttempts = 2;
    }
}
