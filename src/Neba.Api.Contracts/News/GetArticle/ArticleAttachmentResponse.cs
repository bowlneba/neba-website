namespace Neba.Api.Contracts.News.GetArticle;

/// <summary>
/// Represents a file attachment on a news article.
/// </summary>
public sealed record ArticleAttachmentResponse
{
    /// <summary>
    /// The display name for the attachment link.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// A public URL to download or view the attachment, or null if unavailable.
    /// </summary>
    public Uri? Url { get; init; }
}
