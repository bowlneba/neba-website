using Bogus;

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
        IReadOnlyDictionary<Ulid, string>? highGameBowlers = null,
        int? highBlock = null,
        IReadOnlyDictionary<Ulid, string>? highBlockBowlers = null,
        decimal? highAverage = null,
        IReadOnlyDictionary<Ulid, string>? highAverageBowlers = null)
        => new()
        {
            HighGame = highGame ?? ValidHighGame,
            HighGameBowlers = highGameBowlers ?? new Dictionary<Ulid, string> { { Ulid.NewUlid(), ValidBowlerName } },
            HighBlock = highBlock ?? ValidHighBlock,
            HighBlockBowlers = highBlockBowlers ?? new Dictionary<Ulid, string> { { Ulid.NewUlid(), ValidBowlerName } },
            HighAverage = highAverage ?? ValidHighAverage,
            HighAverageBowlers = highAverageBowlers ?? new Dictionary<Ulid, string> { { Ulid.NewUlid(), ValidBowlerName } },
        };

    public static IReadOnlyCollection<SeasonBestsViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SeasonBestsViewModel>()
            .CustomInstantiator(f => new SeasonBestsViewModel
            {
                HighGame = f.Random.Int(270, 300),
                HighGameBowlers = new Dictionary<Ulid, string>
                {
                    { Ulid.Bogus(f, f.Date.Past()), f.Name.FullName() },
                    { Ulid.Bogus(f, f.Date.Past()), f.Name.FullName() },
                    { Ulid.Bogus(f, f.Date.Past()), f.Name.FullName() }
                },
                HighBlock = f.Random.Int(1250, 1350),
                HighBlockBowlers = new Dictionary<Ulid, string> { { Ulid.Bogus(f), f.Name.FullName() } },
                HighAverage = f.Random.Decimal(220, 235),
                HighAverageBowlers = new Dictionary<Ulid, string> { { Ulid.Bogus(f), f.Name.FullName() } },
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}