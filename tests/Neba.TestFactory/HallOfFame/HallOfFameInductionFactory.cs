using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.HallOfFame.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.TestFactory.Storage;

namespace Neba.TestFactory.HallOfFame;

public static class HallOfFameInductionFactory
{
    public const int ValidYear = 2025;

    public static HallOfFameInduction Create(
        HallOfFameId? id = null,
        int? year = null,
        BowlerId? bowlerId = null,
        IReadOnlyCollection<HallOfFameCategory>? categories = null,
        StoredFile? photo = null)
        => new()
        {
            Id = id ?? HallOfFameId.New(),
            Year = year ?? ValidYear,
            BowlerId = bowlerId ?? BowlerId.New(),
            Categories = categories ?? [HallOfFameCategory.SuperiorPerformance],
            Photo = photo
        };

    public static IReadOnlyCollection<HallOfFameInduction> Bogus(
        int count,
        Faker faker,
        UniquePool<BowlerId>? bowlerIds = null)
    {
        ArgumentNullException.ThrowIfNull(faker);
        var poolSeed = faker.Random.Int();
        var uniquePhotos = UniquePool.CreateNullable(StoredFileFactory.Bogus(count, faker), poolSeed);

        return [.. Enumerable.Range(0, count).Select(_ => new HallOfFameInduction
        {
            Id = new HallOfFameId(Ulid.BogusString(faker)),
            Year = faker.Date.PastDateOnly(20).Year,
            BowlerId = bowlerIds?.GetNext() ?? new BowlerId(Ulid.BogusString(faker)),
            Categories = [.. faker.PickRandom(HallOfFameCategory.List, faker.Random.Int(1, HallOfFameCategory.List.Count))],
            Photo = uniquePhotos.GetNextNullable()
        })];
    }

    public static IReadOnlyCollection<HallOfFameInduction> Bogus(
        int count,
        UniquePool<BowlerId>? bowlerIds = null,
        int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker, bowlerIds);
    }
}