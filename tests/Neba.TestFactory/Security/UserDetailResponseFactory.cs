using Bogus;

using Neba.Api.Contracts.Security.GetUser;

namespace Neba.TestFactory.Security;

public static class UserDetailResponseFactory
{
    public static UserDetailResponse Create(
        string? userId = null,
        string? email = null,
        bool? emailConfirmed = null,
        IReadOnlyCollection<string>? roles = null,
        IReadOnlyCollection<string>? addedPermissions = null,
        IReadOnlyCollection<string>? removedPermissions = null)
        => new()
        {
            UserId = userId ?? UserSummaryResponseFactory.ValidUserId,
            Email = email ?? UserSummaryResponseFactory.ValidEmail,
            EmailConfirmed = emailConfirmed ?? true,
            Roles = roles ?? [UserSummaryResponseFactory.ValidRole],
            AddedPermissions = addedPermissions ?? [],
            RemovedPermissions = removedPermissions ?? []
        };

    internal static IReadOnlyCollection<UserDetailResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new UserDetailResponse
        {
            UserId = Ulid.BogusString(faker),
            Email = faker.Internet.Email(),
            EmailConfirmed = faker.Random.Bool(),
            Roles = [faker.Random.Word()],
            AddedPermissions = [],
            RemovedPermissions = []
        })];
    }

    public static IReadOnlyCollection<UserDetailResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
