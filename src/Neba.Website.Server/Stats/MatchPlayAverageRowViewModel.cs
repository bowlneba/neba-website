namespace Neba.Website.Server.Stats;

/// <summary>
/// View model for a single row in the Match Play Average table, which ranks bowlers based on their average score in match play games across all tournaments they have participated in during the season. Match play games are head-to-head competitions where bowlers compete against each other, and the average is calculated by dividing the total pinfall in match play games by the number of match play games played. Bowlers are ranked in descending order of their match play average, with ties broken by the number of wins and then by the bowler's name. This ranking provides insight into which bowlers have performed best in competitive match play situations throughout the season.
/// </summary>
public sealed record MatchPlayAverageRowViewModel
{
    /// <summary>
    /// The rank of the bowler in the Match Play Average standings, starting at 1 for the bowler with the highest match play average. Bowlers are ranked in descending order of their match play average, with ties broken by the number of wins and then by the bowler's name. This ranking provides insight into which bowlers have performed best in competitive match play situations throughout the season.
    /// </summary>
    public required int Rank { get; init; }

    /// <summary>
    /// The unique identifier of the bowler, used for linking to the bowler's profile page. This allows users to click on the bowler's name in the Match Play Average table and navigate to their profile for more detailed information about their performance throughout the season.
    /// </summary>
    public required Ulid BowlerId { get; init; }

    /// <summary>
    /// The name of the bowler, displayed in the Match Play Average table and linked to the bowler's profile page. This provides a user-friendly way to identify the bowler and access more information about their performance in the season.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// The average score of the bowler in match play games across all tournaments they have participated in during the season, calculated by dividing the total pinfall in match play games by the number of match play games played. This is the primary metric used for ranking bowlers in the Match Play Average standings, with higher averages indicating better performance in competitive match play situations. Bowlers are ranked in descending order of their match play average, with ties broken by the number of wins and then by the bowler's name.
    /// </summary>
    public required decimal MatchPlayAverage { get; init; }

    /// <summary>
    /// The total number of match play games the bowler has played across all tournaments in the season. This provides context for the bowler's match play average, as a higher number of games can indicate more experience and consistency in match play situations. This field is also used as a tiebreaker for bowlers with the same match play average, with more games indicating better performance.
    /// </summary>
    public required int Games { get; init; }

    /// <summary>
    /// The total number of wins the bowler has achieved in match play games across all tournaments in the season. This is used as a tiebreaker for bowlers with the same match play average, with more wins indicating better performance in competitive match play situations. This field provides additional context for the bowler's performance in the Match Play Average standings, allowing users to see not only the average score but also the success rate in terms of wins.
    /// </summary>
    public required int Wins { get; init; }

    /// <summary>
    /// The total number of losses the bowler has incurred in match play games across all tournaments in the season. This provides additional context for the bowler's performance in the Match Play Average standings, allowing users to see not only the average score and wins but also the number of losses in competitive match play situations. This field can also be used to calculate the win percentage for the bowler in match play games.
    /// </summary>
    public required int Loses { get; init; }

    /// <summary>
    /// The win percentage of the bowler in match play games across all tournaments in the season, calculated by dividing the number of wins by the total number of games played and multiplying by 100 to get a percentage. This provides a quick and easy way to assess the bowler's success rate in competitive match play situations, with higher percentages indicating better performance. This field is nullable because if a bowler has not played any match play games, the win percentage cannot be calculated and should be represented as null.
    /// </summary>
    public decimal? WinPercentage
        => Games > 0
            ? decimal.Round(Wins * 100m / Games, 2)
            : null;

    /// <summary>
    /// The total winnings the bowler has earned across all tournaments in the season. This provides additional context for the bowler's performance in the Match Play Average standings, allowing users to see not only the average score and win percentage but also the financial success in competitive match play situations. This field can also be used to compare the earnings of different bowlers in relation to their match play performance.
    /// </summary>
    public decimal Winnings { get; init; }
}