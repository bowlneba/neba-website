# Article Creation — Implementation Plan

Three phases, each landing as its own PR (or commit set) so it can be reviewed independently. This doc is a living reference — update it as decisions change or phases complete.

Related: [`docs/ubiquitous-language.md`](../ubiquitous-language.md) (§ News) is the source of truth for the domain terms/rules referenced below; keep both in sync as this feature evolves. [`docs/plans/CanManageArticlesPolicy.md`](CanManageArticlesPolicy.md) covers the `CanManageArticles` authorization policy this feature builds on.

---

## Background / resolved design decisions

- **`Article` currently has no `Create` factory** — it's all `required` init properties. This feature retrofits it to the always-valid pattern already used by `ArticleAttachment.Create` (private/internal constructor + `internal static ErrorOr<Article> Create(...)`).
- **No invariant ties `PublicationStatus` to `PublishDateUtc`.** Any combination is structurally valid at creation time — visibility (`Published && PublishDateUtc <= now`) is already enforced at the query layer (`ListArticlesQueryHandler`, `GetArticleQueryHandler`), not the domain layer. `Create` only requires `PublishDateUtc` to be present.
- **Slug is auto-generated from `Title` (lowercased, hyphenated) but staff-editable.** `Create` accepts an optional `slug` parameter — null/blank derives from title, a supplied value is used verbatim (after the same normalization/validation).
- **Slug uniqueness is enforced by the command handler**, not `Create` — it requires a repository lookup, which is a cross-aggregate/DB concern. Returns `Article.Slug.AlreadyExists` as a `Conflict` (resubmitting with a different slug succeeds).
- **Slug reserved-word rule**: slugs may not equal `"new"` (or other future reserved route segments), since `/news/new` is the article-creation route, not an article detail page. Blazor's router prioritizes literal segments over `{Slug}`, so there's no runtime routing collision — the risk is purely "an article with that slug would be permanently unreachable." This is a validation rule on `Create`.
- **Header image and attachments upload independently of the Article record.** Staff pick files before the Article is ever saved (see Phase 3), so blobs can't be keyed by `ArticleId`. The upload endpoint stores the file and returns a `StoredFile` pointer immediately; the create form carries that pointer through to the final `CreateArticleCommand`, which is what actually associates it with the saved `Article`/`ArticleAttachment` row. A file uploaded but never attached to a saved Article is an orphan, swept by a periodic cleanup job. Blob paths: `news/uploads/header/{ulid}-{filename}` and `news/uploads/attachments/{ulid}-{filename}`.
- **UI never talks to blob storage directly** — all uploads go through the API, even though this costs an extra hop vs. a SAS-token direct-to-blob approach. Locked in as a hard constraint, not just a phase-1 shortcut.
- **`TournamentId` is part of the command from day one**, even though the Phase 1/2 UI always sends `null`. The handler must already validate a non-null `TournamentId` correctly (existence check, `Conflict` if not found) so Phase 2's tournament-linking UI work doesn't require handler changes.
- **Auth**: this endpoint needs a new `News.CreateArticle` permission added to `Permissions.ArticleManagementPermissions` (alongside the existing `News.DeleteArticle`), per the extension point already called out in `CanManageArticlesPolicy.md`. Endpoint uses `Policies(Permissions.CreateArticle.PolicyName)`, matching the delete endpoint's convention (`Policies(PermissionCatalog.DeleteArticle.PolicyName)`).

---

## Phase 1 — API: Create Article

Scope: `Title`, `Slug` (optional override), `Content`, `PublicationStatus`, `PublishDateUtc`, `TournamentId` (always `null` from the UI this phase, but handled correctly if set). No header image, no attachments — those are Phase 3.

**Precedent note**: this is the first "create an aggregate root" feature in the codebase — no existing `CreateXCommandHandler` persists via `appDbContext.Add(...)`, and no aggregate root (only value objects) currently has a `Create` factory. The code below mirrors the closest existing patterns instead: `ArticleAttachment.Create` (always-valid child entity factory), `RegisterEndpoint`/`RegisterCommandHandler` (the only `Send.CreatedAtAsync` + Conflict/Validation split in the repo), and `DeleteArticleCommandHandler` (EF persistence + `IFusionCache` tag invalidation style for this exact aggregate).

### Domain (`Neba.Api/Features/News/Domain`)

**`Article.cs`** — add a slug-normalization helper and a factory method. `Article` stays constructible via object initializer (same as today, same as `ArticleAttachment`) since `internal` factories in this codebase gate *intended* construction by convention, not by a private constructor:

```csharp
using System.Text;

using ErrorOr;

using Neba.Api.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.News.Domain;

public sealed class Article
    : AggregateRoot
{
    // ... existing properties unchanged ...

    private const string ReservedSlugNew = "new";

    internal static ErrorOr<Article> Create(
        string title,
        string? slug,
        string content,
        PublicationStatus status,
        DateTimeOffset publishDateUtc,
        TournamentId? tournamentId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return ArticleErrors.TitleRequired;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return ArticleErrors.ContentRequired;
        }

        var normalizedSlug = NormalizeSlug(string.IsNullOrWhiteSpace(slug) ? title : slug);

        if (string.IsNullOrEmpty(normalizedSlug))
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
            PublicationStatus = status,
            PublishDateUtc = publishDateUtc,
            TournamentId = tournamentId
        };
    }

    /// <summary>
    /// Normalizes a title or a staff-supplied slug override into a URL-safe slug: lowercase,
    /// alphanumeric runs joined by single hyphens, no leading/trailing hyphen. Exposed so the
    /// command handler can compute the same candidate for a uniqueness check before calling
    /// <see cref="Create"/> — both call this so there is one normalization rule, not two.
    /// </summary>
    internal static string NormalizeSlug(string value)
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
}
```

**`ArticleErrors.cs`** — add the new error factories (mirroring the existing `ArticleAttachmentDisplayNameRequired`/`ArticleNotFound` style):

```csharp
internal static class ArticleErrors
{
    // ... existing ArticleAttachmentDisplayNameRequired, ArticleNotFound(slug) unchanged ...

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
```

### Application/API (`Neba.Api/Features/News/CreateArticle/`)

**`CreateArticleCommand.cs`**

```csharp
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.News.CreateArticle;

internal sealed record CreateArticleCommand
    : ICommand<CreatedArticle>
{
    public required string Title { get; init; }

    public string? Slug { get; init; }

    public required string Content { get; init; }

    public required PublicationStatus Status { get; init; }

    public required DateTimeOffset PublishDateUtc { get; init; }

    public TournamentId? TournamentId { get; init; }
}

internal sealed record CreatedArticle(ArticleId ArticleId, string Slug);
```

**`CreateArticleCommandHandler.cs`**

```csharp
using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.News.Domain;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.News.CreateArticle;

internal sealed class CreateArticleCommandHandler(AppDbContext appDbContext, IFusionCache cache)
    : ICommandHandler<CreateArticleCommand, CreatedArticle>
{
    public async Task<ErrorOr<CreatedArticle>> HandleAsync(CreateArticleCommand command, CancellationToken cancellationToken)
    {
        if (command.TournamentId is not null)
        {
            var tournamentExists = await appDbContext.Tournaments
                .AnyAsync(t => t.Id == command.TournamentId, cancellationToken);

            if (!tournamentExists)
            {
                return ArticleErrors.TournamentNotFound(command.TournamentId.Value);
            }
        }

        var slugCandidate = Article.NormalizeSlug(
            string.IsNullOrWhiteSpace(command.Slug) ? command.Title : command.Slug);

        var slugExists = await appDbContext.Articles
            .AnyAsync(a => a.Slug == slugCandidate, cancellationToken);

        if (slugExists)
        {
            return ArticleErrors.SlugAlreadyExists(slugCandidate);
        }

        var article = Article.Create(
            command.Title,
            command.Slug,
            command.Content,
            command.Status,
            command.PublishDateUtc,
            command.TournamentId);

        if (article.IsError)
        {
            return article.Errors;
        }

        appDbContext.Articles.Add(article.Value);
        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("neba:news:articles", token: cancellationToken);

        return new CreatedArticle(article.Value.Id, article.Value.Slug);
    }
}
```

**`CreateArticleEndpoint.cs`**

