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