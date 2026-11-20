using ErrorOr;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.ListStageResults;

internal sealed record ListStageResultsQuery
    : IQuery<ErrorOr<IReadOnlyList<StageResultSummaryDto>>>
{
    public required SeriesId SeriesId { get; init; }

    public required SeriesStageId StageId { get; init; }
}