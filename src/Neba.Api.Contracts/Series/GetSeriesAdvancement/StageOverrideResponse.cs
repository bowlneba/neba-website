namespace Neba.Api.Contracts.Series.GetSeriesAdvancement;

/// <summary>An admin Add or Clear on a Qualifying tournament.</summary>
public sealed record StageOverrideResponse
{
    /// <summary>The override id.</summary>
    public required string OverrideId { get; init; }

    /// <summary>The bowler id.</summary>
    public required string BowlerId { get; init; }

    /// <summary>The bowler name.</summary>
    public required string BowlerName { get; init; }

    /// <summary>The kind name: Add or Clear.</summary>
    public required string Kind { get; init; }

    /// <summary>The reason.</summary>
    public required string Reason { get; init; }

    /// <summary>The admin's user id.</summary>
    public required string CreatedBy { get; init; }

    /// <summary>When the override was made.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>False when the override no longer takes effect.</summary>
    public required bool Applied { get; init; }
}