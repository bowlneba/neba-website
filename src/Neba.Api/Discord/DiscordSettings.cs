using System.ComponentModel.DataAnnotations;

namespace Neba.Api.Discord;

internal sealed class DiscordSettings
{
    public const string SectionName = "Discord";

    [Required]
    public string WebhookUrl { get; init; } = string.Empty;
}