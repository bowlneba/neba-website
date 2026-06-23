using Neba.Api.Features.Bowlers.Domain;

namespace Neba.TestFactory.Bowlers;

public static class BowlerFactory
{
    public static Bowler Create(
        BowlerId? id = null,
        Name? name = null,
        int? websiteId = null,
        int? legacyId = null,
        Gender? gender = null,
        DateOnly? dateOfBirth = null)
        => new()
        {
            Id = id ?? BowlerId.New(),
            Name = name ?? NameFactory.Create(),
            WebsiteId = websiteId,
            LegacyId = legacyId,
            Gender = gender ?? Gender.Male,
            DateOfBirth = dateOfBirth ?? new DateOnly(2000, 5, 1)
        };

    internal static IReadOnlyCollection<Bowler> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var websiteIdPool = UniquePool.CreateNullable(
            Enumerable.Range(1, count).Select(i => (int?)i),
            poolSeed);
        var legacyIdPool = UniquePool.CreateNullable(
            Enumerable.Range(100_001, count).Select(i => (int?)i),
            poolSeed);
        var namePool = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ => new Bowler
        {
            Id = new BowlerId(Ulid.BogusString(faker)),
            Name = namePool.GetNext(),
            WebsiteId = websiteIdPool.GetNextNullable(),
            LegacyId = legacyIdPool.GetNextNullable(),
            Gender = faker.PickRandom(Gender.List.ToArray()),
            DateOfBirth = faker.Date.PastDateOnly(faker.Random.Int(20, 80))
        })];
    }

    public static IReadOnlyCollection<Bowler> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}