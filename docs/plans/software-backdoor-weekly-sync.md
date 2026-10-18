# Software Backdoor — Weekly Sync

A recurring Hangfire job that runs every Monday at 8:00 AM US Eastern. It asks the Software's database (`neba-fwk`) which rows changed in the last week and re-syncs them into the website's database. It catches anything a per-action `/legacy` call missed: Software clients fail to connect, and the lost call is never retried (the Software has no retry queue).

This differs from the other `backdoor-feature` plans in one way: **the website starts the sync, not the Software.** The Software changes nothing. There is no new `/legacy` route, no request DTO, no validator, and no Software call site. The Software-side section says so and lists what that choice costs.

It follows `docs/api/software-backdoor-plan.md` and builds on `docs/plans/software-backdoor-scaffolding.md`. `Legacy/Bowlers/UpdateBowler.cs` is the worked example for the bowler step.

## Decision Recap

- **Trigger**: a Hangfire recurring job, cron `0 8 * * 1`, time zone US Eastern (decided). The existing `IBackgroundJobScheduler.AddOrUpdateRecurring` takes no time zone (UTC only) and requires an `IBackgroundJob` record plus a handler. Backdoor code skips that ceremony on purpose (standing architecture doc), so this job registers through Hangfire's `IRecurringJobManager` directly, with `RecurringJobOptions.TimeZone`.
- **Fan-out**: one parent job, `WeeklyLegacySyncJob`, enqueues one child job per table (decided). A child that fails and retries does not rerun the others. All children get the same `since` value, computed once in the parent, so a retry hours later reads the same window.
- **Scope today**: the `Bowlers` table only (decided). Squad scores stay on the per-squad endpoint (`POST /legacy/squads/scores/sync`); re-trigger a failed squad by hand.
- **Missing website row**: create it (decided), the same as `UpdateBowlerSyncJob` does. The weekly job also heals a lost New Bowler call.
- **Extension pattern**: a new table is one new file (`{Table}WeeklySyncJob`) plus one line in `WeeklyLegacySync.Tables`. Nothing else changes. See "The pattern".
- **Window**: `Audit_UpdatedTimestamp >= since`, where `since` = run time minus **8 days** (decided). The extra day over 7 covers clock skew between client PCs, a late run, and the few hours between UTC and Eastern. **No time zone conversion for the lookup** (decided): the Software's data is all Eastern and the extra day absorbs the offset. The Eastern zone is used only for the cron. Re-syncing a row twice is harmless because every step is idempotent.
- **Idempotency**: a re-run converges. Unchanged rows are detected and skipped without a write. Changed rows are overwritten from the Software, which stays the system of record until sunset.
- **No endpoint** (decided): no manual-trigger route (decided). The Hangfire dashboard's "Trigger now" on the recurring job does the same thing.

## Research: `nebamgmt-v3`

Read from the model and the code. Not checked against the live database.

### `Audit_UpdatedTimestamp` facts

- `Bowlers.Audit_UpdatedTimestamp`: `datetime NOT NULL` (edmx SSDL, `Data/NEBA.Data/NEBADataModel.edmx:251`). Never null, so no null handling in the `WHERE` clause.
- **Set by C# code on the client PC, not the database.** `BaseRepository.PopulateAddAudit` / `PopulateUpdateAudit` (`Data/NEBA.Data/Repositories/BaseRepository.cs:61-82`) use `SystemClock.Now`, which is `DateTime.Now` (`Common/NEBA.Common/TimeProvider.cs:7`). There is no `SaveChanges` override, interceptor, default constraint, or trigger in the model. So the value is the client PC's **local time** (Eastern, confirmed by you) and client clocks can differ. This is why the window gets a day of margin.
- Other tables with the same column (all `datetime NOT NULL`): `Memberships`, `BowlerMemberships`, `Tasks`, `Credits`, `Debits`, `HallOfFame`, `BowlingCenters`, `Tournaments`, `Squads`, `Stats` (base), `CheckIns`, `QualifyingScores`, `Cups`, `CupResults`, `Teams`, `SquadTeams`, `CupTournaments`, `Seasons`. Subtype tables (`Stats_QualifyingStats`, `Credits_BowlerCredit`, …) have no audit columns of their own; the audit lives on the base table row. `TeamMember` and `CreditDebits` have none.

