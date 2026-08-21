using System.Data;

using Dapper;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Email;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Stats.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Identity;

using ZiggyCreatures.Caching.Fusion;

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
    IFusionCache cache,
    IEmailSender emailSender,
    ILogger<GenerateSeasonStatsJob> logger)
{
    // Placeholders are numbered and bound individually (Id0, Id1, ...) rather than relying on
    // Dapper's automatic "IN @Ids" list expansion - see HallOfFame.cs's NewHallOfFameInductionSyncJob
    // for the full rationale: Dapper detects when the provider natively supports array parameters
    // (Npgsql does, the real neba-fwk SQL Server provider does not) and binds the whole collection as
    // a single native array parameter when it can, making "IN @Ids" provider-dependent - a syntax
    // error against the Postgres connection this test suite uses as a neba-fwk stand-in.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2077:Use a parameterized query instead of string formatting.",
        Justification = "The interpolated segment is only generated placeholder names (@Id0, @Id1, ...), never a data value - every id value itself is bound as a real DynamicParameters entry, not concatenated into the SQL text.")]
    private static (DynamicParameters Parameters, string Placeholders) BuildInClauseParameters(string parameterPrefix, List<int> values)
    {
        var parameters = new DynamicParameters();
        var placeholders = new string[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            var name = $"{parameterPrefix}{i}";
            parameters.Add(name, values[i]);
            placeholders[i] = "@" + name;
        }

        return (parameters, string.Join(",", placeholders));
    }

    public async Task SyncAsync(int legacyTournamentId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var tournament = await db.Set<Tournament>()
            .SingleOrDefaultAsync(t => t.LegacyId == legacyTournamentId, ct);
        if (tournament is null)
        {
            await NotifyUnlinkedTournamentAsync(legacyTournamentId, ct);
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

        var seasonTournaments = await FetchSeasonTournamentsAsync(seasonStart, seasonEndExclusive);
        var tournamentIds = seasonTournaments.ConvertAll(t => t.TournamentId);

        var (qualifyingStats, matchPlayStats, sideCutRows) = await FetchTournamentStatsAsync(tournamentIds);

        var bowlerLegacyIdById = await db.Bowlers
            .Where(bowler => bowler.LegacyId != null)
            .ToDictionaryAsync(bowler => bowler.Id, bowler => bowler.LegacyId!.Value, ct);

        var results = BuildLegacyBowlerResults(websiteTournaments, bowlerLegacyIdById, sideCutRows);

        var legacyBowlerIds = qualifyingStats.Select(q => q.BowlerId)
            .Concat(matchPlayStats.Select(m => m.BowlerId))
            .Concat(results.Select(r => r.BowlerId))
            .Distinct()
            .ToList();

        var (bowlerRows, newMembershipTypeId, membershipRows, creditRows, cupResultRows) =
            await FetchBowlerStatsAsync(legacyBowlerIds, seasonStart, seasonEndExclusive);

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

        var unmappedLegacyBowlerIds = await AddBowlerSeasonStatsAsync(season.Id, computedResults, bowlerIdByLegacyId, ct);

        await db.SaveChangesAsync(ct);

        await cache.RemoveByTagAsync($"neba:stats:seasons:{season.Id}", token: ct);

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

    private async Task NotifyUnlinkedTournamentAsync(int legacyTournamentId, CancellationToken ct)
    {
        logger.LogLegacyTournamentNotSyncedForStatsGeneration(legacyTournamentId);

        await emailSender.SendAsync(new EmailMessage
        {
            To = "website@bowlneba.com",
            Subject = "Manual intervention needed: season stats generation with no linked tournament",
            HtmlBody = new UnlinkedTournamentStatsEmail(legacyTournamentId).ToHtmlBody()
        }, ct);
    }

    // See NewBowlerSyncJob.SyncAsync for the rationale on suppressing DAP005 here.
#pragma warning disable DAP005
    private async Task<List<LegacySeasonTournamentRow>> FetchSeasonTournamentsAsync(DateTime seasonStart, DateTime seasonEndExclusive)
        => [.. (await legacyConnection.QueryAsync<LegacySeasonTournamentRow>(
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
            new { SeasonStart = seasonStart, SeasonEndExclusive = seasonEndExclusive }))];

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2077:Use a parameterized query instead of string formatting.",
        Justification = "The interpolated segments below are only generated placeholder names (@TournamentId0, ...) from BuildInClauseParameters, never a data value - every id value itself is bound as a real DynamicParameters entry, not concatenated into the SQL text.")]
    private async Task<(List<LegacyQualifyingStatsRow> Qualifying, List<LegacyMatchPlayStatsRow> MatchPlay, List<LegacySideCutRow> SideCuts)>
        FetchTournamentStatsAsync(List<int> tournamentIds)
    {
        if (tournamentIds.Count == 0)
        {
            return ([], [], []);
        }

        var (parameters, placeholders) = BuildInClauseParameters("TournamentId", tournamentIds);

        var qualifyingStats = (await legacyConnection.QueryAsync<LegacyQualifyingStatsRow>(
            $"""
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
                s.TournamentId IN ({placeholders})
            """,
            parameters)).ToList();

        var matchPlayStats = (await legacyConnection.QueryAsync<LegacyMatchPlayStatsRow>(
            $"""
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
                s.TournamentId IN ({placeholders})
            """,
            parameters)).ToList();

        var sideCutRows = (await legacyConnection.QueryAsync<LegacySideCutRow>(
            $"""
            SELECT
                s.BowlerId,
                s.TournamentId,
                r.SideCut
            FROM
                Stats s
            INNER JOIN Stats_ResultsStats r ON s.Id = r.Id
            WHERE
                s.TournamentId IN ({placeholders})
            """,
            parameters)).ToList();

        return (qualifyingStats, matchPlayStats, sideCutRows);
    }
