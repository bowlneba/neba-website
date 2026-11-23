namespace Neba.Api.Contracts.Series.GetSeriesAdvancement;

/// <summary>A bowler who finished inside a Qualifying Stage's cut but had already advanced from an earlier Stage.</summary>
public sealed record AlreadyAdvancedResponse
{
    /// <summary>The bowler id.</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>The place in this Stage's tournament.</summary>
    public required int Place { get; init; }

    /// <summary>The earlier Stage the bowler advanced from.</summary>
    public required string EarlierStageId { get; init; }

    /// <summary>The earlier Stage's order.</summary>
    public required int EarlierStageOrder { get; init; }
}
