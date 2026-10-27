namespace Neba.Api.Discord;

/// <summary>
/// Posts a news article announcement to the public news channel. Throws when the post fails and
/// never retries, because a retry after a timeout could post the announcement twice.
/// </summary>
internal interface INewsAnnouncementNotifier
{
    Task NotifyAsync(NewsAnnouncement announcement, CancellationToken cancellationToken);
}