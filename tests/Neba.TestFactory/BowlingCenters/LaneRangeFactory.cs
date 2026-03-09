using Bogus;

using Neba.Domain.BowlingCenters;

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

    public static LaneRange Bogus(int? seed = null)
        => Bogus(1, seed).Single();

    public static IReadOnlyCollection<LaneRange> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<LaneRange>()
            .CustomInstantiator(f =>
            {
                var startPairIndex = f.Random.Int(1, 30);
                var startLane = (startPairIndex * 2) - 1;   // odd: 1, 3, 5, ..., 59
                var pairCount = f.Random.Int(1, 15);
                var endLane = startLane + (pairCount * 2) - 1;  // always even
                return Create(startLane, endLane, f.PickRandom(PinFallType.List.ToArray()));
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}