#pragma warning restore DAP005

    // Stats_ResultsStats is only ever meant to hold one row per (BowlerId, TournamentId), but
    // that's convention, not a schema constraint - same caveat SyncTournamentResultsJob already
    // defends against for the Place/Payout/Points read. An anomalous duplicate here would crash
    // an unguarded ToDictionary, so log and skip it instead of failing the whole season regenerate.
    private List<LegacyBowlerResultRow> BuildLegacyBowlerResults(
        List<Tournament> websiteTournaments,
        Dictionary<BowlerId, int> bowlerLegacyIdById,
        List<LegacySideCutRow> sideCutRows)
    {
        var sideCutRowsByKey = sideCutRows.GroupBy(r => (r.BowlerId, r.TournamentId)).ToList();
        foreach (var anomalyKey in sideCutRowsByKey.Where(g => g.Count() > 1).Select(g => g.Key))
        {
            logger.LogLegacyBowlerHasMultipleSideCutRows(anomalyKey.BowlerId, anomalyKey.TournamentId);
        }

        var sideCutByBowlerAndTournament = sideCutRowsByKey
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.Single().SideCut);

        var results = new List<LegacyBowlerResultRow>();

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

        return results;
    }

#pragma warning disable DAP005
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2077:Use a parameterized query instead of string formatting.",
        Justification = "The interpolated segments below are only generated placeholder names (@BowlerId0, ...) from BuildInClauseParameters, never a data value - every id value itself is bound as a real DynamicParameters entry, not concatenated into the SQL text.")]
    private async Task<(List<LegacyBowlerRow> Bowlers, int NewMembershipTypeId, List<LegacyMembershipRow> Memberships, List<LegacyCreditRow> Credits, List<LegacyCupResultRow> CupResults)>
        FetchBowlerStatsAsync(List<int> legacyBowlerIds, DateTime seasonStart, DateTime seasonEndExclusive)
    {
        var newMembershipTypeId = await legacyConnection.QuerySingleAsync<int>(
            """
            SELECT Id FROM Memberships WHERE Name LIKE '%New Member%'
            """);

        if (legacyBowlerIds.Count == 0)
        {
            return ([], newMembershipTypeId, [], [], []);
        }

        var (parameters, placeholders) = BuildInClauseParameters("BowlerId", legacyBowlerIds);

        var bowlerRows = (await legacyConnection.QueryAsync<LegacyBowlerRow>(
            $"""
            SELECT
                Id AS BowlerId,
                Gender,
                DateOfBirth
            FROM
                Bowlers
            WHERE
                Id IN ({placeholders})
            """,
            parameters)).ToList();

        var membershipRows = (await legacyConnection.QueryAsync<LegacyMembershipRow>(
            $"""
            SELECT
                bm.BowlerId,
                bm.MembershipId,
                bm.EndDate
            FROM
                BowlerMemberships bm
            WHERE
                bm.BowlerId IN ({placeholders})
            """,
            parameters)).ToList();

        var creditParameters = BuildInClauseParameters("BowlerId", legacyBowlerIds);
        creditParameters.Parameters.Add("SeasonStart", seasonStart);
        creditParameters.Parameters.Add("SeasonEndExclusive", seasonEndExclusive);
        var creditRows = (await legacyConnection.QueryAsync<LegacyCreditRow>(
            $"""
            SELECT
                bc.BowlerId,
                c.Amount
            FROM
                Credits c
            INNER JOIN Credits_BowlerCredit bc ON c.Id = bc.Id
            WHERE
                bc.BowlerId IN ({creditParameters.Placeholders}) AND bc.Taxable = 1
                AND c.IssuedDate >= @SeasonStart AND c.IssuedDate < @SeasonEndExclusive
            """,
            creditParameters.Parameters)).ToList();

        var cupResultRows = (await legacyConnection.QueryAsync<LegacyCupResultRow>(
            $"""
            SELECT
                cr.BowlerId,
                cr.Payout,
                cu.End AS CupEnd
            FROM
                CupResults cr
            INNER JOIN Cups cu ON cr.CupId = cu.Id
            WHERE
                cr.BowlerId IN ({placeholders})
            """,
            parameters)).ToList();

        return (bowlerRows, newMembershipTypeId, membershipRows, creditRows, cupResultRows);
    }
#pragma warning restore DAP005

    private async Task<List<int>> AddBowlerSeasonStatsAsync(
        SeasonId seasonId,
        IReadOnlyCollection<LegacyBowlerSeasonStatsResult> computedResults,
        Dictionary<int, BowlerId> bowlerIdByLegacyId,
        CancellationToken ct)
    {
        var unmappedLegacyBowlerIds = new List<int>();

        foreach (var result in computedResults)
        {
            if (!bowlerIdByLegacyId.TryGetValue(result.BowlerId, out var bowlerId))
            {
                logger.LogLegacyBowlerNotSyncedForStatsGeneration(result.BowlerId, seasonId);
                unmappedLegacyBowlerIds.Add(result.BowlerId);
                continue;
            }

            await db.BowlerSeasonStats.AddAsync(new BowlerSeasonStats
            {
                SeasonId = seasonId,
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

        return unmappedLegacyBowlerIds;
    }
}