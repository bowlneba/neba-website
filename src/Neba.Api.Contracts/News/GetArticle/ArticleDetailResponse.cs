namespace Neba.Api.Contracts.News.GetArticle;

/// <summary>
/// Represents the full detail of a published news article.
/// </summary>
public sealed record ArticleDetailResponse
{
    /// <summary>
    /// The URL-friendly identifier for the article.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// The title of the article.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The full HTML content of the article.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// A public URL for the article's header image, or null if no image is set.
    /// </summary>
    public Uri? HeaderImageUrl { get; init; }

    /// <summary>
    /// The UTC date and time when the article was published.
    /// </summary>
    public required DateTimeOffset PublishDateUtc { get; init; }

    /// <summary>
    /// The ULID string of an associated tournament, or null if the article is not linked to a tournament.
    /// </summary>
    public string? TournamentId { get; init; }

    /// <summary>
    /// Files attached to the article.
    /// </summary>
    public IReadOnlyCollection<ArticleAttachmentResponse> Attachments { get; init; } = [];
}