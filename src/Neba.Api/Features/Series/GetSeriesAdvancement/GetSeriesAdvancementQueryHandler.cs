using ErrorOr;

using Microsoft.Extensions.Options;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.GetSeriesAdvancement;

internal sealed class GetSeriesAdvancementQueryHandler(
    AppDbContext appDbContext, IOptions<SeriesAdvancementSettings> settings)
    : IQueryHandler<GetSeriesAdvancementQuery, ErrorOr<SeriesAdvancementDto>>
{
    public async Task<ErrorOr<SeriesAdvancementDto>> HandleAsync(
        GetSeriesAdvancementQuery query, CancellationToken cancellationToken)
    {
        var rows = await SeriesAdvancementReader.ReadAsync(appDbContext, query.SeriesId, cancellationToken);
        if (rows is null)
        {
            return SeriesErrors.NotFound(query.SeriesId);
        }

        var grace = settings.Value.OfficialGrace;
        var lastQualifying = rows.Stages.Where(s => s.Kind == StageKind.Qualifying).MaxBy(s => s.Order);

        var stages = rows.Stages.OrderBy(s => s.Order).Select(stage =>
        {
            var isLastQualifying = stage.StageId == lastQualifying?.StageId;

            // The Backfill belongs to the last Qualifying Stage.
            var entries = rows.FieldEntries
                .Where(e => e.SourceStageId == stage.StageId || (isLastQualifying && e.SourceStageId is null))
                .Select(e => new FieldEntryDto(
                    e.BowlerId, e.BowlerName.ToDisplayName(), e.SourceStageId, e.Place, e.BackfillPoints, e.ViaOverride))
                .OrderBy(e => e.Place ?? int.MaxValue)
                .ThenBy(e => e.BowlerName, StringComparer.Ordinal)
                .ThenBy(e => e.BowlerId.Value)
                .ToList();

            int? vacant = stage.SlotCount is { } slots
                ? slots - rows.FieldEntries.Count(e => e.SourceStageId == stage.StageId && !e.ViaOverride)
                : null;

            var alreadyAdvanced = rows.AlreadyAdvanced
                .Where(a => a.StageId == stage.StageId)
                .Select(a => new AlreadyAdvancedDto(
                    a.BowlerId, a.BowlerName.ToDisplayName(), a.Place, a.EarlierStageId,
                    rows.Stages.Single(s => s.StageId == a.EarlierStageId).Order))
                .OrderBy(a => a.Place)
                .ThenBy(a => a.BowlerName, StringComparer.Ordinal)
                .ThenBy(a => a.BowlerId.Value)
                .ToList();

            var overrides = rows.Overrides
                .Where(o => o.StageId == stage.StageId)
                .OrderBy(o => o.CreatedAt)
                .ThenBy(o => o.OverrideId.Value)
                .Select(o => new StageOverrideDto(
                    o.OverrideId, o.BowlerId, o.BowlerName.ToDisplayName(), o.Kind.Name,
                    o.Reason, o.CreatedBy, o.CreatedAt, o.Applied))
                .ToList();

            var outcomes = rows.SurvivorOutcomes
                .Where(o => o.StageId == stage.StageId)
                .Select(o => new SurvivorOutcomeDto(o.BowlerId, o.BowlerName.ToDisplayName(), o.BlockTotal, o.Advanced))
                .OrderByDescending(o => o.BlockTotal ?? int.MinValue)
                .ThenBy(o => o.BowlerName, StringComparer.Ordinal)
                .ThenBy(o => o.BowlerId.Value)
                .ToList();

            return new StageAdvancementDto(
                stage.StageId,
                stage.Order,
                stage.Kind.Name,
                stage.Status.Name,
                stage.IsStale,
                NeedsSquad: stage.Kind == StageKind.Survivor && (stage.SquadId is null || !stage.SquadExists),
                stage.ComputedAt,
                SeriesAdvancementRules.OfficialDueAt(
                    stage.Kind, stage.Status, stage.IsStale, stage.ComputedAt,
                    awaitingBackfill: isLastQualifying && rows.Header.BackfillAsOf is null, grace),
                stage.OfficialAt,
                stage.SlotCount,
                vacant,
                stage.TournamentId,
                stage.TournamentStatus.Name,
                stage.SquadId,
                entries,
                alreadyAdvanced,
                overrides,
                outcomes);
        }).ToList();

        // Target field: a Qualifying Stage's stored slot total, else its advancing count times the team size.
        var targetField = rows.Stages
            .Where(s => s.Kind == StageKind.Qualifying)
            .Sum(s => s.SlotCount ?? (s.AdvancingCount ?? 0) * s.TournamentType.TeamSize);

        return new SeriesAdvancementDto(
            rows.Header.Version,
            rows.Header.BackfillAsOf,
            targetField,
            stages,
            [.. rows.Positions
                .Select(p => new SeriesPositionDto(p.BowlerId, p.BowlerName.ToDisplayName(), p.Position, p.Bowled, p.IsFinalist, p.TournamentPlace))
                .OrderBy(p => p.Position)
                .ThenBy(p => p.BowlerId.Value)]);
    }
}