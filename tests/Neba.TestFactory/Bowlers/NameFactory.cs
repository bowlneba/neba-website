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

    internal static IReadOnlyCollection<Name> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new Name
        {
            FirstName = faker.Person.FirstName,
            LastName = faker.Person.LastName,
            MiddleName = faker.Random.Bool() ? faker.Name.FirstName() : null,
            Suffix = faker.Random.Bool() ? faker.PickRandom(NameSuffix.List.ToList()) : null,
            Nickname = faker.Random.Bool() ? faker.Name.FirstName() : null
        })];
    }

    public static IReadOnlyCollection<Name> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}