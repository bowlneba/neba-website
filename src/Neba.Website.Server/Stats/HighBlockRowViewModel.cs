namespace Neba.Website.Server.Stats;

/// <summary>
/// View model for a single row in the High Block table, which ranks bowlers based on their highest block score across all games they have played in the season. A block is defined as a series of three consecutive games, and the high block score is the total pinfall of the three games in the block. Bowlers are ranked in descending order of their high block score, with ties broken by the highest individual game score within the block and then by the bowler's name. This ranking provides insight into which bowlers have had the most impressive performances in terms of consistency and high scoring across multiple games in a single tournament or across the season.
/// </summary>
public sealed record HighBlockRowViewModel
{
    /// <summary>
    /// The rank of the bowler in the High Block standings, starting at 1 for the bowler with the highest block score. Bowlers are ranked in descending order of their high block score, with ties broken by the highest individual game score within the block and then by the bowler's name. This ranking provides insight into which bowlers have had the most impressive performances in terms of consistency and high scoring across multiple games in a single tournament or across the season.
    /// </summary>
    public required int Rank { get; init; }

    /// <summary>
    /// The unique identifier of the bowler, used for linking to the bowler's profile page. This allows users to click on the bowler's name in the High Block table and navigate to their profile for more detailed information about their performance throughout the season.
    /// </summary>
    public required string BowlerId { get; init; }

    /// <summary>
    /// The name of the bowler, displayed in the High Block table and linked to the bowler's profile page. This provides a user-friendly way to identify the bowler and access more information about their performance in the season.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// The highest block score of the bowler across all games they have played in the season, calculated as the total pinfall of the three games in the block. This is the primary metric used for ranking bowlers in the High Block standings, with higher block scores indicating better performance. Bowlers are ranked in descending order of their high block score, with ties broken by the highest individual game score within the block and then by the bowler's name.
    /// </summary>
    public required int HighBlock { get; init; }

    /// <summary>
    /// The highest individual game score within the block that contributed to the bowler's high block score. This is used as a tiebreaker for bowlers with the same high block score, with higher individual game scores indicating better performance. This field provides additional context for the bowler's performance in the High Block standings, allowing users to see not only the total pinfall of the block but also the highest scoring game within that block.
    /// </summary>
    public required int HighGame { get; init; }
}