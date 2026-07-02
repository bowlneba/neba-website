using Neba.Api.Features.BowlingCenters.Domain;

namespace Neba.TestFactory.BowlingCenters;

public static class LaneConfigurationFactory
{
    public static LaneConfiguration Create(IReadOnlyList<LaneRange>? ranges = null)
        => LaneConfiguration.Create(ranges ?? [LaneRangeFactory.Create()]).Value;

    internal static IReadOnlyCollection<LaneConfiguration> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
            Create([.. LaneRangeFactory.Bogus(1, faker)]))];
    }

    public static IReadOnlyCollection<LaneConfiguration> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}