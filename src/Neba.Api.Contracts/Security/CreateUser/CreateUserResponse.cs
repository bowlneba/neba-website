namespace Neba.Api.Contracts.Security.CreateUser;

/// <summary>Response returned after a successful staff user creation. Contains the new user's unique identifier.</summary>
public sealed record CreateUserResponse
{
    /// <summary>
    /// The unique identifier of the newly created user.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Whether all requested roles were successfully assigned. False means the account was
    /// created and the invite email was sent, but one or more roles didn't apply — the admin
    /// should verify and reassign roles for this user.
    /// </summary>
    public required bool RolesAssigned { get; init; }

    /// <summary>
    /// Whether the requested permission overrides were stored. False means the account was created
    /// and the invite sent, but the overrides didn't apply — the admin should set them from the edit page.
    /// </summary>
    public required bool PermissionOverridesApplied { get; init; }
}