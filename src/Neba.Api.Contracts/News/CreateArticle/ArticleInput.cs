namespace Neba.Api.Contracts.News.CreateArticle;

/// <summary>
/// The fields required to create a news article.
/// </summary>
public sealed record ArticleInput
{
    /// <summary>
    /// The article's title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// An optional staff-supplied slug override. When null or blank, the slug is derived from <see cref="Title"/>.
    /// </summary>
    public string? Slug { get; init; }

    /// <summary>
    /// The full HTML content of the article.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The publication status: "Draft" or "Published".
    /// </summary>
    public required string PublicationStatus { get; init; }

    /// <summary>
    /// The UTC date and time the article is (or will be) published.
    /// </summary>
    public required DateTimeOffset PublishDateUtc { get; init; }

    /// <summary>
    /// The ULID string of an associated tournament, or null if the article is not linked to a tournament.
    /// </summary>
    public string? TournamentId { get; init; }
}
