using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class FieldMatchPlaySummaryResponseFactory
{
    public const decimal ValidHighestWinPercentage = 66.67m;
    public const int ValidMostFinals = 5;
    public const string ValidBowlerId = "01JWXYZTEST000000000000002";
    public const string ValidBowlerName = "Jane Smith";

    public static FieldMatchPlaySummaryResponse Create(
        decimal? highestWinPercentage = null,
        IReadOnlyDictionary<string, string>? highestWinPercentageBowlers = null,
        int? mostFinals = null,
        IReadOnlyDictionary<string, string>? mostFinalsBowlers = null)
        => new()
        {
            HighestWinPercentage = highestWinPercentage ?? ValidHighestWinPercentage,
            HighestWinPercentageBowlers = highestWinPercentageBowlers ?? new Dictionary<string, string> { { ValidBowlerId, ValidBowlerName } },
            MostFinals = mostFinals ?? ValidMostFinals,
            MostFinalsBowlers = mostFinalsBowlers ?? new Dictionary<string, string> { { ValidBowlerId, ValidBowlerName } }
        };

    public static IReadOnlyCollection<FieldMatchPlaySummaryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<FieldMatchPlaySummaryResponse>()
            .CustomInstantiator(f => new FieldMatchPlaySummaryResponse
            {
                HighestWinPercentage = f.Random.Decimal(50, 100),
                HighestWinPercentageBowlers = new Dictionary<string, string> { { Ulid.BogusString(f), f.Name.FullName() } },
                MostFinals = f.Random.Int(1, 15),
                MostFinalsBowlers = new Dictionary<string, string> { { Ulid.BogusString(f), f.Name.FullName() } }
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}