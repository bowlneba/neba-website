namespace Neba.Api.Contracts.Bowlers.SearchBowlers;

/// <summary>
/// One bowler found by a name search.
/// </summary>
public sealed record BowlerSearchResponse
{
    /// <summary>
    /// The bowler's unique ID.
    /// </summary>
    public required string BowlerId { get; init; }

    /// <summary>
    /// The bowler's display name.
    /// </summary>
    /// <example>Dave Smith</example>
    public required string Name { get; init; }

    /// <summary>
    /// The bowler's USBC membership ID; null when it is not known.
    /// </summary>
    /// <example>4821-09317</example>
    public string? UsbcId { get; init; }
}
