namespace Neba.Api.Discord;

internal sealed class DiscordNotifier(HttpClient httpClient, TimeProvider timeProvider, ILogger<DiscordNotifier> logger)
    : IDiscordNotifier
{
    public async Task NotifyAsync(DiscordAlert alert, CancellationToken cancellationToken)
    {
        var embed = new DiscordEmbed(
            alert.Title,
            alert.Body,
            alert.Severity.NotificationColor.RawValue,
            timeProvider.GetUtcNow(),
            alert.Metadata?.Select(field => new DiscordEmbedField(field.Key, field.Value)).ToList());

        var payload = new { embeds = new[] { embed } };

        try
        {
            using var response = await httpClient.PostAsJsonAsync(string.Empty, payload, cancellationToken);

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