```csharp
using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.News.CreateArticle;
using Neba.Api.Features.News.Domain;
using Neba.Api.Features.Tournaments.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.News.CreateArticle;

internal sealed class CreateArticleEndpoint(Messaging.ICommandHandler<CreateArticleCommand, CreatedArticle> commandHandler)
    : Endpoint<CreateArticleRequest, ArticleResponse>
{
    private readonly Messaging.ICommandHandler<CreateArticleCommand, CreatedArticle> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post(string.Empty);
        Group<NewsEndpointGroup>();

        Options(options => options
            .WithVersionSet("News")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CreateArticle.PolicyName);

        Description(description => description
            .WithName("CreateArticle")
            .WithTags("Admin")
            .Produces<ArticleResponse>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(CreateArticleRequest req, CancellationToken ct)
    {
        var command = new CreateArticleCommand
        {
            Title = req.Input.Title,
            Slug = req.Input.Slug,
            Content = req.Input.Content,
            Status = PublicationStatus.FromName(req.Input.PublicationStatus),
            PublishDateUtc = req.Input.PublishDateUtc,
            TournamentId = string.IsNullOrWhiteSpace(req.Input.TournamentId)
                ? null
                : new TournamentId(req.Input.TournamentId)
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.Conflict)
            {
                AddError(result.FirstError.Description);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);

                // Stryker disable once Statement
                return;
            }

            foreach (var error in result.Errors)
                AddError(error.Description);

            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);

            // Stryker disable once Statement
            return;
        }

        var response = new ArticleResponse
        {
            ArticleId = result.Value.ArticleId.Value.ToString(),
            Slug = result.Value.Slug
        };

        // Stryker disable once Statement
        await Send.CreatedAtAsync(
            "GetArticle",
            routeValues: new { slug = result.Value.Slug },
            responseBody: response,
            cancellation: ct);
    }
}
```

