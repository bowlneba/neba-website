# Software Backdoor — Generate Season Stats

Mirrors `nebamgmt-v3`'s season-end "stats dump" report into the website's own `bowler_season_stats` table (`Neba.Api.Features.Stats.Domain.BowlerSeasonStats`), which already exists and already backs `GetSeasonStats` — this plan is about *populating and keeping it current*, not designing it.

This plan covers the `/legacy` endpoint and its background job(s). It follows `docs/api/software-backdoor-plan.md` (standing architecture) and builds on `docs/plans/software-backdoor-scaffolding.md`. `Legacy/Tournaments/Complete/*` (`CompleteTournamentSyncJob`, `SyncTournamentResultsJob`, `TournamentPlaceCalculator`) is the closest worked example and this plan matches its shape and file-per-concern organization throughout. `Legacy/Tournaments/SyncSquadScores.cs` is the closest precedent for the "delete existing rows, then regenerate from scratch" pattern this plan also uses.

## Decision Recap

- **One background job, `GenerateSeasonStatsJob`, triggered two ways**: chained directly from `CompleteTournamentSyncJob` (no HTTP hop — this is exactly the placeholder comment already sitting in `CompleteTournamentSyncJob.cs:59-61`), and from a new standalone endpoint, `POST /legacy/tournaments/stats/update`, taking only `{ TournamentId }` (legacy id) — same trigger-only payload shape as every other `/legacy` action. Both paths enqueue the identical job with the identical argument (the legacy tournament id); the job itself doesn't know or care which path triggered it. **The two paths differ in timing, not payload**: from `CompleteTournamentSyncJob`, `SyncTournamentResultsJob` is enqueued immediately (`jobs.Enqueue<...>`) and `GenerateSeasonStatsJob` is *scheduled* ten minutes out (`jobs.Schedule<GenerateSeasonStatsJob>(job => ..., TimeSpan.FromMinutes(10))`), giving `SyncTournamentResultsJob` time to finish placing/writing `TournamentResult` rows before this job reads them — see "Ordering" below. The standalone endpoint still enqueues `GenerateSeasonStatsJob` immediately (`jobs.Enqueue<...>`); it has no `SyncTournamentResultsJob` step to wait on, since Path 2 only fires for tournaments that are already `Completed` (results already synced).
- **The job regenerates the tournament's entire *season*, not just that tournament**, per direct instruction: delete every `bowler_season_stats` row for the season the given tournament belongs to, then recompute the whole season from scratch — "as if the rows never existed." This matches the Software's own report, which has always been a full, non-incremental recompute (`TournamentStatsRepository.Dump` — confirmed no incremental/cached state, it's a plain LINQ query run fresh every time it's manually triggered).
- **Season is the website's own `Season`, not neba-fwk's `Seasons` table** (explicitly confirmed) — resolve the legacy tournament id to a website `Tournament` (via `LegacyId`), take its `SeasonId`, and use the website `Season.StartDate`/`EndDate` as the date-range bounds for which legacy tournaments belong to "this season." Neba-fwk's own `Seasons` table is never queried by this job. This avoids a second season-resolution mechanism (website Season vs. legacy Season) ever disagreeing about season membership, and it's consistent with the fact that the output rows are keyed by the website's `SeasonId` regardless.
- **`Place`/`Payout`/`Points` are sourced from the website's own `TournamentResult` (already synced by `SyncTournamentResultsJob`, which this job is always chained after — either directly, or transitively since the standalone endpoint is only meant to fire once a tournament is already `Completed`), not from raw legacy `Stats_ResultsStats`** (explicitly confirmed, see "Where this diverges from the Software's own report" below). `SideCut` has no equivalent on `TournamentResult`, so it's still read from legacy `Stats_ResultsStats.SideCut` via Dapper, joined back to the website's own `TournamentResult` rows by (legacy `BowlerId`, legacy `TournamentId`).
- **Everything else** (qualifying stats, match play stats, membership/rookie classification, credits, cup earnings) has no website-side equivalent at all and is read entirely from neba-fwk via Dapper, matching the general `/legacy` pattern (Dapper for reads from the Software, EF for writes to the website).
- **The calculation logic is split into its own pure, unit-testable file** (`LegacySeasonStatsCalculator`), mirroring `TournamentPlaceCalculator` — no I/O, takes plain record rows in, returns plain record results out. A near-final draft of this exact logic, already reshaped for a per-`seasonId` call and already matching `BowlerSeasonStats`'s field set almost 1:1, already exists in `nebamgmt-v3` at `Docs/GetBowlerSeasonStats.cs` (with its written spec at `Docs/tournament-stats.md`) — this plan's calculator is a direct port of that draft (adjusted for the `TournamentResult`-sourced Place/Payout/Points decision above and translated from EF navigation properties to plain Dapper row collections), not a fresh design.
- **No factory/`Create` method on `BowlerSeasonStats` for this** — unlike the `internal static ErrorOr<T> Create(...)` convention for child entities with real invariants, `BowlerSeasonStats` is a computed reporting projection with `required` init-only properties and no structural invariants of its own to enforce (every field is a trusted, already-validated aggregate number, not user input) — it's constructed directly with an object initializer, exactly like `Website Port.linq`'s own historical migration does. The calculator's mapping logic still lives in its own `Legacy`-scoped file for the usual sunset-deletion reason, it just isn't wrapped in a `CreateFromLegacy` extension member, because there's no aggregate invariant for that extension to enforce.
- **Idempotency**: re-running for the same tournament (retry, or the Software firing the stats-update event again) is safe and expected — the whole point of the delete-then-regenerate approach is that repeated runs converge on the same answer rather than needing per-row dedup logic.
- **Cache invalidation**: `GetSeasonStatsQueryHandler` caches its whole `SeasonStatsDto` (which embeds every `bowler_season_stats` row for the season) under the tag `neba:stats:seasons:{seasonId}` (`CacheDescriptors.Stats.BowlerSeasonStats(seasonId)`). This job must evict that tag after `SaveChangesAsync` or the website will keep serving stale season stats until the cache entry's own TTL expires.

