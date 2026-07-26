using Neba.Api.Messaging;

namespace Neba.Api.Security.Password.SetPasswordFromToken;

internal sealed record SetPasswordFromTokenCommand
    : ICommand
{
    public required Ulid UserId { get; init; }

    public required string Token { get; init; }

    public required string NewPassword { get; init; }
}