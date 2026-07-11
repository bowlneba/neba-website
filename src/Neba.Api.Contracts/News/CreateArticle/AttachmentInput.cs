namespace Neba.Api.Contracts.News.CreateArticle;

/// <summary>
/// Represents the input required for an attachment associated with a news article.
/// </summary>
public sealed record AttachmentInput
{
    /// <summary>
    /// The display name of the attachment.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Indicates whether the attachment is inline.
    /// </summary>
    public required bool IsInline { get; init; }

    /// <summary>
    /// The storage container where the attachment is located.
    /// </summary>
    public required string Container { get; init; }

    /// <summary>
    /// The path to the attachment within the storage container.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// The MIME type of the attachment.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// The size of the attachment in bytes.
    /// </summary>
    public required long SizeInBytes { get; init; }
}