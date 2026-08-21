# Software Backdoor — Complete Season

Mirrors "season completed" from `nebamgmt-v3` into the website's own `Season` aggregate (`Neba.Api.Features.Seasons.Domain.Season`), then computes and assigns every season-end award — Bowler of the Year (all six categories, including Rookie) plus High Average and High Block — from the `BowlerSeasonStats` rows `GenerateSeasonStatsJob` has already been populating per tournament all season.

This plan covers the `/legacy` endpoint and its background jobs. It follows `docs/api/software-backdoor-plan.md` (standing architecture) and builds on `docs/plans/software-backdoor-scaffolding.md`. `Legacy/Tournaments/Complete/*` and `Legacy/Tournaments/Stats/*` (`docs/plans/software-backdoor-generate-season-stats.md`) are the closest worked examples and this plan matches their shape and folder-per-concern organization throughout.

## Decision Recap

- **One endpoint, `POST /legacy/seasons/complete`, taking only the Software's own `Season.Id`** (confirmed directly — not a tournament id). Same trigger-only shape as every other `/legacy` action.
- **`Season` has no `LegacyId` column** (unlike `Tournament`/`Bowler`/`BowlingCenter`) — the website creates its own seasons via `CreateNextSeason`, it doesn't sync them from the Software. Per direct instruction, the job resolves the target website `Season` by **date-range match**: read the legacy season's `Start`/`End` via Dapper, then find the website `Season` whose `StartDate`/`EndDate` match. No new column added.
- **Two-stage timing, matching the requester's stated rationale.** `CompleteSeasonSyncJob` runs **immediately** on enqueue and does only one thing: resolve the season and mark it `Complete`. The season-end award jobs are **scheduled one hour later** (`IBackgroundJobClient.Schedule`), to comfortably clear `GenerateSeasonStatsJob`'s own ten-minute-after-tournament-completion delay (see `docs/plans/software-backdoor-generate-season-stats.md`) — by the one-hour mark, every tournament in the season should have its `BowlerSeasonStats` rows fully (re)computed. This mirrors `CompleteTournamentSyncJob`'s `SyncTournamentResultsJob`-then-`GenerateSeasonStatsJob` scheduling gap, one level up.
- **Eight separate, independently-retryable background jobs for the award computation, per direct instruction** — not one big "assign all awards" job. Six for Bowler of the Year (Open, Woman, Senior, Super Senior, Rookie, Youth — one job per `BowlerOfTheYearCategory` value), one for High Average, one for High Block. Each reads the season's already-populated `BowlerSeasonStats` rows, ranks candidates, and calls the matching `Season.Add*Winner` method. A failure or retry in one (e.g. High Block) never blocks or re-runs the others.
- **`Season.CompleteSeason()` is a new, first-class domain method — not a `Neba.Api.Legacy`-scoped extension member.** Unlike the legacy-shaped `Create`/`Update` extension pattern used for `Bowler`, this mirrors `Tournament.CompleteTournament()`, which already exists as a real aggregate method (see `docs/plans/software-backdoor-complete-tournament.md`) — season completion is genuine, permanent domain behavior (`docs/ubiquitous-language.md` already documents a `SeasonCompleted` domain-event *intent*, even though no code raises it yet), not a bridge-only mapping concern that gets deleted at sunset. The `/legacy` endpoint is simply today's only caller.
  - **Mechanical wrinkle, per CLAUDE.md's documented pattern**: `Season.Complete` is currently `{ get; init; }`. `CompleteSeason()` needs to flip it after construction, so the property becomes `{ get; internal set; }` — a real, permanent change to the aggregate (same reasoning CLAUDE.md gives for `ApplyLegacyUpdate`'s setter changes), not something removed at sunset.
  - New error: `SeasonErrors.AlreadyComplete` (`Error.Conflict`, retry-safe semantics — a second "complete" call for the same season is a legitimate retry, not a client mistake).
  - `CompleteSeason()` is **idempotent by design**, matching `CompleteTournamentSyncJob`'s treatment of `Tournament.CompleteTournament()`'s `AlreadyComplete`: the job logs it as informational and still schedules the award jobs — never treats it as a fatal error.
- **Award ranking source is exclusively the website's own `BowlerSeasonStats`** (already fully populated per season by `GenerateSeasonStatsJob`) — no new legacy (`neba-fwk`) reads for the award jobs themselves. The *only* Dapper/legacy-DB access in this whole plan is `CompleteSeasonSyncJob`'s one-time lookup of the legacy season's `Start`/`End` dates. Every award job is a pure website-side EF read (`BowlerSeasonStats` + `Bowler` for DOB/Gender where a category needs it).
- **Rookie of the Year, added per direct instruction, ranks by the same `BowlerOfTheYearPoints` column as Open** (`BowlerSeasonStats` has no dedicated `RookieOfTheYearPoints` field — there is no such column in the schema; the Software itself has no Rookie-of-the-year concept at all, see Research below). The job filters candidates to `IsRookie == true`, then ranks the same way Open does. **Flagged explicitly in "Summary of what's still undecided" below** — this is a reasoned inference from the data available, not something the requester stated in those exact words, and is worth a quick confirm before implementation.
- **`statEligibleTournamentCount` for `AddHighAverageWinner` is a season-wide constant, not a per-bowler value** — per CLAUDE.md's own worked example for this exact method (`AssignHighAverageWinner`/`AddHighAverageWinner`): `await appDbContext.Tournaments.CountAsync(t => t.SeasonId == season.Id && t.StatEligible, ct)`, computed **once** per season and passed identically to every bowler's award call. This is *not* the same as `BowlerSeasonStats.EligibleTournaments` (a per-bowler count of which eligible tournaments that specific bowler entered) — using the per-bowler field here would give bowlers who entered fewer tournaments an easier minimum-games bar, which defeats the point of a season-wide fairness floor. `tournamentsParticipated` (the other, similarly-named parameter) correctly *does* come from the per-bowler `BowlerSeasonStats.TotalTournaments`.
- **`HighBlock`'s `games` parameter is a fixed constant of `5`**, not read from any stored field — `BowlerSeasonStats.HighBlock` only ever comes from a legacy qualifying entry whose `Games` column was exactly `5` (the Software's own inherited limitation, preserved deliberately by `GenerateSeasonStatsJob` and documented in `docs/plans/software-backdoor-generate-season-stats.md`'s "Where this diverges from the Software's own report" §2). `BowlerSeasonStats` stores the winning score but not the game count that produced it, so `5` is the only value consistent with how `HighBlock` is ever populated.
- **Idempotency at the job level, not just the aggregate level.** `Season`'s own invariants (`BowlerAlreadyAwardedHighBlock`, `HighAverageMismatch`, etc.) already prevent duplicate/corrupt rows on a naive retry, but relying on them alone means every retry after a successful first run logs an "already awarded" error for every previously-assigned bowler — noisy, and indistinguishable in the logs from a real anomaly. Each award job instead **checks first** whether this season already has any award of its own kind (`season.BowlerOfTheYearAwards.Any(a => a.Category == Category.X)` / `season.HighAverageAwards.Count > 0` / `season.HighBlockAwards.Count > 0`) and short-circuits with an informational log if so — clean, silent idempotency, no reliance on error-as-control-flow.
- **A season with zero `BowlerSeasonStats` rows at all is an anomaly worth flagging, distinct from a category simply having no eligible candidates.** "No Youth bowlers this season" is normal and just no-ops quietly. "No `BowlerSeasonStats` rows exist for this season at all" almost always means `GenerateSeasonStatsJob` never ran for any tournament in the season (or ran and the season had zero tournaments) — every award job checks for this and logs a warning (see Logging below) rather than silently completing with zero winners across the board.

