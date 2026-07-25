# Delete Tournament

Lets a Webmaster/Admin permanently delete a tournament from its detail page, mirroring the Delete Article pattern — with a new guard that blocks deletion once a tournament has any recorded championship/entry/result history.

## Decisions locked in during scoping

- **Permission/role**: reuses the existing `Tournaments.DeleteTournament` permission (added in commit `0d476c3b`, already granted only to `Webmaster` and `Admin` — `Manager`/`Tournament Director` do not have it). No new role/permission work in this plan; `DeleteTournamentEndpoint` just gates on `Tournaments.DeleteTournament.PolicyName`, same dynamic per-permission policy pattern `EditTournament`/`DeleteArticle` already use.
- **First real invariant — historical records block deletion**: `Tournament` → `TournamentSponsor`/`TournamentOilPattern` cascade-delete at the DB level today (acceptable — they're purely tournament-owned), but `HistoricalTournamentChampion`/`HistoricalTournamentEntry`/`HistoricalTournamentResult` also cascade, which would silently destroy championship history. This plan adds a guard: delete is refused if any of those three tables has a row for the tournament. This is the first of the "invariants coming" the feature description anticipates; no other invariants exist yet.
- **Status code for the guard**: `409 Conflict`, not `422`/`400` — per CLAUDE.md's retry test (valid input; current state blocks the operation; the identical request would succeed later once the historical records are gone/moved), matching how `Tournament.SponsorAlreadyAdded`/`TitleSponsorAlreadyAdded`/`SponsorNotAttached` are already modeled as `Conflict`.
- **No pre-emptive UI disabling**: the delete button is not disabled/hidden based on historical-record state ahead of time (would require a new field on `GetTournament`'s response serving only this one UI decision). The button always shows for authorized users; if blocked, the `409` is surfaced as an error toast after confirming, same as any other conflict.
- **File cleanup**: tournament logo (if any) is deleted via a background job, mirroring `EditTournament`'s `DeleteTournamentFilesJob`/`DeleteTournamentFilesJobHandler` (reused as-is, not duplicated).
- **UI entry point**: originally sketched as a sidebar "Danger zone" panel (`NewsDetail.razor`'s pattern), but `TournamentDetail.razor` has no always-rendered admin sidebar — its `<aside>` rail is public-facing and conditional. Revised during Phase 2 drafting: the Delete button sits next to the existing Edit Tournament button in the hero, both gated by their own `AuthorizeView Policy="..."` block, using the same shared `ConfirmActionModal` `NewsDetail.razor` uses. See Phase 2 for the exact markup.
- **Post-delete redirect**: `/tournaments` (the schedule list), matching Delete Article's redirect to `/news`.

## Phase 1: API

### Domain

**`TournamentErrors.cs`** (edit) — add alongside the other `Conflict` errors (`SponsorAlreadyAdded`, `TitleSponsorAlreadyAdded`, `SponsorNotAttached`):

```csharp
public static Error HasHistoricalRecords(TournamentId id)
    => Error.Conflict(
        code: "Tournament.HasHistoricalRecords",
        description: "This tournament has recorded championship, entry, or result history and cannot be deleted.",
        metadata: new Dictionary<string, object>
        {
            { "TournamentId", id.ToString() }
        });
```

No aggregate method needed — deletion is a repository-level removal (`appDbContext.Tournaments.Remove(tournament)`), not a domain state transition. `Tournament` gets no new public members.

### Database

No migration. No schema changes — the guard is enforced in application code before any delete is attempted, not via a DB constraint.

### Application — `Features/Tournaments/DeleteTournament/`

**`DeleteTournamentCommand.cs`**

```csharp
using ErrorOr;

using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Tournaments.DeleteTournament;

internal sealed record DeleteTournamentCommand
    : ICommand<Deleted>
{
    public required TournamentId TournamentId { get; init; }
}
```

**`DeleteTournamentCommandHandler.cs`**

```csharp
using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Database.Configurations;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Features.Tournaments.EditTournament;
using Neba.Api.Messaging;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Features.Tournaments.DeleteTournament;

internal sealed class DeleteTournamentCommandHandler(
        AppDbContext appDbContext,
        IBackgroundJobScheduler backgroundJobScheduler,
        IFusionCache cache)
    : ICommandHandler<DeleteTournamentCommand, Deleted>
{
    public async Task<ErrorOr<Deleted>> HandleAsync(DeleteTournamentCommand command, CancellationToken cancellationToken)
    {
        var tournament = await appDbContext.Tournaments
            .SingleOrDefaultAsync(t => t.Id == command.TournamentId, cancellationToken);

        if (tournament is null)
        {
            return TournamentErrors.TournamentNotFound(command.TournamentId);
        }

        if (await HasHistoricalRecordsAsync(tournament, cancellationToken))
        {
            return TournamentErrors.HasHistoricalRecords(command.TournamentId);
        }

        var seasonId = tournament.SeasonId;
        var logo = tournament.Logo;

        appDbContext.Tournaments.Remove(tournament);
        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.Id}", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:tournaments:{seasonId}", token: cancellationToken);

        if (logo is not null)
        {
            backgroundJobScheduler.Enqueue(new DeleteTournamentFilesJob
            {
                Files = [new TournamentFileReference { Container = logo.Container, Path = logo.Path }]
            });
        }

        return Result.Deleted;
    }

    private async Task<bool> HasHistoricalRecordsAsync(
        Domain.Tournament tournament, CancellationToken cancellationToken)
    {
        var tournamentDbId = appDbContext.Entry(tournament)
            .Property<int>(ShadowIdConfiguration.DefaultPropertyName).CurrentValue;

        return await appDbContext.HistoricalTournamentChampions
                .AnyAsync(c => c.TournamentId == tournamentDbId, cancellationToken)
            || await appDbContext.HistoricalTournamentEntries
                .AnyAsync(e => e.TournamentId == tournamentDbId, cancellationToken)
            || await appDbContext.HistoricalTournamentResults
                .AnyAsync(r => r.TournamentId == tournamentDbId, cancellationToken);
    }
}
```

**`DeleteTournamentEndpoint.cs`**

```csharp
using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Features.Tournaments.Domain;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Tournaments.DeleteTournament;

internal sealed class DeleteTournamentEndpoint(Messaging.ICommandHandler<DeleteTournamentCommand, Deleted> commandHandler)
    : Endpoint<DeleteTournamentRequest>
{
    private readonly Messaging.ICommandHandler<DeleteTournamentCommand, Deleted> _commandHandler = commandHandler;

    public override void Configure()
    {
        Delete("{id}");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.DeleteTournament.PolicyName);

        Description(description => description
            .WithName("DeleteTournament")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict));
    }

    public override async Task HandleAsync(DeleteTournamentRequest req, CancellationToken ct)
    {
        var command = new DeleteTournamentCommand { TournamentId = new TournamentId(req.Id) };
        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            if (result.FirstError.Type == ErrorType.NotFound)
            {
                await Send.NotFoundAsync(ct);
                // Stryker disable once Statement
                return;
            }

            await TournamentMutationResultSender.SendConflictOrValidationErrorsAsync(
                result.FirstError, result.Errors, error => AddError(error), Send.ErrorsAsync, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}
```

**`DeleteTournamentRequest.cs`**

```csharp
namespace Neba.Api.Features.Tournaments.DeleteTournament;

internal sealed class DeleteTournamentRequest
{
    public required string Id { get; set; }
}
```

**`DeleteTournamentRequestValidator.cs`**

```csharp
using FastEndpoints;

using FluentValidation;

namespace Neba.Api.Features.Tournaments.DeleteTournament;

internal sealed class DeleteTournamentRequestValidator
    : Validator<DeleteTournamentRequest>
{
    public DeleteTournamentRequestValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty()
            .WithErrorCode("DeleteTournamentRequest.IdRequired")
            .WithMessage("Id is required.")
            .Length(26)
            .WithErrorCode("DeleteTournamentRequest.IdInvalidLength")
            .WithMessage("Id must be a 26-character ULID.");
    }
}
```

**`DeleteTournamentSummary.cs`**

```csharp
using FastEndpoints;

namespace Neba.Api.Features.Tournaments.DeleteTournament;

internal sealed class DeleteTournamentSummary : Summary<DeleteTournamentEndpoint>
{
    public DeleteTournamentSummary()
    {
        Summary = "Deletes a tournament.";
        Description = "Permanently deletes the tournament, its sponsor links, and oil pattern assignments. " +
                      "Refuses with 409 if the tournament has recorded championship, entry, or result history. " +
                      "Requires the Tournaments.DeleteTournament permission.";

        Response(204, "Tournament deleted.");
        Response(401, "No valid bearer token provided.");
        Response(403, "Authenticated user does not have the Tournaments.DeleteTournament permission.");
        Response(404, "No tournament exists with the given ID.");
        Response(409, "The tournament has recorded championship, entry, or result history and cannot be deleted.");
    }
}
```

### Authorization

No changes — `Permissions.DeleteTournament` already exists and is seeded onto `Webmaster` (`Admin` gets it via `Permissions.List`). `docs/policies/README.md` needs no new row; the existing generic dynamic `Permission:{value}` row already documents this pattern.

### Contracts (`src/Neba.Api.Contracts/Tournaments/`)

**`ITournamentsApi.cs`** (edit) — add:

```csharp
/// <summary>
/// Deletes a tournament. Refuses with 409 if the tournament has recorded championship,
/// entry, or result history.
/// </summary>
[Delete("/tournaments/{id}")]
Task<IApiResponse> DeleteTournamentAsync(string id, CancellationToken cancellationToken = default);
```

No new DTO — `DELETE` takes only the route ID, same as `RemoveTournamentSponsorAsync`.

### Tests

- `DeleteTournamentCommandHandlerTests` — success path (tournament + sponsors + oil patterns removed, both cache tags evicted, no job enqueued when no logo), success path with logo (`DeleteTournamentFilesJob` enqueued with correct container/path), tournament-not-found (404/`NotFound`), blocked-by-champion/-entry/-result (three separate cases, each asserting `Conflict` with `Tournament.HasHistoricalRecords` and that nothing was removed/saved).
- Endpoint `Configure`/`HandleAsync` tests for `DeleteTournamentEndpoint`, using `Factory.Create<TEndpoint>()` and the existing FastEndpoints Stryker `ignore-methods` — mirrors `DeleteArticleEndpointAuthorizationTests`'s shape (watch for the FastEndpoints static-state leak documented in CLAUDE.md's Learnings if this test spins up a real `WebApplication`).
- No new test factories needed — `TournamentFactory`, `HistoricalTournamentChampionFactory`, `HistoricalTournamentEntryFactory`, `HistoricalTournamentResultFactory` already exist.

## Phase 2: UI

### Adjustment to the confirmed UI entry point

The "sidebar danger zone" decision was modeled on `NewsDetail.razor`'s layout, but `TournamentDetail.razor` doesn't have an always-rendered admin-actions sidebar — its `<aside class="td-body__rail">` is public-facing (price, sponsors, entry count, payout) and only renders conditionally (`HasRailContent`). The page's existing admin action (`Edit Tournament`) instead lives inline in the hero, next to the title. To stay consistent with *this* page's actual structure, the Delete button is placed as a sibling of the existing `Edit Tournament` button in the hero (`td-hero__edit-btn`'s container), not forced into the conditional rail. Same danger styling (`neba-btn-danger`, matching `NewsDetail`'s delete button), same `ConfirmActionModal` usage, same permission-gated `AuthorizeView` pattern — only the placement differs from the original sketch. Flagging this now since it changes what gets edited in `TournamentDetail.razor`.

### Pages

**`TournamentDetail.razor`** (edit) — add the `ToastService` inject (not currently present on this page):

```razor
@inject ApiExecutor ApiExecutor
@inject ITournamentsApi TournamentsApi
@inject ISponsorsApi SponsorsApi
@inject NavigationManager Navigation
@inject IClientTimeZoneService ClientTimeZoneService
@inject ToastService ToastService
```

Replace the existing Edit-only block in the hero (around line 94-101) with an admin-actions row containing both buttons:

```razor
<div class="td-hero__admin-actions">
    <AuthorizeView Policy="@Permissions.EditTournament.PolicyName">
        <Authorized>
            <a href="/tournaments/@Id/edit" class="neba-btn neba-btn-secondary td-hero__edit-btn">
                <span class="material-symbols-outlined">edit</span>
                Edit Tournament
            </a>
        </Authorized>
    </AuthorizeView>

    <AuthorizeView Policy="@Permissions.DeleteTournament.PolicyName">
        <Authorized>
            <button type="button" class="neba-btn neba-btn-danger td-hero__delete-btn" @onclick="OpenDeleteConfirm">
                <span class="material-symbols-outlined">delete</span>
                Delete Tournament
            </button>
        </Authorized>
    </AuthorizeView>
</div>
```

Add the confirm modal just before the closing `}` of the successful-render branch (same position `NewsDetail.razor` places its own, right after `</main>`):

```razor
    </main>

    <ConfirmActionModal IsOpen="@_isDeleteConfirmOpen"
                         Title="Delete tournament?"
                         Message="@($"This permanently removes \"{_model!.Name}\", its sponsor links, and oil pattern assignments. This can't be undone.")"
                         IsBusy="@_isDeleteBusy"
                         OnConfirm="ConfirmDeleteAsync"
                         OnCancel="@(() => _isDeleteConfirmOpen = false)" />
}
```

Add to `@code`:

```csharp
private bool _isDeleteConfirmOpen;
private bool _isDeleteBusy;

private void OpenDeleteConfirm()
{
    _isDeleteConfirmOpen = true;
}

private async Task ConfirmDeleteAsync()
{
    _isDeleteBusy = true;

    var result = await ApiExecutor.ExecuteAsync(
        "TournamentsApi",
        "DeleteTournament",
        ct => TournamentsApi.DeleteTournamentAsync(Id, ct));

    _isDeleteBusy = false;
    _isDeleteConfirmOpen = false;

    if (result.IsError)
    {
        ToastService.Show("Delete Failed", result.FirstError.Description, NotifySeverity.Error);
        return;
    }

    var deletedTournamentName = _model!.Name;
    ToastService.Show("Tournament Deleted", $"\"{deletedTournamentName}\" was successfully deleted.", NotifySeverity.Success);
    Navigation.NavigateTo("/tournaments");
}
```

`TournamentDetail.razor.css` (edit) — add the admin-actions row layout, alongside the existing `.td-hero__edit-btn` rule:

```css
.td-hero__admin-actions {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    flex-wrap: wrap;
    margin-top: 0.5rem;
}
```

(`.td-hero__edit-btn` keeps its existing rule for icon/gap spacing; `td-hero__delete-btn` reuses it too — extend the CSS selector to `.td-hero__edit-btn, .td-hero__delete-btn` rather than duplicating the block.)

### Components

No new components — reuses the existing shared `ConfirmActionModal` as-is.

### API Client

- **`ITournamentsApi.DeleteTournamentAsync`** — already added in Phase 1's Contracts work; Phase 2 just consumes it. No new Refit method needed here.

### State / Dirty Tracking

Not applicable — this is a confirm-and-fire action via a modal, not a form. No `DirtyFormGuard` involved (same as `NewsDetail`'s delete action).

### Page Title / Render Mode

No change — `TournamentDetail.razor` already sets `<PageTitle>@_model!.Name - BowlNEBA</PageTitle>` and already uses `@rendermode @(new InteractiveServerRenderMode(prerender: false))`.

### FAB / List Entry Point

Not applicable — this isn't a creatable-list feature.

### Mockups

- `docs/plans/mockups/delete-tournament/delete-tournament.html` — single data-capture-style mockup (a button + a confirm modal, not a layout with real display tradeoffs, so one mockup rather than 2–3 options). Shows the hero with the new "Delete Tournament" button placed next to the existing "Edit Tournament" button (per the placement adjustment above), the `ConfirmActionModal` confirm dialog (click "Delete Tournament" to open it), and the two outcome states side by side below: the success toast + redirect-to-`/tournaments` note, and the 409 historical-records conflict toast. Built from the actual `neba_theme.css`/`app.css` tokens and `TournamentDetail.razor.css` hero classes, not generic styling.

### Tests

**bUnit** — add to `tests/Neba.Website.Tests/Tournaments/Detail/TournamentDetailTests.cs`, mirroring `NewsDetailTests`' delete-button coverage exactly (uses the class's existing `_ctx`/`_mockApi`/`_authContext`/`_toastService`/`SetupSuccessResponse` fixtures — no new setup needed):

```csharp
// ── Delete tournament ─────────────────────────────────────────────────────

[Fact(DisplayName = "Should not show delete button when user lacks DeleteTournament permission")]
public void Render_ShouldNotShowDeleteButton_WhenUserLacksPermission()
{
    // Arrange
    _authContext.SetAuthorized("test-user");
    SetupSuccessResponse(TournamentDetailResponseFactory.Create());

    // Act
    var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

    // Assert
    cut.Markup.ShouldNotContain("td-hero__delete-btn");
}

[Fact(DisplayName = "Should show delete button when user has DeleteTournament permission")]
public void Render_ShouldShowDeleteButton_WhenUserHasPermission()
{
    // Arrange
    _authContext.SetAuthorized("test-user");
    _authContext.SetPolicies(Permissions.DeleteTournament.PolicyName);
    SetupSuccessResponse(TournamentDetailResponseFactory.Create());

    // Act
    var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

    // Assert
    cut.Find("button.td-hero__delete-btn").ShouldNotBeNull();
}

[Fact(DisplayName = "Should open confirm dialog with tournament name when delete button is clicked")]
public void Click_ShouldOpenConfirmDialog_WhenDeleteButtonIsClicked()
{
    // Arrange
    _authContext.SetAuthorized("test-user");
    _authContext.SetPolicies(Permissions.DeleteTournament.PolicyName);
    SetupSuccessResponse(TournamentDetailResponseFactory.Create(name: "NEBA Winter Championship"));
    var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));

    // Act
    cut.Find("button.td-hero__delete-btn").Click();

    // Assert
    cut.Markup.ShouldContain("Delete tournament?");
    cut.Markup.ShouldContain("NEBA Winter Championship");
}

[Fact(DisplayName = "Should close confirm dialog and stay on page when delete is cancelled")]
public void CancelDelete_ShouldCloseDialogAndStayOnPage_WhenCancelled()
{
    // Arrange
    _authContext.SetAuthorized("test-user");
    _authContext.SetPolicies(Permissions.DeleteTournament.PolicyName);
    SetupSuccessResponse(TournamentDetailResponseFactory.Create());
    var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));
    cut.Find("button.td-hero__delete-btn").Click();

    // Act
    cut.Find("button.confirm-action-modal-cancel").Click();

    // Assert
    cut.Markup.ShouldNotContain("Delete tournament?");
    var navigationManager = _ctx.Services.GetRequiredService<NavigationManager>();
    navigationManager.Uri.ShouldNotEndWith("/tournaments");
}

[Fact(DisplayName = "Should navigate to /tournaments when delete succeeds")]
public void ConfirmDelete_ShouldNavigateToTournaments_WhenDeleteSucceeds()
{
    // Arrange
    _authContext.SetAuthorized("test-user");
    _authContext.SetPolicies(Permissions.DeleteTournament.PolicyName);
    SetupSuccessResponse(TournamentDetailResponseFactory.Create(id: TournamentDetailResponseFactory.ValidId));

    using var deleteResponse = new StubApiResponse<object>
    {
        IsSuccessStatusCode = true,
        StatusCode = System.Net.HttpStatusCode.NoContent
    };
    _mockApi
        .Setup(x => x.DeleteTournamentAsync(TournamentDetailResponseFactory.ValidId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(deleteResponse);

    var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));
    cut.Find("button.td-hero__delete-btn").Click();

    // Act
    cut.Find("button.confirm-action-modal-confirm").Click();

    // Assert
    var navigationManager = _ctx.Services.GetRequiredService<NavigationManager>();
    navigationManager.Uri.ShouldEndWith("/tournaments");
    _toastService.Current.ShouldNotBeNull();
    _toastService.Current.Severity.ShouldBe(NotifySeverity.Success);
}

[Fact(DisplayName = "Should show error toast and stay on the page when delete is blocked by historical records")]
public void ConfirmDelete_ShouldShowErrorToastAndStayOnPage_WhenDeleteFails()
{
    // Arrange
    _authContext.SetAuthorized("test-user");
    _authContext.SetPolicies(Permissions.DeleteTournament.PolicyName);
    SetupSuccessResponse(TournamentDetailResponseFactory.Create(id: TournamentDetailResponseFactory.ValidId, name: "NEBA Winter Championship"));

    using var deleteResponse = new StubApiResponse<object>
    {
        IsSuccessStatusCode = false,
        StatusCode = System.Net.HttpStatusCode.Conflict
    };
    _mockApi
        .Setup(x => x.DeleteTournamentAsync(TournamentDetailResponseFactory.ValidId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(deleteResponse);

    var cut = _ctx.Render<TournamentDetail>(p => p.Add(x => x.Id, TournamentDetailResponseFactory.ValidId));
    cut.Find("button.td-hero__delete-btn").Click();

    // Act
    cut.Find("button.confirm-action-modal-confirm").Click();

    // Assert
    cut.Markup.ShouldNotContain("Delete tournament?");
    var navigationManager = _ctx.Services.GetRequiredService<NavigationManager>();
    navigationManager.Uri.ShouldNotEndWith("/tournaments");
    _toastService.Current.ShouldNotBeNull();
    _toastService.Current.Severity.ShouldBe(NotifySeverity.Error);
}
```

**Playwright** — add a delete section to `tests/e2e/TournamentDetail.spec.ts`, mirroring `News.spec.ts`'s delete section and reusing its `__mock/fail` approach (simulating the 409 rather than needing real seeded historical data) alongside the file's existing mock-ID constants:

```ts
const MOCK_TOURNAMENT_DELETE_ID = '01JX0000000000000000000050';
const MOCK_TOURNAMENT_DELETE_BLOCKED_ID = '01JX0000000000000000000051';

test.describe('Tournament Detail — delete tournament (unauthenticated)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });

  test('does not show the delete button', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_ID}`);
    await page.waitForSelector('.td-hero');
    await expect(page.locator('.td-hero__delete-btn')).toHaveCount(0);
  });
});

