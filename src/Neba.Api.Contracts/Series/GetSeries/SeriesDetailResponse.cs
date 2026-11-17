namespace Neba.Api.Contracts.Series.GetSeries;

/// <summary>Series detail response.</summary>
public sealed record SeriesDetailResponse
{
    /// <summary>The id.</summary>
    public required string Id { get; init; }
    /// <summary>The name.</summary>
    public required string Name { get; init; }
    /// <summary>The season id.</summary>
    public required string SeasonId { get; init; }
    /// <summary>The season description.</summary>
    public required string SeasonDescription { get; init; }
    /// <summary>The season start date.</summary>
    public required DateOnly SeasonStartDate { get; init; }
    /// <summary>The season end date.</summary>
    public required DateOnly SeasonEndDate { get; init; }
    /// <summary>The is locked.</summary>
    public required bool IsLocked { get; init; }

    /// <summary>The Finale, or null when the Series has none.</summary>
    public SeriesFinaleResponse? Finale { get; init; }
    /// <summary>The logo url.</summary>
    public Uri? LogoUrl { get; init; }
    /// <summary>The logo container.</summary>
    public string? LogoContainer { get; init; }
    /// <summary>The logo path.</summary>
    public string? LogoPath { get; init; }
    /// <summary>The logo content type.</summary>
    public string? LogoContentType { get; init; }
    /// <summary>The logo size in bytes.</summary>
    public long? LogoSizeInBytes { get; init; }
    /// <summary>The stages.</summary>
    public required IReadOnlyCollection<SeriesStageResponse> Stages { get; init; }
}
