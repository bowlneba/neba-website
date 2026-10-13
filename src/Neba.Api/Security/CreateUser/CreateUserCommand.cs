using Neba.Api.Messaging;

namespace Neba.Api.Security.CreateUser;

internal sealed record CreateUserCommand
    : ICommand<CreateUserResult>
{
    public required string Email { get; init; }

    public required IReadOnlyCollection<string> Roles { get; init; }

    public string? UsbcId { get; init; }

    public string? PhoneNumber { get; init; }

    public IReadOnlyCollection<string> AddedPermissions { get; init; } = [];

    public IReadOnlyCollection<string> RemovedPermissions { get; init; } = [];

    /// <summary>The caller's permissions. The cross-aggregate fact the escalation guard needs.</summary>
    public required IReadOnlyCollection<string> CallerPermissions { get; init; }
}