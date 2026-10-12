using Bogus;

using Neba.Api.Contracts.Security.UpdateUser;

namespace Neba.TestFactory.Security;

public static class UpdateUserRequestFactory
{
    public const string ValidUserId = "01000000000000000000000002";

    public static UpdateUserRequest Create(
        string? userId = null,
        IReadOnlyCollection<string>? addedPermissions = null,
        IReadOnlyCollection<string>? removedPermissions = null)
        => new()
        {
            UserId = userId ?? ValidUserId,
            User = new UpdateUserInput
            {
                AddedPermissions = addedPermissions ?? ["News.CreateArticle"],
                RemovedPermissions = removedPermissions ?? ["Tournaments.DeleteTournament"]
            }
        };

    internal static IReadOnlyCollection<UpdateUserRequest> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new UpdateUserRequest
        {
            UserId = Ulid.BogusString(faker),
            User = new UpdateUserInput
            {
                AddedPermissions = [],
                RemovedPermissions = []
            }
        })];
    }

    public static IReadOnlyCollection<UpdateUserRequest> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
