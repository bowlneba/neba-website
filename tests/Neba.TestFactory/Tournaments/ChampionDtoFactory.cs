using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.ListChampions;
using Neba.TestFactory.Bowlers;

namespace Neba.TestFactory.Tournaments;

public static class ChampionDtoFactory
{
    public const bool ValidHallOfFame = false;

    public static ChampionDto Create(
        BowlerId? bowlerId = null,
        Name? bowlerName = null,
        bool? hallOfFame = null)
        => new()
        {
            BowlerId = bowlerId ?? BowlerId.New(),
            BowlerName = bowlerName ?? NameFactory.Create(),
            HallOfFame = hallOfFame ?? ValidHallOfFame,
        };

    public static IReadOnlyCollection<ChampionDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<ChampionDto>()
            .CustomInstantiator(f => new ChampionDto
            {
                BowlerId = new BowlerId(Ulid.BogusString(f)),
                BowlerName = new Name { FirstName = f.Person.FirstName, LastName = f.Person.LastName },
                HallOfFame = f.Random.Bool(),
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}