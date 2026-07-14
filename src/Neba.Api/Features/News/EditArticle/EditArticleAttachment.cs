using Neba.Api.Features.Storage.Domain;

namespace Neba.Api.Features.News.EditArticle;

internal sealed record EditArticleAttachment
{
    public required string DisplayName { get; init; }

    public required bool IsInline { get; init; }

    public required StoredFile File { get; init; }
}