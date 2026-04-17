using Neba.Api.Contracts.Stats;
using Neba.Api.Contracts.Stats.GetSeasonStats;
using Neba.Website.Server.Services;

namespace Neba.Website.Server.Stats;

internal sealed class StatsApiService(ApiExecutor executor, IStatsApi statsApi) : IStatsApiService
{
    private const string ApiName = "StatsApi";

    public async Task<StatsPageViewModel> GetStatsAsync(int? year = null, CancellationToken ct = default)
    {
        var result = await executor.ExecuteAsync(
            ApiName,
            nameof(GetStatsAsync),
            token => statsApi.GetSeasonStatsAsync(year, token),
            ct);

        if (result.IsError)
            return EmptyStatsPageViewModel();

        return MapToStatsPageViewModel(result.Value);
    }

    public async Task<IndividualStatsPageViewModel?> GetIndividualStatsAsync(
        string bowlerId, int? year = null, CancellationToken ct = default)
    {
        var result = await executor.ExecuteAsync(
            ApiName,
            nameof(GetIndividualStatsAsync),
            token => statsApi.GetSeasonStatsAsync(year, token),
            ct);

        if (result.IsError)
            return null;

        return MapToIndividualStatsPageViewModel(result.Value, bowlerId);
    }

    private static StatsPageViewModel MapToStatsPageViewModel(GetSeasonStatsResponse response) =>
        new()
        {
            SelectedSeason = response.SelectedSeason,
            MinimumNumberOfGames = response.MinimumNumberOfGames,
            MinimumNumberOfTournaments = response.MinimumNumberOfTournaments,
            MinimumNumberOfEntries = response.MinimumNumberOfEntries,
            AvailableSeasons = response.AvailableSeasons,
            BowlerSearchList = response.BowlerSearchList,
            BowlerOfTheYear = MapBotyStandings(response.BowlerOfTheYear),
            SeniorOfTheYear = MapBotyStandings(response.SeniorOfTheYear),
            SuperSeniorOfTheYear = MapBotyStandings(response.SuperSeniorOfTheYear),
            WomanOfTheYear = MapBotyStandings(response.WomanOfTheYear),
            RookieOfTheYear = MapBotyStandings(response.RookieOfTheYear),
            YouthOfTheYear = MapBotyStandings(response.YouthOfTheYear),
            HighAverage = [.. response.HighAverage.Select((h, i) => new HighAverageRowViewModel
            {
                Rank = i + 1,
                BowlerId = h.BowlerId,
                BowlerName = h.BowlerName,
                Average = h.Average,
                Games = h.Games,
                Tournaments = h.Tournaments,
                FieldAverage = h.FieldAverage
            })],
            HighBlock = [.. response.HighBlock.Select((h, i) => new HighBlockRowViewModel
            {
                Rank = i + 1,
                BowlerId = h.BowlerId,
                BowlerName = h.BowlerName,
                HighBlock = h.HighBlock,
                HighGame = h.HighGame
            })],
            MatchPlayAverage = [.. response.MatchPlayAverage.Select((m, i) => new MatchPlayAverageRowViewModel
            {
                Rank = i + 1,
                BowlerId = m.BowlerId,
                BowlerName = m.BowlerName,
                MatchPlayAverage = m.MatchPlayAverage,
                Games = m.Games,
                Wins = m.Wins,
                Loses = m.Losses,
                Winnings = m.Winnings
            })],
            MatchPlayRecord = [.. response.MatchPlayRecord.Select((m, i) => new MatchPlayRecordRowViewModel
            {
                Rank = i + 1,
                BowlerId = m.BowlerId,
                BowlerName = m.BowlerName,
                Wins = m.Wins,
                Loses = m.Losses,
                Finals = m.Finals,
                MatchPlayAverage = m.MatchPlayAverage,
                Winnings = m.Winnings
            })],
            MatchPlayAppearances = [.. response.MatchPlayAppearances.Select((m, i) => new MatchPlayAppearancesRowViewModel
            {
                Rank = i + 1,
                BowlerId = m.BowlerId,
                BowlerName = m.BowlerName,
                Finals = m.Finals,
                Tournaments = m.Tournaments,
                Entries = m.Entries
            })],
            PointsPerEntry = [.. response.PointsPerEntry.Select((p, i) => new PointsPerEntryRowViewModel
            {
                Rank = i + 1,
                BowlerId = p.BowlerId,
                BowlerName = p.BowlerName,
                Points = p.Points,
                Entries = p.Entries
            })],
            PointsPerTournament = [.. response.PointsPerTournament.Select((p, i) => new PointsPerTournamentRowViewModel
            {
                Rank = i + 1,
                BowlerId = p.BowlerId,
                BowlerName = p.BowlerName,
                Points = p.Points,
                Tournaments = p.Tournaments
            })],
            FinalsPerEntry = [.. response.FinalsPerEntry.Select((f, i) => new FinalsPerEntryRowViewModel
            {
                Rank = i + 1,
                BowlerId = f.BowlerId,
                BowlerName = f.BowlerName,
                Finals = f.Finals,
                Entries = f.Entries
            })],
            AverageFinishes = [.. response.AverageFinishes.Select((a, i) => new AverageFinishRowViewModel
            {
                Rank = i + 1,
                BowlerId = a.BowlerId,
                BowlerName = a.BowlerName,
                AverageFinish = a.AverageFinish,
                Finals = a.Finals,
                Winnings = a.Winnings
            })],
            SeasonAtAGlance = new SeasonAtAGlanceViewModel
            {
                TotalEntries = response.SeasonAtAGlance.TotalEntries,
                TotalPrizeMoney = response.SeasonAtAGlance.TotalPrizeMoney
            },
            SeasonsBests = new SeasonBestsViewModel
            {
                HighGame = response.SeasonsBests.HighGame,
                HighGameBowlers = response.SeasonsBests.HighGameBowlers,
                HighBlock = response.SeasonsBests.HighBlock,
                HighBlockBowlers = response.SeasonsBests.HighBlockBowlers,
                HighAverage = response.SeasonsBests.HighAverage,
                HighAverageBowlers = response.SeasonsBests.HighAverageBowlers
            },
            FieldMatchPlaySummary = new FieldMatchPlaySummaryViewModel
            {
                HighestWinPercentage = response.FieldMatchPlaySummary.HighestWinPercentage,
                HighestWinPercentageBowlers = response.FieldMatchPlaySummary.HighestWinPercentageBowlers,
                MostFinals = response.FieldMatchPlaySummary.MostFinals,
                MostFinalsBowlers = response.FieldMatchPlaySummary.MostFinalsBowlers
            },
            BowlerOfTheYearPointsRace = [.. response.BowlerOfTheYearPointsRace.Select(race => new PointsRaceSeriesViewModel
            {
                BowlerId = race.BowlerId,
                BowlerName = race.BowlerName,
                Results = [.. race.Results.Select(r => new PointsRaceTournamentViewModel
                {
                    TournamentName = r.TournamentName,
                    TournamentDate = r.TournamentDate,
                    CumulativePoints = r.CumulativePoints
                })]
            })],
            AllBowlers = [.. response.AllBowlers.Select((b, i) => new FullStatModalRowViewModel
            {
                Rank = i + 1,
                BowlerId = b.BowlerId,
                BowlerName = b.BowlerName,
                Points = b.Points,
                Average = b.Average,
                Games = b.Games,
                Finals = b.Finals,
                Wins = b.Wins,
                Loses = b.Losses,
                MatchPlayAverage = b.MatchPlayAverage == 0 ? null : b.MatchPlayAverage,
                Winnings = b.Winnings,
                FieldAverage = b.FieldAverage,
                Touranments = b.Tournaments
            })]
        };

