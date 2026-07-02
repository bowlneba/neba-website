using Neba.Api.Security.Domain;

namespace Neba.TestFactory.Security;

public static class ApplicationRoleFactory
{
    public const string ValidName = "Admin";

    public static ApplicationRole Create(
        Ulid? id = null,
        string? name = null)
        => new(name ?? ValidName)
        {
            Id = id ?? Ulid.NewUlid()
        };

    internal static IReadOnlyCollection<ApplicationRole> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new ApplicationRole(faker.Random.Word())
        {
            Id = new Ulid(faker.Random.Guid())
        })];
    }

    public static IReadOnlyCollection<ApplicationRole> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}