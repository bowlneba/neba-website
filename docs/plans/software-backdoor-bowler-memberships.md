# Software Backdoor — Bowler Memberships

Mirror the Software's `BowlerMemberships` table into the website's `bowler_memberships` table. Three per-action routes (new, update, delete), plus a weekly step that catches missed calls and removes website rows the Software no longer has.

It follows `docs/api/software-backdoor-plan.md`, builds on `docs/plans/software-backdoor-scaffolding.md`, and adds a table to the pattern in `docs/plans/software-backdoor-weekly-sync.md`. Worked examples in source: `Legacy/Bowlers/NewBowler.cs`, `UpdateBowler.cs`, `Legacy/Sync/BowlerWeeklySyncJob.cs`.

## Decision Recap

- **Three routes, one per Software event** (decided): `POST /legacy/bowler-memberships/new`, `/update`, `/delete`. Each takes `{ "bowlerMembershipId": N }`, the Software's `BowlerMemberships.Id`. That value becomes `BowlerMembership.LegacyId` on the website.
- **Action-in-path POST routes are a legacy-only exception** (decided). We considered one resource route with `PUT` and `DELETE` verbs (`PUT /legacy/bowler-memberships/{id}`, `DELETE /legacy/bowler-memberships/{id}`). We kept three POST routes to match every other `/legacy` route and because `WebsiteSyncAdapter` sends POST only. This is acceptable only because the whole `/legacy` folder, the adapter and the API key are deleted at Software sunset. **Do not copy this shape into the permanent API.** Routes outside `Legacy/` follow standard REST: a resource path, and the HTTP verb names the action (POST create, PUT/PATCH update, DELETE remove), with the id in the URL.
- **One route for add and check-in** (decided): every Software create path can send the new row's id after commit, so one route serves all of them. See "Software side" for the four hook points.
- **Delete sends the membership id only** (decided). The Software hard-deletes the row, so the website cannot read it back. It finds its own row by `LegacyId` and removes it.
- **New and update both upsert** (decided). Create the row when `LegacyId` is not found, overwrite it when it differs, do nothing when it matches. This differs from `NewBowler` (strict no-op) because "change membership" edits type and dates in place, and the weekly step needs the same behavior to heal a missed edit. The two routes stay separate so the Software call site and the logs say which event happened. Both enqueue the same job.
- **Delay, then create the bowler if missing** (decided).
  - Add and update jobs are scheduled 60 seconds out (`IBackgroundJobClient.Schedule`; `CompleteTournamentSyncJob` already does this with 10 minutes). Delete runs at once.
  - The delay exists because the Software sends the membership call before the bowler call on new-bowler and some check-in flows, and the adapter sends each call from its own background task, with no ordering. Its worst case is about 20 seconds (3 attempts, 5 s timeout, 2 s and 5 s waits). 60 seconds covers that plus queue time, so `NewBowlerSyncJob` normally wins.
  - If the bowler still has no website row, the job creates it from the Software's `Bowlers` row, the same as `UpdateBowlerSyncJob` and the weekly bowler job. `NewBowlerSyncJob` is a safe no-op when the bowler already exists, so a late New Bowler call does no harm.
  - If both jobs create the same bowler at once, the unique `LegacyId` index rejects one with `DbUpdateException`. The job throws, Hangfire retries, and the retry finds the bowler. No special handling.
  - Research confirmed: `NewBowlerSyncJob` logs and returns (no failure) when the bowler already exists or when the Software has no such row. Failing the membership job would not have triggered anything useful, which is why creating the bowler is the better fix.
- **System Import rows are never carried** (decided). `MembershipId = 8` ("System Import [No Data]", `BeginDate = EndDate = 1900-01-01`) is filtered in every Software query: single-row jobs skip it, the weekly query and the id set exclude it. The Software side sends every id; the website filters.
- **Cost comes from `Memberships.Cost` in the Software** (decided). The `BowlerMemberships` row has no cost column. The job joins to `Memberships` and copies the current price. A price change after purchase would show the new price. The Software holds no earlier value, so this cannot do better.
- **Weekly step** (decided): audit-timestamp window as for bowlers, plus an id-set comparison every run (decided: no count gate; it would miss one extra offset by one missing), plus a delete pass. Details below.
- **No deletes in the Software** (decided). Nothing in this plan touches Software data.

## Research: `nebamgmt-v3`

Read from the code by an agent; nothing ran. Column names for `BowlerMemberships` and `Memberships` come from the edmx SSDL (`Memberships.Id` int identity, `Memberships.Cost` decimal(5,2) not null), which you confirmed is good enough for column names.

### Legacy Schema Reference

| Table | Column | Type | Notes |
|---|---|---|---|
| `BowlerMemberships` | `Id` | int, identity | Becomes `LegacyId`. |
| | `BowlerId` | int | FK to `Bowlers`, no cascade. |
| | `MembershipId` | int | FK to `Memberships`. 8 = System Import. Maps by `MembershipType.TryFromLegacyId` (1, 2, 3, 4, 5, 6, 7, 9, 10 known). |
| | `BeginDate` | datetime | The BOM calls it `StartDate`. Convert with `DateOnly.FromDateTime`. |
| | `EndDate` | datetime | Same. |
| | `Audit_CreatedTimestamp`, `Audit_UpdatedTimestamp` | datetime, not null | Local Eastern time from the client PC. |
| `Memberships` | `Id`, `Name`, `Cost` | int, string, decimal | Abstract base of a TPT hierarchy (`YearlyMembership`, `TempMembership`). `Cost` is on the base table. |

`Audit_UpdatedTimestamp` is set on every create and on "change membership" (`BaseRepository.PopulateAddAudit` / `PopulateUpdateAudit`). Only the hand-run merge script skips it.

### Write paths

| # | Op | Path | Commit point | Id after commit |
|---|---|---|---|---|
| A | Create | Bowler profile "Update Membership" → `AddBowlerMembershipBO.Add` | `BowlerMembershipRepository.Add` 21-32 (`Commit()` line 29) | Returned by `Add` (`entity.Id`) |
| B | Create | Renew (VerifyBowler form) → `RenewBowlerMembershipBO.Update` → `BowlerRepository.Update` 111-127 | `Commit()` ~line 126 | On the tracked child entities (set to `Added`, lines 120-124) |
| C | Create | New bowler form → `AddBowlerBO.Add` → `BowlerRepository.Add` 45-59 | `Commit()` ~line 56 | On `entity.Memberships` after commit |
| D | Create | Squad check-in submit → `AddCheckInBO` → `CheckInRepository.Add(IEnumerable)` 28-36 | `Commit()` line 33 | On `checkInEntity.MembershipDue` (set in `ProcessCheckIn`, lines 178-184). **One submit can create several rows, or none.** |
| E | Update | Bowler profile "change membership" → `ChangeBO` → `BowlerMembershipRepository.Change` 52-68 | `Commit()` line 67 | Parameter `bowlerMembershipId` |
| F | Delete | Profile history grid → `DeleteBO` → `BowlerMembershipRepository.Delete(id)` 43-50 | `Commit()` line 49 | Parameter `id` |
| G | Delete | Delete check-in, user confirms "also delete the membership" → `CheckInRepository.Delete` 215-259 | `Commit()` line 258, only when `membershipId != 0` | Local `membershipId` |

