namespace Neba.Api.Discord;

/// <summary>
/// Posts to the manual-intervention webhook. Unlike <see cref="DiscordNotifier"/> it throws when the
/// post fails, so Hangfire retries the job and the failure filter reports it - an alert a person must
/// act on cannot be dropped silently.
/// </summary>
internal sealed class ManualInterventionNotifier(
    HttpClient httpClient,
    TimeProvider timeProvider,
    IDiscordNotifier apiLogNotifier)
    : IManualInterventionNotifier
{
    public async Task NotifyAsync(DiscordAlert alert, CancellationToken cancellationToken)
    {
        using var response = await DiscordWebhook.PostAsync(httpClient, timeProvider, alert, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Critical alerts drop a whole legacy event, so they also reach the channel on-call watches.
        if (alert.Severity == DiscordAlertSeverity.Critical)
        {
            await apiLogNotifier.NotifyAsync(alert, cancellationToken);
        }
    }
}