namespace Neba.Api.Contracts.Series.CreateSeries;

/// <summary>Create series request.</summary>
public sealed record CreateSeriesRequest
{
    /// <summary>The series.</summary>
    public required SeriesInput Series { get; init; }
}
