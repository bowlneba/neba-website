namespace Neba.Website.Server.Stats;

/// <summary>
/// View model for a single row in the Bowler of the Year standings table.
/// </summary>
public sealed record BowlerOfTheYearStandingRowViewModel
{
    /// <summary>
    /// The rank of the bowler in the standings, starting at 1 for the top-ranked bowler.
    /// </summary>
    public required int Rank { get; init; }

    /// <summary>
    /// The unique identifier of the bowler, used for linking to the bowler's profile page.
    /// </summary>
    public required Ulid BowlerId { get; init; }

    /// <summary>
    /// The name of the bowler, displayed in the standings table and linked to the bowler's profile page.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// The total number of points the bowler has accumulated during the season, used for ranking in the standings.
    /// </summary>
    public required int Points { get; init; }

    /// <summary>
    /// The number of tournaments the bowler has participated in during the season.
    /// </summary>
    public required int Tournaments { get; init;}

    /// <summary>
    /// The number of tournament entries the bowler has made during the season.
    /// </summary>
    public required int Entries { get; init;}

    /// <summary>
    /// The number of times the bowler has advanced to the finals of a tournament during the season.
    /// </summary>
    public required int Finals {get; init;}

    /// <summary>
    /// The average finish position of the bowler in the tournaments they have participated in during the season.
    /// </summary>
    /// <remarks>
    /// This is calculated as the total of the bowler's finish positions in all tournaments divided by the number of tournaments they have participated in. A lower average finish indicates better performance.
    /// Previously, if a bowler had not advanced to the finals, their finishing position was not tracked, and will be null.  Currently, all bowlers have their finishing position tracked, so this will be null only if the bowler has not participated in any tournaments.
    /// </remarks>
    public decimal? AverageFinish {get; init;}

    /// <summary>
    /// The total winnings of the bowler during the season, calculated as the sum of the prize money they have earned from all tournaments they have participated in. This is used for informational purposes and is not a factor in the bowler's ranking in the standings.
    /// </summary>
    public required decimal Winnings { get; init; }
}