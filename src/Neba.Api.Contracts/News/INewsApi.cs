using Neba.Api.Contracts.News.CreateArticle;
using Neba.Api.Contracts.News.EditArticle;
using Neba.Api.Contracts.News.GetArticle;
using Neba.Api.Contracts.News.ListArticles;
using Neba.Api.Contracts.Uploads;

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

    /// <summary>
    /// Gets a published article by its URL slug.
    /// </summary>
    /// <param name="slug">The URL-friendly identifier for the article.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The full article detail, or 404 if not found.</returns>
    [Get("/news/{slug}")]
    Task<IApiResponse<ArticleDetailResponse>> GetArticleAsync(
        string slug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an article by its ID. Returns 204 whether or not an article with the given ID existed.
    /// </summary>
    /// <param name="id">The article's strongly-typed ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    [Delete("/news/{id}")]
    Task<IApiResponse> DeleteArticleAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a news article. Requires the News.CreateArticle permission.
    /// </summary>
    /// <param name="request">The article fields to create.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created article's ID and slug.</returns>
    [Post("/news")]
    Task<IApiResponse<ArticleResponse>> CreateArticleAsync(
        CreateArticleRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Edits a news article. Requires the News.EditArticle permission.
    /// </summary>
    /// <param name="id">The article's strongly-typed ID.</param>
    /// <param name="request">The article fields to update.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    [Put("/news/{id}")]
    Task<IApiResponse> EditArticleAsync(
        string id,
        EditArticleRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a news article header image. Requires the News.CreateArticle permission.
    /// </summary>
    /// <param name="file">The image file to upload.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The uploaded file's metadata.</returns>
    [Multipart]
    [Post("/news/header-image")]
    Task<IApiResponse<UploadedFileResponse>> UploadArticleHeaderImageAsync(
        [AliasAs("File")] StreamPart file,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a news article attachment (or inline embedded image). Requires the News.CreateArticle permission.
    /// </summary>
    /// <param name="file">The attachment file to upload.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The uploaded file's metadata.</returns>
    [Multipart]
    [Post("/news/attachments")]
    Task<IApiResponse<UploadedFileResponse>> UploadArticleAttachmentAsync(
        [AliasAs("File")] StreamPart file,
        CancellationToken cancellationToken = default);
}