### Where this diverges from the Software

**The Software has no season-end award concept of its own at all.** Confirmed by research (see below): `nebamgmt-v3`'s Bowler/Senior/Super-Senior/Woman/Rookie-of-Year "points" are read-only SQL *views* (`BowlerOfYearPointsPerSeason` etc.), consumed only by the Hall-of-Fame points report — nothing in the Software persists an "award winner" anywhere, and completing a season triggers no computation at all beyond flipping `Season.Complete`. High Average and High Block awards don't exist in the Software in any form (`grep` for `HighBlock`/`HighAverage` across the whole repo returns nothing). This whole award-assignment feature is a website-only addition; there is nothing in `nebamgmt-v3` to "port" the way `LegacySeasonStatsCalculator` ported `Docs/GetBowlerSeasonStats.cs`. The calculator logic in this plan is therefore original to the website, built from `BowlerSeasonStats`'s already-computed fields and `Season`'s already-built (but never-yet-called) `Add*Winner` invariants.

## Research: `nebamgmt-v3`

### Legacy Schema Reference

Confirmed against `Data/NEBA.Data/NEBADataModel.edmx`'s SSDL (physical schema) — not independently verified against a live database, same caveat every prior `/legacy` plan carries.

| Table | Key columns | Notes |
|---|---|---|
| `Season` | `Id`, `Name`, `Start` (datetime), `End` (datetime), `Complete` (bit) | The only status flag — no separate `Closed`/`Active` column. `Audit` is flattened columns, same convention as `Stats`/`Tournaments`. |

### Every code path that can flip `Season.Complete`

Exactly **one** live entry point exists — confirmed by tracing every caller of the one repository method that can change `Complete`:

| # | Path | File:Method |
|---|---|---|
| 1 | UI button → presenter → BO → repo | `Common/NEBA.Common.UI.Controls/RetrieveSeasonsForm.cs:54-55` (`ButtonComplete_Click`) → `Common/NEBA.Common.UI.Presenters/Seasons/RetrieveSeasonsPresenter.cs:69-106` (`Retrieve.CompleteSeason()`) → `Common/NEBA.Common.BusinessLogic/Seasons/UpdateSeasonBO.vb:24-30` (`Update.Update(id, complete)`) → `Data/NEBA.Data/Repositories/Seasons/SeasonsRepository.cs:40-47` (`Repository.Update(int id, bool complete)`). |

- `RetrieveSeasonsPresenter.CompleteSeason()` is the **only caller** of `UpdateBO.Update(id, true)` in the whole repo. It guards on a season being selected, the season not already `Complete` (shows an informational message and stops if so), and an explicit "this is irreversible, are you sure?" confirm dialog. Season id is in scope as `_view.SelectedSeason.Id`.
- **Not inferred from tournament state or dates.** This is confirmed to be a genuinely separate, explicit action from individual tournament completion — `CompleteSeason()` doesn't check whether every tournament in the season is itself `Completed`; an admin can complete a season regardless (the Software leaves that ordering to the admin's judgment via the confirm dialog). This matches the plan's premise: tournament-complete (per-tournament, already covered by the existing `CompleteTournament` backdoor) and season-complete (once-per-season, this plan) are two structurally independent triggers.
- **The general season-edit form does not expose `Complete` at all.** `UpdateSeasonForm.cs` round-trips whatever `Complete` value was already loaded but has no bound checkbox (`grep`-confirmed across `SeasonControl.Designer.cs`/`UpdateSeasonForm.Designer.cs`). `AddSeasonForm.Designer.cs:48` hardcodes `Complete = false` for new seasons. There is no other way to flip this flag in the entire UI.
- **`Try`/`Catch(DatabaseCommitException)` precedent confirmed** — `UpdateSeasonBO.vb:24-30` follows the identical shape as `AddBowlerBO.cs`/`UpdateBowlerBO.cs`/`CompleteTeamTournamentBO.cs`:
  ```vb
  Try
      DataAccess.Update(id, complete)
  Catch ex As BOM.Exceptions.DatabaseCommitException
      SetErrors(ex.ToErrors())
  End Try
  ```
  The hook belongs right after `DataAccess.Update(id, complete)` succeeds — inside the `Try`, before `Catch` — identical placement to every existing backdoor call site.

### Season-end awards in the Software — computed live, never persisted

