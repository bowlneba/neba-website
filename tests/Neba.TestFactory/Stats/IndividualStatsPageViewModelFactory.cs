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
        IReadOnlyCollection<IndividualBoyProgressionViewModel>? boyProgressions = null)
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
            BoyProgressions = boyProgressions ?? [],
        };

    internal static IReadOnlyCollection<IndividualStatsPageViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var finals = faker.Random.Int(0, 5);
            var tournaments = finals + faker.Random.Int(0, 5);
            var entries = tournaments + faker.Random.Int(0, 5);
            var year = faker.Date.Past(50).Year;
            return new IndividualStatsPageViewModel
            {
                BowlerId = Ulid.BogusString(faker),
                BowlerName = faker.Name.FullName(),
                SelectedSeason = $"{year}-{year + 1}",
                AvailableSeasons = Enumerable.Range(0, faker.Random.Int(1, 5))
                    .ToDictionary(i => year - i, i => $"{year - i}-{year - i + 1}"),
                Points = faker.Random.Int(0, 200),
                Average = faker.Random.Decimal(150, 250),
                Games = entries * faker.Random.Int(3, 6),
                Finals = finals,
                Entries = entries,
                Tournaments = tournaments,
                Winnings = faker.Random.Decimal(0, 10000),
                FieldAverage = faker.Random.Decimal(-20, 20),
                MatchPlayWins = faker.Random.Int(0, 20),
                MatchPlayLosses = faker.Random.Int(0, 20),
                MatchPlayAverage = faker.Random.Bool() ? faker.Random.Decimal(150, 250) : null,
                BowlerOfTheYearRank = faker.Random.Bool() ? faker.Random.Int(1, 50) : null,
                SeniorOfTheYearRank = faker.Random.Bool() ? faker.Random.Int(1, 50) : null,
                SuperSeniorOfTheYearRank = faker.Random.Bool() ? faker.Random.Int(1, 50) : null,
                WomanOfTheYearRank = faker.Random.Bool() ? faker.Random.Int(1, 50) : null,
                RookieOfTheYearRank = faker.Random.Bool() ? faker.Random.Int(1, 50) : null,
                YouthOfTheYearRank = faker.Random.Bool() ? faker.Random.Int(1, 50) : null,
                HighAverageRank = faker.Random.Bool() ? faker.Random.Int(1, 50) : null,
                HighBlockRank = faker.Random.Bool() ? faker.Random.Int(1, 50) : null,
                MatchPlayAverageRank = faker.Random.Bool() ? faker.Random.Int(1, 50) : null,
                MatchPlayRecordRank = faker.Random.Bool() ? faker.Random.Int(1, 50) : null,
                MatchPlayAppearancesRank = faker.Random.Bool() ? faker.Random.Int(1, 50) : null,
                PointsPerEntryRank = faker.Random.Bool() ? faker.Random.Int(1, 50) : null,
                PointsPerTournamentRank = faker.Random.Bool() ? faker.Random.Int(1, 50) : null,
                FinalsPerEntryRank = faker.Random.Bool() ? faker.Random.Int(1, 50) : null,
                AverageFinishRank = faker.Random.Bool() ? faker.Random.Int(1, 50) : null,
                BoyProgressions = [],
            };
        })];
    }

    public static IReadOnlyCollection<IndividualStatsPageViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}