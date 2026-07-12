using ErrorOr;

using Neba.Api.Features.Storage.Domain;

namespace Neba.Api.Features.News.Domain;

/// <summary>
/// A file attached to an <see cref="Article"/>, optionally embedded inline in its body.
/// </summary>
public sealed class ArticleAttachment
{
    /// <summary>
    /// Unique identifier for the attachment.
    /// </summary>
    public required ArticleAttachmentId Id { get; init; }

    /// <summary>
    /// Staff-provided label shown in the attachment list.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Storage address of the attached file.
    /// </summary>
    public required StoredFile File { get; init; }

    /// <summary>
    /// Whether the attached file is also embedded within the article body.
    /// </summary>
    public bool IsInline { get; init; }

    internal static ErrorOr<ArticleAttachment> Create(string displayName, StoredFile file, bool isInline)
    {
        return string.IsNullOrWhiteSpace(displayName)
            ? ArticleErrors.ArticleAttachmentDisplayNameRequired
            : new ArticleAttachment
            {
                Id = ArticleAttachmentId.New(),
                DisplayName = displayName,
                File = file,
                IsInline = isInline
            };
    }
}