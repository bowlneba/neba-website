using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Stats.GetSeasonStats;

/// <summary>
/// Aggregated and pre-computed season statistics derived from the collection of per-bowler stats.
/// All leaderboards are filtered, sorted, and have derived fields (averages, ratios) pre-calculated
/// so that callers perform only field mapping, not computation.
/// </summary>
public sealed record SeasonStatsSummaryDto
{
    // Season At A Glance

    /// <summary>
    /// Total tournament entries across all bowlers and tournaments in the season.
    /// </summary>
    public required int TotalEntries { get; init; }

    /// <summary>
    /// Total prize money paid out across all tournaments in the season.
    /// </summary>
    public required decimal TotalPrizeMoney { get; init; }

    // Season Bests

    /// <summary>
    /// The highest single qualifying game score bowled during the season.
    /// </summary>
    public required int HighGame { get; init; }

    /// <summary>
    /// Bowlers who share the season high game, keyed by BowlerId.
    /// </summary>
    public required IReadOnlyDictionary<BowlerId, Name> HighGameBowlers { get; init; }

    /// <summary>
    /// The highest 5-game qualifying block score bowled during the season.
    /// </summary>
    public required int HighBlock { get; init; }

    /// <summary>
    /// Bowlers who share the season high block, keyed by BowlerId.
    /// </summary>
    public required IReadOnlyDictionary<BowlerId, Name> HighBlockBowlers { get; init; }

    /// <summary>
    /// The highest season-long pin average achieved during the season.
    /// </summary>
    public required decimal HighAverage { get; init; }

    /// <summary>
    /// Bowlers who share the season high average, keyed by BowlerId.
    /// </summary>
    public required IReadOnlyDictionary<BowlerId, Name> HighAverageBowlers { get; init; }

    // Field Match Play Summary

    /// <summary>
    /// The highest match play win percentage achieved by any bowler during the season.
    /// </summary>
    public required decimal HighestMatchPlayWinPercentage { get; init; }

    /// <summary>
    /// Bowlers who share the highest match play win percentage, keyed by BowlerId.
    /// </summary>
    public required IReadOnlyDictionary<BowlerId, Name> HighestMatchPlayWinPercentageBowlers { get; init; }

    /// <summary>
    /// The highest number of Finals appearances by any bowler during the season.
    /// </summary>
    public required int MostFinals { get; init; }

    /// <summary>
    /// Bowlers who share the most Finals appearances, keyed by BowlerId.
    /// </summary>
    public required IReadOnlyDictionary<BowlerId, Name> MostFinalsBowlers { get; init; }

    // Award Standings (ordered by points descending, filtered to category and points > 0)

    /// <summary>
    /// Bowler of the Year (Open) standings, ordered by points descending.
    /// </summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingDto> BowlerOfTheYear { get; init; }

    /// <summary>
    /// Senior of the Year standings, ordered by points descending.
    /// </summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingDto> SeniorOfTheYear { get; init; }

    /// <summary>
    /// Super Senior of the Year standings, ordered by points descending.
    /// </summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingDto> SuperSeniorOfTheYear { get; init; }

    /// <summary>
    /// Woman of the Year standings, ordered by points descending.
    /// </summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingDto> WomanOfTheYear { get; init; }

    /// <summary>
    /// Rookie of the Year standings, ordered by points descending.
    /// </summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingDto> RookieOfTheYear { get; init; }

    /// <summary>
    /// Youth of the Year standings, ordered by points descending.
    /// </summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingDto> YouthOfTheYear { get; init; }

    // Bowler Search List (ordered alphabetically by last name, then first name)

    /// <summary>
    /// All bowlers who participated in the season, ordered alphabetically by last name then first name.
    /// </summary>
    public required IReadOnlyCollection<BowlerSearchEntryDto> BowlerSearchList { get; init; }

    // Leaderboards (pre-filtered, pre-sorted, derived fields pre-computed)

    /// <summary>
    /// High Average leaderboard entries, ordered descending by average.
    /// </summary>
    public required IReadOnlyCollection<HighAverageDto> HighAverageLeaderboard { get; init; }

    /// <summary>
    /// High Block leaderboard entries, ordered descending by block score.
    /// </summary>
    public required IReadOnlyCollection<HighBlockDto> HighBlockLeaderboard { get; init; }

    /// <summary>
    /// Match Play Average leaderboard entries, ordered descending by match play average.
    /// </summary>
    public required IReadOnlyCollection<MatchPlayAverageDto> MatchPlayAverageLeaderboard { get; init; }

    /// <summary>
    /// Match Play Record leaderboard entries, ordered descending by win percentage.
    /// </summary>
    public required IReadOnlyCollection<MatchPlayRecordDto> MatchPlayRecordLeaderboard { get; init; }

    /// <summary>
    /// Match Play Appearances leaderboard entries, ordered descending by Finals count.
    /// </summary>
    public required IReadOnlyCollection<MatchPlayAppearancesDto> MatchPlayAppearancesLeaderboard { get; init; }

    /// <summary>
    /// Points per Entry leaderboard entries, ordered descending by ratio.
    /// </summary>
    public required IReadOnlyCollection<PointsPerEntryDto> PointsPerEntryLeaderboard { get; init; }

    /// <summary>
    /// Points per Tournament leaderboard entries, ordered descending by ratio.
    /// </summary>
    public required IReadOnlyCollection<PointsPerTournamentDto> PointsPerTournamentLeaderboard { get; init; }

    /// <summary>
    /// Finals per Entry leaderboard entries, ordered descending by ratio.
    /// </summary>
    public required IReadOnlyCollection<FinalsPerEntryDto> FinalsPerEntryLeaderboard { get; init; }

    /// <summary>
    /// Average Finishes leaderboard entries, ordered ascending by average finish position.
    /// </summary>
    public required IReadOnlyCollection<AverageFinishDto> AverageFinishesLeaderboard { get; init; }

    // Full Stat Modal (all bowlers ordered by Bowler of the Year points descending)

    /// <summary>
    /// All bowlers with full stats for the season, ordered descending by Bowler of the Year points.
    /// </summary>
    public required IReadOnlyCollection<FullStatModalRowDto> AllBowlers { get; init; }
}