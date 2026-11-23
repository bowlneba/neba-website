using ErrorOr;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.GetSeriesAdvancers;

/// <summary>
/// The public view of advancement: names only. A Stage shows once it is Official. A Qualifying Stage lists its field entries
/// and the bowlers who had already advanced, a Survivor Stage lists its full standings, and a Placement Stage lists the
/// Finale's finalists (nothing when the Series has no Finale).
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
        if (stage.Status != StageStatus.Official)
        {
            return null;
        }

        if (stage.Kind == StageKind.Qualifying)
        {
            // The Backfill belongs to the last Qualifying Stage. A bowler who already advanced from an earlier Stage sits
            // in place, marked, as in the admin view.
            var entries = rows.FieldEntries
                .Where(e => e.SourceStageId == stage.StageId || (isLastQualifying && e.SourceStageId is null))
                .Select(e => new AdvancerDto(e.BowlerId, e.BowlerName.ToDisplayName(), e.Place, null, e.SourceStageId is null, true, null));

            var alreadyAdvanced = rows.AlreadyAdvanced
                .Where(a => a.StageId == stage.StageId)
                .Select(a => new AdvancerDto(
                    a.BowlerId, a.BowlerName.ToDisplayName(), a.Place, null, false, false,
                    rows.Stages.Single(s => s.StageId == a.EarlierStageId).Order));

            return [.. entries.Concat(alreadyAdvanced)
                .OrderBy(a => a.Place ?? int.MaxValue)
                .ThenBy(a => a.BowlerName, StringComparer.Ordinal)
                .ThenBy(a => a.BowlerId.Value)];
        }

        if (stage.Kind == StageKind.Survivor)
        {
            // The full standings, as in the admin view.
            return [.. rows.SurvivorOutcomes
                .Where(o => o.StageId == stage.StageId)
                .Select(o => new AdvancerDto(o.BowlerId, o.BowlerName.ToDisplayName(), null, o.BlockTotal, false, o.Advanced, null))
                .OrderByDescending(a => a.BlockTotal ?? int.MinValue)
                .ThenBy(a => a.BowlerName, StringComparer.Ordinal)
                .ThenBy(a => a.BowlerId.Value)];
        }

        if (rows.Positions.Any(p => p.IsFinalist == true))
        {
            return [.. rows.Positions
                .Where(p => p.IsFinalist == true)
                .OrderBy(p => p.Position)
                .Select(p => new AdvancerDto(p.BowlerId, p.BowlerName.ToDisplayName(), p.TournamentPlace, null, false, true, null))];
        }

        return null;
    }
}