**`CreateArticleRequestValidator.cs`** (structural only — no DB lookups, no business rules, per the codebase's validator convention):

```csharp
using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.News.CreateArticle;
using Neba.Api.Features.News.Domain;

namespace Neba.Api.Features.News.CreateArticle;

internal sealed class CreateArticleRequestValidator
    : Validator<CreateArticleRequest>
{
    public CreateArticleRequestValidator()
    {
        RuleFor(r => r.Input.Title)
            .NotEmpty()
            .WithErrorCode("CreateArticleRequest.TitleRequired")
            .WithMessage("Title is required.")
            .MaximumLength(256)
            .WithErrorCode("CreateArticleRequest.TitleTooLong")
            .WithMessage("Title must be 256 characters or fewer.");

        RuleFor(r => r.Input.Slug)
            .MaximumLength(256)
            .WithErrorCode("CreateArticleRequest.SlugTooLong")
            .WithMessage("Slug must be 256 characters or fewer.")
            .When(r => !string.IsNullOrWhiteSpace(r.Input.Slug));

        RuleFor(r => r.Input.Content)
            .NotEmpty()
            .WithErrorCode("CreateArticleRequest.ContentRequired")
            .WithMessage("Content is required.");

        RuleFor(r => r.Input.PublicationStatus)
            .NotEmpty()
            .WithErrorCode("CreateArticleRequest.PublicationStatusRequired")
            .WithMessage("Publication status is required.")
            .Must(status => PublicationStatus.List.Any(s => s.Name == status))
            .WithErrorCode("CreateArticleRequest.PublicationStatusInvalid")
            .WithMessage("Publication status must be one of: Draft, Published.");

        RuleFor(r => r.Input.PublishDateUtc)
            .NotEqual(default(DateTimeOffset))
            .WithErrorCode("CreateArticleRequest.PublishDateRequired")
            .WithMessage("Publish date is required.");

        RuleFor(r => r.Input.TournamentId)
            .Length(26)
            .WithErrorCode("CreateArticleRequest.TournamentIdInvalidLength")
            .WithMessage("TournamentId must be a 26-character ULID.")
            .When(r => !string.IsNullOrWhiteSpace(r.Input.TournamentId));
    }
}
```

**`CreateArticleSummary.cs`**

```csharp
using FastEndpoints;

using Neba.Api.Contracts.News.CreateArticle;

namespace Neba.Api.Features.News.CreateArticle;

internal sealed class CreateArticleSummary : Summary<CreateArticleEndpoint>
{
    public CreateArticleSummary()
    {
        Summary = "Creates a news article.";
        Description = "Creates a draft or published article. Slug is derived from the title unless a staff-supplied override is given; either way it is normalized and must be unique. Requires the News.CreateArticle permission.";

        Response(201, "Article created.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the News.CreateArticle permission.");
        Response(409, "Slug already taken, or TournamentId does not reference an existing tournament.");
        Response(422, "Title, content, or slug failed a domain validation rule.");
    }
}
```

### Contracts (`Neba.Api.Contracts/News/CreateArticle/`)

**`ArticleInput.cs`**

```csharp
namespace Neba.Api.Contracts.News.CreateArticle;

/// <summary>
/// The fields required to create a news article.
/// </summary>
public sealed record ArticleInput
{
    /// <summary>
    /// The article's title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// An optional staff-supplied slug override. When null or blank, the slug is derived from <see cref="Title"/>.
    /// </summary>
    public string? Slug { get; init; }

    /// <summary>
    /// The full HTML content of the article.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The publication status: "Draft" or "Published".
    /// </summary>
    public required string PublicationStatus { get; init; }

    /// <summary>
    /// The UTC date and time the article is (or will be) published.
    /// </summary>
    public required DateTimeOffset PublishDateUtc { get; init; }

    /// <summary>
    /// The ULID string of an associated tournament, or null if the article is not linked to a tournament.
    /// </summary>
    public string? TournamentId { get; init; }
}
```

**`CreateArticleRequest.cs`**

```csharp
namespace Neba.Api.Contracts.News.CreateArticle;

/// <summary>
/// Creates a news article.
/// </summary>
public sealed record CreateArticleRequest
{
    /// <summary>
    /// The article fields to create.
    /// </summary>
    public required ArticleInput Input { get; init; }
}
```

**`ArticleResponse.cs`**

```csharp
namespace Neba.Api.Contracts.News.CreateArticle;

/// <summary>
/// Response returned after successfully creating a news article.
/// </summary>
public sealed record ArticleResponse
{
    /// <summary>
    /// The ULID string that uniquely identifies the newly created article.
    /// </summary>
    public required string ArticleId { get; init; }

    /// <summary>
    /// The normalized, unique slug assigned to the article (derived from title, or the supplied override).
    /// </summary>
    public required string Slug { get; init; }
}
```

**`INewsApi.cs`** — add:

```csharp
using Neba.Api.Contracts.News.CreateArticle;
// ... existing usings ...

public interface INewsApi
{
    // ... existing members ...

    /// <summary>
    /// Creates a news article. Requires the News.CreateArticle permission.
    /// </summary>
    /// <param name="request">The article fields to create.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created article's ID and slug.</returns>
    [Post("/news")]
    Task<IApiResponse<ArticleResponse>> CreateArticleAsync(
        CreateArticleRequest request,
        CancellationToken cancellationToken = default);
}
```

### Security (`Neba.Api.Contracts/Security/Permission.cs`)

Add inside the `#region News` block, alongside `DeleteArticle`:

```csharp
/// <summary>
/// Permission required to create a news article.
/// </summary>
public static readonly Permissions CreateArticle = new("News.CreateArticle", "Create Article");

/// <summary>
/// This is a temporary permission to set us up until real permissions come into the picture
/// </summary>
public static readonly Permissions DeleteArticle = new("News.DeleteArticle", "Delete Article");

/// <summary>
/// A collection of permissions related to article management.
/// </summary>
public static readonly IReadOnlyCollection<Permissions> ArticleManagementPermissions =
[
    CreateArticle,
    DeleteArticle,
];
```

### Tests

- `Article.Create` unit tests (`Neba.Api.Tests`): title required, content required, slug derived from title when `slug` is null/blank, slug normalization of a supplied override (mixed case, spaces, punctuation → hyphenated lowercase), empty-after-normalization slug is an error, reserved slug `"new"` is rejected (for both a derived and a supplied slug).
- `CreateArticleCommandHandler` unit tests: unknown `TournamentId` → `Article.Tournament.NotFound` (Conflict), duplicate slug (derived and supplied cases) → `Article.Slug.AlreadyExists` (Conflict), success path persists the article and returns `CreatedArticle`, cache tag `neba:news:articles` is invalidated on success.
- Integration test for the endpoint (`CreateArticleEndpointIntegrationTests` alongside the existing `DeleteArticleEndpointAuthorizationTests` pattern): 201 + `Location` header + body shape on success, 400 on structural validation failure (missing title, invalid `PublicationStatus` value), 409 on duplicate slug, 401/403 for missing/insufficient permission — remember the FastEndpoints/Hangfire static-state pitfalls documented in `CLAUDE.md`'s Learnings section if this test spins up a real `WebApplication`.

---

## Phase 1b — Server-side HTML sanitization

Resolves the open item originally flagged under Phase 2's rich text editor work (library choice + placement). Landing it here, against `Article.Create`, rather than waiting for the Phase 2 UI, because `Article.Create` already accepts arbitrary `Content` today — the domain invariant ("`Content` is always safe HTML") shouldn't depend on which UI happens to submit it first.

**Decisions**:

- **Library: `HtmlSanitizer` (Ganss.Xss), not a hand-rolled allowlist on top of the already-referenced `HtmlAgilityPack`.** `HtmlAgilityPack` is already a dependency (`Neba.Api.csproj`), but it's used in `Documents/HtmlProcessor.cs` purely as a parser — no sanitization/allowlist logic to reuse. `HtmlSanitizer` is the de facto standard allowlist sanitizer for .NET, and its **defaults already satisfy every requirement below with zero custom configuration**: strips `<script>`, inline event handler attributes (`onclick`, `onerror`, etc.), `<iframe>`/`<object>`/`<embed>`; default `AllowedSchemes` is `http`/`https`/`mailto` only, so `data:` URIs in `href`/`src` are rejected out of the box (this is also the Phase 3 "reject `<img src="data:...">`" requirement, satisfied for free). Default allowed tags/attributes already cover Quill's actual output shapes (bold/italic/lists/links/headings/blockquote/code/`img[src,alt]`).
- **Placement: inside `Article.Create` itself, not `CreateArticleCommandHandler`.** Mirrors how `NormalizeSlug` already works — a transformation owned by the aggregate so the invariant holds regardless of caller. This matters because the eventual `UpdateArticleCommandHandler` needs the same behavior; if sanitization lived in the create handler, it would have to be duplicated (and could be forgotten) in the update path. A future `Article.UpdateContent(...)`-style method reuses the same static helper.
- **Namespace: its own top-level, feature-agnostic folder — not nested under `Features/News`.** Follows the same precedent as `Compliance/` (PII redaction) — a cross-cutting concern that lives as a sibling to `Features/`, not inside any one feature's domain folder, so any future feature needing rich-text content can call it without creating a cross-feature-domain dependency.
- **Ordering: sanitize first, then re-check for emptiness.** A payload that's *entirely* disallowed markup (e.g. a bare `<script>` tag with no visible text) must not slip past the required-content rule once stripped. Reusing the existing `ArticleErrors.ContentRequired` error for this case (rather than adding a new one) keeps the error surface small — the caller experience is the same either way ("Content must not be empty").

### Package (`Directory.Packages.props` / `src/Neba.Api/Neba.Api.csproj`)

```xml
<!-- Directory.Packages.props -->
<PackageVersion Include="HtmlSanitizer" Version="9.0.892" />
```

```xml
<!-- src/Neba.Api/Neba.Api.csproj -->
<PackageReference Include="HtmlSanitizer" />
```

### `Neba.Api/RichText/HtmlContentSanitizer.cs`

```csharp
using Ganss.Xss;

namespace Neba.Api.RichText;

/// <summary>
/// Sanitizes rich-text HTML (e.g. Quill editor output) before it is persisted, stripping any
/// construct that could lead to XSS when later rendered via <c>MarkupString</c> — script tags,
/// event handler attributes, non-http(s)/mailto URL schemes (including <c>data:</c> URIs), and
/// non-allowlisted tags such as &lt;iframe&gt;/&lt;object&gt;/&lt;embed&gt;. Feature-agnostic by
/// design (sibling to <c>Compliance</c>, not nested under any one feature's domain folder) so any
/// future rich-text field can reuse it, not just <c>Article.Content</c>.
/// </summary>
internal static class HtmlContentSanitizer
{
    // Stryker disable once all : HtmlSanitizer is a third-party allowlist sanitizer; its default
    // configuration is the tested surface, not a mutable rule set authored here.
    private static readonly HtmlSanitizer Sanitizer = new();

    internal static string Sanitize(string html)
        => Sanitizer.Sanitize(html);
}
```

### `Article.cs` — wire into `Create`

```csharp
using Neba.Api.RichText;

// ...

public static ErrorOr<Article> Create(
    string title,
    string? slug,
    string content,
    PublicationStatus publicationStatus,
    DateTimeOffset publishDateUtc,
    TournamentId? tournamentId)
{
    if (string.IsNullOrWhiteSpace(title))
    {
        return ArticleErrors.TitleRequired;
    }

    if (string.IsNullOrWhiteSpace(content))
    {
        return ArticleErrors.ContentRequired;
    }

    var sanitizedContent = HtmlContentSanitizer.Sanitize(content);

    if (string.IsNullOrWhiteSpace(sanitizedContent))
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
        Content = sanitizedContent,
        PublicationStatus = publicationStatus,
        PublishDateUtc = publishDateUtc,
        TournamentId = tournamentId
    };
}
```

### Tests

- **`HtmlContentSanitizerTests` (`Neba.Api.Tests`, new file alongside `Neba.Api/RichText/HtmlContentSanitizer.cs`'s mirrored test path)**:
  - Benign formatting HTML (bold/italic/lists/links/headings/blockquote/code — Quill's actual output shapes) passes through unchanged.
  - `<script>` tags and inline event handler attributes (`onclick`, `onerror`, etc.) are stripped.
  - `javascript:` and `data:` URLs in `href`/`src` are stripped (asserts the default `AllowedSchemes` behavior explicitly, so a future config change that widens the scheme list fails loudly here rather than silently).
  - `<iframe>`/`<object>`/`<embed>` and other non-allowlisted tags are removed.
  - Malformed/unclosed HTML doesn't throw and produces safe output.
  - Idempotency: sanitizing already-sanitized content is a no-op (matters once `UpdateArticleCommandHandler` re-sanitizes existing content on edit).
- **`Article.Create` unit tests** — add to the existing test class:
  - Content that is *entirely* disallowed markup (e.g. `"<script>alert(1)</script>"`, no visible text) returns `ArticleErrors.ContentRequired`, not a successful `Article` with empty `Content`.
  - Content containing a mix of benign and disallowed markup persists with only the disallowed portion removed (`article.Value.Content` assertion against the expected sanitized string).
- **Integration test** for the create endpoint: submitting `Content` containing a script tag returns a persisted `Article` whose stored `Content` has the script tag removed (end-to-end proof sanitization is actually wired into `Article.Create`, not just unit-tested in isolation).

---

## Phase 2 — UI: `/news/new` create page

- New Blazor page under `Neba.Website.Server/News/` at route `"/news/new"`.
- Form fields: Title, Slug (see below), Content, PublicationStatus, PublishDateUtc. No tournament field rendered yet — always submits `null`.
- Gated behind `CanManageArticles` (same policy as the delete button today).

### Slug field — placeholder-driven, not value-driven

- The Slug textbox's **bound value stays empty** unless the user explicitly types into it. As the user types the Title, the would-be slug is recomputed (mirroring `Article.NormalizeSlug` — lowercase, alphanumeric runs joined by single hyphens, no leading/trailing hyphen) and rendered as the Slug field's **placeholder text**, not its value.
- If the computed slug looks right, the user does nothing — the field stays empty, and the form submits `Slug: null`, letting the API derive the slug from `Title` itself (same code path, same normalization, single source of truth).
- If the user wants something different, they type into the field; that typed value becomes the bound value and is sent verbatim as `Slug` in the command, same as today.
- **As built: the preview is computed server-side in C#** (`CreateArticle.razor`'s own `NormalizeSlug`, mirroring `Article.NormalizeSlug`), recomputed on Blazor Server's interactive per-keystroke round-trip — not client-side JS as originally sketched here. Accepted as-is: this is a low-traffic admin-only page, so the extra round-trip per keystroke isn't a real cost, and it avoids introducing a JS-side reimplementation of the normalization rule (one fewer place the algorithm can drift from `Article.NormalizeSlug`).

### Content field — rich text editor

- **Quill** (not Tiptap, not MudBlazor) — this repo has no JS bundler for runtime code (`wwwroot/js/*.js` are plain scripts; webpack/vite aren't in play, jest/stryker are dev/test-only), which rules out Tiptap's module-bundled packages. Quill ships as a single script + stylesheet, needs no build step, and comes with its own toolbar UI out of the box. Pulling in a full component library like MudBlazor for one field isn't worth the dependency footprint. Loaded globally via CDN script/stylesheet tags in `App.razor` (same pattern as the Azure Maps SDK), not as an npm dependency.
- **`RichTextEditor.razor` — done.** Lives in `Neba.Website.Server/Components/`, following this codebase's colocated-JS convention (`Components/RichTextEditor.razor.js`, imported via `JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/RichTextEditor.razor.js")` — matching `NebaMap.razor`/`NebaModal.razor`/etc.), **not** the `wwwroot/js/rich-text-editor.js` path originally sketched above. Generic `Value`/`ValueChanged` two-way binding (not `Content`-specific, so it's reusable), plus `ReadOnly`/`Placeholder`/`CssClass` parameters. The JS module keys Quill instances by container id in a `Map`, so multiple editors can coexist on one page. `NewsDetail.razor:94` already renders `Content` as `(MarkupString)_article.Content`, so Quill's HTML output slots in with no format conversion once the create/edit form binds `Value` to `Content`.
- Verified via a temporary `/test-component` page (`Pages/ComponentTest.razor`) exercising: typed-content two-way binding, read-only toggle, programmatic `Value` overwrite (`setHtml` path), two simultaneous instances (proves no cross-instance state leakage), and mount/unmount (proves clean dispose). **Kept around past Phase 2** — Phase 3 extends this same component with inline image embedding (see below), so this page stays until that work is verified too; delete it only once Phase 3's image-embedding flow is confirmed working end-to-end.
- **Server-side HTML sanitization is already handled** — see Phase 1b above. `Article.Create` sanitizes `Content` before persisting regardless of which UI submits it, so nothing further is needed here once this page binds `Value` to `Content` and submits it as-is.

#### Tests

- **`RichTextEditor.razor` (bUnit, `Neba.Website.Tests`)**:
  - Renders the Quill container element and initializes JS interop on first render (`JSInterop.VerifyInvoke("initRichTextEditor")`), not on subsequent re-renders.
  - Two-way binding: pushing a value into the component's `Content`/`Value` parameter calls the JS `setHtml` interop method; simulating a JS-side change callback (`[JSInvokable]` update) updates the bound value and raises `ValueChanged`.
  - Component disposal invokes the JS `dispose` call (`JSInterop.VerifyInvoke("disposeRichTextEditor")`) so no orphaned Quill instance/listener survives navigation away from the page.
  - Renders read-only/disabled state correctly if the component supports it (relevant if reused later on an edit page).
  - Initial value round-trip: given a non-empty starting `Content`, the JS `setHtml` call receives that exact value.
- **`rich-text-editor.js` (Jest, matches existing `wwwroot/js/*.tests.js` pattern e.g. `breakpoints.tests.js`)**:
  - `init` creates a Quill instance bound to the given container and returns/exposes a handle usable by `getHtml`/`setHtml`/`dispose`.
  - `getHtml` returns the current editor HTML exactly as Quill produces it (`.root.innerHTML`).
  - `setHtml` replaces editor content and `getHtml` reflects the new value immediately after.
  - `dispose` tears down the Quill instance/DOM listeners cleanly — calling `dispose` twice, or calling `getHtml`/`setHtml` after `dispose`, does not throw.
  - Editor content changes fire the expected `.NET` invoke callback (mock `DotNet.invokeMethodAsync`) with the updated HTML.
- Sanitization tests are covered under Phase 1b, not repeated here.

### Deferred to a later sub-phase (tournament linking)

- Page accepts an optional route/query parameter (e.g. `?tournamentId=`) so a tournament-portal context can deep-link into `/news/new` with the tournament pre-selected and the picker hidden; outside that context, a dropdown/picker appears.
- Data source for the picker is undecided — `ListTournamentsInSeason` (`Neba.Api/Features/Tournaments/ListTournamentsInSeason/`) is a plausible fit, but scope (all seasons? current season only? active/upcoming only?) needs a decision when this sub-phase starts.

---

## Phase 3 — Header image + attachments

Hard constraint: **UI never talks to blob storage directly, only through the API.**

Split into two sub-phases, each landing as its own PR: **3a (API)** — upload endpoints, staging/orphan-tracking infrastructure, and the `CreateArticleCommand` changes that claim staged uploads — and **3b (UI)** — the `FileUpload` component, inline image embedding in the rich text editor, and the attachments list. 3a is fully detailed below; 3b keeps its existing high-level design (detailed pass deferred).

Architecture decisions that span both sub-phases:

- **Two separate upload endpoints** — header image and attachment are differentiated by *route*, not by a discriminator field on a shared endpoint, matching this codebase's one-use-case-per-folder REPR convention (and letting each enforce its own validation independently). Both live under the existing `news` group (`Group<NewsEndpointGroup>()`), consistent with List/Get/Delete/Create Article. Neither requires an `ArticleId`.
- **Single file per request, even though the UI's attachment picker allows selecting multiple files.** The `FileUpload` component (3b) uploads each selected file as its own request the moment it's picked — a multi-file selection just means N requests fired back-to-back, one per file, each independently tracked through `Uploading → Uploaded/Failed`. This keeps both endpoints' request/response shape identical to the single-header-image case and avoids a partial-batch-failure story (file 3 of 5 fails, do the other 4 still count?) that a true batch endpoint would have to solve.
- **Files stay at their upload path forever — they are never moved to a per-article folder once the Article saves.** `Article.HeaderImage`/`ArticleAttachment.File` simply store the `news/uploads/header/{ulid}-{filename}` or `news/uploads/attachments/{ulid}-{filename}` path permanently; there is no `news/{articleId}/...` reorganization step. This was a deliberate choice over moving files post-save:
  - Azure Blob Storage has no real folders — paths are just prefixes used for browsing in the portal/CLI. Grouping by `ArticleId` is a cosmetic convenience only, not a functional requirement, since the app always locates a file via the `StoredFile` (Container/Path) stored on the entity, never by directory listing.
  - A move requires a copy+delete (blobs can't be renamed atomically) plus updating the `StoredFile` path on the entity before it's persisted, and introduces a real partial-failure mode to handle (copy succeeds/delete fails → harmless orphan, already covered by the cleanup job below; copy fails outright → must fall back to the original path rather than fail the whole article save).
  - None of that complexity buys anything the app actually needs, so we don't do it. If per-article organization in blob storage ever becomes a real requirement (not just tidiness), this is the section to revisit.
- UI uploads each file **as soon as it's selected**, not on form submit — perceived save stays fast. The final `CreateArticleCommand` carries the already-returned `StoredFile` pointers: one for `HeaderImage`, a list of `{DisplayName, StoredFile, IsInline}` for `Attachments`. `Article.Create`/`AddAttachment` consume them exactly as `ArticleAttachment.Create` already does today.
- **Save-while-uploading is gated client-side, not server-side.** The create form tracks each selected file's upload as `Uploading` → `Uploaded(StoredFile)` / `Failed(error)`. The Save button is disabled (with an inline "Uploading N of M files…" indicator) while anything is `Uploading`, and stays disabled on `Failed` until removed/retried. This guarantees `CreateArticleCommand` is only ever built once every `StoredFile` pointer it needs already exists — the handler never has to special-case an in-flight or missing upload. A user who abandons the page mid-upload just produces an orphaned blob, handled by the cleanup job below like any other abandoned upload.

---

## Phase 3a — API

Three parts, in dependency order: (1) shared upload infrastructure both endpoints sit on top of, (2) the header image endpoint, (3) the attachment endpoint. A fourth section covers the `CreateArticleCommand` changes that let a saved `Article` claim the `StoredFile`s these endpoints hand back.

### Part 1 — Shared upload infrastructure

Both endpoints need the same three things: somewhere to stage the blob + bookkeeping row, a place for the size/content-type validation predicates so the two validators don't duplicate the same boundary logic with copy-pasted drift risk, and a shared response shape. This is a genuine shared dependency (not a premature abstraction over two call sites) because the orphan-cleanup contract — "every successful upload gets a `PendingArticleUpload` row, every claim removes it" — has to hold identically for both endpoints, and duplicating that insert/claim logic is exactly the kind of thing that silently drifts (one endpoint gets a bug fix, the other doesn't).

All new files live under `Neba.Api/Features/News/Uploads/` (new folder, sibling to `CreateArticle`/`DeleteArticle`/`Domain`).

**`PendingArticleUpload.cs`** — bookkeeping row, not a domain aggregate. Natural key is `(Container, Path)`, the same two fields already needed to construct a `StoredFile`, so no separate id needs to travel through the UI → API round trip:

```csharp
namespace Neba.Api.Features.News.Uploads;

internal sealed class PendingArticleUpload
{
    public required string Container { get; init; }

    public required string Path { get; init; }

    public required DateTimeOffset UploadedAtUtc { get; init; }
}
```

**`PendingArticleUploadConfiguration.cs`** — table `pending_article_uploads`, shadow int PK (matches every other entity in this codebase), unique index on the natural key:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Database;

namespace Neba.Api.Features.News.Uploads;

internal sealed class PendingArticleUploadConfiguration : IEntityTypeConfiguration<PendingArticleUpload>
{
    public void Configure(EntityTypeBuilder<PendingArticleUpload> builder)
    {
        builder.ToTable("pending_article_uploads");
        builder.ConfigureShadowId();

        builder.Property(p => p.Container)
            .HasColumnName("container")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(p => p.Path)
            .HasColumnName("path")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(p => p.UploadedAtUtc)
            .HasColumnName("uploaded_at_utc")
            .IsRequired();

        builder.HasIndex(p => new { p.Container, p.Path }).IsUnique();
    }
}
```

Add `internal DbSet<PendingArticleUpload> PendingArticleUploads => Set<PendingArticleUpload>();` to `AppDbContext`, and generate the EF migration.

**`FileUploadValidationRules.cs`** — the two predicates both validators need. Deliberately not tied to either endpoint's specific allowlist/cap so each validator supplies its own constants:

```csharp
namespace Neba.Api.Features.News.Uploads;

internal static class FileUploadValidationRules
{
    internal static bool HasAllowedContentType(string? contentType, IReadOnlySet<string> allowedContentTypes)
        => contentType is not null && allowedContentTypes.Contains(contentType);

    internal static bool IsWithinSizeLimit(long lengthInBytes, long maxSizeBytes)
        => lengthInBytes > 0 && lengthInBytes <= maxSizeBytes;
}
```

**`IArticleUploadStagingService.cs` / `ArticleUploadStagingService.cs`** — the one piece both endpoints actually call: builds the blob path, streams the upload through `IFileStorageService` (never buffers the whole file into a `byte[]`), inserts the `PendingArticleUpload` row, and hands back a `StoredFile` pointer. `pathPrefix` is `"header"` or `"attachments"`, matching the two path conventions above.

```csharp
using Microsoft.AspNetCore.Http;

using Neba.Api.Database;
using Neba.Api.Features.Storage.Domain;
using Neba.Application.Storage;
using Neba.Api.Time;

namespace Neba.Api.Features.News.Uploads;

internal interface IArticleUploadStagingService
{
    Task<StoredFile> StageUploadAsync(IFormFile file, string pathPrefix, CancellationToken cancellationToken);
}

internal sealed class ArticleUploadStagingService(
    IFileStorageService fileStorageService,
    AppDbContext appDbContext,
    IDateTimeProvider dateTimeProvider)
    : IArticleUploadStagingService
{
    private const string ContainerName = "news";

    public async Task<StoredFile> StageUploadAsync(IFormFile file, string pathPrefix, CancellationToken cancellationToken)
    {
        var path = $"news/uploads/{pathPrefix}/{Ulid.NewUlid()}-{file.FileName}";

        await using (var stream = file.OpenReadStream())
        {
            await fileStorageService.UploadFileAsync(ContainerName, path, stream, file.ContentType, cancellationToken);
        }

        appDbContext.PendingArticleUploads.Add(new PendingArticleUpload
        {
            Container = ContainerName,
            Path = path,
            UploadedAtUtc = dateTimeProvider.UtcNow
        });
        await appDbContext.SaveChangesAsync(cancellationToken);

        return new StoredFile
        {
            Container = ContainerName,
            Path = path,
            ContentType = file.ContentType,
            SizeInBytes = file.Length
        };
    }
}
```

Registered in DI the same way as the feature's other internal services (Scrutor scan picking up `IArticleUploadStagingService` → `ArticleUploadStagingService`, alongside `IFileStorageService` → `AzureBlobStorageService`).

**`UploadedFileResponse.cs`** (`Neba.Api.Contracts/News/Uploads/`) — the shared response shape for both endpoints, flattening `StoredFile`'s fields (Contracts can't reference the domain type, same rule as `AttachmentInput` below):

```csharp
namespace Neba.Api.Contracts.News.Uploads;

/// <summary>
/// The stored-file pointer returned after a successful header image or attachment upload.
/// </summary>
public sealed record UploadedFileResponse
{
    /// <summary>
    /// The blob storage container the file was uploaded to.
    /// </summary>
    public required string Container { get; init; }

    /// <summary>
    /// The blob path within the container.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// The file's MIME content type as uploaded.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// The file's size in bytes.
    /// </summary>
    public required long SizeInBytes { get; init; }
}
```

**Orphan cleanup job** — the other half of the staging contract (every staged row eventually either gets claimed by a saved `Article`, or swept). Claiming is covered in Part 4 below (it happens in `CreateArticleCommandHandler`, not here); this is just the sweep side, following the same recurring-Hangfire-job pattern as `SyncDocumentToStorageJob`:

```csharp
namespace Neba.Api.Features.News.Uploads;

internal sealed record CleanupOrphanedArticleUploadsJob;
```

```csharp
using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Messaging;
using Neba.Api.Time;
using Neba.Application.Storage;

namespace Neba.Api.Features.News.Uploads;

internal sealed class CleanupOrphanedArticleUploadsJobHandler(
    AppDbContext appDbContext,
    IFileStorageService fileStorageService,
    IDateTimeProvider dateTimeProvider,
    ILogger<CleanupOrphanedArticleUploadsJobHandler> logger)
    : IBackgroundJobHandler<CleanupOrphanedArticleUploadsJob>
{
    private static readonly TimeSpan OrphanThreshold = TimeSpan.FromHours(24);

    public async Task HandleAsync(CleanupOrphanedArticleUploadsJob job, CancellationToken cancellationToken)
    {
        var cutoffUtc = dateTimeProvider.UtcNow - OrphanThreshold;

        var orphans = await appDbContext.PendingArticleUploads
            .Where(p => p.UploadedAtUtc < cutoffUtc)
            .ToListAsync(cancellationToken);

        foreach (var orphan in orphans)
        {
            var deleted = await fileStorageService.DeleteAsync(orphan.Container, orphan.Path, cancellationToken);

            if (!deleted)
            {
                logger.LogWarning(
                    "Failed to delete orphaned article upload blob {Container}/{Path}; leaving PendingArticleUpload row for the next sweep.",
                    orphan.Container,
                    orphan.Path);
                continue;
            }

            appDbContext.PendingArticleUploads.Remove(orphan);
        }

        await appDbContext.SaveChangesAsync(cancellationToken);
    }
}
```

Registered as a recurring job (`"0 * * * *"` — hourly; a stale-by-up-to-an-hour orphan is cheap, and this avoids a bigger sweep backlog than the once-a-month document sync job needs) from `NewsConfiguration`, mirroring `DocumentsConfiguration.UseDocumentSyncJobs()`. Threshold of 24 hours is a starting point — long enough that a slow upload or a user still filling out the rest of the form isn't punished, short enough that orphaned blobs don't pile up — revisit once real usage is observed.

### Part 2 — Header image upload endpoint

`Neba.Api/Features/News/UploadArticleHeaderImage/` (new folder). Route `POST news/header-image`, path prefix `"header"`, restricted to image content types, capped at 5 MB.

The request DTO carries a raw `IFormFile` and therefore lives in `Neba.Api` only (the endpoint layer) — it is **not** a `Neba.Api.Contracts` type, since `Neba.Api.Contracts` is shared with the Blazor client and `IFormFile` is a server-side ASP.NET Core type with nothing to serialize across the wire. The client instead builds a Refit multipart request directly (`StreamPart`), calling into the shared `UploadedFileResponse` for the response side only.

**`UploadArticleHeaderImageRequest.cs`**

```csharp
using Microsoft.AspNetCore.Http;

namespace Neba.Api.Features.News.UploadArticleHeaderImage;

internal sealed class UploadArticleHeaderImageRequest
{
    public IFormFile File { get; init; } = default!;
}
```

**`UploadArticleHeaderImageRequestValidator.cs`**

```csharp
using FastEndpoints;

using FluentValidation;

using Neba.Api.Features.News.Uploads;

namespace Neba.Api.Features.News.UploadArticleHeaderImage;

internal sealed class UploadArticleHeaderImageRequestValidator
    : Validator<UploadArticleHeaderImageRequest>
{
    private const long MaxSizeBytes = 5 * 1024 * 1024;

    private static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    public UploadArticleHeaderImageRequestValidator()
    {
        RuleFor(r => r.File)
            .NotNull()
            .WithErrorCode("UploadArticleHeaderImageRequest.FileRequired")
            .WithMessage("A file is required.");

        RuleFor(r => r.File.ContentType)
            .Must(contentType => FileUploadValidationRules.HasAllowedContentType(contentType, AllowedContentTypes))
            .WithErrorCode("UploadArticleHeaderImageRequest.ContentTypeNotAllowed")
            .WithMessage("Header image must be JPEG, PNG, WebP, or GIF.")
            .When(r => r.File is not null);

        RuleFor(r => r.File.Length)
            .Must(length => FileUploadValidationRules.IsWithinSizeLimit(length, MaxSizeBytes))
            .WithErrorCode("UploadArticleHeaderImageRequest.FileTooLarge")
            .WithMessage("Header image must be 5 MB or smaller.")
            .When(r => r.File is not null);
    }
}
```

**`UploadArticleHeaderImageEndpoint.cs`**

```csharp
using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.News.Uploads;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.News.UploadArticleHeaderImage;

internal sealed class UploadArticleHeaderImageEndpoint(IArticleUploadStagingService stagingService)
    : Endpoint<UploadArticleHeaderImageRequest, UploadedFileResponse>
{
    public override void Configure()
    {
        Post("header-image");
        Group<NewsEndpointGroup>();
        AllowFileUploads();

        Options(options => options
            .WithVersionSet("News")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CreateArticle.PolicyName);

        Description(description => description
            .WithName("UploadArticleHeaderImage")
            .WithTags("Admin")
            .Produces<UploadedFileResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest));
    }

    public override async Task HandleAsync(UploadArticleHeaderImageRequest req, CancellationToken ct)
    {
        var storedFile = await stagingService.StageUploadAsync(req.File, "header", ct);

        var response = new UploadedFileResponse
        {
            Container = storedFile.Container,
            Path = storedFile.Path,
            ContentType = storedFile.ContentType,
            SizeInBytes = storedFile.SizeInBytes
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}
```

**`UploadArticleHeaderImageSummary.cs`**

```csharp
using FastEndpoints;

using Neba.Api.Contracts.News.Uploads;

namespace Neba.Api.Features.News.UploadArticleHeaderImage;

internal sealed class UploadArticleHeaderImageSummary : Summary<UploadArticleHeaderImageEndpoint>
{
    public UploadArticleHeaderImageSummary()
    {
        Summary = "Uploads a news article header image.";
        Description = "Stages an image file in blob storage ahead of article creation and returns a pointer to it. The pointer is orphaned (and later swept) unless it's included as HeaderImage in a subsequent CreateArticle command. Requires the News.CreateArticle permission.";

        Response<UploadedFileResponse>(200, "File uploaded.");
        Response(400, "File missing, wrong content type, or too large.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the News.CreateArticle permission.");
    }
}
```

**`INewsApi.cs`** — add (Refit multipart, not a JSON body):

```csharp
using Neba.Api.Contracts.News.Uploads;
// ... existing usings ...

public interface INewsApi
{
    // ... existing members ...

    /// <summary>
    /// Uploads a news article header image. Requires the News.CreateArticle permission.
    /// </summary>
    [Multipart]
    [Post("/news/header-image")]
    Task<IApiResponse<UploadedFileResponse>> UploadArticleHeaderImageAsync(
        [AliasAs("File")] StreamPart file,
        CancellationToken cancellationToken = default);
}
```

### Part 3 — Attachment upload endpoint

`Neba.Api/Features/News/UploadArticleAttachment/` (new folder). Route `POST news/attachments`, path prefix `"attachments"`, broader allowlist, capped at 25 MB. Structurally identical to Part 2 — only the route, allowlist, and cap differ — so only the deltas are shown in full.

**`UploadArticleAttachmentRequest.cs`** — same shape as the header image request:

```csharp
using Microsoft.AspNetCore.Http;

namespace Neba.Api.Features.News.UploadArticleAttachment;

internal sealed class UploadArticleAttachmentRequest
{
    public IFormFile File { get; init; } = default!;
}
```

**`UploadArticleAttachmentRequestValidator.cs`** — same structure as `UploadArticleHeaderImageRequestValidator`, different constants:

```csharp
using FastEndpoints;

using FluentValidation;

using Neba.Api.Features.News.Uploads;

namespace Neba.Api.Features.News.UploadArticleAttachment;

internal sealed class UploadArticleAttachmentRequestValidator
    : Validator<UploadArticleAttachmentRequest>
{
    private const long MaxSizeBytes = 25 * 1024 * 1024;

    private static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    public UploadArticleAttachmentRequestValidator()
    {
        RuleFor(r => r.File)
            .NotNull()
            .WithErrorCode("UploadArticleAttachmentRequest.FileRequired")
            .WithMessage("A file is required.");

        RuleFor(r => r.File.ContentType)
            .Must(contentType => FileUploadValidationRules.HasAllowedContentType(contentType, AllowedContentTypes))
            .WithErrorCode("UploadArticleAttachmentRequest.ContentTypeNotAllowed")
            .WithMessage("Attachment must be a PDF, Word/Excel document, or JPEG/PNG/WebP/GIF image.")
            .When(r => r.File is not null);

        RuleFor(r => r.File.Length)
            .Must(length => FileUploadValidationRules.IsWithinSizeLimit(length, MaxSizeBytes))
            .WithErrorCode("UploadArticleAttachmentRequest.FileTooLarge")
            .WithMessage("Attachment must be 25 MB or smaller.")
            .When(r => r.File is not null);
    }
}
```

**`UploadArticleAttachmentEndpoint.cs`** — identical to `UploadArticleHeaderImageEndpoint` except the route and path prefix:

```csharp
using Asp.Versioning;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.News.Uploads;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.News.UploadArticleAttachment;

internal sealed class UploadArticleAttachmentEndpoint(IArticleUploadStagingService stagingService)
    : Endpoint<UploadArticleAttachmentRequest, UploadedFileResponse>
{
    public override void Configure()
    {
        Post("attachments");
        Group<NewsEndpointGroup>();
        AllowFileUploads();

        Options(options => options
            .WithVersionSet("News")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CreateArticle.PolicyName);

        Description(description => description
            .WithName("UploadArticleAttachment")
            .WithTags("Admin")
            .Produces<UploadedFileResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest));
    }

    public override async Task HandleAsync(UploadArticleAttachmentRequest req, CancellationToken ct)
    {
        var storedFile = await stagingService.StageUploadAsync(req.File, "attachments", ct);

        var response = new UploadedFileResponse
        {
            Container = storedFile.Container,
            Path = storedFile.Path,
            ContentType = storedFile.ContentType,
            SizeInBytes = storedFile.SizeInBytes
        };

        // Stryker disable once Statement
        await Send.OkAsync(response, ct);
    }
}
```

**`UploadArticleAttachmentSummary.cs`**

```csharp
using FastEndpoints;

using Neba.Api.Contracts.News.Uploads;

namespace Neba.Api.Features.News.UploadArticleAttachment;

internal sealed class UploadArticleAttachmentSummary : Summary<UploadArticleAttachmentEndpoint>
{
    public UploadArticleAttachmentSummary()
    {
        Summary = "Uploads a news article attachment (or inline embedded image).";
        Description = "Stages a file in blob storage ahead of article creation and returns a pointer to it. Used both for regular downloadable attachments and for images embedded inline in the article body via the rich text editor — the distinction (IsInline) is supplied later, when the pointer is included in the CreateArticle command's Attachments list. The pointer is orphaned (and later swept) unless it's claimed by a subsequent CreateArticle command. Requires the News.CreateArticle permission.";

        Response<UploadedFileResponse>(200, "File uploaded.");
        Response(400, "File missing, wrong content type, or too large.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the News.CreateArticle permission.");
    }
}
```

**`INewsApi.cs`** — add:

```csharp
/// <summary>
/// Uploads a news article attachment (or inline embedded image). Requires the News.CreateArticle permission.
/// </summary>
[Multipart]
[Post("/news/attachments")]
Task<IApiResponse<UploadedFileResponse>> UploadArticleAttachmentAsync(
    [AliasAs("File")] StreamPart file,
    CancellationToken cancellationToken = default);
```

### Part 4 — Command shape and claiming staged uploads

Extends the `CreateArticleCommand`/`Article.Create` from Phase 1 to accept the `StoredFile` pointers these endpoints hand back, and claims them (deletes the matching `PendingArticleUpload` rows) as part of the same save.

**Layer boundary matters for naming, not just placement.** `Neba.Api.Contracts` is shared with `Neba.Website` (Blazor), so its types must never reference a domain type — `AttachmentInput` below is deliberately flattened to raw primitives. The command lives in `Neba.Api` (application layer) and *can* reference the domain `StoredFile` value object directly, since it never crosses the wire. To avoid the nested command-level type looking like another wire-shaped "Input" DTO, it's named `NewArticleAttachment` rather than `ArticleAttachmentInput` — same data, but the name signals "application-layer, may hold domain types" instead of "contract-layer, primitives only."

**`CreateArticleCommand.cs`** — adds `HeaderImage`/`Attachments`:

```csharp
internal sealed record CreateArticleCommand
    : ICommand<CreatedArticle>
{
    public required string Title { get; init; }

    public string? Slug { get; init; }

    public required string Content { get; init; }

    public required PublicationStatus Status { get; init; }

    public required DateTimeOffset PublishDateUtc { get; init; }

    public TournamentId? TournamentId { get; init; }

    public StoredFile? HeaderImage { get; init; }

    public IReadOnlyCollection<NewArticleAttachment> Attachments { get; init; } = [];
}

internal sealed record NewArticleAttachment(string DisplayName, bool IsInline, StoredFile File);

internal sealed record CreatedArticle(ArticleId ArticleId, string Slug);
```

`StoredFile` (`Neba.Api.Features.Storage.Domain`) is already a plain `sealed record` (`Container`, `Path`, `ContentType`, `SizeInBytes`, no factory/invariants) — the command carries it directly rather than inventing a parallel shape.

**`Article.cs`** — `Create` gains a `StoredFile? headerImage` parameter, and a new `AddAttachment` method mirrors the always-valid aggregate-assignment pattern from this doc's Architecture Rules (delegates entity validation to `ArticleAttachment.Create`, only enforces its own invariants):

```csharp
internal static ErrorOr<Article> Create(
    string title,
    string? slug,
    string content,
    PublicationStatus status,
    DateTimeOffset publishDateUtc,
    TournamentId? tournamentId,
    StoredFile? headerImage)
{
    // ... existing title/content/sanitization/slug validation unchanged (Phase 1 + 1b) ...

    return new Article
    {
        Id = ArticleId.New(),
        Title = title,
        Slug = normalizedSlug,
        Content = sanitizedContent,
        PublicationStatus = status,
        PublishDateUtc = publishDateUtc,
        TournamentId = tournamentId,
        HeaderImage = headerImage
    };
}

internal ErrorOr<Success> AddAttachment(string displayName, bool isInline, StoredFile file)
{
    var attachment = ArticleAttachment.Create(displayName, isInline, file);

    if (attachment.IsError)
    {
        return attachment.Errors;
    }

    _attachments.Add(attachment.Value);
    return Result.Success;
}
```

**`CreateArticleCommandHandler.cs`** — after `Article.Create` succeeds, adds each attachment, then claims every referenced `StoredFile`'s staging row before the single `SaveChangesAsync`:

```csharp
internal sealed class CreateArticleCommandHandler(AppDbContext appDbContext, IFusionCache cache)
    : ICommandHandler<CreateArticleCommand, CreatedArticle>
{
    public async Task<ErrorOr<CreatedArticle>> HandleAsync(CreateArticleCommand command, CancellationToken cancellationToken)
    {
        // ... existing TournamentId existence check + slug uniqueness check unchanged ...

        var article = Article.Create(
            command.Title,
            command.Slug,
            command.Content,
            command.Status,
            command.PublishDateUtc,
            command.TournamentId,
            command.HeaderImage);

        if (article.IsError)
        {
            return article.Errors;
        }

        foreach (var attachment in command.Attachments)
        {
            var added = article.Value.AddAttachment(attachment.DisplayName, attachment.IsInline, attachment.File);

            if (added.IsError)
            {
                return added.Errors;
            }
        }

        appDbContext.Articles.Add(article.Value);

        var claimedFiles = command.Attachments
            .Select(a => a.File)
            .Concat(command.HeaderImage is null ? [] : [command.HeaderImage])
            .ToList();

        foreach (var file in claimedFiles)
        {
            var pending = await appDbContext.PendingArticleUploads
                .FirstOrDefaultAsync(p => p.Container == file.Container && p.Path == file.Path, cancellationToken);

            if (pending is not null)
            {
                appDbContext.PendingArticleUploads.Remove(pending);
            }
        }

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("neba:news:articles", token: cancellationToken);

        return new CreatedArticle(article.Value.Id, article.Value.Slug);
    }
}
```

A missing `PendingArticleUpload` row (`pending is null`) is not an error — it just means the row was already swept by the cleanup job (upload sat unclaimed past the 24-hour threshold) or, for a resubmitted form, was already claimed by an earlier attempt. Either way the `StoredFile` pointer itself is still valid; only the bookkeeping row is gone, and the `Article` save proceeds normally.

**Contract-level (`Neba.Api.Contracts/News/CreateArticle/ArticleInput.cs`)** — flattens `StoredFile`'s fields and adds `HeaderImage`/`Attachments` to `ArticleInput`:

```csharp
public sealed record ArticleInput
{
    // ... existing Title, Slug, Content, PublicationStatus, PublishDateUtc, TournamentId unchanged ...

    /// <summary>
    /// The pointer returned by a prior POST /news/header-image call, or null for no header image.
    /// </summary>
    public HeaderImageInput? HeaderImage { get; init; }

    /// <summary>
    /// Pointers returned by prior POST /news/attachments calls, one per attachment/inline image.
    /// </summary>
    public IReadOnlyCollection<AttachmentInput> Attachments { get; init; } = [];
}

public sealed record HeaderImageInput
{
    public required string Container { get; init; }
    public required string Path { get; init; }
    public required string ContentType { get; init; }
    public required long SizeInBytes { get; init; }
}

public sealed record AttachmentInput
{
    public required string DisplayName { get; init; }
    public required bool IsInline { get; init; }
    public required string Container { get; init; }
    public required string Path { get; init; }
    public required string ContentType { get; init; }
    public required long SizeInBytes { get; init; }
}
```

The endpoint maps `req.Input.HeaderImage`/each `AttachmentInput` into `StoredFile`/`NewArticleAttachment`, e.g. `input.Attachments.Select(a => new NewArticleAttachment(a.DisplayName, a.IsInline, new StoredFile { Container = a.Container, Path = a.Path, ContentType = a.ContentType, SizeInBytes = a.SizeInBytes }))`.

### Tests (Phase 3a)

- **`FileUploadValidationRules` unit tests**: content type allowed/not-allowed (including null content type), size within/at/over the limit (boundary at exactly `maxSizeBytes`), zero-length file rejected.
- **`ArticleUploadStagingService` unit tests**: builds the expected `news/uploads/{prefix}/{ulid}-{filename}` path, calls `IFileStorageService.UploadFileAsync` with the file's stream/content type (not a buffered copy — assert the stream reference, not a byte array), inserts exactly one `PendingArticleUpload` row with `UploadedAtUtc` from `IDateTimeProvider`, returns a `StoredFile` matching the uploaded blob.
- **`UploadArticleHeaderImageRequestValidator` / `UploadArticleAttachmentRequestValidator` unit tests**: each allowed content type passes, a disallowed content type fails, file at/under the cap passes, file over the cap fails, null file fails.
- **`UploadArticleHeaderImageEndpoint` / `UploadArticleAttachmentEndpoint` unit tests** (`Factory.Create<TEndpoint>()`): success path stages via `IArticleUploadStagingService` and returns 200 with the mapped `UploadedFileResponse`.
- **Integration tests** for both endpoints: 200 + response shape + a `PendingArticleUpload` row actually persisted on success; 400 for disallowed content type and oversized file; 401/403 for missing/insufficient permission. Remember the FastEndpoints/Hangfire static-state pitfalls documented in `CLAUDE.md`'s Learnings section if these spin up a real `WebApplication`.
- **`CreateArticleCommandHandler` unit tests** — add to the existing test class: success with a `HeaderImage` and multiple `Attachments` persists them all and returns `CreatedArticle`; a `AddAttachment` validation failure (e.g. blank `DisplayName`) short-circuits before `SaveChangesAsync`; claiming removes the matching `PendingArticleUpload` rows for both `HeaderImage` and each attachment's `File`; a claim with no matching pending row (already swept/already claimed) does not throw and still succeeds.
- **`CleanupOrphanedArticleUploadsJobHandler` unit tests**: rows older than the threshold are deleted (blob + row); rows within the threshold are left alone; a blob delete failure leaves the row in place and logs a warning rather than throwing.
- **Integration test for claiming**: `CreateArticleEndpointIntegrationTests` gains a case that uploads a header image via `UploadArticleHeaderImageEndpoint` first, includes the returned pointer in the `CreateArticle` request, and asserts the `PendingArticleUpload` row is gone after the create succeeds.

---

## Phase 3b — UI

### `FileUpload.razor` component (day 1 feature set)

Considered adopting MudBlazor's `MudFileUpload` for its drag-drop/multi-file/preview UX, but rejected for the same reason Phase 2 rejected Tiptap for `RichTextEditor`: this repo has no JS bundler, and pulling in a full component library for one control isn't worth the dependency/theming footprint. Instead, one shared `FileUpload.razor` (parameterized by `MaxFiles`, `Accept`, `MaxFileSizeBytes`) is used for both the header image (`MaxFiles=1`, `Accept="image/*"`) and attachments (`MaxFiles>1`, broader `Accept`), following the same colocated-JS-module pattern already established by `RichTextEditor.razor`/`RichTextEditor.razor.js` (`IJSObjectReference` import, `DotNetObjectReference` callback, explicit `dispose` on teardown).

Four UX features are in scope for day 1, each with a different amount of API impact:

- **Drag-and-drop** — client-only. `InputFile` has no native drop support, so a JS-managed drop zone `<div>` overlays a hidden `<input type="file">`; on `drop`, JS builds a `DataTransfer`, assigns it to the input's `.files`, and dispatches a `change` event so Blazor's `InputFile.OnChange` fires exactly as it would for a click-to-browse selection. No API change.
- **Multi-file select** (attachments only) — client-only. `<InputFile multiple>` + `e.GetMultipleFiles(MaxFiles)`. On the API side this is why the attachments endpoint stays single-file-per-request (see above) — the multiplicity is entirely a client-side fan-out of individual upload calls, never a batch request the API has to parse.
- **Image thumbnail preview** — client-only, and deliberately not round-tripped through the API. The JS module calls `URL.createObjectURL(file)` on the raw browser `File` at selection/drop time and renders it directly, so the preview appears before the upload even starts and never depends on it succeeding.
- **Per-file upload progress** — the one feature with real API-side consequence, covered above: the component wraps `IBrowserFile.OpenReadStream()` in a progress-reporting `Stream` and pipes it straight into the outbound multipart request rather than buffering the file first, so the endpoint must consume `IFormFile` via streaming (`OpenReadStream()`), not a fully-buffered `byte[]`, to keep progress meaningful and memory flat.

Deferred (explicitly not day 1): clipboard paste-to-upload, attachment reordering, chunked/resumable upload, client-side image compression before upload.

### Inline images embedded in the article body

This **modifies the already-built `RichTextEditor.razor`/`RichTextEditor.razor.js` from Phase 2** rather than introducing a new component. `NewArticleAttachment.IsInline` already models "this file is shown inline in the article" — an image embedded in the article body via the editor is simply an attachment with `IsInline: true`, reusing `UploadArticleAttachmentEndpoint`'s validation/size-cap/orphan-tracking wholesale. No new endpoint or table.

- **Quill's default image button is not used.** Out of the box it base64-encodes the picked file and embeds it directly as `<img src="data:image/png;base64,...">` — no upload, no blob storage, and it turns `Content` itself into the file store (bloats the DB row, ~33% larger than the binary, no size cap). That violates the "UI never talks to blob storage directly" constraint in spirit (nothing is uploaded at all) and is explicitly disabled.
- **`RichTextEditor.razor` gains an optional parameter**, keeping the component itself News-agnostic:

  ```csharp
  [Parameter]
  public Func<IJSStreamReference, string, string, Task<string?>>? OnImageSelected { get; set; }
  ```

  Signature is `(fileStream, fileName, contentType) => Task<url-or-null>`. When null, the toolbar's image control is omitted entirely (not rendered disabled) — a bare `RichTextEditor` with no upload wiring (e.g. the `/test-component` page) simply doesn't offer image embedding, rather than silently no-op-ing.
- **JS-to-.NET file streaming** is the bridge for images the user pastes/drops/picks directly in the editor (they don't come through an `<InputFile>`, so `FileUpload.razor`'s streaming approach doesn't directly apply here): the custom Quill image handler wraps the browser `File` with `DotNet.createJSStreamReference(file)` and invokes a `[JSInvokable]` method on `RichTextEditor`, e.g. `Task<string?> RequestImageEmbedAsync(IJSStreamReference fileStream, string fileName, string contentType)`. That method calls the caller-supplied `OnImageSelected`, which the News create page implements: upload via the same code path `UploadArticleAttachmentEndpoint`/`FileUpload.razor` already use, get back a `StoredFile`, resolve a displayable blob URL, add `NewArticleAttachment(DisplayName: fileName, IsInline: true, File: storedFile)` to the page's tracked `Attachments` list, and return the URL. `RichTextEditor` then calls back into JS (`insertEmbeddedImage(containerId, url)`) to `quill.insertEmbed(range.index, 'image', url)` at the cursor position that triggered the upload.
- **Sanitizer note** (ties back to Phase 1b): `HtmlSanitizer`'s default config already permits `<img src alt>` and rejects `src="data:..."` (default `AllowedSchemes` excludes `data:`), so no config change is needed here. Since the only path that ever inserts `<img>` into `Content` is the upload-then-URL flow above, a `data:` URI appearing in submitted `Content` means something bypassed it (e.g. a raw HTML paste) — `Article.Create`'s existing sanitization already strips it.

### Attachments list UI (regular + inline attachments, unified)

- Below the `FileUpload` widget, the create page renders one flat list of everything currently tracked in `Attachments` — both files uploaded via `FileUpload` and images embedded via `RichTextEditor`'s `OnImageSelected` flow — since both are just `NewArticleAttachment` entries differing only in `IsInline`. No separate "inline images" list.
- Each row shows the display name, an **"Inline" badge/icon** when `IsInline == true` (e.g. an image icon with a "Used in article body" tooltip — same tooltip-via-`title`-attribute pattern `RichTextEditor.razor.js` already uses for its toolbar), and the same three actions regardless of `IsInline`: **Download**, **Open in new tab** (both just link to the blob URL — no special-casing), and **Delete**.
- **Deleting an inline attachment requires an extra confirmation step the non-inline delete path doesn't need.** Reuse `ConfirmActionModal` (`Neba.Website.Server/Components/ConfirmActionModal.razor`) with copy specific to inline attachments — e.g. *"This image is embedded in the article body. Removing it will leave a broken image where it appears — you'll need to edit the article body to remove or replace it."* Non-inline attachments keep today's plain delete flow.
- **Deleting does not touch `Content`'s HTML.** Automatically scrubbing the corresponding `<img>` tag out of the rich text body (so no broken image is ever visible) is deliberately **out of scope for v1** — it would require reaching into `RichTextEditor`'s live Quill document from the parent page's delete handler, matched only by URL (there's no other correlation key), which is nontrivial for the value it adds this early. The confirmation warning above is the v1 mitigation; revisit if broken embedded images turn out to be a real support burden.
- This applies just as much pre-save as post-save: a user who embeds an image, sees it render inline, then removes its row from the attachments list while still on the create form still needs the same warning — the image is already visible in the live editor regardless of whether `SaveChangesAsync` has run yet.

### Command shape for uploaded files

No PK or domain id for the `PendingArticleUpload` row needs to travel through the UI → API round trip — that row's natural key is `Container`+`Path`, the same two fields already required to build the `StoredFile`, so the claiming step (below) looks the row up by that pair instead of a separate identifier.

**Layer boundary matters for naming, not just placement.** `Neba.Api.Contracts` is shared with `Neba.Website` (Blazor), so its types must never reference a domain type — `AttachmentInput` below is deliberately flattened to raw primitives. The command lives in `Neba.Api` (application layer) and *can* reference the domain `StoredFile` value object directly, since it never crosses the wire. To avoid the nested command-level type looking like another wire-shaped "Input" DTO, it's named `NewArticleAttachment` rather than `ArticleAttachmentInput` — same data, but the name signals "application-layer, may hold domain types" instead of "contract-layer, primitives only."

Command-level (`Neba.Api/Features/News/CreateArticle/CreateArticleCommand.cs`):

```csharp
public sealed record CreateArticleCommand(
    string Title,
    string? Slug,
    string Content,
    PublicationStatus Status,
    DateTimeOffset PublishDateUtc,
    TournamentId? TournamentId,
    StoredFile? HeaderImage,
    IReadOnlyCollection<NewArticleAttachment> Attachments);

public sealed record NewArticleAttachment(
    string DisplayName,
    bool IsInline,
    StoredFile File);
```

`StoredFile` (`Neba.Api.Features.Storage.Domain`) is already a plain `sealed record` (`Container`, `Path`, `ContentType`, `SizeInBytes`, no factory/invariants) — the command carries it directly rather than inventing a parallel shape.

Contract-level (`Neba.Api.Contracts/News/CreateArticle/ArticleInput.cs`) — flattens `StoredFile`'s fields since `Neba.Api.Contracts` can't reference the domain type:

```csharp
public sealed record AttachmentInput
{
    public string DisplayName { get; init; } = string.Empty;
    public bool IsInline { get; init; }
    public string Container { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeInBytes { get; init; }
}
```

The endpoint maps each `AttachmentInput` → `NewArticleAttachment(input.DisplayName, input.IsInline, new StoredFile { Container = input.Container, Path = input.Path, ContentType = input.ContentType, SizeInBytes = input.SizeInBytes })`.

### Orphan cleanup job

`IFileStorageService` has no blob-listing capability today (`ExistsAsync`/`GetFileAsync`/`UploadFileAsync`/`DeleteAsync`/`GetBlobUri` only), so rather than add one just to sweep storage and cross-reference the DB, orphan tracking is done with a small DB-backed bookkeeping table:

- **New table `PendingArticleUpload`** (`Container`, `Path`, `UploadedAtUtc`). Both `UploadArticleHeaderImageEndpoint` and `UploadArticleAttachmentEndpoint` insert a row here immediately after a successful blob upload, before returning the `StoredFile` pointer to the caller.
- **Claiming**: `CreateArticleCommandHandler` (and later `UpdateArticleCommandHandler`) deletes the matching `PendingArticleUpload` row for every `StoredFile` actually referenced by the saved `Article` (`HeaderImage` + each `Attachment.File`), as part of the same `SaveChangesAsync` that persists the `Article`. If the save fails, the staging rows are left alone — correct, since nothing actually claimed those blobs.
- **Sweep**: a new recurring Hangfire job, `CleanupOrphanedArticleUploadsJob`/`CleanupOrphanedArticleUploadsJobHandler`, registered the same way `DocumentsConfiguration` registers its recurring sync job (`scheduler.AddOrUpdateRecurring(...)`):
  1. Query `PendingArticleUpload` rows where `UploadedAtUtc < now - threshold`.
  2. Delete each blob via the existing `IFileStorageService.DeleteAsync` (same delete-and-log-on-failure shape as `DeleteArticleFilesJobHandler`).
  3. Remove those rows from `PendingArticleUpload`.
- **Threshold**: proposing 24 hours as a starting point (long enough that a slow upload or a user still filling out the rest of the form isn't punished; short enough that orphaned blobs don't pile up) — revisit once we see real usage.

---

## Status

- [x] Phase 1 — API
- [x] Phase 1b — Server-side HTML sanitization
- [x] Phase 2 — UI create page
- [ ] Phase 2b — Tournament linking (deferred sub-phase)
- [ ] Phase 3 — Header image + attachments
