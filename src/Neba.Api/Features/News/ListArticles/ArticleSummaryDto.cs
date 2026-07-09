using Neba.Api.Features.News.Domain;

namespace Neba.Api.Features.News.ListArticles;

/// <summary>
/// Represents a summary of an article, including its slug, title, excerpt, header image information, and publish date.
/// </summary>
public sealed record ArticleSummaryDto
{
    /// <summary>
    /// Gets the unique identifier of the article, which is a strongly-typed ID to ensure type safety when working with article IDs throughout the codebase. The underlying value is a ULID, which provides both uniqueness and chronological sorting capabilities. This ID is required for all articles and is used as the primary key in the database.
    /// </summary>
    public required ArticleId Id { get; init; }

    /// <summary>
    /// Gets the slug of the article, which is a URL-friendly identifier used to access the article.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Gets the publication status of the article, which indicates whether the article is published, draft, or archived. This status is used to control the visibility and accessibility of the article in the application.
    /// </summary>
    public required string PublicationStatus { get; init; }

    /// <summary>
    /// Gets the title of the article, which is a brief and descriptive heading that summarizes the content of the article.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the excerpt of the article, which is a short summary or preview of the article's content, typically used to entice readers to click and read the full article.
    /// </summary>
    public required string Excerpt { get; init; }

    /// <summary>
    /// Gets the URL of the header image of the article; null if no header image is associated.
    /// </summary>
    public Uri? HeaderImageUrl { get; init; }

    /// <summary>
    /// Gets the publish date of the article in UTC. This is typically used to display the date the article was published.
    /// </summary>
    public required DateTimeOffset PublishDateUtc { get; init; }
}