# Edit Article — Implementation Plan

## Design Decisions

- **Permission**: new `Permissions.EditArticle` (`"News.EditArticle"`), added to `ArticleManagementPermissions` alongside `CreateArticle`/`DeleteArticle` so `CanManageArticlesPolicyName` continues to cover all three. Matches the existing one-permission-per-action convention — not reused from `CreateArticle`.
- **HTTP verb**: `PUT {id}` — full replace of the editable field set, mirroring `CreateArticle`'s `ArticleInput` shape one-for-one (title, content, publication status, publish date, tournament, header image, attachments). No `PATCH`.
- **Slug**: immutable after creation. Edit form displays it read-only; the edit payload does not include it. Avoids broken external links/bookmarks and keeps the `neba:news:{slug}` cache tag stable across an edit. `CreateArticle.razor`'s slug field gets a small help text/tooltip ("Cannot be changed after saving") added in Phase 1 so this is surfaced at creation time, not just discovered when editing later.
- **Attachments**: full replace-set. The edit request carries the complete desired attachments collection (kept + newly uploaded). The handler diffs against `Article.Attachments`: anything in the current set but missing from the new set is removed (blob cleanup enqueued via the existing async job pattern); anything new is added via a new `Article.AddAttachment` call (already exists) or equivalent.
- **Shared attachment input type**: promote `CreateArticle`'s internal `NewArticleAttachment` record out of the `CreateArticle` namespace so both `CreateArticle` and `EditArticle` reference the same type instead of each having their own. **Naming collision to resolve during implementation**: `Neba.Api.Features.News.Domain.ArticleAttachment` already exists as the domain entity (Id, DisplayName, File, IsInline). Renaming `NewArticleAttachment` to plain `ArticleAttachment` in a different namespace (e.g. `Neba.Api.Features.News`) is legal C# but will read confusingly next to the domain type of the same simple name in the same feature. Recommend a name that keeps the promotion but avoids the exact collision — e.g. `ArticleAttachmentInput` — placed in a shared location like `Features/News/ArticleAttachmentInput.cs` (sibling to `CreateArticle/` and `EditArticle/`), `internal` to `Neba.Api`. Confirm final name before implementing.
- **Form model**: `EditArticle.razor` uses its own dedicated form model, not shared with `CreateArticle.razor`'s. The two pages have different constraints (slug read-only, attachments pre-populated from existing data, no "new" defaults) that make a shared model more confusing than two small, independent ones.

## Phase 1 — API

