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

    public static IReadOnlyCollection<ApplicationRole> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<ApplicationRole>()
            .CustomInstantiator(f => new ApplicationRole(f.Random.Word())
            {
                Id = new Ulid(f.Random.Guid())
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}