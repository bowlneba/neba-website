using Microsoft.AspNetCore.Authorization;

namespace Neba.Api.Security.Infrastructure.Authorization;

internal sealed class PermissionRequirement(string permission)
    : IAuthorizationRequirement
{
    public string Permission
        => permission;
}