# Software Backdoor — Complete Tournament

Mirrors "tournament completed" from `nebamgmt-v3` into the website's `TournamentResult` rows (see `docs/plans/tournament-results.md`), including the place-assignment gap that leaves team-tournament non-advancers with a `-1` place today (`data-migration/Website Port.linq`'s `MigrateTournamentResultsAsync`).

This plan covers the `/legacy` endpoint and its background job(s). It follows `docs/api/software-backdoor-plan.md` (standing architecture) and builds on `docs/plans/software-backdoor-scaffolding.md`. `Legacy/Tournaments/NewTournament.cs` and `Legacy/Tournaments/SyncSquadScores.cs` are the closest worked examples and this plan matches their shape throughout.

## Decision Recap

- **One endpoint, `POST /legacy/tournaments/complete`, taking only the legacy tournament id.** It represents the Software's "tournament completed" event, not "sync results" specifically. This matches the shape `NewTournament`/`SyncSquadScores` already use (route = event, trigger-only payload).
- **Two chained background jobs, not one, per the requester's explicit direction.** The endpoint enqueues only `CompleteTournamentSyncJob` — a thin job whose entire job is to flip `Tournament.CompleteTournament()` and then `Enqueue` whatever else needs to happen as a result. Today that's `SyncTournamentResultsJob` (this plan's results-population work); a future season-stats-generation job (see Future Work) gets chained the same way, from the same completion job, once it exists. This keeps "mark complete" and "do the follow-on work" as independently retryable Hangfire jobs rather than one large unit of work — a failure in results-population no longer risks re-running (or being blocked behind) the completion flag flip, and a third/fourth follow-on job is just another chained `Enqueue` line, not a restructure.
- **This manual chain is a stand-in for a `TournamentCompleted` domain event that doesn't exist yet — see "Relationship to the eventual `TournamentCompleted` domain event" below.** `Tournament.CompleteTournament()` today raises no domain event; the real infrastructure for one already exists (`AggregateRoot`, `IDomainEvent`, `DomainEventDispatcherInterceptor`, `IDomainEventJob<TEvent>`) but has no feature using it yet. `CompleteTournamentSyncJob`'s hand-written `jobs.Enqueue<SyncTournamentResultsJob>(...)` call is doing by hand, for one known follow-on job, what that interceptor would do on its own for every registered `IDomainEventJob<TournamentCompleted>` handler once the event is added. The two-job split was chosen with this in mind, not just for retry isolation.
- **Team-tournament place-filling is roster-and-squad scoped, confirmed directly by the requester**, not a literal port of `TeamSquadScores.cs` (an early draft of this plan assumed it could reuse that class's "sum each member's own best entry" formula directly — wrong; see "Team place-filling algorithm" below for the corrected shape: composite score per `(roster, squad)`, forfeit-aware, bowler placed through at most one non-forfeited roster). This is the piece the LINQ migration script (`MigrateTournamentResultsAsync`) is missing today, which is why it currently emits `Place = -1` for any team-tournament bowler whose `Stats_ResultsStats.Place` is `null`.
- **The website never needs to look up team membership on its own side.** `TournamentResult` (already built, see `docs/plans/tournament-results.md`) has no team concept — `Squad`/`SquadScore` are per-bowler. Team grouping is looked up from `nebamgmt-v3` only, inside this job, purely to compute `Place` values; nothing about "teams" gets persisted to the website.
- **Idempotency**: re-running for an already-`Complete` tournament is allowed and expected (Hangfire retry, or the Software re-firing the event), for both jobs independently. `CompleteTournamentSyncJob`'s `CompleteTournament()` returning `AlreadyComplete` is treated as informational, not fatal — it still chains `SyncTournamentResultsJob` regardless. `SyncTournamentResultsJob`'s `AddResult`'s `ResultAlreadyRecorded` (duplicate `BowlerId`) is caught per-bowler and skipped, not treated as a job failure — same idempotency posture the `tournament-results.md` plan already assigned to this backdoor.
- **No correction path.** If `Stats_ResultsStats.Place`/`Payout`/`Points` changes in the Software *after* a bowler's `TournamentResult` has already been recorded, this backdoor cannot update it — `AddResult` only creates, `TournamentResult` has no `Update`. This is the same "no edit capability planned" gap `tournament-results.md` already calls out as deliberately deferred; flagging again here because this backdoor is the thing that would hit it first in practice.

## Research: `nebamgmt-v3`

### Legacy Schema Reference

All confirmed against `Data/NEBA.Data/NEBADataModel.edmx` (both CSDL/POCO and SSDL/physical-table sections) — table and column names below are the real DB schema. Flagged where noted as not independently verified against a live database (only the model was inspected).

| Table | Key columns | Notes |
|---|---|---|
| `Tournaments` (+ `Tournaments_TeamTournament` / `Tournaments_SinglesTournament`, TPT) | `Id`, `Completed` (bit) | `Completed` is the flag this backdoor mirrors as `Tournament.CompleteTournament()`. |
| `Teams` | `Id`, `TeamTournamentId` (FK → `Tournaments_TeamTournament.Id`), `Forfeit` | **One row per distinct roster**, not per bowler-pairing-across-the-tournament — if Bowler A partners with B for one squad and C for the next, that's two separate `Teams` rows (A/B and A/C), each with its own roster. `Forfeit` marks a roster that's out of contention entirely; this plan's placement logic must exclude forfeited rosters from ranking and never use their score. |
| `TeamMember` | `Teams_Id` (FK → `Teams.Id`), `Bowlers_Id` (FK → `Bowlers.Id`) | Composite-PK join table, pure roster — **the** table that groups `BowlerId`s under a `Team`. No other columns. A bowler who partnered with more than one person across the tournament has one row per roster they were part of, not one row total. |
| `SquadTeams` | `Id`, `TeamSquadId` (FK → `Squads_TeamSquad.Id`), `TeamId` (FK → `Teams.Id`), `HighGame` | One row per team-per-squad. `HighGame` is the team's persisted high-game total for that squad, set by `SetTeamHighGames` immediately before `Completed()` runs (see below) — this is the only team-level score-ish value stored anywhere. |
| `Squads` (+ `Squads_TeamSquad` / `Squads_SinglesSquad`, TPT) | `Id`, `BowlingDate`; team subclass adds `TournamentId`, `TeamsAssigned` | |
| `Stats` (base, TPT) | `Id`, `BowlerId` (FK → `Bowlers.Id`), `TournamentId` (FK → `Tournaments.Id`) | **One row per squad *entry*, not per bowler.** `Stats.Id` is a surrogate PK, not a composite key on `(BowlerId, TournamentId)` — a bowler who re-enters a tournament (bowls more than one squad, trying to beat their own score) gets a separate `Stats` row, and therefore a separate `Stats_QualifyingStats` row, per entry. Corrected after the requester flagged the original 1:1 assumption in this plan as wrong. |
| `Stats_QualifyingStats` (TPT subclass, `Id` = `Stats.Id`) | `SquadId`, `Score`, `Games`, `HighGame` | One qualifying block per entry. A bowler with 3 entries in one tournament has 3 of these rows. |
| `Stats_ResultsStats` (TPT subclass, `Id` = `Stats.Id`) | `Place` (nullable int), `Payout`, `Points`, `SideCut` | Intended to be one per bowler per tournament (see "Multi-entry bowlers" below) — but this is not schema-enforced, only convention. No `TeamId` column anywhere on this table. |

**Confirmed: there is no team-level `Score` or `Place` row anywhere in the schema.** `Place`/`Payout`/`Points` are always per-`BowlerId`.

### Multi-entry bowlers (re-entries) — no "counting entry" flag exists

Confirmed by a second research pass, prompted by the requester catching the 1:1 assumption above: **no column anywhere marks one of a bowler's multiple entries as the "counting"/"best" one.** `nebamgmt-v3` resolves this ad hoc, per use-case, via `Tournaments/NEBA.Tournaments.BusinessLogic/Cuts/GetBowlersTopScore.cs:9-33` (`GetTopScore<TScore>`): group a bowler's entries by bowler, take the one with the max `Score` (ties resolved by `.First()` on whatever order the entries happen to be enumerated in — not a deterministic tiebreak). This is the precedent for cuts, seeding, and high-out-of-money — never persisted, recomputed each time it's needed.

**This backdoor's own `Place`-filling logic must do the same reduction for singles** before ranking: collapse each bowler's multiple `Stats_QualifyingStats` rows to their single best entry (max `Score`, with a *deterministic* secondary tiebreak on `HighGame` — an improvement on the Software's arbitrary `.First()`, not a literal port of it) before using that score for ranking. **Team tournaments are different — see "Team place-filling algorithm" below.** The reduction there isn't "each bowler's own best entry independently"; it's a roster's own best *composite* squad-entry, confirmed by the requester with a concrete example: if roster A/B bowls squad 1 (A=1000, B=900, composite 1900) and squad 2 (A=950, B=1200, composite 2150), the roster's counting score is 2150 — never a mix of A's squad-1 score with B's squad-2 score.

`TournamentRepository.Completed`'s auto-placeholder insert (`Data/NEBA.Data/Repositories/Tournaments/TournamentRepository.cs:63-106`) dedupes by distinct `BowlerId` when deciding who's missing a `Stats_ResultsStats` row — so at most one placeholder gets inserted per bowler regardless of how many entries they have. **Not confirmed from within this session**: whether the *manual* results-entry UI could ever create a second real `Stats_ResultsStats` row for the same bowler in one tournament. This plan's job treats that as a data anomaly if encountered (see below), not something to silently resolve by picking one.

### How `Place` gets populated today (and why it's sometimes `null`)

- **`Place` is never computed by `nebamgmt-v3`'s own business logic — it is entered manually** through `Tournaments/NEBA.Tournaments.UI/Stats/ResultsStatsForm.cs`, or bulk-imported from a pasted spreadsheet (`LinkLabelPopulateFromClipboard_LinkClicked`, same file, lines ~257–324). For team tournaments, the clipboard-import path parses one pasted row per team (bowler ids slash-delimited), and writes one `Stats_ResultsStats` row per member, all sharing the same pasted `Place` — so *when* a team's place does get entered, it's already consistent across members. The gap is when it's never entered at all for cut teams.
- **Completing a tournament** (`CompleteTeamTournamentBO.Execute` / `CompleteSinglesTournamentBO.Execute`) both funnel to the same shared `Data/NEBA.Data/Repositories/Tournaments/TournamentRepository.cs:62`, `Completed(int tournamentId)`, which:
  1. Sets `Tournaments.Completed = true`.
  2. Converts raw per-game `QualifyingScores` into aggregated `Stats_QualifyingStats` rows, then **deletes the raw per-game rows** (this matters below — the per-game breakdown is gone after this point).
  3. For any bowler with qualifying stats but no `Stats_ResultsStats` row yet, auto-inserts one with `Place = null`, `Payout = 0`, `Points = tournament.EntryPoints`.
- **So every bowler who qualified always has a `Stats_ResultsStats` row with real `Payout`/`Points` values — only `Place` is ever missing**, and only for bowlers nobody got around to manually placing (typically: cut, non-advancing entrants in team events, since match-play finishers are always placed by hand). This is exactly the shape the existing LINQ migration's cut-fill logic assumes for singles; it just never got extended to team.
- **Team-score merge already exists in `nebamgmt-v3`**, used for qualifying cuts/seeding (never persisted, never wired to `Place`): `BOM/NEBA.BOM/Tournaments/Scores/TeamSquadScores.cs`, class `Team`:
  - `Score` = sum of each team member's `Stats_QualifyingStats.Score` (or raw `QualifyingScores` pre-completion).
  - `HighGame` = max per-game team total when raw per-game data is available; **falls back to the persisted `SquadTeams.HighGame` column once raw scores are deleted** (i.e. always, by the time this backdoor's job runs post-`Completed()`).
  - `Games` = sum of member games ÷ team size (not needed for placement — see below).

  **This class's `Score` formula sums a member's `Stats_QualifyingStats.Score` without regard to which squad it came from** — fine for its own use-cases (a `Team`/roster is constructed per-squad by its caller, so the ambiguity never arises there), but not safe to reuse as-is here, where a roster can have qualifying data spanning more than one squad-entry across the whole tournament and a member's *other* roster's squad score must never leak in. This plan's own algorithm (below) is roster-*and*-squad scoped for that reason — inspired by this class, not a literal port of it.

### Team place-filling algorithm

Reworked after the requester walked through the forfeit/partner-swap scenario directly (three follow-up questions, all confirmed — see below). This is **not** "each bowler's own best entry summed independently" (an earlier draft of this plan got this wrong) — it's roster-scoped, forfeit-aware, and a bowler is only placed through the one roster of theirs that's still in contention.

**Confirmed rules** (all from the requester, via direct example and follow-up questions):

- **A roster's composite score for one squad** = sum of its members' `Stats_QualifyingStats.Score` *for that same `SquadId`* — never a member's score from a different squad they happened to bowl with a different partner. Confirmed via the A/B, A=1000/B=900 (squad 1) vs. A=950/B=1200 (squad 2) example: the roster's counting score is squad 2's composite (2150), not a mix of A's squad-1 score and B's squad-2 score.
- **A roster that enters more than one squad together (no partner change, just re-entering to beat their own score) reduces to its single best squad-entry** — same "best entry wins" rule as singles, applied at the roster level: max composite score, deterministic tiebreak on that same squad-entry's `SquadTeams.HighGame` (not maxed independently across squads — the winning entry's own high game travels with it, exactly like the singles reduction in "Multi-entry bowlers" above).
- **Forfeited rosters (`Teams.Forfeit = true`) are excluded from ranking entirely** and contribute nothing to anyone's placement.
- **A bowler is placed through at most one non-forfeited roster** — confirmed guaranteed by tournament rules (a bowler can't have two counting rosters at once; if a bowler bowled with B, then C, then D across three squads, the B and C rosters are forfeited and only the A/D roster can advance or be ranked). If this assumption is ever violated in real data (a bowler somehow has two non-forfeited rosters), that's a data anomaly to log, not a case the algorithm resolves — see Field-mapping edge cases.
- **A bowler with no non-forfeited roster at all** (every roster they were part of got forfeited — e.g. Bowler B in the A/B → A/C swap, where A/B is forfeited and B has no other roster) **goes to one shared last place**, tied with every other such bowler in the tournament — confirmed by the requester as the intended behavior, consistent with how ties already share `Place` elsewhere in this design. They are not scored or ranked against each other; there's no meaningful score to rank them by once their only roster is forfeited.

**Algorithm**, scoped to only the bowlers missing a `Place`:

1. For each `Teams.Id` (roster) that has at least one `SquadTeams` row, compute a composite entry per `(TeamId, SquadId)`: `Score` = sum of the roster's members' `Stats_QualifyingStats.Score` for that `SquadId`; `Games` = sum of the same members' `Games` for that `SquadId`; `HighGame` = that specific `SquadTeams` row's persisted `HighGame` column (not maxed across the roster's other squad-entries).
2. Reduce each roster to its single best `(TeamId, SquadId)` entry: max `Score`, tiebreak max `HighGame`.
3. Exclude any roster where `Teams.Forfeit = true`.
4. For each missing bowler, find the one non-forfeited roster they belong to (via `TeamMember`). If none, they're deferred to step 6.
5. Rank the remaining (non-forfeited, missing-`Place`) rosters: `Games` descending, then `Score` descending, then `HighGame` descending — same three-key order as singles, confirmed by the requester. Starting at `MAX(Place)` among already-placed bowlers + 1, assign one `Place` per roster, in rank order, to every member of that roster who's still missing a `Place`.
6. Every bowler still missing a `Place` after step 5 (no non-forfeited roster at all) shares one common `Place` value — `nextPlace` after the last ranked roster — tied with each other, not individually ranked.

Singles ranking is unchanged from the prior draft: `Games DESC, Score DESC, HighGame DESC` on each bowler's own best-entry reduction (see "Multi-entry bowlers" above).

### Entry points for "tournament completed" — all found

No API, console app, or scheduled job touches tournament completion today; the WinForms UI is the only caller. Both formats funnel to the same shared repository method, so this backdoor needs **two Software-side call sites** (one per format's BO), not one — the shared method itself (`TournamentRepository.Completed`) is the ideal *conceptual* hook point but sits below the BO layer's `Errors`/warning plumbing the failure-philosophy pattern relies on (see Software Side below).

| # | Path | File:Method |
|---|---|---|
| 1 (Team) | UI menu → presenter → BO → repo | `Tournaments/NEBA.Tournaments.UI/TournamentPortal.cs:204` → `Tournaments/NEBA.Tournaments.UI/Team/TeamTournamentPortal.cs:23` → `Tournaments/NEBA.Tournaments.UI.Presenters/Team/TeamTournamentPortalPresenter.cs:14` → `Tournaments/NEBA.Tournaments.UI.Presenters/TournamentPortalPresenter.cs:56` (`CompleteTournament()`) → **`Tournaments/NEBA.Tournaments.BusinessLogic/Team/CompleteTeamTournamentBO.cs:21`** (`Execute`) → `Data/NEBA.Data/Repositories/Tournaments/Team/TeamTournamentRepository.cs:50` → shared `TournamentRepository.Completed`. |
| 2 (Singles) | Parallel path, same shared repo call | `Tournaments/NEBA.Tournaments.UI/Singles/SinglesTournamentPortal.cs` → `Tournaments/NEBA.Tournaments.UI.Presenters/Singles/SinglesTournamentPortalPresenter.cs:14` → same shared presenter → **`Tournaments/NEBA.Tournaments.BusinessLogic/Singles/CompleteSinglesTournamentBO.cs`** → `Data/NEBA.Data/Repositories/Tournaments/Singles/SinglesTournamentRepository.cs:35` → shared `TournamentRepository.Completed`. |

**Excluded, with reason**: `Tournaments/NEBA.Tournaments.UI/Stats/ResultsStatsForm.cs` (manual `Place`/`Payout`/`Points` entry) does *not* flip `Completed` and can be edited both before and after completion — it's not a "tournament completed" trigger, it's ordinary results data entry that happens to feed the same tables. `Main/NEBA.WebAPI` has no tournament-related controller (only `PingController`/`StatsController`, the latter serving hardcoded sample data, confirmed by reading the full file) — no API path exists today.

## Website Side (`Legacy/Tournaments/CompleteTournament.cs`)

```csharp
using System.Data;
using System.Globalization;
using System.Net;

using Dapper;

using FluentValidation;

using Hangfire;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Email;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Identity;

namespace Neba.Api.Legacy.Tournaments;

internal static class CompleteTournamentEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapCompleteTournament()
        {
            app.MapPost("/tournaments/complete", (
                CompleteTournamentRequest request,
                IValidator<CompleteTournamentRequest> validator,
                IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                jobs.Enqueue<CompleteTournamentSyncJob>(job => job.SyncAsync(request.TournamentId, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}

internal sealed record CompleteTournamentRequest(int TournamentId);

internal sealed class CompleteTournamentRequestValidator
    : AbstractValidator<CompleteTournamentRequest>
{
    public CompleteTournamentRequestValidator()
    {
        RuleFor(request => request.TournamentId)
            .GreaterThan(0);
    }
}

// Thin on purpose: this job's only job is "mark the website tournament complete, then hand off
// to whatever else needs to happen as a result." It does not itself populate TournamentResult
// rows or touch neba-fwk beyond the one EF write - that's SyncTournamentResultsJob's job,
// chained from here so it (and any future sibling, e.g. a season-stats generator) runs as its
// own independent, independently-retryable Hangfire job rather than being bundled into one
// large unit of work.
internal sealed class CompleteTournamentSyncJob(
    AppDbContext db,
    IBackgroundJobClient jobs,
    IEmailSender emailSender,
    ILogger<CompleteTournamentSyncJob> logger)
{
    public async Task SyncAsync(int legacyTournamentId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var tournament = await db.Set<Tournament>()
            .SingleOrDefaultAsync(t => t.LegacyId == legacyTournamentId, ct);
        if (tournament is null)
        {
            logger.LogLegacyTournamentNotSyncedForCompletion(legacyTournamentId);

            await emailSender.SendAsync(new EmailMessage
            {
                To = "website@bowlneba.com",
                Subject = "Manual intervention needed: tournament completion with no linked tournament",
                HtmlBody = new UnlinkedTournamentCompletionEmail(legacyTournamentId).ToHtmlBody()
            }, ct);

            return;
        }

        var completeResult = tournament.CompleteTournament();
        if (completeResult.IsError)
        {
            // AlreadyComplete: expected on retry/re-fire. Not fatal — still chain the follow-on
            // jobs below (see idempotency decision above); they're each independently safe to
            // re-run.
            logger.LogLegacyTournamentAlreadyCompleteForResultSync(legacyTournamentId);
        }
        else
        {
            await db.SaveChangesAsync(ct);
        }

        jobs.Enqueue<SyncTournamentResultsJob>(job => job.SyncAsync(legacyTournamentId, CancellationToken.None));

        // A season-stats generator job is expected to be chained from here too, once that job
        // exists (see Future Work) — one more jobs.Enqueue<...>() line, same legacy tournament
        // id, added alongside the line above. Not designed by this plan.
    }
}

// Does the actual TournamentResult population (including the team place-filling algorithm
// below) - split from CompleteTournamentSyncJob so it's its own independently-retryable job,
// per the requester's direction that "complete" and "populate results" be separate background
// jobs rather than one large unit of work.
internal sealed class SyncTournamentResultsJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    IEmailSender emailSender,
    ILogger<SyncTournamentResultsJob> logger)
{
    public async Task SyncAsync(int legacyTournamentId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var tournament = await db.Set<Tournament>()
            .Include(t => t.Results)
            .SingleOrDefaultAsync(t => t.LegacyId == legacyTournamentId, ct);
        if (tournament is null)
        {
            // Shouldn't happen in practice - CompleteTournamentSyncJob already confirmed the
            // tournament exists and is linked before enqueuing this job - but a job can run on
            // a different worker at a different time, so defend against it rather than assume.
            logger.LogLegacyTournamentNotSyncedForResultSync(legacyTournamentId);
            return;
        }

        // See NewBowlerSyncJob.SyncAsync for the rationale on suppressing DAP005 here.
#pragma warning disable DAP005
        var resultRows = (await legacyConnection.QueryAsync<LegacyResultRow>(
            """
            SELECT
                s.BowlerId,
                r.Place,
                r.Payout AS PrizeMoney,
                r.Points
            FROM
                Stats s
            INNER JOIN Stats_ResultsStats r ON s.Id = r.Id
            WHERE
                s.TournamentId = @TournamentId
            """,
            new { TournamentId = legacyTournamentId })).ToList();

        var qualifyingRows = (await legacyConnection.QueryAsync<LegacyQualifyingRow>(
            """
            SELECT
                s.BowlerId,
                q.SquadId,
                q.Score,
                q.Games,
                q.HighGame
            FROM
                Stats s
            INNER JOIN Stats_QualifyingStats q ON s.Id = q.Id
            WHERE
                s.TournamentId = @TournamentId
            """,
            new { TournamentId = legacyTournamentId })).ToList();

        var isTeamTournament = tournament.TournamentType.TeamSize > 1;

        List<LegacyTeamRow> teamRows = [];
        List<LegacyTeamMemberRow> teamMemberRows = [];
        List<LegacyTeamSquadRow> teamSquadRows = [];

        if (isTeamTournament)
        {
            // One row per roster (Teams.Id) - a bowler who partnered with different people
            // across the tournament has one Teams row per pairing, not one per bowler.
            teamRows = (await legacyConnection.QueryAsync<LegacyTeamRow>(
                """
                SELECT
                    t.Id AS TeamId,
                    t.Forfeit
                FROM
                    Teams t
                WHERE
                    t.TeamTournamentId = @TournamentId
                """,
                new { TournamentId = legacyTournamentId })).ToList();

            teamMemberRows = (await legacyConnection.QueryAsync<LegacyTeamMemberRow>(
                """
                SELECT
                    tm.Bowlers_Id AS BowlerId,
                    tm.Teams_Id AS TeamId
                FROM
                    TeamMember tm
                INNER JOIN Teams t ON t.Id = tm.Teams_Id
                WHERE
                    t.TeamTournamentId = @TournamentId
                """,
                new { TournamentId = legacyTournamentId })).ToList();

            // One row per (roster, squad) - a roster that re-entered the same squad grouping
            // more than once (no partner change) has more than one row here.
            teamSquadRows = (await legacyConnection.QueryAsync<LegacyTeamSquadRow>(
                """
                SELECT
                    st.TeamId,
                    st.TeamSquadId AS SquadId,
                    st.HighGame
                FROM
                    Teams t
                INNER JOIN SquadTeams st ON st.TeamId = t.Id
                WHERE
                    t.TeamTournamentId = @TournamentId
                """,
                new { TournamentId = legacyTournamentId })).ToList();
        }
#pragma warning restore DAP005

        // Stats_ResultsStats is only ever meant to hold one row per BowlerId per tournament
        // (TournamentRepository.Completed's own placeholder-insert dedupes by BowlerId), but
        // that's convention, not a schema constraint - not confirmed from within this session
        // whether manual results entry could ever create a second row for the same bowler. If
        // it happens, treat it as a data anomaly worth a human look rather than silently
        // picking one arbitrarily.
        var resultRowsByBowlerId = resultRows.GroupBy(r => r.BowlerId).ToList();
        var anomalousLegacyBowlerIds = resultRowsByBowlerId.Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        foreach (var legacyBowlerId in anomalousLegacyBowlerIds)
        {
            logger.LogLegacyBowlerHasMultipleResultRows(legacyBowlerId, legacyTournamentId);
        }
        var singleResultRows = resultRowsByBowlerId.Where(g => g.Count() == 1).Select(g => g.Single()).ToList();

        var placeByLegacyBowlerId = ComputePlaces(singleResultRows, qualifyingRows, teamRows, teamMemberRows, teamSquadRows);

        var legacyBowlerIds = singleResultRows.Select(r => r.BowlerId).Distinct().ToList();
        var bowlerIdsByLegacyId = await db.Bowlers
            .Where(bowler => bowler.LegacyId != null && legacyBowlerIds.Contains(bowler.LegacyId.Value))
            .ToDictionaryAsync(bowler => bowler.LegacyId!.Value, bowler => bowler.Id, ct);

        var unmappedLegacyBowlerIds = new List<int>();

        foreach (var row in singleResultRows)
        {
            if (!bowlerIdsByLegacyId.TryGetValue(row.BowlerId, out var bowlerId))
            {
                logger.LogLegacyBowlerNotSyncedForResultSync(row.BowlerId, legacyTournamentId);
                unmappedLegacyBowlerIds.Add(row.BowlerId);
                continue;
            }

            if (!placeByLegacyBowlerId.TryGetValue(row.BowlerId, out var place))
            {
                // No Place on the row and no qualifying row to derive one from (e.g. a
                // no-show with no qualifying stats at all) — can't be placed. Logged and
                // skipped; needs manual entry in the Software.
                logger.LogLegacyResultCannotBePlaced(row.BowlerId, legacyTournamentId);
                continue;
            }

            var added = tournament.AddResult(bowlerId, place, row.PrizeMoney, row.Points);
            if (added.IsError)
            {
                // Expected on retry: ResultAlreadyRecorded for a bowler synced by a prior run.
                logger.LogLegacyResultSyncSkipped(row.BowlerId, legacyTournamentId, added.FirstError.Description);
            }
        }

        await db.SaveChangesAsync(ct);

        if (unmappedLegacyBowlerIds.Count > 0)
        {
            await emailSender.SendAsync(new EmailMessage
            {
                To = "website@bowlneba.com",
                Subject = "Manual intervention needed: unsynced bowler(s) in tournament result sync",
                HtmlBody = new UnsyncedBowlerResultSyncEmail(unmappedLegacyBowlerIds, legacyTournamentId, isTeamTournament).ToHtmlBody()
            }, ct);
        }
    }

    // Pure mapping logic — no I/O — so it's unit-testable on its own (see Tests, layer 3).
    // Returns a Place for every bowler that can be placed: those with a real Place already,
    // plus (singles) individually-ranked fills, plus (team) roster-ranked fills, plus every
    // remaining bowler whose only roster(s) were forfeited, sharing one last-place value.
    internal static Dictionary<int, int> ComputePlaces(
        IReadOnlyCollection<LegacyResultRow> results,
        IReadOnlyCollection<LegacyQualifyingRow> qualifying,
        IReadOnlyCollection<LegacyTeamRow> teams,
        IReadOnlyCollection<LegacyTeamMemberRow> teamMembers,
        IReadOnlyCollection<LegacyTeamSquadRow> teamSquads)
    {
        var places = results
            .Where(r => r.Place.HasValue)
            .ToDictionary(r => r.BowlerId, r => r.Place!.Value);

        var nextPlace = (places.Count > 0 ? places.Values.Max() : 0) + 1;

        var missingBowlerIds = results
            .Where(r => !r.Place.HasValue)
            .Select(r => r.BowlerId)
            .ToHashSet();

        var isTeamTournament = teams.Count > 0;

        if (!isTeamTournament)
        {
            // A bowler can have more than one qualifying row (re-entries) - collapse to their
            // single best entry first, same rule the Software itself uses for cuts/seeding
            // (GetBowlersTopScore.GetTopScore: max Score), with a deterministic HighGame
            // tiebreak in place of the Software's arbitrary .First()-on-tie.
            var bestQualifyingByBowlerId = qualifying
                .GroupBy(q => q.BowlerId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(q => q.Score).ThenByDescending(q => q.HighGame).First());

            foreach (var bowlerId in missingBowlerIds
                .Where(bestQualifyingByBowlerId.ContainsKey)
                .OrderByDescending(id => bestQualifyingByBowlerId[id].Games)
                .ThenByDescending(id => bestQualifyingByBowlerId[id].Score)
                .ThenByDescending(id => bestQualifyingByBowlerId[id].HighGame))
            {
                places[bowlerId] = nextPlace++;
            }

            return places;
        }

        // Team tournament: a roster's composite score for one squad is the sum of its members'
        // scores *for that specific squad* - never mixed across a roster's other squad-entries
        // (confirmed by the requester's A/B-vs-A/C-across-two-squads example).
        var qualifyingByBowlerAndSquad = qualifying.ToDictionary(q => (q.BowlerId, q.SquadId));
        var membersByTeamId = teamMembers
            .GroupBy(m => m.TeamId)
            .ToDictionary(g => g.Key, g => g.Select(m => m.BowlerId).ToList());
        var forfeitByTeamId = teams.ToDictionary(t => t.TeamId, t => t.Forfeit);

        // Reduce each roster to its single best (roster, squad) composite entry - same
        // "best entry wins" rule as singles, but scored at the roster level, and the winning
        // entry's own HighGame travels with it rather than being maxed independently.
        var bestEntryByTeamId = teamSquads
            .GroupBy(ts => ts.TeamId)
            .ToDictionary(g => g.Key, g =>
            {
                var members = membersByTeamId.GetValueOrDefault(g.Key, []);
                return g.Select(ts => new
                {
                    ts.HighGame,
                    Score = members.Sum(b => qualifyingByBowlerAndSquad.TryGetValue((b, ts.SquadId), out var q) ? q.Score : 0),
                    Games = members.Sum(b => qualifyingByBowlerAndSquad.TryGetValue((b, ts.SquadId), out var q) ? q.Games : 0)
                })
                .OrderByDescending(entry => entry.Score)
                .ThenByDescending(entry => entry.HighGame)
                .First();
            });

        // Bowler -> the one non-forfeited roster they belong to, if any. Guaranteed at most
        // one per the requester (a bowler can't have two counting rosters at once) - if that's
        // ever violated in real data, GroupBy(...).ToDictionary below throws, surfacing it as
        // an anomaly rather than silently picking one (see Field-mapping edge cases).
        var countingTeamIdByBowlerId = teamMembers
            .Where(m => !forfeitByTeamId.GetValueOrDefault(m.TeamId))
            .ToDictionary(m => m.BowlerId, m => m.TeamId);

        var rankedTeamIds = missingBowlerIds
            .Where(countingTeamIdByBowlerId.ContainsKey)
            .Select(id => countingTeamIdByBowlerId[id])
            .Distinct()
            .Where(bestEntryByTeamId.ContainsKey)
            .OrderByDescending(teamId => bestEntryByTeamId[teamId].Games)
            .ThenByDescending(teamId => bestEntryByTeamId[teamId].Score)
            .ThenByDescending(teamId => bestEntryByTeamId[teamId].HighGame);

        foreach (var teamId in rankedTeamIds)
        {
            foreach (var bowlerId in membersByTeamId.GetValueOrDefault(teamId, []).Where(missingBowlerIds.Contains))
            {
                places[bowlerId] = nextPlace;
            }

            nextPlace++;
        }

        // Every bowler still missing a Place has no non-forfeited roster at all (every roster
        // they were part of was forfeited) - confirmed by the requester: they all share one
        // common last place, tied, not individually ranked against each other.
        foreach (var bowlerId in missingBowlerIds.Where(id => !places.ContainsKey(id)))
        {
            places[bowlerId] = nextPlace;
        }

        return places;
    }
}

internal sealed record LegacyResultRow(int BowlerId, int? Place, decimal PrizeMoney, int Points);

internal sealed record LegacyQualifyingRow(int BowlerId, int SquadId, int Score, int Games, int HighGame);

internal sealed record LegacyTeamRow(int TeamId, bool Forfeit);

internal sealed record LegacyTeamMemberRow(int BowlerId, int TeamId);

internal sealed record LegacyTeamSquadRow(int TeamId, int SquadId, int HighGame);

internal static partial class CompleteTournamentSyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No website tournament found for legacy id {LegacyTournamentId}; skipping completion sync.")]
    public static partial void LogLegacyTournamentNotSyncedForCompletion(this ILogger<CompleteTournamentSyncJob> logger, int legacyTournamentId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Legacy tournament {LegacyTournamentId} was already complete; chaining the result sync job anyway.")]
    public static partial void LogLegacyTournamentAlreadyCompleteForResultSync(this ILogger<CompleteTournamentSyncJob> logger, int legacyTournamentId);
}

internal static partial class SyncTournamentResultsJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No website tournament found for legacy id {LegacyTournamentId}; skipping result sync. Shouldn't happen - CompleteTournamentSyncJob already confirmed the link before chaining this job.")]
    public static partial void LogLegacyTournamentNotSyncedForResultSync(this ILogger<SyncTournamentResultsJob> logger, int legacyTournamentId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No website bowler found for legacy bowler {LegacyBowlerId} (legacy tournament {LegacyTournamentId}); skipping their result and sending a manual-intervention email.")]
    public static partial void LogLegacyBowlerNotSyncedForResultSync(this ILogger<SyncTournamentResultsJob> logger, int legacyBowlerId, int legacyTournamentId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Legacy bowler {LegacyBowlerId} has more than one Stats_ResultsStats row for legacy tournament {LegacyTournamentId}; skipping - this shouldn't happen and needs manual review in the Software.")]
    public static partial void LogLegacyBowlerHasMultipleResultRows(this ILogger<SyncTournamentResultsJob> logger, int legacyBowlerId, int legacyTournamentId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Legacy bowler {LegacyBowlerId} (legacy tournament {LegacyTournamentId}) has no Place and no qualifying stats to derive one from; skipping.")]
    public static partial void LogLegacyResultCannotBePlaced(this ILogger<SyncTournamentResultsJob> logger, int legacyBowlerId, int legacyTournamentId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Skipped syncing result for legacy bowler {LegacyBowlerId} (legacy tournament {LegacyTournamentId}): {Reason}")]
    public static partial void LogLegacyResultSyncSkipped(this ILogger<SyncTournamentResultsJob> logger, int legacyBowlerId, int legacyTournamentId, string reason);
}

internal sealed class UnlinkedTournamentCompletionEmail(int legacyTournamentId)
{
    public string ToHtmlBody()
    {
        var body = $"""
                    <p>Legacy tournament id <strong>{WebUtility.HtmlEncode(legacyTournamentId.ToString(CultureInfo.CurrentCulture))}</strong> was reported complete, but no website tournament is linked to it (no <code>Tournament.LegacyId</code> match).</p>
                    <p>This usually means the <code>NewTournament</code> backdoor sync never ran or couldn't resolve a unique match. Results were not synced and will need to be re-synced (re-triggering completion again after the tournament is linked will pick them up).</p>
                    """;

        return EmailLayout.Wrap(body);
    }
}

internal sealed class UnsyncedBowlerResultSyncEmail(IReadOnlyCollection<int> legacyBowlerIds, int legacyTournamentId, bool isTeamTournament)
{
    public string ToHtmlBody()
    {
        var idRows = string.Concat(legacyBowlerIds
            .Select(id => $"<tr><td>{WebUtility.HtmlEncode(id.ToString(CultureInfo.CurrentCulture))}</td></tr>"));

        var teamNote = isTeamTournament
            ? "<p>This is a team tournament — an unmapped bowler is also excluded from their team's merged qualifying score, which may affect their teammates' computed <code>Place</code> if the team didn't advance.</p>"
            : "";

        var body = $"""
                    <p>Legacy tournament id <strong>{WebUtility.HtmlEncode(legacyTournamentId.ToString(CultureInfo.CurrentCulture))}</strong> completed with bowler(s) that have no matching website bowler (no <code>Bowler.LegacyId</code> match).</p>
                    {teamNote}
                    <p>This usually means the <code>NewBowler</code>/<code>UpdateBowler</code> backdoor sync never ran for them. Their results were not saved and will need to be re-synced.</p>
                    <table><thead><tr><th>Legacy Bowler Id</th></tr></thead><tbody>{idRows}</tbody></table>
                    """;

        return EmailLayout.Wrap(body);
    }
}
```

### Field-mapping edge cases

- **Re-entries (multiple `Stats_QualifyingStats` rows per bowler/roster).** Confirmed by the requester: the app's rule is "highest score counts," and the ranking key for filling missing places is `Games` descending, then `Score` descending, then `HighGame` descending (all three, matching the existing singles cut-fill in `MigrateTournamentResultsAsync` exactly, and confirmed as intentional — not a bug to fix). Singles: `ComputePlaces` reduces each bowler to their single best qualifying entry before ranking. Team: the reduction happens at the *roster* level instead — a roster's own multiple squad-entries reduce to its single best composite entry (see "Team place-filling algorithm" above) — never a per-bowler reduction summed afterward, which would incorrectly let a member's score from a *different* roster leak into the composite.
- **Forfeited rosters (`Teams.Forfeit`) are excluded from ranking entirely**, and a bowler whose every roster was forfeited is placed last, tied with every other such bowler (see "Team place-filling algorithm" above) — confirmed by the requester via the partner-swap scenario (A/B → A/C, with A/B forfeited: A is placed via the A/C roster; B, having no non-forfeited roster, goes to the shared last place).
- **A bowler with two non-forfeited rosters at once** is asserted by the requester not to happen under tournament rules. `ComputePlaces`' `countingTeamIdByBowlerId` dictionary build (`teamMembers.Where(...).ToDictionary(m => m.BowlerId, ...)`) will throw `ArgumentException` on a duplicate key if it ever does — deliberately not caught/swallowed, since this should surface as a data anomaly requiring a human look, not something the algorithm silently resolves by picking one.
- **More than one `Stats_ResultsStats` row for the same `BowlerId`** in one tournament is unconfirmed-but-possible (see "Multi-entry bowlers" above) — the job detects this via grouping and treats it as a data anomaly: logs at `Error`, skips that bowler's result entirely (does not guess which row is authoritative), and does not fail the rest of the job.
- **`Place` nullable → required.** `Stats_ResultsStats.Place` is `int?`; `TournamentResult.Place`/`AddResult`'s `place` parameter are non-nullable `int`. `ComputePlaces` is the single place this nullable→required conversion happens; any bowler it can't resolve a `Place` for (no `Place` and no qualifying row) is logged and simply not synced this run — never passed a sentinel value.
- **`Payout`/`Points` are never filled/computed here** — per the schema research above, every qualified bowler already has a real `Stats_ResultsStats` row with these values (the Software auto-inserts a placeholder with `Points = EntryPoints`, `Payout = 0` at completion time), so this job only ever reads them, same as the existing singles migration logic.
- **Team detection uses the website's own `Tournament.TournamentType.TeamSize`**, not a legacy-side query — since `CompleteTournament` only runs after `NewTournament` has already linked the tournament (and derived its `TournamentType`), there's no need to re-derive team-ness from `Squads_TeamSquad` the way `NewTournamentSyncJob` had to.
- **A bowler with no qualifying row at all** (no-show, or a bowler who never bowled a game) has no `Score`/`HighGame` of their own to contribute. Singles: simply not placed, logged, skipped. Team: contributes `0` to their roster's composite score for whichever squad is being scored (doesn't block placing the rest of the roster) but is itself still logged individually — if their `Place` was also `null`, they still get their roster's assigned `Place` via `ComputePlaces`' `foreach (var bowlerId in membersByTeamId...)`, consistent with "every non-forfeited-team member who didn't advance gets the same placing" — but they still need a `Bowler.LegacyId` mapping to be synced at all (the unmapped-bowler path is separate and still applies).

## DI Registration — `IValidator<CompleteTournamentRequest>`

Three places, per the standing architecture doc:

1. **Production** — `LegacyConfiguration.cs`'s `AddLegacy()`: `builder.Services.AddScoped<IValidator<CompleteTournamentRequest>, CompleteTournamentRequestValidator>();`
2. **This action's own test file** (`tests/Neba.Api.Tests/Legacy/Tournaments/CompleteTournamentTests.cs`) — register its own validator **plus every other action's validator already registered by the other test files below**, since `MapLegacyGroup()` maps the whole `/legacy` group on first request.
3. **Every existing `/legacy` test file's `InitializeAsync()`** — add `IValidator<CompleteTournamentRequest>` to each of:
   - `tests/Neba.Api.Tests/Legacy/Bowlers/NewBowlerTests.cs`
   - `tests/Neba.Api.Tests/Legacy/Bowlers/UpdateBowlerTests.cs`
   - `tests/Neba.Api.Tests/Legacy/HallOfFame/HallOfFameTests.cs`
   - `tests/Neba.Api.Tests/Legacy/Tournaments/NewTournamentTests.cs`
   - `tests/Neba.Api.Tests/Legacy/Tournaments/SyncSquadScoresTests.cs`
   - `tests/Neba.Api.Tests/Legacy/HealthTests.cs` — confirm at implementation time whether `Health` maps through `MapLegacyGroup()` (if so it needs the addition too; if it's a bare non-grouped health check it doesn't).

Also update `Legacy/LegacyEndpoints.cs`: add `app.MapCompleteTournament();`.

## Tests

One file, `tests/Neba.Api.Tests/Legacy/Tournaments/CompleteTournamentTests.cs`, covering both job classes — per the architecture doc's five layers, with an extra sub-layer (2b) for the job-chaining boundary the two-job split introduces:

1. **Validator** — `TournamentId` `GreaterThan(0)`.
2. **Endpoint + auth (integration, `TestHost` through `MapLegacyGroup()`)** — missing/wrong API key → `401`; invalid body → `400`; valid request → `202` and `IBackgroundJobClient.Enqueue<CompleteTournamentSyncJob>(job => job.SyncAsync(expectedId, ...))` verified via `Mock<IBackgroundJobClient>(MockBehavior.Strict)`.
2b. **`CompleteTournamentSyncJob` chaining (unit, real `AppDbContext`/SQLite or Testcontainers, mocked `IBackgroundJobClient`)** — this job's whole job is "complete, then chain," so it needs its own coverage separate from the endpoint test above: tournament found + not yet complete → `Tournament.Complete` becomes `true`, `SaveChangesAsync` happens, and `IBackgroundJobClient.Enqueue<SyncTournamentResultsJob>(job => job.SyncAsync(legacyTournamentId, ...))` is verified via `Mock<IBackgroundJobClient>(MockBehavior.Strict)`; tournament already complete → the chain still fires (idempotent re-fire, see Decision Recap); tournament not found (no `LegacyId` match) → the chain does **not** fire and the unlinked-tournament email is sent instead — assert `jobs.Verify(j => j.Enqueue<SyncTournamentResultsJob>(It.IsAny<...>()), Times.Never)` for that case specifically, since this is the one path where *not* chaining is the correct behavior.
3. **Mapping logic (unit, no I/O)** — test `SyncTournamentResultsJob.ComputePlaces` directly against constructed `LegacyResultRow`/`LegacyQualifyingRow`/`LegacyTeamRow`/`LegacyTeamMemberRow`/`LegacyTeamSquadRow` lists. This is where the team-merge algorithm gets its real coverage:
   - Singles: some `Place` already set, some missing → missing ones filled by `Games`/`Score`/`HighGame` rank, sequential from `max(Place) + 1`.
   - **Re-entries (singles)**: a bowler with two (or more) `LegacyQualifyingRow`s (different `SquadId`s) → only their highest-`Score` entry is used for ranking; a tie on `Score` between two of a bowler's own entries → the higher-`HighGame` one wins.
   - **Roster composite scoring**: roster A/B with A's squad-1 score + B's squad-1 score (same `SquadId`) → composite is their sum; a *different* roster's or a mismatched-`SquadId` row must never contribute — assert a roster containing a member with a qualifying row on a squad the roster didn't jointly enter does not pull that score in.
   - **Roster re-entry (team)**: the requester's own example — roster A/B qualifies twice (squad 1: A=1000/B=900, composite 1900; squad 2: A=950/B=1200, composite 2150) → the roster's counting entry is squad 2 (2150), and squad 2's `HighGame` travels with it (not squad 1's, not a max of both).
   - **Forfeit exclusion**: a roster with `Forfeit = true` is never ranked and contributes nothing, even if it has the highest composite score of any roster in the tournament.
   - **Partner swap**: replicate the requester's exact scenario — Teams `{A,B}` (forfeited) and `{A,C}` (not forfeited) → A is placed via the `{A,C}` roster's rank; B (no non-forfeited roster) is *not* included in that ranked group.
   - **Shared last place**: two or more bowlers whose only roster(s) were all forfeited → all receive the identical `Place` value (one past the last ranked roster), not individually ranked against each other.
   - **Team, no forfeits**: a roster where every member is missing `Place` → all members get the *same* computed `Place`; two rosters tie on games/score → tie broken by `HighGame`; a roster member missing from `qualifying` entirely still receives the roster's `Place`.
   - Edge case: a bowler missing `Place`, not on any roster at all (data gap) → excluded from the result dictionary entirely (caller logs and skips).
   - Anomaly case (tested at the `SyncAsync`/caller level, not inside `ComputePlaces` itself, since the grouping happens before the call): two `LegacyResultRow`s for the same `BowlerId` → both excluded from `ComputePlaces`' input, logged at `Error`, no exception thrown. Separately, a bowler on two non-forfeited rosters (asserted not to happen, per the requester) → `ComputePlaces` throws `ArgumentException` rather than silently picking one; assert the throw, don't try to make it succeed.
4. **Legacy query correctness (integration, Postgres temp table standing in for `neba-fwk`)** — seed `Stats`/`Stats_ResultsStats`/`Stats_QualifyingStats`/`Teams`/`TeamMember`/`SquadTeams`-shaped temp tables, assert `SyncTournamentResultsJob`'s four Dapper queries return the expected rows for a given legacy tournament id, including a roster with more than one `SquadTeams` row (re-entry) and a forfeited roster.
5. **Idempotency (end-to-end, real Testcontainers `AppDbContext`)** — run `CompleteTournamentSyncJob.SyncAsync` followed by `SyncTournamentResultsJob.SyncAsync` (the real chain, called directly rather than through Hangfire) twice for the same legacy tournament id: first pass creates `TournamentResult` rows and completes the tournament; second pass (tournament already `Complete`) doesn't error, doesn't duplicate rows, and any newly-appeared results (a bowler synced after the first pass) still get added.

## Software Side (`nebamgmt-v3`)

### Hook points

Two call sites, one per format's BO, both immediately after their existing `_repository.Completed(tournamentId)` call succeeds (inside the same `try`/`catch(DatabaseCommitException)` shape the architecture doc's precedent uses):

- `Tournaments/NEBA.Tournaments.BusinessLogic/Team/CompleteTeamTournamentBO.cs:21` (`Execute`), right after `_repository.Completed(tournamentId)`.
- `Tournaments/NEBA.Tournaments.BusinessLogic/Singles/CompleteSinglesTournamentBO.cs`, same point in its equivalent `Execute`.

The shared `TournamentRepository.Completed` method itself is the conceptually cleaner single hook point (both formats already funnel through it), but the non-blocking-warning mechanism the failure philosophy relies on (`SetWarning`/`Errors`) is precedented at the BO layer, not the repository layer — **not confirmed from within this session whether either `CompleteTeamTournamentBO` or `CompleteSinglesTournamentBO` actually exposes that mechanism** (see Open Items). If neither does, adding it (or an equivalent) to both BOs is part of this change, not a pre-existing gotcha to work around.

### Adapter shape

Same deviations from the `HttpPostAdapter` precedent already established for prior backdoor actions (not re-verified fresh for this specific action — carried forward per standing guidance):

- `HttpClient` with an explicit short timeout (a few seconds), not the ~100s default.
- Static/singleton lifetime, independent of the calling form/presenter.
- Dispatched off the UI thread (`Task.Run` or a real async path); the dispatched closure captures only the legacy tournament id (a plain `int`), never `this`/a presenter/a form/any `Control`.
- Non-blocking failure handling via `SetWarning`/`Errors` (see Hook points above); no retry queue on the Software side.
- Abandoning an in-flight call on process exit is accepted, not solved here.

### Prompt for the `nebamgmt-v3` implementation

> Add a call to the website's `/legacy/tournaments/complete` backdoor endpoint whenever a tournament is marked complete in this app. The website mirrors NEBA's tournament data for the eventual retirement of this application; this call tells it a tournament just finished so it can sync final results.
>
> **Two call sites**, both immediately after the existing `_repository.Completed(tournamentId)` call succeeds (never before — the local completion must commit first):
> - `Tournaments/NEBA.Tournaments.BusinessLogic/Team/CompleteTeamTournamentBO.cs`, method `Execute(int tournamentId)`.
> - `Tournaments/NEBA.Tournaments.BusinessLogic/Singles/CompleteSinglesTournamentBO.cs`, its equivalent `Execute`.
>
> **Request**: `POST` to the configured `/legacy` base URL + `/tournaments/complete`, JSON body `{"tournamentId": <the same int tournamentId already in scope>}`, header `X-Api-Key: <configured key>` (same per-environment config pattern as other `App.{Config}.config` settings).
>
> **Adapter**: build a new small adapter parallel to the existing `Adapters/HttpPostAdapter.vb` (`SmartyStreetsAdapter`) — but with these deliberate differences from that precedent, because this call fires on live, frequent completion actions rather than a dormant one-off path:
> - Use `HttpClient` (not `HttpWebRequest`/`WebClient`) with an explicit short timeout (a few seconds) — do not rely on the ~100s default.
> - Give the `HttpClient` a lifetime independent of any calling form/presenter (static/singleton) — never construct-and-dispose it per call.
> - Dispatch the call off the UI thread (`Task.Run` or a real async path) so `Execute` never blocks on the network round-trip. The dispatched closure must capture only the plain `int tournamentId` — never `this`, the BO, a presenter, a form, or any `Control`/`IDisposable`.
> - On failure (network, non-2xx, timeout): do not throw back into `Execute`'s caller. Surface it as a non-blocking warning through whatever `SetWarning`/`Errors` mechanism the BO already exposes (same pattern `HttpPostAdapter`'s callers use). **First confirm whether `CompleteTeamTournamentBO`/`CompleteSinglesTournamentBO` currently expose such a mechanism** — if not, this change needs to add one (or route the warning through whatever the calling presenter already surfaces to the user), rather than silently swallowing the failure.
> - No retry queue — a dropped call is a rare, acceptable loss; the website's Hangfire retry covers transient failures on receipt.
> - Abandoning an in-flight call if the process exits mid-call is an accepted consequence, not something to guard against.
>
> Open questions to resolve or explicitly flag while implementing (do not assume answers):
> - Does `_repository.Completed(tournamentId)` run inside a wider transaction/rollback scope in either BO? If so, the backdoor call must still fire only after that whole scope commits, not just after the inner method call returns.
> - Confirm the exact per-environment config key name(s) to add for the `/legacy` base URL and API key (match whatever naming convention `App.{Config}.config` already uses for similar settings).
> - Confirm whether `CompleteTeamTournamentBO`/`CompleteSinglesTournamentBO` have an existing warning-surfacing mechanism (see above) before assuming one exists.

## Relationship to the eventual `TournamentCompleted` domain event

The website already has real domain-event infrastructure — `Domain/IDomainEvent.cs`, `Domain/AggregateRoot.cs` (`IAggregateRoot.DomainEvents`), `Database/Interceptors/DomainEventDispatcherInterceptor.cs`, `BackgroundJobs/IDomainEventJob.cs` — but no feature raises or handles a domain event yet; `Tournament.CompleteTournament()` today just flips the `Complete` flag and returns. `DomainEventDispatcherInterceptor` runs on every `SaveChangesAsync`: it collects any events an aggregate raised, and for each one, enqueues **one Hangfire job per registered `IDomainEventJob<TEvent>` implementation** — so N handlers subscribed to the same event become N independently-retryable jobs, automatically, with no hand-written `Enqueue` calls.

This plan's `CompleteTournamentSyncJob` → `SyncTournamentResultsJob` chain is a manual simulation of exactly that fan-out, built before the event exists: `CompleteTournamentSyncJob.SyncAsync` flips `Complete`, then hand-writes `jobs.Enqueue<SyncTournamentResultsJob>(...)`. Once a `TournamentCompleted` domain event is added (raised inside `Tournament.CompleteTournament()` itself, following the `AggregateRoot`/`IAggregateRoot.DomainEvents` pattern), the interceptor takes over that fan-out on its own — `SyncTournamentResultsJob` becomes an `IDomainEventJob<TournamentCompleted>` implementation instead of a job manually enqueued by name, and the future season-stats-generation job (see below) becomes a second `IDomainEventJob<TournamentCompleted>` implementation registered alongside it, not another line added to `CompleteTournamentSyncJob`. `CompleteTournamentSyncJob` itself would most likely collapse away entirely at that point — its whole remaining job, "flip `Complete`," is just the aggregate method call that raises the event; the chaining half of its job is superseded by the interceptor.

Until that event exists, this plan's explicit two-hop `Enqueue` chain is the correct shape — it is not a shortcut to be later "fixed," just the manual version of a fan-out the domain-event infrastructure will eventually own outright. This is noted here, rather than acted on, because introducing `TournamentCompleted` is a change to the `Tournament` aggregate itself and to how completion is handled across every caller (this backdoor and any future non-legacy caller alike) — out of scope for a backdoor-only plan.

## Future Work (explicitly out of scope here)

- **Season stats generation as a second job chained from `CompleteTournamentSyncJob`.** Mentioned by the requester as "the upcoming stats generator" — not designed here. When it exists, `CompleteTournamentSyncJob.SyncAsync` gets one more `jobs.Enqueue<...>()` line alongside the existing `SyncTournamentResultsJob` line (both chained from the completion job, not from the endpoint itself — the endpoint only ever enqueues `CompleteTournamentSyncJob`); no other change to this plan's shape is expected.
- **Corrections after first sync.** No update path exists for a `TournamentResult` once recorded (matches `tournament-results.md`'s own deferred "editing a recorded result" item). If the Software's `Place`/`Payout`/`Points` change after this backdoor already synced a bowler, the website will not reflect it until an edit mechanism is designed.
- **Leveraging this same algorithm in `data-migration/Website Port.linq`.** The requester asked about this explicitly. `MigrateTournamentResultsAsync` in that script currently only fills missing `Place` for singles tournaments (`isSinglesTournament` check) and falls through to `Place = -1` for team tournaments — the exact gap this plan's `ComputePlaces` closes. Note the script's existing singles cut-fill logic does **not** yet reduce a bowler's multiple qualifying entries to their best one before ranking (it wasn't written knowing re-entries could produce more than one qualifying row per bowler); porting the algorithm should carry that reduction step over too, not just the team-merge piece. The team piece specifically needs: `Teams.Forfeit` respected (forfeited rosters excluded from ranking and never contribute a score), composite scoring done per `(roster, squad)` rather than per-member, a roster reduced to its single best squad-entry, a bowler placed through at most one non-forfeited roster (querying `TeamMember` to find which), and any bowler with no non-forfeited roster at all placed last, tied with every other such bowler — the full "Team place-filling algorithm" above, not a simplified version of it. The script runs against the website's own already-migrated Postgres database via LINQPad's EFCore dynamic driver (not against `neba-fwk` directly for this step — it queries `neba-fwk` through `QuerySoftwareDatabaseAsync`, same as this plan's job does via Dapper), so the same tables and the same ranking algorithm apply almost verbatim; it cannot literally call into `SyncTournamentResultsJob.ComputePlaces` (that's `internal` to `Neba.Api`, and the script isn't a project reference away from it), so the algorithm would need to be reimplemented in the script by hand, kept in sync manually. This is a separate, follow-up edit to the `.linq` file — out of scope for this markdown-only plan.

## Summary of what's still undecided

1. ~~Endpoint shape: one route vs. per-format routes.~~ **Decided**: one route, `POST /legacy/tournaments/complete`, single trigger payload — both formats resolve to the same request shape (a legacy tournament id) before calling out, per the architecture doc's collapsing rule.
2. ~~Where team-score-merge logic comes from.~~ **Decided, then substantially revised**: an early draft reused `TeamSquadScores.cs`'s "sum each member's own best entry" formula directly — the requester corrected this: team composite scoring is roster-and-squad scoped (sum members' scores *for the same squad*, reduce the roster to its single best composite squad-entry), not a sum of each member's independently-best entry. See "Team place-filling algorithm."
3. ~~Whether `Payout`/`Points` need to be computed/derived for non-advancing bowlers.~~ **Decided, no**: every qualified bowler already has a real `Stats_ResultsStats` row with these values by the time a tournament is `Completed`, confirmed by reading `TournamentRepository.Completed`'s placeholder-insert logic.
4. ~~Forfeited rosters and partner swaps.~~ **Decided**: confirmed by the requester via a concrete partner-swap example (A/B forfeited, A/C counts) — forfeited rosters (`Teams.Forfeit`) are excluded from ranking entirely; a bowler is placed through at most one non-forfeited roster (guaranteed by tournament rules, not just an assumption); a bowler with no non-forfeited roster at all shares one common last place with every other such bowler, untied to any score. See "Team place-filling algorithm."
5. ~~Cut-fill sort order.~~ **Decided**: `Games` descending, then `Score` descending, then `HighGame` descending, confirmed by the requester — matches the existing `MigrateTournamentResultsAsync` sort exactly. An earlier draft of this plan incorrectly flagged that sort as a discrepancy against the `tournament-results.md` SME write-up; that flag is withdrawn.
6. ~~Whether a bowler could end up with more than one `Stats_ResultsStats` row.~~ **Decided, treat as anomaly**: not confirmed the Software can actually produce this, but the job defends against it regardless — detects duplicates by `BowlerId`, logs at `Error`, skips that bowler's result, does not fail the job or guess which row is authoritative.
7. ~~Whether a bowler could end up counted on two non-forfeited rosters at once.~~ **Decided, treat as anomaly**: confirmed by the requester this shouldn't happen under tournament rules. `ComputePlaces` doesn't defensively catch it — the dictionary build throws on a duplicate key, deliberately surfacing it rather than silently choosing one roster.
8. **Whether `CompleteTeamTournamentBO`/`CompleteSinglesTournamentBO` expose a `SetWarning`/`Errors`-style non-blocking failure surface** for the Software-side adapter to use. Could not confirm from within this session — flagged explicitly in the Software-side implementation prompt as something to check before assuming the pattern applies unchanged.
9. **Whether `_repository.Completed(tournamentId)` runs inside a wider transaction scope** in either BO, which would affect exactly where "after the local commit succeeds" falls. Could not confirm from within this session.
10. **Real column/table names against a live `neba-fwk` database** — everything in the Legacy Schema Reference table was confirmed against the EDMX model (both CSDL and SSDL sections), not a live query. Standard caveat per the architecture doc; same as every prior backdoor plan.
11. ~~One job vs. two chained jobs.~~ **Decided**: the requester explicitly asked for `CompleteTournamentSyncJob` to only set the tournament complete and then chain separate background jobs for the rest, rather than doing completion and results-population in one job. An earlier draft of this plan had both in a single `CompleteTournamentSyncJob`; that's now split into `CompleteTournamentSyncJob` (thin, chains) and `SyncTournamentResultsJob` (the actual results work), both still declared in the same `Legacy/Tournaments/CompleteTournament.cs` file since `SyncTournamentResultsJob` has no HTTP route of its own — this file-boundary call was made by the plan, not explicitly requested, and is easy to revise if a separate file is preferred once the season-stats job makes the file larger.
