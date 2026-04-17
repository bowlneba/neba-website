using Bogus;

using Neba.Application.Stats.GetSeasonStats;
using Neba.Domain.Bowlers;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Stats;

public static class AverageFinishDtoFactory
{
    public const decimal ValidAverageFinish = 4.5m;
    public const int ValidFinals = 3;
    public const decimal ValidWinnings = 2500m;

    public static AverageFinishDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        decimal? averageFinish = null,
        int? finals = null,
        decimal? winnings = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create(),
            AverageFinish = averageFinish ?? ValidAverageFinish,
            Finals = finals ?? ValidFinals,
            Winnings = winnings ?? ValidWinnings
        };

    public static IReadOnlyCollection<AverageFinishDto> Bogus(int count, int? seed = null)
    {
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, seed), seed);

        var faker = new Faker<AverageFinishDto>()
            .CustomInstantiator(f => new AverageFinishDto
            {
                BowlerId = new BowlerId(Ulid.BogusString(f)),
                BowlerName = bowlerNamePool.GetNext(),
                AverageFinish = f.Random.Decimal(1, 15),
                Finals = f.Random.Int(1, 15),
                Winnings = f.Random.Decimal(0, 5000)
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
