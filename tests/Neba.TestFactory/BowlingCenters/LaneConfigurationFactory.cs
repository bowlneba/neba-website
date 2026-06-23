using Neba.Api.Features.BowlingCenters.Domain;

namespace Neba.TestFactory.BowlingCenters;

public static class LaneConfigurationFactory
{
    public static LaneConfiguration Create(IReadOnlyList<LaneRange>? ranges = null)
        => LaneConfiguration.Create(ranges ?? [LaneRangeFactory.Create()]).Value;

    public static IReadOnlyCollection<LaneConfiguration> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<LaneConfiguration>()
            .CustomInstantiator(f => Create([.. LaneRangeFactory.Bogus(1, f)]));

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<LaneConfiguration> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}