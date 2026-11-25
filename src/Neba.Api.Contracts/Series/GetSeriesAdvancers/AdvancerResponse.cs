namespace Neba.Api.Contracts.Series.GetSeriesAdvancers;

/// <summary>A bowler listed under a tournament. Most advanced; the Survivor standings, the Placement positions and the already-advanced rows also list those who did not.</summary>
public sealed record AdvancerResponse
{
    /// <summary>The bowler id.</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>The place in the Series tournament. Null for a Bowler of the Year point list entry, an added bowler or a Survivor tournament.</summary>
    public int? Place { get; init; }

    /// <summary>The block total of a Survivor tournament. Null otherwise.</summary>
    public int? BlockTotal { get; init; }

    /// <summary>True when the bowler took a vacant slot through the Bowler of the Year point list.</summary>
    public required bool IsBackfill { get; init; }

    /// <summary>True when the bowler advanced out of the tournament. False for a Survivor bowler who was cut, or a bowler who had already advanced.</summary>
    public required bool Advanced { get; init; }

    /// <summary>The order of the earlier tournament the bowler already advanced from. Null otherwise.</summary>
    public int? AlreadyAdvancedFromOrder { get; init; }

    /// <summary>The open-category Bowler of the Year points of a Bowler of the Year point list entry. Null otherwise.</summary>
    public int? BackfillPoints { get; init; }

    /// <summary>The position on the Placement tournament. Null on other tournaments.</summary>
    public int? Position { get; init; }

    /// <summary>True when a Placement bowler bowled the Series tournament. Null on other tournaments.</summary>
    public bool? Bowled { get; init; }

    /// <summary>True when a Placement bowler made the Finale. Null when the Series has no Finale, or on other tournaments.</summary>
    public bool? IsFinalist { get; init; }
}