using Bogus;

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
        IReadOnlyCollection<PointsRaceSeriesResponse>? bowlerOfTheYearPointsRace = null,
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
            BowlerOfTheYearPointsRace = bowlerOfTheYearPointsRace ?? [PointsRaceSeriesResponseFactory.Create()],
            AllBowlers = allBowlers ?? [FullStatModalRowResponseFactory.Create()]
        };

    public static IReadOnlyCollection<GetSeasonStatsResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<GetSeasonStatsResponse>()
            .CustomInstantiator(f =>
            {
                var year = f.Random.Int(2000, 2030);
                return new GetSeasonStatsResponse
                {
                    SelectedSeason = $"{year}-{year + 1} Season",
                    AvailableSeasons = Enumerable.Range(0, f.Random.Int(1, 5))
                        .ToDictionary(i => year - i, i => $"{year - i - 1}-{year - i} Season"),
                    BowlerSearchList = Enumerable.Range(0, f.Random.Int(5, 20))
                        .ToDictionary(_ => Ulid.BogusString(f), _ => f.Name.FullName()),
                    BowlerOfTheYear = BowlerOfTheYearStandingResponseFactory.Bogus(f.Random.Int(5, 20), seed),
                    SeniorOfTheYear = BowlerOfTheYearStandingResponseFactory.Bogus(f.Random.Int(1, 5), seed),
                    SuperSeniorOfTheYear = BowlerOfTheYearStandingResponseFactory.Bogus(f.Random.Int(1, 5), seed),
                    WomanOfTheYear = BowlerOfTheYearStandingResponseFactory.Bogus(f.Random.Int(1, 5), seed),
                    RookieOfTheYear = BowlerOfTheYearStandingResponseFactory.Bogus(f.Random.Int(1, 5), seed),
                    YouthOfTheYear = BowlerOfTheYearStandingResponseFactory.Bogus(f.Random.Int(0, 3), seed),
                    HighAverage = HighAverageResponseFactory.Bogus(f.Random.Int(5, 20), seed),
                    HighBlock = HighBlockResponseFactory.Bogus(f.Random.Int(5, 20), seed),
                    MatchPlayAverage = MatchPlayAverageResponseFactory.Bogus(f.Random.Int(5, 20), seed),
                    MatchPlayRecord = MatchPlayRecordResponseFactory.Bogus(f.Random.Int(5, 20), seed),
                    MatchPlayAppearances = MatchPlayAppearancesResponseFactory.Bogus(f.Random.Int(5, 20), seed),
                    PointsPerEntry = PointsPerEntryResponseFactory.Bogus(f.Random.Int(5, 20), seed),
                    PointsPerTournament = PointsPerTournamentResponseFactory.Bogus(f.Random.Int(5, 20), seed),
                    FinalsPerEntry = FinalsPerEntryResponseFactory.Bogus(f.Random.Int(5, 20), seed),
                    AverageFinishes = AverageFinishResponseFactory.Bogus(f.Random.Int(5, 20), seed),
                    SeasonAtAGlance = SeasonAtAGlanceResponseFactory.Bogus(1, seed).Single(),
                    SeasonsBests = SeasonBestsResponseFactory.Bogus(1, seed).Single(),
                    FieldMatchPlaySummary = FieldMatchPlaySummaryResponseFactory.Bogus(1, seed).Single(),
                    BowlerOfTheYearPointsRace = PointsRaceSeriesResponseFactory.Bogus(f.Random.Int(3, 10), seed),
                    AllBowlers = FullStatModalRowResponseFactory.Bogus(f.Random.Int(10, 30), seed),
                    MinimumNumberOfGames = f.Random.Decimal(10, 60),
                    MinimumNumberOfTournaments = f.Random.Decimal(2, 8),
                    MinimumNumberOfEntries = f.Random.Decimal(3, 12)
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}