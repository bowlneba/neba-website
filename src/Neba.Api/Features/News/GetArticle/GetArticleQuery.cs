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

    /// <summary>Whether the caller can manage articles; gates draft visibility and is part of the cache key so results aren't shared across permission levels.</summary>
    public required bool CallerHasArticleManagementPermission { get; init; }

    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.News.Article(Slug, CallerHasArticleManagementPermission);

    /// <inheritdoc />
    public TimeSpan Expiry
        => TimeSpan.FromDays(7);
}