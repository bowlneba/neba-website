using Neba.Api.Features.Series.Domain;

namespace Neba.TestFactory.Series;

public static class BowlerSortNameFactory
{
    public const string ValidLastName = "Smith";
    public const string ValidFirstName = "Alex";

    public static BowlerSortName Create(string? lastName = null, string? firstName = null)
        => new(lastName ?? ValidLastName, firstName ?? ValidFirstName);

    public static IReadOnlyList<BowlerSortName> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue)
        {
            faker.Random = new Randomizer(seed.Value);
        }

        return [.. Enumerable.Range(0, count).Select(_ => Create(faker.Name.LastName(), faker.Name.FirstName()))];
    }
}