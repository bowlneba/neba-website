namespace Neba.Api.Features.Tournaments.GetTournament;

/// <summary>
/// A published article associated with this tournament.
/// </summary>
public sealed record TournamentDetailArticleDto
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