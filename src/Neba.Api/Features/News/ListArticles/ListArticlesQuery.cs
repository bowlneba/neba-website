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
        => CacheDescriptors.News.ListArticles(Page, PageSize, CallerHasArticleManagementPermission);

    /// <inheritdoc />
    public TimeSpan Expiry
        => TimeSpan.FromMinutes(45);

    /// <inheritdoc />
    public int Page { get; init; }

    /// <inheritdoc />
    public int PageSize { get; init; }

    /// <summary>
    /// Indicates whether the caller has permission to manage articles. This property is required and must be set when creating an instance of the ListArticlesQuery record. It is used to determine if the caller has the necessary permissions to perform article management operations, such as creating, updating, or deleting articles.
    /// </summary>
    public required bool CallerHasArticleManagementPermission { get; init; }
}