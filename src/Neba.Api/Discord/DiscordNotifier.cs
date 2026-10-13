namespace Neba.Api.Discord;

internal sealed class DiscordNotifier(HttpClient httpClient, TimeProvider timeProvider, ILogger<DiscordNotifier> logger)
    : IDiscordNotifier
{
    public async Task NotifyAsync(DiscordAlert alert, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await DiscordWebhook.PostAsync(httpClient, timeProvider, alert, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogDiscordPostRejected(alert.Title, (int)response.StatusCode);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogDiscordPostFailed(ex, alert.Title);
        }
    }
}

internal static partial class DiscordNotifierLogMessages
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Discord webhook rejected alert '{Title}' with status {StatusCode}.")]
    public static partial void LogDiscordPostRejected(this ILogger<DiscordNotifier> logger, string title, int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to post Discord alert '{Title}'.")]
    public static partial void LogDiscordPostFailed(this ILogger<DiscordNotifier> logger, Exception exception, string title);
}