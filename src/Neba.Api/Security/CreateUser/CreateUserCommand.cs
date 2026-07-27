using Neba.Api.Messaging;

namespace Neba.Api.Security.CreateUser;

internal sealed record CreateUserCommand
    : ICommand<Ulid>
{
    public required string Email { get; init; }

    public required IReadOnlyCollection<string> Roles { get; init; }

    public string? UsbcId { get; init; }

    public string? PhoneNumber { get; init; }

    public IReadOnlyCollection<(string Type, string Value)> Claims { get; init; } = [];
}