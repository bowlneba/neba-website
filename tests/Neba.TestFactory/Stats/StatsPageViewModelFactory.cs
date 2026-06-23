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
        IReadOnlyCollection<PointsRaceSeriesViewModel>? openPointsRace = null,
        IReadOnlyCollection<PointsRaceSeriesViewModel>? seniorPointsRace = null,
        IReadOnlyCollection<PointsRaceSeriesViewModel>? superSeniorPointsRace = null,
        IReadOnlyCollection<PointsRaceSeriesViewModel>? womenPointsRace = null,
        IReadOnlyCollection<PointsRaceSeriesViewModel>? youthPointsRace = null,
        IReadOnlyCollection<PointsRaceSeriesViewModel>? rookiePointsRace = null,
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
            OpenPointsRace = openPointsRace ?? [PointsRaceSeriesViewModelFactory.Create()],
            SeniorPointsRace = seniorPointsRace ?? [],
            SuperSeniorPointsRace = superSeniorPointsRace ?? [],
            WomenPointsRace = womenPointsRace ?? [],
            YouthPointsRace = youthPointsRace ?? [],
            RookiePointsRace = rookiePointsRace ?? [],
            AllBowlers = allBowlers ?? [FullStatModalRowViewModelFactory.Create()],
        };

    internal static IReadOnlyCollection<StatsPageViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var year = faker.Date.Past(50).Year;
            return new StatsPageViewModel
            {
                SelectedSeason = $"{year} Season",
                AvailableSeasons = Enumerable.Range(0, faker.Random.Int(1, 5))
                    .ToDictionary(i => year - i, i => $"{year - i}-{year - i + 1}"),
                BowlerSearchList = Enumerable.Range(0, faker.Random.Int(1, 10))
                    .ToDictionary(_ => Ulid.BogusString(faker), _ => faker.Name.FullName()),
                BowlerOfTheYear = BowlerOfTheYearStandingRowViewModelFactory.Bogus(10, faker),
                SeniorOfTheYear = BowlerOfTheYearStandingRowViewModelFactory.Bogus(5, faker),
                SuperSeniorOfTheYear = BowlerOfTheYearStandingRowViewModelFactory.Bogus(5, faker),
                WomanOfTheYear = BowlerOfTheYearStandingRowViewModelFactory.Bogus(5, faker),
                RookieOfTheYear = BowlerOfTheYearStandingRowViewModelFactory.Bogus(5, faker),
                YouthOfTheYear = BowlerOfTheYearStandingRowViewModelFactory.Bogus(5, faker),
                HighAverage = HighAverageRowViewModelFactory.Bogus(5, faker),
                HighBlock = HighBlockRowViewModelFactory.Bogus(5, faker),
                MatchPlayAverage = MatchPlayAverageRowViewModelFactory.Bogus(5, faker),
                MatchPlayRecord = MatchPlayRecordRowViewModelFactory.Bogus(5, faker),
                MatchPlayAppearances = MatchPlayAppearancesRowViewModelFactory.Bogus(5, faker),
                PointsPerEntry = PointsPerEntryRowViewModelFactory.Bogus(5, faker),
                PointsPerTournament = PointsPerTournamentRowViewModelFactory.Bogus(5, faker),
                FinalsPerEntry = FinalsPerEntryRowViewModelFactory.Bogus(5, faker),
                AverageFinishes = AverageFinishRowViewModelFactory.Bogus(5, faker),
                SeasonAtAGlance = SeasonAtAGlanceViewModelFactory.Bogus(1, faker).Single(),
                SeasonsBests = SeasonBestsViewModelFactory.Bogus(1, faker).Single(),
                FieldMatchPlaySummary = FieldMatchPlaySummaryViewModelFactory.Bogus(1, faker).Single(),
                OpenPointsRace = PointsRaceSeriesViewModelFactory.Bogus(faker.Random.Int(1, 5), faker),
                SeniorPointsRace = [],
                SuperSeniorPointsRace = [],
                WomenPointsRace = [],
                YouthPointsRace = [],
                RookiePointsRace = [],
                AllBowlers = FullStatModalRowViewModelFactory.Bogus(25, faker),
                MinimumNumberOfGames = faker.Random.Decimal(10, 60),
                MinimumNumberOfTournaments = faker.Random.Decimal(2, 8),
                MinimumNumberOfEntries = faker.Random.Decimal(3, 12),
            };
        })];
    }

    public static IReadOnlyCollection<StatsPageViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}