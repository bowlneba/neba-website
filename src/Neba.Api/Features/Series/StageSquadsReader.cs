using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series;

/// <summary>UTC instant only. The Eastern date and time conversion belongs to the facts builder (backend-api).</summary>
internal sealed record StageSquadRow(SquadId SquadId, DateTimeOffset BowlingDateTimeUtc, bool HasScores);

internal static class StageSquadsReader
{
    /// <summary>Shared by the squad facts reader and the ListStageSquads use case.</summary>
    public static async Task<IReadOnlyList<StageSquadRow>> ReadForTournamentAsync(
        AppDbContext appDbContext, TournamentId tournamentId, CancellationToken cancellationToken)
        => await appDbContext.Tournaments.AsNoTracking()
            .Where(tournament => tournament.Id == tournamentId)
            .SelectMany(tournament => tournament.Squads)
            .OrderBy(squad => squad.BowlingDateTimeUtc)
            .Select(squad => new StageSquadRow(
                squad.Id,
                squad.BowlingDateTimeUtc,
                appDbContext.SquadScores.Any(score => score.SquadId == squad.Id)))
            .ToListAsync(cancellationToken);

    /// <summary>The squads of every tournament in one query, each tournament's squads ordered by time.</summary>
    public static async Task<ILookup<TournamentId, StageSquadRow>> ReadForTournamentsAsync(
        AppDbContext appDbContext, IReadOnlyCollection<TournamentId> tournamentIds, CancellationToken cancellationToken)
    {
        var rows = await appDbContext.Tournaments.AsNoTracking()
            .Where(tournament => tournamentIds.Contains(tournament.Id))
            .SelectMany(tournament => tournament.Squads, (tournament, squad) => new
            {
                TournamentId = tournament.Id,
                SquadId = squad.Id,
                squad.BowlingDateTimeUtc,
                HasScores = appDbContext.SquadScores.Any(score => score.SquadId == squad.Id),
            })
            .OrderBy(x => x.BowlingDateTimeUtc)
            .ToListAsync(cancellationToken);

        return rows.ToLookup(x => x.TournamentId, x => new StageSquadRow(x.SquadId, x.BowlingDateTimeUtc, x.HasScores));
    }

    /// <summary>Null when the Series or Stage does not exist.</summary>
    public static async Task<IReadOnlyList<StageSquadRow>?> ReadForStageAsync(
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
            : await ReadForTournamentAsync(appDbContext, stage.TournamentId, cancellationToken);
    }
}