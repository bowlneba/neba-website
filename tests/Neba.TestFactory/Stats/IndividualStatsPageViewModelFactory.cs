using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class IndividualStatsPageViewModelFactory
{
    public const string ValidBowlerName = "Test Bowler";
    public const string ValidSelectedSeason = "2024-2025";
    public const int ValidPoints = 50;
    public const decimal ValidAverage = 210m;
    public const int ValidGames = 24;
    public const int ValidFinals = 3;
    public const int ValidEntries = 6;
    public const int ValidTournaments = 5;
    public const decimal ValidWinnings = 500m;
    public const decimal ValidFieldAverage = 5m;
    public const int ValidMatchPlayWins = 6;
    public const int ValidMatchPlayLosses = 4;

    public static IndividualStatsPageViewModel Create(
        string? bowlerId = null,
        string? bowlerName = null,
        string? selectedSeason = null,
        IReadOnlyDictionary<int, string>? availableSeasons = null,
        int? points = null,
        decimal? average = null,
        int? games = null,
        int? finals = null,
        int? entries = null,
        int? tournaments = null,
        decimal? winnings = null,
        decimal? fieldAverage = null,
        int? matchPlayWins = null,
        int? matchPlayLosses = null,
        decimal? matchPlayAverage = null,
        int? bowlerOfTheYearRank = null,
        int? seniorOfTheYearRank = null,
        int? superSeniorOfTheYearRank = null,
        int? womanOfTheYearRank = null,
        int? rookieOfTheYearRank = null,
        int? youthOfTheYearRank = null,
        int? highAverageRank = null,
        int? highBlockRank = null,
        int? matchPlayAverageRank = null,
        int? matchPlayRecordRank = null,
        int? matchPlayAppearancesRank = null,
        int? pointsPerEntryRank = null,
        int? pointsPerTournamentRank = null,
        int? finalsPerEntryRank = null,
        int? averageFinishRank = null,
        PointsRaceSeriesViewModel? bowlerOfTheYearPointsRace = null)
        => new()
        {
            BowlerId = bowlerId ?? Ulid.NewUlid().ToString(),
            BowlerName = bowlerName ?? ValidBowlerName,
            SelectedSeason = selectedSeason ?? ValidSelectedSeason,
            AvailableSeasons = availableSeasons ?? new Dictionary<int, string> { [DateTime.Now.Year] = ValidSelectedSeason },
            Points = points ?? ValidPoints,
            Average = average ?? ValidAverage,
            Games = games ?? ValidGames,
            Finals = finals ?? ValidFinals,
            Entries = entries ?? ValidEntries,
            Tournaments = tournaments ?? ValidTournaments,
            Winnings = winnings ?? ValidWinnings,
            FieldAverage = fieldAverage ?? ValidFieldAverage,
            MatchPlayWins = matchPlayWins ?? ValidMatchPlayWins,
            MatchPlayLosses = matchPlayLosses ?? ValidMatchPlayLosses,
            MatchPlayAverage = matchPlayAverage,
            BowlerOfTheYearRank = bowlerOfTheYearRank,
            SeniorOfTheYearRank = seniorOfTheYearRank,
            SuperSeniorOfTheYearRank = superSeniorOfTheYearRank,
            WomanOfTheYearRank = womanOfTheYearRank,
            RookieOfTheYearRank = rookieOfTheYearRank,
            YouthOfTheYearRank = youthOfTheYearRank,
            HighAverageRank = highAverageRank,
            HighBlockRank = highBlockRank,
            MatchPlayAverageRank = matchPlayAverageRank,
            MatchPlayRecordRank = matchPlayRecordRank,
            MatchPlayAppearancesRank = matchPlayAppearancesRank,
            PointsPerEntryRank = pointsPerEntryRank,
            PointsPerTournamentRank = pointsPerTournamentRank,
            FinalsPerEntryRank = finalsPerEntryRank,
            AverageFinishRank = averageFinishRank,
            BowlerOfTheYearPointsRace = bowlerOfTheYearPointsRace,
        };

    public static IReadOnlyCollection<IndividualStatsPageViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<IndividualStatsPageViewModel>()
            .CustomInstantiator(f =>
            {
                var finals = f.Random.Int(0, 5);
                var tournaments = finals + f.Random.Int(0, 5);
                var entries = tournaments + f.Random.Int(0, 5);
                var year = f.Date.Past(50).Year;

                return new IndividualStatsPageViewModel
                {
                    BowlerId = Ulid.BogusString(f),
                    BowlerName = f.Name.FullName(),
                    SelectedSeason = $"{year}-{year + 1}",
                    AvailableSeasons = Enumerable.Range(0, f.Random.Int(1, 5))
                        .ToDictionary(i => year - i, i => $"{year - i}-{year - i + 1}"),
                    Points = f.Random.Int(0, 200),
                    Average = f.Random.Decimal(150, 250),
                    Games = entries * f.Random.Int(3, 6),
                    Finals = finals,
                    Entries = entries,
                    Tournaments = tournaments,
                    Winnings = f.Random.Decimal(0, 10000),
                    FieldAverage = f.Random.Decimal(-20, 20),
                    MatchPlayWins = f.Random.Int(0, 20),
                    MatchPlayLosses = f.Random.Int(0, 20),
                    MatchPlayAverage = f.Random.Bool() ? f.Random.Decimal(150, 250) : null,
                    BowlerOfTheYearRank = f.Random.Bool() ? f.Random.Int(1, 50) : null,
                    SeniorOfTheYearRank = f.Random.Bool() ? f.Random.Int(1, 50) : null,
                    SuperSeniorOfTheYearRank = f.Random.Bool() ? f.Random.Int(1, 50) : null,
                    WomanOfTheYearRank = f.Random.Bool() ? f.Random.Int(1, 50) : null,
                    RookieOfTheYearRank = f.Random.Bool() ? f.Random.Int(1, 50) : null,
                    YouthOfTheYearRank = f.Random.Bool() ? f.Random.Int(1, 50) : null,
                    HighAverageRank = f.Random.Bool() ? f.Random.Int(1, 50) : null,
                    HighBlockRank = f.Random.Bool() ? f.Random.Int(1, 50) : null,
                    MatchPlayAverageRank = f.Random.Bool() ? f.Random.Int(1, 50) : null,
                    MatchPlayRecordRank = f.Random.Bool() ? f.Random.Int(1, 50) : null,
                    MatchPlayAppearancesRank = f.Random.Bool() ? f.Random.Int(1, 50) : null,
                    PointsPerEntryRank = f.Random.Bool() ? f.Random.Int(1, 50) : null,
                    PointsPerTournamentRank = f.Random.Bool() ? f.Random.Int(1, 50) : null,
                    FinalsPerEntryRank = f.Random.Bool() ? f.Random.Int(1, 50) : null,
                    AverageFinishRank = f.Random.Bool() ? f.Random.Int(1, 50) : null,
                    BowlerOfTheYearPointsRace = f.Random.Bool()
                        ? PointsRaceSeriesViewModelFactory.Bogus(1, seed).Single()
                        : null,
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}