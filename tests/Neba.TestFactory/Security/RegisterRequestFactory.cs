using Bogus;

using Neba.Api.Contracts.Security.Register;

namespace Neba.TestFactory.Security;

public static class RegisterRequestFactory
{
    public const string ValidEmail = "test@bowlneba.com";
    public const string ValidPassword = "Password1";

    public static RegisterRequest Create(
        string? email = null,
        string? password = null)
        => new()
        {
            Input = new RegisterInput
            {
                Email = email ?? ValidEmail,
                Password = password ?? ValidPassword,
            }
        };

    internal static IReadOnlyCollection<RegisterRequest> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new RegisterRequest
        {
            Input = new RegisterInput
            {
                Email = faker.Internet.Email(),
                Password = faker.Internet.Password(8, false, "\\w", $"{faker.Random.Int(0, 9)}"),
            }
        })];
    }

    public static IReadOnlyCollection<RegisterRequest> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}