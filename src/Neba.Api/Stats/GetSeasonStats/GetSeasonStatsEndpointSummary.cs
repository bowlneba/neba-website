using System.Net.Mime;

using FastEndpoints;

using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.Api.Stats.GetSeasonStats;

internal sealed class GetSeasonStatsEndpointSummary : Summary<GetSeasonStatsEndpoint>
{
    public GetSeasonStatsEndpointSummary()
    {
        const string janeSmithBowlerId = "01JWXYZEXAMPLE000000000002";
        const string janeSmithBowlerName = "Jane Smith";

        Summary = "Gets season statistics.";
        Description = "Returns the full statistics page data for a season, including award standings, averages, match play, efficiency leaderboards, sidebar summaries, and the bowler of the year points race. Omit the year to retrieve the most recent season with stats.";

        Response(200, "The season statistics.",
            contentType: MediaTypeNames.Application.Json,
            example: new GetSeasonStatsResponse
            {
                SelectedSeason = "2024-2025 Season",
                MinimumNumberOfGames = 45m,
                MinimumNumberOfTournaments = 5m,
                MinimumNumberOfEntries = 7.5m,
                AvailableSeasons = new Dictionary<int, string> { { 2025, "2024-2025 Season" } },
                BowlerSearchList = new Dictionary<string, string> { { janeSmithBowlerId, janeSmithBowlerName } },
                BowlerOfTheYear = [new BowlerOfTheYearStandingResponse { BowlerId = janeSmithBowlerId, BowlerName = janeSmithBowlerName, Points = 320, Tournaments = 10, Entries = 12, Finals = 5, AverageFinish = 3.2m, Winnings = 1500m }],
                SeniorOfTheYear = [],
                SuperSeniorOfTheYear = [],
                WomanOfTheYear = [],
                RookieOfTheYear = [],
                YouthOfTheYear = [],
                HighAverage = [new HighAverageResponse { BowlerId = janeSmithBowlerId, BowlerName = janeSmithBowlerName, Average = 221.4m, Games = 60, Tournaments = 10, FieldAverage = 12.5m }],
                HighBlock = [new HighBlockResponse { BowlerId = janeSmithBowlerId, BowlerName = janeSmithBowlerName, HighBlock = 1182, HighGame = 258 }],
                MatchPlayAverage = [new MatchPlayAverageResponse { BowlerId = janeSmithBowlerId, BowlerName = janeSmithBowlerName, MatchPlayAverage = 218.0m, Games = 15, Wins = 9, Losses = 6, WinPercentage = 60.0m, Winnings = 1500m }],
                MatchPlayRecord = [new MatchPlayRecordResponse { BowlerId = janeSmithBowlerId, BowlerName = janeSmithBowlerName, Wins = 9, Losses = 6, WinPercentage = 60.0m, Finals = 5, MatchPlayAverage = 218.0m, Winnings = 1500m }],
                MatchPlayAppearances = [new MatchPlayAppearancesResponse { BowlerId = janeSmithBowlerId, BowlerName = janeSmithBowlerName, Finals = 5, Tournaments = 10, Entries = 12 }],
                PointsPerEntry = [new PointsPerEntryResponse { BowlerId = janeSmithBowlerId, BowlerName = janeSmithBowlerName, PointsPerEntry = 26.67m, Points = 320, Entries = 12 }],
                PointsPerTournament = [new PointsPerTournamentResponse { BowlerId = janeSmithBowlerId, BowlerName = janeSmithBowlerName, Points = 320, Tournaments = 10, PointsPerTournament = 32.0m }],
                FinalsPerEntry = [new FinalsPerEntryResponse { BowlerId = janeSmithBowlerId, BowlerName = janeSmithBowlerName, Finals = 5, Entries = 12, FinalsPerEntry = 0.42m }],
                AverageFinishes = [new AverageFinishResponse { BowlerId = janeSmithBowlerId, BowlerName = janeSmithBowlerName, AverageFinish = 3.2m, Finals = 5, Winnings = 1500m }],
                SeasonAtAGlance = new SeasonAtAGlanceResponse { TotalEntries = 245, TotalPrizeMoney = 18500m },
                SeasonsBests = new SeasonBestsResponse { HighGame = 279, HighGameBowlers = new Dictionary<string, string> { { janeSmithBowlerId, janeSmithBowlerName } }, HighBlock = 1250, HighBlockBowlers = new Dictionary<string, string> { { janeSmithBowlerId, janeSmithBowlerName } }, HighAverage = 225.35m, HighAverageBowlers = new Dictionary<string, string> { { janeSmithBowlerId, janeSmithBowlerName } } },
                FieldMatchPlaySummary = new FieldMatchPlaySummaryResponse { HighestWinPercentage = 66.67m, HighestWinPercentageBowlers = new Dictionary<string, string> { { janeSmithBowlerId, janeSmithBowlerName } }, MostFinals = 5, MostFinalsBowlers = new Dictionary<string, string> { { janeSmithBowlerId, janeSmithBowlerName } } },
                BowlerOfTheYearPointsRace = [new PointsRaceSeriesResponse { BowlerId = janeSmithBowlerId, BowlerName = janeSmithBowlerName, Results = [new PointsRaceTournamentResponse { TournamentName = "Spring Open", TournamentDate = new DateOnly(2024, 3, 15), CumulativePoints = 80 }] }],
                AllBowlers = [new FullStatModalRowResponse { BowlerId = janeSmithBowlerId, BowlerName = janeSmithBowlerName, Points = 320, Average = 221.4m, Games = 60, Finals = 5, Wins = 9, Losses = 6, WinPercentage = 60.0m, MatchPlayAverage = 218.0m, Winnings = 1500m, FieldAverage = 12.5m, Tournaments = 10 }]
            });

        Response(404, "No stats found for the requested season.");
    }
}