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

    public static IReadOnlyCollection<HighBlockDto> Bogus(int count, int? seed = null)
    {
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, seed), seed);

        var faker = new Faker<HighBlockDto>()
            .CustomInstantiator(f => new HighBlockDto
            {
                BowlerId = new BowlerId(Ulid.BogusString(f)),
                BowlerName = bowlerNamePool.GetNext(),
                HighBlock = f.Random.Int(900, 1500),
                HighGame = f.Random.Int(200, 300)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}