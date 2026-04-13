using Bogus;

using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class FieldMatchPlaySummaryViewModelFactory
{
    public const decimal ValidHighestWinPercentage = 0.75m;
    public const int ValidMostFinals = 8;
    public const string ValidBowlerName = "Test Bowler";

    public static FieldMatchPlaySummaryViewModel Create(
        decimal? highestWinPercentage = null,
        IReadOnlyDictionary<Ulid, string>? highestWinPercentageBowlers = null,
        int? mostFinals = null,
        IReadOnlyDictionary<Ulid, string>? mostFinalsBowlers = null)
        => new()
        {
            HighestWinPercentage = highestWinPercentage ?? ValidHighestWinPercentage,
            HighestWinPercentageBowlers = highestWinPercentageBowlers ?? new Dictionary<Ulid, string> { { Ulid.NewUlid(), ValidBowlerName } },
            MostFinals = mostFinals ?? ValidMostFinals,
            MostFinalsBowlers = mostFinalsBowlers ?? new Dictionary<Ulid, string> { { Ulid.NewUlid(), ValidBowlerName } },
        };

    public static IReadOnlyCollection<FieldMatchPlaySummaryViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<FieldMatchPlaySummaryViewModel>()
            .CustomInstantiator(f => new FieldMatchPlaySummaryViewModel
            {
                HighestWinPercentage = f.Random.Decimal(0, 1),
                HighestWinPercentageBowlers = new Dictionary<Ulid, string>
                {
                    { Ulid.Bogus(f, f.Date.Past()), f.Name.FullName() },
                    { Ulid.Bogus(f, f.Date.Past()), f.Name.FullName() }
                },
                MostFinals = f.Random.Int(0, 20),
                MostFinalsBowlers = new Dictionary<Ulid, string> { { Ulid.Bogus(f), f.Name.FullName() } },
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}