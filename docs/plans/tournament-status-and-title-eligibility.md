# Tournament Status and Title Eligibility

Relates to GitHub issue #26. Replaces `Tournament.Complete` (bool) with a `TournamentStatus` (`Scheduled`/`Completed`/`Truncated`/`Cancelled`), adds `TournamentType.MinimumEntries`, and adds a `Tournament.TitleEligible` flag so a tournament can be marked as counting toward season stats without counting toward a title — the case that prompted this work: a doubles event shortened by a state of emergency (issue #26) counted for stats but not for a title.

## Decisions locked in during scoping

- **Criticality**: not critical. Affects title/HOF accuracy (reputationally significant), but moves no money, touches no new PII, and a wrong flag is correctable via admin edit. Full NFR checkpoint skipped.
- **`TournamentType.MinimumEntries`**: new `int` property on the existing SmartEnum (`src/Neba.Api/Features/Tournaments/Domain/TournamentType.cs`), alongside `TeamSize`/`ActiveFormat`. Not stored per-tournament — read live off the SmartEnum only at the moment a tournament transitions to `Completed`.
- **`TournamentStatus`**: new SmartEnum (`Scheduled`, `Completed`, `Truncated`, `Cancelled`), following the existing `PublicationStatus`/`BowlingCenterStatus` pattern. Replaces `Tournament.Complete`.
  - `Scheduled` — default; covers both "not started" and "in progress" (`StartDate`/`EndDate` already distinguish those).
  - `Completed` — the tournament ran its full planned format. The only status that can ever be title-eligible. Covers *both* "met minimum entries" and "held anyway despite falling short" — those are distinguished by `TitleEligible`, not by a separate status.
  - `Truncated` — held, but didn't finish as planned (e.g. finals cancelled for a state of emergency, seeding-based payout instead). Matches the Feb 2026 doubles in issue #26. Never title-eligible, regardless of entries. `StatsEligible` is set independently and can be `true` (per the issue #26 resolution: participation + qualifying bonus points still counted).
  - `Cancelled` — no official NEBA event happened under this record (nothing bowled at all, or entries too low to sanction — any resulting sweeper is untracked). Never stats- or title-eligible. Still persisted as a Tournament record (not deleted), so it stays visible in schedule/history; stat/BOY calculations simply exclude it.
