using Bogus;
using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class FieldMatchPlaySummaryResponseFactory
{
    public const decimal ValidHighestWinPercentage = 66.67m;
    public const int ValidMostFinals = 5;
    public const string ValidBowlerName = "Jane Smith";

    public static FieldMatchPlaySummaryResponse Create(
        decimal? highestWinPercentage = null,
        IReadOnlyCollection<string>? highestWinPercentageBowlers = null,
        int? mostFinals = null,
        IReadOnlyCollection<string>? mostFinalsBowlers = null)
        => new()
        {
            HighestWinPercentage = highestWinPercentage ?? ValidHighestWinPercentage,
            HighestWinPercentageBowlers = highestWinPercentageBowlers ?? [ValidBowlerName],
            MostFinals = mostFinals ?? ValidMostFinals,
            MostFinalsBowlers = mostFinalsBowlers ?? [ValidBowlerName]
        };

    public static IReadOnlyCollection<FieldMatchPlaySummaryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<FieldMatchPlaySummaryResponse>()
            .CustomInstantiator(f => new FieldMatchPlaySummaryResponse
            {
                HighestWinPercentage = f.Random.Decimal(50, 100),
                HighestWinPercentageBowlers = [f.Name.FullName()],
                MostFinals = f.Random.Int(1, 15),
                MostFinalsBowlers = [f.Name.FullName()]
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
