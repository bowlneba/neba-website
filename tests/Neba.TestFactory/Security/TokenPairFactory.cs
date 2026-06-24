using Bogus;

using Neba.Api.Security.Domain;

namespace Neba.TestFactory.Security;

internal static class TokenPairFactory
{
    public const string ValidAccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test-access-token";
    public const string ValidRefreshToken = "test-refresh-token-value";
    public static readonly DateTimeOffset ValidExpiresAt = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static TokenPair Create(
        string? accessToken = null,
        string? refreshToken = null,
        DateTimeOffset? expiresAt = null)
        => new()
        {
            AccessToken = accessToken ?? ValidAccessToken,
            RefreshToken = refreshToken ?? ValidRefreshToken,
            ExpiresAt = expiresAt ?? ValidExpiresAt,
        };

    internal static IReadOnlyCollection<TokenPair> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new TokenPair
        {
            AccessToken = faker.Random.AlphaNumeric(256),
            RefreshToken = faker.Random.AlphaNumeric(64),
            ExpiresAt = faker.Date.FutureOffset(),
        })];
    }

    public static IReadOnlyCollection<TokenPair> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}