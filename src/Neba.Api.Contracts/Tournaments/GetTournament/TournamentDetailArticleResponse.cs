namespace Neba.Api.Contracts.Tournaments.GetTournament;

/// <summary>
/// A published article associated with a tournament.
/// </summary>
public sealed record TournamentDetailArticleResponse
{
    /// <summary>
    /// Article headline.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// URL-safe slug used to navigate to the article detail page.
    /// </summary>
    public required string Slug { get; init; }
}
