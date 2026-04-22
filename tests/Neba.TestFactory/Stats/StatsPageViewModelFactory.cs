using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class StatsPageViewModelFactory
{
    public const string ValidSelectedSeason = "2024-2025";

    public static StatsPageViewModel Create(
        string? selectedSeason = null,
        IReadOnlyDictionary<int, string>? availableSeasons = null,
        IReadOnlyDictionary<string, string>? bowlerSearchList = null,
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
        SeasonAtAGlanceViewModel? seasonAtAGlance = null,
        SeasonBestsViewModel? seasonsBests = null,
        FieldMatchPlaySummaryViewModel? fieldMatchPlaySummary = null,
        IReadOnlyCollection<PointsRaceSeriesViewModel>? bowlerOfTheYearPointsRace = null,
        IReadOnlyCollection<FullStatModalRowViewModel>? allBowlers = null)
        => new()
        {
            SelectedSeason = selectedSeason ?? ValidSelectedSeason,
            MinimumNumberOfGames = 45m,
            MinimumNumberOfTournaments = 5m,
            MinimumNumberOfEntries = 7.5m,
            AvailableSeasons = availableSeasons ?? new Dictionary<int, string> { [DateTime.Now.Year] = ValidSelectedSeason },
            BowlerSearchList = bowlerSearchList ?? new Dictionary<string, string> { [Ulid.NewUlid().ToString()] = BowlerOfTheYearStandingRowViewModelFactory.ValidBowlerName },
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
                        .ToDictionary(i => year - i, i => $"{year - i}-{year - i + 1}"),
                    BowlerSearchList = Enumerable.Range(0, f.Random.Int(1, 10))
                        .ToDictionary(_ => Ulid.BogusString(f), _ => f.Name.FullName()),
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
                    SeasonAtAGlance = SeasonAtAGlanceViewModelFactory.Bogus(1, seed).Single(),
                    SeasonsBests = SeasonBestsViewModelFactory.Bogus(1, seed).Single(),
                    FieldMatchPlaySummary = FieldMatchPlaySummaryViewModelFactory.Bogus(1, seed).Single(),
                    BowlerOfTheYearPointsRace = PointsRaceSeriesViewModelFactory.Bogus(f.Random.Int(1, 5), seed),
                    AllBowlers = FullStatModalRowViewModelFactory.Bogus(25, seed),
                    MinimumNumberOfGames = f.Random.Decimal(10, 60),
                    MinimumNumberOfTournaments = f.Random.Decimal(2, 8),
                    MinimumNumberOfEntries = f.Random.Decimal(3, 12),
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}