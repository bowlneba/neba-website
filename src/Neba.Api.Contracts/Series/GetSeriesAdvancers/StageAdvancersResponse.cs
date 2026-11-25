namespace Neba.Api.Contracts.Series.GetSeriesAdvancers;

/// <summary>The bowlers who advanced out of one tournament.</summary>
public sealed record StageAdvancersResponse
{
    /// <summary>The Series tournament id.</summary>
    public required string StageId { get; init; }

    /// <summary>The order.</summary>
    public required int Order { get; init; }

    /// <summary>The kind name: Qualifying, Survivor or Placement.</summary>
    public required string Kind { get; init; }

    /// <summary>The bowlers listed under the tournament. A Placement tournament lists every position.</summary>
    public required IReadOnlyList<AdvancerResponse> Advancers { get; init; }
}