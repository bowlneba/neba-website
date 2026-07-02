using Bogus;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Security.GetCurrentUser;

namespace Neba.TestFactory.Security;

public static class GetCurrentUserResponseFactory
{
    public const string ValidUserId = "01000000000000000000000001";
    public const string ValidEmail = "testuser@bowlneba.com";
    public const string ValidRole = "Admin";

    public static GetCurrentUserResponse Create(
        Ulid? userId = null,
        string? email = null,
        IReadOnlyCollection<string>? roles = null,
        IReadOnlyCollection<Permissions>? permissions = null,
        string? usbcId = null)
        => new()
        {
            UserId = userId?.ToString() ?? ValidUserId,
            Email = email ?? ValidEmail,
            Roles = roles ?? [ValidRole],
            Permissions = permissions?.Select(p => p.Value).ToList() ?? [Permissions.Read.Value],
            UsbcId = usbcId
        };

    internal static IReadOnlyCollection<GetCurrentUserResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new GetCurrentUserResponse
        {
            UserId = Ulid.BogusString(faker),
            Email = faker.Internet.Email(),
            Roles = [faker.Random.Word()],
            Permissions = [.. faker.PickRandom(Permissions.List.ToArray(), faker.Random.Int(1, Permissions.List.Count)).Select(p => p.Value)],
            UsbcId = faker.Random.Bool() ? $"{faker.Random.Int(10, 9999)}-{faker.Random.Int(1000, 99999)}" : null
        })];
    }

    public static IReadOnlyCollection<GetCurrentUserResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}