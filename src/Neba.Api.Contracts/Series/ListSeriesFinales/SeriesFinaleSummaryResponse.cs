using Neba.Api.Contracts.Series.GetSeries;

namespace Neba.Api.Contracts.Series.ListSeriesFinales;

/// <summary>Series finale summary response.</summary>
public sealed record SeriesFinaleSummaryResponse
{
    /// <summary>The series id.</summary>
    public required string SeriesId { get; init; }
    /// <summary>The series name.</summary>
    public required string SeriesName { get; init; }
    /// <summary>The season id.</summary>
    public required string SeasonId { get; init; }
    /// <summary>The logo url.</summary>
    public Uri? LogoUrl { get; init; }
    /// <summary>The finale.</summary>
    public required SeriesFinaleResponse Finale { get; init; }
}
