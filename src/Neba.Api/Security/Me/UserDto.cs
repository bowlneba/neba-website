namespace Neba.Api.Security.Me;

internal sealed record UserDto
{
    public required Ulid UserId { get; init; }

    public required string Email { get; init; }

    public required IReadOnlyCollection<string> Roles { get; init; }

    public string? UsbcId { get; init; }
}