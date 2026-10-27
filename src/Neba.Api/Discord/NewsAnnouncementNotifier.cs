using System.Text.Json.Serialization;

namespace Neba.Api.Discord;

internal sealed class NewsAnnouncementNotifier(HttpClient httpClient)
    : INewsAnnouncementNotifier
{
    public async Task NotifyAsync(NewsAnnouncement announcement, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(announcement);

        var embed = new DiscordNewsEmbed(
            announcement.Title,
            announcement.ArticleUrl.AbsoluteUri,
            announcement.ImageUrl is null ? null : new DiscordNewsEmbedImage(announcement.ImageUrl.AbsoluteUri));

        using var response = await httpClient.PostAsJsonAsync(string.Empty, new { embeds = new[] { embed } }, cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}

internal sealed record DiscordNewsEmbed(
    string Title,
    string Url,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DiscordNewsEmbedImage? Image);

internal sealed record DiscordNewsEmbedImage(string Url);
