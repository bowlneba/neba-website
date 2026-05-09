namespace Neba.Api.Contracts.Stats.GetSeasonStats;

/// <summary>
/// The full stats page response for a Season. Contains all award standings, scoring and match play leaderboards,
/// efficiency metrics, sidebar summaries, the Bowler of the Year points race, and the full-stat modal data.
/// </summary>
public sealed record GetSeasonStatsResponse
{
    /// <summary>The description of the selected Season (e.g. "2024-2025 Season").</summary>
    public required string SelectedSeason { get; init; }

    /// <summary>All Seasons that have stats data available, keyed by end year, ordered most-recent first. Used to populate the season selector.</summary>
    public required IReadOnlyDictionary<int, string> AvailableSeasons { get; init; }

    /// <summary>All bowlers who participated in the Season, keyed by ULID string to display name, ordered alphabetically. Used to drive the full-stat modal bowler search.</summary>
    public required IReadOnlyDictionary<string, string> BowlerSearchList { get; init; }

    /// <summary>Minimum games required for a bowler to appear in the High Average leaderboard.</summary>
    public required decimal MinimumNumberOfGames { get; init; }

    /// <summary>Minimum eligible tournaments required for a bowler to appear in tournament-based leaderboards.</summary>
    public required decimal MinimumNumberOfTournaments { get; init; }

    /// <summary>Minimum eligible entries required for a bowler to appear in entry-based leaderboards.</summary>
    public required decimal MinimumNumberOfEntries { get; init; }

    // Award Standings

    /// <summary>Bowler of the Year (Open) standings for the Season, ordered by points descending.</summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingResponse> BowlerOfTheYear { get; init; }

    /// <summary>Senior of the Year standings for the Season (bowlers age 50+), ordered by points descending.</summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingResponse> SeniorOfTheYear { get; init; }

    /// <summary>Super Senior of the Year standings for the Season (bowlers age 60+), ordered by points descending.</summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingResponse> SuperSeniorOfTheYear { get; init; }

    /// <summary>Woman of the Year standings for the Season, ordered by points descending.</summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingResponse> WomanOfTheYear { get; init; }

    /// <summary>Rookie of the Year standings for the Season (first-year paid members), ordered by points descending.</summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingResponse> RookieOfTheYear { get; init; }

    /// <summary>Youth of the Year standings for the Season (bowlers under 18), ordered by points descending.</summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingResponse> YouthOfTheYear { get; init; }

    // Qualifying

    /// <summary>High average leaderboard for the Season, ordered by average descending. Only includes bowlers with at least one game.</summary>
    public required IReadOnlyCollection<HighAverageResponse> HighAverage { get; init; }

    /// <summary>High block leaderboard for the Season, ordered by high block descending. A block is the highest score across a 5-game qualifying window.</summary>
    public required IReadOnlyCollection<HighBlockResponse> HighBlock { get; init; }

    /// <summary>Match play average leaderboard for the Season, ordered by match play average descending. Only includes bowlers who reached Finals.</summary>
    public required IReadOnlyCollection<MatchPlayAverageResponse> MatchPlayAverage { get; init; }

    // Match Play

    /// <summary>Match play win-loss record leaderboard for the Season, ordered by win percentage descending.</summary>
    public required IReadOnlyCollection<MatchPlayRecordResponse> MatchPlayRecord { get; init; }

    /// <summary>Match play appearances leaderboard for the Season (number of Finals reached), ordered by Finals descending.</summary>
    public required IReadOnlyCollection<MatchPlayAppearancesResponse> MatchPlayAppearances { get; init; }

    // Efficiency

    /// <summary>Points per eligible entry leaderboard for the Season, ordered by points per entry descending.</summary>
    public required IReadOnlyCollection<PointsPerEntryResponse> PointsPerEntry { get; init; }

    /// <summary>Points per eligible tournament leaderboard for the Season, ordered by points per tournament descending.</summary>
    public required IReadOnlyCollection<PointsPerTournamentResponse> PointsPerTournament { get; init; }

    /// <summary>Finals per eligible entry leaderboard for the Season, ordered by finals per entry descending.</summary>
    public required IReadOnlyCollection<FinalsPerEntryResponse> FinalsPerEntry { get; init; }

    /// <summary>Average finish leaderboard for the Season, ordered by average finish ascending (lower is better).</summary>
    public required IReadOnlyCollection<AverageFinishResponse> AverageFinishes { get; init; }

    // Sidebar

    /// <summary>High-level participation summary for the Season (total entries and total prize money).</summary>
    public required SeasonAtAGlanceResponse SeasonAtAGlance { get; init; }

    /// <summary>Best individual performances for the Season (high game, high block, high average).</summary>
    public required SeasonBestsResponse SeasonsBests { get; init; }

    /// <summary>Field match play summary for the Season (best win percentage and most Finals reached).</summary>
    public required FieldMatchPlaySummaryResponse FieldMatchPlaySummary { get; init; }

    /// <summary>Open (Bowler of the Year) points race series data, showing cumulative points earned by each bowler across tournaments.</summary>
    public required IReadOnlyCollection<PointsRaceSeriesResponse> OpenPointsRace { get; init; }

    /// <summary>Senior points race series data.</summary>
    public required IReadOnlyCollection<PointsRaceSeriesResponse> SeniorPointsRace { get; init; }

    /// <summary>Super Senior points race series data.</summary>
    public required IReadOnlyCollection<PointsRaceSeriesResponse> SuperSeniorPointsRace { get; init; }

    /// <summary>Women points race series data.</summary>
    public required IReadOnlyCollection<PointsRaceSeriesResponse> WomenPointsRace { get; init; }

    /// <summary>Youth points race series data.</summary>
    public required IReadOnlyCollection<PointsRaceSeriesResponse> YouthPointsRace { get; init; }

    /// <summary>Rookie points race series data.</summary>
    public required IReadOnlyCollection<PointsRaceSeriesResponse> RookiePointsRace { get; init; }

    // Full stat modal

    /// <summary>Complete per-bowler statistics for all participants in the Season, ordered by Bowler of the Year points descending.</summary>
    public required IReadOnlyCollection<FullStatModalRowResponse> AllBowlers { get; init; }
}