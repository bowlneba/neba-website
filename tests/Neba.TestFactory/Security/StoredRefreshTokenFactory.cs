using Bogus;

using Neba.Api.Security.Domain;

namespace Neba.TestFactory.Security;

public static class StoredRefreshTokenFactory
{
    public const string ValidHash = "hashed-refresh-token-value";
    public static readonly DateTimeOffset ValidIssuedAt = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static StoredRefreshToken Create(
        string? hash = null,
        DateTimeOffset? issuedAt = null)
        => new()
        {
            Hash = hash ?? ValidHash,
            IssuedAt = issuedAt ?? ValidIssuedAt,
        };

    internal static IReadOnlyCollection<StoredRefreshToken> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new StoredRefreshToken
        {
            Hash = faker.Random.Hash(),
            IssuedAt = faker.Date.PastOffset(2),
        })];
    }

    public static IReadOnlyCollection<StoredRefreshToken> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