Naming decision resolved: the promoted attachment-input type is called `ArticleAttachmentInput` (avoids colliding with the domain's `ArticleAttachment`).

Reuse note: `DeleteArticleCommandHandler`'s orphaned-blob cleanup already goes through `DeleteArticleFilesJob` + `StoredFileReference` (`src/Neba.Api/Features/News/DeleteArticle/`). Edit reuses both types as-is for cleaning up a replaced header image or removed attachments — no new job type needed.

---

### 1. Permission (`src/Neba.Api.Contracts/Security/Permission.cs`)

Add `EditArticle` and register it in `ArticleManagementPermissions`:

```csharp
/// <summary>
/// Permission to create a news article.
/// </summary>
public static readonly Permissions CreateArticle = new("News.CreateArticle", "Create Article");

/// <summary>
/// Permission to edit a news article.
/// </summary>
public static readonly Permissions EditArticle = new("News.EditArticle", "Edit Article");

/// <summary>
/// Permission to delete a news article.
/// </summary>
public static readonly Permissions DeleteArticle = new("News.DeleteArticle", "Delete Article");

/// <summary>
/// A collection of permissions related to article management.
/// </summary>
public static readonly IReadOnlyCollection<Permissions> ArticleManagementPermissions =
[
    CreateArticle,
    EditArticle,
    DeleteArticle,
];
```

---

### 2. Domain — `src/Neba.Api/Features/News/Domain/Article.cs`

`Title`, `Content`, `PublicationStatus`, `PublishDateUtc`, `TournamentId`, and `HeaderImage` change from `init` to `private set` so `Update` can mutate them post-construction. `Id` and `Slug` stay `init`-only (immutable). Full updated file:

```csharp
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
    public required ArticleId Id { get; init; }

    /// <summary>
    /// The article's title, displayed on the list and detail pages.
    /// </summary>
    public required string Title { get; private set; }

    /// <summary>
    /// URL-friendly, unique identifier used in the article's route (<c>/news/{slug}</c>). Immutable
    /// once assigned by <see cref="Create"/> — there is no <c>Update</c> parameter for it.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// The article's sanitized rich-text (HTML) body.
    /// </summary>
    public required string Content { get; private set; }

    /// <summary>
    /// Whether the article is a draft or published.
    /// </summary>
    public required PublicationStatus PublicationStatus { get; private set; }

    /// <summary>
    /// The UTC date/time the article becomes publicly visible when published.
    /// </summary>
    public required DateTimeOffset PublishDateUtc { get; private set; }

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
    /// normalized slug is the reserved value "new".
    /// </summary>
    public static ErrorOr<Article> Create(
        string title,
        string? slug,
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
            Id = ArticleId.New(),
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
    public ErrorOr<Success> Update(
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

        return Result.Success;
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
```

Add the matching error to `src/Neba.Api/Features/News/Domain/ArticleErrors.cs`:

```csharp
public static Error AttachmentNotFound(ArticleAttachmentId attachmentId)
    => Error.NotFound(
        code: "Article.Attachment.NotFound",
        description: "No attachment with this ID exists on the article.",
        metadata: new Dictionary<string, object> { { "ArticleAttachmentId", attachmentId.Value.ToString() } });
```

---

### 3. Shared attachment input type — `src/Neba.Api/Features/News/ArticleAttachmentInput.cs` (new file)

Promotes `CreateArticle`'s `NewArticleAttachment` out of that namespace so `EditArticle` can use the same type. **Delete** `src/Neba.Api/Features/News/CreateArticle/NewArticleAttachment.cs` once this lands.

```csharp
using Neba.Api.Features.Storage.Domain;

namespace Neba.Api.Features.News;

internal sealed record ArticleAttachmentInput
{
    public required string DisplayName { get; init; }

    public required bool IsInline { get; init; }

    public required StoredFile File { get; init; }
}
```

**Update references** in `CreateArticle`:

- `CreateArticleCommand.cs` — change `Attachments` to `IReadOnlyCollection<ArticleAttachmentInput>`.
- `CreateArticleCommandHandler.cs` — change `AddAttachments`'s parameter type to `IReadOnlyCollection<ArticleAttachmentInput>`.
- `CreateArticleEndpoint.cs` — change `new NewArticleAttachment { ... }` to `new ArticleAttachmentInput { ... }` in the attachment projection.

---

### 4. Command — `src/Neba.Api/Features/News/EditArticle/EditArticleCommand.cs` (new file)

```csharp
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.News.EditArticle;

internal sealed record EditArticleCommand
    : ICommand<Updated>
{
    public required ArticleId ArticleId { get; init; }

    public required string Title { get; init; }

    public required string Content { get; init; }

    public required PublicationStatus PublicationStatus { get; init; }

    /// <summary>
    /// The publish date/time, local to the caller (offset embedded). The handler converts this to UTC
    /// before it reaches the domain, which requires UTC.
    /// </summary>
    public required DateTimeOffset PublishDate { get; init; }

    public TournamentId? TournamentId { get; init; }

    public StoredFile? HeaderImage { get; init; }

    public IReadOnlyCollection<ArticleAttachmentInput> Attachments { get; init; } = [];
}
```

---

### 5. Handler — `src/Neba.Api/Features/News/EditArticle/EditArticleCommandHandler.cs` (new file)

```csharp
using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.News.CreateArticle;
using Neba.Api.Features.News.DeleteArticle;
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.News.EditArticle;

internal sealed class EditArticleCommandHandler(
        AppDbContext appDbContext,
        IBackgroundJobScheduler backgroundJobScheduler,
        IFusionCache cache)
    : ICommandHandler<EditArticleCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(EditArticleCommand command, CancellationToken cancellationToken)
    {
        var article = await appDbContext.Articles
            .Include(a => a.Attachments)
            .SingleOrDefaultAsync(a => a.Id == command.ArticleId, cancellationToken);

        if (article is null)
        {
            return ArticleErrors.ArticleNotFound(command.ArticleId.Value.ToString());
        }

        var tournamentCheck = await EnsureTournamentExistsAsync(command.TournamentId, cancellationToken);

        if (tournamentCheck.IsError)
        {
            return tournamentCheck.Errors;
        }

        var sanitizedContent = HtmlContentSanitizer.Sanitize(command.Content);

        // Must snapshot before Update() — HeaderImage is mutated in place, so reading it after the
        // call would return the new value, not the one being replaced.
        var previousHeaderImage = article.HeaderImage;

        var updateResult = article.Update(
            command.Title,
            sanitizedContent,
            command.PublicationStatus,
            command.PublishDate.ToUniversalTime(),
            command.TournamentId,
            command.HeaderImage);

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        var attachmentsResult = ReconcileAttachments(article, command.Attachments, out var orphanedFiles);

        if (attachmentsResult.IsError)
        {
            return attachmentsResult.Errors;
        }

        // StoredFile is a sealed record, so != is value equality (all four properties), not reference
        // equality — no hand-rolled comparison needed.
        if (previousHeaderImage is not null && previousHeaderImage != command.HeaderImage)
        {
            orphanedFiles.Add(new StoredFileReference
            {
                Container = previousHeaderImage.Container,
                Path = previousHeaderImage.Path
            });
        }

        await RemoveClaimedPendingUploadsAsync(command, cancellationToken);

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("neba:news:articles", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:news:{article.Slug}", token: cancellationToken);

        if (orphanedFiles.Count > 0)
        {
            backgroundJobScheduler.Enqueue(new DeleteArticleFilesJob
            {
                Files = orphanedFiles
            });
        }

        return Result.Updated;
    }

    private async Task<ErrorOr<Success>> EnsureTournamentExistsAsync(TournamentId? tournamentId, CancellationToken cancellationToken)
    {
        if (tournamentId is null)
        {
            return Result.Success;
        }

        var tournamentExists = await appDbContext.Tournaments
            .AnyAsync(tournament => tournament.Id == tournamentId, cancellationToken);

        return tournamentExists
            ? Result.Success
            : ArticleErrors.TournamentNotFound(tournamentId.Value);
    }

    // Full replace-set: anything in article.Attachments not present (by id) in the incoming
    // collection is removed (its blob queued for cleanup); anything in the incoming collection with
    // no matching existing attachment id is a newly uploaded attachment and gets added. Matching is by
    // storage address (container + path) since new attachments have no ArticleAttachmentId yet.
    private static ErrorOr<Success> ReconcileAttachments(
        Article article,
        IReadOnlyCollection<ArticleAttachmentInput> desiredAttachments,
        out List<StoredFileReference> orphanedFiles)
    {
        orphanedFiles = [];

        var desiredKeys = desiredAttachments
            .Select(a => (a.File.Container, a.File.Path))
            .ToHashSet();

        var toRemove = article.Attachments
            .Where(existing => !desiredKeys.Contains((existing.File.Container, existing.File.Path)))
            .ToList();

        foreach (var existing in toRemove)
        {
            var removed = article.RemoveAttachment(existing.Id);

            if (removed.IsError)
            {
                return removed.Errors;
            }

            orphanedFiles.Add(new StoredFileReference
            {
                Container = existing.File.Container,
                Path = existing.File.Path
            });
        }

        var existingKeys = article.Attachments
            .Select(a => (a.File.Container, a.File.Path))
            .ToHashSet();

        var toAdd = desiredAttachments
            .Where(desired => !existingKeys.Contains((desired.File.Container, desired.File.Path)));

        foreach (var attachment in toAdd)
        {
            var added = article.AddAttachment(attachment.DisplayName, attachment.File, attachment.IsInline);

            if (added.IsError)
            {
                return added.Errors;
            }
        }

        return Result.Success;
    }

    private async Task RemoveClaimedPendingUploadsAsync(EditArticleCommand command, CancellationToken cancellationToken)
    {
        var claimedFiles = command.Attachments
            .Select(attachment => attachment.File)
            .Concat(command.HeaderImage is null ? [] : [command.HeaderImage])
            .ToList();

        if (claimedFiles.Count == 0)
        {
            return;
        }

        var claimedContainers = claimedFiles.Select(file => file.Container).Distinct().ToList();

        var candidates = await appDbContext.PendingUploads
            .Where(pending => claimedContainers.Contains(pending.Container))
            .ToListAsync(cancellationToken);

        var claimedPaths = claimedFiles.Select(file => (file.Container, file.Path)).ToHashSet();
        var claimed = candidates.Where(pending => claimedPaths.Contains((pending.Container, pending.Path)));

        appDbContext.PendingUploads.RemoveRange(claimed);
    }
}
```

**Note on attachment matching by (container, path)**: newly uploaded attachments arrive from the client with no `ArticleAttachmentId` (only `EditArticle.razor`'s form model would carry one for pre-existing attachments, and that id isn't part of `ArticleAttachmentInput`/`AttachmentInput` today). Matching by storage address is simplest given the current wire shape. If Phase 2 needs to distinguish "kept, unchanged" from "removed-then-re-added-under-a-different-path" more precisely, revisit whether `AttachmentInput` should carry the existing `ArticleAttachmentId` for pre-existing attachments — but the above is sufficient for correctness (it never loses or double-counts a file) and keeps Phase 1 self-contained.

---

### 6. Endpoint — `src/Neba.Api/Features/News/EditArticle/EditArticleEndpoint.cs` (new file)

```csharp
using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.News.EditArticle;
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.News.EditArticle;

internal sealed class EditArticleEndpoint(Messaging.ICommandHandler<EditArticleCommand, Updated> commandHandler)
    : Endpoint<EditArticleRequest>
{
    private readonly Messaging.ICommandHandler<EditArticleCommand, Updated> _commandHandler = commandHandler;

    public override void Configure()
    {
        Put("{id}");
        Group<NewsEndpointGroup>();

        Options(options => options
            .WithVersionSet("News")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditArticle.PolicyName);

        Description(description => description
            .WithName("EditArticle")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(EditArticleRequest req, CancellationToken ct)
    {
        var command = new EditArticleCommand
        {
            ArticleId = new ArticleId(req.Id),
            Title = req.Article.Title,
            Content = req.Article.Content,
            PublicationStatus = PublicationStatus.FromName(req.Article.PublicationStatus),
            PublishDate = req.Article.PublishDate,
            TournamentId = string.IsNullOrWhiteSpace(req.Article.TournamentId)
                ? null
                : new TournamentId(req.Article.TournamentId),
            HeaderImage = req.Article.HeaderImage is null
                ? null
                : new StoredFile
                {
                    Container = req.Article.HeaderImage.Container,
                    Path = req.Article.HeaderImage.Path,
                    ContentType = req.Article.HeaderImage.ContentType,
                    SizeInBytes = req.Article.HeaderImage.SizeInBytes
                },
            Attachments = [.. req.Article.Attachments.Select(attachment => new ArticleAttachmentInput
            {
                DisplayName = attachment.DisplayName,
                IsInline = attachment.IsInline,
                File = new StoredFile
                {
                    Container = attachment.Container,
                    Path = attachment.Path,
                    ContentType = attachment.ContentType,
                    SizeInBytes = attachment.SizeInBytes
                }
            })]
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                await Send.NotFoundAsync(ct);

                // Stryker disable once Statement
                return;
            }

            if (result.FirstError.Type == ErrorType.Conflict)
            {
                AddError(result.FirstError.Description);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);

                // Stryker disable once Statement
                return;
            }

            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }

            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);

            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}
```

---

### 7. Validator — `src/Neba.Api/Features/News/EditArticle/EditArticleRequestValidator.cs` (new file)

Same field rules as `CreateArticleRequestValidator`, minus slug, plus an `Id` rule matching `DeleteArticleRequestValidator`'s:

```csharp
using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.News.EditArticle;
using Neba.Api.Features.News.Domain;

namespace Neba.Api.Features.News.EditArticle;

internal sealed class EditArticleRequestValidator
    : Validator<EditArticleRequest>
{
    public EditArticleRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty()
            .WithErrorCode("EditArticleRequest.IdRequired")
            .WithMessage("Id is required.")
            .Length(26)
            .WithErrorCode("EditArticleRequest.IdInvalidLength")
            .WithMessage("Id must be a 26-character ULID.");

        RuleFor(r => r.Article.Title)
            .NotEmpty()
            .WithErrorCode("EditArticleRequest.TitleRequired")
            .WithMessage("Title is required.")
            .MaximumLength(256)
            .WithErrorCode("EditArticleRequest.TitleTooLong")
            .WithMessage("Title must be 256 characters or fewer.");

        RuleFor(r => r.Article.Content)
            .NotEmpty()
            .WithErrorCode("EditArticleRequest.ContentRequired")
            .WithMessage("Content is required.");

        RuleFor(r => r.Article.PublicationStatus)
            .NotEmpty()
            .WithErrorCode("EditArticleRequest.PublicationStatusRequired")
            .WithMessage("Publication status is required.")
            .Must(status => PublicationStatus.List.Any(s => s.Name == status))
            .WithErrorCode("EditArticleRequest.PublicationStatusInvalid")
            .WithMessage("Publication status must be one of: Draft, Published.");

        RuleFor(r => r.Article.PublishDate)
            .NotEqual(default(DateTimeOffset))
            .WithErrorCode("EditArticleRequest.PublishDateRequired")
            .WithMessage("Publish date is required.");

        RuleFor(r => r.Article.TournamentId)
            .Length(26)
            .WithErrorCode("EditArticleRequest.TournamentIdInvalidLength")
            .WithMessage("TournamentId must be a 26-character ULID.")
            .When(r => !string.IsNullOrWhiteSpace(r.Article.TournamentId));
    }
}
```

---

### 8. Summary — `src/Neba.Api/Features/News/EditArticle/EditArticleSummary.cs` (new file)

```csharp
using FastEndpoints;

namespace Neba.Api.Features.News.EditArticle;

internal sealed class EditArticleSummary : Summary<EditArticleEndpoint>
{
    public EditArticleSummary()
    {
        Summary = "Edits a news article.";
        Description = "Replaces the article's editable fields (title, content, publication status, publish date, tournament link, header image, attachments). The slug is immutable and is not part of this request. Attachments are a full replace-set: any existing attachment missing from the request is removed. Requires the News.EditArticle permission.";

        Response(204, "Article updated.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the News.EditArticle permission.");
        Response(404, "No article exists with the given ID.");
        Response(409, "TournamentId does not reference an existing tournament.");
        Response(422, "Title or content failed a domain validation rule.");
    }
}
```

---

### 9. Contracts — `src/Neba.Api.Contracts/News/EditArticle/` (new folder)

`EditArticleInput.cs` — same shape as `ArticleInput` minus `Slug`:

```csharp
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
```

`EditArticleRequest.cs`:

```csharp
namespace Neba.Api.Contracts.News.EditArticle;

/// <summary>
/// Edits a news article.
/// </summary>
public sealed record EditArticleRequest
{
    /// <summary>
    /// The ULID string identifying the article to edit.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The article fields to update.
    /// </summary>
    public required EditArticleInput Article { get; init; }
}
```

`AttachmentInput`/`HeaderImageInput` are reused directly from `Neba.Api.Contracts.News.CreateArticle` (`using` reference, not duplicated) — they're already public wire DTOs with no Create-specific meaning.

**`INewsApi.cs`** — add:

```csharp
/// <summary>
/// Edits a news article. Requires the News.EditArticle permission.
/// </summary>
/// <param name="id">The article's strongly-typed ID.</param>
/// <param name="request">The article fields to update.</param>
/// <param name="cancellationToken">A token to cancel the operation.</param>
[Put("/news/{id}")]
Task<IApiResponse> EditArticleAsync(
    string id,
    EditArticleRequest request,
    CancellationToken cancellationToken = default);
```

(Add the corresponding `using Neba.Api.Contracts.News.EditArticle;` at the top of `INewsApi.cs`.)

---

### 10. Tests

- **Domain unit tests** (`Neba.Api.Tests`, `Features/News/Domain/ArticleTests.cs` — extend existing): `Update` success path (verify all six fields changed); `Update` returns `ArticleErrors.TitleRequired`/`ContentRequired` for blank input; `RemoveAttachment` success (attachment no longer in `Attachments`); `RemoveAttachment` returns `ArticleErrors.AttachmentNotFound` for an unknown id.
- **Handler unit tests** (`Neba.Api.Tests`, `Features/News/EditArticle/EditArticleCommandHandlerTests.cs`): article not found → `NotFound`; tournament not found → `Conflict`; success with no attachment changes (no job enqueued); success with a new attachment added; success with an existing attachment removed (verify `DeleteArticleFilesJob` enqueued with that file); `RemoveClaimedPendingUploadsAsync` removes matching `PendingUpload` rows.
- **Header image change-detection tests** — dedicated cases, since the `previousHeaderImage != command.HeaderImage` check now relies on `StoredFile`'s full 4-property record equality (`Container`, `Path`, `ContentType`, `SizeInBytes`), not just container+path. Each case asserts both sides: whether `DeleteArticleFilesJob` was enqueued (via the `IBackgroundJobScheduler` mock) *and* that `article.HeaderImage` ends up equal to `command.HeaderImage` after the save:
  - **Identical header image resubmitted** (same `Container`, `Path`, `ContentType`, `SizeInBytes` as the existing one) → no cleanup job enqueued. This is the case a naive container+path-only check would also get right, but confirms the record-equality path doesn't false-positive on an unchanged image.
  - **Different `Path`/`Container`** (a genuinely different blob) → cleanup job enqueued for the *old* file's container/path (assert on the enqueued job's contents, not just that `Enqueue` was called once).
  - **Same `Container`/`Path` but different `ContentType`** → cleanup job enqueued. This is the case that was silently missed before removing the hand-rolled `StoredFileEquals` (which only compared container+path) — regression-guard this explicitly so a future refactor back to a partial comparison gets caught.
  - **Same `Container`/`Path` but different `SizeInBytes`** → cleanup job enqueued, same rationale as above.
  - **Header image removed entirely** (`command.HeaderImage` is `null`, article previously had one) → old image enqueued for cleanup, `article.HeaderImage` ends up `null`.
  - **Header image added where none existed before** (`previousHeaderImage` is `null`, `command.HeaderImage` is not) → no cleanup job (nothing to orphan), `article.HeaderImage` set to the new value. Confirms the `previousHeaderImage is not null` guard doesn't also suppress the case where the article gains a header image for the first time.