### Where this diverges from the Software's own report

Two deliberate, confirmed divergences from what `TournamentStatsRepository.Dump` itself would compute — flagged prominently because they mean this job's output is not byte-for-byte reproducible from a fresh read of `nebamgmt-v3` alone, and won't exactly match a historical `Stat{year}.json` export for tournaments with unplaced bowlers:

1. **Place source** (see Decision Recap above). The Software leaves `Stats_ResultsStats.Place` `null` for bowlers nobody got around to placing manually (typically cut/forfeited team-tournament non-advancers) — `HighFinish`/`AverageFinish`/`Cash` and the points formulas all key off `Place.HasValue`/`Payout`/`Points` and so silently exclude those bowlers in the Software's own report. The website's `TournamentPlaceCalculator` (run by `SyncTournamentResultsJob`, chained ahead of this job) already fills in a `Place` for every such bowler before this job ever runs. Sourcing from `TournamentResult` instead of raw `Stats_ResultsStats` therefore makes this job's `Cashes`/`HighFinish`/`AverageFinish`/points fields **more complete** than the Software's own report for the same tournament — every qualified bowler who has a `TournamentResult` row contributes, not just the ones someone manually placed. This is intentional, not a bug to reconcile.
2. **`HighBlock`'s 5-game-only window** (not something this plan changes — flagged so the limitation is inherited deliberately, not accidentally). The Software's `HighBlock` is `Max(Score)` among qualifying entries whose `Games` column happens to equal exactly 5 — there is no sliding-window "best 5 consecutive games out of a longer block" computation anywhere in `nebamgmt-v3`. A qualifying entry with more than 5 games in one block is silently excluded from `HighBlock` entirely. This plan's calculator reproduces that exact behavior (same filter, `Games == 5`) rather than inventing real windowing logic the Software itself never had — changing this would be a real behavior change to season awards, not a bridge-code detail, and is out of scope here.

## Research: `nebamgmt-v3`

### Where the report logic actually lives

`TournamentStatsRepository.Dump(DateTime asOfDate)` (`Reporting/NEBA.Reporting.Data/Tournaments/Stats/TournamentStatsRepository.cs:15-164`) is the single authoritative computation, triggered only from a manual WinForms report (`Bowler.DumpToolStripMenuItem_Click` → `Reporting/NEBA.Reporting.UI/Membership/BowlerReportsForm.cs:210-218` → `Reports.Tournaments.Stats.Dump.Execute`), rendered via `StatDump.rdlc` and exported to the historical `Stat{year}.json` files this website's `data-migration/Website Port.linq` already consumed once (`MigrateBowlerSeasonStatsAsync`, lines 672-735 of that script — its `BowlerSeasonStatsJson` class is the exact field-mapping precedent this plan's Dapper rows follow).

**A pre-existing draft port of this exact logic, already reshaped for a per-season call and already targeting `BowlerSeasonStats`'s field set, exists in `nebamgmt-v3` itself**: `Docs/GetBowlerSeasonStats.cs` (the method body) and `Docs/tournament-stats.md` (the written field-by-field spec, cross-checked against the live `Dump` source and confirmed accurate). This plan's calculator ports that draft almost directly — see "Website Side" below for the concrete adaptation.

### Legacy Schema Reference

All confirmed against `Data/NEBA.Data/NEBADataModel.edmx`'s **SSDL** (physical schema) section — not independently verified against a live database, only the model was inspected, same caveat as the `CompleteTournament` plan.

