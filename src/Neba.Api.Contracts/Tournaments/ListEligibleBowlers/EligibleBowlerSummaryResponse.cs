namespace Neba.Api.Contracts.Tournaments.ListEligibleBowlers;

/// <summary>
/// A bowler who qualifies for an Invitational.
/// </summary>
public sealed record EligibleBowlerSummaryResponse
{
    /// <summary>
    /// The bowler's display name (nickname when set).
    /// </summary>
    /// <example>Jane Smith</example>
    public required string Name { get; init; }

    /// <summary>
    /// The bowler's USBC ID, or <see langword="null"/> when none is on file.
    /// </summary>
    /// <example>4821-09317</example>
    public string? UsbcId { get; init; }
}
