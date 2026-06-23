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

    public static IReadOnlyCollection<ApplicationUser> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var email = faker.Internet.Email();
            return new ApplicationUser
            {
                Id = new Ulid(faker.Random.Guid()),
                UserName = email,
                Email = email,
                UsbcId = faker.Random.Bool() ? $"{faker.Random.Int(10, 9999)}-{faker.Random.Int(1000, 99999)}" : null
            };
        })];
    }

    public static IReadOnlyCollection<ApplicationUser> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}