- No `HighBlock`/`HighAverage` concept anywhere in `nebamgmt-v3` (confirmed by full-repo grep) — those two awards are website-only additions with nothing to port.
- Bowler/Senior/Super-Senior/Woman/Rookie-of-Year points exist only as **read-only SQL views**, each mapped as a bare `{BowlerId, SeasonId, Points}` EF partial class: `Data/NEBA.Data/BowlerOfYearPointsPerSeason.cs`, `SeniorOfYearPointsPerSeason.cs`, `SuperSeniorOfYearPointsPerSeason.cs`, `WomanOfYearPointsPerSeason.cs`, `RookieOfYearPointsPerSeason.cs`. **`RookieOfYearPointsPerSeason` exists in the Software as a view name, but it has no dedicated points formula of its own** — same conclusion as this plan's own "rank by `BowlerOfTheYearPoints`" decision, though not independently confirmed the view computes it the identical way (the view's SQL definition wasn't inspected — flagged as unverified, see undecided items).
- These views are consumed only by `Data/NEBA.Data/Repositories/Membership/HallOfFameRepository.cs`'s `Points(...)` method, which filters to `Season.Complete == true` seasons and reads the views for a Hall-of-Fame points report. **This is the one place `Season.Complete` already gates something in the Software** — but it's a read-only report, not an award-assignment side effect.
- **Conclusion: completing a season in the Software has zero automatic downstream effect on awards.** Nothing writes an "award winner" anywhere. This confirms the website's award-assignment behavior (this plan) is a genuinely new capability, not a mirror of existing Software behavior.

### Outbound adapter — correction to the architecture doc's illustrative reference

The actual existing outbound-sync mechanism already used by every prior `/legacy` action is **`Common/NEBA.Common/Adapters/WebsiteSyncAdapter.cs`** (a static class with `NotifyNewBowler`, `NotifyBowlerUpdated`, `NotifyNewTournament`, `NotifyTournamentCompleted`, `NotifySquadScoresSynced`, `NotifyNewHallOfFameInductions`, `CheckHealthAsync`/`IsSyncEnabled`) — living in `NEBA.Common` rather than `NEBA.Common.BusinessLogic` specifically so lower-layer callers (e.g. `NEBA.Data`'s `CheckInRepository`) can reach it without inverting layering (see the class's own header comment). This plan adds one more method, `NotifySeasonCompleted(int seasonId)`, following the identical shape as `NotifyTournamentCompleted`.

## Website Side (`Legacy/Seasons/Complete/`)

New folder, several files, following `Legacy/Tournaments/Complete/`'s and `Legacy/Tournaments/Stats/`'s organization (one concern per file).

### `Season.cs` — domain change (not under `Legacy/`)

```csharp
// Season.cs — Complete becomes settable after construction
public bool Complete { get; internal set; }

// New method, alongside the existing Add*Winner methods
/// <summary>
/// Marks the season complete, allowing awards to be assigned. Idempotent — completing an
/// already-complete season returns <see cref="SeasonErrors.AlreadyComplete"/> rather than
/// throwing or silently no-oping, so callers can log it as informational on retry.
/// </summary>
public ErrorOr<Success> CompleteSeason()
{
    if (Complete)
    {
        return SeasonErrors.AlreadyComplete;
    }

    Complete = true;

    return Result.Success;
}
```

```csharp
// SeasonErrors.cs — new error
public static readonly Error AlreadyComplete = Error.Conflict(
    code: "Season.AlreadyComplete",
    description: "Season has already been marked complete.");
```

### `Legacy/Seasons/Complete/CompleteSeasonEndpoint.cs`

```csharp
namespace Neba.Api.Legacy.Seasons.Complete;

internal static class CompleteSeasonEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapCompleteSeason()
        {
            app.MapPost("/seasons/complete", (
                CompleteSeasonRequest request,
                IValidator<CompleteSeasonRequest> validator,
                IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                jobs.Enqueue<CompleteSeasonSyncJob>(job => job.SyncAsync(request.SeasonId, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}

internal sealed record CompleteSeasonRequest(int SeasonId);

internal sealed class CompleteSeasonRequestValidator : AbstractValidator<CompleteSeasonRequest>
{
    public CompleteSeasonRequestValidator() => RuleFor(r => r.SeasonId).GreaterThan(0);
}
```

### `Legacy/Seasons/Complete/CompleteSeasonSyncJob.cs`

```csharp
namespace Neba.Api.Legacy.Seasons.Complete;

internal sealed class CompleteSeasonSyncJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    IBackgroundJobClient jobs,
    IEmailSender emailSender,
    ILogger<CompleteSeasonSyncJob> logger)
{
    private static readonly TimeSpan AwardJobDelay = TimeSpan.FromHours(1);

    public async Task SyncAsync(int legacySeasonId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        // See NewBowlerSyncJob.SyncAsync for the rationale on suppressing DAP005 here.
#pragma warning disable DAP005
        var legacySeason = await legacyConnection.QuerySingleOrDefaultAsync<LegacySeasonRow>(
            "SELECT Start, [End] FROM Season WHERE Id = @SeasonId",
            new { SeasonId = legacySeasonId });
#pragma warning restore DAP005

        if (legacySeason is null)
        {
            logger.LogLegacySeasonNotFound(legacySeasonId);
            await emailSender.SendAsync(new EmailMessage
            {
                To = "website@bowlneba.com",
                Subject = "Manual intervention needed: season completion for unknown legacy season",
                HtmlBody = new UnknownLegacySeasonEmail(legacySeasonId).ToHtmlBody()
            }, ct);
            return;
        }

        var startDate = DateOnly.FromDateTime(legacySeason.Start);
        var endDate = DateOnly.FromDateTime(legacySeason.End);

        var season = await db.Seasons.SingleOrDefaultAsync(
            s => s.StartDate == startDate && s.EndDate == endDate, ct);

        if (season is null)
        {
            logger.LogLegacySeasonNotMatched(legacySeasonId, startDate, endDate);
            await emailSender.SendAsync(new EmailMessage
            {
                To = "website@bowlneba.com",
                Subject = "Manual intervention needed: season completion with no matching website season",
                HtmlBody = new UnmatchedSeasonEmail(legacySeasonId, startDate, endDate).ToHtmlBody()
            }, ct);
            return;
        }

        var completeResult = season.CompleteSeason();
        if (completeResult.IsError)
        {
            // AlreadyComplete: expected on retry/re-fire. Not fatal — still schedule the award
            // jobs anyway; each is independently idempotent (see award job "already assigned"
            // guard below), so there's no harm in re-scheduling them.
            logger.LogLegacySeasonAlreadyComplete(legacySeasonId, season.Id);
        }
        else
        {
            await db.SaveChangesAsync(ct);
        }

        jobs.Schedule<AssignOpenBowlerOfTheYearAwardJob>(job => job.AssignAsync(season.Id, CancellationToken.None), AwardJobDelay);
        jobs.Schedule<AssignWomanOfTheYearAwardJob>(job => job.AssignAsync(season.Id, CancellationToken.None), AwardJobDelay);
        jobs.Schedule<AssignSeniorBowlerOfTheYearAwardJob>(job => job.AssignAsync(season.Id, CancellationToken.None), AwardJobDelay);
        jobs.Schedule<AssignSuperSeniorBowlerOfTheYearAwardJob>(job => job.AssignAsync(season.Id, CancellationToken.None), AwardJobDelay);
        jobs.Schedule<AssignRookieBowlerOfTheYearAwardJob>(job => job.AssignAsync(season.Id, CancellationToken.None), AwardJobDelay);
        jobs.Schedule<AssignYouthBowlerOfTheYearAwardJob>(job => job.AssignAsync(season.Id, CancellationToken.None), AwardJobDelay);
        jobs.Schedule<AssignHighAverageAwardJob>(job => job.AssignAsync(season.Id, CancellationToken.None), AwardJobDelay);
        jobs.Schedule<AssignHighBlockAwardJob>(job => job.AssignAsync(season.Id, CancellationToken.None), AwardJobDelay);
    }
}

internal sealed record LegacySeasonRow(DateTime Start, DateTime End);
```

