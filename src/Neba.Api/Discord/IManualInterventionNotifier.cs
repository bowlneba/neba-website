namespace Neba.Api.Discord;

/// <summary>
/// Posts alerts that need a person to fix data to the manual-intervention channel.
/// </summary>
internal interface IManualInterventionNotifier
{
    Task NotifyAsync(DiscordAlert alert, CancellationToken cancellationToken);
}
