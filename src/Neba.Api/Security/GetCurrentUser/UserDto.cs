using Neba.Api.Contracts.Security;

namespace Neba.Api.Security.GetCurrentUser;

internal sealed record UserDto
{
    public required Ulid UserId { get; init; }

    public required string Email { get; init; }

    public required IReadOnlyCollection<string> Roles { get; init; }

    public required IReadOnlyCollection<Permissions> Permissions { get; init; }

    public string? UsbcId { get; init; }
}