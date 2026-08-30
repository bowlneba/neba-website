namespace Neba.Api.Discord;

internal sealed record DiscordEmbed(string Title, string Description, int Color, DateTimeOffset Timestamp, IReadOnlyList<DiscordEmbedField>? Fields);

internal sealed record DiscordEmbedField(string Name, object Value, bool Inline = true);
