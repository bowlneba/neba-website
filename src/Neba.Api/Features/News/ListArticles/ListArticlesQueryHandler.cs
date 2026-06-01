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
        : IQueryHandler<ListArticlesQuery, IReadOnlyCollection<ArticleSummaryDto>>
{
    private readonly IQueryable<Article> _articles = appDbContext.Articles.AsNoTracking();
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public async Task<IReadOnlyCollection<ArticleSummaryDto>> HandleAsync(ListArticlesQuery query, CancellationToken cancellationToken)
    {
        var articles = await _articles
            .Where(article => article.PublicationStatus == PublicationStatus.Published
                && article.PublishDateUtc <= _timeProvider.GetUtcNow())
            .Select(article => new ArticleSummaryDto
            {
                Slug = article.Slug,
                Title = article.Title,
                Excerpt = $"{article.Content.Substring(0, 200)}...",
                HeaderImageContainer = article.HeaderImage != null
                    ? article.HeaderImage.Container
                    : null,
                HeaderImagePath = article.HeaderImage != null
                    ? article.HeaderImage.Path
                    : null,
                PublishDateUtc = article.PublishDateUtc,
            })
            .OrderByDescending(article => article.PublishDateUtc)
            .ToListAsync(cancellationToken);

        return [.. articles
            .ConvertAll(article => article.HeaderImageContainer != null && article.HeaderImagePath != null
                ? article with { HeaderImageUrl = _fileStorageService.GetBlobUri(article.HeaderImageContainer, article.HeaderImagePath) }
                : article)];
    }
}