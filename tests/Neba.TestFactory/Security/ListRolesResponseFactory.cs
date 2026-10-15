using Neba.Api.Contracts.Security.ListRoles;

namespace Neba.TestFactory.Security;

public static class ListRolesResponseFactory
{
    public static ListRolesResponse Create(IReadOnlyCollection<RoleResponse>? roles = null)
        => new() { Roles = roles ?? [RoleResponseFactory.Create()] };
}