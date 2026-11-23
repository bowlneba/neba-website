using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Series.GetSeriesAdvancers;

internal sealed record SeriesAdvancersDto(IReadOnlyList<StageAdvancersDto> Stages);

internal sealed record StageAdvancersDto(
    SeriesStageId StageId, int Order, string Kind, IReadOnlyList<AdvancerDto> Advancers);

internal sealed record AdvancerDto(BowlerId BowlerId, string BowlerName, int? Place, int? BlockTotal, bool IsBackfill);