**Open item, flagged rather than assumed**: whether Hangfire's default JSON job-argument serializer round-trips a `[StronglyTypedId("ulid-full")] SeasonId` struct cleanly through `Schedule<T>(job => job.AssignAsync(season.Id, ...), ...)` the same way it already handles the plain `int legacyTournamentId` arguments every other `/legacy` job uses. No existing Hangfire job in this codebase currently passes a strongly-typed domain id as an argument (`CreateNextSeasonJob`, the only other season-related background job, takes no parameters at all) — this is a genuinely new pattern for this codebase's Hangfire usage, not a re-tread of an existing one. Verify at implementation time (a quick Hangfire dashboard check after enqueuing one manually is enough); if it doesn't round-trip cleanly, fall back to passing the id's underlying `Ulid`/string value and reconstructing `SeasonId` at the top of each award job's `AssignAsync`.

### `Legacy/Seasons/Complete/BowlerSeasonStatsRanking.cs` — shared pure helper

Not a violation of "separate job per award" — that instruction is about independent scheduling/retry, not about banning a shared, stateless ranking helper every job calls the same way `TournamentPlaceCalculator` is shared logic within the `Complete/` folder.

```csharp
internal static class BowlerSeasonStatsRanking
{
    /// <summary>
    /// Every candidate tied for the maximum of <paramref name="selector"/> — empty if
    /// <paramref name="candidates"/> is empty. Ties are intentional: <see cref="Season"/>'s own
    /// Add*Winner methods already support multiple winners sharing the same value.
    /// </summary>
    public static IReadOnlyCollection<BowlerSeasonStats> TopTiedBy<TValue>(
        IEnumerable<BowlerSeasonStats> candidates,
        Func<BowlerSeasonStats, TValue> selector)
        where TValue : IComparable<TValue>
    {
        var list = candidates.ToList();
        if (list.Count == 0)
        {
            return [];
        }

        var max = list.Max(selector);
        return list.Where(c => selector(c).CompareTo(max) == 0).ToList();
    }
}
```

### `Legacy/Seasons/Complete/SeasonAgeCalculator.cs` — shared pure helper

Deliberately duplicated from `Legacy/Tournaments/Stats/LegacySeasonStatsCalculator.cs`'s private `AgeOnDate` (itself ported from `Data/NEBA.Data/EntityExtensionMethods.cs`), rather than made cross-folder-shared — matching the standing convention that each `/legacy` action's folder is self-contained for clean sunset deletion, not cross-wired to a sibling action's private logic.

```csharp
internal static class SeasonAgeCalculator
{
    public static int? AgeOnDate(DateOnly? dateOfBirth, DateOnly asOf)
    {
        if (dateOfBirth is not { } dob)
        {
            return null;
        }

        var age = asOf.Year - dob.Year;
        if (dob > asOf.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
```

### Bowler of the Year jobs — six files

All six share the same shape: filter `BowlerSeasonStats` by the category's eligibility flag (Open has none — every bowler is a candidate), rank by points via `BowlerSeasonStatsRanking.TopTiedBy`, and call the matching `Season.Add*Winner`. Two need `Bowler.DateOfBirth` (Senior/SuperSenior/Youth need a numeric `age`); one needs `Bowler.Gender` (Woman); Open and Rookie need neither.

`Legacy/Seasons/Complete/AssignOpenBowlerOfTheYearAwardJob.cs`:

```csharp
internal sealed class AssignOpenBowlerOfTheYearAwardJob(
    AppDbContext db, ILogger<AssignOpenBowlerOfTheYearAwardJob> logger)
{
    public async Task AssignAsync(SeasonId seasonId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var season = await db.Set<Season>()
            .Include(s => s.BowlerOfTheYearAwards)
            .SingleAsync(s => s.Id == seasonId, ct);

        if (season.BowlerOfTheYearAwards.Any(a => a.Category == BowlerOfTheYearCategory.Open))
        {
            logger.LogAwardAlreadyAssigned(seasonId, nameof(BowlerOfTheYearCategory.Open));
            return;
        }

        var stats = await db.BowlerSeasonStats.Where(s => s.SeasonId == seasonId).ToListAsync(ct);
        if (stats.Count == 0)
        {
            logger.LogNoBowlerSeasonStatsForSeason(seasonId);
            return;
        }

        var winners = BowlerSeasonStatsRanking.TopTiedBy(stats, s => s.BowlerOfTheYearPoints);

        foreach (var winner in winners)
        {
            var result = season.AddOpenBowlerOfTheYearWinner(winner.BowlerId);
            if (result.IsError)
            {
                logger.LogAwardAssignmentFailed(seasonId, winner.BowlerId, nameof(BowlerOfTheYearCategory.Open), result.FirstError.Description);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
```

