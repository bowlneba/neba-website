using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;

namespace Neba.TestFactory.Seasons;

public static class HighBlockAwardFactory
{
    public const int ValidBlockScore = 1300;

    public static HighBlockAward Create(
        SeasonAwardId? id = null,
        BowlerId? bowlerId = null,
        int? blockScore = null)
    {
        return new HighBlockAward
        {
            Id = id ?? SeasonAwardId.New(),
            BowlerId = bowlerId ?? BowlerId.New(),
            BlockScore = blockScore ?? ValidBlockScore
        };
    }

    public static IReadOnlyCollection<HighBlockAward> Bogus(
        int count,
        Faker faker,
        UniquePool<BowlerId>? bowlerIds = null)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new HighBlockAward
        {
            Id = new SeasonAwardId(Ulid.BogusString(faker)),
            BowlerId = bowlerIds?.GetNext() ?? new BowlerId(Ulid.BogusString(faker)),
            BlockScore = faker.Random.Int(1250, 1400)
        })];
    }

    public static IReadOnlyCollection<HighBlockAward> Bogus(
        int count,
        UniquePool<BowlerId>? bowlerIds = null,
        int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker, bowlerIds);
    }
}