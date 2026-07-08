namespace Neba.Api.Contracts.News.ListArticles;

/// <summary>
/// Represents a summary of a published article for display in a list.
/// </summary>
public sealed record ArticleSummaryResponse
{
    /// <summary>
    /// The ULID string that uniquely identifies the article.
    /// </summary>
    public required string ArticleId { get; init; }

    /// <summary>
    /// The URL-friendly identifier for the article.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// The publication status of the article, which indicates whether the article is published, draft, or archived. This status is used to control the visibility and accessibility of the article in the application.
    /// </summary>
    public required string PublicationStatus { get; init; }

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