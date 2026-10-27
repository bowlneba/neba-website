namespace Neba.Api.Discord;

internal static class OilPatternAnnouncementMessage
{
    /// <summary>
    /// Builds the announcement that a tournament's oil pattern is revealed: the sentence, linked to the
    /// tournament details page, with the tournament logo when there is one.
    /// </summary>
    public static NewsAnnouncement Create(string tournamentName, string tournamentId, string baseUrl, Uri? logoUrl)
        => new(
            $"The oil pattern for {tournamentName} has been announced.",
            new Uri($"{baseUrl.TrimEnd('/')}/tournaments/{tournamentId}"),
            logoUrl);
}