Notes the paths share:

- All deletes are hard deletes (`Remove`, then `SaveChanges`). Deleting a membership cascade-deletes the check-in that paid for it (`CheckIns.MembershipDue_Id`, `ON DELETE CASCADE`). The website has no check-ins, so this needs nothing.
- Path C's duplicate-bowler branch (`AddBowlerBO` lines 91-109) calls the renew path (B) instead. Hooking B covers it.
- No `TransactionScope` anywhere. A hook placed after `Commit()` returns cannot be rolled back.
- Path B and C already send `bowlers/update` and `bowlers/new` today. Those stay.
- In C, `AddBowlerBO` sends `NotifyNewBowler` (line 117) **after** the repository returns, so a hook inside the repository sends the membership call **first**. This is why the website delays.
- Reservations (`CheckInRepository.Add` lines 39-51) pass no membership, so they create none.

### Excluded, with reason

- **`Data/NEBA.Data.Batch/Jobs/Repository.cs` `DeleteImportMembership`** (system-import removal): a developer batch tool that deletes only 1900-dated import rows, which the website never carries. No hook. The id-set comparison ignores type 8 on the Software side.
- **`Scripts/Merge Bowler.sql`**: hand-run SQL. It re-points `BowlerMemberships.BowlerId` without updating the audit timestamp and never calls the adapter. Nothing can hook it, and you decided not to cover it (Undecided #6).
- **`BowlerMergeMapper`**: copies scalar bowler fields only; it never touches memberships.
- **`MembershipRepository.Memberships.Remove` and the `Delete*MembershipDefinitionBO` classes**: delete membership type definitions, not bowler memberships.
- **Import tools** (`MembershipImport`, `MemberImport`): developer one-shots. They write type 8 and 1900-dated rows; if they ran with sync on, the website filters them.
- **`UpdateBowlerBO`**: the bowler comes from the form with an empty membership list, so it inserts none. Not traced for every caller.
- **Check-in edit of an existing check-in** (`CheckInRepository` lines 84-95): touches `Paid` and `Assignment`, then `ProcessCheckIn`. It creates a membership only when one is passed, which is path D.

## Website side

Folder `src/Neba.Api/Legacy/BowlerMemberships/`, deleted whole at sunset.

| File | Contents |
|---|---|
| `NewBowlerMembership.cs` | route `MapNewBowlerMembership`, `NewBowlerMembershipRequest`, validator |
| `UpdateBowlerMembership.cs` | route `MapUpdateBowlerMembership`, `UpdateBowlerMembershipRequest`, validator |
| `DeleteBowlerMembership.cs` | route `MapDeleteBowlerMembership`, `DeleteBowlerMembershipRequest`, validator |
| `BowlerMembershipSyncJob.cs` | `UpsertAsync`, `DeleteAsync`, row record, legacy extension, log messages |
| `Sync/BowlerMembershipWeeklySyncJob.cs` | the weekly step (next to `BowlerWeeklySyncJob.cs`) |

### Routes

Same shape as `MapNewBowler`. Relative paths (the group supplies `/legacy`).

```csharp
internal static class NewBowlerMembershipEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapNewBowlerMembership()
        {
            app.MapPost("/bowler-memberships/new", (
                NewBowlerMembershipRequest request,
                [FromServices] IValidator<NewBowlerMembershipRequest> validator,
                [FromServices] IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                jobs.Schedule<BowlerMembershipSyncJob>(
                    job => job.UpsertAsync(request.BowlerMembershipId, CancellationToken.None),
                    BowlerMembershipSyncJob.BowlerSettleDelay);

                return Results.Accepted();
            });
        }
    }
}

internal sealed record NewBowlerMembershipRequest(int BowlerMembershipId);

internal sealed class NewBowlerMembershipRequestValidator : AbstractValidator<NewBowlerMembershipRequest>
{
    public NewBowlerMembershipRequestValidator()
        => RuleFor(request => request.BowlerMembershipId).GreaterThan(0);
}
```

- `/update` is identical with `UpdateBowlerMembershipRequest` and the same `Schedule` call (an edit sent right after an add must not run before the add job).
- `/delete` is identical with `DeleteBowlerMembershipRequest`, but calls `jobs.Enqueue<BowlerMembershipSyncJob>(job => job.DeleteAsync(request.BowlerMembershipId, CancellationToken.None))` with no delay.
- Delete before add is safe: the delete finds nothing and returns, and the add job then reads the Software, finds the row gone, and logs and skips.
- Aggregator: add `app.MapNewBowlerMembership(); app.MapUpdateBowlerMembership(); app.MapDeleteBowlerMembership();` to `LegacyEndpoints.MapLegacyEndpoints()`.

### Domain construction and mutation

- **Create**: `Bowler.AddLegacyMembership(legacyId, type, cost, startDate, endDate)` already exists, is `internal` (same assembly), runs no eligibility rules, and rejects a repeated `LegacyId`. Call it directly. No new extension.
- **Update**: `BowlerMembership` properties `Type`, `Cost`, `StartDate`, `EndDate` have `private set`, and an extension member cannot reach them. Change them to `internal set`. This is a permanent aggregate change; it is what the first-class "change membership" feature will need. Then add the legacy-shaped logic as an extension, in `BowlerMembershipSyncJob.cs`:

```csharp
internal static class LegacyBowlerMembershipExtensions
{
    extension(BowlerMembership membership)
    {
        public ErrorOr<Updated> ApplyLegacyUpdate(MembershipType type, decimal cost, DateOnly startDate, DateOnly endDate)
        {
            // Same structural checks as BowlerMembership.CreateFromLegacy; no eligibility rules.
            if (cost < 0) return BowlerMembershipErrors.NegativeCost;
            if (endDate < startDate) return BowlerMembershipErrors.EndDateBeforeStartDate(startDate, endDate);

            membership.Type = type;
            membership.Cost = cost;
            membership.StartDate = startDate;
            membership.EndDate = endDate;

            return Result.Updated;
        }
    }
}
```

  `BowlerMembershipErrors` is `internal`, so the extension can use it. The errors are duplicated from `CreateFromLegacy`; that is deliberate, because the extension is deleted at sunset and `CreateFromLegacy` stays until then.
- **Delete**: the aggregate has no removal method and should not gain a legacy one. Load the row with `db.Set<BowlerMembership>().SingleOrDefaultAsync(m => m.LegacyId == id)` and call `db.Set<BowlerMembership>().Remove(entity)`. Do not use `ExecuteDeleteAsync`: it skips Audit.EntityFramework, so no audit event would record the delete.
- **Loading the bowler for create**: `Bowler.Memberships` is an `internal` navigation; load it with `.Include(bowler => bowler.Memberships)` so `AddLegacyMembership`'s duplicate check sees the existing rows.

### `BowlerMembershipSyncJob`

```csharp
internal sealed record LegacyBowlerMembershipRow(int Id, int BowlerId, int MembershipId, DateTime BeginDate, DateTime EndDate, decimal Cost);

internal sealed class BowlerMembershipSyncJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    IFusionCache cache,
    ILogger<BowlerMembershipSyncJob> logger)
{
    // Covers the Software adapter's worst case (3 attempts x 5 s timeout + 2 s + 5 s waits, about 20 s)
    // plus queue time, so NewBowlerSyncJob normally creates the bowler first.
    public static readonly TimeSpan BowlerSettleDelay = TimeSpan.FromSeconds(60);

    // The System Import membership: never carried to the website.
    public const int SystemImportMembershipId = 8;

    public async Task UpsertAsync(int legacyId, CancellationToken ct)
    {
        using var _ = LegacyActor.EnterActorScope();

        // See NewBowlerSyncJob.SyncAsync for the rationale on suppressing DAP005 here.
#pragma warning disable DAP005
        var row = await legacyConnection.QuerySingleOrDefaultAsync<LegacyBowlerMembershipRow>(
            """
            SELECT
                bm.Id,
                bm.BowlerId,
                bm.MembershipId,
                bm.BeginDate,
                bm.EndDate,
                m.Cost
            FROM
                BowlerMemberships bm
            INNER JOIN Memberships m ON m.Id = bm.MembershipId
            WHERE
                bm.Id = @Id
            """,
            new { Id = legacyId });
#pragma warning restore DAP005

        if (row is null)
        {
            // Deleted in the Software before this ran (delete beat the delayed add). Nothing to mirror.
            logger.LogLegacyBowlerMembershipNotFound(legacyId);
            return;
        }

        if (row.MembershipId == SystemImportMembershipId)
        {
            logger.LogLegacyBowlerMembershipSkippedSystemImport(legacyId);
            return;
        }

        if (!MembershipType.TryFromLegacyId(row.MembershipId, out var type))
        {
            // Retrying cannot fix an unknown type; log and stop.
            logger.LogLegacyBowlerMembershipTypeUnmapped(legacyId, row.MembershipId);
            return;
        }

        var bowler = await db.Set<Bowler>()
            .Include(b => b.Memberships)
            .SingleOrDefaultAsync(b => b.LegacyId == row.BowlerId, ct);

        var bowlerCreated = false;
        if (bowler is null)
        {
            // Decided: create it from the Software, as UpdateBowlerSyncJob does.
            bowler = await CreateBowlerAsync(row.BowlerId, ct);   // null when the Software has no such bowler or the name is invalid
            if (bowler is null) return;
            bowlerCreated = true;
        }

        var outcome = Apply(bowler, row, type);   // Created | Updated | Unchanged | Failed
        // ... SaveChangesAsync; when bowlerCreated, evict "neba:bowlers" (same tag the weekly bowler job evicts).
    }

    public async Task DeleteAsync(int legacyId, CancellationToken ct)
    {
        using var _ = LegacyActor.EnterActorScope();

        var existing = await db.Set<BowlerMembership>().SingleOrDefaultAsync(m => m.LegacyId == legacyId, ct);
        if (existing is null)
        {
            // Decided: idempotent. Already gone (retry, or never synced), the same as the delete
            // convention elsewhere (204 on a missing record).
            logger.LogLegacyBowlerMembershipAlreadyDeleted(legacyId);
            return;
        }

        db.Set<BowlerMembership>().Remove(existing);
        await db.SaveChangesAsync(ct);
    }
}
```

`Apply` is the single decision point, shared with the weekly step (make it `internal static`-ish or put both on a shared helper so the weekly job does not copy it):

- no website row for `row.Id` → `bowler.AddLegacyMembership(...)` → `Created`
- website row, `Type`, `Cost`, `StartDate`, `EndDate` all equal → `Unchanged`, no write
- website row, any differs → `existing.ApplyLegacyUpdate(...)` → `Updated`
- domain errors → log, `Failed`

**Missing-bowler creation** needs the bowler's `Bowlers` row mapped to a `Bowler`. That block (gender, date of birth, suffix, quoted nickname → `Bowler.CreateFromLegacy`) now exists in `UpdateBowlerSyncJob` and `BowlerWeeklySyncJob`. This would be the third copy, which `UpdateBowler.cs` says is the trigger to extract it. Extract it as `LegacyBowlerMapping.CreateBowler(LegacyBowlerRow row, ILogger logger)` returning `ErrorOr<Bowler>`, and switch the two existing callers to it in the same change. Existing `UpdateBowler` and weekly tests must pass unchanged.

#### Field mapping

| Software | Website | Edge |
|---|---|---|
| `MembershipId` | `MembershipType.TryFromLegacyId` | 8 → skip and log. Any other unknown id → log an error and stop (no throw: retries cannot fix it). The weekly step also counts it as failed and alerts. |
| `BeginDate`, `EndDate` (`datetime`) | `StartDate`, `EndDate` (`DateOnly`) | `DateOnly.FromDateTime`. Same convention as `LegacyMembershipRow` / `LegacyBowlerRow`. |
| `Memberships.Cost` | `Cost` | `decimal`. The website column is `numeric(5,2)`; all current prices fit. |
| `BowlerId` | the website bowler with that `LegacyId` | Missing → create the bowler (above). |

### Logging

`internal static partial class BowlerMembershipSyncJobLogMessages`. Every value is an id or a code, so no `[PersonalData]` or `[PrivateData]` is needed.

```csharp
internal static partial class BowlerMembershipSyncJobLogMessages
{
    [LoggerMessage(Level = LogLevel.Warning,
        Message = "No bowler membership found in neba-fwk for legacy id {LegacyBowlerMembershipId}; skipping sync.")]
    public static partial void LogLegacyBowlerMembershipNotFound(this ILogger<BowlerMembershipSyncJob> logger, int legacyBowlerMembershipId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Legacy bowler membership {LegacyBowlerMembershipId} is a System Import record; not carried to the website.")]
    public static partial void LogLegacyBowlerMembershipSkippedSystemImport(this ILogger<BowlerMembershipSyncJob> logger, int legacyBowlerMembershipId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Legacy bowler membership {LegacyBowlerMembershipId} has membership type {LegacyMembershipId}, which maps to no MembershipType; skipping sync.")]
    public static partial void LogLegacyBowlerMembershipTypeUnmapped(this ILogger<BowlerMembershipSyncJob> logger, int legacyBowlerMembershipId, int legacyMembershipId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Legacy bowler {LegacyBowlerId} (for membership {LegacyBowlerMembershipId}) is not in neba-fwk; cannot create the bowler or the membership.")]
    public static partial void LogLegacyBowlerMissingForMembership(this ILogger<BowlerMembershipSyncJob> logger, int legacyBowlerId, int legacyBowlerMembershipId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Legacy bowler {LegacyBowlerId} had no website record when membership {LegacyBowlerMembershipId} synced; created it.")]
    public static partial void LogLegacyBowlerCreatedForMembership(this ILogger<BowlerMembershipSyncJob> logger, int legacyBowlerId, int legacyBowlerMembershipId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Could not apply legacy bowler membership {LegacyBowlerMembershipId}: {Errors}.")]
    public static partial void LogLegacyBowlerMembershipApplyFailed(this ILogger<BowlerMembershipSyncJob> logger, int legacyBowlerMembershipId, string errors);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Legacy bowler membership {LegacyBowlerMembershipId} not on the website; nothing to delete.")]
    public static partial void LogLegacyBowlerMembershipAlreadyDeleted(this ILogger<BowlerMembershipSyncJob> logger, int legacyBowlerMembershipId);
}
```

### Cache eviction

`CacheDescriptors.cs` has no descriptor whose query reads `bowler_memberships`; season stats are computed from the Software's tables by `GenerateSeasonStatsJob`, not from website memberships. So membership writes evict nothing. The one exception: when the job **creates a bowler** (missing-bowler path), evict the tag `neba:bowlers`, the same tag `BowlerWeeklySyncJob` evicts. Revisit this section when a website feature starts reading memberships.

### DI registration of the three new validators

1. **Production**: add to `LegacyConfiguration.AddLegacy()`, after the `UpdateBowlerRequest` line:
   ```csharp
   builder.Services.AddScoped<IValidator<NewBowlerMembershipRequest>, NewBowlerMembershipRequestValidator>();
   builder.Services.AddScoped<IValidator<UpdateBowlerMembershipRequest>, UpdateBowlerMembershipRequestValidator>();
   builder.Services.AddScoped<IValidator<DeleteBowlerMembershipRequest>, DeleteBowlerMembershipRequestValidator>();
   ```
   plus `using Neba.Api.Legacy.BowlerMemberships;`. The job classes need no registration (`NewBowlerSyncJob` has none).
2. **This action's tests** (`BowlerMembershipTests.cs` `InitializeAsync()`): register the three new validators **and every validator already registered in the sibling files** (all eight today: `NewBowler`, `UpdateBowler`, `NewTournament`, `SyncSquadScores`, `CompleteTournament`, `UpdateTournamentStats`, `NewHallOfFameInduction`, `CompleteSeason`). Comment at the site, matching the existing one: `MapLegacyGroup()` maps every endpoint in the group, and an unregistered sibling validator throws when route metadata builds on the first request.
3. **Every existing test file that calls `MapLegacyGroup()` and registers validators** must also register the three new ones. Per `grep -rl "AddScoped<IValidator" tests/Neba.Api.Tests/Legacy` these are:
   - `Legacy/Bowlers/NewBowlerTests.cs`
   - `Legacy/Bowlers/UpdateBowlerTests.cs`
   - `Legacy/HallOfFame/HallOfFameTests.cs`
   - `Legacy/PingTests.cs`
   - `Legacy/Seasons/Complete/CompleteSeasonEndpointTests.cs`
   - `Legacy/Tournaments/Complete/CompleteTournamentEndpointTests.cs`
   - `Legacy/Tournaments/Complete/SyncTournamentResultsEndpointTests.cs`
   - `Legacy/Tournaments/NewTournamentTests.cs`
   - `Legacy/Tournaments/Stats/UpdateTournamentStatsEndpointTests.cs`
   - `Legacy/Tournaments/SyncSquadScoresTests.cs`

   `Legacy/HealthTests.cs` also calls `MapLegacyGroup()` but registers no validators; re-run the grep and read it before assuming it needs nothing.

## Weekly sync step

`Legacy/Sync/BowlerMembershipWeeklySyncJob.cs`, plus one line in `WeeklyLegacySync.Tables`. It adds a second pass the bowler step does not have, which the weekly-sync plan anticipated (its Undecided #6).

Register it **15 minutes after** the bowler step so bowlers exist first (decided; avoids two jobs creating the same bowler). `Tables` entries are `Action<IBackgroundJobClient, DateTime>`, so:

```csharp
(jobs, since) => jobs.Schedule<BowlerMembershipWeeklySyncJob>(
    job => job.SyncAsync(since, CancellationToken.None), TimeSpan.FromMinutes(15)),
```

`SyncAsync(DateTime since, CancellationToken ct)` runs two passes.

### Pass 1 — upsert what changed

```sql
SELECT bm.Id, bm.BowlerId, bm.MembershipId, bm.BeginDate, bm.EndDate, m.Cost
FROM BowlerMemberships bm
INNER JOIN Memberships m ON m.Id = bm.MembershipId
WHERE bm.Audit_UpdatedTimestamp >= @Since
  AND bm.MembershipId <> 8
ORDER BY bm.Id
```

Chunks of 50 with one `SaveChangesAsync` per chunk (the 64 KB audit property limit, as in `BowlerWeeklySyncJob`). Per chunk: load the website bowlers for the chunk's `BowlerId` values with `Include(Memberships)`; fetch any missing bowlers' `Bowlers` rows in one `WHERE Id IN @Ids` query and create them; call the shared `Apply` for each row. On `DbUpdateException`, retry row by row and send failures to the alert, copying `SaveChunkAsync` from `BowlerWeeklySyncJob`. Rows with an unmapped type count as failed. Evict `neba:bowlers` only if a bowler was created.

### Pass 2 — compare id sets, then delete extras

A timestamp cannot show a hard delete, so compare the full id sets on every run (decided). A count gate would pass when one extra and one missing row offset each other. The Software side is one `int` column, at most tens of thousands of rows, once a week, so the query is cheap.

1. `SELECT Id FROM BowlerMemberships WHERE MembershipId <> 8` into a `HashSet<int>`. Read the website's `LegacyId` values (`db.Set<BowlerMembership>().Where(m => m.LegacyId != null).Select(m => m.LegacyId!.Value)`) into a second set. Website-created rows (`LegacyId` null) are never in it.
2. Diff both ways: `extras` = website minus Software; `missing` = Software minus website. Both empty → log and stop.
3. **Website rows the Software lacks** (extras): re-check each candidate in the Software right before deleting (`SELECT Id FROM BowlerMemberships WHERE Id IN @Ids`, in chunks). This closes the window where a row was added after the id snapshot and synced by a push job. Delete the ones still absent, through `db.Set<BowlerMembership>().Remove` in chunks.
4. **Software rows the website lacks** (missing): run them through the same upsert as Pass 1 (in chunks of 50). A gap means a push call and the window both missed the row.
5. **Guards** (decided: cap deletes, alert):
   - Software id set empty while the website has legacy rows → delete nothing, send a Warning alert ("connection or wrong database?"), end.
   - More than 1 extra → delete nothing, send a Warning alert listing the count and the ids, end. A human decides. (Decided: the Software rarely deletes memberships, so one stray row per week is plausible and two or more is not.) Exactly 1 extra is re-checked and deleted, and still reported in the step 6 alert.
6. Log both set sizes and the diff. Send one Discord alert through `IManualInterventionNotifier` whenever anything was deleted or a guard fired, with the ids via `DiscordFieldFormatter.JoinIds`.

### Weekly log messages

```csharp
internal static partial class BowlerMembershipWeeklySyncJobLogMessages
{
    [LoggerMessage(Level = LogLevel.Information,
        Message = "Weekly bowler membership sync finished: {Total} changed in the Software, {Created} created, {Updated} updated, {Unchanged} already current, {Failed} failed.")]
    public static partial void LogBowlerMembershipWeeklySyncCompleted(this ILogger<BowlerMembershipWeeklySyncJob> logger, int total, int created, int updated, int unchanged, int failed);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Bowler membership ids match: {Count} in the Software and on the website.")]
    public static partial void LogBowlerMembershipIdsMatch(this ILogger<BowlerMembershipWeeklySyncJob> logger, int count);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Bowler membership ids differ: {SoftwareCount} in the Software, {WebsiteCount} on the website; {Extra} extra and {Missing} missing.")]
    public static partial void LogBowlerMembershipIdsDiffer(this ILogger<BowlerMembershipWeeklySyncJob> logger, int softwareCount, int websiteCount, int extra, int missing);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Deleted {Deleted} website bowler memberships the Software no longer has.")]
    public static partial void LogBowlerMembershipsDeleted(this ILogger<BowlerMembershipWeeklySyncJob> logger, int deleted);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Skipped deleting website bowler memberships: {Reason} ({Extra} candidates, {WebsiteCount} on the website).")]
    public static partial void LogBowlerMembershipDeleteGuardTripped(this ILogger<BowlerMembershipWeeklySyncJob> logger, string reason, int extra, int websiteCount);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Weekly sync could not save a chunk of {Count} bowler memberships; retrying each on its own.")]
    public static partial void LogBowlerMembershipWeeklySyncChunkFailed(this ILogger<BowlerMembershipWeeklySyncJob> logger, Exception exception, int count);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Weekly sync could not save legacy bowler membership {LegacyBowlerMembershipId}.")]
    public static partial void LogBowlerMembershipWeeklySyncRowSaveFailed(this ILogger<BowlerMembershipWeeklySyncJob> logger, Exception exception, int legacyBowlerMembershipId);
}
```

## Tests

Two files, so the test surface goes away with the source at sunset. Traits `[UnitTest]` / `[IntegrationTest]` and `[Component("Legacy")]`; strict mocks; AAA comments; `DisplayName` everywhere; Shouldly; factories from `Neba.TestFactory` (add a `BowlerMembershipFactory` if none exists, following `Create()` / `Bogus()`).

`tests/Neba.Api.Tests/Legacy/BowlerMemberships/BowlerMembershipTests.cs`:

1. **Validation** — one fact each for the three validators: `0` and negative fail, positive passes.
2. **Endpoint + auth** (`TestHost`, through `MapLegacyGroup()`): missing/wrong key → 401; invalid body → 400; valid → 202 and `Mock<IBackgroundJobClient>(Strict)` verifies the expected call. For new/update, the call is `Schedule<BowlerMembershipSyncJob>(job => job.UpsertAsync(id, ...), BowlerSettleDelay)`; for delete, `Enqueue<BowlerMembershipSyncJob>(job => job.DeleteAsync(id, ...))` with no delay. Follow `UpdateBowlerTests`' registration pattern, with the three new validators and all sibling validators.
3. **Mapping (unit)** — `Apply` and `ApplyLegacyUpdate`: create, unchanged, each changed field, negative cost, end before start, duplicate `LegacyId`.
4. **Legacy query (integration)** — against `LegacySqlServerFixture`, as `UpdateBowlerSyncJobTests` does (`legacyFixture.GetOrCreateDatabaseAsync(...)` with a `CreateSchemaAsync` that builds `Bowlers`, `Memberships`, `BowlerMemberships`). The architecture doc still says "Postgres temp table"; the code moved to a real SQL Server fixture, so follow the code. Cases: row returns with joined cost; unknown id returns null; type 8 returns the row (the job, not the query, skips it).
5. **Job behavior and idempotency (integration)**, real `AppDbContext`:
   - upsert twice → one row, `LegacyId` set, second run writes nothing.
   - upsert after the Software row changed type/dates → website row updated.
   - Software row missing → logged, nothing written.
   - type 8 → skipped, logged; unknown type → logged, nothing written.
   - bowler missing → bowler created from the Software, membership added, `neba:bowlers` evicted (strict `IFusionCache` mock); bowler already present → no eviction.
   - delete existing → removed, audit event written; delete missing → no-op, logged.
   - `FakeLogger<T>` assertions for each log message above.
   - **Order convergence** (checklist row 3): membership job first creates the bowler, then `NewBowlerSyncJob` for the same bowler is a no-op (one bowler, one membership). Reverse order: `NewBowlerSyncJob` first, then the membership job finds the bowler and adds the membership.
   - **Create race**: arrange both jobs to create the same bowler (second `SaveChangesAsync` hits the unique `LegacyId` index and throws `DbUpdateException`); re-running the loser succeeds with one bowler and one membership.

`tests/Neba.Api.Tests/Legacy/Sync/BowlerMembershipWeeklySyncTests.cs`:

6. **Window query** — rows before, at and after `since` (boundary included); type 8 excluded; ordered by `Id`.
7. **Pass 1** — created / updated / unchanged / failed counts; more than 50 rows saves all; one bad row fails alone (`FakeLogger`), others save; `DbUpdateException` falls back to per-row.
8. **Pass 2** —
   - id sets equal → no deletes, no upserts.
   - one extra and one missing (equal counts) → the extra is deleted and the missing one is upserted.
   - exactly 1 extra → deleted and reported; re-check spares a row the Software now has.
   - 2 or more extras → nothing deleted, alert sent.
   - empty Software set with website rows → nothing deleted, alert sent.
   - missing rows → upserted.
   - website rows with null `LegacyId` never compared or deleted.
   - type 8 rows in the Software are excluded from the id set.
9. **Registry** — `WeeklyLegacySyncTests` (existing file): `Tables` now enqueues both jobs; the membership one with a 15-minute delay and the same `since`.

## Software side (`nebamgmt-v3`)

### Adapter

No new adapter. `Common/NEBA.Common/Adapters/WebsiteSyncAdapter.cs` already meets every requirement the skill carries forward: one static `HttpClient` with a 5 s timeout, up to 3 attempts (2 s then 5 s waits), a `Task.Run` fire-and-forget `Send` that captures only strings, no exception back to the caller, and URL / API key gating from the existing `App.config` keys. (Correction after the Software implementer checked: `WebsiteSyncEnabled` is read only by the health-check label. The calls are gated by an empty URL or key, which makes every `Notify*` return early.) It sends POST only, which is why delete is a POST route.

Add three methods beside `NotifyNewBowler`:

- `NotifyNewBowlerMembership(int bowlerMembershipId)` → `/legacy/bowler-memberships/new`, body `{"bowlerMembershipId":N}`
- `NotifyBowlerMembershipUpdated(int bowlerMembershipId)` → `/legacy/bowler-memberships/update`
- `NotifyBowlerMembershipDeleted(int bowlerMembershipId)` → `/legacy/bowler-memberships/delete`

Abandoning an in-flight call when the process exits is an accepted consequence of this design. The weekly sync is the net under it.

### Hook points

Every hook goes **after** `Commit()` returns, inside the repository, with only the integer id captured. All repositories live in `NEBA.Data`, which can reference `NEBA.Common`, as `CheckInRepository` already does.

| Path | File / method | Hook |
|---|---|---|
| A add membership | `Data/NEBA.Data/Repositories/Membership/BowlerMembershipRepository.cs` `Add`, after `Commit()` (line 29), before `return entity.Id` | `NotifyNewBowlerMembership(entity.Id)` |
| B renew | `BowlerRepository.cs` `Update` 111-127, after `Commit()` (~126) | for each child entity set to `Added` at lines 120-124: `NotifyNewBowlerMembership(membership.Id)` |
| C new bowler | `BowlerRepository.cs` `Add` 45-59, after `Commit()` (~56) | for each `entity.Memberships`: `NotifyNewBowlerMembership(membership.Id)` |
| D check-in | `Data/NEBA.Data/Repositories/Tournaments/CheckInRepository.cs`, `ProcessCheckIn` (lines 178-184) collects each `checkInEntity.MembershipDue` into a list, the way `_newBowlers` is collected; `Add(IEnumerable)` (lines 28-36) sends after `Commit()` (line 33), next to `NotifyNewBowlers()` | one `NotifyNewBowlerMembership(id)` per collected entity; clear the list at the start of each `Add` |
| E change | `BowlerMembershipRepository.cs` `Change` 52-68, after `Commit()` (line 67) | `NotifyBowlerMembershipUpdated(bowlerMembershipId)` |
| F delete | `BowlerMembershipRepository.cs` `Delete(int id)` 43-50, after `Commit()` (line 49) | `NotifyBowlerMembershipDeleted(id)` |
| G delete check-in + membership | `CheckInRepository.cs` `Delete` 215-259, after `Commit()` (line 258) | only when `membershipId != 0`: `NotifyBowlerMembershipDeleted(membershipId)` |

Always fires vs conditional:
- A, E, F always fire when the commit succeeds.
- B and C fire once per membership on the entity, so zero if none.
- D fires once per check-in that carried a membership, which can be several per submit or none.
- G fires only when the user confirmed deleting the membership.

Path D includes the case where a reservation converts to a check-in with a membership, since that also goes through `ProcessCheckIn`.

### Result of the Software implementation (reported by the implementer, 2026-10-26)

The hooks are built. This is the implementer's report, taken as given; nothing here was run by anyone yet.

1. **Check-in ids.** `CheckInRepository` collects `checkInEntity.MembershipDue` (`_newMemberships`, line ~202); `NotifyNewMemberships()` (lines ~72-73) reads `membership.Id` after `Commit()`. Each call sends the `BowlerMembership.Id` primary key. The list holds entities, not ints, but each `Notify*` call receives only an `int`. The list is cleared at the start of both `Add` overloads and after sending.
   - **Not confirmed**: a membership for a brand-new bowler in the same check-in submit. The membership's `BowlerId` is the negative temporary id; EF should link it through the check-in graph and write the identity back. If the link failed, the save would fail and nothing would send. Needs one run.
2. **New-bowler hook placement.** Confirmed from the code: the hook is at `BowlerRepository.cs` 58-59, right after the commit. `AddBowlerBO.SystemGeneratedTasks` (line ~115) and `NotifyNewBowler` (line ~117) run after it. So the membership call goes first, which confirms the 60 s delay and create-bowler-if-missing in this plan are needed, not optional.
3. **Batch/import apps.** `Data/NEBA.Data.Batch` and `Data/NEBA.Data.Import` have no URL or key in any config, so every `Notify*` returns early and those apps send nothing. They still call the same repositories (`MembershipImport.cs:104,149`, `MemberImport.cs:40`) and would send if someone added the keys. The website skips type 8 anyway.
4. **Coverage.** One call per membership on every path in the hook table; none for reservations and none for the system-import removal tool. Two caveats:
   - **Renew** relies on `RenewBowlerMembership.Update` using `bowler.Memberships.Single()`. The implementer did not read `VerifyBowlerPresenter`.
   - **`UpdateBowlerBO.Update`** (line ~75) also calls `DataAccess.Update(bowler)`. If an edit-bowler BOM ever carries existing memberships, `BowlerRepository.Update` marks them `Added`, which would insert duplicate Software rows and send a `new` for each. Assumed the edit flow passes none; not confirmed. Website side: the upsert is by `LegacyId`, so a repeated id is harmless.
5. **No throw into the caller.** Every hook is after `Commit()` returns. The `NotifyBowlerMembership` wrapper in `WebsiteSyncAdapter` catches every exception, including a failed decrypt or missing config, and logs it with `Trace`. A throw from `Commit()` skips the hook, as intended.
6. **Per-environment config**: none new.

**Still to do before sign-off** (not run by anyone):
- One end-to-end run per path against a local website: add, renew, new bowler with membership, check-in with membership (existing bowler and brand-new bowler), change, both deletes.
- Confirm in the Hangfire dashboard that each path produces exactly one job per membership.
- Read the edit-bowler form and presenter to confirm `UpdateBowlerBO` passes no memberships, and `VerifyBowlerPresenter` to confirm renew builds exactly one.

### End-to-end check before sign-off

Run against a local website with a reachable Software database, URL and key set in the Software's `App.config`. For each row, do the action, then open the Hangfire dashboard and check the website's `bowler_memberships` table. Pass means exactly one job per membership and a matching row (or no row, for deletes).

| # | Action in the Software | Expected website result |
|---|---|---|
| 1 | Bowler profile: add a membership to an existing bowler | One `new` job, 60 s delay, one row with the Software id as `LegacyId`. |
| 2 | Renew through the VerifyBowler form | One `new` job (plus the existing `bowlers/update`). Confirm the form builds exactly one membership. |
| 3 | Add a brand-new bowler with a membership | One `new` job arrives **before** `bowlers/new`. After the delay the bowler and membership both exist, with no failed job. |
| 4 | Add a new bowler that matches a duplicate | One `new` job, via the renew path. |
| 5 | Check-in with a membership, existing bowler | One `new` job per check-in that carried a membership. |
| 6 | Check-in with a membership, **brand-new bowler in the same submit** | The save succeeds, `membership.Id` is a real positive id, and one `new` job exists. If the save fails or the id is negative, stop and fix before shipping. |
| 7 | Check-in with no membership; a reservation | No membership job. |
| 8 | Change a membership (type and dates) | One `update` job. Website row shows the new type and dates. |
| 9 | Delete a membership from the profile history | One `delete` job. Row is gone. |
| 10 | Delete a check-in and confirm "also delete the membership" | One `delete` job. Answer "no": none. |
| 11 | Edit an existing bowler's name or details | **No** membership job, and no new `BowlerMemberships` row in the Software. Read the edit form and presenter first to confirm `UpdateBowlerBO` passes no memberships. |
| 12 | Bowler with a membership of type 8 (or a stub system-import row) | If a call reaches the website, the job logs "System Import" and writes nothing. |
| 13 | Run the weekly step with the website one row short and one row over | The missing row is upserted and the extra row is deleted and reported. With two extras, nothing is deleted and the alert fires. |

#### Which rows are automated

Decided: automate everything an integration test can reach; keep a short manual list for the rest.

| Rows | Where | How |
|---|---|---|
| 1, 8, 9, 12, 13 | Website, `BowlerMembershipTests.cs` and `BowlerMembershipWeeklySyncTests.cs` | Real `AppDbContext` plus `LegacySqlServerFixture`, as listed under Tests. |
| 3 | Website | Order-convergence and create-race tests (Tests, item 5). |
| 2, 4, 5, 6, 7, 10 and the "none" half of 11 | **Software repo**, new DB-backed tests | See "Software integration tests" below. |
| Reading `VerifyBowlerPresenter` and the edit-bowler form/presenter | Manual | A code read; no test reaches a WinForms presenter. |
| One smoke run of rows 1, 3 and 5 against a local website, checking the Hangfire dashboard | Manual | Proves the HTTP call, key and URL wiring that the Software tests stub out. |

### Software integration tests (`nebamgmt-v3`)

New tests in `Tests/` (NUnit), against a real SQL Server built from `Data/NEBA.Data/NEBADataModel.edmx.sql`. The Software repo has no database-backed tests today, so this adds a small fixture. The repositories each build their own `new Entities()`, so the tests call the repositories directly and read results back through a second context.

- **Database.** Use `Testcontainers.MsSql` (it supports .NET Framework test projects through netstandard2.0), or LocalDB if Docker is not available on the implementer's machine. Create the schema once per test run from `NEBADataModel.edmx.sql`, seed the `Memberships` rows (ids 1, 2, 5, 6, 8) and clear `BowlerMemberships`, `CheckIns`, `Bowlers` between tests. Point `Entities` at it through the test project's `App.config` connection string.
- **Capturing the calls.** Prefer no production change: start an in-process `HttpListener` on localhost in the fixture, set the website URL to it and the API key in the test `App.config`, and record each request path and body. If the adapter's `Decrypt()` on the key makes that impractical, add the smallest seam instead (for example an internal static `Func<string, string, string, Task>` sender in `WebsiteSyncAdapter` that defaults to the real `Send`) and say so in the PR. The adapter sends from `Task.Run`, so the recorder must wait for the expected number of requests with a timeout, not sleep.
- **Cases** (each asserts the paths and ids received, and that the id equals the `BowlerMemberships.Id` read back from the database):
  1. `BowlerMembershipRepository.Add` → one `new`.
  2. `BowlerRepository.Update` with one new child membership (renew) → one `new`.
  3. `BowlerRepository.Add` with a bowler and one membership → one `new`.
  4. Duplicate-bowler path (`AddBowlerBO.Add` matching an existing bowler) → one `new`, via renew.
  5. `CheckInRepository.Add` for an existing bowler with a membership → one `new`; with two check-ins (main plus an additional one), both carrying memberships → two; none carrying one → zero.
  6. **`CheckInRepository.Add` for a brand-new bowler (negative temporary id) with a membership in the same submit** → the save succeeds, the `BowlerMemberships` row has a positive `Id` and its `BowlerId` equals the new bowler's real id, and one `new` is sent with that id. This is the case nobody has run.
  7. `CheckInRepository.Add(squadId, bowler, lane)` (reservation) and a check-in with no membership → zero membership calls.
  8. `BowlerMembershipRepository.Change` → one `updated`.
  9. `BowlerMembershipRepository.Delete` → one `deleted` with the right id.
  10. `CheckInRepository.Delete` with the confirm callback true → one `deleted`; false → none; a check-in with no membership → none.
  11. `UpdateBowlerBO.Update` with a bowler shaped like the edit form's (empty `Memberships`) → zero membership calls and no new `BowlerMemberships` row.
  12. A hook failure does not reach the caller: with the listener returning 500 or closed, each repository call above still returns normally.
- Rows that start from a form (2 as the VerifyBowler flow, the delete confirmation dialog) are tested at the repository or BO below the presenter. The presenter reads in the manual list cover the rest.

### Prompt for the `nebamgmt-v3` implementation

> You are changing the `nebamgmt-v3` repo (VB.NET/C# WinForms, EF6). Goal: tell the NEBA website about every create, edit and delete of a `BowlerMemberships` row, so the website mirrors it. The website exposes three POST routes under the base URL in `WebsiteSyncAdapter`, each with an `X-Api-Key` header and a JSON body `{"bowlerMembershipId": N}` where N is `BowlerMemberships.Id`:
> - `/legacy/bowler-memberships/new`
> - `/legacy/bowler-memberships/update`
> - `/legacy/bowler-memberships/delete`
>
> **Adapter.** Do not write a new one. Add `NotifyNewBowlerMembership(int)`, `NotifyBowlerMembershipUpdated(int)` and `NotifyBowlerMembershipDeleted(int)` to `Common/NEBA.Common/Adapters/WebsiteSyncAdapter.cs`, copying `NotifyNewBowler`. It already uses one static `HttpClient` (5 s timeout), up to 3 attempts, fire-and-forget `Task.Run` with only string captures, and never throws to the caller. Keep all of that. Each call site may capture only the integer id, never a presenter, form, entity or `Control`.
>
> **Call sites.** Each goes after the `Commit()` call returns, never before, and must never throw into the caller:
> - `Data/NEBA.Data/Repositories/Membership/BowlerMembershipRepository.cs`: `Add` (after `Commit()`, line ~29) → new with `entity.Id`; `Change` (after `Commit()`, line ~67) → updated with `bowlerMembershipId`; `Delete(int id)` (after `Commit()`, line ~49) → deleted with `id`.
> - `Data/NEBA.Data/Repositories/Membership/BowlerRepository.cs`: `Add` (lines ~45-59, after `Commit()`) → new for each `entity.Memberships` item; `Update` (lines ~111-127, after `Commit()`) → new for each child membership the method marks `Added` (lines ~120-124).
> - `Data/NEBA.Data/Repositories/Tournaments/CheckInRepository.cs`: in `ProcessCheckIn` (lines ~178-184), collect each `checkInEntity.MembershipDue` entity into a list, the way `_newBowlers` is collected. In `Add(IEnumerable)` (lines ~28-36), after `Commit()` and next to `NotifyNewBowlers()`, send new for each collected id, then clear the list (also clear at the start of each `Add`). In `Delete` (lines ~215-259), after `Commit()` (line ~258), when `membershipId != 0`, send deleted with `membershipId`.
>
> **Do not hook** `Data/NEBA.Data.Batch/Jobs/Repository.cs` (system-import removal), `Scripts/Merge Bowler.sql`, the import tools, or the membership-definition delete classes. Leave the existing `NotifyNewBowler` / `NotifyBowlerUpdated` calls alone.
>
> **Resolve or flag, do not assume:**
> 1. Is `BowlerMembership.Id` set on `checkInEntity.MembershipDue` after `Commit()` in the check-in add, including for a brand-new bowler added in the same submit?
> 2. In `BowlerRepository.Add`/`AddBowler.Add`, system-generated task commits run after the bowler commit inside the same `try`. Put the hook right after the commit that inserts the membership so a later failure cannot skip it. Confirm that is possible without restructuring.
> 3. Do the developer batch/import apps have a website URL and API key configured? If so, say so; they write `MembershipId = 8` rows the website ignores. (Gating is the URL and key, not `WebsiteSyncEnabled`.)
>
> **Tests.** Add DB-backed integration tests in `Tests/` (NUnit) that call the repositories against a real SQL Server built from `Data/NEBA.Data/NEBADataModel.edmx.sql` (Testcontainers.MsSql, or LocalDB). Capture the outgoing calls with an in-process `HttpListener` set as the website URL (add the smallest seam to `WebsiteSyncAdapter` only if the encrypted key makes that impractical, and say so). Cases, each asserting paths and ids against the ids read back from the database:
> 1. `BowlerMembershipRepository.Add` → one new.
> 2. `BowlerRepository.Update` with one new child membership → one new.
> 3. `BowlerRepository.Add` with one membership → one new.
> 4. Duplicate-bowler path in `AddBowlerBO.Add` → one new.
> 5. `CheckInRepository.Add`, existing bowler: one membership → one new; two check-ins both with memberships → two; none → zero.
> 6. `CheckInRepository.Add`, brand-new bowler (negative temporary id) with a membership: the save succeeds, the row's `Id` is positive, its `BowlerId` is the new bowler's real id, and one new is sent with that id. This one has never been run.
> 7. Reservation add, and a check-in without a membership → zero.
> 8. `BowlerMembershipRepository.Change` → one updated.
> 9. `BowlerMembershipRepository.Delete` → one deleted.
> 10. `CheckInRepository.Delete`: confirm true → one deleted; false → none; no membership → none.
> 11. `UpdateBowlerBO.Update` with a bowler shaped like the edit form's (empty `Memberships`) → zero calls and no new `BowlerMemberships` row.
> 12. With the listener returning 500 or closed, each call above still returns normally.
>
> Wait for the expected number of requests with a timeout; the adapter sends from `Task.Run`, so do not sleep. Then, by hand, read `VerifyBowlerPresenter` and the edit-bowler presenter to confirm renew builds exactly one membership and edit passes none, and do one smoke run (add membership, new bowler with membership, check-in with membership) against a local website, checking the Hangfire dashboard shows one job per membership.

## Summary of what's still undecided

1. ~~**Delete payload.**~~ **Decided**: membership id only; the row is gone from the Software by the time anyone looks.
2. ~~**One route for add and check-in.**~~ **Decided**: one `/new` route; each hook sends the id after commit.
3. ~~**Missing bowler / ordering.**~~ **Decided**: schedule add and update 60 s out; create the bowler from the Software if still missing. `NewBowlerSyncJob` is a safe no-op when the bowler exists.
4. ~~**Count gate vs always diffing ids.**~~ **Decided**: no count gate; compare the full id sets every run. It is one small `int` query, once a week, and it catches offsetting extras and gaps.
5. ~~**Update route.**~~ **Decided**: include `/update` and the `internal set` change on `BowlerMembership`.
6. ~~**Merge script and moved memberships.**~~ **Decided**: out of scope. You do not need merge scripts covered, so nothing detects a membership moved by `Scripts/Merge Bowler.sql`.
7. ~~**Cost source.**~~ **Decided**: `Memberships.Cost` in the Software.
8. ~~**Batch tool and type 8.**~~ **Decided**: no hook; type 8 is excluded from every query and from the id set.
9. ~~**Weekly job order.**~~ **Decided**: the membership step is scheduled 15 minutes after the bowler step.
10. ~~**Delete guard.**~~ **Decided**: more than 1 extra warns and deletes nothing, because the Software rarely deletes memberships. The 60 s delay is still my estimate from the adapter's worst case (about 20 s), not a measurement.
11. ~~**Column names.**~~ **Decided**: the edmx is good for column names. It lists both tables, including `Memberships.Cost`.
12. **Software hook details.** Mostly **decided** from the implementer's report (see "Result of the Software implementation"): hooks placed after commit, no throw, batch/import send nothing. **Still open**: nobody has run the hooks end to end, including a brand-new bowler's membership in a check-in, and the edit-bowler and renew form flows were not read.
13. ~~**Aggregate delete method.**~~ **Decided**: not needed yet, because the website does not manage memberships. Add it with the check-in and membership UI. Until then the job removes through `DbSet`.
