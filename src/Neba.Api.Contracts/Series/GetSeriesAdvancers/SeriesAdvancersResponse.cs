namespace Neba.Api.Contracts.Series.GetSeriesAdvancers;

/// <summary>The bowlers who advanced out of each settled Stage of a Series.</summary>
public sealed record SeriesAdvancersResponse
{
    /// <summary>The settled Stages, in order. A Stage that is not settled is left out.</summary>
    public required IReadOnlyList<StageAdvancersResponse> Stages { get; init; }
}