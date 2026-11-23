using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;

namespace Neba.TestFactory.Series;

public static class BowlerPointsFactFactory
{
    public const int ValidPoints = 100;

    public static BowlerPointsFact Create(BowlerId? bowlerId = null, int? points = null)
        => new(bowlerId ?? BowlerId.New(), points ?? ValidPoints);

    public static IReadOnlyList<BowlerPointsFact> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            new BowlerId(Ulid.BogusString(faker)),
            faker.Random.Int(1, 500)))];
    }
}