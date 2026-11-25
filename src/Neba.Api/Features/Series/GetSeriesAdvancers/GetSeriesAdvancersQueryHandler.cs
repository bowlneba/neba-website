using ErrorOr;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.GetSeriesAdvancers;

/// <summary>
/// The public view of advancement: names only, for each Official Stage (entries, standings or positions by kind).
/// </summary>
internal sealed class GetSeriesAdvancersQueryHandler(AppDbContext appDbContext)
    : IQueryHandler<GetSeriesAdvancersQuery, ErrorOr<SeriesAdvancersDto>>
{
    public async Task<ErrorOr<SeriesAdvancersDto>> HandleAsync(
        GetSeriesAdvancersQuery query, CancellationToken cancellationToken)
    {
        var rows = await SeriesAdvancementReader.ReadPublicAsync(appDbContext, query.SeriesId, cancellationToken);
        if (rows is null)
        {
            return SeriesErrors.NotFound(query.SeriesId);
        }

        var lastQualifying = rows.Stages.Where(s => s.Kind == StageKind.Qualifying).MaxBy(s => s.Order);

        var stages = rows.Stages
            .Where(s => s.Status == StageStatus.Official)
            .OrderBy(s => s.Order)
            .Select(stage => new StageAdvancersDto(
                stage.StageId,
                stage.Order,
                stage.Kind.Name,
                AdvancersOf(rows, stage, isLastQualifying: stage.StageId == lastQualifying?.StageId)))
            .ToList();

        return new SeriesAdvancersDto(stages);
    }

    private static List<AdvancerDto> AdvancersOf(SeriesAdvancementRows rows, AdvancementStageRow stage, bool isLastQualifying)
    {
        if (stage.Kind == StageKind.Qualifying)
        {
            // The Backfill belongs to the last Qualifying Stage. A bowler who already advanced from an earlier Stage sits
            // in place, marked, as in the admin view.
            var entries = rows.FieldEntries
                .Where(e => e.SourceStageId == stage.StageId || (isLastQualifying && e.SourceStageId is null))
                .Select(e => (
                    Name: e.BowlerName,
                    Advancer: new AdvancerDto(e.BowlerId, e.BowlerName.ToDisplayName(), e.Place, null, e.SourceStageId is null, true, null)
                    {
                        BackfillPoints = e.BackfillPoints,
                    }));

            var alreadyAdvanced = rows.AlreadyAdvanced
                .Where(a => a.StageId == stage.StageId)
                .Select(a => (
                    Name: a.BowlerName,
                    Advancer: new AdvancerDto(
                        a.BowlerId, a.BowlerName.ToDisplayName(), a.Place, null, false, false,
                        rows.Stages.Single(s => s.StageId == a.EarlierStageId).Order)));

            return [.. entries.Concat(alreadyAdvanced)
                .InFieldOrder(
                    x => x.Advancer.Place,
                    x => x.Advancer.BackfillPoints,
                    x => x.Name.LastName,
                    x => x.Name.FirstName,
                    x => x.Advancer.BowlerId.Value)
                .Select(x => x.Advancer)];
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

        // The Placement Stage: every position, with who made the Finale when the Series has one.
        return [.. rows.Positions
            .OrderBy(p => p.Position)
            .Select(p => new AdvancerDto(p.BowlerId, p.BowlerName.ToDisplayName(), p.TournamentPlace, null, false, p.IsFinalist == true, null)
            {
                Position = p.Position,
                Bowled = p.Bowled,
                IsFinalist = p.IsFinalist,
            })];
    }
}