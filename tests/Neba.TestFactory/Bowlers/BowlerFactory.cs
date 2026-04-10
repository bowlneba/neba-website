using Bogus;

using Neba.Domain.Bowlers;

namespace Neba.TestFactory.Bowlers;

public static class BowlerFactory
{
    public static Bowler Create(
        BowlerId? id = null,
        Name? name = null,
        int? websiteId = null,
        int? legacyId = null)
        => new()
        {
            Id = id ?? BowlerId.New(),
            Name = name ?? NameFactory.Create(),
            WebsiteId = websiteId,
            LegacyId = legacyId
        };

    public static IReadOnlyCollection<Bowler> Bogus(int count, int? seed = null)
    {
        var websiteIdPool = UniquePool.CreateNullable(
            Enumerable.Range(1, count).Select(i => (int?)i),
            seed);

        var legacyIdPool = UniquePool.CreateNullable(
            Enumerable.Range(100_001, count).Select(i => (int?)i),
            seed);

        var namePool = UniquePool.Create(
            NameFactory.Bogus(count, seed),
            seed);

        var faker = new Faker<Bowler>()
            .CustomInstantiator(f => new()
            {
                Id = new BowlerId(Ulid.BogusString(f)),
                Name = namePool.GetNext(),
                WebsiteId = websiteIdPool.GetNextNullable(),
                LegacyId = legacyIdPool.GetNextNullable()
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}