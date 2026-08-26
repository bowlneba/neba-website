using Bogus;

using Neba.Api.Contracts.Security.ListUsers;

namespace Neba.TestFactory.Security;

public static class UserSummaryResponseFactory
{
    public const string ValidUserId = "01000000000000000000000001";
    public const string ValidEmail = "webmaster@bowlneba.com";
    public const string ValidRole = "Webmaster";

    public static UserSummaryResponse Create(
        string? userId = null,
        string? email = null,
        bool? emailConfirmed = null,
        IReadOnlyCollection<string>? roles = null)
        => new()
        {
            UserId = userId ?? ValidUserId,
            Email = email ?? ValidEmail,
            EmailConfirmed = emailConfirmed ?? true,
            Roles = roles ?? [ValidRole]
        };

    internal static IReadOnlyCollection<UserSummaryResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new UserSummaryResponse
        {
            UserId = Ulid.BogusString(faker),
            Email = faker.Internet.Email(),
            EmailConfirmed = faker.Random.Bool(),
            Roles = [faker.Random.Word()]
        })];
    }

    public static IReadOnlyCollection<UserSummaryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
