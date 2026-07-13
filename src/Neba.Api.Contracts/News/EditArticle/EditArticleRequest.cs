namespace Neba.Api.Contracts.News.EditArticle;

/// <summary>
/// Edits a news article.
/// </summary>
public sealed record EditArticleRequest
{
    /// <summary>
    /// The ULID string identifying the article to edit.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The article fields to update.
    /// </summary>
    public required EditArticleInput Article { get; init; }
}