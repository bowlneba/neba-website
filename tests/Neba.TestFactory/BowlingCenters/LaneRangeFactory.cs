using Neba.Api.Features.BowlingCenters.Domain;

namespace Neba.TestFactory.BowlingCenters;

public static class LaneRangeFactory
{
    public const int ValidStartLane = 1;
    public const int ValidEndLane = 10;
    public static readonly PinFallType ValidPinFallType = PinFallType.FreeFall;

    public static LaneRange Create(int? startLane = null, int? endLane = null, PinFallType? pinFallType = null)
        => LaneRange.Create(
            startLane ?? ValidStartLane,
            endLane ?? ValidEndLane,
            pinFallType ?? ValidPinFallType).Value;

    internal static IReadOnlyCollection<LaneRange> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var startPairIndex = faker.Random.Int(1, 30);
            var startLane = (startPairIndex * 2) - 1;
            var pairCount = faker.Random.Int(1, 15);
            var endLane = startLane + (pairCount * 2) - 1;
            return Create(startLane, endLane, faker.PickRandom(PinFallType.List.ToArray()));
        })];
    }

    public static IReadOnlyCollection<LaneRange> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}