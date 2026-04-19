namespace Neba.Website.Server.Stats;

/// <summary>
/// View model for the individual stats page for a bowler.
/// </summary>
public record IndividualStatsPageViewModel
{
    /// <summary>
    /// The unique identifier for the bowler.
    /// </summary>
    public required string BowlerId { get; init; }

    /// <summary>
    /// The name of the bowler.
    /// </summary>
    public required string BowlerName { get; init; }

    /// <summary>
    /// The currently selected season for the bowler.
    /// </summary>
    public required string SelectedSeason { get; init; }

    /// <summary>
    /// The available seasons for the bowler.
    /// </summary>
    public required IReadOnlyDictionary<int, string> AvailableSeasons { get; init; }

    // Season stats

    /// <summary>
    /// The total points for the bowler in the selected season.
    /// </summary>
    public required int Points { get; init; }

    /// <summary>
    /// The average score for the bowler in the selected season.
    /// </summary>
    public required decimal Average { get; init; }

    /// <summary>
    /// The number of games the bowler has played in the selected season.
    /// </summary>
    public required int Games { get; init; }

    /// <summary>
    /// The number of finals the bowler has reached in the selected season.
    /// </summary>
    public required int Finals { get; init; }

    /// <summary>
    /// The number of entries the bowler has in the selected season.
    /// </summary>
    public required int Entries { get; init; }

    /// <summary>
    /// The number of tournaments the bowler has participated in the selected season.
    /// </summary>
    public required int Tournaments { get; init; }

    /// <summary>
    /// The total winnings for the bowler in the selected season.
    /// </summary>
    public required decimal Winnings { get; init; }

    /// <summary>
    /// Differential between this bowler's average and the field average of tournaments they competed in.
    /// Positive = above field average.
    /// </summary>
    public required decimal FieldAverage { get; init; }

    /// <summary>
    /// The number of match play wins for the bowler in the selected season.
    /// </summary>
    public required int MatchPlayWins { get; init; }

    /// <summary>
    /// The number of match play losses for the bowler in the selected season.
    /// </summary>
    public required int MatchPlayLosses { get; init; }

    /// <summary>
    /// The average score for the bowler in match play games in the selected season.
    /// </summary>
    public required decimal? MatchPlayAverage { get; init; }

    /// <summary>
    /// The win percentage for the bowler in match play games in the selected season. This is calculated based on the number of match play wins and losses for the bowler in the selected season and is used to determine the bowler's performance in match play games in the selected season and is also used to determine the bowler's rank in match play games in the selected season.
    /// </summary>
    public decimal? WinPercentage
        => (MatchPlayWins + MatchPlayLosses) > 0
            ? decimal.Round(MatchPlayWins * 100m / (MatchPlayWins + MatchPlayLosses), 1)
            : null;

    // Rankings — null means not ranked / not eligible in this category

    /// <summary>
    /// The rank of the bowler in the Bowler of the Year race for the selected season. This is calculated based on the points of the bowler in the selected season and is used to determine the bowler's performance in the Bowler of the
    /// </summary>
    public int? BowlerOfTheYearRank { get; init; }

    /// <summary>
    /// The rank of the bowler in the Senior Bowler of the Year race for the selected season. This is calculated based on the points of the bowler in the selected season and is used to determine the bowler's performance in the Senior Bowler
    /// </summary>
    public int? SeniorOfTheYearRank { get; init; }

    /// <summary>
    /// The rank of the bowler in the Super Senior Bowler of the Year race for the selected season. This is calculated based on the points of the bowler in the selected season and is used to determine the bowler's performance in the Super Senior
    /// </summary>
    public int? SuperSeniorOfTheYearRank { get; init; }

    /// <summary>
    /// The rank of the bowler in the Woman of the Year race for the selected season. This is calculated based on the points of the bowler in the selected season and is used to determine the bowler's performance
    /// </summary>
    public int? WomanOfTheYearRank { get; init; }

