# Edit Article — Implementation Plan

## Design Decisions

- **Permission**: new `Permissions.EditArticle` (`"News.EditArticle"`), added to `ArticleManagementPermissions` alongside `CreateArticle`/`DeleteArticle` so `CanManageArticlesPolicyName` continues to cover all three. Matches the existing one-permission-per-action convention — not reused from `CreateArticle`.
- **HTTP verb**: `PUT {id}` — full replace of the editable field set, mirroring `CreateArticle`'s `ArticleInput` shape one-for-one (title, content, publication status, publish date, tournament, header image, attachments). No `PATCH`.
- **Slug**: immutable after creation. Edit form displays it read-only; the edit payload does not include it. Avoids broken external links/bookmarks and keeps the `neba:news:{slug}` cache tag stable across an edit. `CreateArticle.razor`'s slug field gets a small help text/tooltip ("Cannot be changed after saving") added in Phase 1 so this is surfaced at creation time, not just discovered when editing later.
- **Attachments**: full replace-set. The edit request carries the complete desired attachments collection (kept + newly uploaded). The handler diffs against `Article.Attachments`: anything in the current set but missing from the new set is removed (blob cleanup enqueued via the existing async job pattern); anything new is added via a new `Article.AddAttachment` call (already exists) or equivalent.
- **Shared attachment input type**: promote `CreateArticle`'s internal `NewArticleAttachment` record out of the `CreateArticle` namespace so both `CreateArticle` and `EditArticle` reference the same type instead of each having their own. **Naming collision to resolve during implementation**: `Neba.Api.Features.News.Domain.ArticleAttachment` already exists as the domain entity (Id, DisplayName, File, IsInline). Renaming `NewArticleAttachment` to plain `ArticleAttachment` in a different namespace (e.g. `Neba.Api.Features.News`) is legal C# but will read confusingly next to the domain type of the same simple name in the same feature. Recommend a name that keeps the promotion but avoids the exact collision — e.g. `ArticleAttachmentInput` — placed in a shared location like `Features/News/ArticleAttachmentInput.cs` (sibling to `CreateArticle/` and `EditArticle/`), `internal` to `Neba.Api`. Confirm final name before implementing.
- **Form model**: `EditArticle.razor` uses its own dedicated form model, not shared with `CreateArticle.razor`'s. The two pages have different constraints (slug read-only, attachments pre-populated from existing data, no "new" defaults) that make a shared model more confusing than two small, independent ones.

## Phase 1 — API

### Create page touch-up (`src/Neba.Website.Server/News/CreateArticle.razor`)

- Add brief help text or a tooltip next to the slug field: "Cannot be changed after saving." Small, low-risk change bundled with Phase 1 since it's about slug immutability, not the edit feature itself — gives users a heads-up before they ever hit the (not-yet-built) edit page.

### Domain (`src/Neba.Api/Features/News/Domain/Article.cs`)

- Add `Article.Update(title, content, publicationStatus, publishDate, tournamentId, headerImage)` — enforces the same structural invariants as `Create` (title/content non-empty, valid publication status/date), returns `ErrorOr<Success>`. Slug is not a parameter.
- Add `Article.RemoveAttachment(ArticleAttachmentId id)` — returns `ErrorOr<Success>` (`Error.NotFound` if the id isn't present).
- No new invariants expected between header image / attachments / publication status — confirm during implementation that none exist before assuming this.

### Command (`src/Neba.Api/Features/News/EditArticle/`)

- `EditArticleEndpoint.cs` — `Put("{id}")` under `Group<NewsEndpointGroup>()`, versioned to match `CreateArticle`, `Policies(PermissionCatalog.EditArticle.PolicyName)`, produces 204/400/401/403/404/409/422.
- `EditArticleCommand.cs` — `ICommand<Success>` (or `ICommand` if one exists) with `ArticleId Id`, Title, Content, PublicationStatus, PublishDate, TournamentId?, HeaderImage (StoredFile?), Attachments (`IReadOnlyCollection<ArticleAttachmentInput>` — the promoted, shared type; see naming note in Design Decisions).
- `EditArticleCommandHandler.cs`:
  1. Load the `Article` (`.Include(Attachments)`); `Error.NotFound` if missing.
  2. Validate `TournamentId` exists if provided (same check as Create).
  3. Sanitize HTML content (`HtmlContentSanitizer`, reuse from Create).
  4. Call `article.Update(...)`.
  5. Diff attachments: removed ids → `article.RemoveAttachment(id)` + collect `StoredFileReference`s for cleanup; new entries → `article.AddAttachment(...)`.
  6. If header image changed (old vs. new `StoredFile` differ), collect the old one as an orphaned blob.
  7. Enqueue orphaned-blob deletion the same way `DeleteArticleCommandHandler` does (background job), rather than deleting inline.
  8. `RemoveClaimedPendingUploadsAsync` for any newly claimed uploads (same as Create).
  9. Save changes.
  10. Invalidate `neba:news:articles` and `neba:news:{slug}` cache tags (slug unchanged, so this is deterministic from the loaded entity).
- `EditArticleRequestValidator.cs` — same field validators as `CreateArticleRequestValidator` minus slug.
- `EditArticleSummary.cs`.

### Shared attachment input type (`src/Neba.Api/Features/News/ArticleAttachmentInput.cs`)

- Move/rename `CreateArticle`'s `NewArticleAttachment` record here (DisplayName, IsInline, StoredFile File), `internal` to `Neba.Api`. Update `CreateArticleCommand`/`CreateArticleCommandHandler` to reference the promoted type; `EditArticleCommand` references it from the start.

### Contracts (`src/Neba.Api.Contracts/News/EditArticle/`)

- `EditArticleRequest` (wraps `ArticleInput`-shaped input, no slug field — either a new `EditArticleInput` or confirm whether `ArticleInput` can be reused with slug ignored/optional; prefer a dedicated `EditArticleInput` to keep the contract honest about what's actually editable).
- Reuse `AttachmentInput`, `HeaderImageInput` as-is.

### Tests

- Domain unit tests: `Article.Update` success/validation-failure paths, `Article.RemoveAttachment` found/not-found.
- Handler unit tests (`Neba.Api.Tests`): not-found, tournament-not-found, success with no attachment changes, success with additions, success with removals (verify cleanup job enqueued), success with header image replacement.
- Validator unit tests mirroring `CreateArticleRequestValidator`'s.
- Endpoint authorization integration test mirroring `DeleteArticleEndpointAuthorizationTests` pattern (watch for the FastEndpoints static-state gotchas documented in CLAUDE.md if this test spins up a real `WebApplication`).
- `PermissionCatalog`/`Permissions` unit test update if one exists enumerating all permissions/policies.

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

- Final name for the promoted attachment input type (`ArticleAttachmentInput` proposed to avoid colliding with the domain's `ArticleAttachment`) — confirm before implementing.
- Whether the domain layer needs any new invariants once `Update`/`RemoveAttachment` exist (e.g. can't remove the last attachment if inline content still references it) — confirm no such rule is implied by current behavior before assuming none.
