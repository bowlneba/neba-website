using Bogus;

using Neba.Api.Contracts.Security.GetCurrentUser;

namespace Neba.TestFactory.Security;

public static class MeResponseFactory
{
    public const string ValidUserId = "01000000000000000000000001";
    public const string ValidEmail = "testuser@bowlneba.com";
    public const string ValidRole = "Admin";

    public static MeResponse Create(
        Ulid? userId = null,
        string? email = null,
        IReadOnlyCollection<string>? roles = null,
        string? usbcId = null)
        => new()
        {
            UserId = userId?.ToString() ?? ValidUserId,
            Email = email ?? ValidEmail,
            Roles = roles ?? [ValidRole],
            UsbcId = usbcId
        };

    internal static IReadOnlyCollection<MeResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new MeResponse
        {
            UserId = Ulid.BogusString(faker),
            Email = faker.Internet.Email(),
            Roles = [faker.Random.Word()],
            UsbcId = faker.Random.Bool() ? $"{faker.Random.Int(10, 9999)}-{faker.Random.Int(1000, 99999)}" : null
        })];
    }

    public static IReadOnlyCollection<MeResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}