`Legacy/Seasons/Complete/AssignRookieBowlerOfTheYearAwardJob.cs` (same shape, filtered, ranked by the same points field per the Decision Recap above):

```csharp
internal sealed class AssignRookieBowlerOfTheYearAwardJob(
    AppDbContext db, ILogger<AssignRookieBowlerOfTheYearAwardJob> logger)
{
    public async Task AssignAsync(SeasonId seasonId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var season = await db.Set<Season>()
            .Include(s => s.BowlerOfTheYearAwards)
            .SingleAsync(s => s.Id == seasonId, ct);

        if (season.BowlerOfTheYearAwards.Any(a => a.Category == BowlerOfTheYearCategory.Rookie))
        {
            logger.LogAwardAlreadyAssigned(seasonId, nameof(BowlerOfTheYearCategory.Rookie));
            return;
        }

        var stats = await db.BowlerSeasonStats
            .Where(s => s.SeasonId == seasonId && s.IsRookie)
            .ToListAsync(ct);

        if (stats.Count == 0)
        {
            logger.LogNoEligibleCandidatesForCategory(seasonId, nameof(BowlerOfTheYearCategory.Rookie));
            return;
        }

        // No dedicated RookieOfTheYearPoints column exists — ranked by the same
        // BowlerOfTheYearPoints as Open, filtered to IsRookie. See Decision Recap.
        var winners = BowlerSeasonStatsRanking.TopTiedBy(stats, s => s.BowlerOfTheYearPoints);

        foreach (var winner in winners)
        {
            var result = season.AddRookieBowlerOfTheYearWinner(winner.BowlerId, isRookie: true);
            if (result.IsError)
            {
                logger.LogAwardAssignmentFailed(seasonId, winner.BowlerId, nameof(BowlerOfTheYearCategory.Rookie), result.FirstError.Description);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
```

`Legacy/Seasons/Complete/AssignWomanOfTheYearAwardJob.cs` (needs `Bowler.Gender`):

```csharp
internal sealed class AssignWomanOfTheYearAwardJob(
    AppDbContext db, ILogger<AssignWomanOfTheYearAwardJob> logger)
{
    public async Task AssignAsync(SeasonId seasonId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var season = await db.Set<Season>()
            .Include(s => s.BowlerOfTheYearAwards)
            .SingleAsync(s => s.Id == seasonId, ct);

        if (season.BowlerOfTheYearAwards.Any(a => a.Category == BowlerOfTheYearCategory.Woman))
        {
            logger.LogAwardAlreadyAssigned(seasonId, nameof(BowlerOfTheYearCategory.Woman));
            return;
        }

        var stats = await db.BowlerSeasonStats
            .Where(s => s.SeasonId == seasonId && s.IsWoman)
            .ToListAsync(ct);

        if (stats.Count == 0)
        {
            logger.LogNoEligibleCandidatesForCategory(seasonId, nameof(BowlerOfTheYearCategory.Woman));
            return;
        }

        var winners = BowlerSeasonStatsRanking.TopTiedBy(stats, s => s.WomanOfTheYearPoints);
        var genderByBowlerId = await db.Bowlers
            .Where(b => winners.Select(w => w.BowlerId).Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.Gender, ct);

        foreach (var winner in winners)
        {
            if (genderByBowlerId.GetValueOrDefault(winner.BowlerId) is not { } gender)
            {
                logger.LogAwardCandidateMissingBowlerData(seasonId, winner.BowlerId, nameof(BowlerOfTheYearCategory.Woman));
                continue;
            }

            var result = season.AddWomanOfTheYearWinner(winner.BowlerId, gender);
            if (result.IsError)
            {
                logger.LogAwardAssignmentFailed(seasonId, winner.BowlerId, nameof(BowlerOfTheYearCategory.Woman), result.FirstError.Description);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
```

`Legacy/Seasons/Complete/AssignSeniorBowlerOfTheYearAwardJob.cs` / `AssignSuperSeniorBowlerOfTheYearAwardJob.cs` / `AssignYouthBowlerOfTheYearAwardJob.cs` (same shape as Woman, but need `Bowler.DateOfBirth` → `SeasonAgeCalculator.AgeOnDate(dob, season.EndDate)` instead of `Gender`; filter on `IsSenior`/`IsSuperSenior`/`IsYouth` respectively, rank by `SeniorOfTheYearPoints`/`SuperSeniorOfTheYearPoints`/`YouthOfTheYearPoints`, call `AddSeniorBowlerOfTheYearWinner(bowlerId, age)` / `AddSuperSeniorBowlerOfTheYearWinner(bowlerId, age)` / `AddYouthBowlerOfTheYearWinner(bowlerId, age)`):

```csharp
// AssignSeniorBowlerOfTheYearAwardJob.cs — representative of all three age-gated jobs
internal sealed class AssignSeniorBowlerOfTheYearAwardJob(
    AppDbContext db, ILogger<AssignSeniorBowlerOfTheYearAwardJob> logger)
{
    public async Task AssignAsync(SeasonId seasonId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var season = await db.Set<Season>()
            .Include(s => s.BowlerOfTheYearAwards)
            .SingleAsync(s => s.Id == seasonId, ct);

        if (season.BowlerOfTheYearAwards.Any(a => a.Category == BowlerOfTheYearCategory.Senior))
        {
            logger.LogAwardAlreadyAssigned(seasonId, nameof(BowlerOfTheYearCategory.Senior));
            return;
        }

        var stats = await db.BowlerSeasonStats
            .Where(s => s.SeasonId == seasonId && s.IsSenior)
            .ToListAsync(ct);

        if (stats.Count == 0)
        {
            logger.LogNoEligibleCandidatesForCategory(seasonId, nameof(BowlerOfTheYearCategory.Senior));
            return;
        }

        var winners = BowlerSeasonStatsRanking.TopTiedBy(stats, s => s.SeniorOfTheYearPoints);
        var dobByBowlerId = await db.Bowlers
            .Where(b => winners.Select(w => w.BowlerId).Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, b => b.DateOfBirth, ct);

        foreach (var winner in winners)
        {
            var age = SeasonAgeCalculator.AgeOnDate(dobByBowlerId.GetValueOrDefault(winner.BowlerId), season.EndDate);
            if (age is not { } value)
            {
                logger.LogAwardCandidateMissingBowlerData(seasonId, winner.BowlerId, nameof(BowlerOfTheYearCategory.Senior));
                continue;
            }

            var result = season.AddSeniorBowlerOfTheYearWinner(winner.BowlerId, value);
            if (result.IsError)
            {
                logger.LogAwardAssignmentFailed(seasonId, winner.BowlerId, nameof(BowlerOfTheYearCategory.Senior), result.FirstError.Description);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
```