| Table | Key columns | Notes |
|---|---|---|
| `Tournaments` (base) | `Id`, `Start` (datetime), `End` (datetime), `YearlyStatEligible` (bit), `Completed` (bit) | `YearlyStatEligible` and `Completed` both live on the base table, no join needed. This is the "eligible" flag — restricted-field tournaments (senior-only, women-only, youth-only, non-champions, TOC) are not eligible on their own, though age/gender-qualified bowlers can still earn category-specific points from them. |
| `Tournaments_SinglesTournament` | `Id`, `TournamentType` (plain `int`, no lookup table) | Enum: `SinglesTournamentTypes` (`BOM/NEBA.BOM/Tournaments/Singles/SinglesTournamentTypes.cs:4-15`) — `Standard=0, NonChampions=1, Senior=2, Women=3, Champions=4, Invitational=5, Masters=6, Youth=7, SeniorWithWomen=8`. Team tournaments have no row here at all. |
| `Stats` (base, TPT) | `Id`, `BowlerId`, `TournamentId`, `Audit_UpdatedTimestamp` | One row per squad *entry* (re-entries get separate rows), same as documented in the `CompleteTournament` plan. `Audit` is flattened columns on the entity itself (`Audit_CreatedTimestamp`/`Audit_UpdatedTimestamp`/`Audit_CreatedUserName`/`Audit_UpdatedUserName`), never a separate joined table. |
| `Stats_QualifyingStats` | `Id`, `SquadId`, `Score`, `Games`, `HighGame` | Already documented in the `CompleteTournament` plan. |
| `Stats_ResultsStats` | `Id`, `Place` (nullable), `Payout`, `Points`, `SideCut` (nullable) | **This plan only reads `SideCut` from here** — `Place`/`Payout`/`Points` come from the website's own `TournamentResult` instead (see Decision Recap). `SideCut` values: `1`=Senior, `2`=SuperSenior, `3`=Woman, `999`=Combined, `null`=main cut. |
| `Stats_MatchPlayStats` | `Id`, `Round`, `Score`, `Games`, `HighGame`, `Winner` (bit) | Not previously documented. `BowlerId`/`TournamentId` live on base `Stats`, joined via `Id`. |
| `Bowlers` | `Id`, `Gender` (int), `DateOfBirth` (nullable datetime) | |
| `Memberships` | `Id`, `Name` (nvarchar(30)) | This is the **membership-type lookup table** (Standard, New Member, etc.), *not* a per-bowler record — confirmed by re-reading the SSDL after an initial wrong assumption. Query `WHERE Name LIKE '%New Member%'` to find the "New Member" type's `Id`. |
| `BowlerMemberships` | `Id`, `BowlerId`, `MembershipId` (FK → `Memberships.Id`), `BeginDate`, `EndDate` | The actual per-bowler membership record — join to `Memberships` for the type name. `IsMember` = has a row with `EndDate == season.EndDate`; `IsRookie` = `IsMember &&` their most-recent-`EndDate` row's `MembershipId` is the "New Member" type. |
| `Credits` (base, TPT) | `Id`, `Amount`, `IssuedDate`, `ExpirationDate` | |
| `Credits_BowlerCredit` | `Id` (= `Credits.Id`), `BowlerId`, `Taxable` (bit) | Join to base `Credits` for `Amount`/`IssuedDate`. Only `Taxable = 1` rows count toward `Credits` (the website field). |
| `Cups` | `Id`, `Name`, `Start`, `End` | |
| `CupResults` | `Id`, `CupId` (FK → `Cups.Id`), `BowlerId`, `Place`, `Payout` | `CupEarnings` = sum of `Payout` for a bowler's `CupResults` where the linked `Cup.End.Year == season.EndDate.Year` (matching the `nebamgmt-v3` draft's own rule — a calendar-year match on the cup's end date, not a date-range containment check). |

### Where the Software's own report gets classification/points data — field-by-field mapping

The `Docs/GetBowlerSeasonStats.cs` draft (read in full during planning, not reproduced here) already encodes every formula this job needs — season-scoped `Tournaments`/`Entries` eligibility (including the Tournament-of-Champions double-dip exclusion for the non-champion single-day winner), `IsMember`/`IsRookie`/`IsSenior`/`IsSuperSenior`/`IsWoman`/`IsYouth` classification, `FieldAverage` (bowler's qualifying average minus the field's, scoped to the specific eligible tournaments the bowler personally entered), `QualifyingHighGame`/`HighBlock`/`MatchPlayHighGame`, the five award-points formulas (`BowlerOfTheYearPoints`/`SeniorOfTheYearPoints`/`SuperSeniorOfTheYearPoints`/`WomanOfTheYearPoints`/`YouthOfTheYearPoints`, including side-cut handling), and `TournamentWinnings`/`CupEarnings`/`Credits`. This plan's calculator is a line-by-line port of that draft's logic, with two structural changes:

1. Every EF navigation property (`bowler.Stats.OfType<QualifyingStats>()`, `bowler.Memberships`, `bowler.Credits`, `bowler.CupResults`, etc.) becomes a plain in-memory `IReadOnlyCollection<TRow>` passed into the calculator, populated by a handful of Dapper queries in the job itself (grouped by legacy `BowlerId` where the draft groups by EF navigation).
2. `resultsStats`/`eligibleResultsStats`/`youthStats`/`seniorStats`/`superSeniorStats` (all of which read `Place`/`Payout`/`Points`/`SideCut` in the draft) are built from the website's own `TournamentResult` rows (joined to legacy `SideCut` by `BowlerId`+`TournamentId`) instead of raw `Stats_ResultsStats`, per the Decision Recap.

## Website Side (`Legacy/Tournaments/Stats/`)

New folder, several files, following `Legacy/Tournaments/Complete/`'s organization (one concern per file, not force-fit into a single file — that convention already gave way to a folder for `CompleteTournament` once the logic got this involved).

### `Legacy/Tournaments/Stats/UpdateTournamentStats.cs` — endpoint

```csharp
namespace Neba.Api.Legacy.Tournaments.Stats;

internal static class UpdateTournamentStatsEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapUpdateTournamentStats()
        {
            app.MapPost("/tournaments/stats/update", (
                UpdateTournamentStatsRequest request,
                [FromServices] IValidator<UpdateTournamentStatsRequest> validator,
                [FromServices] IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                jobs.Enqueue<GenerateSeasonStatsJob>(job => job.SyncAsync(request.TournamentId, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}

internal sealed record UpdateTournamentStatsRequest(int TournamentId);

internal sealed class UpdateTournamentStatsRequestValidator : AbstractValidator<UpdateTournamentStatsRequest>
{
    public UpdateTournamentStatsRequestValidator() => RuleFor(r => r.TournamentId).GreaterThan(0);
}
```

