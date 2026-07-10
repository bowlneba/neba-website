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

## Phase 2 — UI: `/news/new` create page

- New Blazor page under `Neba.Website.Server/News/` at route `"/news/new"`.
- Form fields: Title, Slug (client-derived from Title as the user types, editable), Content, PublicationStatus, PublishDateUtc. No tournament field rendered yet — always submits `null`.
- Gated behind `CanManageArticles` (same policy as the delete button today).

### Content field — rich text editor

- **Quill** (not Tiptap, not MudBlazor) — this repo has no JS bundler for runtime code (`wwwroot/js/*.js` are plain scripts; webpack/vite aren't in play, jest/stryker are dev/test-only), which rules out Tiptap's module-bundled packages. Quill ships as a single script + stylesheet, needs no build step, and comes with its own toolbar UI out of the box. Pulling in a full component library like MudBlazor for one field isn't worth the dependency footprint.
- New `RichTextEditor.razor` component wrapping Quill via JS interop (`wwwroot/js/rich-text-editor.js`: init/getHtml/setHtml/dispose), two-way bound to the `Content` field. `NewsDetail.razor:94` already renders `Content` as `(MarkupString)_article.Content`, so Quill's HTML output slots in with no format conversion.
- **New requirement this introduces: server-side HTML sanitization.** Today `Article.Create` only checks `Content` is non-empty — nothing sanitizes it. Once a rich text editor is the input source, arbitrary HTML (including `<script>`, event handler attributes, etc., whether from a compromised staff account or a malicious paste) can reach `Content` and then `MarkupString` renders it unescaped. Needs a sanitization step (e.g. an allowlist-based HTML sanitizer) applied server-side before persisting — either in `Article.Create`/`CreateArticleCommandHandler` or as a shared helper reused by the eventual `UpdateArticleCommandHandler`. Library choice (e.g. `HtmlSanitizer`/Ganss.XSS) and exact placement is an open question to resolve when this sub-phase starts.

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
- **Sanitization unit tests (`Neba.Api.Tests`)**, wherever the sanitizer is placed (`Article.Create` or a shared helper):
  - Benign formatting HTML (bold/italic/lists/links/headings — Quill's actual output shapes) passes through unchanged.
  - `<script>` tags, inline event handler attributes (`onclick`, `onerror`, etc.), and `javascript:` URLs in `href`/`src` are stripped or neutralized.
  - `<iframe>`/`<object>`/`<embed>` and other non-allowlisted tags are removed.
  - Malformed/unclosed HTML doesn't throw and produces safe output.
  - Idempotency: sanitizing already-sanitized content is a no-op (matters once `UpdateArticleCommandHandler` re-sanitizes existing content on edit).
  - Empty/whitespace-only content after sanitization is still caught by the existing `Article.Content.Required` rule (i.e. a payload that's *entirely* disallowed markup, like a bare `<script>` tag with no visible text, must not slip past required-content validation once stripped).
- **Integration test** for the create endpoint: submitting `Content` containing a script tag returns a persisted `Article` whose stored `Content` has the script tag removed (end-to-end proof sanitization is actually wired into the handler, not just unit-tested in isolation).

### Deferred to a later sub-phase (tournament linking)

- Page accepts an optional route/query parameter (e.g. `?tournamentId=`) so a tournament-portal context can deep-link into `/news/new` with the tournament pre-selected and the picker hidden; outside that context, a dropdown/picker appears.
- Data source for the picker is undecided — `ListTournamentsInSeason` (`Neba.Api/Features/Tournaments/ListTournamentsInSeason/`) is a plausible fit, but scope (all seasons? current season only? active/upcoming only?) needs a decision when this sub-phase starts.

---

## Phase 3 — Header image + attachments

Hard constraint: **UI never talks to blob storage directly, only through the API.**

- **Two separate upload endpoints** — header image and attachment are differentiated by *route*, not by a discriminator field on a shared endpoint, matching this codebase's one-use-case-per-folder REPR convention (and letting each enforce its own validation independently). Both live under the existing `news` group (`Group<NewsEndpointGroup>()`), consistent with List/Get/Delete/Create Article:
  - `POST news/header-image` — `UploadArticleHeaderImageEndpoint`, stores under `news/uploads/header/{ulid}-{filename}`, validator restricted to image content types (+ likely a size cap).
  - `POST news/attachments` — `UploadArticleAttachmentEndpoint`, stores under `news/uploads/attachments/{ulid}-{filename}`, broader allowed file types.
  - Neither requires an `ArticleId`. Both return the same `StoredFile`-shaped response (e.g. `UploadedFileResponse`: Container, Path, ContentType, SizeInBytes).
- **Files stay at their upload path forever — they are never moved to a per-article folder once the Article saves.** `Article.HeaderImage`/`ArticleAttachment.File` simply store the `news/uploads/header/{ulid}-{filename}` or `news/uploads/attachments/{ulid}-{filename}` path permanently; there is no `news/{articleId}/...` reorganization step. This was a deliberate choice over moving files post-save:
  - Azure Blob Storage has no real folders — paths are just prefixes used for browsing in the portal/CLI. Grouping by `ArticleId` is a cosmetic convenience only, not a functional requirement, since the app always locates a file via the `StoredFile` (Container/Path) stored on the entity, never by directory listing.
  - A move requires a copy+delete (blobs can't be renamed atomically) plus updating the `StoredFile` path on the entity before it's persisted, and introduces a real partial-failure mode to handle (copy succeeds/delete fails → harmless orphan, already covered by the cleanup job below; copy fails outright → must fall back to the original path rather than fail the whole article save).
  - None of that complexity buys anything the app actually needs, so we don't do it. If per-article organization in blob storage ever becomes a real requirement (not just tidiness), this is the section to revisit.
- UI uploads each file **as soon as it's selected**, not on form submit — perceived save stays fast. The final `CreateArticleCommand` carries the already-returned `StoredFile` pointers: one for `HeaderImage`, a list of `{DisplayName, StoredFile, IsInline}` for `Attachments`. `Article.Create`/`AddAttachment` consume them exactly as `ArticleAttachment.Create` already does today.
- **Save-while-uploading is gated client-side, not server-side.** The create form tracks each selected file's upload as `Uploading` → `Uploaded(StoredFile)` / `Failed(error)`. The Save button is disabled (with an inline "Uploading N of M files…" indicator) while anything is `Uploading`, and stays disabled on `Failed` until removed/retried. This guarantees `CreateArticleCommand` is only ever built once every `StoredFile` pointer it needs already exists — the handler never has to special-case an in-flight or missing upload. A user who abandons the page mid-upload just produces an orphaned blob, handled by the cleanup job below like any other abandoned upload.
- **Open question**: size/type allowlist at the upload endpoint (e.g. images only for header image, size cap for attachments) — not decided yet, to be resolved when this endpoint is designed.

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

- [ ] Phase 1 — API
- [ ] Phase 2 — UI create page
- [ ] Phase 2b — Tournament linking (deferred sub-phase)
- [ ] Phase 3 — Header image + attachments