### `Legacy/Seasons/Complete/AssignHighAverageAwardJob.cs`

```csharp
internal sealed class AssignHighAverageAwardJob(
    AppDbContext db, ILogger<AssignHighAverageAwardJob> logger)
{
    private const decimal MinimumGamesMultiplier = 4.5m;

    public async Task AssignAsync(SeasonId seasonId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var season = await db.Set<Season>()
            .Include(s => s.HighAverageAwards)
            .SingleAsync(s => s.Id == seasonId, ct);

        if (season.HighAverageAwards.Count > 0)
        {
            logger.LogAwardAlreadyAssigned(seasonId, "HighAverage");
            return;
        }

        // Season-wide constant — every bowler's minimum-games bar uses the same count of the
        // season's own stat-eligible tournaments, not each bowler's personal EligibleTournaments
        // (see Decision Recap: "statEligibleTournamentCount ... is a season-wide constant").
        var statEligibleTournamentCount = await db.Set<Tournament>()
            .CountAsync(t => t.SeasonId == seasonId && t.StatEligible, ct);
        var minimumGames = (int)Math.Floor(MinimumGamesMultiplier * statEligibleTournamentCount);

        var stats = await db.BowlerSeasonStats
            .Where(s => s.SeasonId == seasonId && s.TotalGames >= minimumGames && s.TotalGames > 0)
            .ToListAsync(ct);

        if (stats.Count == 0)
        {
            logger.LogNoEligibleCandidatesForCategory(seasonId, "HighAverage");
            return;
        }

        var winners = BowlerSeasonStatsRanking.TopTiedBy(stats, s => s.TotalPinfall / (decimal)s.TotalGames);

        foreach (var winner in winners)
        {
            var average = winner.TotalPinfall / (decimal)winner.TotalGames;
            var result = season.AddHighAverageWinner(
                winner.BowlerId, average, winner.TotalGames, winner.TotalTournaments, statEligibleTournamentCount);

            if (result.IsError)
            {
                logger.LogAwardAssignmentFailed(seasonId, winner.BowlerId, "HighAverage", result.FirstError.Description);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
```

### `Legacy/Seasons/Complete/AssignHighBlockAwardJob.cs`

```csharp
internal sealed class AssignHighBlockAwardJob(
    AppDbContext db, ILogger<AssignHighBlockAwardJob> logger)
{
    // HighBlock is only ever populated from a legacy qualifying entry whose Games column was
    // exactly 5 (GenerateSeasonStatsJob's inherited Software limitation) — BowlerSeasonStats
    // stores the winning score but not the game count, so 5 is the only value consistent with
    // how HighBlock is ever produced. See Decision Recap.
    private const int HighBlockGames = 5;

    public async Task AssignAsync(SeasonId seasonId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var season = await db.Set<Season>()
            .Include(s => s.HighBlockAwards)
            .SingleAsync(s => s.Id == seasonId, ct);

        if (season.HighBlockAwards.Count > 0)
        {
            logger.LogAwardAlreadyAssigned(seasonId, "HighBlock");
            return;
        }

        var stats = await db.BowlerSeasonStats
            .Where(s => s.SeasonId == seasonId && s.HighBlock > 0)
            .ToListAsync(ct);

        if (stats.Count == 0)
        {
            logger.LogNoEligibleCandidatesForCategory(seasonId, "HighBlock");
            return;
        }

        var winners = BowlerSeasonStatsRanking.TopTiedBy(stats, s => s.HighBlock);

        foreach (var winner in winners)
        {
            var result = season.AddHighBlockWinner(winner.BowlerId, winner.HighBlock, HighBlockGames);
            if (result.IsError)
            {
                logger.LogAwardAssignmentFailed(seasonId, winner.BowlerId, "HighBlock", result.FirstError.Description);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
```

### Logging

`internal static partial class CompleteSeasonSyncJobLogMessages` (extension methods on `ILogger<CompleteSeasonSyncJob>`):

```csharp
internal static partial class CompleteSeasonSyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No legacy season found for legacy id {LegacySeasonId}; skipping season completion.")]
    public static partial void LogLegacySeasonNotFound(this ILogger<CompleteSeasonSyncJob> logger, int legacySeasonId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Legacy season {LegacySeasonId} ({StartDate:yyyy-MM-dd}-{EndDate:yyyy-MM-dd}) has no matching website season; skipping completion.")]
    public static partial void LogLegacySeasonNotMatched(this ILogger<CompleteSeasonSyncJob> logger, int legacySeasonId, DateOnly startDate, DateOnly endDate);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Legacy season {LegacySeasonId} (website season {SeasonId}) was already complete; scheduling award jobs anyway.")]
    public static partial void LogLegacySeasonAlreadyComplete(this ILogger<CompleteSeasonSyncJob> logger, int legacySeasonId, SeasonId seasonId);
}
```