    /// <summary>
    /// The rank of the bowler in the Rookie of the Year race for the selected season. This is calculated based on the points of the bowler in the selected season and is used to determine the bowler's performance in the Rookie of the
    /// </summary>
    public int? RookieOfTheYearRank { get; init; }

    /// <summary>
    /// The rank of the bowler in the Youth of the Year race for the selected season. This is calculated based on the points of the bowler in the selected season and is used to determine the bowler's performance in the Youth of the
    /// </summary>
    public int? YouthOfTheYearRank { get; init; }

    /// <summary>
    /// The rank of the bowler in the High Average category for the selected season. This is calculated based on the average of the bowler in the selected season and is used to determine the bowler's performance in the High Average category for the selected season and is also used to determine the bowler's rank in the High Average category for the selected season.
    /// </summary>
    public int? HighAverageRank { get; init; }

    /// <summary>
    /// The rank of the bowler in the High Block category for the selected season. This is calculated based on the average of the bowler in the selected season and is used to determine the bowler's performance in the High Block category for the selected season and is also used to determine the bowler's rank in the High Block category for the selected season.
    /// </summary>
    public int? HighBlockRank { get; init; }

    /// <summary>
    /// The rank of the bowler in the Match Play Average category for the selected season. This is calculated based on the match play average of the bowler in the selected season and is used to determine the bowler's performance in the Match Play Average category for the selected season and is also used to determine the bowler's rank in the Match Play Average category for the selected season.
    /// </summary>
    public int? MatchPlayAverageRank { get; init; }

    /// <summary>
    /// The rank of the bowler in the Match Play Record category for the selected season. This is calculated based on the win percentage of the bowler in match play games in the selected season and is used to determine the bowler's performance in the Match Play Record category for the selected season and is also used to determine the bowler's rank in the Match Play Record category for the selected season.
    /// </summary>
    public int? MatchPlayRecordRank { get; init; }

    /// <summary>
    /// The rank of the bowler in the Match Play Appearances category for the selected season. This is calculated based on the number of match play appearances of the bowler in the selected season and is used to determine the bowler's performance in the Match Play Appearances category for the selected season and is also used to determine the bowler's rank in the Match Play Appearances category for the selected season.
    /// </summary>
    public int? MatchPlayAppearancesRank { get; init; }

    /// <summary>
    /// The rank of the bowler in the Winnings category for the selected season. This is calculated based on the winnings of the bowler in the selected season and is used to determine the bowler's performance in the Winnings category for the selected season and is also used to determine the bowler's rank in the Winnings category for the selected season.
    /// </summary>
    public int? PointsPerEntryRank { get; init; }

    /// <summary>
    /// The rank of the bowler in the Points Per Tournament category for the selected season. This is calculated based on the points per tournament of the bowler in the selected season and is used to determine the bowler's performance in the Points Per Tournament category for the selected season and is also used to determine the bowler's rank in the Points Per Tournament category for the selected season.
    /// </summary>
    public int? PointsPerTournamentRank { get; init; }

    /// <summary>
    /// The rank of the bowler in the Finals Per Entry category for the selected season. This is calculated based on the finals per entry of the bowler in the selected season and is used to determine the bowler's performance in the Finals Per Entry category for the selected season and is also used to determine the bowler's rank in the Finals Per Entry category for the selected season.
    /// </summary>
    public int? FinalsPerEntryRank { get; init; }

    /// <summary>
    /// The rank of the bowler in the Average Finish category for the selected season. This is calculated based on the average finish of the bowler in the selected season and is used to determine the bowler's performance in the Average Finish category for the selected season and is also used to determine the bowler's rank in the Average Finish category for the selected season.
    /// </summary>
    public int? AverageFinishRank { get; init; }

    /// <summary>
    /// The points race series information for the bowler in the Bowler of the Year race for the selected season. This is used to determine the bowler's performance in the Bowler of the Year
    /// </summary>
    public PointsRaceSeriesViewModel? BowlerOfTheYearPointsRace { get; init; }
}