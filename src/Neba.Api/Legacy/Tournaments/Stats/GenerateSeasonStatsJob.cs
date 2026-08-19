using System.Data;

using Dapper;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

using Neba.Api.Caching;
using Neba.Api.Database;
using Neba.Api.Email;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Stats.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Identity;

namespace Neba.Api.Legacy.Tournaments.Stats;

// Regenerates the *entire season* a tournament belongs to, not just that tournament - delete every
// existing bowler_season_stats row for the season, then recompute from scratch, "as if the rows
// never existed." See the plan's Decision Recap for why this is season-scoped rather than
// tournament-scoped, and why Place/PrizeMoney/Points come from the website's own TournamentResult
// (already synced by SyncTournamentResultsJob, which this job is always chained after) rather than
// raw legacy Stats_ResultsStats.
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

            await emailSender.SendAsync(new EmailMessage
            {
                To = "website@bowlneba.com",
                Subject = "Manual intervention needed: season stats generation with no linked tournament",
                HtmlBody = new UnlinkedTournamentStatsEmail(legacyTournamentId).ToHtmlBody()
            }, ct);

            return;
        }

        var season = await db.Seasons.SingleAsync(s => s.Id == tournament.SeasonId, ct);

        // Half-open interval: Season.StartDate/EndDate are DateOnly, legacy Tournaments.Start/End are datetime.
        var seasonStart = season.StartDate.ToDateTime(TimeOnly.MinValue);
        var seasonEndExclusive = season.EndDate.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var existing = await db.BowlerSeasonStats.Where(s => s.SeasonId == season.Id).ToListAsync(ct);
        db.BowlerSeasonStats.RemoveRange(existing);

        var websiteTournaments = await db.Set<Tournament>()
            .Include(t => t.Results)
            .Where(t => t.SeasonId == season.Id && t.LegacyId != null)
            .ToListAsync(ct);

        // See NewBowlerSyncJob.SyncAsync for the rationale on suppressing DAP005 here.
#pragma warning disable DAP005
        var seasonTournaments = (await legacyConnection.QueryAsync<LegacySeasonTournamentRow>(
            """
            SELECT
                t.Id AS TournamentId,
                t.Start,
                t.End,
                t.YearlyStatEligible,
                st.TournamentType AS SinglesTournamentType
            FROM
                Tournaments t
            LEFT JOIN Tournaments_SinglesTournament st ON st.Id = t.Id
            WHERE
                t.Start >= @SeasonStart AND t.End < @SeasonEndExclusive
            """,
            new { SeasonStart = seasonStart, SeasonEndExclusive = seasonEndExclusive })).ToList();

        var tournamentIds = seasonTournaments.Select(t => t.TournamentId).ToList();

        var qualifyingStats = (await legacyConnection.QueryAsync<LegacyQualifyingStatsRow>(
            """
            SELECT
                s.BowlerId,
                s.TournamentId,
                q.SquadId,
                q.Score,
                q.Games,
                q.HighGame
            FROM
                Stats s
            INNER JOIN Stats_QualifyingStats q ON s.Id = q.Id
            WHERE
                s.TournamentId IN @TournamentIds
            """,
            new { TournamentIds = tournamentIds })).ToList();

        var matchPlayStats = (await legacyConnection.QueryAsync<LegacyMatchPlayStatsRow>(
            """
            SELECT
                s.BowlerId,
                s.TournamentId,
                m.Score,
                m.Games,
                m.HighGame,
                m.Winner
            FROM
                Stats s
            INNER JOIN Stats_MatchPlayStats m ON s.Id = m.Id
            WHERE
                s.TournamentId IN @TournamentIds
            """,
            new { TournamentIds = tournamentIds })).ToList();

        var sideCutRows = (await legacyConnection.QueryAsync<LegacySideCutRow>(
            """
            SELECT
                s.BowlerId,
                s.TournamentId,
                r.SideCut
            FROM
                Stats s
            INNER JOIN Stats_ResultsStats r ON s.Id = r.Id
            WHERE
                s.TournamentId IN @TournamentIds
            """,
            new { TournamentIds = tournamentIds })).ToList();
#pragma warning restore DAP005

        var bowlerLegacyIdById = await db.Bowlers
            .Where(bowler => bowler.LegacyId != null)
            .ToDictionaryAsync(bowler => bowler.Id, bowler => bowler.LegacyId!.Value, ct);

        var results = new List<LegacyBowlerResultRow>();
        var sideCutByBowlerAndTournament = sideCutRows.ToDictionary(r => (r.BowlerId, r.TournamentId), r => r.SideCut);

        foreach (var websiteTournament in websiteTournaments)
        {
            if (websiteTournament.LegacyId is not { } legacyId)
            {
                continue;
            }

            foreach (var result in websiteTournament.Results)
            {
                if (!bowlerLegacyIdById.TryGetValue(result.BowlerId, out var legacyBowlerId))
                {
                    continue;
                }

                sideCutByBowlerAndTournament.TryGetValue((legacyBowlerId, legacyId), out var sideCut);

                results.Add(new LegacyBowlerResultRow(legacyBowlerId, legacyId, result.Place, result.PrizeMoney, result.Points, sideCut));
            }
        }

        var legacyBowlerIds = qualifyingStats.Select(q => q.BowlerId)
            .Concat(matchPlayStats.Select(m => m.BowlerId))
            .Concat(results.Select(r => r.BowlerId))
            .Distinct()
            .ToList();