Shared across all eight award job classes (`internal static partial class SeasonAwardJobLogMessages`, one generic-`ILogger<T>` set reused via the `[LoggerMessage]` source generator's support for a shared partial class — same pattern already established if the codebase does this elsewhere; otherwise, duplicate per class the same way `CompleteTournamentSyncJobLogMessages`/`SyncTournamentResultsJobLogMessages` are two separate partial classes today):

```csharp
[LoggerMessage(Level = LogLevel.Information, Message = "Season {SeasonId} already has a {Category} award assigned; skipping.")]
public static partial void LogAwardAlreadyAssigned(this ILogger logger, SeasonId seasonId, string category);

[LoggerMessage(Level = LogLevel.Warning, Message = "Season {SeasonId} has no BowlerSeasonStats rows at all; skipping award assignment.")]
public static partial void LogNoBowlerSeasonStatsForSeason(this ILogger logger, SeasonId seasonId);

[LoggerMessage(Level = LogLevel.Information, Message = "Season {SeasonId} has no eligible candidates for {Category}.")]
public static partial void LogNoEligibleCandidatesForCategory(this ILogger logger, SeasonId seasonId, string category);

[LoggerMessage(Level = LogLevel.Warning, Message = "Season {SeasonId} award candidate {BowlerId} ({Category}) is missing required Bowler data (DateOfBirth/Gender); skipping.")]
public static partial void LogAwardCandidateMissingBowlerData(this ILogger logger, SeasonId seasonId, BowlerId bowlerId, string category);

[LoggerMessage(Level = LogLevel.Error, Message = "Failed to assign {Category} award to bowler {BowlerId} in season {SeasonId}: {Reason}")]
public static partial void LogAwardAssignmentFailed(this ILogger logger, SeasonId seasonId, BowlerId bowlerId, string category, string reason);
```

None of these log parameters need `[PersonalData]`/`[PrivateData]` — `SeasonId`/`BowlerId`/category names are structural identifiers, not names/emails/DOB (per CLAUDE.md's PII redaction convention).

### Emails

`UnknownLegacySeasonEmail` and `UnmatchedSeasonEmail`, same `EmailLayout.Wrap(...)` shape as `UnlinkedTournamentCompletionEmail`/`UnlinkedTournamentStatsEmail` — one-paragraph explanation plus the relevant ids/dates, `WebUtility.HtmlEncode`d. Not reproduced in full here; follow the existing templates verbatim for shape.

### DI registration

`IValidator<CompleteSeasonRequest>` needs registering in three places, same as every other `/legacy` action:

1. **Production**: `LegacyConfiguration.cs`'s `AddLegacy()`.
2. **This action's own new test file's** `InitializeAsync()`.
3. **Every existing `/legacy` endpoint test file's** `InitializeAsync()` — `MapLegacyGroup()` maps the whole group on first request, so a missing sibling validator throws for the whole group. Files to update (confirmed via `grep -rl MapLegacyGroup tests/Neba.Api.Tests/Legacy` at planning time):
   - `tests/Neba.Api.Tests/Legacy/HealthTests.cs`
   - `tests/Neba.Api.Tests/Legacy/HallOfFame/HallOfFameTests.cs`
   - `tests/Neba.Api.Tests/Legacy/Bowlers/NewBowlerTests.cs`
   - `tests/Neba.Api.Tests/Legacy/Bowlers/UpdateBowlerTests.cs`
   - `tests/Neba.Api.Tests/Legacy/Tournaments/NewTournamentTests.cs`
   - `tests/Neba.Api.Tests/Legacy/Tournaments/SyncSquadScoresTests.cs`
   - `tests/Neba.Api.Tests/Legacy/Tournaments/Complete/CompleteTournamentEndpointTests.cs`
   - `tests/Neba.Api.Tests/Legacy/Tournaments/Stats/UpdateTournamentStatsEndpointTests.cs`

No award-job class or `CompleteSeasonSyncJob` itself needs separate DI registration — like every other `*SyncJob`, they're plain classes with constructor-injected dependencies, resolved automatically by Hangfire's activator.

Also add `app.MapCompleteSeason();` to `LegacyEndpoints.cs`'s `MapLegacyEndpoints()`, alongside the existing lines, with a `using Neba.Api.Legacy.Seasons.Complete;`.

### Tests

Following `docs/api/software-backdoor-plan.md`'s five layers, under `tests/Neba.Api.Tests/Legacy/Seasons/Complete/`:

1. **Request validation** — `CompleteSeasonRequestValidatorTests.cs`, standard FluentValidation unit test.
2. **Endpoint + auth (integration)** — `CompleteSeasonEndpointTests.cs`, `TestHost` + real `MapLegacyGroup()`, `Mock<IBackgroundJobClient>(MockBehavior.Strict)` verifying `Enqueue<CompleteSeasonSyncJob>(job => job.SyncAsync(expectedSeasonId, ...))`.
3. **`Season.CompleteSeason()` and the eight `Add*Winner` calls — pure domain logic (unit)** — these already have (or need, if missing) direct `SeasonTests.cs` unit tests independent of the backdoor: `CompleteSeason` idempotency (`AlreadyComplete` on second call), each `Add*Winner`'s existing invariants. Not new work if `Season.cs`'s existing test coverage already exercises the `Add*Winner` methods — verify and fill gaps only.
4. **Award-job ranking logic (unit, in-memory/SQLite `AppDbContext`)** — one test class per job (`AssignOpenBowlerOfTheYearAwardJobTests.cs`, etc.), covering: correct winner selection, tie handling (multiple winners sharing the max), "already assigned" short-circuit, "no eligible candidates" no-op, "zero `BowlerSeasonStats` rows at all" warning path, and (for the age-gated BOTY jobs) the "Bowler has no `DateOfBirth` on file" skip path. `AssignHighAverageAwardJobTests.cs` additionally covers the season-wide `statEligibleTournamentCount` computation and the minimum-games filter.
5. **`CompleteSeasonSyncJob` — legacy query correctness (integration)** — Postgres Testcontainers + `CREATE TEMP TABLE Season (...)`, per the standard pattern (see `docs/api/software-backdoor-plan.md`'s Testing §4), asserting the Dapper lookup and the date-range match against a seeded website `Season`.
6. **`CompleteSeasonSyncJob` idempotency (integration)** — run `SyncAsync` twice for the same legacy season id; assert the second run doesn't re-error the job (logs `AlreadyComplete` informationally) and still re-schedules the award jobs.

## Software Side (WinForms, `nebamgmt-v3`)

### Hook site

`Common/NEBA.Common.BusinessLogic/Seasons/UpdateSeasonBO.vb:24-30`, immediately after `DataAccess.Update(id, complete)` succeeds:

```vb
Try
    DataAccess.Update(id, complete)
    If complete Then
        NEBA.Common.Adapters.WebsiteSyncAdapter.NotifySeasonCompleted(id)
    End If
Catch ex As BOM.Exceptions.DatabaseCommitException
    SetErrors(ex.ToErrors())
End Try
```

Guarded on `complete` even though the method's one live caller (`RetrieveSeasonsPresenter.CompleteSeason()`) only ever passes `true` — `Update(id, complete)` is technically a general-purpose setter, so guarding defensively costs nothing and matches the same caution already present in the existing adapters.

### Adapter

New method on the existing `Common/NEBA.Common/Adapters/WebsiteSyncAdapter.cs`, following `NotifyTournamentCompleted`'s identical shape: short-timeout `HttpClient` (already static/singleton on this adapter), fire-and-forget off the UI thread, non-blocking failure (log + existing warning mechanism, no retry queue) — see `docs/api/software-backdoor-plan.md`'s Software Side section for the full standing shape; no new adapter class, just one more method:

```csharp
public static void NotifySeasonCompleted(int seasonId) =>
    Send("/legacy/seasons/complete", new { seasonId });
```

(Matching whatever the existing `Send`/`NotifyTournamentCompleted` helper's actual signature is — this plan assumes it mirrors the other `Notify*` methods exactly, not independently re-verified line-by-line during this planning session.)

### Prompt for the `nebamgmt-v3` implementation

> Add a new outbound call to the website's `/legacy/seasons/complete` backdoor endpoint (`POST`, body `{ "seasonId": <int> }`, same `X-Api-Key` header and adapter shape already used for every other backdoor call — add one more method, `NotifySeasonCompleted(int seasonId)`, to the existing static `Common/NEBA.Common/Adapters/WebsiteSyncAdapter.cs` class, following `NotifyTournamentCompleted`'s exact pattern; don't build a new adapter). The website will mark its own season complete and — one hour later — compute and assign every season-end award. This call is fire-and-forget, non-blocking, and must never fail the user's action in the Software if the website is unreachable (log + existing warning mechanism, no retry).
>
> Fire this call from `Common/NEBA.Common.BusinessLogic/Seasons/UpdateSeasonBO.vb:24-30`, `Update.Update(id, complete)`, immediately after `DataAccess.Update(id, complete)` succeeds (inside the existing `Try`, before `Catch`), and only when `complete` is `True`:
>
> ```vb
> Try
>     DataAccess.Update(id, complete)
>     If complete Then
>         NEBA.Common.Adapters.WebsiteSyncAdapter.NotifySeasonCompleted(id)
>     End If
> Catch ex As BOM.Exceptions.DatabaseCommitException
>     SetErrors(ex.ToErrors())
> End Try
> ```
>
> There is exactly one caller of `Update(id, true)` in the whole codebase: `Common/NEBA.Common.UI.Presenters/Seasons/RetrieveSeasonsPresenter.cs:69-106`, `Retrieve.CompleteSeason()`, wired to a dedicated "Complete" button on the seasons list screen (`Common/NEBA.Common.UI.Controls/RetrieveSeasonsForm.cs:54-55`). No other Software action ever flips a season to complete — you don't need to search further for additional call sites.
>
> Open items to confirm or flag during implementation (do not assume):
> - Confirm `WebsiteSyncAdapter`'s actual `Send`/helper method signature (this plan's sketch assumes it matches `NotifyTournamentCompleted`'s call shape exactly, but wasn't re-verified line-by-line during planning).
> - Whether this call site sits inside any wider transaction/rollback scope that would make "fire after the local commit succeeds" ambiguous (not established during planning).

## Summary of what's still undecided

1. ~~Whether the legacy trigger id is the Software's `Season.Id` or a tournament id.~~ **Decided**: `Season.Id`, confirmed directly.
2. ~~Whether the website resolves the season via a new `LegacyId` column or by date-range match.~~ **Decided**: date-range match against the legacy season's `Start`/`End` — no new column, since the website creates its own seasons and has never needed one before.
3. ~~Whether `Season.Complete` gets flipped by this same backdoor action, and whether the award jobs are separate from that.~~ **Decided**: `CompleteSeasonSyncJob` flips `Complete` immediately (via a new first-class `Season.CompleteSeason()` domain method, not a `Legacy`-scoped extension), then schedules eight independent award jobs one hour later.
4. ~~Whether Bowler of the Year should be one job per category or a single combined job.~~ **Decided**: eight separate jobs total (six BOTY categories + High Average + High Block), confirmed directly, including adding Rookie of the Year (not part of the original ask, added mid-planning).
5. **Rookie of the Year's ranking field is a reasoned inference, not an explicit instruction.** The requester said Rookie is possible "because on the bowler stats table we have the is rookie flag," which this plan reads as "filter candidates by `IsRookie`, then rank by the same `BowlerOfTheYearPoints` column Open uses" (there is no dedicated `RookieOfTheYearPoints` field in the schema, and the Software's own `RookieOfYearPointsPerSeason` view's actual formula wasn't inspected during research — see #6). Worth a quick confirm before implementation rather than treating as settled.
6. **The Software's own `RookieOfYearPointsPerSeason` SQL view's formula was not inspected** — only its existence as a `{BowlerId, SeasonId, Points}`-shaped EF-mapped view was confirmed. Could not confirm from within this session whether it computes points identically to `BowlerOfTheYearPoints`, or via some other formula. If it turns out to differ meaningfully, that would be a reason to reconsider #5.
7. **Whether Hangfire's job-argument serializer round-trips a strongly-typed `SeasonId` (`ulid-full`) cleanly** — every existing `/legacy` job passes a plain `int`; this plan is the first to pass a website domain id across a Hangfire job boundary. Not confirmed from within this session; flagged with a fallback (pass the underlying value, reconstruct on the other side) in the Website Side section above.
8. **Real legacy `Season` table/column names are model-only** — confirmed against `NEBADataModel.edmx`'s SSDL, not a live database, same caveat every prior `/legacy` plan carries. Verify at implementation time.
9. **`WebsiteSyncAdapter`'s exact `Send`/helper signature was not independently re-verified line-by-line** during this planning session — the sketch above assumes it mirrors `NotifyTournamentCompleted` exactly. Flagged as an explicit open item in the Software-side implementation prompt.
10. **Whether the Software-side hook site sits inside any wider transaction/rollback scope** — not established during planning, flagged in the implementation prompt per the standing pattern from prior plans.
