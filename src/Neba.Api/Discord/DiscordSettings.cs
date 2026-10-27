using System.ComponentModel.DataAnnotations;

namespace Neba.Api.Discord;

internal sealed class DiscordSettings
{
    public const string SectionName = "Discord";

    [Required]
    public string ApiLogsWebhookUrl { get; init; } = string.Empty;

    [Required]
    public string ManualInterventionWebhookUrl { get; init; } = string.Empty;

    [Required]
    public string NewsWebhookUrl { get; init; } = string.Empty;
}