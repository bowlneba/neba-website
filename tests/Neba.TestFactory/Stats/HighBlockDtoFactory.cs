using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Stats;

public static class HighBlockDtoFactory
{
    public const int ValidHighBlock = 1150;
    public const int ValidHighGame = 289;

    public static HighBlockDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        int? highBlock = null,
        int? highGame = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create(),
            HighBlock = highBlock ?? ValidHighBlock,
            HighGame = highGame ?? ValidHighGame
        };

    internal static IReadOnlyCollection<HighBlockDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);
        return [.. Enumerable.Range(0, count).Select(_ => new HighBlockDto
        {
            BowlerId = new BowlerId(Ulid.BogusString(faker)),
            BowlerName = bowlerNamePool.GetNext(),
            HighBlock = faker.Random.Int(900, 1500),
            HighGame = faker.Random.Int(200, 300)
        })];
    }

    public static IReadOnlyCollection<HighBlockDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}