namespace Neba.Website.Server.Tournaments.Detail;

/// <summary>
/// A published article associated with a tournament, used to render hyperlinks on the detail page.
/// </summary>
public sealed record TournamentDetailArticleViewModel
{
    /// <summary>
    /// Article headline.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// URL-safe slug used to build the link to the article detail page.
    /// </summary>
    public required string Slug { get; init; }
}
