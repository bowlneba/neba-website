namespace Neba.Api.Security.ListRoles;

internal sealed record RoleDto
{
    public required string Name { get; init; }

    public required IReadOnlyCollection<string> Permissions { get; init; }
}
