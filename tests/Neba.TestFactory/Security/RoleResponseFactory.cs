using Bogus;

using Neba.Api.Contracts.Security.ListRoles;

namespace Neba.TestFactory.Security;

public static class RoleResponseFactory
{
    public const string ValidName = "Tournament Director";

    public static RoleResponse Create(string? name = null, IReadOnlyCollection<string>? permissions = null)
        => new()
        {
            Name = name ?? ValidName,
            Permissions = permissions ?? ["Tournaments.CreateTournament", "Tournaments.EditTournament"]
        };

    internal static IReadOnlyCollection<RoleResponse> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        return [.. Enumerable.Range(0, count).Select(_ => new RoleResponse
        {
            Name = faker.Random.Word(),
            Permissions = [faker.Random.Word()]
        })];
    }

    public static IReadOnlyCollection<RoleResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}