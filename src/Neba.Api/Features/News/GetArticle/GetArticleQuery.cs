using ErrorOr;

using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.News.GetArticle;

/// <summary>Query to retrieve a news article by its URL slug.</summary>
public sealed record GetArticleQuery
    : ICachedQuery<ErrorOr<ArticleDetailDto>>
{
    /// <summary>URL-friendly identifier for the article.</summary>
    public required string Slug { get; init; }

    /// <summary>
    /// Indicates whether the caller has permission to manage articles. This property is required and must be set when creating an instance of the GetArticleQuery record. It is used to determine if the caller has the necessary permissions to perform article management operations, such as creating, updating, or deleting articles.
    /// </summary>
    public required bool CallerHasArticleManagementPermission { get; init; }

    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.News.Article(Slug, CallerHasArticleManagementPermission);

    /// <inheritdoc />
    public TimeSpan Expiry
        => TimeSpan.FromDays(7);
}