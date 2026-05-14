using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class SeasonBestsResponseFactory
{
    public const int ValidHighGame = 279;
    public const int ValidHighBlock = 1250;
    public const decimal ValidHighAverage = 225.35m;
    public const string ValidBowlerId = "01JWXYZTEST000000000000002";
    public const string ValidBowlerName = "Jane Smith";

    public static SeasonBestsResponse Create(
        int? highGame = null,
        IReadOnlyDictionary<string, string>? highGameBowlers = null,
        int? highBlock = null,
        IReadOnlyDictionary<string, string>? highBlockBowlers = null,
        decimal? highAverage = null,
        IReadOnlyDictionary<string, string>? highAverageBowlers = null)
        => new()
        {
            HighGame = highGame ?? ValidHighGame,
            HighGameBowlers = highGameBowlers ?? new Dictionary<string, string> { { ValidBowlerId, ValidBowlerName } },
            HighBlock = highBlock ?? ValidHighBlock,
            HighBlockBowlers = highBlockBowlers ?? new Dictionary<string, string> { { ValidBowlerId, ValidBowlerName } },
            HighAverage = highAverage ?? ValidHighAverage,
            HighAverageBowlers = highAverageBowlers ?? new Dictionary<string, string> { { ValidBowlerId, ValidBowlerName } }
        };

    public static IReadOnlyCollection<SeasonBestsResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SeasonBestsResponse>()
            .CustomInstantiator(f => new SeasonBestsResponse
            {
                HighGame = f.Random.Int(270, 300),
                HighGameBowlers = new Dictionary<string, string> { { Ulid.BogusString(f), f.Name.FullName() } },
                HighBlock = f.Random.Int(1200, 1400),
                HighBlockBowlers = new Dictionary<string, string> { { Ulid.BogusString(f), f.Name.FullName() } },
                HighAverage = f.Random.Decimal(210, 240),
                HighAverageBowlers = new Dictionary<string, string> { { Ulid.BogusString(f), f.Name.FullName() } }
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}