# Delete Article — Implementation Plan

Three phases, iterate in order. Each phase should land as its own PR (or at least its own set of commits) so it can be reviewed independently.

---

## Phase 1 — Core delete (API only, no blob cleanup yet)

### 1a. Standardize permissions (first "real" permission)

`Permissions` (`src/Neba.Api.Contracts/Security/Permission.cs`) currently only has temporary `Read`/`Write` values. We're introducing the `Feature.Action` naming convention starting now, without migrating the existing temporary values yet.

- Add a new permission value: `Permissions.News.DeleteArticle` — but `Permissions` is a flat `SmartEnum<Permissions, string>`, not nested classes, so the convention needs to live in the `Value`/`Name` strings, not in C# nesting. Two ways to do this cleanly — **decide when implementing**:
  - **Option A**: Keep a single flat `Permissions` SmartEnum, add `public static readonly Permissions DeleteArticle = new("News.DeleteArticle", "News.DeleteArticle");`. Simple, no structural change.
  - **Option B**: Introduce nested static classes purely for organizing the *constants* (e.g. a `PermissionNames.News.DeleteArticle` string constant), while `Permissions` SmartEnum values still reference those constants. More ceremony, pays off once there are many permissions.
  - Recommendation: start with **Option A** (flat, just adopt the dotted string). Revisit nesting once there are enough permissions that flat becomes unwieldy.
