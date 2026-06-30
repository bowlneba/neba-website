using Neba.Api.Messaging;

namespace Neba.Api.Security.RefreshToken;

internal sealed record RefreshTokenCommand
    : ICommand<RefreshTokenDto>
{
    public required Ulid UserId { get; init; }

    public required string RefreshToken { get; init; }
}