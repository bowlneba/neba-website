using System.Diagnostics.CodeAnalysis;
using System.Text;

using ErrorOr;

using Neba.Api.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.News.Domain;

/// <summary>
/// A news article published on the website, optionally linked to a tournament.
/// </summary>
public sealed class Article
    : AggregateRoot
{
    /// <summary>
    /// Unique identifier for the article.
    /// </summary>
    public ArticleId Id { get; init; }

    /// <summary>
    /// The article's title, displayed on the list and detail pages.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// URL-friendly, unique identifier used in the article's route (<c>/news/{slug}</c>).
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// The article's sanitized rich-text (HTML) body.
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the article is a draft or published.
    /// </summary>
    public PublicationStatus PublicationStatus { get; private set; } = PublicationStatus.Draft;

    /// <summary>
    /// The UTC date/time the article becomes publicly visible when published.
    /// </summary>
    public DateTimeOffset PublishDateUtc { get; private set; }

    /// <summary>
    /// Optional header image displayed at the top of the article.
    /// </summary>
    public StoredFile? HeaderImage { get; private set; }

    /// <summary>
    /// Optional tournament this article relates to.
    /// </summary>
    public TournamentId? TournamentId { get; private set; }

    internal Tournament? Tournament { get; init; }

    private readonly List<ArticleAttachment> _attachments = [];

    /// <summary>
    /// Files attached to the article.
    /// </summary>
    public IReadOnlyList<ArticleAttachment> Attachments
        => _attachments.AsReadOnly();

    private const string ReservedSlugNew = "new";

    /// <summary>
    /// Creates a new article. <paramref name="content"/> must already be sanitized by the caller
    /// (see <c>HtmlContentSanitizer</c> in the <c>CreateArticle</c> use case) — the domain only
    /// validates that it is non-empty, it does not sanitize HTML itself. If <paramref name="slug"/>
    /// is null or empty, the slug is generated from <paramref name="title"/>. Returns a validation
    /// error if title/content are empty, the normalized slug has no alphanumeric characters, or the
    /// normalized slug is the reserved value "new". <paramref name="id"/> is production-optional —
    /// it exists only so test factories can assign a deterministic ID for stable Verify snapshots;
    /// production callers always omit it and get a newly generated <see cref="ArticleId"/>.
    /// </summary>
    public static ErrorOr<Article> Create(
        string title,
        string? slug,
        string content,
        PublicationStatus publicationStatus,
        DateTimeOffset publishDateUtc,
        TournamentId? tournamentId,
        StoredFile? headerImage,
        ArticleId? id = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return ArticleErrors.TitleRequired;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return ArticleErrors.ContentRequired;
        }

        var normalizedSlug = NormalizeSlug(string.IsNullOrEmpty(slug)
            ? title
            : slug);

        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return ArticleErrors.SlugInvalid;
        }

        if (normalizedSlug == ReservedSlugNew)
        {
            return ArticleErrors.SlugReserved;
        }

        return new Article
        {
            Id = id ?? ArticleId.New(),
            Title = title,
            Slug = normalizedSlug,
            Content = content,
            PublicationStatus = publicationStatus,
            PublishDateUtc = publishDateUtc,
            TournamentId = tournamentId,
            HeaderImage = headerImage
        };
    }

    /// <summary>
    /// Updates the article's editable fields in place. The slug is immutable and is not a parameter —
    /// see the remarks on <see cref="Slug"/>. <paramref name="content"/> must already be sanitized by
    /// the caller, matching <see cref="Create"/>. Returns a validation error if title/content are empty.
    /// </summary>
    public ErrorOr<Updated> Update(
        string title,
        string content,
        PublicationStatus publicationStatus,
        DateTimeOffset publishDateUtc,
        TournamentId? tournamentId,
        StoredFile? headerImage)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return ArticleErrors.TitleRequired;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return ArticleErrors.ContentRequired;
        }

        Title = title;
        Content = content;
        PublicationStatus = publicationStatus;
        PublishDateUtc = publishDateUtc;
        TournamentId = tournamentId;
        HeaderImage = headerImage;

        return Result.Updated;
    }

    /// <summary>
    /// Normalizes a title or a staff-supplied slug override into a URL-safe slug: lowercase,
    /// alphanumeric runs joined by single hyphens, no leading/trailing hyphen. Only called from
    /// <see cref="Create"/> — the resulting <see cref="Article.Slug"/> is what the command handler
    /// checks for uniqueness, so there is a single source of truth for slug normalization.
    /// </summary>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Slugs are URL-facing and must be lowercase, not normalized for security comparisons.")]
    private static string NormalizeSlug(string value)
    {
        var lowered = value.Trim().ToLowerInvariant();
        var builder = new StringBuilder(lowered.Length);
        var lastWasHyphen = false;

        foreach (var c in lowered)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && builder.Length > 0)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        return builder.ToString().TrimEnd('-');
    }

    /// <summary>
    /// Adds an attachment to the article. Returns a validation error if the display name is empty.
    /// </summary>
    public ErrorOr<Success> AddAttachment(string displayName, StoredFile file, bool isInline)
    {
        var attachment = ArticleAttachment.Create(displayName, file, isInline);

        if (attachment.IsError)
        {
            return attachment.Errors;
        }

        _attachments.Add(attachment.Value);

        return Result.Success;
    }

    /// <summary>
    /// Removes an attachment from the article. Returns <see cref="ArticleErrors.AttachmentNotFound"/>
    /// if no attachment with <paramref name="attachmentId"/> exists. Does not delete the underlying
    /// blob — callers are responsible for enqueuing that separately (see <c>EditArticleCommandHandler</c>).
    /// </summary>
    public ErrorOr<Success> RemoveAttachment(ArticleAttachmentId attachmentId)
    {
        var attachment = _attachments.Find(a => a.Id == attachmentId);

        if (attachment is null)
        {
            return ArticleErrors.AttachmentNotFound(attachmentId);
        }

        _attachments.Remove(attachment);

        return Result.Success;
    }
}