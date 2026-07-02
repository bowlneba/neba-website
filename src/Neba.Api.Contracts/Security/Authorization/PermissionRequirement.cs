using Microsoft.AspNetCore.Authorization;

namespace Neba.Api.Contracts.Security.Authorization;

/// <summary>
/// An authorization requirement satisfied when the current user has a claim of type
/// <see cref="Permissions.ClaimType"/> whose value matches <see cref="Permission"/>.
/// </summary>
public sealed class PermissionRequirement(string permission)
    : IAuthorizationRequirement
{
    /// <summary>
    /// The permission value required to satisfy this requirement.
    /// </summary>
    public string Permission
        => permission;
}