using ErrorOr;

using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.News.Domain;

internal static class ArticleErrors
{
    public static Error ArticleAttachmentDisplayNameRequired
        => Error.Validation("ArticleAttachment.DisplayName", "Display name must not be empty.");

    public static Error AttachmentNotFound(ArticleAttachmentId attachmentId)
        => Error.NotFound(
            code: "Article.Attachment.NotFound",
            description: "No attachment with this ID exists on the article.",
            metadata: new Dictionary<string, object> { { "ArticleAttachmentId", attachmentId.Value.ToString() } });

    public static Error ArticleNotFound(string slug)
        => Error.NotFound(
            code: "Article.NotFound",
            description: "Article with slug not found.",
            metadata: new Dictionary<string, object>
            {
                { "Slug", slug }
            });

    public static Error TitleRequired
        => Error.Validation("Article.Title.Required", "Title must not be empty.");

    public static Error ContentRequired
        => Error.Validation("Article.Content.Required", "Content must not be empty.");

    public static Error SlugInvalid
        => Error.Validation("Article.Slug.Invalid", "Slug must contain at least one alphanumeric character.");

    public static Error SlugReserved
        => Error.Validation("Article.Slug.Reserved", "Slug 'new' is reserved for the article-creation route.");

    public static Error SlugAlreadyExists(string slug)
        => Error.Conflict(
            code: "Article.Slug.AlreadyExists",
            description: "An article with this slug already exists.",
            metadata: new Dictionary<string, object> { { "Slug", slug } });

    public static Error TournamentNotFound(TournamentId tournamentId)
        => Error.Conflict(
            code: "Article.Tournament.NotFound",
            description: "The specified tournament does not exist.",
            metadata: new Dictionary<string, object> { { "TournamentId", tournamentId.Value.ToString() } });
}