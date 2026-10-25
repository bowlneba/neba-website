using Bogus;

using Neba.Api.Contracts.Security.ForgotPassword;

namespace Neba.TestFactory.Security;

public static class ForgotPasswordRequestFactory
{
    public const string ValidEmail = "test@bowlneba.com";

    public static ForgotPasswordRequest Create(string? email = null)
        => new()
        {
            Email = email ?? ValidEmail
        };

    internal static IReadOnlyCollection<ForgotPasswordRequest> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);

        return [.. Enumerable.Range(0, count).Select(_ => new ForgotPasswordRequest
        {
            Email = faker.Internet.Email()
        })];
    }

    public static IReadOnlyCollection<ForgotPasswordRequest> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
