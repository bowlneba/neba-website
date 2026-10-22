namespace Neba.Api.Contracts.Security.UpdateUser;

/// <summary>The editable fields of a user. Today only permission overrides; replace-set semantics.</summary>
public sealed record UpdateUserInput
{
    /// <summary>Permissions granted regardless of role. Replaces the stored set; empty clears it.</summary>
    public required IReadOnlyCollection<string> AddedPermissions { get; init; }

    /// <summary>Permissions withheld even when a role grants them. Replaces the stored set; empty clears it.</summary>
    public required IReadOnlyCollection<string> RemovedPermissions { get; init; }
}