- **Validator unit tests** (`EditArticleRequestValidatorTests.cs`): mirror `CreateArticleRequestValidatorTests`, replacing slug-related cases with the `Id` length/required cases from `DeleteArticleRequestValidatorTests`.
- **Endpoint authorization integration test**: mirror `DeleteArticleEndpointAuthorizationTests` (watch for the FastEndpoints static-state gotchas in CLAUDE.md's "Process-Wide Static State Leaks Between Integration Tests" section if this spins up a real `WebApplication` — disable `UsePropertyNamingPolicy`, use `StopAsync` not `DisposeAsync`).
- Update `CreateArticleCommandHandlerTests`/related tests for the `NewArticleAttachment` → `ArticleAttachmentInput` rename (compile-time only change, no behavior difference).
- If a test enumerates all `Permissions`/policies, add `EditArticle`.

## Phase 2 — UI

### Blazor page (`src/Neba.Website.Server/News/EditArticle.razor`)

- Route: `/news/{slug}/edit` — slug-based, matching how the article detail page already routes.
- `@rendermode @(new InteractiveServerRenderMode(prerender: false))` (data-loading page, per CLAUDE.md Page Titles convention) with `<PageTitle>Edit {model.Title} - BowlNEBA</PageTitle>`.
- `<AuthorizeView Policy="@Permissions.EditArticle.PolicyName">` gate at the route level (page-level authorization, not just an entry-point icon/button).
- On load: fetch the article via existing `GetArticle` query/API service, populate a dedicated `EditArticleFormModel` (not shared with `CreateArticle.razor`'s model — see Design Decisions).
- Slug field rendered read-only (disabled input or plain text), not part of the submitted payload.
- Reuse `DirtyFormGuard` pattern: explicit `EditContext` in constructor, `OnFieldChanged` → `MarkDirty()`, explicit `MarkDirty()` calls for `RichTextEditor`, `FileUpload` add/remove, tournament picker — identical wiring to `CreateArticle.razor`.
- Attachments: preload existing attachments into the same add/remove list `CreateArticle.razor` uses for new uploads; removing a pre-existing attachment marks it for removal in the diff (no immediate blob delete client-side) and triggers `MarkDirty()`. Removing an inline (embedded) attachment still goes through the existing `ConfirmActionModal` guard.
- Header image: same `FileUpload` component pre-populated with the current image; replacing it stages a new upload the same way Create does.
- Submit → `PUT` via the API service (add `EditArticleAsync` alongside `ITournamentApiService`-style existing article API service), reset `_isDirty = false`, `StateHasChanged(); await Task.Yield();` before navigating away (per the CLAUDE.md ordering note), then navigate to the article detail page.
- **Entry points** (two, both permission-gated on `Permissions.EditArticle.PolicyName`, independent of `FabCreateButton` which stays Create-only):
  1. **Article card (list page)**: a pencil icon, styled/positioned analogously to the existing trash icon used for delete, visible only when the user holds the edit permission. Links to `/news/{slug}/edit`.
  2. **Article detail page**: an Edit button in the same admin-action area as the existing Delete button, but **visually separated** from Delete (spacing/grouping, not adjacent) — Delete is destructive and should stay isolated per standard practice; Edit is a plain navigation action and shouldn't sit right next to it where a misclick risks deleting instead of editing.

### Tests

- bUnit tests for `EditArticle.razor`: loads and pre-populates fields, dirty tracking marks correctly for each input type, slug is read-only/not submitted, attachment removal marks dirty and excludes from resubmission, successful save resets dirty and navigates, guard blocks navigation when dirty.
- E2E test (`tests/e2e/`) covering the golden path: open edit, change a field, save, verify update reflected; and the dirty-guard path (attempt to navigate away with unsaved changes, confirm/cancel).

## Open Items to Confirm During Implementation

- Whether the domain layer needs any new invariants once `Update`/`RemoveAttachment` exist (e.g. can't remove the last attachment if inline content still references it) — confirm no such rule is implied by current behavior before assuming none.
- `ArticleConfiguration.cs` (EF mapping) should need no changes — property names/types are unchanged, only `init` → `private set` on `Article`, which EF Core materializes identically via reflection. Worth a quick double-check against the actual mapping file during implementation in case it does anything unusual (e.g. explicit backing-field wiring) that assumes `init`.
