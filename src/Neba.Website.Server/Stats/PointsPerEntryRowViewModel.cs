namespace Neba.Website.Server.Stats;

/// <summary>
/// ViewModel representing a row in the "Points Per Entry" statistics table, which ranks bowlers based on their average points earned per tournament entry. It includes the bowler's rank, name, total points, and total entries, allowing for a quick comparison of performance efficiency across different bowlers.
/// </summary>
public sealed record PointsPerEntryRowViewModel
{
    /// <summary>
    /// The rank of the bowler in the "Points Per Entry" standings, where 1 represents the highest average points per entry. Rankings are determined by sorting bowlers in descending order based on their PointsPerEntry value, with ties broken by total points and then by total entries if necessary.
    /// </summary>
    public int Rank { get; init; }

    /// <summary>
    /// The unique identifier of the bowler, represented as a ULID (Universally Unique Lexicographically Sortable Identifier). This ID is used to associate the row with a specific bowler in the database and can be used for linking to the bowler's profile or for other data retrieval purposes.
    /// </summary>
    public Ulid BowlerId { get; init; }

    /// <summary>
    /// The full name of the bowler, which is displayed in the statistics table. This field provides a human-readable identifier for the bowler, allowing users to easily recognize and differentiate between bowlers in the standings.
    /// </summary>
    public string BowlerName { get; init; } = null!;

    /// <summary>
    /// The average points earned per tournament entry for the bowler, calculated as the total points divided by the total entries. This value is rounded to three decimal places for display purposes. If the bowler has no entries, this value defaults to 0 to avoid division by zero errors. This metric provides insight into the efficiency of the bowler's performance across their tournament participations.
    /// </summary>
    public decimal PointsPerEntry
        => Entries > 0 ? decimal.Round((decimal)Points / Entries, 3) : 0m;

    /// <summary>
    /// The total points earned by the bowler across all tournament entries. This value is used in conjunction with the total entries to calculate the PointsPerEntry metric, which is the primary basis for ranking bowlers in this statistics table.
    /// </summary>
    public int Points { get; init; }

    /// <summary>
    /// The total number of tournament entries for the bowler. This value is used to calculate the PointsPerEntry metric and provides context for the bowler's performance, indicating how many times they have participated in tournaments. A higher number of entries with a high PointsPerEntry value indicates consistent performance across many tournaments, while a lower number of entries with a high PointsPerEntry value may indicate strong performance in fewer participations.
    /// </summary>
    public int Entries { get; init; }
}