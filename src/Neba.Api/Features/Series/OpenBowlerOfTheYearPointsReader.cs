using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series;

internal sealed record BowlerOfTheYearPointsRow(BowlerId BowlerId, int Points);

/// <summary>Points as of a date. <paramref name="IsReady"/> is false while a stats-eligible tournament ending on or before the date is still Scheduled or finalized without results.</summary>
/// <param name="IsReady">False while the data is incomplete.</param>
/// <param name="Points">One row per bowler with points above zero, highest first.</param>
internal sealed record OpenBowlerOfTheYearPoints(bool IsReady, IReadOnlyList<BowlerOfTheYearPointsRow> Points);

/// <summary>
/// Open Bowler of the Year points as of a date, from website results. Mirrors the Bowler of the Year part of
/// LegacySeasonStatsCalculator.ComputeAwardPoints. Senior, Super Senior, Woman and Youth points are not needed.
/// </summary>
internal static class OpenBowlerOfTheYearPointsReader
{
    public static async Task<OpenBowlerOfTheYearPoints> ReadAsync(
        AppDbContext appDbContext, SeasonId seasonId, DateOnly asOf, CancellationToken cancellationToken)
    {
        var eligibleToDate = appDbContext.Tournaments.AsNoTracking()
            .Where(tournament => tournament.SeasonId == seasonId
                && tournament.StatsEligible
                && tournament.EndDate <= asOf);

        // About a dozen rows: one query tells both whether a tournament is still Scheduled and whether a finalized one has no results.
        var eligibleRows = await eligibleToDate
            .Select(tournament => new { tournament.Status, HasResults = tournament.Results.Any() })
            .ToListAsync(cancellationToken);

        var stillOpen = eligibleRows.Any(row => row.Status == TournamentStatus.Scheduled);

        var finalizedWithoutResults = eligibleRows.Any(row =>
            (row.Status == TournamentStatus.Completed || row.Status == TournamentStatus.Truncated) && !row.HasResults);

        // Tournament of Champions double-dip: the Non-Champions single-day winner's forced berth in the
        // Tournament of Champions earns no Bowler of the Year points. Identify both from every finalized
        // tournament in the season up to the date, not only stats-eligible ones, as the legacy code does.
        var doubleDipCandidates = await appDbContext.Tournaments.AsNoTracking()
            .Where(tournament => tournament.SeasonId == seasonId
                && tournament.EndDate <= asOf
                && (tournament.Status == TournamentStatus.Completed || tournament.Status == TournamentStatus.Truncated)
                && ((tournament.TournamentType == TournamentType.NonChampions && tournament.StartDate == tournament.EndDate)
                    || tournament.TournamentType == TournamentType.TournamentOfChampions))
            .Select(tournament => new { tournament.Id, tournament.TournamentType })
            .ToListAsync(cancellationToken);

        var nonChampionsIds = doubleDipCandidates
            .Where(tournament => tournament.TournamentType == TournamentType.NonChampions)
            .Select(tournament => tournament.Id)
            .ToList();

        var championsIds = doubleDipCandidates
            .Where(tournament => tournament.TournamentType == TournamentType.TournamentOfChampions)
            .Select(tournament => tournament.Id)
            .ToList();

        var excludedTournamentId = default(TournamentId);
        List<BowlerId> excludedBowlerIds = [];

        if (nonChampionsIds.Count == 1 && championsIds.Count == 1)
        {
            excludedTournamentId = championsIds[0];
            var nonChampionsId = nonChampionsIds[0];

            excludedBowlerIds = await appDbContext.Tournaments.AsNoTracking()
                .Where(tournament => tournament.Id == nonChampionsId)
                .SelectMany(tournament => tournament.Results)
                .Where(result => result.Place == 1)
                .Select(result => result.BowlerId)
                .ToListAsync(cancellationToken);
        }

        // An empty excludedBowlerIds makes the NOT clause true for every row, so no exclusion applies.
        // EF cannot translate the group and sum after a SelectMany with a result selector, so the rows
        // (about 100 bowlers over about 11 tournaments) come back and the sum runs here.
        var rows = await eligibleToDate
            .Where(tournament => tournament.Status == TournamentStatus.Completed || tournament.Status == TournamentStatus.Truncated)
            .SelectMany(tournament => tournament.Results, (tournament, result) => new { TournamentId = tournament.Id, result.BowlerId, result.Points })
            .Where(row => row.TournamentId != excludedTournamentId || !excludedBowlerIds.Contains(row.BowlerId))
            .ToListAsync(cancellationToken);

        var points = rows
            .GroupBy(row => row.BowlerId)
            .Select(group => new BowlerOfTheYearPointsRow(group.Key, group.Sum(row => row.Points)))
            .Where(row => row.Points > 0)
            .OrderByDescending(row => row.Points)
            .ThenBy(row => row.BowlerId)
            .ToList();

        return new OpenBowlerOfTheYearPoints(!stillOpen && !finalizedWithoutResults, points);
    }
}