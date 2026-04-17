using Bogus;
using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class SeasonBestsResponseFactory
{
    public const int ValidHighGame = 279;
    public const int ValidHighBlock = 1250;
    public const decimal ValidHighAverage = 225.35m;
    public const string ValidBowlerName = "Jane Smith";

    public static SeasonBestsResponse Create(
        int? highGame = null,
        IReadOnlyCollection<string>? highGameBowlers = null,
        int? highBlock = null,
        IReadOnlyCollection<string>? highBlockBowlers = null,
        decimal? highAverage = null,
        IReadOnlyCollection<string>? highAverageBowlers = null)
        => new()
        {
            HighGame = highGame ?? ValidHighGame,
            HighGameBowlers = highGameBowlers ?? [ValidBowlerName],
            HighBlock = highBlock ?? ValidHighBlock,
            HighBlockBowlers = highBlockBowlers ?? [ValidBowlerName],
            HighAverage = highAverage ?? ValidHighAverage,
            HighAverageBowlers = highAverageBowlers ?? [ValidBowlerName]
        };

    public static IReadOnlyCollection<SeasonBestsResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SeasonBestsResponse>()
            .CustomInstantiator(f => new SeasonBestsResponse
            {
                HighGame = f.Random.Int(270, 300),
                HighGameBowlers = [f.Name.FullName()],
                HighBlock = f.Random.Int(1200, 1400),
                HighBlockBowlers = [f.Name.FullName()],
                HighAverage = f.Random.Decimal(210, 240),
                HighAverageBowlers = [f.Name.FullName()]
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
