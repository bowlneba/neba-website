using Bogus;

using ErrorOr;

using Neba.Domain.Awards;
using Neba.Domain.Bowlers;

namespace Neba.TestFactory.Awards;

public static class BowlerOfTheYearAwardFactory
{
    /// <summary>
    /// Creates a valid <see cref="BowlerOfTheYearAward"/> via the appropriate per-category factory.
    /// When specifying an age-gated category (Senior, SuperSenior, Youth) you must pass a conforming
    /// <paramref name="age"/>: ≥ 50 for Senior, ≥ 60 for SuperSenior, &lt; 18 for Youth.
    /// Defaults: Open category, age 55 (valid for Senior), Gender.Female, isRookie: true.
    /// </summary>
    public static BowlerOfTheYearAward Create(
        BowlerId? bowlerId = null,
        BowlerOfTheYearCategory? category = null,
        Gender? gender = null,
        int age = 55,
        bool isRookie = true)
    {
        var resolvedBowlerId = bowlerId ?? BowlerId.New();
        var resolvedCategory = category ?? BowlerOfTheYearCategory.Open;

        ErrorOr<BowlerOfTheYearAward> result;

        if (resolvedCategory == BowlerOfTheYearCategory.Woman)
            result = BowlerOfTheYearAward.CreateWoman(resolvedBowlerId, gender ?? Gender.Female);
        else if (resolvedCategory == BowlerOfTheYearCategory.Senior)
            result = BowlerOfTheYearAward.CreateSenior(resolvedBowlerId, age);
        else if (resolvedCategory == BowlerOfTheYearCategory.SuperSenior)
            result = BowlerOfTheYearAward.CreateSuperSenior(resolvedBowlerId, age);
        else if (resolvedCategory == BowlerOfTheYearCategory.Rookie)
            result = BowlerOfTheYearAward.CreateRookie(resolvedBowlerId, isRookie);
        else if (resolvedCategory == BowlerOfTheYearCategory.Youth)
            result = BowlerOfTheYearAward.CreateYouth(resolvedBowlerId, age);
        else
            result = BowlerOfTheYearAward.CreateOpen(resolvedBowlerId);

        return result.Value;
    }

    public static IReadOnlyCollection<BowlerOfTheYearAward> Bogus(
        int count,
        UniquePool<BowlerId>? bowlerIds = null,
        int? seed = null
    )
    {
        var faker = new Faker<BowlerOfTheYearAward>()
            .CustomInstantiator(f => new()
            {
                Id = new SeasonAwardId(Ulid.Bogus(f)),
                BowlerId = bowlerIds?.GetNext() ?? new BowlerId(Ulid.Bogus(f)),
                Category = f.PickRandom(BowlerOfTheYearCategory.List.ToArray())
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
