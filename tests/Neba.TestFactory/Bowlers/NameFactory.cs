using Neba.Api.Features.Bowlers.Domain;

namespace Neba.TestFactory.Bowlers;

public static class NameFactory
{
    public const string ValidFirstName = "John";
    public const string ValidLastName = "Doe";
    public const string ValidMiddleName = "M.";
    public static readonly NameSuffix ValidSuffix = NameSuffix.Jr;
    public const string ValidNickname = "Johnny";

    public static Name Create(
        string? firstName = null,
        string? lastName = null,
        string? middleName = null,
        NameSuffix? suffix = null,
        string? nickname = null)
         => new()
         {
             FirstName = firstName ?? ValidFirstName,
             LastName = lastName ?? ValidLastName,
             MiddleName = middleName,
             Suffix = suffix,
             Nickname = nickname
         };

    public static IReadOnlyCollection<Name> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<Name>()
            .CustomInstantiator(f =>
            new Name
            {
                FirstName = f.Person.FirstName,
                LastName = f.Person.LastName,
                MiddleName = f.Random.Bool() ? f.Name.FirstName() : null,
                Suffix = f.Random.Bool() ? f.PickRandom(NameSuffix.List.ToList()) : null,
                Nickname = f.Random.Bool() ? f.Name.FirstName() : null
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}