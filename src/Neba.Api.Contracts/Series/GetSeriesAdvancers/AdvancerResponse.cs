namespace Neba.Api.Contracts.Series.GetSeriesAdvancers;

/// <summary>A bowler listed under a Stage. Most advanced; the Survivor standings and the already-advanced rows also list those who did not.</summary>
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

    /// <summary>True when the bowler advanced out of the Stage. False for a Survivor bowler who was cut, or a bowler who had already advanced.</summary>
    public required bool Advanced { get; init; }

    /// <summary>The order of the earlier Stage the bowler already advanced from. Null otherwise.</summary>
    public int? AlreadyAdvancedFromOrder { get; init; }
}