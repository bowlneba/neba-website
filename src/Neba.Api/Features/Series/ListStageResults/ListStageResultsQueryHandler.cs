using ErrorOr;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.ListStageResults;

internal sealed class ListStageResultsQueryHandler(AppDbContext appDbContext)
    : IQueryHandler<ListStageResultsQuery, ErrorOr<IReadOnlyList<StageResultSummaryDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<StageResultSummaryDto>>> HandleAsync(
        ListStageResultsQuery query, CancellationToken cancellationToken)
    {
        var rows = await StageResultRowsReader.ReadAsync(appDbContext, query.SeriesId, query.StageId, cancellationToken);

        return rows is null
            ? SeriesAdvancementErrors.StageNotFound(query.StageId)
            : rows
                .Select(row => new StageResultSummaryDto(row.BowlerId, row.BowlerName.ToDisplayName(), row.Place))
                .ToList();
    }
}