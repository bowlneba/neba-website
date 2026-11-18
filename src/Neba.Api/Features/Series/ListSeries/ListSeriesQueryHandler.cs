using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;
using Neba.Api.Storage;

namespace Neba.Api.Features.Series.ListSeries;

internal sealed class ListSeriesQueryHandler(AppDbContext appDbContext, IFileStorageService fileStorageService, TimeProvider timeProvider)
    : IQueryHandler<ListSeriesQuery, IReadOnlyCollection<SeriesSummaryDto>>
{
    public async Task<IReadOnlyCollection<SeriesSummaryDto>> HandleAsync(ListSeriesQuery query, CancellationToken cancellationToken)
    {
        // Same cut-off as ListUpcomingTournaments: back up one day so a tournament ending today
        // still counts in every U.S. time zone.
        var cutoff = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime).AddDays(-1);

        List<SeriesSummaryDto> summaries = await appDbContext.Series.AsNoTracking()
            .Join(appDbContext.Seasons.AsNoTracking(), series => series.SeasonId, season => season.Id,
                (series, season) => new { series, season })
            .OrderByDescending(x => x.season.StartDate)
            .ThenBy(x => x.series.Name)
            .Select(x => new SeriesSummaryDto
            {
                Id = x.series.Id,
                Name = x.series.Name,
                SeasonId = x.season.Id,
                SeasonDescription = x.season.Description,
                SeasonStartDate = x.season.StartDate,
                SeasonEndDate = x.season.EndDate,
                QualifyingStageCount = x.series.Stages.Count(s => s.Kind == StageKind.Qualifying),
                SurvivorStageCount = x.series.Stages.Count(s => s.Kind == StageKind.Survivor),
                PlacementStageCount = x.series.Stages.Count(s => s.Kind == StageKind.Placement),
                IsLocked = x.series.Stages.Any(s => s.Status != StageStatus.Pending),
                LogoContainer = x.series.Logo == null ? null : x.series.Logo.Container,
                LogoPath = x.series.Logo == null ? null : x.series.Logo.Path,
            })
            .ToListAsync(cancellationToken);

        // The URL is built here, the same way GetTournamentQueryHandler builds LogoUrl.
        summaries = [.. summaries.Select(s => s.LogoContainer is not null && s.LogoPath is not null
            ? s with { LogoUrl = fileStorageService.GetBlobUri(s.LogoContainer, s.LogoPath) }
            : s)];

        // Every Stage of every Series (a few dozen rows at most), in one query. It gives each Series' tournament
        // IDs, and the first Stage that has not ended.
        var stages = await appDbContext.Series.AsNoTracking()
            .SelectMany(series => series.Stages, (series, stage) => new { SeriesId = series.Id, stage.Order, stage.Kind, stage.TournamentId })
            .Join(appDbContext.Tournaments.AsNoTracking(), x => x.TournamentId, t => t.Id,
                (x, t) => new { x.SeriesId, x.Order, x.Kind, x.TournamentId, t.Name, t.StartDate, t.EndDate })
            .ToListAsync(cancellationToken);

        var stagesBySeries = stages.ToLookup(x => x.SeriesId);

        var nextBySeries = stages
            .Where(x => x.EndDate >= cutoff)
            .GroupBy(x => x.SeriesId)
            .ToDictionary(g => g.Key, g => g.MinBy(x => x.Order)!);

        return [.. summaries.Select(summary => nextBySeries.TryGetValue(summary.Id, out var next)
            ? summary with
            {
                StageTournamentIds = [.. stagesBySeries[summary.Id].OrderBy(x => x.Order).Select(x => x.TournamentId)],
                NextStage = new SeriesNextStageDto
                {
                    Kind = next.Kind.Name,
                    TournamentId = next.TournamentId,
                    TournamentName = next.Name,
                    TournamentStartDate = next.StartDate,
                    TournamentEndDate = next.EndDate,
                },
            }
            : summary with { StageTournamentIds = [.. stagesBySeries[summary.Id].OrderBy(x => x.Order).Select(x => x.TournamentId)] })];
    }
}