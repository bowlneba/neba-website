using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series;

internal sealed record StageResultPickRow(BowlerId BowlerId, Name BowlerName, int Place);

internal static class StageResultRowsReader
{
    /// <summary>Null when the Series or Stage does not exist. An empty list means the tournament has no results yet.</summary>
    public static async Task<IReadOnlyList<StageResultPickRow>?> ReadAsync(
        AppDbContext appDbContext, SeriesId seriesId, SeriesStageId stageId, CancellationToken cancellationToken)
    {
        var stage = await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == seriesId)
            .SelectMany(series => series.Stages)
            .Where(s => s.Id == stageId)
            .Select(s => new { s.TournamentId })
            .SingleOrDefaultAsync(cancellationToken);

        return stage is null
            ? null
            : await appDbContext.Tournaments.AsNoTracking()
                .Where(tournament => tournament.Id == stage.TournamentId)
                .SelectMany(tournament => tournament.Results)
                .OrderBy(result => result.Place)
                .ThenBy(result => result.Bowler.Name.LastName)
                .ThenBy(result => result.Bowler.Name.FirstName)
                .Select(result => new StageResultPickRow(result.BowlerId, result.Bowler.Name, result.Place))
                .ToListAsync(cancellationToken);
    }

    /// <summary>The place of every bowler in each of the tournaments, in one query. A tournament with no results has no entry.</summary>
    public static async Task<ILookup<TournamentId, StageResultRow>> ReadForTournamentsAsync(
        AppDbContext appDbContext, IReadOnlyCollection<TournamentId> tournamentIds, CancellationToken cancellationToken)
    {
        var rows = await appDbContext.Tournaments.AsNoTracking()
            .Where(tournament => tournamentIds.Contains(tournament.Id))
            .SelectMany(tournament => tournament.Results, (tournament, result) => new { TournamentId = tournament.Id, result.BowlerId, result.Place })
            .OrderBy(row => row.Place)
            .ToListAsync(cancellationToken);

        return rows.ToLookup(row => row.TournamentId, row => new StageResultRow(row.BowlerId, row.Place));
    }
}