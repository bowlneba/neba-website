using Neba.Api.Messaging;
using Neba.Api.Security.Login;

namespace Neba.Api.Security.RefreshToken;

internal sealed record RefreshTokenCommand
    : ICommand<LoginDto>
{
    public required Ulid UserId { get; init; }

    public required string RefreshToken { get; init; }
}