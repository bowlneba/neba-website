using Neba.Api.Features.Stats.GetSeasonStats;
using Neba.Api.Features.Bowlers.Domain;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Stats;

public static class MatchPlayAverageDtoFactory
{
    public const decimal ValidMatchPlayAverage = 200.00m;
    public const int ValidGames = 6;
    public const int ValidWins = 4;
    public const int ValidLosses = 2;
    public const decimal ValidWinPercentage = 66.67m;
    public const decimal ValidWinnings = 2500m;

    public static MatchPlayAverageDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        decimal? matchPlayAverage = null,
        int? games = null,
        int? wins = null,
        int? losses = null,
        decimal? winPercentage = null,
        decimal? winnings = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create(),
            MatchPlayAverage = matchPlayAverage ?? ValidMatchPlayAverage,
            Games = games ?? ValidGames,
            Wins = wins ?? ValidWins,
            Losses = losses ?? ValidLosses,
            WinPercentage = winPercentage ?? ValidWinPercentage,
            Winnings = winnings ?? ValidWinnings
        };

    public static IReadOnlyCollection<MatchPlayAverageDto> Bogus(int count, int? seed = null)
    {
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, seed), seed);

        var faker = new Faker<MatchPlayAverageDto>()
            .CustomInstantiator(f => new MatchPlayAverageDto
            {
                BowlerId = new BowlerId(Ulid.BogusString(f)),
                BowlerName = bowlerNamePool.GetNext(),
                MatchPlayAverage = f.Random.Decimal(150, 230),
                Games = f.Random.Int(2, 20),
                Wins = f.Random.Int(0, 10),
                Losses = f.Random.Int(0, 10),
                WinPercentage = f.Random.Decimal(0, 100),
                Winnings = f.Random.Decimal(0, 5000)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}