using Neba.Api.Messaging;

namespace Neba.Api.Security.Login;

internal sealed record LoginCommand
    : ICommand<LoginDto>
{
    public required string Email { get; init; }

    public required string Password { get; init; }
}