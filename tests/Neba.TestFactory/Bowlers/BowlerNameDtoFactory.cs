using Bogus;

using Neba.Application.Bowlers;
using Neba.Domain.Bowlers;

namespace Neba.TestFactory.Bowlers;

public static class BowlerNameDtoFactory
{
    public static BowlerNameDto Create(
        string? firstName = null,
        string? lastName = null,
        string? middleName = null,
        string? suffix = null,
        string? nickname = null)
        => new()
        {
            FirstName = firstName ?? NameFactory.ValidFirstName,
            LastName = lastName ?? NameFactory.ValidLastName,
            MiddleName = middleName,
            Suffix = suffix,
            Nickname = nickname
        };

    public static IReadOnlyCollection<BowlerNameDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<BowlerNameDto>()
            .CustomInstantiator(f => new BowlerNameDto
            {
                FirstName = f.Person.FirstName,
                LastName = f.Person.LastName,
                MiddleName = f.Random.Bool() ? f.Name.FirstName() : null,
                Suffix = f.Random.Bool() ? f.PickRandom(NameSuffix.List.ToList()).Value : null,
                Nickname = f.Random.Bool() ? f.Name.FirstName() : null
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}