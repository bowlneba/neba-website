namespace Neba.Api.Discord;

internal interface IDiscordNotifier
{
    Task NotifyAsync(DiscordAlert alert, CancellationToken cancellationToken);
}