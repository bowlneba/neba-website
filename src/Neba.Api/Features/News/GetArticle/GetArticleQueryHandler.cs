using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;
using Neba.Api.Storage;

namespace Neba.Api.Features.News.GetArticle;

internal sealed class GetArticleQueryHandler(
    AppDbContext appDbContext,
    TimeProvider timeProvider,
    IFileStorageService fileStorageService)
        : IQueryHandler<GetArticleQuery, ErrorOr<ArticleDetailDto>>
{
    private readonly IQueryable<Article> _articles = appDbContext.Articles.AsNoTracking();
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public async Task<ErrorOr<ArticleDetailDto>> HandleAsync(GetArticleQuery query, CancellationToken cancellationToken)
    {
        var canManageArticles = query.CallerHasArticleManagementPermission;
        var now = _timeProvider.GetUtcNow();

        var row = await ProjectArticleRow(FilterVisibleArticles(query.Slug, canManageArticles, now), canManageArticles)
            .SingleOrDefaultAsync(cancellationToken);

        return row is null 
            ? ArticleErrors.ArticleNotFound(query.Slug)
            : MapToDto(row, canManageArticles, _fileStorageService);
    }

    private IQueryable<Article> FilterVisibleArticles(string slug, bool canManageArticles, DateTimeOffset now) =>
        _articles.Where(article => article.Slug == slug
            && (canManageArticles
                || (article.PublicationStatus == PublicationStatus.Published && article.PublishDateUtc <= now)));

    private static IQueryable<ArticleRow> ProjectArticleRow(IQueryable<Article> articles, bool canManageArticles) =>
        articles.Select(article => new ArticleRow
        {
            Id = article.Id,
            Slug = article.Slug,
            PublicationStatus = article.PublicationStatus.Name,
            Title = article.Title,
            Content = article.Content,
            HeaderImageContainer = article.HeaderImage != null ? article.HeaderImage.Container : null,
            HeaderImagePath = article.HeaderImage != null ? article.HeaderImage.Path : null,
            HeaderImageContentType = article.HeaderImage != null ? article.HeaderImage.ContentType : null,
            HeaderImageSizeInBytes = article.HeaderImage != null ? article.HeaderImage.SizeInBytes : null,
            PublishDateUtc = article.PublishDateUtc,
            Attachments = (canManageArticles
                    ? article.Attachments
                    : article.Attachments.Where(attachment => !attachment.IsInline))
                .Select(attachment => new ArticleAttachmentRow
                {
                    DisplayName = attachment.DisplayName,
                    Container = attachment.File.Container,
                    Path = attachment.File.Path,
                    ContentType = attachment.File.ContentType,
                    SizeInBytes = attachment.File.SizeInBytes,
                    IsInline = attachment.IsInline
                }).ToList(),
            TournamentId = article.TournamentId
        });

    private static ArticleDetailDto MapToDto(ArticleRow row, bool canManageArticles, IFileStorageService fileStorageService)
    {
        var headerImageUrl = row.HeaderImageContainer != null && row.HeaderImagePath != null
            ? fileStorageService.GetBlobUri(row.HeaderImageContainer, row.HeaderImagePath)
            : null;

        return new ArticleDetailDto
        {
            Id = row.Id,
            Slug = row.Slug,
            PublicationStatus = row.PublicationStatus,
            Title = row.Title,
            Content = row.Content,
            HeaderImageUrl = headerImageUrl,
            HeaderImageContainer = canManageArticles ? row.HeaderImageContainer : null,
            HeaderImagePath = canManageArticles ? row.HeaderImagePath : null,
            HeaderImageContentType = canManageArticles ? row.HeaderImageContentType : null,
            HeaderImageSizeInBytes = canManageArticles ? row.HeaderImageSizeInBytes : null,
            PublishDateUtc = row.PublishDateUtc,
            Attachments = [.. row.Attachments.Select(a => MapAttachment(a, canManageArticles, fileStorageService))],
            TournamentId = row.TournamentId
        };
    }

    private static ArticleAttachmentDto MapAttachment(ArticleAttachmentRow a, bool canManageArticles, IFileStorageService fileStorageService) =>
        new()
        {
            DisplayName = a.DisplayName,
            ContentType = a.ContentType,
            Url = fileStorageService.GetBlobUri(a.Container, a.Path),
            IsInline = a.IsInline,
            Container = canManageArticles ? a.Container : null,
            Path = canManageArticles ? a.Path : null,
            SizeInBytes = canManageArticles ? a.SizeInBytes : null
        };

    private sealed class ArticleRow
    {
        public required ArticleId Id { get; init; }
        public required string Slug { get; init; }
        public required string PublicationStatus { get; init; }
        public required string Title { get; init; }
        public required string Content { get; init; }
        public string? HeaderImageContainer { get; init; }
        public string? HeaderImagePath { get; init; }
        public string? HeaderImageContentType { get; init; }
        public long? HeaderImageSizeInBytes { get; init; }
        public required DateTimeOffset PublishDateUtc { get; init; }
        public required List<ArticleAttachmentRow> Attachments { get; init; }
        public TournamentId? TournamentId { get; init; }
    }

    private sealed class ArticleAttachmentRow
    {
        public required string DisplayName { get; init; }
        public required string Container { get; init; }
        public required string Path { get; init; }
        public required string ContentType { get; init; }
        public required long SizeInBytes { get; init; }
        public required bool IsInline { get; init; }
    }
}