# Refresh Document Cache

Lets a Webmaster force-refresh a Google Docs-backed document (Tournament Rules, Bylaws, or any future document) by clearing its cached copy and reloading the page so it's re-fetched from Google Drive and re-cached, without waiting for the 7-day cache expiry. Addresses issue #21.

## Decisions locked in during scoping

- **Not critical/high-stakes** — admin cache-refresh utility, no money/PII/compliance angle, low blast radius (worst case: one extra Google Drive round-trip). NFR checkpoint (Step 2) skipped.
- **Separate, narrower permission** — `Permissions.RefreshDocument` (`"Documents.RefreshDocument"`, `"Refresh Document"`), granted to the `Webmaster` role. Deliberately distinct from the existing `Cache.Clear` permission (`Features/Cache/ClearCache/`), which is Admin-only and wipes the *entire* FusionCache/HybridCache plus every cached document in blob storage — that blast radius is intentionally much larger than what this feature needs.
- **Clear scope is single-document, not global** — evicts only `CacheDescriptors.Documents.Content(documentName)`'s FusionCache tag (`neba:document:{name}`) and deletes only that document's blob (`bowlneba-private/documents/{name}`). No other documents, no other cache tags, and no `HybridCache` involvement (HybridCache is only touched by the unrelated global `ClearCache` endpoint — per [[feedback_fusioncache_vs_hybridcache]], query caching goes through `IFusionCache`).
- **Generic by document name** — endpoint takes `{DocumentName}` the same way `GetDocumentEndpoint` does, so it works for `tournament-rules`, `bylaws`, or any document added to `GoogleSettings.Documents` in the future with no further backend changes.
- **Button placement** — lives inside the shared `NebaDocument.razor` component, next to the existing "Last updated: ..." text in both the desktop TOC sidebar and the mobile TOC modal. Every current and future document page gets it automatically with no per-page wiring. The slide-over panel (used for cross-document links, e.g. viewing Bylaws from inside Tournament Rules) intentionally does **not** get its own refresh control — a Webmaster fixing a document is expected to navigate to it directly.
- **Visibility gating** — wrapped in `<AuthorizeView Policy="Permission:Documents.RefreshDocument">`, so it only renders for users holding the permission.
- **Processing feedback** — reuses the existing `NebaLoadingIndicator` (`Scope="Page"`, e.g. `Text="Refreshing document..."`) to overlay the document content while the clear-cache call is in flight and the button is disabled. On success, the page does a full reload (`NavigationManager.Refresh(forceLoad: true)`), and the reload's own existing "Loading tournament rules..."/"Loading bylaws..." indicator (already built into each page) takes over from there — no separate indicator needed for the reload itself.
- **Route/naming** — `DELETE /documents/{DocumentName}/cache`, nested under the existing `DocumentsEndpointGroup`, mirroring the resource-oriented shape of the existing `DELETE /cache` (global) endpoint. Command/endpoint classes named `RefreshDocumentCommand`/`RefreshDocumentEndpoint` per the confirmed action name.

## Phase 1: API

### Security

- **Edit** `src/Neba.Api.Contracts/Security/Permission.cs` — add a new `#region Documents` (placed after `#region Tournaments`, before `#region Background Jobs`) containing `public static readonly Permissions RefreshDocument = new("Documents.RefreshDocument", "Refresh Document");`.
- **Edit** `src/Neba.Api/Security/Infrastructure/SecurityRoleSeeder.cs` — add `Permissions.RefreshDocument` to the `Roles.Webmaster` permission list. `Roles.Admin` already gets every permission via `Permissions.List`, so no separate change needed there.
- No `docs/policies/README.md` update needed — this uses the existing generic `Permission:{value}` dynamic policy row, same as `Cache.Clear` today (single-permission policies don't get their own row per that doc's "When to update" rule).

```csharp
// Permission.cs — new region, placed after #region Tournaments
#region Documents

/// <summary>
/// Permission to force-refresh a single document's cached copy from Google Drive.
/// </summary>
public static readonly Permissions RefreshDocument = new("Documents.RefreshDocument", "Refresh Document");

#endregion
```

```csharp
// SecurityRoleSeeder.cs — add to the Roles.Webmaster list, after ViewBackgroundJobsDashboard
Permissions.RefreshDocument
```

