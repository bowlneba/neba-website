using Neba.Api.Contracts.News;
using Neba.Api.Contracts.News.CreateArticle;
using Neba.Api.Features.News.Domain;

namespace Neba.TestFactory.News;

public static class ArticleInputFactory
{
    public const string ValidTitle = "Test Article";
    public const string ValidContent = "Test content.";
    public static readonly DateTimeOffset ValidPublishDate = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static ArticleInput Create(
        string? title = null,
        string? slug = null,
        string? content = null,
        PublicationStatus? publicationStatus = null,
        DateTimeOffset? publishDate = null,
        string? tournamentId = null,
        HeaderImageInput? headerImage = null,
        IReadOnlyCollection<AttachmentInput>? attachments = null)
        => new()
        {
            Title = title ?? ValidTitle,
            Slug = slug,
            Content = content ?? ValidContent,
            PublicationStatus = (publicationStatus ?? PublicationStatus.Draft).Name,
            PublishDate = publishDate ?? ValidPublishDate,
            TournamentId = tournamentId,
            HeaderImage = headerImage,
            Attachments = attachments ?? []
        };
}