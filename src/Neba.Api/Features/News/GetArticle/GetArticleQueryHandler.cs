using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.News.Domain;
using Neba.Api.Messaging;
using Neba.Api.Storage;

namespace Neba.Api.Features.News.GetArticle;

internal sealed class GetArticleQueryHandler(
    AppDbContext appDbContext,
    IFileStorageService fileStorageService)
        : IQueryHandler<GetArticleQuery, ErrorOr<ArticleDetailDto>>
{
    private readonly IQueryable<Article> _articles = appDbContext.Articles.AsNoTracking();
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public async Task<ErrorOr<ArticleDetailDto>> HandleAsync(GetArticleQuery query, CancellationToken cancellationToken)
    {
        var row = await _articles
            .Where(article => article.Slug == query.Slug)
            .Select(article => new
            {
                article.Slug,
                article.Title,
                article.Content,
                HeaderImageContainer = article.HeaderImage != null ? article.HeaderImage.Container : null,
                HeaderImagePath = article.HeaderImage != null ? article.HeaderImage.Path : null,
                article.PublishDateUtc,
                Attachments = article.Attachments.Select(attachment => new
                {
                    attachment.DisplayName,
                    attachment.File.Container,
                    attachment.File.Path
                }).ToList(),
                article.TournamentId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return ArticleErrors.ArticleNotFound(query.Slug);
        }

        var headerImageUrl = row.HeaderImageContainer != null && row.HeaderImagePath != null
            ? _fileStorageService.GetBlobUri(row.HeaderImageContainer, row.HeaderImagePath)
            : null;

        return new ArticleDetailDto
        {
            Slug = row.Slug,
            Title = row.Title,
            Content = row.Content,
            HeaderImageUrl = headerImageUrl,
            PublishDateUtc = row.PublishDateUtc,
            Attachments = [.. row.Attachments.Select(a => new ArticleAttachmentDto
            {
                DisplayName = a.DisplayName,
                Url = _fileStorageService.GetBlobUri(a.Container, a.Path)
            })],
            TournamentId = row.TournamentId
        };
    }
}