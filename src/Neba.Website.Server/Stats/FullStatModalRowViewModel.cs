namespace Neba.Website.Server.Stats;

/// <summary>
/// View model for a single row in the full stat modal. Contains all the necessary information to display the stats for a single bowler.
/// </summary>
public sealed record FullStatModalRowViewModel
{
    /// <summary>
    /// The rank of the bowler in the current season. This is calculated based on the points and average of the bowler compared to other bowlers in the same season.
    /// </summary>
    public required int Rank { get; init; }

    /// <summary>
    /// The unique identifier of the bowler. This is used to link to the bowler's profile page and to fetch additional information about the bowler if needed.
    /// </summary>
    public required string BowlerId { get; init; }

    /// <summary>
    /// The name of the bowler. This is displayed in the full stat modal and is also used to link to the bowler's profile page.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// The points of the bowler for the current season. This is calculated based on the bowler's performance in the current season and is used to determine the bowler's rank in the current season.
    /// </summary>
    public required int Points { get; init; }

    /// <summary>
    /// The average of the bowler for the current season. This is calculated based on the bowler's performance in the current season and is used to determine the bowler's rank in the current season.
    /// </summary>
    public required decimal Average { get; init; }

    /// <summary>
    /// The number of games played by the bowler in the current season. This is used to determine if the bowler has played enough games to be ranked in the current season.
    /// </summary>
    public required int Games { get; init; }

    /// <summary>
    /// This is the number of finals the bowler has reached in the current season. This is used to determine the bowler's performance in the current season and is also used to determine the bowler's rank in the current season.
    /// </summary>
    public required int Finals { get; init; }

    /// <summary>
    /// This is the number of wins the bowler has in the current season. This is used to determine the bowler's performance in the current season and is also used to determine the bowler's rank in the current season.
    /// </summary>
    public int Wins { get; init; }

    /// <summary>
    /// This is the number of losses the bowler has in the current season. This is used to determine the bowler's performance in the current season and is also used to determine the bowler's rank in the current season.
    /// </summary>
    public int Loses { get; init; }

    /// <summary>
    /// This is the win percentage of the bowler in the current season. This is calculated based on the number of wins and losses of the bowler in the current season and is used to determine the bowler's performance in the current season and is also used to determine the bowler's rank in the current season.
    /// </summary>
    public decimal? WinPercentage
        => (Wins + Loses) > 0 ? (decimal)Wins / (Wins + Loses) : null;

    /// <summary>
    /// This is the match play average of the bowler in the current season. This is calculated based on the bowler's performance in match play games in the current season and is used to determine the bowler's performance in match play games in the current season and is also used to determine the bowler's rank in match play games in the current season.
    /// </summary>
    public decimal? MatchPlayAverage { get; init; }

    /// <summary>
    /// This is the total winnings of the bowler in the current season. This is calculated based on the bowler's performance in the current season and is used to determine the bowler's performance in the current season and is also used to determine the bowler's rank in the current season.
    /// </summary>
    public decimal Winnings { get; init; }

    /// <summary>
    /// This compares the bowler's average to the average of the field in which the bowler has competed. This is calculated based on the bowler's average and the average of the field in which the bowler has competed and is used to determine the bowler's performance compared to the field in which the bowler has competed and is also used to determine the bowler's rank compared to the field in which the bowler has competed.
    /// </summary>
    public decimal FieldAverage { get; init; }

    /// <summary>
    /// This is the number of tournaments the bowler has competed in during the current season. This is used to determine the bowler's experience and consistency in the current season and is also used to determine the bowler's rank in the current season.
    /// </summary>
    public required int Touranments { get; init; }
}