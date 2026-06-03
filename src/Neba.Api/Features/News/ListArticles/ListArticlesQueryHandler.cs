using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.News.Domain;
using Neba.Api.Messaging;
using Neba.Api.Storage;

namespace Neba.Api.Features.News.ListArticles;

internal sealed class ListArticlesQueryHandler(
    AppDbContext appDbContext,
    TimeProvider timeProvider,
    IFileStorageService fileStorageService)
        : IQueryHandler<ListArticlesQuery, PagedResult<ArticleSummaryDto>>
{
    private readonly IQueryable<Article> _articles = appDbContext.Articles.AsNoTracking();
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public async Task<PagedResult<ArticleSummaryDto>> HandleAsync(ListArticlesQuery query, CancellationToken cancellationToken)
    {
        var baseQuery = _articles
            .Where(article => article.PublicationStatus == PublicationStatus.Published
                && article.PublishDateUtc <= _timeProvider.GetUtcNow());

        var totalItems = await baseQuery.CountAsync(cancellationToken);

        var rows = await baseQuery
            .Select(article => new
            {
                article.Slug,
                article.Title,
                Excerpt = $"{article.Content.Substring(0, 200)}...",
                HeaderImageContainer = article.HeaderImage != null ? article.HeaderImage.Container : null,
                HeaderImagePath = article.HeaderImage != null ? article.HeaderImage.Path : null,
                article.PublishDateUtc,
            })
            .OrderByDescending(article => article.PublishDateUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .ConvertAll(row => new ArticleSummaryDto
            {
                Slug = row.Slug,
                Title = row.Title,
                Excerpt = row.Excerpt,
                HeaderImageUrl = row.HeaderImageContainer != null && row.HeaderImagePath != null
                    ? _fileStorageService.GetBlobUri(row.HeaderImageContainer, row.HeaderImagePath)
                    : null,
                PublishDateUtc = row.PublishDateUtc,
            });

        return new PagedResult<ArticleSummaryDto>([.. items], totalItems);
    }
}