using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Bowlers.GetBowlerTitles;

namespace Neba.TestFactory.Bowlers;

public static class BowlerTitlesDtoFactory
{
    public const bool ValidHallOfFame = false;

    public static BowlerTitlesDto Create(
        Name? bowlerName = null,
        bool? hallOfFame = null,
        IReadOnlyCollection<BowlerTitleDto>? titles = null)
        => new()
        {
            BowlerName = bowlerName ?? NameFactory.Create(),
            HallOfFame = hallOfFame ?? ValidHallOfFame,
            Titles = titles ?? [],
        };

    public static IReadOnlyCollection<BowlerTitlesDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BowlerTitlesDto>()
            .CustomInstantiator(f => new BowlerTitlesDto
            {
                BowlerName = new Name
                {
                    FirstName = f.Person.FirstName,
                    LastName = f.Person.LastName,
                },
                HallOfFame = f.Random.Bool(),
                Titles = BowlerTitleDtoFactory.Bogus(f.Random.Int(0, 10), seed),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}