test.describe('Tournament Detail — delete tournament (authorized)', () => {
  test.use({ viewport: { width: 1200, height: 800 } });
  test.describe.configure({ mode: 'serial' });

  test.beforeEach(async ({ page }) => {
    await page.request.post('/__test/login?permissions=Tournaments.DeleteTournament');
  });

  test('shows the delete button', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_DELETE_ID}`);
    await page.waitForSelector('.td-hero');
    await expect(page.locator('.td-hero__delete-btn')).toBeVisible();
  });

  test('navigates back to the tournament schedule after confirming delete', async ({ page }) => {
    await page.goto(`/tournaments/${MOCK_TOURNAMENT_DELETE_ID}`);
    await page.waitForSelector('.td-hero');

    await page.locator('.td-hero__delete-btn').click();
    await expect(page.locator('.neba-modal-content')).toContainText('Delete tournament?');

    await page.locator('button.confirm-action-modal-confirm').click();

    await expect(page).toHaveURL(/\/tournaments$/);
  });

  test('shows a conflict toast and stays on the page when the tournament has historical records', async ({ page }) => {
    await page.request.post(
      `http://localhost:5151/__mock/fail?path=/tournaments/${MOCK_TOURNAMENT_DELETE_BLOCKED_ID}&status=409`);

    await page.goto(`/tournaments/${MOCK_TOURNAMENT_DELETE_BLOCKED_ID}`);
    await page.waitForSelector('.td-hero');

    await page.locator('.td-hero__delete-btn').click();
    await page.locator('button.confirm-action-modal-confirm').click();

    await expect(page.locator('.neba-toast')).toContainText('Delete Failed');
    await expect(page).toHaveURL(new RegExp(`/tournaments/${MOCK_TOURNAMENT_DELETE_BLOCKED_ID}$`));

    await page.request.post(
      `http://localhost:5151/__mock/reset?path=/tournaments/${MOCK_TOURNAMENT_DELETE_BLOCKED_ID}`);
  });
});
```
