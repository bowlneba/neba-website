namespace Neba.Website.Server.Stats;

/// <summary>
/// View model for the stats page. Contains all the necessary information to display the stats page, including the award standings, averages &amp; scoring, match play, efficiency, minimum thresholds, sidebar information, and the full stat modal.
/// </summary>
public record StatsPageViewModel
{
    /// <summary>
    /// The name of the selected season. This is used to display the selected season in the UI and to fetch the stats for the selected season. The season is represented as a string in the format "YYYY-YYYY" (e.g. "2023-2024") to make it more user-friendly and easier to display in the UI. The actual season ID is stored in the AvailableSeasons dictionary, which maps the season ID (Ulid) to the season name (string).
    /// </summary>
    public required string SelectedSeason { get; init; }

    /// <summary>
    /// A dictionary of available seasons, where the key is the season ID (Ulid) and the value is the season name (string). This is used to populate the season dropdown in the UI and to fetch the stats for the selected season when the user selects a different season from the dropdown. The season name is represented as a string in the format "YYYY-YYYY" (e.g. "2023-2024") to make it more user-friendly and easier to display in the UI. The actual season ID is stored in the key of the dictionary, which is used to fetch the stats for the selected season when the user selects a different season from the dropdown.
    /// </summary>
    public required IReadOnlyDictionary<Ulid, string> AvailableSeasons { get; init; }

    /// <summary>
    /// A dictionary of bowlers, where the key is the bowler ID (Ulid) and the value is the bowler name (string). This is used to populate the bowler search list in the full stat modal and to link to the bowler's profile page when the user clicks on a bowler's name in the full stat modal. The bowler name is represented as a string to make it more user-friendly and easier to display in the UI. The actual bowler ID is stored in the key of the dictionary, which is used to link to the bowler's profile page when the user clicks on a bowler's name in the full stat modal.
    /// </summary>
    public required IReadOnlyDictionary<Ulid, string> BowlerSearchList { get; init; }

    // Award Standings

    /// <summary>
    /// A collection of bowler of the year standings, where each item in the collection represents a single bowler's standing in the bowler of the year. This is used to display the bowler of the year standings in the UI and to determine the bowler of the year for the selected season. Each item in the collection contains information about the bowler's rank, points, average, games played, finals reached, wins, losses, win percentage, match play average, and other relevant information that is used to determine the bowler's standing in the bowler of the year and to display the bowler's performance in the selected season.
    /// </summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingRowViewModel> BowlerOfTheYear { get; init; }

    /// <summary>
    /// A collection of senior of the year standings, where each item in the collection represents a single bowler's standing in the senior of the year. This is used to display the senior of the year standings in the UI and to determine the senior of the year for the selected season. Each item in the collection contains information about the bowler's rank, points, average, games played, finals reached, wins, losses, win percentage, match play average, and other relevant information that is used to determine the bowler's standing in the senior of the year and to display the bowler's performance in the selected season.
    /// </summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingRowViewModel> SeniorOfTheYear { get; init; }

    /// <summary>
    /// A collection of super senior of the year standings, where each item in the collection represents a single bowler's standing in the super senior of the year. This is used to display the super senior of the year standings in the UI and to determine the super senior of the year for the selected season. Each item in the collection contains information about the bowler's rank, points, average, games played, finals reached, wins, losses, win percentage, match play average, and other relevant information that is used to determine the bowler's standing in the super senior of the year and to display the bowler's performance in the selected season.
    /// </summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingRowViewModel> SuperSeniorOfTheYear { get; init; }

    /// <summary>
    /// A collection of woman of the year standings, where each item in the collection represents a single bowler's standing in the woman of the year. This is used to display the woman of the year standings in the UI and to determine the woman of the year for the selected season. Each item in the collection contains information about the bowler's rank, points, average, games played, finals reached, wins, losses, win percentage, match play average, and other relevant information that is used to determine the bowler's standing in the woman of the year and to display the bowler's performance in the selected season.
    /// </summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingRowViewModel> WomanOfTheYear { get; init; }

    /// <summary>
    /// A collection of rookie of the year standings, where each item in the collection represents a single bowler's standing in the rookie of the year. This is used to display the rookie of the year standings in the UI and to determine the rookie of the year for the selected season. Each item in the collection contains information about the bowler's rank, points, average, games played, finals reached, wins, losses, win percentage, match play average, and other relevant information that is used to determine the bowler's standing in the rookie of the year and to display the bowler's performance in the selected season.
    /// </summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingRowViewModel> RookieOfTheYear { get; init; }

    /// <summary>
    /// A collection of youth of the year standings, where each item in the collection represents a single bowler's standing in the youth of the year. This is used to display the youth of the year standings in the UI and to determine the youth of the year for the selected season. Each item in the collection contains information about the bowler's rank, points, average, games played, finals reached, wins, losses, win percentage, match play average, and other relevant information that is used to determine the bowler's standing in the youth of the year and to display the bowler's performance in the selected season.
    /// </summary>
    public required IReadOnlyCollection<BowlerOfTheYearStandingRowViewModel> YouthOfTheYear { get; init; }

