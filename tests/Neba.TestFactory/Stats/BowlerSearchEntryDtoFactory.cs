using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Bowlers;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Stats;

public static class BowlerSearchEntryDtoFactory
{
    public static BowlerSearchEntryDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create()
        };

    public static IReadOnlyCollection<BowlerSearchEntryDto> Bogus(int count, int? seed = null)
    {
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, seed), seed);

        var faker = new Faker<BowlerSearchEntryDto>()
            .CustomInstantiator(f => new BowlerSearchEntryDto
            {
                BowlerId = new BowlerId(Ulid.BogusString(f)),
                BowlerName = bowlerNamePool.GetNext()
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}