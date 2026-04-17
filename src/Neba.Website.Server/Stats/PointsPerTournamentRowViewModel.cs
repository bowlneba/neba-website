namespace Neba.Website.Server.Stats;

/// <summary>
/// View model representing the points per tournament for a bowler in the stats page.
/// </summary>
public sealed record PointsPerTournamentRowViewModel
{
    /// <summary>
    /// The rank of the bowler based on points per tournament, with 1 being the highest.
    /// </summary>
    public required int Rank { get; init; }

    /// <summary>
    /// The unique identifier of the bowler, represented as an Ulid. This is used to link to the bowler's profile page.
    /// </summary>
    public required string BowlerId { get; init; }

    /// <summary>
    /// The name of the bowler. This is displayed in the stats table and linked to the bowler's profile page.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// The total points earned by the bowler across all tournaments. This is used to calculate the points per tournament and is displayed in the stats table.
    /// </summary>
    public required int Points { get; init; }

    /// <summary>
    /// The total number of tournaments the bowler has participated in. This is used to calculate the points per tournament and is displayed in the stats table.
    /// </summary>
    public required int Tournaments { get; init; }

    /// <summary>
    /// The average points earned per tournament by the bowler. This is calculated by dividing the total points by the total number of tournaments. If the bowler has not participated in any tournaments, this value will be 0 to avoid division by zero errors. This is displayed in the stats table and used for ranking the bowlers.
    /// </summary>
    public decimal PointsPerTournament
        => Tournaments == 0 ? 0 : (decimal)Points / Tournaments;
}