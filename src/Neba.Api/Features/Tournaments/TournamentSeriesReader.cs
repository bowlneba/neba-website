using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Storage;

namespace Neba.Api.Features.Tournaments;

/// <summary>
/// Loads the Series each of a page of tournaments is a Stage of, in one query. A tournament is a Stage of at
/// most one Series, so the result has no duplicates. Cancelled tournaments keep their Series.
/// </summary>
internal sealed class TournamentSeriesReader(AppDbContext appDbContext, IFileStorageService fileStorageService)
{
    public async Task<IReadOnlyDictionary<TournamentId, TournamentSeriesDto>> LoadAsync(
        IReadOnlyCollection<TournamentId> tournamentIds, CancellationToken cancellationToken)
    {
        if (tournamentIds.Count == 0)
        {
            return new Dictionary<TournamentId, TournamentSeriesDto>();
        }

        var rows = await appDbContext.Series.AsNoTracking()
            .SelectMany(series => series.Stages
                .Where(stage => tournamentIds.Contains(stage.TournamentId))
                .Select(stage => new
                {
                    stage.TournamentId,
                    SeriesId = series.Id,
                    series.Name,
                    LogoContainer = series.Logo == null ? null : series.Logo.Container,
                    LogoPath = series.Logo == null ? null : series.Logo.Path,
                    stage.Kind,
                    stage.AdvancingCount,
                    SquadDate = stage.SquadDesignation == null ? (DateOnly?)null : stage.SquadDesignation.Date,
                    SquadStartTime = stage.SquadDesignation == null ? (TimeOnly?)null : stage.SquadDesignation.StartTime,
                    FinaleCount = series.Finale == null ? (int?)null : series.Finale.FinalistCount,
                    FinaleDate = series.Finale == null ? (DateOnly?)null : series.Finale.Date,
                    FinaleStartTime = series.Finale == null ? (TimeOnly?)null : series.Finale.StartTime,
                }))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            row => row.TournamentId,
            row => new TournamentSeriesDto
            {
                SeriesId = row.SeriesId,
                Name = row.Name,
                LogoUrl = row.LogoContainer is not null && row.LogoPath is not null
                    ? fileStorageService.GetBlobUri(row.LogoContainer, row.LogoPath)
                    : null,
                Kind = row.Kind.Name,
                AdvancingCount = row.AdvancingCount,
                SquadDate = row.SquadDate,
                SquadStartTime = row.SquadStartTime,

                // Only the Placement Stage points at the Finale; other rounds don't mention it.
                FinaleCount = row.Kind == StageKind.Placement ? row.FinaleCount : null,
                FinaleDate = row.Kind == StageKind.Placement ? row.FinaleDate : null,
                FinaleStartTime = row.Kind == StageKind.Placement ? row.FinaleStartTime : null,
            });
    }
}