using Bogus;

using Neba.Api.Contracts.Security.RefreshToken;

namespace Neba.TestFactory.Security;

public static class RefreshTokenRequestFactory
{
    public const string ValidUserId = "01000000000000000000000001";
    public const string ValidRefreshToken = "test-refresh-token-value";

    public static RefreshTokenRequest Create(
        string? userId = null,
        string? refreshToken = null)
        => new()
        {
            UserId = userId ?? ValidUserId,
            RefreshToken = refreshToken ?? ValidRefreshToken,
        };

    internal static IReadOnlyCollection<RefreshTokenRequest> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new RefreshTokenRequest
        {
            UserId = Ulid.BogusString(faker),
            RefreshToken = faker.Random.Hash(),
        })];
    }

    public static IReadOnlyCollection<RefreshTokenRequest> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
