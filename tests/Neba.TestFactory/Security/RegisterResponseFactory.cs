using Bogus;

using Neba.Api.Contracts.Security.Register;

namespace Neba.TestFactory.Security;

public static class RegisterResponseFactory
{
    public const string ValidUserId = "01000000000000000000000001";

    public static RegisterResponse Create(string? userId = null)
        => new()
        {
            UserId = userId ?? ValidUserId,
        };

    internal static IReadOnlyCollection<RegisterResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new RegisterResponse
        {
            UserId = Ulid.BogusString(faker),
        })];
    }

    public static IReadOnlyCollection<RegisterResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}