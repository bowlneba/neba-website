using Bogus;

using Neba.Domain.Bowlers;
using Neba.Domain.Seasons;

namespace Neba.TestFactory.Seasons;

public static class HighBlockAwardFactory
{
    public static HighBlockAward Create(
        SeasonAwardId? id = null,
        BowlerId? bowlerId = null,
        int? blockScore = null)
    {
        return new HighBlockAward
        {
            Id = id ?? SeasonAwardId.New(),
            BowlerId = bowlerId ?? BowlerId.New(),
            BlockScore = blockScore ?? 1300
        };
    }

    public static IReadOnlyCollection<HighBlockAward> Bogus(
        int count,
        UniquePool<BowlerId>? bowlerIds = null,
        int? seed = null
    )
    {
        var faker = new Faker<HighBlockAward>()
            .CustomInstantiator(f => new()
            {
                Id = new SeasonAwardId(Ulid.BogusString(f)),
                BowlerId = bowlerIds?.GetNext() ?? new BowlerId(Ulid.BogusString(f)),
                BlockScore = f.Random.Int(1250, 1400)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}