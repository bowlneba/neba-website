namespace Neba.Api.Security.ListUsers;

internal sealed record UserSummaryDto
{
    public required Ulid UserId { get; init; }

    public required string Email { get; init; }

    public required bool EmailConfirmed { get; init; }

    public required IReadOnlyCollection<string> Roles { get; init; }
}