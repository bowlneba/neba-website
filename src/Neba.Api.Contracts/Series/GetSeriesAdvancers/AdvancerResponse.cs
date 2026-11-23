namespace Neba.Api.Contracts.Series.GetSeriesAdvancers;

/// <summary>A bowler who advanced out of a Stage.</summary>
public sealed record AdvancerResponse
{
    /// <summary>The bowler id.</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>The place in the Stage's tournament. Null for a Backfill entry, an added bowler or a Survivor Stage.</summary>
    public int? Place { get; init; }

    /// <summary>The block total of a Survivor Stage. Null otherwise.</summary>
    public int? BlockTotal { get; init; }

    /// <summary>True when the bowler took a vacant slot through the Backfill.</summary>
    public required bool IsBackfill { get; init; }
}
