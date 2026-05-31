using ErrorOr;

using Neba.Api.Features.Storage.Domain;

namespace Neba.Api.Features.News.Domain;

/// <summary>
/// Represents an attachment associated with a news article. This could be an image, document, or any other file type that is relevant to the article. The attachment has a unique identifier, a display name for user-friendly identification, a reference to the stored file, and a flag indicating whether it should be displayed inline with the article content or as a separate attachment.
/// </summary>
public sealed class ArticleAttachment
{
    /// <summary>
    /// Unique identifier for the article attachment. This is a strongly-typed ID to ensure type safety when working with attachment IDs throughout the codebase. The underlying value is a ULID, which provides both uniqueness and chronological sorting capabilities. This ID is required for all attachments and is used as the primary key in the database.
    /// </summary>
    public required ArticleAttachmentId Id { get; init; }

    /// <summary>
    /// A user-friendly name for the attachment, used for display purposes on the website. This should be a concise and descriptive name that helps users understand the content of the attachment without needing to open it. The display name is required for all attachments and is often shown alongside the attachment file when displayed in the article content or in a list of attachments. It can be different from the actual file name to provide better context or readability for users.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// A reference to the stored file associated with this attachment. This is required for all attachments and should point to a valid file in the storage system (e.g., a URL or file ID). The StoredFile object contains all necessary information about the file, such as its location, size, and content type, allowing for proper handling and display of the attachment on the website. The presence of a valid StoredFile is essential for the attachment to be functional and accessible to users.
    /// </summary>
    public required StoredFile File { get; init; }

    /// <summary>
    /// Indicates whether the attachment should be displayed inline with the article content or as a separate attachment. If IsInline is true, the attachment will be rendered directly within the article body at the appropriate location (e.g., an image displayed within the text). If IsInline is false, the attachment will be listed separately (e.g., as a downloadable file or a link) rather than being embedded in the article content. This allows for flexibility in how attachments are presented to users based on their relevance and importance to the article's content.
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