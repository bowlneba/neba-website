using Neba.Website.Server.Stats;

namespace Neba.TestFactory.Stats;

public static class FieldMatchPlaySummaryViewModelFactory
{
    public const decimal ValidHighestWinPercentage = 0.75m;
    public const int ValidMostFinals = 8;
    public const string ValidBowlerName = "Test Bowler";

    public static FieldMatchPlaySummaryViewModel Create(
        decimal? highestWinPercentage = null,
        IReadOnlyDictionary<string, string>? highestWinPercentageBowlers = null,
        int? mostFinals = null,
        IReadOnlyDictionary<string, string>? mostFinalsBowlers = null)
        => new()
        {
            HighestWinPercentage = highestWinPercentage ?? ValidHighestWinPercentage,
            HighestWinPercentageBowlers = highestWinPercentageBowlers ?? new Dictionary<string, string> { { Ulid.NewUlid().ToString(), ValidBowlerName } },
            MostFinals = mostFinals ?? ValidMostFinals,
            MostFinalsBowlers = mostFinalsBowlers ?? new Dictionary<string, string> { { Ulid.NewUlid().ToString(), ValidBowlerName } },
        };

    internal static IReadOnlyCollection<FieldMatchPlaySummaryViewModel> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new FieldMatchPlaySummaryViewModel
        {
            HighestWinPercentage = faker.Random.Decimal(0, 1),
            HighestWinPercentageBowlers = new Dictionary<string, string>
            {
                { Ulid.BogusString(faker), faker.Name.FullName() },
                { Ulid.BogusString(faker), faker.Name.FullName() }
            },
            MostFinals = faker.Random.Int(0, 20),
            MostFinalsBowlers = new Dictionary<string, string> { { Ulid.BogusString(faker), faker.Name.FullName() } },
        })];
    }

    public static IReadOnlyCollection<FieldMatchPlaySummaryViewModel> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}