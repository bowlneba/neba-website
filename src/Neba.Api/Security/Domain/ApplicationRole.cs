using Microsoft.AspNetCore.Identity;

namespace Neba.Api.Security.Domain;

/// <summary>
/// Represents a role in the identity system
/// </summary>
public class ApplicationRole
    : IdentityRole<RoleId>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ApplicationRole"/>.
    /// </summary>
    public ApplicationRole()
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="ApplicationRole"/>.
    /// </summary>
    /// <param name="roleName"></param>
    public ApplicationRole(string roleName)
        : base(roleName)
    { }
}