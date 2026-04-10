namespace Neba.Website.Server.Stats;

/// <summary>
/// View model for a single row in the High Average table, which ranks bowlers based on their average score across all games they have played in the season. Bowlers must have played a minimum number of games to be included in this ranking, and the average is calculated as the total pinfall divided by the total number of games played. The field average represents the average score of all bowlers in the ranking, providing context for how a bowler's average compares to the overall performance of the field.
/// </summary>
public sealed record HighAverageRowViewModel
{
    /// <summary>
    /// The rank of the bowler in the High Average standings, starting at 1 for the bowler with the highest average score. Bowlers are ranked in descending order of their average score, with ties broken by total pinfall and then by number of games played.
    /// </summary>
    public required int Rank { get; init; }

    /// <summary>
    /// The unique identifier of the bowler, used for linking to the bowler's profile page. This allows users to click on the bowler's name in the High Average table and navigate to their profile for more detailed information about their performance throughout the season.
    /// </summary>
    public required Ulid BowlerId { get; init; }

    /// <summary>
    /// The name of the bowler, displayed in the High Average table and linked to the bowler's profile page. This provides a user-friendly way to identify the bowler and access more information about their performance in the season.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// The average score of the bowler across all games they have played in the season, calculated as the total pinfall divided by the total number of games played. This is the primary metric used for ranking bowlers in the High Average standings, with higher averages indicating better performance. Bowlers must have played a minimum number of games to be included in this ranking, ensuring that the averages are representative of consistent performance throughout the season.
    /// </summary>
    public required decimal Average { get; init; }

    /// <summary>
    /// The total number of games the bowler has played in the season, which is used to determine eligibility for inclusion in the High Average standings and provides context for the bowler's average score. A higher number of games indicates a more consistent performance across the season, while a lower number of games may indicate that the bowler has not participated in enough tournaments to be included in the ranking or that their average may be less representative of their overall performance.
    /// </summary>
    public required int Games { get; init; }

    /// <summary>
    /// The total pinfall of the bowler across all games they have played in the season, which is used as a tiebreaker for bowlers with the same average score and provides additional context for the bowler's performance. A higher total pinfall indicates that the bowler has scored more pins overall, which can be an indicator of better performance even if their average score is the same as another bowler with fewer total pins.
    /// </summary>
    public required int Tournaments { get; init; }

    /// <summary>
    /// The average score of all bowlers in the High Average ranking, calculated as the total pinfall of all bowlers divided by the total number of games played by all bowlers. This field provides context for how a bowler's average score compares to the overall performance of the field, allowing users to see whether a bowler's average is above or below the average score of all bowlers in the ranking.
    /// </summary>
    /// <remarks>
    /// This is stored as a differential between the bowler's average and the field average of tournaments in which they compete.
    /// </remarks>
    public required decimal FieldAverage { get; init; }
}