    private static IndividualStatsPageViewModel? MapToIndividualStatsPageViewModel(
        GetSeasonStatsResponse response, string bowlerId)
    {
        var full = response.AllBowlers.FirstOrDefault(b => b.BowlerId == bowlerId);
        if (full is null)
            return null;

        var allStandings = response.BowlerOfTheYear
            .Concat(response.SeniorOfTheYear)
            .Concat(response.SuperSeniorOfTheYear)
            .Concat(response.WomanOfTheYear)
            .Concat(response.RookieOfTheYear)
            .Concat(response.YouthOfTheYear);

        var entriesFromStanding = allStandings
            .FirstOrDefault(s => s.BowlerId == bowlerId)?.Entries;

        return new IndividualStatsPageViewModel
        {
            BowlerId = bowlerId,
            BowlerName = full.BowlerName,
            SelectedSeason = response.SelectedSeason,
            AvailableSeasons = response.AvailableSeasons,

            Points = full.Points,
            Average = full.Average,
            Games = full.Games,
            Finals = full.Finals,
            Entries = entriesFromStanding ?? full.Tournaments,
            Tournaments = full.Tournaments,
            Winnings = full.Winnings,
            FieldAverage = full.FieldAverage,
            MatchPlayWins = full.Wins,
            MatchPlayLosses = full.Losses,
            MatchPlayAverage = full.MatchPlayAverage == 0 ? null : full.MatchPlayAverage,

            BowlerOfTheYearRank = FindRank(response.BowlerOfTheYear, bowlerId, x => x.BowlerId),
            SeniorOfTheYearRank = FindRank(response.SeniorOfTheYear, bowlerId, x => x.BowlerId),
            SuperSeniorOfTheYearRank = FindRank(response.SuperSeniorOfTheYear, bowlerId, x => x.BowlerId),
            WomanOfTheYearRank = FindRank(response.WomanOfTheYear, bowlerId, x => x.BowlerId),
            RookieOfTheYearRank = FindRank(response.RookieOfTheYear, bowlerId, x => x.BowlerId),
            YouthOfTheYearRank = FindRank(response.YouthOfTheYear, bowlerId, x => x.BowlerId),

            HighAverageRank = FindRank(response.HighAverage, bowlerId, x => x.BowlerId),
            HighBlockRank = FindRank(response.HighBlock, bowlerId, x => x.BowlerId),
            MatchPlayAverageRank = FindRank(response.MatchPlayAverage, bowlerId, x => x.BowlerId),

            MatchPlayRecordRank = FindRank(response.MatchPlayRecord, bowlerId, x => x.BowlerId),
            MatchPlayAppearancesRank = FindRank(response.MatchPlayAppearances, bowlerId, x => x.BowlerId),

            PointsPerEntryRank = FindRank(response.PointsPerEntry, bowlerId, x => x.BowlerId),
            PointsPerTournamentRank = FindRank(response.PointsPerTournament, bowlerId, x => x.BowlerId),
            FinalsPerEntryRank = FindRank(response.FinalsPerEntry, bowlerId, x => x.BowlerId),
            AverageFinishRank = FindRank(response.AverageFinishes, bowlerId, x => x.BowlerId),

            BowlerOfTheYearPointsRace = response.BowlerOfTheYearPointsRace
                .Where(r => r.BowlerId == bowlerId)
                .Select(r => new PointsRaceSeriesViewModel
                {
                    BowlerId = bowlerId,
                    BowlerName = r.BowlerName,
                    Results = [.. r.Results.Select(t => new PointsRaceTournamentViewModel
                    {
                        TournamentName = t.TournamentName,
                        TournamentDate = t.TournamentDate,
                        CumulativePoints = t.CumulativePoints
                    })]
                })
                .FirstOrDefault()
        };
    }