    // Averages & Scoring

    /// <summary>
    /// A collection of high average rows, where each item in the collection represents a single bowler's high average for the selected season. This is used to display the high average leaderboard in the UI and to determine the bowlers with the highest averages for the selected season. Each item in the collection contains information about the bowler's rank, name, average, games played, and other relevant information that is used to determine the bowler's standing in the high average leaderboard and to display the bowler's performance in terms of average for the selected season.
    /// </summary>
    public required IReadOnlyCollection<HighAverageRowViewModel> HighAverage { get; init; }

    /// <summary>
    /// A collection of high block rows, where each item in the collection represents a single bowler's high block for the selected season. This is used to display the high block leaderboard in the UI and to determine the bowlers with the highest blocks for the selected season. Each item in the collection contains information about the bowler's rank, name, block average, games played, and other relevant information that is used to determine the bowler's standing in the high block leaderboard and to display the bowler's performance in terms of high block for the selected season.
    /// </summary>
    public required IReadOnlyCollection<HighBlockRowViewModel> HighBlock { get; init; }

    /// <summary>
    /// A collection of match play average rows, where each item in the collection represents a single bowler's match play average for the selected season. This is used to display the match play average leaderboard in the UI and to determine the bowlers with the highest match play averages for the selected season. Each item in the collection contains information about the bowler's rank, name, match play average, match play games played, and other relevant information that is used to determine the bowler's standing in the match play average leaderboard and to display the bowler's performance in terms of match play average for the selected season.
    /// </summary>
    public required IReadOnlyCollection<MatchPlayAverageRowViewModel> MatchPlayAverage { get; init; }

    // Match Play

    /// <summary>
    /// A collection of match play record rows, where each item in the collection represents a single bowler's match play record for the selected season. This is used to display the match play record leaderboard in the UI and to determine the bowlers with the best match play records for the selected season. Each item in the collection contains information about the bowler's rank, name, wins, losses, win percentage, and other relevant information that is used to determine the bowler's standing in the match play record leaderboard and to display the bowler's performance in terms of match play record for the selected season.
    /// </summary>
    public required IReadOnlyCollection<MatchPlayRecordRowViewModel> MatchPlayRecord { get; init; }

    /// <summary>
    /// A collection of match play appearances rows, where each item in the collection represents a single bowler's match play appearances for the selected season. This is used to display the match play appearances leaderboard in the UI and to determine the bowlers with the most match play appearances for the selected season. Each item in the collection contains information about the bowler's rank, name, match play appearances, and other relevant information that is used to determine the bowler's standing in the match play appearances leaderboard and to display the bowler's performance in terms of match play appearances for the selected season.
    /// </summary>
    public required IReadOnlyCollection<MatchPlayAppearancesRowViewModel> MatchPlayAppearances { get; init; }


    // Efficiency

    /// <summary>
    /// A collection of points per entry rows, where each item in the collection represents a single bowler's points per entry for the selected season. This is used to display the points per entry leaderboard in the UI and to determine the bowlers with the highest points per entry for the selected season. Each item in the collection contains information about the bowler's rank, name, points per entry, and other relevant information that is used to determine the bowler's standing in the points per entry leaderboard and to display the bowler's performance in terms of points per entry for the selected season.
    /// </summary>
    public required IReadOnlyCollection<PointsPerEntryRowViewModel> PointsPerEntry { get; init; }

    /// <summary>
    /// A collection of points per tournament rows, where each item in the collection represents a single bowler's points per tournament for the selected season. This is used to display the points per tournament leaderboard in the UI and to determine the bowlers with the highest points per tournament for the selected season. Each item in the collection contains information about the bowler's rank, name, points per tournament, and other relevant information that is used to determine the bowler's standing in the points per tournament leaderboard and to display the bowler's performance in terms of points per tournament for the selected season.
    /// </summary>
    public required IReadOnlyCollection<PointsPerTournamentRowViewModel> PointsPerTournament { get; init; }

    /// <summary>
    /// A collection of finals per entry rows, where each item in the collection represents a single bowler's finals per entry for the selected season. This is used to display the finals per entry leaderboard in the UI and to determine the bowlers with the highest finals per entry for the selected season. Each item in the collection contains information about the bowler's rank, name, finals per entry, and other relevant information that is used to determine the bowler's standing in the finals per entry leaderboard and to display the bowler's performance in terms of finals per entry for the selected season.
    /// </summary>
    public required IReadOnlyCollection<FinalsPerEntryRowViewModel> FinalsPerEntry { get; init; }