### `CompleteTournamentSyncJob.cs` — one-line addition

Replace the placeholder comment at `CompleteTournamentSyncJob.cs:59-61` with the real chain:

```csharp
jobs.Enqueue<SyncTournamentResultsJob>(job => job.SyncAsync(legacyTournamentId, CancellationToken.None));
jobs.Schedule<GenerateSeasonStatsJob>(job => job.SyncAsync(legacyTournamentId, CancellationToken.None), TimeSpan.FromMinutes(10));
```

`SyncTournamentResultsJob` is enqueued to run immediately. `GenerateSeasonStatsJob` is *scheduled* ten minutes out via Hangfire's delayed-job support (`IBackgroundJobClient.Schedule`), not enqueued for immediate parallel execution — deliberately giving `SyncTournamentResultsJob` a window to finish placing/writing `TournamentResult` rows for the just-completed tournament before `GenerateSeasonStatsJob` reads them, so the season recompute reads accurate `Place`/`Payout`/`Points` for that tournament on its very first (and normally only) run, rather than relying on a later self-correcting run to pick up what an immediate parallel run would have undercounted.

Ten minutes is a fixed, non-configurable delay — chosen as comfortably longer than `SyncTournamentResultsJob`'s expected run time for a single tournament, not tied to any measured p99. `GenerateSeasonStatsJob`'s delete-and-regenerate is still idempotent, so this delay is a data-freshness improvement, not a correctness dependency — if `SyncTournamentResultsJob` is ever still running (or fails and retries) past the ten-minute mark, the same self-correction this plan always had still applies: Hangfire's automatic retry, a later `stats/update` call (Path 2), or the next tournament completion in the same season will re-run `GenerateSeasonStatsJob` and converge. This matches the backdoor's general eventual-consistency posture elsewhere (see `docs/api/software-backdoor-plan.md`'s "Reconciliation safety net").

**This only applies to Path 1** (chained from tournament completion). The standalone endpoint (Path 2, `UpdateTournamentStatsEndpoint`) keeps enqueueing `GenerateSeasonStatsJob` immediately (`jobs.Enqueue<GenerateSeasonStatsJob>(...)`, unchanged from the endpoint code above) — Path 2 only fires for tournaments already `Completed`, so there's no `SyncTournamentResultsJob` run in flight to wait on.

### `Legacy/Tournaments/Stats/GenerateSeasonStatsJob.cs` — the job

```csharp
internal sealed class GenerateSeasonStatsJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    HybridCache cache,
    IEmailSender emailSender,
    ILogger<GenerateSeasonStatsJob> logger)
{
    public async Task SyncAsync(int legacyTournamentId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var tournament = await db.Set<Tournament>()
            .SingleOrDefaultAsync(t => t.LegacyId == legacyTournamentId, ct);
        if (tournament is null)
        {
            logger.LogLegacyTournamentNotSyncedForStatsGeneration(legacyTournamentId);
            await emailSender.SendAsync(new EmailMessage { /* UnlinkedTournamentStatsEmail, same shape as CompleteTournamentSyncJob's */ }, ct);
            return;
        }

        var season = await db.Seasons.SingleAsync(s => s.Id == tournament.SeasonId, ct);

        // Half-open interval: Season.StartDate/EndDate are DateOnly, Tournaments.Start/End are datetime.
        var seasonStart = season.StartDate.ToDateTime(TimeOnly.MinValue);
        var seasonEndExclusive = season.EndDate.AddDays(1).ToDateTime(TimeOnly.MinValue);

        // 1. Delete every existing row for this season - "as if it never existed."
        var existing = await db.BowlerSeasonStats.Where(s => s.SeasonId == season.Id).ToListAsync(ct);
        db.BowlerSeasonStats.RemoveRange(existing);

        // 2. Website tournaments in this season, with their already-synced TournamentResult rows
        //    (Place/PrizeMoney/Points - the divergence documented in the Decision Recap).
        var websiteTournaments = await db.Set<Tournament>()
            .Include(t => t.Results)
            .Where(t => t.SeasonId == season.Id && t.LegacyId != null)
            .ToListAsync(ct);

        // 3. Legacy reads (Dapper) - see "Dapper queries" below for the actual SQL, grouped:
        //    - season tournaments (Start/End/YearlyStatEligible) + singles TournamentType, by date range
        //    - Stats_QualifyingStats rows for every tournament in that set
        //    - Stats_MatchPlayStats rows for the same set
        //    - Stats_ResultsStats.SideCut for the same set (joined to TournamentResult in step 4)
        //    - Bowlers (Gender/DateOfBirth), BowlerMemberships+Memberships, Credits+BowlerCredit, CupResults+Cups
        //      for every legacy bowler id that appears in any of the above

        // 4. Join website TournamentResult rows to legacy SideCut by (legacy BowlerId, legacy TournamentId)
        //    using tournament.LegacyId and each website Bowler's LegacyId (looked up once, in bulk).

        // 5. LegacySeasonStatsCalculator.Compute(...) - pure, see below.

        // 6. Map each computed result (for bowlers with a website Bowler.LegacyId match) directly into
        //    `new BowlerSeasonStats { SeasonId = season.Id, BowlerId = ..., ... }`, db.BowlerSeasonStats.Add(...).
        //    Bowlers with no website match: log + collect for one summary email, same pattern as
        //    SyncSquadScoresSyncJob's unmapped-bowler handling.

        await db.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync($"neba:stats:seasons:{season.Id}", ct);

        // unmapped-bowler summary email, sent after SaveChangesAsync, same ordering as SyncSquadScoresSyncJob
    }
}
```

### Dapper queries (legacy reads)

All scoped to the legacy tournament ids found in the season's date range (`@SeasonStart`/`@SeasonEndExclusive` as computed above), and to the distinct legacy bowler ids that appear across all of them:

```sql
-- Season tournaments + singles type (LEFT JOIN - team tournaments have no Tournaments_SinglesTournament row)
SELECT t.Id AS TournamentId, t.Start, t.End, t.YearlyStatEligible, st.TournamentType
FROM Tournaments t
LEFT JOIN Tournaments_SinglesTournament st ON st.Id = t.Id
WHERE t.Start >= @SeasonStart AND t.End < @SeasonEndExclusive

-- Qualifying stats
SELECT s.BowlerId, s.TournamentId, q.SquadId, q.Score, q.Games, q.HighGame
FROM Stats s INNER JOIN Stats_QualifyingStats q ON s.Id = q.Id
WHERE s.TournamentId IN @TournamentIds

-- Match play stats
SELECT s.BowlerId, s.TournamentId, m.Score, m.Games, m.HighGame, m.Winner
FROM Stats s INNER JOIN Stats_MatchPlayStats m ON s.Id = m.Id
WHERE s.TournamentId IN @TournamentIds

-- SideCut only (Place/Payout/Points come from the website's own TournamentResult - see Decision Recap)
SELECT s.BowlerId, s.TournamentId, r.SideCut
FROM Stats s INNER JOIN Stats_ResultsStats r ON s.Id = r.Id
WHERE s.TournamentId IN @TournamentIds

-- Bowler demographics
SELECT Id AS BowlerId, Gender, DateOfBirth FROM Bowlers WHERE Id IN @BowlerIds

-- Membership: the "New Member" type id first, then each bowler's membership rows
SELECT Id FROM Memberships WHERE Name LIKE '%New Member%'

SELECT bm.BowlerId, bm.MembershipId, bm.EndDate
FROM BowlerMemberships bm
WHERE bm.BowlerId IN @BowlerIds

-- Taxable credits issued within the season window
SELECT bc.BowlerId, c.Amount
FROM Credits c INNER JOIN Credits_BowlerCredit bc ON c.Id = bc.Id
WHERE bc.BowlerId IN @BowlerIds AND bc.Taxable = 1
  AND c.IssuedDate >= @SeasonStart AND c.IssuedDate < @SeasonEndExclusive

-- Cup results (filtered to Cup.End.Year == season end year in C#, not SQL - keeps the rule visible/testable)
SELECT cr.BowlerId, cr.Payout, cu.End AS CupEnd
FROM CupResults cr INNER JOIN Cups cu ON cr.CupId = cu.Id
WHERE cr.BowlerId IN @BowlerIds
```

`#pragma warning disable/restore DAP005` around these, matching every other Dapper call already in `Legacy/` (see `NewBowlerSyncJob.SyncAsync` for the rationale comment to reuse verbatim).

### `Legacy/Tournaments/Stats/LegacySeasonStatsCalculator.cs` — pure logic

```csharp
internal static class LegacySeasonStatsCalculator
{
    public static IReadOnlyCollection<LegacyBowlerSeasonStatsResult> Compute(
        DateOnly seasonEndDate,
        int newMembershipTypeId,
        IReadOnlyCollection<LegacySeasonTournamentRow> seasonTournaments,
        IReadOnlyCollection<LegacyQualifyingStatsRow> qualifyingStats,
        IReadOnlyCollection<LegacyMatchPlayStatsRow> matchPlayStats,
        IReadOnlyCollection<LegacyBowlerResultRow> results, // TournamentResult + joined SideCut, see below
        IReadOnlyCollection<LegacyBowlerRow> bowlers,
        IReadOnlyCollection<LegacyMembershipRow> memberships,
        IReadOnlyCollection<LegacyCreditRow> credits,
        IReadOnlyCollection<LegacyCupResultRow> cupResults)
    {
        // Direct port of Docs/GetBowlerSeasonStats.cs's method body, with:
        //  - eligibleSeasonTournamentIds / seasonSeniorTournaments / seasonYouthTournaments /
        //    seasonWomenTournamentIds / nonChampionSingleDayWinnerId / tournamentOfChampionsId
        //    computed from `seasonTournaments` the same way the draft computes them from
        //    NebaEntities.Tournaments.OfType<SinglesTournament>(), using the
        //    SinglesTournamentTypes int values from the Legacy Schema Reference table above.
        //  - the per-bowler loop unchanged in structure, reading from the passed-in row
        //    collections (grouped by BowlerId) instead of EF navigation properties.
        //  - AgeOnDate(DateOnly? dateOfBirth, DateOnly asOf) ported from
        //    Data/NEBA.Data/EntityExtensionMethods.cs:7-17 as a private static helper -
        //    returns null if dateOfBirth is null (matching the legacy null-DOB exclusion).
        //  - resultsStats/eligibleResultsStats/etc. all read Place/PrizeMoney/Points from
        //    `results` (TournamentResult-sourced) and SideCut from the same row's joined value.
    }
}

internal sealed record LegacySeasonTournamentRow(int TournamentId, DateTime Start, DateTime End, bool YearlyStatEligible, int? SinglesTournamentType);
internal sealed record LegacyQualifyingStatsRow(int BowlerId, int TournamentId, int SquadId, int Score, int Games, int HighGame);
internal sealed record LegacyMatchPlayStatsRow(int BowlerId, int TournamentId, int Score, int Games, int HighGame, bool Winner);
internal sealed record LegacyBowlerResultRow(int BowlerId, int TournamentId, int Place, decimal PrizeMoney, int Points, int? SideCut);
internal sealed record LegacyBowlerRow(int BowlerId, int? Gender, DateOnly? DateOfBirth);
internal sealed record LegacyMembershipRow(int BowlerId, int MembershipId, DateOnly EndDate);
internal sealed record LegacyCreditRow(int BowlerId, decimal Amount);
internal sealed record LegacyCupResultRow(int BowlerId, decimal Payout, DateOnly CupEnd);

internal sealed record LegacyBowlerSeasonStatsResult(
    int BowlerId, bool IsMember, bool IsRookie, bool IsSenior, bool IsSuperSenior, bool IsWoman, bool IsYouth,
    int EligibleTournaments, int TotalTournaments, int EligibleEntries, int TotalEntries, int Cashes, int Finals,
    int TotalGames, int TotalPinfall, decimal FieldAverage, int QualifyingHighGame, int HighBlock,
    int? HighFinish, decimal? AverageFinish,
    int MatchPlayWins, int MatchPlayLosses, int MatchPlayGames, int MatchPlayPinfall, int MatchPlayHighGame,
    int BowlerOfTheYearPoints, int SeniorOfTheYearPoints, int SuperSeniorOfTheYearPoints,
    int WomanOfTheYearPoints, int YouthOfTheYearPoints,
    decimal TournamentWinnings, decimal CupEarnings, decimal Credits,
    DateTimeOffset LastUpdatedUtc);
```

**Defensive change from the legacy draft**: `Docs/GetBowlerSeasonStats.cs` assumes every bowler in its working set has at least one `ResultsStats` row (comment: "qualifying implies results," relying on `TournamentRepository.Completed()`'s auto-placeholder-insert). Since this job's `results` collection is sourced from `TournamentResult` instead, and `TournamentResult` rows only exist for bowlers `SyncTournamentResultsJob` successfully placed (see its own `unmappedLegacyBowlerIds` skip logic), a bowler with qualifying/match-play stats but genuinely no `TournamentResult` row is possible (an unmapped or unplaceable bowler upstream). The calculator must not call `.Max()`/`.Single()` unguarded against an empty `results`-derived collection for such a bowler — use `.Any()` guards (as the legacy draft already does for most fields) and treat `LastUpdatedUtc` as "now" (the job's own run time) rather than deriving it from a result row that doesn't exist, logging this as an anomaly rather than throwing.

### DI registration

`IValidator<UpdateTournamentStatsRequest>` needs registering in three places, same as every other `/legacy` action:

1. **Production**: `LegacyConfiguration.cs`'s `AddLegacy()`.
2. **This action's own new test file's** `InitializeAsync()`.
3. **Every existing `/legacy` endpoint test file's** `InitializeAsync()` — `MapLegacyGroup()` maps the whole group on first request, so a missing sibling validator throws for the whole group. Files to update (found via `grep -rl MapLegacyGroup tests/Neba.Api.Tests/Legacy`):
   - `tests/Neba.Api.Tests/Legacy/HealthTests.cs`
   - `tests/Neba.Api.Tests/Legacy/HallOfFame/HallOfFameTests.cs`
   - `tests/Neba.Api.Tests/Legacy/Bowlers/UpdateBowlerTests.cs`
   - `tests/Neba.Api.Tests/Legacy/Bowlers/NewBowlerTests.cs`
   - `tests/Neba.Api.Tests/Legacy/Tournaments/SyncSquadScoresTests.cs`
   - `tests/Neba.Api.Tests/Legacy/Tournaments/NewTournamentTests.cs`
   - `tests/Neba.Api.Tests/Legacy/Tournaments/Complete/CompleteTournamentEndpointTests.cs`

### Tests

Following `docs/api/software-backdoor-plan.md`'s five layers, collapsed into files under `tests/Neba.Api.Tests/Legacy/Tournaments/Stats/`:

1. **Request validation** — `UpdateTournamentStatsRequestValidatorTests.cs`, standard FluentValidation unit test.
2. **Endpoint + auth (integration)** — `UpdateTournamentStatsEndpointTests.cs`, `TestHost` + real `MapLegacyGroup()`, `Mock<IBackgroundJobClient>(MockBehavior.Strict)` verifying `Enqueue<GenerateSeasonStatsJob>(job => job.SyncAsync(expectedTournamentId, ...))`.
3. **Calculator — pure logic (unit)** — `LegacySeasonStatsCalculatorTests.cs`, the bulk of the test surface: one test per formula/edge case documented in this plan and in `Docs/tournament-stats.md` — eligible-vs-total tournament/entry counts, TOC double-dip exclusion, Cash/Finals, `FieldAverage`, `HighBlock`'s exactly-5-games limitation, each of the five points formulas (including side-cut handling), Rookie/Member/Senior/SuperSenior/Woman/Youth classification, `HighFinish`/`AverageFinish` null-when-no-results, `TournamentWinnings`/`CupEarnings`/`Credits`, and the defensive "bowler has no `TournamentResult`-sourced row" case described above.
4. **Job — legacy query correctness (integration)** — `GenerateSeasonStatsJobQueryTests.cs`, Postgres Testcontainers + `CREATE TEMP TABLE` shaped like the real legacy tables, per the standard pattern.
5. **Idempotency (integration)** — `GenerateSeasonStatsJobIdempotencyTests.cs` — run `SyncAsync` twice for the same tournament id, assert the second run produces the same row set (not duplicates) and that a `bowler_season_stats` row present after the first run but which should no longer exist after the second (e.g. a bowler who no longer qualifies) is actually gone — this is the one place where a real "delete-then-regenerate" test matters more than most other actions' idempotency tests, since correctness here specifically depends on the delete step, not just on `Update`-vs-`Create` branching.

## Software Side (WinForms, `nebamgmt-v3`)

### Path 1 — chained from tournament completion: no Software change needed

`CompleteTournamentSyncJob`'s new `jobs.Enqueue<GenerateSeasonStatsJob>(...)` line (see Website Side above) is a website-only change. Nothing in `nebamgmt-v3` needs to change for this path — it was already covered by `CompleteTournament`'s existing Software-side call sites (`CompleteTeamTournamentBO.Execute` / `CompleteSinglesTournamentBO.Execute`).

### Path 2 — new trigger: any ResultsStats/MatchPlayStats save on an already-completed tournament

Per direct confirmation: fire on every `Stats_ResultsStats` or `Stats_MatchPlayStats` create/update, but **only when the tournament is already `Completed`** (normal pre-completion data entry should not fire this — that data isn't final yet and will be covered once by Path 1 when the tournament completes).

All four save paths funnel through the same generic `Add<TStats>`/`UpdateStats<TStats>` business-logic base classes and `Base<TBOM,TEntity>` repository base class (`Data/NEBA.Data/Repositories/Tournaments/Stats/BaseStatsRepository.cs:12-44`, `Commit()` → `DBContextFunctions.Commit()` → `SaveChanges()`), but **`Completed` is only in scope at the UI/form layer**, not the business-logic or repository layers — `ResultsStatsForm`/`MatchPlayStatsForm` both receive `Tournament.Completed` directly as a constructor parameter from `TournamentPortal.cs` (which already holds the loaded `Tournament` object), and store it (`_completed`). The hook therefore belongs in each form's save handler, after the persistence call succeeds, reusing the form's own `_completed` field rather than adding a new lookup:

| # | Save type | Event handler | Persistence call (must succeed first) | `Completed` source |
|---|---|---|---|---|
| 1 | Results, bulk/staged | `ResultsStatsForm.ButtonSave_Click`, `Tournaments/NEBA.Tournaments.UI/Stats/ResultsStatsForm.cs:156-161` | `await new Presenters.Stats.Results(this).AddAsync()` | `_completed` (form field, `ResultsStatsForm.cs:19,46,52`) |
| 2 | Results, single-row edit | `UpdateResultsStatsForm`'s OK handler, `UpdateResultsStatsForm.cs:26` | `new Presenters.Stats.UpdateResults(this).Execute()` | **Not confirmed** — see undecided items |
| 3 | Match play, bulk/staged | `MatchPlayStatsForm.ButtonSave_Click`, `Tournaments/NEBA.Tournaments.UI/Stats/MatchPlayStatsForm.cs:174-179` | `await new Presenters.Stats.MatchPlay(this).AddAsync()` | `_completed` (form field, `MatchPlayStatsForm.cs:46,52,57`) |
| 4 | Match play, single-row edit | `UpdateMatchPlayStatsForm.cs:34` | `new Presenters.Stats.UpdateMatchPlay(this).Execute()` | **Not confirmed** — see undecided items |

**Bulk-save dedup note**: `AddAsync()` (rows 1 and 3) internally commits **once per staged row** (`BaseAddStatsBusinessLogic.cs:20-36`'s `foreach (var stat in valid) _dataAccess.Add(stat);`), not once for the whole batch. The hook still only needs to fire **once**, because it's placed at the form's event handler — after the single `await AddAsync()` call for the whole staged batch returns, not inside the per-row loop. No extra dedup logic needed on the Software side; this is a property of where the hook is placed, not something to build.

### Adapter

Same new adapter as every other `/legacy` action (see `docs/api/software-backdoor-plan.md`'s Software Side section for the full shape): short-timeout `HttpClient` (not `HttpWebRequest`), static/singleton lifetime, dispatched off the UI thread (`Task.Run`, capturing only the plain `int tournamentId` — never `this`/a form/a presenter), non-blocking failure (log + existing warning mechanism, no retry queue). This plan adds no new adapter — it reuses whatever adapter `CompleteTournament`'s implementation already built (same base URL, same API key header, same failure philosophy), pointed at a different route (`/legacy/tournaments/stats/update`) and a different id (this endpoint always takes a **tournament** id, matching every one of the four save handlers above, which all already have `Tournament.Id` in scope).

### Prompt for the `nebamgmt-v3` implementation

> Add a new outbound call to the website's `/legacy/tournaments/stats/update` backdoor endpoint (`POST`, body `{ "tournamentId": <int> }`, same `X-Api-Key` header and adapter shape already used for the tournament-completion backdoor call — reuse that adapter, don't build a new one, just call it with a different route/id). The website will enqueue a background job that regenerates that tournament's entire season's stats; this call is fire-and-forget, non-blocking, and must never fail the user's save action if the website is unreachable (log + existing warning mechanism, no retry).
>
> Fire this call after a `Stats_ResultsStats` or `Stats_MatchPlayStats` row is successfully saved (created or updated) — but **only if the tournament is already `Completed`**. Four call sites, all in `Tournaments/NEBA.Tournaments.UI*`:
>
> 1. `ResultsStatsForm.ButtonSave_Click` (`Tournaments/NEBA.Tournaments.UI/Stats/ResultsStatsForm.cs:156-161`) — after `await new Presenters.Stats.Results(this).AddAsync()` succeeds, if `_completed` is true, fire once for `this.TournamentId` (the field the form was constructed with). This covers both manual entry and the clipboard-paste bulk-import path (`LinkLabelPopulateFromClipboard_LinkClicked`), since both stage rows into the same in-memory list that only actually persists on this Save click.
> 2. `UpdateResultsStatsForm`'s OK/save handler (`UpdateResultsStatsForm.cs:26`, calls `new Presenters.Stats.UpdateResults(this).Execute()`) — same pattern, but **first confirm this form has access to the tournament's `Completed` flag** (not confirmed during planning — check the form's constructor/fields; if it's not currently passed in, it needs to be, matching how `ResultsStatsForm`/`MatchPlayStatsForm` already receive it from `TournamentPortal.cs`).
> 3. `MatchPlayStatsForm.ButtonSave_Click` (`Tournaments/NEBA.Tournaments.UI/Stats/MatchPlayStatsForm.cs:174-179`) — same pattern as #1, using `_completed` (already a field, `MatchPlayStatsForm.cs:46,52,57`).
> 4. `UpdateMatchPlayStatsForm.cs:34` — same open item as #2: confirm/add `Completed` availability.
>
> For #1 and #3 (bulk saves), only fire the outbound call **once** after the whole `AddAsync()` call returns — do not fire it per-row even though the underlying repository commits per-row internally; that's an implementation detail of `AddAsync()`, not something the caller needs to account for.
>
> Open items to resolve or flag during implementation (do not assume):
> - Whether `UpdateResultsStatsForm`/`UpdateMatchPlayStatsForm` currently have the tournament's `Completed` flag in scope, and if not, the least invasive way to thread it through (their presenters currently only see `_view.Stats`/`_view.UpdateStat`, not `Completed`, per the research this plan was built on).
> - Whether this call site sits inside any wider transaction/rollback scope that would make "fire after the local commit succeeds" ambiguous (not established during planning).

## Summary of what's still undecided

1. ~~Whether the season's date bounds come from the website's own `Season` or neba-fwk's `Seasons` table.~~ **Decided**: website's own `Season.StartDate`/`EndDate`. Avoids a second season-resolution mechanism that could disagree with the website's own notion of season membership.
2. ~~Where the Software-side trigger for the standalone stats-update endpoint should live.~~ **Decided**: every `Stats_ResultsStats`/`Stats_MatchPlayStats` save, gated on the tournament already being `Completed`.
3. ~~Whether `Place`/`Payout`/`Points` should be sourced from raw legacy `Stats_ResultsStats` or the website's own already-synced `TournamentResult`.~~ **Decided**: the website's own `TournamentResult`, joined to legacy `SideCut` only. This is a deliberate, documented divergence from what the Software's own report would compute for tournaments with unplaced bowlers (see "Where this diverges from the Software's own report").
4. **`Completed` availability on `UpdateResultsStatsForm`/`UpdateMatchPlayStatsForm`** (the single-row edit dialogs) — not confirmed during planning whether these forms/presenters currently have access to the tournament's `Completed` flag the way `ResultsStatsForm`/`MatchPlayStatsForm` do. Flagged as an explicit open item in the Software-side implementation prompt; could not be confirmed from within this session.
5. ~~Ordering between `SyncTournamentResultsJob` and `GenerateSeasonStatsJob`.~~ **Decided (revised)**: for Path 1 (tournament completion), `SyncTournamentResultsJob` is enqueued immediately and `GenerateSeasonStatsJob` is scheduled ten minutes later (`IBackgroundJobClient.Schedule`), giving the results sync time to finish before the season recompute reads `TournamentResult`. This replaces the original "run in parallel, self-correct later" decision, which is kept only as a fallback: the delete-and-regenerate is still idempotent, so if `SyncTournamentResultsJob` hasn't finished by the ten-minute mark, the same self-correction (Hangfire retry, a later `stats/update` call, or the next tournament completion in the season) still applies. Path 2 (the standalone endpoint) is unaffected — it always enqueues immediately, since it only fires post-completion after results are already synced.
6. **Real legacy table/column names, and the `SinglesTournamentTypes` enum values, are model-only** — confirmed against `NEBADataModel.edmx`'s SSDL, not a live database, same caveat every prior `/legacy` plan has carried. Verify at implementation time.
7. **`HighBlock`'s inherited 5-game-only limitation** (see "Where this diverges" above) is being knowingly preserved, not fixed, in this plan. Flagging once more here since it's a real, user-visible quirk (a tournament with a 6+ game qualifying block never contributes to anyone's `HighBlock`) that a future ask might reasonably want fixed — but that would be a genuine behavior change to season awards, out of scope for a bridge-code port.
