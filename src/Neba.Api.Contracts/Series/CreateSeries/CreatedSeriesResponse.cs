namespace Neba.Api.Contracts.Series.CreateSeries;

/// <summary>Created series response.</summary>
public sealed record CreatedSeriesResponse
{
    /// <summary>The series id.</summary>
    public required string SeriesId { get; init; }
}