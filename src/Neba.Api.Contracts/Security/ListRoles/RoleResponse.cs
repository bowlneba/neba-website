namespace Neba.Api.Contracts.Security.ListRoles;

/// <summary>A role and the permission values it grants.</summary>
public sealed record RoleResponse
{
    /// <summary>The role name (e.g. "Tournament Director").</summary>
    public required string Name { get; init; }

    /// <summary>The permission values (e.g. "Tournaments.CreateTournament") the role grants.</summary>
    public required IReadOnlyCollection<string> Permissions { get; init; }
}