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

    public static Bowler Bogus(int? seed = null)
        => Bogus(1, seed).Single();

    public static IReadOnlyCollection<Bowler> Bogus(int count, int? seed = null)
    {
        var websiteIdPool = UniqueIdPool.Create(
            Enumerable.Range(1, count).Select(i => (int?)i),
            seed,
            probabilityOfValue: 0.5f);

        var legacyIdPool = UniqueIdPool.Create(
            Enumerable.Range(100_001, count).Select(i => (int?)i),
            seed,
            probabilityOfValue: 0.5f);

        var namePool = UniqueIdPool.Create(
            NameFactory.Bogus(count, seed),
            seed);

        var faker = new Faker<Bowler>()
            .CustomInstantiator(f => new()
            {
                Id = BowlerId.New(),
                Name = namePool.GetNext()!,
                WebsiteId = websiteIdPool.GetNext(),
                LegacyId = legacyIdPool.GetNext()
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}