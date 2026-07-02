using Neba.Api.Messaging;

namespace Neba.Api.Security.Register;

internal sealed record RegisterCommand
    : ICommand<Ulid>
{
    public required string Email { get; init; }

    public required string Password { get; init; }
}