    private static IReadOnlyCollection<BowlerOfTheYearStandingRowViewModel> MapBotyStandings(
        IReadOnlyCollection<BowlerOfTheYearStandingResponse> standings) =>
        [.. standings.Select((s, i) => new BowlerOfTheYearStandingRowViewModel
        {
            Rank = i + 1,
            BowlerId = s.BowlerId,
            BowlerName = s.BowlerName,
            Points = s.Points,
            Tournaments = s.Tournaments,
            Entries = s.Entries,
            Finals = s.Finals,
            AverageFinish = s.AverageFinish,
            Winnings = s.Winnings
        })];

    private static int? FindRank<T>(IReadOnlyCollection<T> list, string bowlerId, Func<T, string> keySelector)
    {
        var index = list.ToList().FindIndex(x => keySelector(x) == bowlerId);
        return index >= 0 ? index + 1 : null;
    }

    private static StatsPageViewModel EmptyStatsPageViewModel() =>
        new()
        {
            SelectedSeason = "",
            MinimumNumberOfGames = 0,
            MinimumNumberOfTournaments = 0,
            MinimumNumberOfEntries = 0,
            AvailableSeasons = new Dictionary<int, string>(),
            BowlerSearchList = new Dictionary<string, string>(),
            BowlerOfTheYear = [],
            SeniorOfTheYear = [],
            SuperSeniorOfTheYear = [],
            WomanOfTheYear = [],
            RookieOfTheYear = [],
            YouthOfTheYear = [],
            HighAverage = [],
            HighBlock = [],
            MatchPlayAverage = [],
            MatchPlayRecord = [],
            MatchPlayAppearances = [],
            PointsPerEntry = [],
            PointsPerTournament = [],
            FinalsPerEntry = [],
            AverageFinishes = [],
            SeasonAtAGlance = new SeasonAtAGlanceViewModel { TotalEntries = 0, TotalPrizeMoney = 0 },
            SeasonsBests = new SeasonBestsViewModel
            {
                HighGame = 0,
                HighGameBowlers = new Dictionary<string, string>(),
                HighBlock = 0,
                HighBlockBowlers = new Dictionary<string, string>(),
                HighAverage = 0,
                HighAverageBowlers = new Dictionary<string, string>()
            },
            FieldMatchPlaySummary = new FieldMatchPlaySummaryViewModel
            {
                HighestWinPercentage = 0,
                HighestWinPercentageBowlers = new Dictionary<string, string>(),
                MostFinals = 0,
                MostFinalsBowlers = new Dictionary<string, string>()
            },
            BowlerOfTheYearPointsRace = [],
            AllBowlers = []
        };
}