using Neba.Api.Contracts.News.ListArticles;

using Refit;

namespace Neba.Api.Contracts.News;

/// <summary>
/// Defines the news API contract.
/// </summary>
public interface INewsApi
{
    /// <summary>
    /// Lists published articles, ordered by publish date descending.
    /// </summary>
    /// <param name="page">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of articles per page.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A paginated collection of article summaries.</returns>
    [Get("/news")]
    Task<IApiResponse<PaginationResponse<ArticleSummaryResponse>>> ListArticlesAsync(
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
}
