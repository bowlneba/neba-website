using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Stats.GetSeasonStats;
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

    internal static IReadOnlyCollection<AverageFinishDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var bowlerNamePool = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);
        return [.. Enumerable.Range(0, count).Select(_ => new AverageFinishDto
        {
            BowlerId = new BowlerId(Ulid.BogusString(faker)),
            BowlerName = bowlerNamePool.GetNext(),
            AverageFinish = faker.Random.Decimal(1, 15),
            Finals = faker.Random.Int(1, 15),
            Winnings = faker.Random.Decimal(0, 5000)
        })];
    }

    public static IReadOnlyCollection<AverageFinishDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}