### Gaps the timestamp does not cover

| Write path | Updates `Audit_UpdatedTimestamp`? | Matters to the website today? |
|---|---|---|
| Add / update bowler (`BowlerRepository.cs:49,115`), check-in add (`CheckInRepository.cs:197`) | Yes | Yes |
| Bowler merge in `AddBowlerBO` (`BowlerMergeMapper`, goes through `BowlerRepository.Update`) | Yes | Yes |
| `Deceased()` (`BowlerRepository.cs:178-207`) | **No** | No. `Bowler` has no deceased field. |
| `SetAsChampion()` (`BowlerRepository.cs:209-221`) | **No** | No. `Bowler` has no champion field. |
| `Scripts/Merge Bowler.sql` (hand-run; re-points children, then `delete from Bowlers`) | **No** | Yes, in the future: the website keeps the old bowler row. Nothing in this plan fixes it. |
| Hard deletes of `BowlerMemberships`, `QualifyingScores`, `Stats` | n/a (row is gone) | Yes, once those tables sync |

Consequences:

1. For `Bowlers` today the timestamp misses nothing the website stores. It would miss `Deceased` and `Champion` if the website ever models them. Then the Software must touch the audit in those two methods (a Software change you have ruled out for now) or the job needs a different change signal for that column.
2. **An "updated since" query can never see a hard delete.** It cannot reach `BowlerMemberships`, `QualifyingScores` or `Stats` deletes. When `BowlerMemberships` joins, its step needs a second pass that compares the id set for the affected bowlers (the Software's current ids vs the website's), not just a timestamp filter. Decided (Undecided #6): do this for `BowlerMemberships` when it joins, not in this iteration. Bowlers need no reconcile because the Software never deletes them in application code.
3. `QualifyingScores.Add` is an upsert that stamps only rows whose score changed, so the timestamp works for squad scores if you add them later. Row removals (`RemoveRange`) still need the id-set pass.

### Existing push sync (context)

`Common/NEBA.Common/Adapters/WebsiteSyncAdapter.cs` already posts to `/legacy` (5-second timeout, up to 3 tries, `X-Api-Key` header). Call sites: `AddBowlerBO:117`, `UpdateBowlerBO:77`, `RenewBowlerMembershipBO:92`, `CheckInRepository:56`, `HallOfFameRepository:55`. This plan does not touch it.

### Legacy Schema Reference

| Table | Columns the weekly step reads | Notes |
|---|---|---|
| `Bowlers` | `Id`, `FirstName`, `MiddleInitial`, `LastName`, `Suffix`, `Gender`, `DateOfBirth`, `Audit_UpdatedTimestamp` | `Gender` is an `int` enum. `MiddleInitial`/`Suffix` are `NOT NULL` strings, blank when empty. Same columns `UpdateBowlerSyncJob` already queries. Column names for those seven are confirmed by the existing code running in production; `Audit_UpdatedTimestamp` is confirmed only from the edmx. |

## The pattern

Three parts. Only the third is per-table.

```
Recurring job (Mon 08:00 ET)
  └─ WeeklyLegacySyncJob.RunAsync()           // parent: computes `since`, fans out
       ├─ BowlerWeeklySyncJob.SyncAsync(since)        // one Hangfire job per table
       ├─ BowlerMembershipWeeklySyncJob.SyncAsync(since)   // future: one file + one registry line
       └─ …
```

### `Legacy/Sync/WeeklyLegacySync.cs` — registry, parent job, scheduling

One file, deleted at sunset with the rest of `Legacy/`.

```csharp
using Hangfire;

namespace Neba.Api.Legacy.Sync;

internal static class WeeklyLegacySync
{
    public const string RecurringJobId = "legacy-weekly-sync";

    // Monday 08:00 in RecurringJobOptions.TimeZone (Eastern).
    public const string Cron = "0 8 * * 1";

    // Days back from the run time. 7 days of changes plus a day of margin for client-clock skew
    // (Audit_UpdatedTimestamp is the client PC's DateTime.Now), a late run, and the few hours
    // between UTC and Eastern. Re-syncing a row
    // twice is harmless, so overlap costs nothing.
    public const int LookbackDays = 8;

    // The one place a new table is registered: add a line that enqueues its job.
    // Each entry enqueues independently so one table's failure and retry does not rerun the others.
    public static readonly IReadOnlyList<Action<IBackgroundJobClient, DateTime>> Tables =
    [
        (jobs, since) => jobs.Enqueue<BowlerWeeklySyncJob>(job => job.SyncAsync(since, CancellationToken.None)),
        // (jobs, since) => jobs.Enqueue<BowlerMembershipWeeklySyncJob>(job => job.SyncAsync(since, CancellationToken.None)),
    ];

    public static TimeZoneInfo EasternTimeZone { get; } = FindEasternTimeZone();

    // Used only for the cron schedule. Same lookup NewTournament uses: "Eastern Standard Time" on Windows, "America/New_York" on Linux.
    // Reuse that helper (decided): make `NewTournament`'s `FindEasternTimeZone` internal and call it from here.
    private static TimeZoneInfo FindEasternTimeZone() { /* … */ }
}

internal sealed class WeeklyLegacySyncJob(IBackgroundJobClient jobs, TimeProvider clock, ILogger<WeeklyLegacySyncJob> logger)
{
    public Task RunAsync()
    {
        using var _ = LegacyActor.EnterActorScope();

        // Software timestamps are Eastern wall-clock time. No conversion: the extra lookback day
        // covers the 4-5 hour offset from UTC.
        var since = clock.GetUtcNow().UtcDateTime.AddDays(-WeeklyLegacySync.LookbackDays);

        logger.LogWeeklyLegacySyncStarted(since, WeeklyLegacySync.Tables.Count);

        foreach (var enqueue in WeeklyLegacySync.Tables)
        {
            enqueue(jobs, since);
        }

        return Task.CompletedTask;
    }
}

internal static partial class WeeklyLegacySyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Weekly legacy sync started: syncing {TableCount} tables changed since {Since:u}.")]
    public static partial void LogWeeklyLegacySyncStarted(this ILogger<WeeklyLegacySyncJob> logger, DateTime since, int tableCount);
}
```

Scheduling, wired from `LegacyConfiguration.cs` and called from `InfrastructureConfiguration.UseInfrastructure()` next to `UseSeasonRolloverJob()`:

```csharp
// LegacyConfiguration.cs
extension(WebApplication app)
{
    public void UseLegacyWeeklySync()
    {
        // Locally the Legacy connection string is usually unset (no access to the Software's
        // database). A job that can only fail every Monday is noise, so register it only when
        // the same condition AddLegacy() uses for the health check holds.
        var legacyConnectionString = app.Configuration.GetSection("Legacy")["ConnectionString"];
        if (string.IsNullOrWhiteSpace(legacyConnectionString))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        recurringJobs.AddOrUpdate<WeeklyLegacySyncJob>(
            WeeklyLegacySync.RecurringJobId,
            job => job.RunAsync(),
            WeeklyLegacySync.Cron,
            new RecurringJobOptions { TimeZone = WeeklyLegacySync.EasternTimeZone });
    }
}
```

Notes for the implementer:

- Confirm `TimeProvider` is registered in DI (`builder.Services.AddSingleton(TimeProvider.System)`). If the codebase has its own clock abstraction, use that instead.
- `Hangfire` serializes the `DateTime since` argument as JSON. so every retry reads the same window. The child passes it straight to Dapper as `@Since`.
- The global `DiscordJobFailureFilter` already alerts when a job fails for good; the weekly children get that for free.

### `Legacy/Sync/BowlerWeeklySyncJob.cs` — the bowler step

Reuses the field mapping from `UpdateBowlerSyncJob`. **Do not copy it a third time.** `UpdateBowler.cs:203` says the third caller of `MapSuffix` is the trigger to extract it. Do that extraction as part of this change: move `MapSuffix` into `Legacy/Bowlers/LegacySuffixParsing.cs` next to `LegacyNameParsing.cs`, and extract the row → `Name`/`Gender`/`DateOfBirth` mapping into one shared method that `UpdateBowlerSyncJob` and the weekly job both call. That keeps one definition of "how a legacy bowler row becomes website fields." It also means a bug fix applies to both. Existing `UpdateBowler` tests must still pass unchanged.

```csharp
internal sealed class BowlerWeeklySyncJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    IFusionCache cache,
    ILogger<BowlerWeeklySyncJob> logger)
{
    private const int SaveBatchSize = 50;

    public async Task SyncAsync(DateTime since, CancellationToken ct)
    {
        using var _ = LegacyActor.EnterActorScope();

        // See NewBowlerSyncJob.SyncAsync for the rationale on suppressing DAP005 here.
#pragma warning disable DAP005
        var rows = (await legacyConnection.QueryAsync<LegacyBowlerRow>(
            """
            SELECT
                Id,
                FirstName,
                MiddleInitial,
                LastName,
                Suffix,
                Gender,
                DateOfBirth
            FROM
                Bowlers
            WHERE
                Audit_UpdatedTimestamp >= @Since
            ORDER BY
                Id
            """,
            new { Since = since })).ToList();
#pragma warning restore DAP005

        var created = 0;
        var updated = 0;
        var unchanged = 0;
        var failed = 0;

        foreach (var chunk in rows.Chunk(SaveBatchSize))
        {
            var legacyIds = chunk.Select(row => (int?)row.Id).ToList();
            var existingByLegacyId = await db.Set<Bowler>()
                .Where(bowler => legacyIds.Contains(bowler.LegacyId))
                .ToDictionaryAsync(bowler => bowler.LegacyId!.Value, ct);

            foreach (var row in chunk)
            {
                // Map via the shared row mapping (extracted from UpdateBowlerSyncJob), then:
                //  - no website row            -> Bowler.CreateFromLegacy(...), db.Add          (created++)
                //  - website row, fields same  -> skip, no write                                (unchanged++)
                //  - website row, fields differ -> existing.ApplyLegacyUpdate(...)
                //                                  db.Entry(existing).State = Modified          (updated++)
                //    (same owned-Name audit workaround as UpdateBowlerSyncJob)
                //  - mapping returns errors    -> LogLegacyBowlerWeeklySyncFailed, failed++, continue
            }

            // One save per 50 rows, not one per run: Audit.EntityFramework writes every changed
            // entry into one audit property, and Azure Table Storage caps a property at 64 KB
            // (see SyncSquadScores.cs). Saving per chunk keeps each audit event small.
            await db.SaveChangesAsync(ct);
        }

        logger.LogBowlerWeeklySyncCompleted(rows.Count, created, updated, unchanged, failed);

        if (created + updated > 0)
        {
            await cache.RemoveByTagAsync("neba", token: ct);
        }
    }
}
```

Decisions inside the step:

- **Skip unchanged rows** rather than write them. Most of the 7-day window will already match (the per-action call usually landed). Skipping avoids hundreds of no-op audit events. Compare the mapped first/middle/last name, suffix, nickname, gender and date of birth against the tracked entity. If `Name` already has value equality, use it; otherwise compare its components.
- **One failed row does not stop the run.** A mapping error (for example a name that fails `Name.Create`) is logged and counted, and the loop continues. Retry would not fix it. If `failed > 0` at the end, send one Discord alert through `IManualInterventionNotifier` listing the legacy ids, same shape as the squad-score alert in `SyncSquadScores.cs`. A transient database error still throws, so Hangfire retries the whole step. That is safe because the step is idempotent.
- **No transaction around the whole run.** Each chunk commits on its own; a retry skips the rows already written because they now compare as unchanged. (`SyncSquadScores` needs a transaction because it deletes and re-inserts. This step does not.) Because the app uses `NpgsqlRetryingExecutionStrategy`, any explicit transaction would have to go through `CreateExecutionStrategy()` anyway.
- **`LegacyId` is `init`-only on `Bowler`**; this step only sets it on create, through `CreateFromLegacy`. No setter change is needed. `Name`, `Gender`, `DateOfBirth` already have `internal set` (from the `UpdateBowler` work).
- **No deletes.** The Software never deletes a bowler in application code, so there is nothing to mirror. The hand-run merge script is the exception; see the gaps table.

Log messages (all ids and counts, no `[PersonalData]` needed; the row-failure message carries only the legacy id and error descriptions, as in `UpdateBowlerSyncJobLogMessages`):

```csharp
internal static partial class BowlerWeeklySyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Weekly bowler sync finished: {Total} changed in the Software, {Created} created, {Updated} updated, {Unchanged} already current, {Failed} failed.")]
    public static partial void LogBowlerWeeklySyncCompleted(this ILogger<BowlerWeeklySyncJob> logger, int total, int created, int updated, int unchanged, int failed);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Weekly sync could not apply legacy bowler {LegacyBowlerId}: {Errors}.")]
    public static partial void LogLegacyBowlerWeeklySyncFailed(this ILogger<BowlerWeeklySyncJob> logger, int legacyBowlerId, string errors);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not map legacy suffix '{LegacySuffix}' (bowler {LegacyBowlerId}) to a known NameSuffix; leaving suffix blank.")]
    public static partial void LogLegacySuffixUnmappedForWeeklySync(this ILogger<BowlerWeeklySyncJob> logger, int legacyBowlerId, string legacySuffix);
}
```

(If `MapSuffix` moves to the shared file, its log method moves with it and takes a plain `ILogger`; drop the third message above.)

### Cache eviction

The job writes through `AppDbContext`, so it bypasses the command-handler eviction. After the last chunk, and only if something was created or updated, evict the root tag `neba` (decided). Every descriptor in `CacheDescriptors.cs` carries that tag, so one call clears the whole cache. This avoids working out which cached queries show bowler fields. It costs one cold-cache burst after a run that changed data, which is acceptable for a weekly job that exists only until the Software is sunset.

```csharp
if (created + updated > 0)
{
    await cache.RemoveByTagAsync("neba", token: ct);
}
```

If the weekly run changes nothing, nothing is evicted. `UpdateBowlerSyncJob` and `NewBowlerSyncJob` keep their narrower eviction; this plan does not change them.

### DI registration

- **No new `IValidator<T>`.** The weekly job has no request DTO, so none of the three validator registrations in the skill checklist apply: not `LegacyConfiguration.AddLegacy()`, not this feature's tests, and **not a backfill into the existing `/legacy` endpoint test files**. No existing test file changes for this reason.
- The job classes (`WeeklyLegacySyncJob`, `BowlerWeeklySyncJob`) resolve the same way `NewBowlerSyncJob` and `UpdateBowlerSyncJob` do (neither is registered by hand in `AddLegacy()`). Confirm that when you build it; if a sibling job is registered somewhere I missed, register these the same way.
- `UseLegacyWeeklySync()` needs adding to `UseInfrastructure()` (`InfrastructureConfiguration.cs:~89`).

### Config

No new setting. The cron, lookback and job id are constants. If you later want to change the schedule without a deploy, move them to `LegacySettings`; not worth it now.

## Tests

One file, `tests/Neba.Api.Tests/Legacy/Sync/WeeklyLegacySyncTests.cs`, several classes, so the whole test surface goes away with the source at sunset. Traits: `[UnitTest]`/`[IntegrationTest]` and `[Component("Legacy")]`; strict mocks; AAA comments; `DisplayName` on every test; Shouldly. Use factories from `Neba.TestFactory`.

Layers from the architecture doc, mapped to this job:

1. **Request validation** — not applicable (no request).
2. **Endpoint + auth** — not applicable (no route, so `MapLegacyGroup()` is untouched).
3. **Parent job (unit)** — `WeeklyLegacySyncJobTests`, with a fake `TimeProvider` and `Mock<IBackgroundJobClient>(MockBehavior.Strict)`:
   - enqueues one child job per `WeeklyLegacySync.Tables` entry, each with the same `since`.
   - `since` is the fake clock's UTC time minus `LookbackDays` (8 days).
   - Pin the schedule: `WeeklyLegacySync.Cron == "0 8 * * 1"` and the zone resolves to a zone with an Eastern offset. (Cheap guard against someone changing it by accident.)
4. **Bowler step, mapping (unit/in-memory)** — `BowlerWeeklySyncJobMappingTests`: the extracted shared mapping gets its own cases (suffix `Jr`/`Jr.`/`JR.`/unknown, gender 0/1/other, quoted nickname, null date of birth). Move or reuse the cases from `UpdateBowler` tests rather than duplicating them; both jobs call the same method now.
5. **Bowler step, legacy query (integration)** — `BowlerWeeklySyncQueryTests` against the Postgres Testcontainers fixture with a per-connection `CREATE TEMP TABLE Bowlers (… Audit_UpdatedTimestamp timestamp NOT NULL)`. Seed rows before, at, and after `since`. Assert: only rows `>= since` return (boundary included), ordered by `Id`. The SQL is plain enough to run on Postgres; if `ORDER BY`/`@Since` need any dialect change, use the same approach the other legacy query tests use.
6. **Idempotency (integration)** — `BowlerWeeklySyncIdempotencyTests`, real `AppDbContext`: run `SyncAsync` twice with the same rows. First run creates (with `LegacyId`) and updates; second run changes nothing, writes no audit event, and evicts nothing. Plus:
   - website row missing → created with the right `LegacyId`.
   - website row differs → updated; website row identical → `unchanged`, entity state stays `Unchanged`.
   - one bad row (name fails `Name.Create`) → counted as failed, logged via `FakeLogger<T>`, the other rows still saved.
   - more than 50 changed rows → all saved (exercises the chunk loop).
   - cache: `Mock<IFusionCache>` strict, expects `RemoveByTagAsync("neba")` once after the final chunk when `created + updated > 0`, and no calls when zero.

## Software side (`nebamgmt-v3`)

**No change.** The website starts this sync and only reads `neba-fwk`. There is no Software call site, no adapter work, and no timeout or threading question to answer.

What that choice costs, so it is a decision and not an oversight:

- The Software keeps firing its per-action `/legacy` calls as today. The weekly job is a net under them, not a replacement.
- `Deceased()`, `SetAsChampion()` and the hand-run merge script never update `Audit_UpdatedTimestamp` and never call the adapter. The weekly job cannot see them. Harmless today (the website stores neither flag); needs a Software change or a different signal the day it does.
- Hard deletes are invisible to this approach (see the gaps table).

### Prompt for the `nebamgmt-v3` implementation

Not needed for this plan. If you later decide the Software should stamp the audit in `Deceased()` and `SetAsChampion()` (`Data/NEBA.Data/Repositories/Membership/BowlerRepository.cs:178-221`), that is a small separate change: call `PopulateUpdateAudit` on the entity in both methods before commit.

## Summary of what's still undecided

1. ~~**Lookback window.**~~ **Decided**: 8 days, no time zone conversion for the lookup (the Software's data is all Eastern; the extra day absorbs the offset).
2. ~~**Manual trigger route.**~~ **Decided**: none. Use "Trigger now" on the Hangfire recurring job.
3. ~~**Time zone.**~~ **Decided**: US Eastern, via `RecurringJobOptions.TimeZone`.
4. ~~**Cache tags to evict.**~~ **Decided**: evict the root `neba` tag after any run that changes data. The job is temporary, so a full eviction is fine.
5. ~~**Eastern time zone helper (cron only).**~~ **Decided**: reuse the helper. Make `NewTournament`'s `FindEasternTimeZone` internal and share it; do not write a second copy.
6. ~~**Deletes and reconcile pass.**~~ **Decided**: not needed for `Bowlers` (the Software never hard-deletes a bowler in application code). When `BowlerMemberships` joins, its step compares the full set of membership ids, not only those of bowlers changed in the window: a membership can be deleted without touching the bowler's `Audit_UpdatedTimestamp`. It deletes website rows whose Software id no longer exists. Out of scope for this iteration. Still open then: how the website row stores the Software's membership id, and a guard against deleting everything if the Software query returns no rows (connection fault).
7. ~~**Squad scores in this job.**~~ **Decided**: bowlers only for now.
8. ~~**Missing website row.**~~ **Decided**: create it.
9. ~~**Run shape.**~~ **Decided**: one parent job, one child job per table.
10. ~~**Client PC time zone.**~~ **Decided**: everything in the Software's database is Eastern, so the lookup needs no conversion.
11. ~~**Production triggers or defaults.**~~ **Decided**: you confirmed the real database has no triggers.
12. ~~**Column name `Audit_UpdatedTimestamp` in the live database.**~~ **Decided**: you confirmed the name.
13. ~~**Recurring-job registration safety.**~~ **Decided**: no issue. The `DocumentsConfiguration.cs` comment is about the startup *enqueue* (a pre-warm run per pod), not the recurring entry. `AddOrUpdate` with a fixed id is safe when every pod boots, and this job enqueues nothing at startup.
14. ~~**Squad-score failure.**~~ **Decided**: out of scope. It prompted this work; this plan does not diagnose it.
