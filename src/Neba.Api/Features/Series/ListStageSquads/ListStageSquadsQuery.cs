using ErrorOr;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.ListStageSquads;

internal sealed record ListStageSquadsQuery
    : IQuery<ErrorOr<IReadOnlyList<StageSquadSummaryDto>>>
{
    public required SeriesId SeriesId { get; init; }

    public required SeriesStageId StageId { get; init; }
}