- `PolicyName` stays as-is: `$"Permission:{Value}"` → produces `"Permission:News.DeleteArticle"`.
- Register the new permission in `SecurityRoleSeeder.cs` (`RolePermissions[Roles.Admin]` currently uses `Permissions.List` which is presumably "all defined permissions" — confirm this picks up the new value automatically, since `Permissions.List` is a `SmartEnum` built-in that returns all declared instances).
- Endpoint uses `Policies(Permissions.News.DeleteArticle.PolicyName)` (mirroring how `Policies(SecurityConfiguration.AuthenticatedPolicy)` is used elsewhere) rather than `Roles(SecurityRoles.Admin)` — this is the first endpoint to actually exercise the permission-policy path instead of role-based auth. Need to confirm a permission-based authorization policy is actually registered somewhere (search for where `Permission:{value}` policies get added to `AddAuthorization(...)` — if this doesn't exist yet, it needs to be added as part of this phase, since currently nothing calls `Policies()` with a permission-based policy name).

### 1b. Expose `ArticleId` on existing query DTOs

- `ArticleSummaryDto` (`src/Neba.Api/Features/News/ListArticles/ArticleSummaryDto.cs`) — add `public required string ArticleId { get; init; }` (serialize `ArticleId` as its underlying ULID string, consistent with how other strongly-typed IDs cross the API boundary — confirm the existing convention, e.g. check how `BowlerId`/`TournamentId` are serialized in other DTOs).
- `ArticleDetailDto` (`src/Neba.Api/Features/News/GetArticle/ArticleDetailDto.cs`) — add the same `ArticleId` field.
- Update `ListArticlesQueryHandler` and `GetArticleQueryHandler` projections to select `article.Id` and map it into the DTOs.
- Update any existing tests asserting on these DTOs' shape (unit tests for both handlers, and any snapshot/contract tests).

### 1c. `DeleteArticleCommand` + handler

New folder: `src/Neba.Api/Features/News/DeleteArticle/`

- `DeleteArticleCommand.cs` — `public sealed record DeleteArticleCommand : ICommand { public required ArticleId ArticleId { get; init; } }` (mirror `ResetPasswordCommand` shape).
- `DeleteArticleCommandHandler.cs` — `ICommandHandler<DeleteArticleCommand>` (no response, matches `ResetPasswordCommandHandler`'s `ICommandHandler<ResetPasswordCommand>` — confirm exact generic shape for void-returning commands).
  - Load the `Article` by `ArticleId` (tracked, not `AsNoTracking()`, since we're deleting) via `appDbContext.Articles`.
  - **Decision point per your instructions**: "whether the article actually exists or not, we will return a 204 as long as the user has permission to delete an article." This means the handler should treat "not found" as a **success**, not `ErrorType.NotFound`. So:
    - If `article is null` → return `Result.Success` (not an error) — the endpoint always maps to 204 regardless.
    - If found → `appDbContext.Articles.Remove(article)`, `await appDbContext.SaveChangesAsync(ct)`, return `Result.Success`.
  - This means `DeleteArticleCommandHandler` never actually returns an error in the "normal" sense — simplifies the endpoint (no `NotFound` branch needed, unlike `ResetPasswordEndpoint`). Confirm this is really the desired behavior (idempotent delete, no leakage of existence via status code) before implementing — it's a deliberate security/UX choice (avoids leaking article existence to unauthorized-but-authenticated callers, and makes retries safe).
  - Cascading delete: EF Core handles `ArticleAttachment` cascade automatically per your note — confirm the FK relationship in `AppDbContext`'s `ArticleConfiguration` (or wherever configured) is set to `DeleteBehavior.Cascade` (default for required FKs) so no explicit removal of attachments is needed.
- No cache invalidation in this phase yet — deferred to be bundled with Phase 2's blob-cleanup work, OR pull forward into 1c since it's cheap. **Recommendation: do cache invalidation in 1c**, not deferred — it's a correctness issue (stale cached list/detail pages) independent of blob cleanup. Inject `HybridCache`, after successful delete call:
  - `await hybridCache.RemoveByTagAsync("neba:news:articles", ct)` (invalidate all list pages)
  - `await hybridCache.RemoveByTagAsync($"neba:news:{article.Slug}", ct)` (invalidate the detail cache) — only possible if the article was found; if not found, there's nothing cached under an unknown slug anyway, so skip.

### 1d. `DeleteArticleEndpoint`

- `Delete("{id}")` under `NewsEndpointGroup` (route becomes `DELETE /news/{id}` given the group prefix is `"news"` — confirm final path matches `/articles/...` intent or adjust; the existing List/Get endpoints are under the `news` group, e.g. `GET news/{slug}` and `GET news`. Decide whether delete should be `DELETE news/{id}` for consistency, even though your framing said "/articles/{id}" — **use whatever the group's actual base path resolves to**, likely `news/{id}`).
- `Policies(Permissions.News.DeleteArticle.PolicyName)`.
- Parse `req.Id` into `ArticleId` (mirror `Ulid.Parse(req.UserId, CultureInfo.InvariantCulture)` pattern from `ResetPasswordEndpoint`, using `ArticleId`'s parsing convention — check `ArticleId.cs` for how it's constructed from a raw string).
- Always `await Send.NoContentAsync(ct)` on success (no error branch needed per 1c's design — command handler never errors).
- `Description(...)`: `WithName("DeleteArticle")`, `Produces(204)`, `ProducesProblemDetails(401)`, `ProducesProblemDetails(403)`. No 404 (by design) and no 422 (no validation needed on the parsed ID beyond FastEndpoints' own route-binding failure).

### 1e. Tests (Phase 1)

- `DeleteArticleCommandHandlerTests`: found+deleted case, not-found case (both assert `Result.Success` / no error), cascading attachment removal (integration test), cache invalidation calls.
- `DeleteArticleEndpointTests` (or equivalent Configure test): asserts route, policy, `ignore-methods` per the mutation-testing conventions already documented in CLAUDE.md.
- Update `ListArticlesQueryHandlerTests` / `GetArticleQueryHandlerTests` for the new `ArticleId` field.

---

## Phase 2 — Background job for blob cleanup

### 2a. `DeleteArticleFilesJob`

New folder: `src/Neba.Api/Features/News/DeleteArticle/` (co-located with the command) or a shared `src/Neba.Api/Features/News/Jobs/` — **decide based on whether other News background jobs are anticipated**; if this is the only one, co-locate with `DeleteArticleCommand`.

Per your answered question: **one job, takes a collection of files.**

```csharp
public sealed record DeleteArticleFilesJob : IBackgroundJob
{
    public required IReadOnlyCollection<StoredFileReference> Files { get; init; }
    public string JobName => $"DeleteArticleFiles: {Files.Count} file(s)";
}

public sealed record StoredFileReference
{
    public required string Container { get; init; }
    public required string Path { get; init; }
}
```

(`StoredFileReference` needed because `StoredFile` the domain value object may not be directly serializable/appropriate to pass through Hangfire's job storage — confirm whether `StoredFile` itself is already a simple serializable record and can be reused directly instead of introducing a parallel type.)

### 2b. `DeleteArticleFilesJobHandler`

- `IBackgroundJobHandler<DeleteArticleFilesJob>`.
- Loop through `job.Files`, call `_fileStorageService.DeleteAsync(file.Container, file.Path, ct)` for each.
- Decide error handling for partial failure: log and continue (best-effort, matches "we don't need to worry about" tone from your Phase-1 scoping) vs. fail the whole job on first exception (Hangfire would then retry the *whole* job, re-attempting already-deleted files — `DeleteAsync` should be idempotent/no-op on a missing blob, confirm `AzureBlobStorageService.DeleteAsync` doesn't throw on not-found). **Recommendation**: catch per-file, log a warning per failure, don't rethrow — since this is best-effort cleanup after the article record is already gone; a stuck orphaned blob is a minor issue, not worth blocking/retrying the whole job indefinitely.
- Add `[LoggerMessage]` partials + a small metrics class, matching `SyncDocumentToStorageJobHandler`'s shape (`SyncDocumentToStorageMetrics` as the template) if you want parity — **optional, ask whether full OTel/metrics parity is warranted for this job or if plain logging suffices** (it's a much simpler job than document sync).

### 2c. Wire into `DeleteArticleCommandHandler`

- Before deleting the article row, capture the header image + all attachments' `StoredFile` (Container/Path) — you specifically called out "we will need to do a get beforehand to get the attachment/header file details," meaning: don't rely on the entity still being in memory/tracked after `SaveChangesAsync` if there's any risk of it being detached — but since we already loaded `article` as tracked in 1c to delete it, we already have `article.HeaderImage` and `article.Attachments` in memory before deletion. Confirm no extra "get" query is actually needed beyond the load already done in 1c — likely your "get beforehand" instinct is satisfied by the existing tracked load, not a second round-trip.
- After `SaveChangesAsync` succeeds, build the file list (header image if non-null, each attachment's `File`) and `backgroundJobScheduler.Enqueue(new DeleteArticleFilesJob { Files = [...] })`.
- If `article` was null (not-found case), skip the enqueue entirely — nothing to clean up.

### 2d. Tests (Phase 2)

- `DeleteArticleFilesJobHandlerTests`: all-succeed, partial-failure (one file throws, others still attempted), empty collection.
- `DeleteArticleCommandHandlerTests`: assert `Enqueue` is called with the correct file list (header image + non-inline + inline attachments — confirm whether inline attachments, which `GetArticleQueryHandler` filters out of the response DTO, should still be included in the deletion list; they should, since they're still blobs in storage even if not shown as separate "attachments" in the UI).

---

## Phase 3 — UI (article list + detail pages)

### 3a. Permission-gated trash icon — placement mockups (to be discussed live, not decided here)

Candidate placements to mock up when we get to this phase:
1. **List page** (`NewsList.razor`) — trash icon overlaid on each `ArticleCard` (e.g. top-right corner, shown on hover or always-visible if permission granted).
2. **Detail page** (`NewsDetail.razor`) — trash icon near the article title/header, or in a small admin action bar above the article body.

No existing permission-gated UI pattern beyond generic `<AuthorizeView>` — will need a way to check the specific `News.DeleteArticle` permission client-side (likely a custom `IsInRole`-equivalent claim check, or a small `HasPermissionAsync`/`<AuthorizeView Policy="...">` if ASP.NET Core policy-based auth extends cleanly into Blazor's `AuthorizeView` — confirm during implementation).

### 3b. Confirmation dialog

No existing destructive-action confirm dialog to copy. Build using `NebaModal.razor` as the base (`HeaderContent`/`ChildContent`/`FooterContent`, `IsOpen`/`OnClose`) with a `ConfirmDeleteArticleModal` (or a generic reusable `ConfirmActionModal` if you want this to also serve future destructive actions — **worth deciding now**: generic reusable component vs. article-specific one-off).

### 3c. Delete flow

- Call `DELETE news/{id}` via the API client (`INewsApi` or equivalent, check `ApiExecutor` usage in `NewsList.razor`/`NewsDetail.razor` for the pattern).
- **List page**: on success, remove the article from the in-memory list (no full reload needed) — client-side splice, not a re-fetch.
- **Detail page**: on success, navigate back to `/news` (`NavigationManager.NavigateTo("/news")`).
- No client-side cache to worry about beyond what the server already invalidates (Phase 1c) — confirm the Blazor Server pages aren't doing their own separate client-side caching layer that would also need invalidation.

### 3d. Tests (Phase 3)

- bUnit tests for the trash icon visibility (permission granted vs. not), confirm-dialog interaction (cancel vs. confirm), list-removal behavior, detail-page navigation-on-delete.
- E2E test (`tests/e2e/`) covering the full delete flow if there's an existing E2E suite for News.

---

## Open questions to resolve as you implement

1. Does a permission-based authorization policy (`"Permission:{value}"`) actually get registered in `AddAuthorization(...)` today, or does Phase 1a need to add that wiring for the very first time?
2. Is `StoredFile` (the domain value object) directly reusable as a Hangfire job payload, or does it need a lightweight serializable projection (`StoredFileReference`)?
3. Does `AzureBlobStorageService.DeleteAsync` no-op safely on a missing blob (needed for the "best-effort, don't fail the whole job" approach in 2b)?
4. Full OTel/metrics parity for `DeleteArticleFilesJobHandler` (like `SyncDocumentToStorageJobHandler`), or keep it lean with just logging?
5. Generic reusable `ConfirmActionModal` vs. article-specific `ConfirmDeleteArticleModal`?
6. Confirm final route shape — `DELETE news/{id}` (given the `NewsEndpointGroup` prefix) vs. some other path structure implied by "/articles/{id}" in your original framing.
