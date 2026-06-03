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

    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.News.Article(Slug);

    /// <inheritdoc />
    public TimeSpan Expiry
        => TimeSpan.FromDays(7);
}