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

    public static IReadOnlyCollection<BowlerTitlesDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new BowlerTitlesDto
        {
            BowlerName = new Name
            {
                FirstName = faker.Person.FirstName,
                LastName = faker.Person.LastName,
            },
            HallOfFame = faker.Random.Bool(),
            Titles = BowlerTitleDtoFactory.Bogus(faker.Random.Int(0, 10), faker),
        })];
    }

    public static IReadOnlyCollection<BowlerTitlesDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}