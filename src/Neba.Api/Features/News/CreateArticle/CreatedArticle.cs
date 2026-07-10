using Neba.Api.Features.News.Domain;

namespace Neba.Api.Features.News.CreateArticle;

/// <summary>
/// Represents the result of successfully creating a news article, including its identifier and normalized slug.
/// </summary>
public sealed record CreatedArticle
{
    /// <summary>
    /// Gets the unique identifier of the newly created article.
    /// </summary>
    public required ArticleId Id { get; init; }

    /// <summary>
    /// Gets the normalized slug assigned to the newly created article.
    /// </summary>
    public required string Slug { get; init; }
}