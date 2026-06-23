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

    public static IReadOnlyCollection<SeasonBestsResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new SeasonBestsResponse
        {
            HighGame = faker.Random.Int(270, 300),
            HighGameBowlers = new Dictionary<string, string> { { Ulid.BogusString(faker), faker.Name.FullName() } },
            HighBlock = faker.Random.Int(1200, 1400),
            HighBlockBowlers = new Dictionary<string, string> { { Ulid.BogusString(faker), faker.Name.FullName() } },
            HighAverage = faker.Random.Decimal(210, 240),
            HighAverageBowlers = new Dictionary<string, string> { { Ulid.BogusString(faker), faker.Name.FullName() } }
        })];
    }

    public static IReadOnlyCollection<SeasonBestsResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}