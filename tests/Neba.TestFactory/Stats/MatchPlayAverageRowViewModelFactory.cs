using Bogus;

using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class MatchPlayAverageRowViewModelFactory
{
    public const string ValidBowlerName = "Test Bowler";
    public const int ValidRank = 1;
    public const decimal ValidMatchPlayAverage = 200m;
    public const int ValidGames = 10;
    public const int ValidWins = 5;
    public const int ValidLoses = 5;
    public const decimal ValidWinnings = 1000m;

    public static MatchPlayAverageRowViewModel Create(
        int? rank = null,
        string? bowlerId = null,
        string? bowlerName = null,
        decimal? matchPlayAverage = null,
        int? games = null,
        int? wins = null,
        int? loses = null,
        decimal? winnings = null)
        => new()
        {
            Rank = rank ?? ValidRank,
            BowlerId = bowlerId ?? Ulid.NewUlid().ToString(),
            BowlerName = bowlerName ?? ValidBowlerName,
            MatchPlayAverage = matchPlayAverage ?? ValidMatchPlayAverage,
            Games = games ?? ValidGames,
            Wins = wins ?? ValidWins,
            Loses = loses ?? ValidLoses,
            Winnings = winnings ?? ValidWinnings
        };

    public static IReadOnlyCollection<MatchPlayAverageRowViewModel> Bogus(int count, int? seed = null)
    {
        var rank = 1;
        const decimal maxAverage = 250m;
        const decimal minAverage = 150m;
        var step = count > 1
            ? (maxAverage - minAverage) / (count - 1)
            : 0m;

        var faker = new Faker<MatchPlayAverageRowViewModel>()
            .CustomInstantiator(f =>
            {
                var currentRank = rank++;

                return new MatchPlayAverageRowViewModel
                {
                    Rank = currentRank,
                    BowlerId = Ulid.BogusString(f),
                    BowlerName = f.Name.FullName(),
                    MatchPlayAverage = decimal.Round(maxAverage - ((currentRank - 1) * step), 2),
                    Games = f.Random.Int(0, 20),
                    Wins = f.Random.Int(0, 20),
                    Loses = f.Random.Int(0, 20),
                    Winnings = f.Random.Decimal(0, 10000)
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}