using ErrorOr;

namespace Neba.Api.Features.News.Domain;

internal static class ArticleErrors
{
    public static Error ArticleAttachmentDisplayNameRequired
        => Error.Validation("ArticleAttachment.DisplayName", "Display name must not be empty.");
}