### Application / API (`src/Neba.Api/Features/Documents/RefreshDocument/`)

- **`RefreshDocumentCommand.cs`** — command record, `ICommand<Deleted>` (matches `DeleteTournamentCommand`'s pattern — clearing a cached copy is a deletion of that cached representation).
- **`RefreshDocumentCommandHandler.cs`** — evicts the FusionCache tag scoped to that one document and deletes its blob. Always returns `Result.Deleted` — clearing the cache for a document that was never cached, or doesn't exist, is a no-op, not an error (same idempotency posture as [[project_delete_idempotency_convention]]). Tag is built the same way every other handler in the codebase builds a scoped eviction tag — a direct interpolated string matching `CacheDescriptors.Documents.Content`'s format (`neba:document:{name}`), not by reflecting on the descriptor's `Tags` collection, to match the established convention (see `EditSponsorCommandHandler`, `DeleteTournamentCommandHandler`, etc. — all hardcode the scoped tag string directly rather than deriving it from a `CacheDescriptor`).
- **`RefreshDocumentEndpoint.cs`** — route `DELETE {DocumentName}/cache` under `DocumentsEndpointGroup` (full path `documents/{DocumentName}/cache`), gated by the new permission. No validator file — `DocumentName` is a plain route-bound string with no structural rule beyond "present," same as `GetDocumentEndpoint`.
- **`RefreshDocumentSummary.cs`** — documents `204`/`401`/`403`, mirroring `ClearCacheSummary`'s style.

```csharp
// RefreshDocumentCommand.cs
using ErrorOr;

using Neba.Api.Messaging;

namespace Neba.Api.Features.Documents.RefreshDocument;

internal sealed record RefreshDocumentCommand : ICommand<Deleted>
{
    public required string DocumentName { get; init; }
}
```

```csharp
// RefreshDocumentCommandHandler.cs
using ErrorOr;

using Neba.Api.Messaging;
using Neba.Api.Storage;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Documents.RefreshDocument;

internal sealed class RefreshDocumentCommandHandler(
    IFusionCache fusionCache,
    IFileStorageService storageService)
        : ICommandHandler<RefreshDocumentCommand, Deleted>
{
    private const string DocumentsContainer = "bowlneba-private";

    public async Task<ErrorOr<Deleted>> HandleAsync(RefreshDocumentCommand command, CancellationToken cancellationToken)
    {
        await fusionCache.RemoveByTagAsync($"neba:document:{command.DocumentName}", token: cancellationToken);
        await storageService.DeleteAsync(DocumentsContainer, $"documents/{command.DocumentName}", cancellationToken);

        return Result.Deleted;
    }
}
```

```csharp
// RefreshDocumentEndpoint.cs
using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Documents.RefreshDocument;
using Neba.Api.Messaging;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Documents.RefreshDocument;

internal sealed class RefreshDocumentEndpoint(ICommandHandler<RefreshDocumentCommand, Deleted> commandHandler)
        : Endpoint<RefreshDocumentRequest>
{
    private readonly ICommandHandler<RefreshDocumentCommand, Deleted> _commandHandler = commandHandler;

    public override void Configure()
    {
        Delete("{DocumentName}/cache");
        Group<DocumentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Documents")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.RefreshDocument.PolicyName);

        Description(description => description
            .WithName("RefreshDocument")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden));
    }

    public override async Task HandleAsync(RefreshDocumentRequest req, CancellationToken ct)
    {
        var command = new RefreshDocumentCommand { DocumentName = req.DocumentName };
        await _commandHandler.HandleAsync(command, ct);

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}
```

```csharp
// RefreshDocumentSummary.cs
using FastEndpoints;

namespace Neba.Api.Features.Documents.RefreshDocument;

internal sealed class RefreshDocumentSummary : Summary<RefreshDocumentEndpoint>
{
    public RefreshDocumentSummary()
    {
        Summary = "Clears a single document's cached copy.";
        Description = "Evicts the document's FusionCache entry and deletes its cached copy from blob storage, so the next request to GetDocument re-fetches it from Google Drive. Requires the Documents.RefreshDocument permission.";

        Response(204, "Document cache cleared.");
        Response(401, "No valid bearer token provided.");
        Response(403, "The caller lacks the Documents.RefreshDocument permission.");
    }
}
```

### Contracts (`src/Neba.Api.Contracts/Documents/`)

```csharp
// RefreshDocument/RefreshDocumentRequest.cs
namespace Neba.Api.Contracts.Documents.RefreshDocument;

public sealed record RefreshDocumentRequest
{
    public required string DocumentName { get; init; }
}
```

```csharp
// IDocumentsApi.cs — add alongside the existing GetDocumentAsync method
[Delete("/documents/{documentName}/cache")]
Task<IApiResponse> RefreshDocumentAsync(string documentName, CancellationToken cancellationToken = default);
```

### Tests

- **New** `tests/Neba.Api.Tests/Features/Documents/RefreshDocument/RefreshDocumentCommandHandlerTests.cs` — real `IFusionCache` instance (per `ClearCacheCommandHandlerTests`'s pattern), `Mock<IFileStorageService>` with `MockBehavior.Strict`. Cases: evicts only the target document's tag (a probe entry tagged for a *different* document's tag must survive); deletes the correct blob path (`bowlneba-private`/`documents/{name}`); returns `Success` even when `DeleteAsync` returns `false` (nothing was cached).
- **New** `tests/Neba.Api.Tests/Features/Documents/RefreshDocument/RefreshDocumentEndpointTests.cs` — `Factory.Create<RefreshDocumentEndpoint>()`, following `GetDocumentEndpointTests.cs`'s shape: maps the route-bound `DocumentName` onto the command, asserts the route/policy via `endpoint.Definition`, and asserts `204` is sent.
- No new `Neba.TestFactory` factory needed — `RefreshDocumentCommand`/`RefreshDocumentRequest` are single-field records constructed inline in tests, same as `ClearCacheCommand`.

### Out of scope for Phase 1

- No change to `GetDocumentQueryHandler`/`GetDocumentQuery` — they already fall through to Google Drive and re-cache whenever the blob/FusionCache entry is missing, which is exactly the state this endpoint puts them in.
- No validation that `DocumentName` matches a configured `GoogleSettings.Documents` entry — clearing a nonexistent document's cache is harmless and idempotent, consistent with the decision above.

## Phase 2: UI

### Contracts (Refit client)

- Already covered in Phase 1 — `IDocumentsApi.RefreshDocumentAsync(string documentName, CancellationToken)` is a shared Contracts-layer change, consumed directly by the Website app once regenerated. No separate Website-side client interface needed.

### Components (`src/Neba.Website.Server/Documents/NebaDocument.razor`)

Everything lives in the one shared component — no changes needed to `TournamentRules.razor`, `Bylaws.razor`, or any future document page, since they already pass `DocumentId` (which is always the same string as the API document name, e.g. `"bylaws"`, `"tournament-rules"` — confirmed by cross-checking both pages' `GetDocumentAsync` calls).

- **New injects**: `ApiExecutor`, `IDocumentsApi`, `ToastService`, `NavigationManager` (mirrors the injection pattern in `TournamentRules.razor`/`Bylaws.razor` and `DebugCacheTools.razor`).
- **New field**: `_isRefreshing` (bool).
- **New method** `RefreshDocumentAsync()`: guards on `DocumentId` being set and not already refreshing; sets `_isRefreshing = true`; calls `DocumentsApi.RefreshDocumentAsync(DocumentId, ct)` via `ApiExecutor.ExecuteAsync`; on error, resets `_isRefreshing` and shows an error toast (component stays interactive, user can retry); on success, does a full reload via `NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true)` — same forced-reload pattern used by `Login.razor`/`Logout.razor`/`SetPassword.razor` — which re-runs the host page's `OnInitializedAsync` and re-fetches from (now-empty) cache.
- **New markup**: a "Refresh" button, gated by `<AuthorizeView Policy="@Permissions.RefreshDocument.PolicyName">` (matches the `@Permissions.X.PolicyName` convention used throughout the Website, e.g. `EditSponsor.razor`, `AccountMenu.razor` — not a raw `"Permission:..."` string literal), placed immediately next to the existing "Last updated: ..." line in **both** the desktop TOC sidebar (`.toc-sticky`) and the mobile TOC modal body — the two places that line is already duplicated. Disabled while `_isRefreshing` is true.
- **New loading feedback**: a second `<NebaLoadingIndicator IsVisible="@_isRefreshing" Text="Refreshing document..." Scope="LoadingIndicatorScope.Page" />`, rendered inside `neba-document-container` alongside the existing content (not replacing it, unlike the initial-load indicator) so the current document stays visible and readable while the clear-cache call is in flight. Because a successful refresh immediately triggers `forceLoad: true`, this indicator's only job is to cover the brief window between click and the browser beginning its full-page navigation — the reload itself then shows the page's own pre-existing "Loading tournament rules..."/"Loading bylaws..." indicator.
- **CSS**: small addition to `wwwroot/neba-document.css` for the new button (sized/positioned to sit inline with `.neba-document-toc-last-updated`), following that file's existing conventions (global selectors, not scoped `.razor.css`, per the component's documented CSS-location constraint).

No `DirtyFormGuard` (not a data-entry form), no new `<PageTitle>` (existing pages unchanged), no FAB (not a create action).

### Mockups

- [`mockups/refresh-document-cache/refresh-document-cache.html`](mockups/refresh-document-cache/refresh-document-cache.html) — single mockup (data-capture-style addition, no layout tradeoff left to weigh since placement was already confirmed). Shows the Tournament Rules page with the "Refresh" button next to "Last updated" in the desktop TOC sidebar and the mobile TOC modal, reusing the app's existing `.neba-btn.neba-btn-sm.neba-btn-secondary` button classes (no new button style invented). Includes clickable demo toggles for the three real states: default (Webmaster, idle), refreshing (button disabled, spinning icon, page-overlay spinner matching `NebaLoadingIndicator`'s look), and no-permission (button/row hidden entirely, matching `AuthorizeView`'s behavior).

### Tests

- **Edit** `tests/Neba.Website.Tests/Documents/NebaDocumentTests.cs` — add cases using the existing `_ctx.AddAuthorization()` / `SetAuthorized(...)` / `SetPolicies(...)` bUnit pattern (see `EditSponsorTests.cs`):
  - Refresh button is **not** rendered when the caller lacks `Documents.RefreshDocument`.
  - Refresh button **is** rendered when the caller holds it, for a component with `DocumentId` set.
  - Clicking it calls `IDocumentsApi.RefreshDocumentAsync` with the component's `DocumentId` (strict-mocked `IDocumentsApi`), and on success triggers a `forceLoad` navigation (assert via bUnit's `NavigationManager` test double, e.g. `_ctx.Services.GetRequiredService<FakeNavigationManager>()`/equivalent already used elsewhere in this test project).
  - On an error response, shows an error toast and does **not** navigate, and `_isRefreshing` resets (button re-enabled).
- No Playwright test — this is an internal component interaction fully coverable by bUnit (mocked API + navigation), consistent with the bUnit-vs-Playwright decision table in `new-endpoint`/`pull-request-prep` (bUnit for internal component logic; Playwright reserved for real browser + HTTP flows).

### Code

```razor
@* NebaDocument.razor — new @using/@inject lines, added after the existing @implements line *@
@using Neba.Api.Contracts.Documents
@using Neba.Api.Contracts.Security
@using Neba.Website.Server.Services

@inject IDocumentsApi DocumentsApi
@inject ApiExecutor ApiExecutor
@inject ToastService ToastService
@inject NavigationManager NavigationManager
```

```razor
@* Inside the .toc-sticky block, immediately after the existing
   @if (!string.IsNullOrWhiteSpace(lastUpdatedText)) block that renders
   neba-document-toc-last-updated — replace that block with this one *@
@if (!string.IsNullOrWhiteSpace(lastUpdatedText) || CanRefresh)
{
    <div class="neba-document-toc-last-updated">
        @if (!string.IsNullOrWhiteSpace(lastUpdatedText))
        {
            <span class="updated-text">@lastUpdatedText</span>
        }
        <AuthorizeView Policy="@Permissions.RefreshDocument.PolicyName">
            <Authorized>
                <button type="button" class="neba-btn neba-btn-sm neba-btn-secondary"
                        disabled="@_isRefreshing" @onclick="RefreshDocumentAsync"
                        title="Refresh from Google Drive">
                    <svg class="neba-document-refresh-icon" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                        <path d="M8 3a5 5 0 1 0 4.546 2.914.5.5 0 0 1 .908-.417A6 6 0 1 1 8 2v1z" />
                        <path d="M8 4.466V.534a.25.25 0 0 1 .41-.192l2.36 1.966c.12.1.12.284 0 .384L8.41 4.658A.25.25 0 0 1 8 4.466z" />
                    </svg>
                    <span>@(_isRefreshing ? "Refreshing..." : "Refresh")</span>
                </button>
            </Authorized>
        </AuthorizeView>
    </div>
}
```

The identical block (last-updated text + `AuthorizeView`-gated button) replaces the existing standalone `lastUpdatedText` rendering inside the mobile TOC modal body too — same markup, no changes needed beyond the one already shared by both spots today.

```razor
@* Refresh overlay — added as the first child inside the neba-document-container div,
   alongside (not replacing) the existing content, since Content is still visible during a refresh *@
<NebaLoadingIndicator IsVisible="@_isRefreshing" Text="Refreshing document..." Scope="LoadingIndicatorScope.Page" />
```

```csharp
// NebaDocument.razor @code — new field + property + method
private bool _isRefreshing;

private bool CanRefresh => !string.IsNullOrWhiteSpace(DocumentId);

private async Task RefreshDocumentAsync()
{
    if (!CanRefresh || _isRefreshing)
    {
        return;
    }

    _isRefreshing = true;
    StateHasChanged();

    var result = await ApiExecutor.ExecuteAsync(
        "Documents",
        "RefreshDocument",
        ct => DocumentsApi.RefreshDocumentAsync(DocumentId!, ct));

    if (result.IsError)
    {
        _isRefreshing = false;
        ToastService.Show("Refresh Failed", result.FirstError.Description, NotifySeverity.Error);
        StateHasChanged();
        return;
    }

    NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
}
```

```css
/* wwwroot/neba-document.css — small addition near the existing "Last Updated" rules */

.neba-document-toc-last-updated {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem;
}

.neba-document-refresh-icon {
    width: 14px;
    height: 14px;
    flex-shrink: 0;
}
```

```csharp
// tests/Neba.Website.Tests/Documents/NebaDocumentTests.cs — representative new cases
// (added to the existing test class; follows its established _ctx.Render<NebaDocument>(...) style)

[Fact(DisplayName = "Should not render refresh button when caller lacks RefreshDocument permission")]
public void Render_ShouldNotShowRefreshButton_WhenCallerLacksPermission()
{
    // Arrange
    var authContext = _ctx.AddAuthorization();
    authContext.SetAuthorized("test-user");

    // Act
    var cut = _ctx.Render<NebaDocument>(parameters => parameters
        .Add(p => p.Content, new MarkupString("<p>Body</p>"))
        .Add(p => p.DocumentId, "bylaws"));

    // Assert
    cut.FindAll("button[title='Refresh from Google Drive']").ShouldBeEmpty();
}

[Fact(DisplayName = "Clicking refresh calls the API and force-reloads on success")]
public async Task RefreshDocumentAsync_ShouldCallApiAndForceReload_WhenSuccessful()
{
    // Arrange
    var authContext = _ctx.AddAuthorization();
    authContext.SetAuthorized("test-user");
    authContext.SetPolicies(Permissions.RefreshDocument.PolicyName);

    var mockApi = new Mock<IDocumentsApi>(MockBehavior.Strict);
    mockApi
        .Setup(api => api.RefreshDocumentAsync("bylaws", It.IsAny<CancellationToken>()))
        .ReturnsAsync(/* IApiResponse success stub, per this project's existing Refit test helpers */);
    _ctx.Services.AddSingleton(mockApi.Object);
    _ctx.Services.AddSingleton(sp => new ApiExecutor(/* ... */));

    var cut = _ctx.Render<NebaDocument>(parameters => parameters
        .Add(p => p.Content, new MarkupString("<p>Body</p>"))
        .Add(p => p.DocumentId, "bylaws"));

    // Act
    await cut.Find("button[title='Refresh from Google Drive']").ClickAsync(new MouseEventArgs());

    // Assert
    mockApi.VerifyAll();
    var nav = _ctx.Services.GetRequiredService<FakeNavigationManager>();
    nav.Uri.ShouldNotBeNull(); // reload targets the same URI with forceLoad — assert via this project's existing NavigationManager test double conventions
}
```
