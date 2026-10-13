using Neba.Api.Security.ListRoles;

namespace Neba.TestFactory.Security;

public static class RoleDtoFactory
{
    internal static RoleDto Create(string? name = null, IReadOnlyCollection<string>? permissions = null)
        => new()
        {
            Name = name ?? RoleResponseFactory.ValidName,
            Permissions = permissions ?? ["Tournaments.CreateTournament", "Tournaments.EditTournament"]
        };
}