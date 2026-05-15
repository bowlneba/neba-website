namespace Neba.Api.Features.Stats.GetSeasonStats;

/// <summary>
/// A single bowler's computed standing in an award category (Bowler of the Year, Senior of the Year,
/// Super Senior of the Year, Woman of the Year, Rookie of the Year, or Youth of the Year).
/// Points have been selected for the category; the collection is ordered by points descending.
/// </summary>
public sealed record BowlerOfTheYearStandingDto
{
    /// <summary>
    /// The unique identifier of the bowler.
    /// </summary>
    public required BowlerId BowlerId { get; init; }

    /// <summary>
    /// The bowler's display name.
    /// </summary>
    public required Name BowlerName { get; init; }

    /// <summary>
    /// Points accumulated for this award category during the season.
    /// </summary>
    public required int Points { get; init; }

    /// <summary>
    /// The number of distinct tournaments the bowler participated in during the season.
    /// </summary>
    public required int Tournaments { get; init; }

    /// <summary>
    /// The total number of tournament entries during the season.
    /// </summary>
    public required int Entries { get; init; }

    /// <summary>
    /// The number of Finals appearances during the season.
    /// </summary>
    public required int Finals { get; init; }

    /// <summary>
    /// The mean finishing position, or null if the bowler had no recorded finishing positions.
    /// </summary>
    public required decimal? AverageFinish { get; init; }

    /// <summary>
    /// Total prize money earned during the season.
    /// </summary>
    public required decimal Winnings { get; init; }
}