using Neba.Api.Messaging;

namespace Neba.Api.Security.Logout;

internal sealed record LogoutCommand
    : ICommand
{
    public required Ulid UserId { get; init; }
}