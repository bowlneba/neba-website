using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Series.GetSeriesAdvancers;

internal sealed record SeriesAdvancersDto(IReadOnlyList<StageAdvancersDto> Stages);

internal sealed record StageAdvancersDto(
    SeriesStageId StageId, int Order, string Kind, IReadOnlyList<AdvancerDto> Advancers);

internal sealed record AdvancerDto(BowlerId BowlerId, string BowlerName, int? Place, int? BlockTotal, bool IsBackfill, bool Advanced, int? AlreadyAdvancedFromOrder)
{
    /// <summary>Open-category Bowler of the Year points of a Backfill entry.</summary>
    public int? BackfillPoints { get; init; }

    /// <summary>Placement position; null on other Stages.</summary>
    public int? Position { get; init; }

    /// <summary>Whether a Placement bowler bowled the Stage's tournament; null on other Stages.</summary>
    public bool? Bowled { get; init; }

    /// <summary>Whether a Placement bowler made the Finale; null when the Series has no Finale or on other Stages.</summary>
    public bool? IsFinalist { get; init; }
}