namespace Neba.Website.Server.Stats;

/// <summary>
/// Represents a row in the match play record statistics table, containing information about a bowler's performance in match play, including wins, losses, finals appearances, match play average, and winnings.
/// </summary>
public sealed record MatchPlayRecordRowViewModel
{
    /// <summary>
    /// Gets the rank of the bowler in the match play record standings, where 1 represents the highest rank.
    /// </summary>
    public required int Rank { get; init; }

    /// <summary>
    /// Gets the unique identifier of the bowler associated with this match play record. This identifier is used to link the record to the specific bowler's profile and statistics.
    /// </summary>
    public required string BowlerId { get; init; }

    /// <summary>
    /// Gets the name of the bowler associated with this match play record. This is typically the full name of the bowler and is used for display purposes in the statistics table.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// Gets the number of wins the bowler has achieved in match play. This statistic is used to calculate the win percentage and to evaluate the bowler's performance in match play competitions.
    /// </summary>
    public required int Wins { get; init; }

    /// <summary>
    /// Gets the number of losses the bowler has incurred in match play. This statistic, along with wins, is used to calculate the win percentage and to assess the bowler's overall performance in match play competitions.
    /// </summary>
    public required int Loses { get; init; }

    /// <summary>
    /// Gets the win percentage for the bowler in match play, calculated as (Wins / (Wins + Loses)) * 100. This statistic provides insight into the bowler's success rate in match play competitions and is rounded to two decimal places for display purposes.
    /// </summary>
    public decimal WinPercentage
        => Wins + Loses > 0
            ? decimal.Round(Wins * 100m / (Wins + Loses), 2)
            : 0m;

    /// <summary>
    /// Gets the number of finals appearances the bowler has made in match play competitions. This statistic is used to evaluate the bowler's consistency and ability to perform well in high-pressure situations, as reaching the finals is often indicative of a strong performance throughout the competition.
    /// </summary>
    public required int Finals { get; init; }

    /// <summary>
    /// Gets the match play average for the bowler, which is calculated based on the total pinfall in match play games divided by the number of games played. This statistic provides insight into the bowler's scoring performance in match play competitions and is rounded to two decimal places for display purposes.
    /// </summary>
    public required decimal MatchPlayAverage { get; init; }

    /// <summary>
    /// Gets the total winnings for the bowler in match play competitions. This statistic represents the financial success of the bowler in match play events and is typically calculated based on the prize money earned from wins, finals appearances, and overall performance in match play tournaments.
    /// </summary>
    public required decimal Winnings { get; init; }
}