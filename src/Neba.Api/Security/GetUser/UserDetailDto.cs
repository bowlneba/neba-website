namespace Neba.Api.Security.GetUser;

internal sealed record UserDetailDto
{
    public required Ulid UserId { get; init; }

    public required string Email { get; init; }

    public required bool EmailConfirmed { get; init; }

    public required IReadOnlyCollection<string> Roles { get; init; }

    public required IReadOnlyCollection<string> AddedPermissions { get; init; }

    public required IReadOnlyCollection<string> RemovedPermissions { get; init; }
}
