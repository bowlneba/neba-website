using ErrorOr;

using Neba.Api.Messaging;

namespace Neba.Api.Security.UpdateUser;

internal sealed record UpdateUserCommand
    : ICommand<Updated>
{
    public required Ulid UserId { get; init; }

    public required IReadOnlyCollection<string> AddedPermissions { get; init; }

    public required IReadOnlyCollection<string> RemovedPermissions { get; init; }

    /// <summary>The caller's user ID. The fact the self-edit rule needs.</summary>
    public required Ulid CallerUserId { get; init; }

    /// <summary>The caller's permissions. The cross-aggregate fact the escalation guard needs.</summary>
    public required IReadOnlyCollection<string> CallerPermissions { get; init; }
}