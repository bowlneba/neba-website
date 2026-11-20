using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Features.Series;

internal sealed class SeriesAdvancementFactsReader(AppDbContext appDbContext)
    : ISeriesAdvancementFactsReader
{
    public async Task<AdvancementFacts> ReadAsync(SeriesAggregate series, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(series);

        var stages = series.Stages.OrderBy(stage => stage.Order).ToList();
        var tournamentIds = stages.Select(stage => stage.TournamentId).Distinct().ToList();

        var tournamentRows = await appDbContext.Tournaments.AsNoTracking()
            .Where(tournament => tournamentIds.Contains(tournament.Id))
            .Select(tournament => new { tournament.Id, tournament.Status, tournament.StartDate, tournament.EndDate })
            .ToDictionaryAsync(tournament => tournament.Id, cancellationToken);

        var tournaments = new Dictionary<TournamentId, StageTournamentResults>();
        foreach (var stage in stages)
        {
            if (tournaments.ContainsKey(stage.TournamentId) || !tournamentRows.TryGetValue(stage.TournamentId, out var row))
            {
                continue;
            }

            var finalized = row.Status == TournamentStatus.Completed || row.Status == TournamentStatus.Truncated;

            tournaments[stage.TournamentId] = new StageTournamentResults
            {
                TournamentId = stage.TournamentId,
                Status = row.Status,
                StartDate = row.StartDate,
                EndDate = row.EndDate,
                Results = finalized ? await ReadResultsAsync(series.Id, stage.Id, cancellationToken) : [],
                Squads = stage.Kind == StageKind.Survivor
                    ? await ReadSquadsAsync(stage.TournamentId, cancellationToken)
                    : [],
            };
        }

        var qualifyingDates = stages
            .Where(stage => stage.Kind == StageKind.Qualifying)
            .Select(stage => tournaments.GetValueOrDefault(stage.TournamentId))
            .OfType<StageTournamentResults>()
            .Select(result => result.EndDate)
            .Distinct()
            .ToList();

        var membersOnDate = new Dictionary<DateOnly, IReadOnlySet<BowlerId>>();
        foreach (var date in qualifyingDates)
        {
            membersOnDate[date] = await MembersOnDateReader.ReadAsync(appDbContext, date, cancellationToken);
        }

        var backfill = await ReadBackfillAsync(series, tournaments, cancellationToken);

        var sortNames = await ReadSortNamesAsync(series, tournaments.Values, backfill, cancellationToken);

        return new AdvancementFacts
        {
            Tournaments = tournaments,
            MembersOnDate = membersOnDate,
            Backfill = backfill,
            SortNames = sortNames,
        };
    }

    private async Task<IReadOnlyList<StageResultRow>> ReadResultsAsync(
        SeriesId seriesId, SeriesStageId stageId, CancellationToken cancellationToken)
    {
        var rows = await StageResultRowsReader.ReadAsync(appDbContext, seriesId, stageId, cancellationToken);

        return rows is null ? [] : [.. rows.Select(row => new StageResultRow(row.BowlerId, row.Place))];
    }

    private async Task<IReadOnlyList<StageSquadFact>> ReadSquadsAsync(TournamentId tournamentId, CancellationToken cancellationToken)
    {
        var squads = await StageSquadsReader.ReadForTournamentAsync(appDbContext, tournamentId, cancellationToken);
        if (squads.Count == 0)
        {
            return [];
        }

        var squadIds = squads.Select(squad => squad.SquadId).ToList();

        var totals = await appDbContext.SquadScores.AsNoTracking()
            .Where(score => squadIds.Contains(score.SquadId))
            .GroupBy(score => new { score.SquadId, score.BowlerId })
            .Select(group => new { group.Key.SquadId, group.Key.BowlerId, Total = group.Sum(score => score.Score), Games = group.Count() })
            .ToListAsync(cancellationToken);

        return [.. squads.Select(squad =>
        {
            // Squad times are stored in UTC; the Series designation compares Eastern wall-clock time.
            var (date, time) = EasternTime.ToLocal(squad.BowlingDateTimeUtc);

            return new StageSquadFact(
                squad.SquadId,
                date,
                time,
                [.. totals
                    .Where(total => total.SquadId == squad.SquadId)
                    .Select(total => new SquadBowlerTotal(total.BowlerId, total.Total, total.Games))]);
        })];
    }

    private async Task<BackfillFacts?> ReadBackfillAsync(
        SeriesAggregate series,
        Dictionary<TournamentId, StageTournamentResults> tournaments,
        CancellationToken cancellationToken)
    {
        var lastQualifying = series.LastQualifyingStage;
        if (lastQualifying is null || !tournaments.TryGetValue(lastQualifying.TournamentId, out var tournament))
        {
            return null;
        }

        var asOf = tournament.EndDate;
        var points = await OpenBowlerOfTheYearPointsReader.ReadAsync(appDbContext, series.SeasonId, asOf, cancellationToken);

        return new BackfillFacts(
            asOf,
            points.IsReady,
            [.. points.Points.Select(row => new BowlerPointsFact(row.BowlerId, row.Points))]);
    }

    private async Task<IReadOnlyDictionary<BowlerId, BowlerSortName>> ReadSortNamesAsync(
        SeriesAggregate series,
        IEnumerable<StageTournamentResults> tournaments,
        BackfillFacts? backfill,
        CancellationToken cancellationToken)
    {
        var bowlerIds = tournaments.SelectMany(tournament => tournament.Results.Select(result => result.BowlerId))
            .Concat(backfill?.Points.Select(point => point.BowlerId) ?? [])
            .Concat(series.FieldEntries.Select(entry => entry.BowlerId))
            .Distinct()
            .ToList();

        if (bowlerIds.Count == 0)
        {
            return new Dictionary<BowlerId, BowlerSortName>();
        }

        return await appDbContext.Bowlers.AsNoTracking()
            .Where(bowler => bowlerIds.Contains(bowler.Id))
            .Select(bowler => new { bowler.Id, bowler.Name.LastName, bowler.Name.FirstName })
            .ToDictionaryAsync(bowler => bowler.Id, bowler => new BowlerSortName(bowler.LastName, bowler.FirstName), cancellationToken);
    }
}