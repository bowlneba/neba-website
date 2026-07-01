using Microsoft.AspNetCore.Authorization;

namespace Neba.Api.Contracts.Security.Authorization;

public sealed class PermissionRequirement(string permission)
    : IAuthorizationRequirement
{
    public string Permission
        => permission;
}
