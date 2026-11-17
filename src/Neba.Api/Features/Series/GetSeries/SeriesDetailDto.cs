using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Series.GetSeries;

internal sealed record SeriesDetailDto
{
    public required SeriesId Id { get; init; }
    public required string Name { get; init; }
    public required SeasonId SeasonId { get; init; }
    public required string SeasonDescription { get; init; }
    public required DateOnly SeasonStartDate { get; init; }
    public required DateOnly SeasonEndDate { get; init; }
    public required bool IsLocked { get; init; }

    /// <summary>The Finale, or null when the Series has none.</summary>
    public SeriesFinaleDto? Finale { get; init; }

    /// <summary>The Series' own logo (no fallback). The edit page resends container, path, type and size.</summary>
    public Uri? LogoUrl { get; init; }
    public string? LogoContainer { get; init; }
    public string? LogoPath { get; init; }
    public string? LogoContentType { get; init; }
    public long? LogoSizeInBytes { get; init; }

    public required IReadOnlyCollection<SeriesStageDto> Stages { get; init; }
}