    /// <summary>
    /// A collection of average finish rows, where each item in the collection represents a single bowler's average finish for the selected season. This is used to display the average finish leaderboard in the UI and to determine the bowlers with the highest average finish for the selected season. Each item in the collection contains information about the bowler's rank, name, average finish, and other relevant information that is used to determine the bowler's standing in the average finish leaderboard and to display the bowler's performance in terms of average finish for the selected season.
    /// </summary>
    public required IReadOnlyCollection<AverageFinishRowViewModel> AverageFinishes { get; init; }


    // Minimum Threshholds

    /// <summary>
    /// The minimum number of games required to be eligible for the high average leaderboard. This is used to determine if a bowler is eligible to be ranked in the high average leaderboard for the selected season. If a bowler has played fewer games than the minimum threshold, they will not be ranked in the high average leaderboard, even if their average is high enough to be ranked. This is used to ensure that only bowlers who have played a sufficient number of games are ranked in the high average leaderboard, which helps to maintain the integrity of the leaderboard and to ensure that it accurately reflects the performance of bowlers who have participated in a significant portion of the season.
    /// </summary>
    public required int MinGamesHighAverage { get; init; }

    /// <summary>
    /// The minimum number of games required to be eligible for the high block leaderboard. This is used to determine if a bowler is eligible to be ranked in the high block leaderboard for the selected season. If a bowler has played fewer games than the minimum threshold, they will not be ranked in the high block leaderboard, even if their block average is high enough to be ranked. This is used to ensure that only bowlers who have played a sufficient number of games are ranked in the high block leaderboard, which helps to maintain the integrity of the leaderboard and to ensure that it accurately reflects the performance of bowlers who have participated in a significant portion of the season.
    /// </summary>
    public required int MinMatchPlayAppearances { get; init; }

    /// <summary>
    /// The minimum number of entries required to be eligible for the bowler of the year standings. This is used to determine if a bowler is eligible to be ranked in the bowler of the year standings for the selected season. If a bowler has fewer entries than the minimum threshold, they will not be ranked in the bowler of the year standings, even if their points and average are high enough to be ranked. This is used to ensure that only bowlers who have participated in a sufficient number of tournaments are ranked in the bowler of the year standings, which helps to maintain the integrity of the standings and to ensure that it accurately reflects the performance of bowlers who have participated in a significant portion of the season.
    /// </summary>
    public required int MinEntries { get; init; }


    // Sidebar

    /// <summary>
    /// The season at a glance information for the selected season. This contains information about the number of tournaments, total entries, average entries per tournament, and other relevant information that provides an overview of the season at a glance. This is used to display the season at a glance information in the sidebar of the stats page and to provide users with a quick overview of the season's activity and participation levels. The information in this view model is calculated based on the data for the selected season and is used to give users insights into the overall trends and patterns of the season.
    /// </summary>
    public required SeasonAtAGlanceViewModel SeasonAtAGlance { get; init; }

    /// <summary>
    /// The season's bests for the selected season. This contains information about the best performances in various categories for the selected season, such as the highest average, highest block average, most points in a tournament, and other relevant information that highlights the standout performances of the season. This is used to display the season's bests in the sidebar of the stats page and to provide users with insights into the top performances of the season. The information in this view model is calculated based on the data for the selected season and is used to showcase the exceptional achievements of bowlers during the season.
    /// </summary>
    public required SeasonBestsViewModel SeasonsBests { get; init; }

    /// <summary>
    /// The field match play summary for the selected season. This contains information about the performance of bowlers in field match play for the selected season, including the number of match play appearances, wins, losses, and other relevant information that summarizes the field match play performance of bowlers during the season. This is used to display the field match play summary in the sidebar of the stats page and to provide users with insights into how bowlers performed in field match play during the season. The information in this view model is calculated based on the data for the selected season and is used to give users an overview of the field match play landscape for the season.
    /// </summary>
    public required FieldMatchPlaySummaryViewModel FieldMatchPlaySummary { get; init; }

    /// <summary>
    /// A collection of bowler of the year points race series, where each item in the collection represents a single bowler's points race performance for the selected season. This is used to display the bowler of the year points race in the sidebar of the stats page and to provide users with insights into how bowlers performed in the bowler of the year points race. Each item in the collection contains information about the bowler's name, points earned in each tournament, and other relevant information that is used to determine the bowler's performance in the bowler of the
    /// </summary>
    public required IReadOnlyCollection<PointsRaceSeriesViewModel> BowlerOfTheYearPointsRace { get; init; }


    // Full stat modal

    /// <summary>
    /// A collection of full stat modal rows, where each item in the collection represents a single bowler's full stat modal information for the selected season. This is used to display the full stat modal for bowlers in the UI and to provide users with detailed information about a bowler's performance in the selected season when they click on a bowler's name in the full stat modal. Each item in the collection contains information about the bowler's rank, name, points, average, games played, finals reached, wins, losses, win percentage, match play average, and other relevant information that is used to determine the bowler's standing in the selected season and to display the bowler's performance in detail in the full stat modal.
    /// </summary>
    public required IReadOnlyCollection<FullStatModalRowViewModel> AllBowlers { get; init; }
}