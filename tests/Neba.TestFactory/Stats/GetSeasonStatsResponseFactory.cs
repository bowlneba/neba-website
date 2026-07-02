using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class GetSeasonStatsResponseFactory
{
    public const string ValidSelectedSeason = "2024-2025 Season";

    public static GetSeasonStatsResponse Create(
        string? selectedSeason = null,
        IReadOnlyDictionary<int, string>? availableSeasons = null,
        IReadOnlyDictionary<string, string>? bowlerSearchList = null,
        IReadOnlyCollection<BowlerOfTheYearStandingResponse>? bowlerOfTheYear = null,
        IReadOnlyCollection<BowlerOfTheYearStandingResponse>? seniorOfTheYear = null,
        IReadOnlyCollection<BowlerOfTheYearStandingResponse>? superSeniorOfTheYear = null,
        IReadOnlyCollection<BowlerOfTheYearStandingResponse>? womanOfTheYear = null,
        IReadOnlyCollection<BowlerOfTheYearStandingResponse>? rookieOfTheYear = null,
        IReadOnlyCollection<BowlerOfTheYearStandingResponse>? youthOfTheYear = null,
        IReadOnlyCollection<HighAverageResponse>? highAverage = null,
        IReadOnlyCollection<HighBlockResponse>? highBlock = null,
        IReadOnlyCollection<MatchPlayAverageResponse>? matchPlayAverage = null,
        IReadOnlyCollection<MatchPlayRecordResponse>? matchPlayRecord = null,
        IReadOnlyCollection<MatchPlayAppearancesResponse>? matchPlayAppearances = null,
        IReadOnlyCollection<PointsPerEntryResponse>? pointsPerEntry = null,
        IReadOnlyCollection<PointsPerTournamentResponse>? pointsPerTournament = null,
        IReadOnlyCollection<FinalsPerEntryResponse>? finalsPerEntry = null,
        IReadOnlyCollection<AverageFinishResponse>? averageFinishes = null,
        SeasonAtAGlanceResponse? seasonAtAGlance = null,
        SeasonBestsResponse? seasonsBests = null,
        FieldMatchPlaySummaryResponse? fieldMatchPlaySummary = null,
        IReadOnlyCollection<PointsRaceSeriesResponse>? openPointsRace = null,
        IReadOnlyCollection<PointsRaceSeriesResponse>? seniorPointsRace = null,
        IReadOnlyCollection<PointsRaceSeriesResponse>? superSeniorPointsRace = null,
        IReadOnlyCollection<PointsRaceSeriesResponse>? womenPointsRace = null,
        IReadOnlyCollection<PointsRaceSeriesResponse>? youthPointsRace = null,
        IReadOnlyCollection<PointsRaceSeriesResponse>? rookiePointsRace = null,
        IReadOnlyCollection<FullStatModalRowResponse>? allBowlers = null)
        => new()
        {
            SelectedSeason = selectedSeason ?? ValidSelectedSeason,
            MinimumNumberOfGames = 45m,
            MinimumNumberOfTournaments = 5m,
            MinimumNumberOfEntries = 7.5m,
            AvailableSeasons = availableSeasons ?? new Dictionary<int, string> { { 2025, "2024-2025 Season" } },
            BowlerSearchList = bowlerSearchList ?? new Dictionary<string, string> { { "01JWXYZTEST000000000000002", "Jane Smith" } },
            BowlerOfTheYear = bowlerOfTheYear ?? [BowlerOfTheYearStandingResponseFactory.Create()],
            SeniorOfTheYear = seniorOfTheYear ?? [],
            SuperSeniorOfTheYear = superSeniorOfTheYear ?? [],
            WomanOfTheYear = womanOfTheYear ?? [],
            RookieOfTheYear = rookieOfTheYear ?? [],
            YouthOfTheYear = youthOfTheYear ?? [],
            HighAverage = highAverage ?? [HighAverageResponseFactory.Create()],
            HighBlock = highBlock ?? [HighBlockResponseFactory.Create()],
            MatchPlayAverage = matchPlayAverage ?? [MatchPlayAverageResponseFactory.Create()],
            MatchPlayRecord = matchPlayRecord ?? [MatchPlayRecordResponseFactory.Create()],
            MatchPlayAppearances = matchPlayAppearances ?? [MatchPlayAppearancesResponseFactory.Create()],
            PointsPerEntry = pointsPerEntry ?? [PointsPerEntryResponseFactory.Create()],
            PointsPerTournament = pointsPerTournament ?? [PointsPerTournamentResponseFactory.Create()],
            FinalsPerEntry = finalsPerEntry ?? [FinalsPerEntryResponseFactory.Create()],
            AverageFinishes = averageFinishes ?? [AverageFinishResponseFactory.Create()],
            SeasonAtAGlance = seasonAtAGlance ?? SeasonAtAGlanceResponseFactory.Create(),
            SeasonsBests = seasonsBests ?? SeasonBestsResponseFactory.Create(),
            FieldMatchPlaySummary = fieldMatchPlaySummary ?? FieldMatchPlaySummaryResponseFactory.Create(),
            OpenPointsRace = openPointsRace ?? [PointsRaceSeriesResponseFactory.Create()],
            SeniorPointsRace = seniorPointsRace ?? [],
            SuperSeniorPointsRace = superSeniorPointsRace ?? [],
            WomenPointsRace = womenPointsRace ?? [],
            YouthPointsRace = youthPointsRace ?? [],
            RookiePointsRace = rookiePointsRace ?? [],
            AllBowlers = allBowlers ?? [FullStatModalRowResponseFactory.Create()]
        };

    internal static IReadOnlyCollection<GetSeasonStatsResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var year = faker.Random.Int(2000, 2030);
            return new GetSeasonStatsResponse
            {
                SelectedSeason = $"{year}-{year + 1} Season",
                AvailableSeasons = Enumerable.Range(0, faker.Random.Int(1, 5))
                    .ToDictionary(i => year - i, i => $"{year - i - 1}-{year - i} Season"),
                BowlerSearchList = Enumerable.Range(0, faker.Random.Int(5, 20))
                    .ToDictionary(_ => Ulid.BogusString(faker), _ => faker.Name.FullName()),
                BowlerOfTheYear = BowlerOfTheYearStandingResponseFactory.Bogus(faker.Random.Int(5, 20), faker),
                SeniorOfTheYear = BowlerOfTheYearStandingResponseFactory.Bogus(faker.Random.Int(1, 5), faker),
                SuperSeniorOfTheYear = BowlerOfTheYearStandingResponseFactory.Bogus(faker.Random.Int(1, 5), faker),
                WomanOfTheYear = BowlerOfTheYearStandingResponseFactory.Bogus(faker.Random.Int(1, 5), faker),
                RookieOfTheYear = BowlerOfTheYearStandingResponseFactory.Bogus(faker.Random.Int(1, 5), faker),
                YouthOfTheYear = BowlerOfTheYearStandingResponseFactory.Bogus(faker.Random.Int(0, 3), faker),
                HighAverage = HighAverageResponseFactory.Bogus(faker.Random.Int(5, 20), faker),
                HighBlock = HighBlockResponseFactory.Bogus(faker.Random.Int(5, 20), faker),
                MatchPlayAverage = MatchPlayAverageResponseFactory.Bogus(faker.Random.Int(5, 20), faker),
                MatchPlayRecord = MatchPlayRecordResponseFactory.Bogus(faker.Random.Int(5, 20), faker),
                MatchPlayAppearances = MatchPlayAppearancesResponseFactory.Bogus(faker.Random.Int(5, 20), faker),
                PointsPerEntry = PointsPerEntryResponseFactory.Bogus(faker.Random.Int(5, 20), faker),
                PointsPerTournament = PointsPerTournamentResponseFactory.Bogus(faker.Random.Int(5, 20), faker),
                FinalsPerEntry = FinalsPerEntryResponseFactory.Bogus(faker.Random.Int(5, 20), faker),
                AverageFinishes = AverageFinishResponseFactory.Bogus(faker.Random.Int(5, 20), faker),
                SeasonAtAGlance = SeasonAtAGlanceResponseFactory.Bogus(1, faker).Single(),
                SeasonsBests = SeasonBestsResponseFactory.Bogus(1, faker).Single(),
                FieldMatchPlaySummary = FieldMatchPlaySummaryResponseFactory.Bogus(1, faker).Single(),
                OpenPointsRace = PointsRaceSeriesResponseFactory.Bogus(faker.Random.Int(3, 10), faker),
                SeniorPointsRace = [],
                SuperSeniorPointsRace = [],
                WomenPointsRace = [],
                YouthPointsRace = [],
                RookiePointsRace = [],
                AllBowlers = FullStatModalRowResponseFactory.Bogus(faker.Random.Int(10, 30), faker),
                MinimumNumberOfGames = faker.Random.Decimal(10, 60),
                MinimumNumberOfTournaments = faker.Random.Decimal(2, 8),
                MinimumNumberOfEntries = faker.Random.Decimal(3, 12)
            };
        })];
    }

    public static IReadOnlyCollection<GetSeasonStatsResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}