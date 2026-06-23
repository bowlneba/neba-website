using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class MatchPlayAverageResponseFactory
{
    public const string ValidBowlerId = "01JWXYZTEST000000000000006";
    public const string ValidBowlerName = "Jane Smith";
    public const decimal ValidMatchPlayAverage = 218.0m;
    public const int ValidGames = 15;
    public const int ValidWins = 9;
    public const int ValidLosses = 6;
    public const decimal ValidWinPercentage = 60.0m;
    public const decimal ValidWinnings = 1500m;

    public static MatchPlayAverageResponse Create(
        string? bowlerId = null,
        string? bowlerName = null,
        decimal? matchPlayAverage = null,
        int? games = null,
        int? wins = null,
        int? losses = null,
        decimal? winPercentage = null,
        decimal? winnings = null)
        => new()
        {
            BowlerId = bowlerId ?? ValidBowlerId,
            BowlerName = bowlerName ?? ValidBowlerName,
            MatchPlayAverage = matchPlayAverage ?? ValidMatchPlayAverage,
            Games = games ?? ValidGames,
            Wins = wins ?? ValidWins,
            Losses = losses ?? ValidLosses,
            WinPercentage = winPercentage ?? ValidWinPercentage,
            Winnings = winnings ?? ValidWinnings
        };

    public static IReadOnlyCollection<MatchPlayAverageResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var wins = faker.Random.Int(1, 20);
            var losses = faker.Random.Int(1, 20);
            return new MatchPlayAverageResponse
            {
                BowlerId = Ulid.BogusString(faker),
                BowlerName = faker.Name.FullName(),
                MatchPlayAverage = faker.Random.Decimal(180, 240),
                Games = wins + losses,
                Wins = wins,
                Losses = losses,
                WinPercentage = Math.Round((decimal)wins / (wins + losses) * 100, 2),
                Winnings = faker.Random.Decimal(0, 5000)
            };
        })];
    }

    public static IReadOnlyCollection<MatchPlayAverageResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}