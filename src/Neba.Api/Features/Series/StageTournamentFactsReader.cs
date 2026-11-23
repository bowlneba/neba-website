using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series;

internal static class StageTournamentFactsReader
{
    public static async Task<IReadOnlyDictionary<TournamentId, StageTournamentFacts>> ReadAsync(
        AppDbContext appDbContext,
        IReadOnlyCollection<TournamentId> tournamentIds,
        SeriesId? excludingSeriesId,
        CancellationToken cancellationToken)
    {
        var otherSeries = appDbContext.Series.AsNoTracking();
        if (excludingSeriesId is { } excluded)
        {
            otherSeries = otherSeries.Where(series => series.Id != excluded);
        }

        var holders = await otherSeries
            .SelectMany(series => series.Stages
                .Where(stage => tournamentIds.Contains(stage.TournamentId))
                .Select(stage => new { stage.TournamentId, SeriesName = series.Name }))
            .ToDictionaryAsync(x => x.TournamentId, x => x.SeriesName, cancellationToken);

        return await appDbContext.Tournaments
            .AsNoTracking()
            .Where(tournament => tournamentIds.Contains(tournament.Id))
            .Select(tournament => new StageTournamentFacts
            {
                TournamentId = tournament.Id,
                TournamentName = tournament.Name,
                SeasonId = tournament.SeasonId,
                Status = tournament.Status,
                StartDate = tournament.StartDate,
                EndDate = tournament.EndDate,
            })
            .ToDictionaryAsync(
                facts => facts.TournamentId,
                facts => holders.TryGetValue(facts.TournamentId, out var name) ? facts with { OtherSeriesName = name } : facts,
                cancellationToken);
    }
}