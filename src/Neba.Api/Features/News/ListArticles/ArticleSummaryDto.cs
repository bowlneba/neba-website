namespace Neba.Api.Features.News.ListArticles;

/// <summary>
/// Represents a summary of an article, including its slug, title, excerpt, header image information, and publish date.
/// </summary>
public sealed record ArticleSummaryDto
{
    /// <summary>
    /// Gets the slug of the article, which is a URL-friendly identifier used to access the article.
    /// </summary>
    public required string Slug { get; init; }

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