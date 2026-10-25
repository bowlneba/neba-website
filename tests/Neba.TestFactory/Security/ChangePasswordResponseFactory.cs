using Bogus;

using Neba.Api.Contracts.Security.ChangePassword;

namespace Neba.TestFactory.Security;

public static class ChangePasswordResponseFactory
{
    public const string ValidAccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test-access-token";
    public const string ValidRefreshToken = "test-refresh-token-value";
    public static readonly DateTimeOffset ValidExpiresAt = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public const string ValidUserId = "01000000000000000000000001";
    public const string ValidEmail = "test@bowlneba.com";

    public static ChangePasswordResponse Create(
        string? accessToken = null,
        string? refreshToken = null,
        DateTimeOffset? expiresAt = null,
        string? userId = null,
        string? email = null)
        => new()
        {
            AccessToken = accessToken ?? ValidAccessToken,
            RefreshToken = refreshToken ?? ValidRefreshToken,
            ExpiresAt = expiresAt ?? ValidExpiresAt,
            UserId = userId ?? ValidUserId,
            Email = email ?? ValidEmail,
        };

    internal static IReadOnlyCollection<ChangePasswordResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => new ChangePasswordResponse
        {
            AccessToken = faker.Random.AlphaNumeric(256),
            RefreshToken = faker.Random.AlphaNumeric(64),
            ExpiresAt = faker.Date.FutureOffset(),
            UserId = Ulid.BogusString(faker),
            Email = faker.Internet.Email(),
        })];
    }

    public static IReadOnlyCollection<ChangePasswordResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
