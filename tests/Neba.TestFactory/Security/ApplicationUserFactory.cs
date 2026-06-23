using Neba.Api.Security.Domain;

namespace Neba.TestFactory.Security;

public static class ApplicationUserFactory
{
    public const string ValidUserName = "testuser@bowlneba.com";
    public const string ValidEmail = "testuser@bowlneba.com";

    public static ApplicationUser Create(
        Ulid? id = null,
        string? userName = null,
        string? email = null,
        string? usbcId = null)
        => new()
        {
            Id = id ?? Ulid.NewUlid(),
            UserName = userName ?? ValidUserName,
            Email = email ?? ValidEmail,
            UsbcId = usbcId
        };

    public static IReadOnlyCollection<ApplicationUser> Bogus(int count, int? seed = null)
    {
        var faker = new Faker<ApplicationUser>()
            .CustomInstantiator(f =>
            {
                var email = f.Internet.Email();
                return new ApplicationUser
                {
                    Id = new Ulid(f.Random.Guid()),
                    UserName = email,
                    Email = email,
                    UsbcId = f.Random.Bool() ? $"{f.Random.Int(10, 9999)}-{f.Random.Int(1000, 99999)}" : null
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }

    public static IReadOnlyCollection<ApplicationUser> Bogus(int count, Faker parentFaker)
    {
        ArgumentNullException.ThrowIfNull(parentFaker);
        return Bogus(count, seed: parentFaker.Random.Int());
    }
}
