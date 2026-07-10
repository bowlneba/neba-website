namespace Neba.Api.Contracts.News.CreateArticle;

/// <summary>
/// Response returned after successfully creating a news article.
/// </summary>
public sealed record ArticleResponse
{
    /// <summary>
    /// The ULID string that uniquely identifies the newly created article.
    /// </summary>
    public required string ArticleId { get; init; }

    /// <summary>
    /// The normalized, unique slug assigned to the article (derived from title, or the supplied override).
    /// </summary>
    public required string Slug { get; init; }
}
