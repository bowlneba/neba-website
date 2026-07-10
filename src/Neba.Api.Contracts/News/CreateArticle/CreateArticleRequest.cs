namespace Neba.Api.Contracts.News.CreateArticle;

/// <summary>
/// Creates a news article.
/// </summary>
public sealed record CreateArticleRequest
{
    /// <summary>
    /// The article fields to create.
    /// </summary>
    public required ArticleInput Article { get; init; }
}
