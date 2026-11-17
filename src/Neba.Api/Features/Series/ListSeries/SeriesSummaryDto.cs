using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Series.ListSeries;

internal sealed record SeriesSummaryDto
{
    public required SeriesId Id { get; init; }
    public required string Name { get; init; }
    public required SeasonId SeasonId { get; init; }
    public required string SeasonDescription { get; init; }
    public required DateOnly SeasonStartDate { get; init; }
    public required DateOnly SeasonEndDate { get; init; }
    public required int QualifyingStageCount { get; init; }
    public required int SurvivorStageCount { get; init; }
    public required int PlacementStageCount { get; init; }
    public required bool IsLocked { get; init; }
    public Uri? LogoUrl { get; init; }

    /// <summary>Projection only; not mapped to the response.</summary>
    public string? LogoContainer { get; init; }

    /// <summary>Projection only; not mapped to the response.</summary>
    public string? LogoPath { get; init; }

    /// <summary>The first Stage (by order) whose tournament hasn't ended; null when all have.</summary>
    public SeriesNextStageDto? NextStage { get; init; }
}
