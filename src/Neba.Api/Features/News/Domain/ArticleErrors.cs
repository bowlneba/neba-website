using ErrorOr;

namespace Neba.Api.Features.News.Domain;

internal static class ArticleErrors
{
    public static Error ArticleAttachmentDisplayNameRequired
        => Error.Validation("ArticleAttachment.DisplayName", "Display name must not be empty.");

    public static Error ArticleNotFound(string slug)
        => Error.NotFound(
            code: "Article.NotFound",
            description: "Article with slug not found.",
            metadata: new Dictionary<string, object>
            {
                { "Slug", slug }
            });
}