using Bogus;

using Neba.Api.Contracts.Security.Login;

namespace Neba.TestFactory.Security;

public static class LoginRequestFactory
{
    public const string ValidEmail = "test@bowlneba.com";
    public const string ValidPassword = "Password1";

    public static LoginRequest Create(
        string? email = null,
        string? password = null)
        => new()
        {
            Email = email ?? ValidEmail,
            Password = password ?? ValidPassword,
        };

    internal static IReadOnlyCollection<LoginRequest> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new LoginRequest
        {
            Email = faker.Internet.Email(),
            Password = faker.Internet.Password(8, false, "\\w", $"{faker.Random.Int(0, 9)}"),
        })];
    }

    public static IReadOnlyCollection<LoginRequest> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
