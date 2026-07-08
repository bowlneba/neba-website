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

> **Status**: `DeleteArticleFilesJob` and `StoredFileReference` already exist in `src/Neba.Api/Features/News/DeleteArticle/`. `DeleteArticleCommandHandler` deletes the article row and invalidates cache but does **not** yet enqueue the cleanup job. What remains: the job handler, wiring the handler's enqueue call into the command handler, and registration (which is automatic — see below).

### 2a. `DeleteArticleFilesJob` (done)

```csharp
// src/Neba.Api/Features/News/DeleteArticle/DeleteArticleFilesJob.cs
public sealed record DeleteArticleFilesJob
    : IBackgroundJob
{
    public required IReadOnlyCollection<StoredFileReference> Files { get; init; }

    public string JobName
        => $"{nameof(DeleteArticleFilesJob)}: {Files.Count} file(s)";
}
```

```csharp
// src/Neba.Api/Features/News/DeleteArticle/StoredFileReference.cs
public sealed record StoredFileReference
{
    public required string Container { get; init; }
    public required string Path { get; init; }
}
```

`StoredFileReference` is a deliberate parallel type, not a reuse of the domain `StoredFile` value object (`Neba.Api.Features.Storage.Domain.StoredFile`) — `DeleteArticleCommandHandler` lives in `Neba.Api.Features.News`, and domain value objects from `Storage` shouldn't be threaded through Hangfire's job-storage serialization as a cross-feature contract. `StoredFileReference` is projected from `StoredFile.Container`/`StoredFile.Path` at the call site (2c).

### 2b. `DeleteArticleFilesJobHandler` (to build)

New file: `src/Neba.Api/Features/News/DeleteArticle/DeleteArticleFilesJobHandler.cs`

Kept lean — plain `[LoggerMessage]` logging, no dedicated metrics class. This is a much simpler job than `SyncDocumentToStorageJobHandler` (no external API call, no OTel activity tags beyond what Hangfire itself records); metrics parity isn't worth the extra ceremony here. Per-file failures are caught and logged, not rethrown — this is best-effort cleanup running after the article row is already gone, so a stuck orphaned blob is a minor, non-blocking issue. `IFileStorageService.DeleteAsync` already no-ops on a missing blob (confirmed via `AzureBlobStorageService`'s use of `BlobClient.DeleteIfExistsAsync`), so a retry of the whole job would also be safe if that approach were used instead — but per-file catch is still preferred so one bad file doesn't block cleanup of the rest.

```csharp
using Neba.Api.BackgroundJobs;
using Neba.Api.Storage;

namespace Neba.Api.Features.News.DeleteArticle;

internal sealed class DeleteArticleFilesJobHandler(
    IFileStorageService fileStorageService,
    ILogger<DeleteArticleFilesJobHandler> logger)
        : IBackgroundJobHandler<DeleteArticleFilesJob>
{
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly ILogger<DeleteArticleFilesJobHandler> _logger = logger;

    public async Task ExecuteAsync(DeleteArticleFilesJob job, CancellationToken cancellationToken)
    {
        foreach (var file in job.Files)
        {
            try
            {
                await _fileStorageService.DeleteAsync(file.Container, file.Path, cancellationToken);

                _logger.LogDeletedArticleFile(file.Container, file.Path);
            }
            catch (Exception ex)
            {
                _logger.LogFailedToDeleteArticleFile(ex, file.Container, file.Path);
            }
        }
    }
}

internal static partial class DeleteArticleFilesJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Deleted article file '{Path}' from container '{Container}'.")]
    public static partial void LogDeletedArticleFile(
        this ILogger<DeleteArticleFilesJobHandler> logger,
        string container,
        string path);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to delete article file '{Path}' from container '{Container}'.")]
    public static partial void LogFailedToDeleteArticleFile(
        this ILogger<DeleteArticleFilesJobHandler> logger,
        Exception ex,
        string container,
        string path);
}
```

