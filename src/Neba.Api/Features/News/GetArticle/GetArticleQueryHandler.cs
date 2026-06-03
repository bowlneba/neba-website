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
        var article = await _articles
            .Where(article => article.Slug == query.Slug)
            .Select(article => new ArticleDetailDto
            {
                Slug = article.Slug,
                Title = article.Title,
                Content = article.Content,
                HeaderImageContainer = article.HeaderImage != null
                    ? article.HeaderImage.Container
                    : null,
                HeaderImagePath = article.HeaderImage != null
                    ? article.HeaderImage.Path
                    : null,
                PublishDateUtc = article.PublishDateUtc,
                Attachments = article.Attachments.Select(attachment => new ArticleAttachmentDto
                {
                    DisplayName = attachment.DisplayName,
                    Container = attachment.File.Container,
                    Path = attachment.File.Path
                }).ToList(),
                TournamentId = article.TournamentId
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (article is null)
        {
            return ArticleErrors.ArticleNotFound(query.Slug);
        }

        if (article.HeaderImageContainer != null && article.HeaderImagePath != null)
        {
            article = article with
            {
                HeaderImageUrl = _fileStorageService.GetBlobUri(article.HeaderImageContainer, article.HeaderImagePath)
            };
        }

        return article with
        {
            Attachments = [.. article.Attachments
                .Select(a => a with
                    {
                        Url = _fileStorageService.GetBlobUri(a.Container, a.Path)
                    })]
        };
    }
}