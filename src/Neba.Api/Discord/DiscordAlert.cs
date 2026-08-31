using Ardalis.SmartEnum;

namespace Neba.Api.Discord;

internal sealed record DiscordAlert(DiscordAlertSeverity Severity, string Title, string Body, IReadOnlyDictionary<string, string>? Metadata = null);

internal sealed class DiscordAlertSeverity
    : SmartEnum<DiscordAlertSeverity>
{
    public static readonly DiscordAlertSeverity Info = new(nameof(Info), 0, DiscordColor.Blue);
    public static readonly DiscordAlertSeverity Warning = new(nameof(Warning), 1, DiscordColor.Yellow);
    public static readonly DiscordAlertSeverity Critical = new(nameof(Critical), 2, DiscordColor.Red);

    private DiscordAlertSeverity(string name, int value, DiscordColor notificationColor)
        : base(name, value)
    {
        NotificationColor = notificationColor;
    }

    public DiscordColor NotificationColor { get; }
}