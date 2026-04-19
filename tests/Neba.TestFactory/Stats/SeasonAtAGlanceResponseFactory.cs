using Bogus;

using Neba.Api.Contracts.Stats.GetSeasonStats;

namespace Neba.TestFactory.Stats;

public static class SeasonAtAGlanceResponseFactory
{
    public const int ValidTotalEntries = 245;
    public const decimal ValidTotalPrizeMoney = 18500m;

    public static SeasonAtAGlanceResponse Create(
        int? totalEntries = null,
        decimal? totalPrizeMoney = null)
        => new()
        {
            TotalEntries = totalEntries ?? ValidTotalEntries,
            TotalPrizeMoney = totalPrizeMoney ?? ValidTotalPrizeMoney
        };

    public static IReadOnlyCollection<SeasonAtAGlanceResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<SeasonAtAGlanceResponse>()
            .CustomInstantiator(f => new SeasonAtAGlanceResponse
            {
                TotalEntries = f.Random.Int(50, 500),
                TotalPrizeMoney = f.Random.Decimal(5000, 50000)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}