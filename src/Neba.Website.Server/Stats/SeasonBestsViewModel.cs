namespace Neba.Website.Server.Stats;

/// <summary>
/// Represents the best performances of the season, including high game, high block, and high average, along with the bowlers who achieved these feats.
/// </summary>
public sealed record SeasonBestsViewModel
{
    /// <summary>
    /// The highest game score achieved during the season.
    /// </summary>
    public required int HighGame { get; init; }

    /// <summary>
    /// A dictionary mapping bowler IDs to their names for those who achieved the highest game score during the season.
    /// </summary>
    public required IReadOnlyDictionary<string, string> HighGameBowlers { get; init; }

    /// <summary>
    /// The highest block score achieved during the season.
    /// </summary>
    public required int HighBlock { get; init; }

    /// <summary>
    /// A dictionary mapping bowler IDs to their names for those who achieved the highest block score during the season.
    /// </summary>
    public required IReadOnlyDictionary<string, string> HighBlockBowlers { get; init; }

    /// <summary>
    /// The highest average score achieved during the season.
    /// </summary>
    public required decimal HighAverage { get; init; }

    /// <summary>
    /// A dictionary mapping bowler IDs to their names for those who achieved the highest average score during the season.
    /// </summary>
    public required IReadOnlyDictionary<string, string> HighAverageBowlers { get; init; }
}