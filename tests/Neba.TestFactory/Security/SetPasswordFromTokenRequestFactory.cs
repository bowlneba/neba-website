using Bogus;

using Neba.Api.Contracts.Security.SetPasswordFromToken;

namespace Neba.TestFactory.Security;

public static class SetPasswordFromTokenRequestFactory
{
    public const string ValidUserId = "01000000000000000000000001";
    public const string ValidToken = "valid-token";
    public const string ValidNewPassword = "NewPassword1";

    public static SetPasswordFromTokenRequest Create(
        string? userId = null,
        string? token = null,
        string? newPassword = null)
        => new()
        {
            UserId = userId ?? ValidUserId,
            Token = token ?? ValidToken,
            NewPassword = newPassword ?? ValidNewPassword
        };

    internal static IReadOnlyCollection<SetPasswordFromTokenRequest> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => new SetPasswordFromTokenRequest
        {
            UserId = Ulid.BogusString(faker),
            Token = faker.Random.AlphaNumeric(32),
            NewPassword = faker.Internet.Password(12) + "1"
        })];
    }

    public static IReadOnlyCollection<SetPasswordFromTokenRequest> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
