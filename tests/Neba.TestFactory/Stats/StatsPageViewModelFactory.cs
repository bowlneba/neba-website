using Bogus;

using Neba.Application.Stats.GetSeasonStats;
using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class StatsPageViewModelFactory
{
    public const string ValidSelectedSeason = "2024-2025";
    public const int ValidMinGamesHighAverage = 24;
    public const int ValidMinMatchPlayGames = 12;
    public const int ValidMinMatchPlayAppearances = 8;
    public const int ValidMinEntries = 6;

    public static StatsPageViewModel Create(
        string? selectedSeason = null,
        IReadOnlyDictionary<Ulid, string>? availableSeasons = null,
        IReadOnlyDictionary<Ulid, string>? bowlerSearchList = null,
        IReadOnlyCollection<BowlerOfTheYearStandingRowViewModel>? bowlerOfTheYear = null,
        IReadOnlyCollection<BowlerOfTheYearStandingRowViewModel>? seniorOfTheYear = null,
        IReadOnlyCollection<BowlerOfTheYearStandingRowViewModel>? superSeniorOfTheYear = null,
        IReadOnlyCollection<BowlerOfTheYearStandingRowViewModel>? womanOfTheYear = null,
        IReadOnlyCollection<BowlerOfTheYearStandingRowViewModel>? rookieOfTheYear = null,
        IReadOnlyCollection<BowlerOfTheYearStandingRowViewModel>? youthOfTheYear = null,
        IReadOnlyCollection<HighAverageRowViewModel>? highAverage = null,
        IReadOnlyCollection<HighBlockRowViewModel>? highBlock = null,
        IReadOnlyCollection<MatchPlayAverageRowViewModel>? matchPlayAverage = null,
        IReadOnlyCollection<MatchPlayRecordRowViewModel>? matchPlayRecord = null,
        IReadOnlyCollection<MatchPlayAppearancesRowViewModel>? matchPlayAppearances = null,
        IReadOnlyCollection<PointsPerEntryRowViewModel>? pointsPerEntry = null,
        IReadOnlyCollection<PointsPerTournamentRowViewModel>? pointsPerTournament = null,
        IReadOnlyCollection<FinalsPerEntryRowViewModel>? finalsPerEntry = null,
        IReadOnlyCollection<AverageFinishRowViewModel>? averageFinishes = null,
        int? minGamesHighAverage = null,
        int? minMatchPlayGames = null,
        int? minMatchPlayAppearances = null,
        int? minEntries = null,
        SeasonAtAGlanceViewModel? seasonAtAGlance = null,
        SeasonBestsViewModel? seasonsBests = null,
        FieldMatchPlaySummaryViewModel? fieldMatchPlaySummary = null,
        IReadOnlyCollection<PointsRaceSeriesViewModel>? bowlerOfTheYearPointsRace = null,
        IReadOnlyCollection<FullStatModalRowViewModel>? allBowlers = null)
        => new()
        {
            SelectedSeason = selectedSeason ?? ValidSelectedSeason,
            AvailableSeasons = availableSeasons ?? new Dictionary<Ulid, string> { [Ulid.NewUlid()] = ValidSelectedSeason },
            BowlerSearchList = bowlerSearchList ?? new Dictionary<Ulid, string> { [Ulid.NewUlid()] = BowlerOfTheYearStandingRowViewModelFactory.ValidBowlerName },
            BowlerOfTheYear = bowlerOfTheYear ?? [BowlerOfTheYearStandingRowViewModelFactory.Create()],
            SeniorOfTheYear = seniorOfTheYear ?? [BowlerOfTheYearStandingRowViewModelFactory.Create()],
            SuperSeniorOfTheYear = superSeniorOfTheYear ?? [BowlerOfTheYearStandingRowViewModelFactory.Create()],
            WomanOfTheYear = womanOfTheYear ?? [BowlerOfTheYearStandingRowViewModelFactory.Create()],
            RookieOfTheYear = rookieOfTheYear ?? [BowlerOfTheYearStandingRowViewModelFactory.Create()],
            YouthOfTheYear = youthOfTheYear ?? [BowlerOfTheYearStandingRowViewModelFactory.Create()],
            HighAverage = highAverage ?? [HighAverageRowViewModelFactory.Create()],
            HighBlock = highBlock ?? [HighBlockRowViewModelFactory.Create()],
            MatchPlayAverage = matchPlayAverage ?? [MatchPlayAverageRowViewModelFactory.Create()],
            MatchPlayRecord = matchPlayRecord ?? [MatchPlayRecordRowViewModelFactory.Create()],
            MatchPlayAppearances = matchPlayAppearances ?? [MatchPlayAppearancesRowViewModelFactory.Create()],
            PointsPerEntry = pointsPerEntry ?? [PointsPerEntryRowViewModelFactory.Create()],
            PointsPerTournament = pointsPerTournament ?? [PointsPerTournamentRowViewModelFactory.Create()],
            FinalsPerEntry = finalsPerEntry ?? [FinalsPerEntryRowViewModelFactory.Create()],
            AverageFinishes = averageFinishes ?? [AverageFinishRowViewModelFactory.Create()],
            MinGamesHighAverage = minGamesHighAverage ?? ValidMinGamesHighAverage,
            MinMatchPlayGames = minMatchPlayGames ?? ValidMinMatchPlayGames,
            MinTournaments = minMatchPlayAppearances ?? ValidMinMatchPlayAppearances,
            MinEntries = minEntries ?? ValidMinEntries,
            SeasonAtAGlance = seasonAtAGlance ?? SeasonAtAGlanceViewModelFactory.Create(),
            SeasonsBests = seasonsBests ?? SeasonBestsViewModelFactory.Create(),
            FieldMatchPlaySummary = fieldMatchPlaySummary ?? FieldMatchPlaySummaryViewModelFactory.Create(),
            BowlerOfTheYearPointsRace = bowlerOfTheYearPointsRace ?? [PointsRaceSeriesViewModelFactory.Create()],
            AllBowlers = allBowlers ?? [FullStatModalRowViewModelFactory.Create()],
        };

    public static IReadOnlyCollection<StatsPageViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<StatsPageViewModel>()
            .CustomInstantiator(f =>
            {
                var year = f.Date.Past(50).Year;

                return new StatsPageViewModel
                {
                    SelectedSeason = $"{year} Season",
                    AvailableSeasons = Enumerable.Range(0, f.Random.Int(1, 5))
                        .ToDictionary(_ => Ulid.Bogus(f), _ => $"{f.Date.Past(50).Year}-{f.Date.Past(50).Year + 1}"),
                    BowlerSearchList = Enumerable.Range(0, f.Random.Int(1, 10))
                        .ToDictionary(_ => Ulid.Bogus(f), _ => f.Name.FullName()),
                    BowlerOfTheYear = BowlerOfTheYearStandingRowViewModelFactory.Bogus(10, seed),
                    SeniorOfTheYear = BowlerOfTheYearStandingRowViewModelFactory.Bogus(5, seed),
                    SuperSeniorOfTheYear = BowlerOfTheYearStandingRowViewModelFactory.Bogus(5, seed),
                    WomanOfTheYear = BowlerOfTheYearStandingRowViewModelFactory.Bogus(5, seed),
                    RookieOfTheYear = BowlerOfTheYearStandingRowViewModelFactory.Bogus(5, seed),
                    YouthOfTheYear = BowlerOfTheYearStandingRowViewModelFactory.Bogus(5, seed),
                    HighAverage = HighAverageRowViewModelFactory.Bogus(5, seed),
                    HighBlock = HighBlockRowViewModelFactory.Bogus(5, seed),
                    MatchPlayAverage = MatchPlayAverageRowViewModelFactory.Bogus(5, seed),
                    MatchPlayRecord = MatchPlayRecordRowViewModelFactory.Bogus(5, seed),
                    MatchPlayAppearances = MatchPlayAppearancesRowViewModelFactory.Bogus(5, seed),
                    PointsPerEntry = PointsPerEntryRowViewModelFactory.Bogus(5, seed),
                    PointsPerTournament = PointsPerTournamentRowViewModelFactory.Bogus(5, seed),
                    FinalsPerEntry = FinalsPerEntryRowViewModelFactory.Bogus(5, seed),
                    AverageFinishes = AverageFinishRowViewModelFactory.Bogus(5, seed),
                    MinGamesHighAverage = f.Random.Int(10, 50),
                    MinMatchPlayGames = f.Random.Int(10, 50),
                    MinTournaments = f.Random.Int(2, 15),
                    MinEntries = f.Random.Int(2, 12),
                    SeasonAtAGlance = SeasonAtAGlanceViewModelFactory.Bogus(1, seed).Single(),
                    SeasonsBests = SeasonBestsViewModelFactory.Bogus(1, seed).Single(),
                    FieldMatchPlaySummary = FieldMatchPlaySummaryViewModelFactory.Bogus(1, seed).Single(),
                    BowlerOfTheYearPointsRace = PointsRaceSeriesViewModelFactory.Bogus(f.Random.Int(1, 5), seed),
                    AllBowlers = FullStatModalRowViewModelFactory.Bogus(25, seed),
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}