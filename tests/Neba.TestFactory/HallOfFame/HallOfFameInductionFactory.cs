using Bogus;

using Neba.Domain.Bowlers;
using Neba.Domain.HallOfFame;

namespace Neba.TestFactory.HallOfFame;

public static class HallOfFameInductionFactory
{
    public const int ValidYear = 2025;

    public static HallOfFameInduction Create(
        HallOfFameId? id = null,
        int? year = null,
        BowlerId? bowlerId = null,
        IReadOnlyCollection<HallOfFameCategory>? categories = null)
        => new()
        {
            Id = id ?? HallOfFameId.New(),
            Year = year ?? ValidYear,
            BowlerId = bowlerId ?? BowlerId.New(),
            Categories = categories ?? [HallOfFameCategory.SuperiorPerformance]
        };

    public static HallOfFameInduction Bogus(int? seed = null)
        => Bogus(1, seed).Single();

    public static IReadOnlyCollection<HallOfFameInduction> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<HallOfFameInduction>()
            .CustomInstantiator(f => new()
            {
                Id = HallOfFameId.New(),
                Year = f.Date.PastDateOnly(20).Year,
                BowlerId = BowlerId.New(),
                Categories = [.. f.PickRandom(HallOfFameCategory.List, f.Random.Int(1, HallOfFameCategory.List.Count))]
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}