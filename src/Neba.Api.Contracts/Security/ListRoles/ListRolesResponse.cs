namespace Neba.Api.Contracts.Security.ListRoles;

/// <summary>Every role with the permissions it grants.</summary>
public sealed record ListRolesResponse
{
    /// <summary>The roles, ordered by name.</summary>
    public required IReadOnlyCollection<RoleResponse> Roles { get; init; }
}
