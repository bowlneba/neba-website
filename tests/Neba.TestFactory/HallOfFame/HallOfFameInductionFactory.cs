using Bogus;

using Neba.Domain.Bowlers;
using Neba.Domain.HallOfFame;
using Neba.Domain.Storage;
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
        UniquePool<BowlerId>? bowlerIds = null,
        int? seed = null)
    {
        var uniquePhotos = UniquePool.CreateNullable(StoredFileFactory.Bogus(count, seed), seed);

        var faker = new Faker<HallOfFameInduction>()
            .CustomInstantiator(f => new()
            {
                Id = new HallOfFameId(Ulid.BogusString(f)),
                Year = f.Date.PastDateOnly(20).Year,
                BowlerId = bowlerIds?.GetNext() ?? new BowlerId(Ulid.BogusString(f)),
                Categories = [.. f.PickRandom(HallOfFameCategory.List, f.Random.Int(1, HallOfFameCategory.List.Count))],
                Photo = uniquePhotos.GetNextNullable()
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}