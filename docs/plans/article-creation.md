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
- **Auth**: this endpoint needs a new `News.CreateArticle` permission added to `Permissions.ArticleManagementPermissions` (alongside the existing `News.DeleteArticle`), per the extension point already called out in `CanManageArticlesPolicy.md`. Endpoint uses `Policies(Permissions.CanManageArticlesPolicyName)`, matching the delete endpoint's convention.

---

## Phase 1 — API: Create Article

Scope: `Title`, `Slug` (optional override), `Content`, `PublicationStatus`, `PublishDateUtc`, `TournamentId` (always `null` from the UI this phase, but handled correctly if set). No header image, no attachments — those are Phase 3.

### Domain (`Neba.Api/Features/News/Domain`)

- `Article`: add private/internal constructor + `internal static ErrorOr<Article> Create(string title, string? slug, string content, PublicationStatus status, DateTimeOffset publishDateUtc, TournamentId? tournamentId)`.
  - Derives slug from `title` when `slug` is null/blank; otherwise normalizes the supplied value.
  - Validates: title required, content required, slug format valid (empty-after-normalization is an error), slug not a reserved word.
  - Does **not** validate slug uniqueness or tournament existence — those require persistence lookups and belong in the handler.
- `ArticleErrors`: add `Article.Title.Required`, `Article.Content.Required`, `Article.Slug.Invalid`, `Article.Slug.Reserved`, `Article.Slug.AlreadyExists` (Conflict), `Article.Tournament.NotFound` (Conflict).

### Application/API (`Neba.Api/Features/News/CreateArticle/`)

- `CreateArticleCommand(string Title, string? Slug, string Content, PublicationStatus Status, DateTimeOffset PublishDateUtc, TournamentId? TournamentId)`
- `CreateArticleCommandHandler`:
  1. If `TournamentId` provided, verify the tournament exists → `Article.Tournament.NotFound` (Conflict) if not.
  2. Check slug uniqueness (derived or supplied) → `Article.Slug.AlreadyExists` (Conflict) if taken.
  3. Call `Article.Create(...)`, persist, return DTO.
- `CreateArticleEndpoint` — `Post(string.Empty)` + `Group<NewsEndpointGroup>()` (routes to `POST news/`, matching `ListArticlesEndpoint`'s `Get(string.Empty)`), `Policies(Permissions.CanManageArticlesPolicyName)`, `WithName("CreateArticle")`, `Produces<ArticleResponse>(201)`, `ProducesProblemDetails(400)`, `ProducesProblemDetails(409)`.
- `CreateArticleSummary`, `CreateArticleValidator` (structural only: title/content non-empty + max length, publish date required).

### Contracts (`Neba.Api.Contracts/News/CreateArticle/`)

- `ArticleInput` (Title, Slug?, Content, PublicationStatus, PublishDateUtc, TournamentId?)
- `CreateArticleRequest` (wraps `ArticleInput`, per the Request-wraps-Input convention)
- `INewsApi` gets `CreateAsync`

### Security (`Neba.Api.Contracts/Security/Permission.cs`)

- Add `News.CreateArticle` permission, add to `ArticleManagementPermissions`.

### Tests

- `Article.Create` unit tests: title/content required, slug derivation, slug normalization of a supplied override, reserved-slug rejection.
- `CreateArticleCommandHandler` unit tests: duplicate slug → Conflict, unknown tournament → Conflict, success path (derived slug, supplied slug override).
- Integration test for the endpoint (201 + Location header, 400 on structural validation failure, 409 on duplicate slug).

---

## Phase 2 — UI: `/news/new` create page

- New Blazor page under `Neba.Website.Server/News/` at route `"/news/new"`.
- Form fields: Title, Slug (client-derived from Title as the user types, editable), Content, PublicationStatus, PublishDateUtc. No tournament field rendered yet — always submits `null`.
- Gated behind `CanManageArticles` (same policy as the delete button today).

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
