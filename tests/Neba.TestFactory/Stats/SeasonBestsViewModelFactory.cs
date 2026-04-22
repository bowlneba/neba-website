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

    public static IReadOnlyCollection<SeasonBestsViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SeasonBestsViewModel>()
            .CustomInstantiator(f => new SeasonBestsViewModel
            {
                HighGame = f.Random.Int(270, 300),
                HighGameBowlers = new Dictionary<string, string>
                {
                    { Ulid.BogusString(f), f.Name.FullName() },
                    { Ulid.BogusString(f), f.Name.FullName() },
                    { Ulid.BogusString(f), f.Name.FullName() }
                },
                HighBlock = f.Random.Int(1250, 1350),
                HighBlockBowlers = new Dictionary<string, string> { { Ulid.BogusString(f), f.Name.FullName() } },
                HighAverage = f.Random.Decimal(220, 235),
                HighAverageBowlers = new Dictionary<string, string> { { Ulid.BogusString(f), f.Name.FullName() } },
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}