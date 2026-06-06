using Neba.Api.Caching;
using Neba.Api.Messaging;

namespace Neba.Api.Features.News.ListArticles;

/// <summary>
/// Represents a query to list articles, including pagination parameters and caching information. This query is used to retrieve a paginated list of article summaries, which may include details such as the article's slug, title, excerpt, header image information, and publish date. The query implements the ICachedQuery interface to specify caching behavior and the IPaginationQuery interface to include pagination parameters.
/// </summary>
public sealed record ListArticlesQuery
    : ICachedQuery<PagedResult<ArticleSummaryDto>>, IPaginationQuery
{
    /// <inheritdoc />
    public CacheDescriptor Cache
        => CacheDescriptors.News.ListArticles(Page, PageSize);

    /// <inheritdoc />
    public TimeSpan Expiry
        => new(0, 45, 0);

    /// <inheritdoc />
    public int Page { get; init; }

    /// <inheritdoc />
    public int PageSize { get; init; }
}