using ErrorOr;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.GetSeriesAdvancers;

/// <summary>
/// The public view of advancement: names only. A Qualifying or Survivor Stage shows once it is Official. A Placement Stage
/// never becomes Official, so it shows once computed, and only when the Series has a Finale (its finalists advance).
/// </summary>
internal sealed class GetSeriesAdvancersQueryHandler(AppDbContext appDbContext)
    : IQueryHandler<GetSeriesAdvancersQuery, ErrorOr<SeriesAdvancersDto>>
{
    public async Task<ErrorOr<SeriesAdvancersDto>> HandleAsync(
        GetSeriesAdvancersQuery query, CancellationToken cancellationToken)
    {
        var rows = await SeriesAdvancementReader.ReadAsync(appDbContext, query.SeriesId, cancellationToken);
        if (rows is null)
        {
            return SeriesErrors.NotFound(query.SeriesId);
        }

        var lastQualifying = rows.Stages.Where(s => s.Kind == StageKind.Qualifying).MaxBy(s => s.Order);

        var stages = new List<StageAdvancersDto>();
        foreach (var stage in rows.Stages.OrderBy(s => s.Order))
        {
            var advancers = AdvancersOf(rows, stage, isLastQualifying: stage.StageId == lastQualifying?.StageId);
            if (advancers is not null)
            {
                stages.Add(new StageAdvancersDto(stage.StageId, stage.Order, stage.Kind.Name, advancers));
            }
        }

        return new SeriesAdvancersDto(stages);
    }

    private static List<AdvancerDto>? AdvancersOf(SeriesAdvancementRows rows, AdvancementStageRow stage, bool isLastQualifying)
    {
        if (stage.Kind == StageKind.Qualifying && stage.Status == StageStatus.Official)
        {
            // The Backfill belongs to the last Qualifying Stage.
            return [.. rows.FieldEntries
                .Where(e => e.SourceStageId == stage.StageId || (isLastQualifying && e.SourceStageId is null))
                .Select(e => new AdvancerDto(e.BowlerId, e.BowlerName.ToDisplayName(), e.Place, null, e.SourceStageId is null))
                .OrderBy(a => a.Place ?? int.MaxValue)
                .ThenBy(a => a.BowlerName, StringComparer.Ordinal)
                .ThenBy(a => a.BowlerId.Value)];
        }

        if (stage.Kind == StageKind.Survivor && stage.Status == StageStatus.Official)
        {
            return [.. rows.SurvivorOutcomes
                .Where(o => o.StageId == stage.StageId && o.Advanced)
                .Select(o => new AdvancerDto(o.BowlerId, o.BowlerName.ToDisplayName(), null, o.BlockTotal, false))
                .OrderByDescending(a => a.BlockTotal ?? int.MinValue)
                .ThenBy(a => a.BowlerName, StringComparer.Ordinal)
                .ThenBy(a => a.BowlerId.Value)];
        }

        if (stage.Kind == StageKind.Placement && stage.Status != StageStatus.Pending && rows.Positions.Any(p => p.IsFinalist == true))
        {
            return [.. rows.Positions
                .Where(p => p.IsFinalist == true)
                .OrderBy(p => p.Position)
                .Select(p => new AdvancerDto(p.BowlerId, p.BowlerName.ToDisplayName(), p.TournamentPlace, null, false))];
        }

        return null;
    }
}