`catch (Exception ex)` here is an intentional broad catch — matches the "always log a caught exception before swallowing" convention. No custom `[BackgroundJobsAssembly]`/interface registration is needed: `BackgroundJobsConfiguration.AddBackgroundJobs` already Scrutor-scans `IBackgroundJobHandler<>` implementations in the `Neba.Api` assembly and registers them scoped — this handler is picked up automatically once it exists.

### 2c. Wire into `DeleteArticleCommandHandler` (to build)

The tracked `article` loaded in Phase 1 already has `HeaderImage` and `Attachments` in memory before deletion — no second "get" query is needed. Build the file list from that same instance, after `SaveChangesAsync` succeeds, and enqueue. Include inline attachments too — they're still blobs in storage even though `GetArticleQueryHandler`'s response DTO filters them out of the "attachments" list shown in the UI.

```csharp
// src/Neba.Api/Features/News/DeleteArticle/DeleteArticleCommandHandler.cs
using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Messaging;

namespace Neba.Api.Features.News.DeleteArticle;

internal sealed class DeleteArticleCommandHandler(
    AppDbContext appDbContext,
    HybridCache cache,
    IBackgroundJobScheduler backgroundJobScheduler)
        : ICommandHandler<DeleteArticleCommand, Deleted>
{
    public async Task<ErrorOr<Deleted>> HandleAsync(DeleteArticleCommand command, CancellationToken cancellationToken)
    {
        var article = await appDbContext.Articles
            .SingleOrDefaultAsync(a => a.Id == command.ArticleId, cancellationToken);

        if (article is null)
        {
            return Result.Deleted;
        }

        var filesToDelete = BuildFileReferences(article);

        appDbContext.Articles.Remove(article);
        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("neba:news:articles", cancellationToken);
        await cache.RemoveByTagAsync($"neba:news:{article.Slug}", cancellationToken);

        if (filesToDelete.Count > 0)
        {
            backgroundJobScheduler.Enqueue(new DeleteArticleFilesJob { Files = filesToDelete });
        }

        return Result.Deleted;
    }

    private static List<StoredFileReference> BuildFileReferences(Domain.Article article)
    {
        List<StoredFileReference> files = [];

        if (article.HeaderImage is not null)
        {
            files.Add(new StoredFileReference
            {
                Container = article.HeaderImage.Container,
                Path = article.HeaderImage.Path
            });
        }

        files.AddRange(article.Attachments.Select(a => new StoredFileReference
        {
            Container = a.File.Container,
            Path = a.File.Path
        }));

        return files;
    }
}
```

