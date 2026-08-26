using Bogus;

using Neba.Api.Security.ListUsers;

namespace Neba.TestFactory.Security;

public static class UserSummaryDtoFactory
{
    public const string ValidEmail = "webmaster@bowlneba.com";
    public const string ValidRole = "Webmaster";

    internal static UserSummaryDto Create(
        Ulid? userId = null,
        string? email = null,
        bool? emailConfirmed = null,
        IReadOnlyCollection<string>? roles = null)
        => new()
        {
            UserId = userId ?? Ulid.NewUlid(),
            Email = email ?? ValidEmail,
            EmailConfirmed = emailConfirmed ?? true,
            Roles = roles ?? [ValidRole]
        };

    internal static IReadOnlyCollection<UserSummaryDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new UserSummaryDto
        {
            UserId = Ulid.Bogus(faker),
            Email = faker.Internet.Email(),
            EmailConfirmed = faker.Random.Bool(),
            Roles = [faker.Random.Word()]
        })];
    }

    internal static IReadOnlyCollection<UserSummaryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
