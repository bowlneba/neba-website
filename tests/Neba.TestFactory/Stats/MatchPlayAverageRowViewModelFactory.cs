using Bogus;

using Neba.Domain.Bowlers;
using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class MatchPlayAverageRowViewModelFactory
{
    public static MatchPlayAverageRowViewModel Create(
        int? rank = null,
        BowlerId? bowlerId = null,
        string? bowlerName = null,
        decimal? matchPlayAverage = null,
        int? games = null,
        int? wins = null,
        int? loses = null,
        decimal? winnings = null)
        => new()
        {
            Rank = rank ?? 1,
            BowlerId = bowlerId?.Value ?? Ulid.NewUlid(),
            BowlerName = bowlerName ?? "Test Bowler",
            MatchPlayAverage = matchPlayAverage ?? 200m,
            Games = games ?? 10,
            Wins = wins ?? 5,
            Loses = loses ?? 5,
            Winnings = winnings ?? 1000m
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
                    BowlerId = Ulid.Bogus(f),
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