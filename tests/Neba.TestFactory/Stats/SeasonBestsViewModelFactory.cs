using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class SeasonBestsViewModelFactory
{
    public const int ValidHighGame = 279;
    public const int ValidHighBlock = 1250;
    public const decimal ValidHighAverage = 225.35m;
    public const string ValidBowlerName = "Test Bowler";

    public static SeasonBestsViewModel Create(
        int? highGame = null,
        IReadOnlyDictionary<string, string>? highGameBowlers = null,
        int? highBlock = null,
        IReadOnlyDictionary<string, string>? highBlockBowlers = null,
        decimal? highAverage = null,
        IReadOnlyDictionary<string, string>? highAverageBowlers = null)
        => new()
        {
            HighGame = highGame ?? ValidHighGame,
            HighGameBowlers = highGameBowlers ?? new Dictionary<string, string> { { Ulid.NewUlid().ToString(), ValidBowlerName } },
            HighBlock = highBlock ?? ValidHighBlock,
            HighBlockBowlers = highBlockBowlers ?? new Dictionary<string, string> { { Ulid.NewUlid().ToString(), ValidBowlerName } },
            HighAverage = highAverage ?? ValidHighAverage,
            HighAverageBowlers = highAverageBowlers ?? new Dictionary<string, string> { { Ulid.NewUlid().ToString(), ValidBowlerName } },
        };

    internal static IReadOnlyCollection<SeasonBestsViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new SeasonBestsViewModel
        {
            HighGame = faker.Random.Int(270, 300),
            HighGameBowlers = new Dictionary<string, string>
            {
                { Ulid.BogusString(faker), faker.Name.FullName() },
                { Ulid.BogusString(faker), faker.Name.FullName() },
                { Ulid.BogusString(faker), faker.Name.FullName() }
            },
            HighBlock = faker.Random.Int(1250, 1350),
            HighBlockBowlers = new Dictionary<string, string> { { Ulid.BogusString(faker), faker.Name.FullName() } },
            HighAverage = faker.Random.Decimal(220, 235),
            HighAverageBowlers = new Dictionary<string, string> { { Ulid.BogusString(faker), faker.Name.FullName() } },
        })];
    }

    public static IReadOnlyCollection<SeasonBestsViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}