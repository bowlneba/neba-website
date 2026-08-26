using System.Data;

using Dapper;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Email;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Identity;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.Legacy.Tournaments.Complete;

// Does the actual TournamentResult population (via TournamentPlaceCalculator for any bowler
// missing a Place) - split from CompleteTournamentSyncJob so it's its own independently-retryable
// job, per the two-jobs-not-one decision (see the plan).
internal sealed class SyncTournamentResultsJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    IFusionCache cache,
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
            teamRows = [.. (await legacyConnection.QueryAsync<LegacyTeamRow>(
                """
                SELECT
                    t.Id AS TeamId,
                    t.Forfeit
                FROM
                    Teams t
                WHERE
                    t.TeamTournamentId = @TournamentId
                """,
                new { TournamentId = legacyTournamentId }))];

            teamMemberRows = [.. (await legacyConnection.QueryAsync<LegacyTeamMemberRow>(
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
                new { TournamentId = legacyTournamentId }))];

            // One row per (roster, squad) - a roster that re-entered the same squad grouping
            // more than once (no partner change) has more than one row here.
            teamSquadRows = [.. (await legacyConnection.QueryAsync<LegacyTeamSquadRow>(
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
                new { TournamentId = legacyTournamentId }))];
        }
#pragma warning restore DAP005

        // Stats_ResultsStats is only ever meant to hold one row per BowlerId per tournament, but
        // that's convention, not a schema constraint - if it happens, treat it as a data anomaly
        // worth a human look rather than silently picking one arbitrarily.
        var resultRowsByBowlerId = resultRows.GroupBy(r => r.BowlerId).ToList();
        var anomalousLegacyBowlerIds = resultRowsByBowlerId.Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        foreach (var legacyBowlerId in anomalousLegacyBowlerIds)
        {
            logger.LogLegacyBowlerHasMultipleResultRows(legacyBowlerId, legacyTournamentId);
        }

        var singleResultRows = resultRowsByBowlerId.Where(g => g.Count() == 1).Select(g => g.Single()).ToList();

        var placeByLegacyBowlerId = TournamentPlaceCalculator.ComputePlaces(singleResultRows, qualifyingRows, teamRows, teamMemberRows, teamSquadRows);

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
                // no-show with no qualifying stats at all) - can't be placed. Logged and
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

        // Place == 1 results feed the champions list (ListChampionsQueryHandler) directly, in
        // addition to the tournament's own detail view.
        await cache.RemoveByTagAsync($"neba:tournaments:{tournament.Id}", token: ct);
        await cache.RemoveByTagAsync("neba:tournaments:champions", token: ct);

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
}