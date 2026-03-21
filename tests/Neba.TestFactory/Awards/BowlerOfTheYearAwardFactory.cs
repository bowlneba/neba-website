using Bogus;

using Neba.Domain.Awards;
using Neba.Domain.Bowlers;

namespace Neba.TestFactory.Awards;

public static class BowlerOfTheYearAwardFactory
{
    public static BowlerOfTheYearAward Create(
        SeasonAwardId? id = null,
        BowlerId? bowlerId = null,
        BowlerOfTheYearCategory? category = null)
    {
        return new BowlerOfTheYearAward
        {
            Id = id ?? SeasonAwardId.New(),
            BowlerId = bowlerId ?? BowlerId.New(),
            Category = category ?? BowlerOfTheYearCategory.Open
        };
    }

    public static IReadOnlyCollection<BowlerOfTheYearAward> Bogus(
        int count,
        UniquePool<BowlerId>? bowlerIds = null,
        int? seed = null
    )
    {
        var faker = new Faker<BowlerOfTheYearAward>()
            .CustomInstantiator(f => new()
            {
                Id = new SeasonAwardId(Ulid.Bogus(f)),
                BowlerId = bowlerIds?.GetNext() ?? new BowlerId(Ulid.Bogus(f)),
                Category = f.PickRandom(BowlerOfTheYearCategory.List.ToArray())
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}