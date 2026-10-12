using Bogus;

using Neba.Api.Security.GetUser;

namespace Neba.TestFactory.Security;

public static class UserDetailDtoFactory
{
    public const string ValidEmail = "director@bowlneba.com";
    public const string ValidRole = "Tournament Director";

    internal static UserDetailDto Create(
        Ulid? userId = null,
        string? email = null,
        bool? emailConfirmed = null,
        IReadOnlyCollection<string>? roles = null,
        IReadOnlyCollection<string>? addedPermissions = null,
        IReadOnlyCollection<string>? removedPermissions = null)
        => new()
        {
            UserId = userId ?? Ulid.NewUlid(),
            Email = email ?? ValidEmail,
            EmailConfirmed = emailConfirmed ?? true,
            Roles = roles ?? [ValidRole],
            AddedPermissions = addedPermissions ?? ["Sponsors.CreateSponsor"],
            RemovedPermissions = removedPermissions ?? ["Tournaments.DeleteTournament"]
        };

    internal static IReadOnlyCollection<UserDetailDto> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new UserDetailDto
        {
            UserId = Ulid.Bogus(faker),
            Email = faker.Internet.Email(),
            EmailConfirmed = faker.Random.Bool(),
            Roles = [faker.Random.Word()],
            AddedPermissions = [],
            RemovedPermissions = []
        })];
    }

    internal static IReadOnlyCollection<UserDetailDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}