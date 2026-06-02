namespace Neba.Api.Contracts.News.ListArticles;

/// <summary>
/// Represents a summary of a published article for display in a list.
/// </summary>
public sealed record ArticleSummaryResponse
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
    /// A truncated preview of the article body.
    /// </summary>
    public required string Excerpt { get; init; }

    /// <summary>
    /// A public URL for the article's header image, or null if no image is set.
    /// </summary>
    public Uri? HeaderImageUrl { get; init; }

    /// <summary>
    /// The UTC date and time when the article was published.
    /// </summary>
    public required DateTimeOffset PublishDateUtc { get; init; }
}
