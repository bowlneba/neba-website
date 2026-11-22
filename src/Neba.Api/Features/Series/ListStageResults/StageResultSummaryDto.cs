using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.ListStageResults;

internal sealed record StageResultSummaryDto(BowlerId BowlerId, string BowlerName, int Place);