using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.ListStageSquads;

internal sealed class ListStageSquadsQueryHandler(AppDbContext appDbContext)
    : IQueryHandler<ListStageSquadsQuery, ErrorOr<IReadOnlyList<StageSquadSummaryDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<StageSquadSummaryDto>>> HandleAsync(
        ListStageSquadsQuery query, CancellationToken cancellationToken)
    {
        var squads = await StageSquadsReader.ReadForStageAsync(appDbContext, query.SeriesId, query.StageId, cancellationToken);

        if (squads is null)
        {
            return SeriesAdvancementErrors.StageNotFound(query.StageId);
        }

        // The reader returns the Stage's squads; the squad the Stage uses is on the Stage's designation.
        var selectedSquadId = await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == query.SeriesId)
            .SelectMany(series => series.Stages)
            .Where(stage => stage.Id == query.StageId)
            .Select(stage => stage.SquadDesignation == null ? null : stage.SquadDesignation.SquadId)
            .SingleAsync(cancellationToken);

        return squads
            .Select(squad =>
            {
                var (date, time) = EasternTime.ToLocal(squad.BowlingDateTimeUtc);

                return new StageSquadSummaryDto(squad.SquadId, date, time, squad.HasScores, squad.SquadId == selectedSquadId);
            })
            .ToList();
    }
}