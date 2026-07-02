using Bogus;

using Neba.Api.Contracts.Security;
using Neba.Api.Security.GetCurrentUser;

namespace Neba.TestFactory.Security;

public static class UserDtoFactory
{
    public const string ValidEmail = "test@bowlneba.com";
    public const string ValidRole = "Member";
    public const string ValidUsbcId = "123-45678";

    internal static UserDto Create(
        Ulid? userId = null,
        string? email = null,
        IReadOnlyCollection<string>? roles = null,
        IReadOnlyCollection<Permissions>? permissions = null,
        string? usbcId = null)
        => new()
        {
            UserId = userId ?? Ulid.NewUlid(),
            Email = email ?? ValidEmail,
            Roles = roles ?? [ValidRole],
            Permissions = permissions ?? [Permissions.Read],
            UsbcId = usbcId
        };

    internal static IReadOnlyCollection<UserDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new UserDto
        {
            UserId = Ulid.Bogus(faker),
            Email = faker.Internet.Email(),
            Roles = [faker.Random.Word()],
            Permissions = [.. faker.PickRandom(Permissions.List.ToArray(), faker.Random.Int(1, Permissions.List.Count))],
            UsbcId = faker.Random.Bool() ? $"{faker.Random.Int(10, 9999)}-{faker.Random.Int(1000, 99999)}" : null
        })];
    }

    internal static IReadOnlyCollection<UserDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}