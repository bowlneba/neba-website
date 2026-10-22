namespace Neba.Api.Discord;

internal static class DiscordWebhook
{
    public static Task<HttpResponseMessage> PostAsync(
        HttpClient httpClient,
        TimeProvider timeProvider,
        DiscordAlert alert,
        CancellationToken cancellationToken)
    {
        var embed = new DiscordEmbed(
            alert.Title,
            alert.Body,
            alert.Severity.NotificationColor.RawValue,
            timeProvider.GetUtcNow(),
            alert.Metadata?.Select(field => new DiscordEmbedField(field.Key, field.Value)).ToList());

        var payload = new { embeds = new[] { embed } };

        return httpClient.PostAsJsonAsync(string.Empty, payload, cancellationToken);
    }
}