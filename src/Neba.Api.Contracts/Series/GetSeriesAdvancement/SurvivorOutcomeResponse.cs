namespace Neba.Api.Contracts.Series.GetSeriesAdvancement;

/// <summary>A field bowler's result at a Survivor tournament.</summary>
public sealed record SurvivorOutcomeResponse
{
    /// <summary>The bowler id.</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>The block total. Null when the bowler bowled no games.</summary>
    public int? BlockTotal { get; init; }

    /// <summary>True when the bowler advanced.</summary>
    public required bool Advanced { get; init; }
}