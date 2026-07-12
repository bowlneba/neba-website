using Neba.Api.Contracts.News.CreateArticle;

namespace Neba.Api.Contracts.News.EditArticle;

/// <summary>
/// The fields required to edit a news article. The slug is immutable and is not included here.
/// </summary>
public sealed record EditArticleInput
{
    /// <summary>
    /// The article's title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The full HTML content of the article.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The publication status: "Draft" or "Published".
    /// </summary>
    public required string PublicationStatus { get; init; }

    /// <summary>
    /// The date and time the article is (or will be) published, local to the caller. Unsuffixed date/time
    /// properties in this API are always local to the caller — the offset is embedded in the value, and
    /// the server converts to UTC where needed.
    /// </summary>
    public required DateTimeOffset PublishDate { get; init; }

    /// <summary>
    /// The ULID string of an associated tournament, or null if the article is not linked to a tournament.
    /// </summary>
    public string? TournamentId { get; init; }

    /// <summary>
    /// The header image associated with the article, or null if there is no header image.
    /// </summary>
    public HeaderImageInput? HeaderImage { get; init; }

    /// <summary>
    /// The full desired collection of attachments associated with the article (kept + newly uploaded).
    /// Any existing attachment not present here is removed.
    /// </summary>
    public IReadOnlyCollection<AttachmentInput> Attachments { get; init; } = [];
}
