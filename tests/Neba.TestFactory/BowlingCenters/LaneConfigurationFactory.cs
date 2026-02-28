using Bogus;

using Neba.Domain.BowlingCenters;

namespace Neba.TestFactory.BowlingCenters;

public static class LaneConfigurationFactory
{
    public static LaneConfiguration Create(IReadOnlyList<LaneRange>? ranges = null)
        => LaneConfiguration.Create(ranges ?? [LaneRangeFactory.Create()]).Value;

    public static LaneConfiguration Bogus(int? seed = null)
        => Bogus(1, seed).Single();

    public static IReadOnlyCollection<LaneConfiguration> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<LaneConfiguration>()
            .CustomInstantiator(_ => Create([LaneRangeFactory.Bogus(seed)]));

        if (seed.HasValue)
            faker.UseSeed(seed.Value);

        return faker.Generate(count);
    }
}
