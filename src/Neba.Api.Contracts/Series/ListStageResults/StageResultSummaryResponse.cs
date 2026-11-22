namespace Neba.Api.Contracts.Series.ListStageResults;

/// <summary>A result row of a Stage's tournament.</summary>
public sealed record StageResultSummaryResponse
{
    /// <summary>The bowler id.</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>The place.</summary>
    public required int Place { get; init; }
}