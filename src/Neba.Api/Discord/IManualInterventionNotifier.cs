namespace Neba.Api.Discord;

/// <summary>
/// Posts alerts that need a person to fix data to the manual-intervention channel. Throws when the
/// post fails, so the calling job retries.
/// </summary>
internal interface IManualInterventionNotifier
{
    Task NotifyAsync(DiscordAlert alert, CancellationToken cancellationToken);
}