#pragma warning disable DAP005
        var bowlerRows = legacyBowlerIds.Count == 0
            ? []
            : (await legacyConnection.QueryAsync<LegacyBowlerRow>(
                """
                SELECT
                    Id AS BowlerId,
                    Gender,
                    DateOfBirth
                FROM
                    Bowlers
                WHERE
                    Id IN @BowlerIds
                """,
                new { BowlerIds = legacyBowlerIds })).ToList();

        var newMembershipTypeId = await legacyConnection.QuerySingleAsync<int>(
            """
            SELECT Id FROM Memberships WHERE Name LIKE '%New Member%'
            """);

        var membershipRows = legacyBowlerIds.Count == 0
            ? []
            : (await legacyConnection.QueryAsync<LegacyMembershipRow>(
                """
                SELECT
                    bm.BowlerId,
                    bm.MembershipId,
                    bm.EndDate
                FROM
                    BowlerMemberships bm
                WHERE
                    bm.BowlerId IN @BowlerIds
                """,
                new { BowlerIds = legacyBowlerIds })).ToList();

        var creditRows = legacyBowlerIds.Count == 0
            ? []
            : (await legacyConnection.QueryAsync<LegacyCreditRow>(
                """
                SELECT
                    bc.BowlerId,
                    c.Amount
                FROM
                    Credits c
                INNER JOIN Credits_BowlerCredit bc ON c.Id = bc.Id
                WHERE
                    bc.BowlerId IN @BowlerIds AND bc.Taxable = 1
                    AND c.IssuedDate >= @SeasonStart AND c.IssuedDate < @SeasonEndExclusive
                """,
                new { BowlerIds = legacyBowlerIds, SeasonStart = seasonStart, SeasonEndExclusive = seasonEndExclusive })).ToList();

        var cupResultRows = legacyBowlerIds.Count == 0
            ? []
            : (await legacyConnection.QueryAsync<LegacyCupResultRow>(
                """
                SELECT
                    cr.BowlerId,
                    cr.Payout,
                    cu.End AS CupEnd
                FROM
                    CupResults cr
                INNER JOIN Cups cu ON cr.CupId = cu.Id
                WHERE
                    cr.BowlerId IN @BowlerIds
                """,
                new { BowlerIds = legacyBowlerIds })).ToList();
#pragma warning restore DAP005

        var computedResults = LegacySeasonStatsCalculator.Compute(
            season.EndDate,
            newMembershipTypeId,
            seasonTournaments,
            qualifyingStats,
            matchPlayStats,
            results,
            bowlerRows,
            membershipRows,
            creditRows,
            cupResultRows);

        var bowlerIdByLegacyId = await db.Bowlers
            .Where(bowler => bowler.LegacyId != null && legacyBowlerIds.Contains(bowler.LegacyId.Value))
            .ToDictionaryAsync(bowler => bowler.LegacyId!.Value, bowler => bowler.Id, ct);

        var unmappedLegacyBowlerIds = new List<int>();

        foreach (var result in computedResults)
        {
            if (!bowlerIdByLegacyId.TryGetValue(result.BowlerId, out var bowlerId))
            {
                logger.LogLegacyBowlerNotSyncedForStatsGeneration(result.BowlerId, season.Id);
                unmappedLegacyBowlerIds.Add(result.BowlerId);
                continue;
            }

            await db.BowlerSeasonStats.AddAsync(new BowlerSeasonStats
            {
                SeasonId = season.Id,
                BowlerId = bowlerId,
                IsMember = result.IsMember,
                IsRookie = result.IsRookie,
                IsSenior = result.IsSenior,
                IsSuperSenior = result.IsSuperSenior,
                IsWoman = result.IsWoman,
                IsYouth = result.IsYouth,
                EligibleTournaments = result.EligibleTournaments,
                TotalTournaments = result.TotalTournaments,
                EligibleEntries = result.EligibleEntries,
                TotalEntries = result.TotalEntries,
                Cashes = result.Cashes,
                Finals = result.Finals,
                TotalGames = result.TotalGames,
                TotalPinfall = result.TotalPinfall,
                FieldAverage = result.FieldAverage,
                QualifyingHighGame = result.QualifyingHighGame,
                HighBlock = result.HighBlock,
                HighFinish = result.HighFinish,
                AverageFinish = result.AverageFinish,
                MatchPlayWins = result.MatchPlayWins,
                MatchPlayLosses = result.MatchPlayLosses,
                MatchPlayGames = result.MatchPlayGames,
                MatchPlayPinfall = result.MatchPlayPinfall,
                MatchPlayHighGame = result.MatchPlayHighGame,
                BowlerOfTheYearPoints = result.BowlerOfTheYearPoints,
                SeniorOfTheYearPoints = result.SeniorOfTheYearPoints,
                SuperSeniorOfTheYearPoints = result.SuperSeniorOfTheYearPoints,
                WomanOfTheYearPoints = result.WomanOfTheYearPoints,
                YouthOfTheYearPoints = result.YouthOfTheYearPoints,
                TournamentWinnings = result.TournamentWinnings,
                CupEarnings = result.CupEarnings,
                Credits = result.Credits,
                LastUpdatedUtc = result.LastUpdatedUtc
            }, ct);
        }

        await db.SaveChangesAsync(ct);

        var cacheDescriptor = CacheDescriptors.Stats.BowlerSeasonStats(season.Id);
        await cache.RemoveByTagAsync(cacheDescriptor.Tags.Last(), ct);

        if (unmappedLegacyBowlerIds.Count > 0)
        {
            await emailSender.SendAsync(new EmailMessage
            {
                To = "website@bowlneba.com",
                Subject = "Manual intervention needed: unsynced bowler(s) in season stats generation",
                HtmlBody = new UnmappedBowlerStatsEmail(season.Id, unmappedLegacyBowlerIds).ToHtmlBody()
            }, ct);
        }
    }
}
