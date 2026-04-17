using Neba.Domain.Bowlers;

namespace Neba.Application.Stats.GetSeasonStats;

/// <summary>
/// A lightweight bowler entry used to populate the season stats bowler search list.
/// Ordered alphabetically by last name then first name.
/// </summary>
public sealed record BowlerSearchEntryDto
{
    /// <summary>The unique identifier of the bowler.</summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>The bowler's display name.</summary>
    public required Name BowlerName { get; init; }
}