- **`Tournament.TitleEligible`**: new persisted `bool`. Set once, only when transitioning to `Completed`, as `entryCount >= TournamentType.MinimumEntries`. Never recomputed afterward — this is what makes a later `MinimumEntries` change non-retroactive (the completed tournament's flag was already frozen at completion time; only future completions see the new value). Always `false` for every other status.
- **No new `EntryCount` field on `Tournament`.** Entry count is already computed at query time elsewhere (`GetTournamentQueryHandler`, from `HistoricalTournamentEntries` or distinct `SquadScore` bowler/squad pairs ÷ `TeamSize`) and is already displayed for both legacy and website-tracked tournaments — no need to persist it separately.
- **BOY point calculation**: unchanged by this plan. Already gated by `StatsEligible` alone; `Cancelled` tournaments are simply excluded from whatever query feeds that calculation (they carry no results). HOF point calculation is explicitly out of scope — revisited separately when HOF is ported to the website.
- **Status triggers**:
  - `Completed` stays driven by the existing legacy sync job (`CompleteTournamentSyncJob`, `src/Neba.Api/Legacy/Tournaments/Complete/`), which already calls `tournament.CompleteTournament()` on the Software's "tournament completed" event. This plan updates that call to also determine `TitleEligible` from entry count vs. `MinimumEntries`.
  - `Truncated` and `Cancelled` are new, manual, board-driven actions from the website admin UI — the legacy system has no way to express either, so they never come from the sync job.
- **Authorization**: new dedicated policy (`ManageTournamentStatus` or similar), not a reuse of `EditTournament` — status changes are judgment calls distinct from routine editing. Granted to the same roles as `EditTournament` today: Admin (via `Permissions.List`), Webmaster, Manager, TournamentDirector.
- **UI flow** (confirmed): Tournament detail/edit view shows current status. While `Scheduled`, two actions are available: **Mark Truncated** and **Cancel Tournament** (no manual "Complete" action — stays legacy-job-driven). Each opens a confirmation modal stating the consequence in plain terms, no rationale/notes field (governance/audit fields from issue #26 are deferred to a future issue). Once `Truncated`/`Cancelled`/`Completed`, the tournament is terminal — no further status action shown.

## Phase 1: API

### Domain

**`Features/Tournaments/Domain/TournamentType.cs`** — add `MinimumEntries`:

```csharp
public static readonly TournamentType Singles = new(nameof(Singles), 100, 1, true, 50);
public static readonly TournamentType Doubles = new(nameof(Doubles), 200, 2, true, 25);
public static readonly TournamentType Trios = new(nameof(Trios), 300, 3, true, 10);
public static readonly TournamentType Baker = new(nameof(Baker), 500, 5, true, 10);
public static readonly TournamentType NonChampions = new("Non-Champions", 101, 1, true, 30);
public static readonly TournamentType TournamentOfChampions = new("Tournament of Champions", 102, 1, true, 20);
public static readonly TournamentType Invitational = new(nameof(Invitational), 103, 1, true, 50);
public static readonly TournamentType Masters = new(nameof(Masters), 104, 1, true, 50);
public static readonly TournamentType HighRoller = new("High Roller", 105, 1, false, 0);
public static readonly TournamentType Senior = new(nameof(Senior), 106, 1, true, 25);
public static readonly TournamentType Women = new(nameof(Women), 107, 1, true, 25);
public static readonly TournamentType OverForty = new("Over 40", 108, 1, false, 0);
public static readonly TournamentType FortyToFortyNine = new("40 - 49", 109, 1, false, 0);
public static readonly TournamentType Youth = new(nameof(Youth), 110, 1, true, 25);
public static readonly TournamentType Eliminator = new(nameof(Eliminator), 111, 1, false, 0);
public static readonly TournamentType SeniorAndWomen = new("Senior / Women", 112, 1, true, 40);
public static readonly TournamentType OverUnderFiftyDoubles = new("Over/Under 50 Doubles", 201, 2, true, 15);
public static readonly TournamentType OverUnderFortyDoubles = new("Over/Under 40 Doubles", 202, 2, false, 0);

private TournamentType(string name, int value, int teamSize, bool activeFormat, int minimumEntries)
    : base(name, value)
{
    TeamSize = teamSize;
    ActiveFormat = activeFormat;
    MinimumEntries = minimumEntries;
}

public int TeamSize { get; }
public bool ActiveFormat { get; }

/// <summary>
/// The minimum number of entries (bowlers for a 1-bowler format, teams otherwise) required for a
/// completed tournament of this type to count toward a NEBA title. Not retroactive — a change to
/// this value only affects tournaments completed after the change; <see cref="Tournament.TitleEligible"/>
/// is frozen at the moment a tournament transitions to <see cref="TournamentStatus.Completed"/>.
/// </summary>
public int MinimumEntries { get; }
```

**`Features/Tournaments/Domain/TournamentStatus.cs`** (new):

```csharp
using Ardalis.SmartEnum;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// The lifecycle state of a <see cref="Tournament"/>.
/// </summary>
public sealed class TournamentStatus
    : SmartEnum<TournamentStatus>
{
    /// <summary>
    /// The tournament has been created but hasn't finished — covers both "not yet started" and
    /// "in progress"; <see cref="Tournament.StartDate"/>/<see cref="Tournament.EndDate"/> already
    /// distinguish those. Default status.
    /// </summary>
    public static readonly TournamentStatus Scheduled = new(nameof(Scheduled), 0);

    /// <summary>
    /// The tournament ran its full planned format to conclusion. The only status a tournament can
    /// be title-eligible from — see <see cref="Tournament.TitleEligible"/>.
    /// </summary>
    public static readonly TournamentStatus Completed = new(nameof(Completed), 1);

    /// <summary>
    /// The tournament was held but didn't finish as planned (e.g. finals cancelled for a state of
    /// emergency, seeding-based payout instead). Never title-eligible, regardless of entries.
    /// </summary>
    public static readonly TournamentStatus Truncated = new(nameof(Truncated), 2);

    /// <summary>
    /// No official NEBA event took place under this record — either nothing was bowled, or entries
    /// were too low to sanction it. Never stats- or title-eligible.
    /// </summary>
    public static readonly TournamentStatus Cancelled = new(nameof(Cancelled), 3);

    private TournamentStatus(string name, int value)
        : base(name, value)
    { }
}
```

**`Features/Tournaments/Domain/Tournament.cs`**:

```csharp
public TournamentStatus Status { get; private set; } = TournamentStatus.Scheduled;

/// <summary>
/// Whether this tournament counts toward a NEBA title. Set once, only when transitioning to
/// <see cref="TournamentStatus.Completed"/>, from entries compared against
/// <see cref="TournamentType.MinimumEntries"/> at that moment — never recomputed afterward, so a
/// later change to <see cref="TournamentType.MinimumEntries"/> can't retroactively affect it.
/// Always <see langword="false"/> for every other status.
/// </summary>
public bool TitleEligible { get; private set; }

/// <summary>
/// Marks the tournament complete. <paramref name="entryCount"/> is supplied by the caller (the
/// legacy completion sync today) and compared against <see cref="TournamentType.MinimumEntries"/>
/// to freeze <see cref="TitleEligible"/> — the aggregate enforces the rule, the caller supplies the
/// fact, per the cross-aggregate-data pattern.
/// </summary>
public ErrorOr<Success> CompleteTournament(int entryCount)
{
    if (Status != TournamentStatus.Scheduled)
    {
        return TournamentErrors.TournamentAlreadyFinalized;
    }

    Status = TournamentStatus.Completed;
    TitleEligible = entryCount >= TournamentType.MinimumEntries;

    return Result.Success;
}

/// <summary>
/// Marks the tournament truncated — held, but didn't finish as planned. A board-level decision,
/// made from the website admin panel; never title-eligible.
/// </summary>
public ErrorOr<Success> TruncateTournament()
{
    if (Status != TournamentStatus.Scheduled)
    {
        return TournamentErrors.TournamentAlreadyFinalized;
    }

    Status = TournamentStatus.Truncated;

    return Result.Success;
}

/// <summary>
/// Marks the tournament cancelled — no official NEBA event took place under this record. A
/// board-level decision, made from the website admin panel.
/// </summary>
public ErrorOr<Success> CancelTournament()
{
    if (Status != TournamentStatus.Scheduled)
    {
        return TournamentErrors.TournamentAlreadyFinalized;
    }

    Status = TournamentStatus.Cancelled;

    return Result.Success;
}
```

`AddResult`'s guard:

```csharp
/// <summary>
/// Records a bowler's result; returns an error if the tournament isn't finalized (Completed or
/// Truncated) or the bowler already has a result recorded.
/// </summary>
/// <remarks>
/// Results are only ever added after the tournament is finalized, never before — this mirrors
/// nebamgmt-v3's own flow, where finalists' results are entered manually while the tournament is
/// still open, then the Software auto-generates participation placeholders for everyone else at
/// the moment it's marked complete (see `docs/plans/software-backdoor-complete-tournament.md`).
/// The website only sees results once, in one batch, via <c>SyncTournamentResultsJob</c>, which
/// is chained from <c>CompleteTournamentSyncJob</c> and therefore always runs after
/// <see cref="CompleteTournament"/>/<see cref="TruncateTournament"/> has already set
/// <see cref="Status"/> — this guard exists to enforce that ordering on the website side too, not
/// to support results trickling in before finalization the way the Software's manual entry does.
/// </remarks>
public ErrorOr<Success> AddResult(BowlerId bowlerId, int place, decimal prizeMoney, int points)
{
    if (Status is TournamentStatus.Scheduled or TournamentStatus.Cancelled)
    {
        return TournamentErrors.TournamentNotFinalized;
    }
    // ... unchanged below (ResultAlreadyRecorded check, TournamentResult.Create, add, return)
}
```

**`Features/Tournaments/Domain/TournamentErrors.cs`** — replace `AlreadyComplete`/`TournamentNotComplete`:

```csharp
public static Error TournamentAlreadyFinalized
    => Error.Conflict(
        code: "Tournament.AlreadyFinalized",
        description: "This tournament has already been completed, truncated, or cancelled.");

public static Error TournamentNotFinalized
    => Error.Conflict(
        code: "Tournament.NotFinalized",
        description: "Results may only be recorded for a completed or truncated tournament.");
```

### Application / Legacy Sync

**`Legacy/Tournaments/Complete/CompleteTournamentSyncJob.cs`** — inject `IDbConnection legacyConnection`, query entry count before completing:

```csharp
internal sealed class CompleteTournamentSyncJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    IBackgroundJobClient jobs,
    IFusionCache cache,
    IEmailSender emailSender,
    IDiscordNotifier discordNotifier,
    ILogger<CompleteTournamentSyncJob> logger)
{
    public async Task SyncAsync(int legacyTournamentId, CancellationToken ct)
    {
        // ... unchanged: actor scope, tournament lookup, unlinked-tournament email/alert

        var entryCount = await GetEntryCountAsync(legacyTournamentId, tournament.TournamentType.TeamSize, ct);

        var completeResult = tournament.CompleteTournament(entryCount);
        if (completeResult.IsError)
        {
            // AlreadyFinalized: expected on retry/re-fire, or if the tournament was already
            // Truncated/Cancelled from the admin panel before the legacy event arrived. Not fatal —
            // still chain the follow-on jobs below; they're each independently safe to re-run.
            logger.LogLegacyTournamentAlreadyCompleteForResultSync(legacyTournamentId);
        }
        else
        {
            await db.SaveChangesAsync(ct);
            await cache.RemoveByTagAsync($"neba:tournaments:{tournament.Id}", token: ct);
            await cache.RemoveByTagAsync($"neba:tournaments:{tournament.SeasonId}", token: ct);
        }

        // ... unchanged: jobs.Enqueue<SyncTournamentResultsJob>, jobs.Schedule<GenerateSeasonStatsJob>
    }

    // Same distinct-(BowlerId, SquadId)-pairs-÷-TeamSize formula GetTournamentQueryHandler already
    // uses for website-tracked tournaments (see EntryCount there) — just read from the legacy DB,
    // since this job runs before SyncTournamentResultsJob populates any website-side SquadScore rows.
#pragma warning disable DAP005
    private async Task<int> GetEntryCountAsync(int legacyTournamentId, int teamSize, CancellationToken ct)
    {
        var pairCount = await legacyConnection.QuerySingleAsync<int>(
            new CommandDefinition(
                """
                SELECT COUNT(DISTINCT CONCAT(s.BowlerId, '-', q.SquadId))
                FROM Stats s
                INNER JOIN Stats_QualifyingStats q ON s.Id = q.Id
                WHERE s.TournamentId = @TournamentId
                """,
                new { TournamentId = legacyTournamentId },
                cancellationToken: ct));

        return pairCount / teamSize;
    }
#pragma warning restore DAP005
}
```

**New: `Features/Tournaments/TruncateTournament/`** (mirrors `DeleteTournament`'s no-body-request shape exactly):

```csharp
// TruncateTournamentRequest.cs
internal sealed class TruncateTournamentRequest
{
    public required string Id { get; set; }
}

// TruncateTournamentRequestValidator.cs — identical rule shape to DeleteTournamentRequestValidator
// (NotEmpty + 26-char ULID length), error codes renamed to TruncateTournamentRequest.*.

// TruncateTournamentCommand.cs
internal sealed record TruncateTournamentCommand
    : ICommand<Success>
{
    public required TournamentId TournamentId { get; init; }
}

// TruncateTournamentCommandHandler.cs
internal sealed class TruncateTournamentCommandHandler(AppDbContext appDbContext, IFusionCache cache)
    : ICommandHandler<TruncateTournamentCommand, Success>
{
    public async Task<ErrorOr<Success>> HandleAsync(TruncateTournamentCommand command, CancellationToken cancellationToken)
    {
        var tournament = await appDbContext.Tournaments
            .SingleOrDefaultAsync(t => t.Id == command.TournamentId, cancellationToken);

        if (tournament is null)
        {
            return TournamentErrors.TournamentNotFound(command.TournamentId);
        }

        var truncateResult = tournament.TruncateTournament();
        if (truncateResult.IsError)
        {
            return truncateResult.Errors;
        }

        await appDbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.Id}", token: cancellationToken);
        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.SeasonId}", token: cancellationToken);

        return Result.Success;
    }
}
```

**New: `Features/Tournaments/CancelTournament/`** — identical shape, calling `tournament.CancelTournament()`.

### API

```csharp
// TruncateTournamentEndpoint.cs
internal sealed class TruncateTournamentEndpoint(Messaging.ICommandHandler<TruncateTournamentCommand, Success> commandHandler)
    : Endpoint<TruncateTournamentRequest>
{
    public override void Configure()
    {
        Patch("{id}/truncate");
        Group<TournamentsEndpointGroup>();

        Options(options => options
            .WithVersionSet("Tournaments")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.ManageTournamentStatus.PolicyName);

        Description(description => description
            .WithName("TruncateTournament")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict));
    }

    public override async Task HandleAsync(TruncateTournamentRequest req, CancellationToken ct)
    {
        var command = new TruncateTournamentCommand { TournamentId = new TournamentId(req.Id) };
        var result = await commandHandler.HandleAsync(command, ct);

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

`CancelTournamentEndpoint` is identical, `Patch("{id}/cancel")`, `WithName("CancelTournament")`.

**`Features/Tournaments/GetTournament/GetTournamentQueryHandler.cs` / `TournamentDetailDto.cs`** — add to the existing DTO construction:

```csharp
Status = tournament.Status.Name,
TitleEligible = tournament.TitleEligible,
```

### Contracts

**`Neba.Api.Contracts/Security/Permission.cs`** (in the `#region Tournaments` block):

```csharp
/// <summary>
/// Permission to mark a tournament truncated or cancelled.
/// </summary>
public static readonly Permissions ManageTournamentStatus = new("Tournaments.ManageTournamentStatus", "Manage Tournament Status");

public static readonly IReadOnlyCollection<Permissions> TournamentManagementPermissions =
[
    CreateTournament,
    ManageTournamentSponsors,
    EditTournament,
    DeleteTournament,
    ManageTournamentStatus
];
```

**`Security/Infrastructure/SecurityRoleSeeder.cs`** — add `Permissions.ManageTournamentStatus` to the `Webmaster`, `Manager`, and `TournamentDirector` permission lists, alongside `EditTournament`.

**`Neba.Api.Contracts/Tournaments/GetTournament/TournamentDetailResponse.cs`**:

```csharp
/// <summary>
/// Current lifecycle status of the tournament ("Scheduled", "Completed", "Truncated", "Cancelled").
/// </summary>
public required string Status { get; init; }

/// <summary>
/// Whether this tournament counts toward a NEBA title.
/// </summary>
public required bool TitleEligible { get; init; }
```

### Database

**`Database/Configurations/TournamentConfiguration.cs`** — add, alongside the existing `StatsEligible` configuration:

```csharp
builder.Property(tournament => tournament.Status)
    .IsRequired();

builder.Property(tournament => tournament.TitleEligible)
    .IsRequired();
```

`Status` picks up the int-value `SmartEnumConverter` automatically via the model-wide `ConfigureSmartEnum()` convention (same as `TournamentType`) — no explicit `.HasConversion()` needed.

**EF migration**: drop the `complete` column, add `status` (int, not null) and `title_eligible` (bool, not null) columns. Backfill (confirmed): existing `complete = true` rows → `status = 1` (`Completed`), `title_eligible = true`. Existing `complete = false` rows → `status = 0` (`Scheduled`), `title_eligible = false`.

### Tests

- **`Neba.TestFactory/Tournaments/TournamentFactory.cs`**: add optional `TournamentStatus? status` and, when `status == Completed`, an optional `int? entryCountForTitleEligibility` param to `Create()` — after building the tournament via `Tournament.Create(...)`, call `tournament.CompleteTournament(...)`/`TruncateTournament()`/`CancelTournament()` based on the requested status before returning it. Default: no call, tournament stays `Scheduled`.
- New unit tests (`Neba.Api.Tests`, `[UnitTest]`, `[Component("Tournaments")]`): `CompleteTournament` (title-eligible when entries meet `MinimumEntries`, not title-eligible when they don't, `AlreadyFinalized` guard from each non-`Scheduled` status), `TruncateTournament`/`CancelTournament` (success + `AlreadyFinalized` guard), `AddResult` (allowed from `Completed`/`Truncated`, rejected from `Scheduled`/`Cancelled` via `TournamentNotFinalized`).
- New command handler tests: `TruncateTournamentCommandHandlerTests`/`CancelTournamentCommandHandlerTests` (`TournamentNotFound`, success + cache eviction, `AlreadyFinalized` passthrough).
- New endpoint `Configure` tests for both endpoints (route, policy, per the FastEndpoints unit-test `ignore-methods` conventions in CLAUDE.md).
- Update `CompleteTournamentSyncJobTests` for the new `GetEntryCountAsync` query and `TitleEligible` assertions (both above and below `MinimumEntries`).

## Phase 2: UI

### Mockups

- [`mockups/tournament-status-and-title-eligibility/tournament-detail-status-actions.html`](mockups/tournament-status-and-title-eligibility/tournament-detail-status-actions.html) — confirmed. Shows the hero fragment across four states (Scheduled/Truncated/Cancelled/Completed-under-minimum) via a state switcher, plus the Truncate/Cancel confirm modal. Source of truth for the markup below: Truncate/Cancel reuse `neba-btn-secondary`'s visual weight (same as Edit — deliberate-but-not-destructive), distinct only by icon; the lifecycle status badge and eligibility note are new, page-scoped elements.

### Pages

**`Tournaments/Detail/TournamentDetail.razor`** — the only page touched. New markup in the hero, matching the confirmed mockup:

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

    @if (_model!.Status == "Scheduled")
    {
        <AuthorizeView Policy="@Permissions.ManageTournamentStatus.PolicyName">
            <Authorized>
                <button type="button" class="neba-btn neba-btn-secondary td-hero__truncate-btn" @onclick="OpenTruncateConfirm">
                    <span class="material-symbols-outlined">warning</span>
                    Mark Truncated
                </button>
                <button type="button" class="neba-btn neba-btn-secondary td-hero__cancel-btn" @onclick="OpenCancelConfirm">
                    <span class="material-symbols-outlined">block</span>
                    Cancel Tournament
                </button>
            </Authorized>
        </AuthorizeView>
    }

    <AuthorizeView Policy="@Permissions.DeleteTournament.PolicyName">
        <Authorized>
            <button type="button" class="neba-btn neba-btn-danger td-hero__delete-btn" @onclick="OpenDeleteConfirm">
                <span class="material-symbols-outlined">delete</span>
                Delete Tournament
            </button>
            <HelpButton DocName="delete-tournament" Light="true" />
        </Authorized>
    </AuthorizeView>
</div>
```

Status badge — placed right after `<h1 class="td-hero__title">`, only rendered for the two non-default statuses:

```razor
@if (_model.Status is "Truncated" or "Cancelled")
{
    <span class="td-status-badge td-status-badge--@(_model.Status.ToLowerInvariant())">
        <span class="material-symbols-outlined">@(_model.Status == "Truncated" ? "warning" : "block")</span>
        @_model.Status
    </span>
}
```

Eligibility note — placed after `td-hero__chips`, covering both the `Truncated` and `Completed`-but-not-title-eligible cases the mockup shows:

```razor
@if (_model.Status == "Truncated")
{
    <div class="td-eligibility-note">
        <span class="material-symbols-outlined">warning</span>
        <div>
            <strong>Does not count toward a title</strong>
            <span class="sub">This tournament didn't finish as planned. Results and stats still count.</span>
        </div>
    </div>
}
else if (_model.Status == "Completed" && !_model.TitleEligible)
{
    <div class="td-eligibility-note">
        <span class="material-symbols-outlined">warning</span>
        <div>
            <strong>Completed — did not meet the minimum entries for a title</strong>
            <span class="sub">Stats still count.</span>
        </div>
    </div>
}
else if (_model.Status == "Cancelled")
{
    <div class="td-eligibility-note td-eligibility-note--cancelled">
        <span class="material-symbols-outlined">block</span>
        <div>
            <strong>No official results</strong>
            <span class="sub">This event did not run as a sanctioned NEBA tournament.</span>
        </div>
    </div>
}
```

Modals, alongside the existing `_isDeleteConfirmOpen` one:

```razor
<ConfirmActionModal IsOpen="@_isTruncateConfirmOpen"
                    Title="Mark tournament truncated?"
                    Message="This tournament will count toward season stats but will never be eligible for a title. This can't be undone."
                    IsBusy="@_isTruncateBusy"
                    OnConfirm="ConfirmTruncateAsync"
                    OnCancel="@(() => _isTruncateConfirmOpen = false)" />

<ConfirmActionModal IsOpen="@_isCancelConfirmOpen"
                    Title="Cancel this tournament?"
                    Message="This tournament will not count toward stats or a title. This can't be undone."
                    IsBusy="@_isCancelBusy"
                    OnConfirm="ConfirmCancelAsync"
                    OnCancel="@(() => _isCancelConfirmOpen = false)" />
```

`@code` additions, alongside the existing `_isDeleteConfirmOpen`/`OpenDeleteConfirm`/`ConfirmDeleteAsync`:

```csharp
private bool _isTruncateConfirmOpen;
private bool _isTruncateBusy;
private bool _isCancelConfirmOpen;
private bool _isCancelBusy;

private void OpenTruncateConfirm() => _isTruncateConfirmOpen = true;

private async Task ConfirmTruncateAsync()
{
    _isTruncateBusy = true;

    var result = await ApiExecutor.ExecuteAsync(
        "TournamentsApi",
        "TruncateTournament",
        ct => TournamentsApi.TruncateTournamentAsync(Id, ct));

    _isTruncateBusy = false;
    _isTruncateConfirmOpen = false;

    if (result.IsError)
    {
        ToastService.Show("Truncate Failed", result.FirstError.Description, NotifySeverity.Error);
        return;
    }

    ToastService.Show("Tournament Truncated", "Stats will still count; this tournament will not award a title.", NotifySeverity.Success);
    await ReloadTournamentAsync();
}

private void OpenCancelConfirm() => _isCancelConfirmOpen = true;

private async Task ConfirmCancelAsync()
{
    _isCancelBusy = true;

    var result = await ApiExecutor.ExecuteAsync(
        "TournamentsApi",
        "CancelTournament",
        ct => TournamentsApi.CancelTournamentAsync(Id, ct));

    _isCancelBusy = false;
    _isCancelConfirmOpen = false;

    if (result.IsError)
    {
        ToastService.Show("Cancel Failed", result.FirstError.Description, NotifySeverity.Error);
        return;
    }

    ToastService.Show("Tournament Cancelled", "This tournament no longer counts toward stats or a title.", NotifySeverity.Success);
    await ReloadTournamentAsync();
}
```

**`Tournaments/Detail/TournamentDetail.razor.css`** additions, following the file's existing token/naming conventions:

```css
.td-hero__truncate-btn,
.td-hero__cancel-btn {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    width: fit-content;
}

.td-status-badge {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    font-family: var(--neba-font-display);
    font-weight: 700;
    font-size: 0.78rem;
    padding: 0.3rem 0.7rem;
    border-radius: 999px;
    margin-bottom: 0.75rem;
}

.td-status-badge .material-symbols-outlined {
    font-size: 1rem;
}

.td-status-badge--truncated {
    background: color-mix(in srgb, var(--neba-warning) 22%, transparent);
    color: #ffd699;
}

.td-status-badge--cancelled {
    background: rgba(255, 255, 255, 0.14);
    color: rgba(255, 255, 255, 0.85);
}

.td-eligibility-note {
    margin-top: 1rem;
    background: white;
    color: var(--neba-text);
    border-left: 4px solid var(--neba-warning);
    border-radius: var(--neba-radius-lg);
    padding: 0.85rem 1rem;
    display: flex;
    gap: 0.65rem;
    align-items: flex-start;
    font-size: 0.9rem;
}

.td-eligibility-note--cancelled {
    border-left-color: var(--neba-gray-500);
}

.td-eligibility-note .material-symbols-outlined {
    color: var(--neba-warning);
    font-size: 1.2rem;
    margin-top: 0.1rem;
}

.td-eligibility-note--cancelled .material-symbols-outlined {
    color: var(--neba-gray-600);
}

.td-eligibility-note strong {
    display: block;
    margin-bottom: 0.15rem;
}

.td-eligibility-note .sub {
    color: var(--neba-gray-600);
    font-size: 0.85rem;
}
```

### Components

- No new standalone components — Truncate/Cancel reuse the existing `ConfirmActionModal` (two more instances, same as the single `Delete` one today).

### API Client

**`Neba.Api.Contracts/Tournaments/ITournamentsApi.cs`**:

```csharp
/// <summary>
/// Marks a tournament truncated — held, but didn't finish as planned. Never title-eligible.
/// </summary>
[Patch("/tournaments/{id}/truncate")]
Task<IApiResponse> TruncateTournamentAsync(string id, CancellationToken cancellationToken = default);

/// <summary>
/// Marks a tournament cancelled — no official NEBA event took place under this record.
/// </summary>
[Patch("/tournaments/{id}/cancel")]
Task<IApiResponse> CancelTournamentAsync(string id, CancellationToken cancellationToken = default);
```

**`Tournaments/Detail/TournamentDetailViewModel.cs`** — add, alongside `StatsEligible`:

```csharp
public required string Status { get; init; }
public required bool TitleEligible { get; init; }
```

**`Tournaments/Detail/TournamentDetailMappingExtensions.cs`** — add to the existing `ToViewModel()` object initializer:

```csharp
Status = response.Status,
TitleEligible = response.TitleEligible,
```

### State / Dirty-Tracking

- Not applicable — these are direct confirm-modal actions against an existing record, not a form, matching the existing `Delete Tournament` action (no `DirtyFormGuard` involvement there either).

### `<PageTitle>` / Render Mode

- No change — `TournamentDetail.razor` is an existing page with its own `<PageTitle>`/render mode already; this plan only adds actions and display to it.

### Tests

- **bUnit** (`Neba.Website.Tests`): new tests for `TournamentDetail.razor` covering: Truncate/Cancel buttons render only for `ManageTournamentStatus`-authorized users, only when `Status == "Scheduled"`; confirming each modal calls the right Refit method and triggers a reload; the status badge and each eligibility-note branch (`Truncated`, `Completed`+`!TitleEligible`, `Cancelled`) render correctly. Mirrors whatever existing bUnit coverage the `Delete Tournament` confirm flow already has.
- **Playwright** (`tests/e2e/`): one end-to-end flow (e.g. Cancel) exercising the real browser + HTTP round trip per the bUnit-vs-Playwright split in CLAUDE.md/`new-endpoint`.
- **Help docs**: out of scope for this plan's drafting — run `/help-documentation` against the new endpoints once implemented, per the existing process for admin actions (e.g. `delete-tournament`'s `HelpButton`).