Note: `filesToDelete` is captured **before** `appDbContext.Articles.Remove(article)`/`SaveChangesAsync` purely for readability (the entity itself isn't mutated by removal, so capturing before or after `SaveChangesAsync` is equivalent here) — but capturing it before the delete keeps the "gather facts, then act" shape consistent with the rest of the handler.

### 2d. Tests (Phase 2)

Deferred to the dedicated test-writing pass — not detailed here. Will cover: `DeleteArticleFilesJobHandler` (all-succeed, partial-failure, empty collection) and `DeleteArticleCommandHandler`'s enqueue behavior (header image + inline + non-inline attachments all included; not-found case skips enqueue; empty article — no header, no attachments — also skips enqueue).

---

## Phase 3 — UI (article list + detail pages)

> **Status**: `INewsApi.DeleteArticleAsync` already exists in `src/Neba.Api.Contracts/News/INewsApi.cs`. Everything else in this phase — the `ApiExecutor` overload, the confirm-dialog component, and both pages' delete flows — is new.

### 3a. `ApiExecutor` non-generic overload (to build)

`DeleteArticleAsync` returns non-generic `Task<IApiResponse>` (no response body, matching the 204 the endpoint sends), but `ApiExecutor.ExecuteAsync<TResponse>` (`src/Neba.Website.Server/Services/ApiExecutor.cs`) only accepts `Func<CancellationToken, Task<IApiResponse<TResponse>>>`. Add a non-generic overload rather than forcing a `TResponse` through a bodyless response:

```csharp
// src/Neba.Website.Server/Services/ApiExecutor.cs
public async Task<ErrorOr<Success>> ExecuteAsync(
    string apiName,
    string operationName,
    Func<CancellationToken, Task<IApiResponse>> apiCall,
    CancellationToken cancellationToken = default)
{
    // Same activity/metrics/try-catch shape as the generic overload above,
    // but branches on response.IsSuccessStatusCode directly (no response.Content
    // null-check branch, since IApiResponse has no Content) and returns
    // Result.Success instead of response.Content on the success path.
    // Reuses HandleException<TResponse> by instantiating it as HandleException<Success>
    // for the catch blocks, or extracts a shared non-generic error-mapping helper —
    // decide the least-duplication shape when implementing.
}
```

### 3b. Permission-gated trash icon

Client-side permission checks aren't used anywhere in the Website project yet (only a bare `<AuthorizeView>` with no `Policy=`/`Roles=` in `AccountMenu.razor`), but the underlying plumbing is already registered — `AccountConfiguration.cs` wires `PermissionPolicyProvider`/`PermissionAuthorizationHandler` and `AddCascadingAuthenticationState()`, and `Permissions.DeleteArticle.PolicyName` (`"Permission:News.DeleteArticle"`) already exists in `Neba.Api.Contracts.Security`. So `<AuthorizeView Policy="...">` works directly, no new claim-checking helper needed:

```razor
@* src/Neba.Website.Server/News/ArticleCard.razor — added to the existing card markup *@
<AuthorizeView Policy="@Permissions.DeleteArticle.PolicyName">
    <Authorized>
        <button type="button"
                class="article-card-delete"
                aria-label="Delete article"
                @onclick="OnDeleteRequested"
                @onclick:stopPropagation="true"
                @onclick:preventDefault="true">
            <TrashIcon />
        </button>
    </Authorized>
</AuthorizeView>
```

`@onclick:stopPropagation`/`preventDefault` are required because `ArticleCard` itself is a wrapping `<a>` — without them, clicking the trash icon would also navigate to the article. `ArticleCard` gains an `[Parameter] EventCallback<ArticleSummaryResponse> OnDeleteRequested` that `NewsList.razor` binds to open the confirm modal for that article.

Detail page (`NewsDetail.razor`) gets the same `<AuthorizeView Policy="...">` block near the title, wired to a local `OpenDeleteConfirm()` instead of a parameter.

### 3c. Confirmation dialog

No existing destructive-action confirm dialog to copy from, but `NebaModal.razor` (`HeaderContent`/`ChildContent`/`FooterContent`, `IsOpen`/`OnClose`, `MaxWidth`) is already used for non-destructive dialogs (`DirectionsModal.razor`, `BowlerTitlesModal.razor`) and composes cleanly for this. Build a **generic reusable `ConfirmActionModal`** rather than an article-specific one — deleting articles won't be the last destructive action in this app (bowling centers, sponsors, etc. are plausible future candidates), and the component is trivial enough that genericizing it now costs nothing:

```razor
@* src/Neba.Website.Server/Components/ConfirmActionModal.razor *@
<NebaModal IsOpen="@IsOpen" OnClose="@OnCancel" Title="@Title" MaxWidth="480px">
    <ChildContent>
        <p>@Message</p>
    </ChildContent>
    <FooterContent>
        <div class="flex justify-end gap-2">
            <button class="neba-btn neba-btn-secondary" @onclick="OnCancel">@CancelLabel</button>
            <button class="neba-btn neba-btn-danger" @onclick="OnConfirm" disabled="@IsBusy">
                @(IsBusy ? "Deleting..." : ConfirmLabel)
            </button>
        </div>
    </FooterContent>
</NebaModal>

@code {
    [Parameter, EditorRequired] public bool IsOpen { get; set; }
    [Parameter, EditorRequired] public EventCallback OnConfirm { get; set; }
    [Parameter, EditorRequired] public EventCallback OnCancel { get; set; }
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;
    [Parameter, EditorRequired] public string Message { get; set; } = string.Empty;
    [Parameter] public string ConfirmLabel { get; set; } = "Delete";
    [Parameter] public string CancelLabel { get; set; } = "Cancel";
    [Parameter] public bool IsBusy { get; set; }
}
```

`IsBusy` disables the confirm button while the delete API call is in flight, preventing a double-submit.

### 3d. Delete flow

**List page** (`NewsList.razor`) — on confirm, call the API, then splice the deleted article out of the in-memory list rather than re-fetching the page:

```csharp
private async Task ConfirmDeleteAsync()
{
    _isDeleteBusy = true;

    var result = await ApiExecutor.ExecuteAsync(
        "News", "DeleteArticle",
        ct => NewsApi.DeleteArticleAsync(_articlePendingDelete!.ArticleId, ct));

    _isDeleteBusy = false;
    _isDeleteConfirmOpen = false;

    if (result.IsError)
    {
        _errorMessage = result.FirstError.Description;
        return;
    }

    _articles.Remove(_articlePendingDelete!);
    _articlePendingDelete = null;
}
```

**Detail page** (`NewsDetail.razor`) — on confirm, call the API, then navigate back to the list:

```csharp
private async Task ConfirmDeleteAsync()
{
    _isDeleteBusy = true;

    var result = await ApiExecutor.ExecuteAsync(
        "News", "DeleteArticle",
        ct => NewsApi.DeleteArticleAsync(Article!.ArticleId, ct));

    _isDeleteBusy = false;

    if (result.IsError)
    {
        _isDeleteConfirmOpen = false;
        _errorMessage = result.FirstError.Description;
        return;
    }

    NavigationManager.NavigateTo("/news");
}
```

Both pages are `@rendermode @(new InteractiveServerRenderMode(prerender: false))` already, so `@onclick` handlers and the modal's JS interop work without further rendermode changes. No separate client-side cache exists in the Blazor Server pages beyond what `DeleteArticleCommandHandler` already invalidates server-side (Phase 1c) — `NewsList.razor`/`NewsDetail.razor` re-fetch from the API on navigation, they don't maintain their own cache layer.

### 3e. Tests (Phase 3)

Deferred to the dedicated test-writing pass — not detailed here. Will cover: bUnit tests for trash-icon visibility (permission granted vs. not), `ConfirmActionModal` cancel/confirm interaction, list-splice behavior, detail-page navigation-on-delete, and an E2E test for the full delete flow if `tests/e2e/` has an existing News suite to extend.

---

## Resolved decisions

These were open questions in earlier drafts of this plan; resolved during Phase 1–2 implementation and this doc update:

1. **Permission-based authorization policy wiring** — confirmed registered (`PermissionPolicyProvider`/`PermissionAuthorizationHandler`, wired in both API and Website `AccountConfiguration.cs`/security configuration). No first-time wiring needed.
2. **`StoredFile` vs. `StoredFileReference` for the job payload** — `StoredFileReference` is the deliberate choice (§2a): keeps the Hangfire job payload decoupled from the `Storage` feature's domain value object.
3. **Blob delete idempotency** — `IFileStorageService.DeleteAsync` no-ops on a missing blob, confirmed via `AzureBlobStorageService`'s use of `DeleteIfExistsAsync`. Per-file catch-and-log in the job handler is still used (§2b) so one bad file doesn't block the rest.
4. **Metrics parity for `DeleteArticleFilesJobHandler`** — kept lean, logging only, no dedicated metrics class (§2b).
5. **Generic vs. article-specific confirm modal** — generic `ConfirmActionModal` (§3c).
6. **Final route shape** — `DELETE news/{id}` (confirmed live in `DeleteArticleEndpoint.cs`, under `NewsEndpointGroup`'s `"news"` prefix).
