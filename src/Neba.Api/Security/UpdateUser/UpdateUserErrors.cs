using ErrorOr;

namespace Neba.Api.Security.UpdateUser;

internal static class UpdateUserErrors
{
    public static Error UserNotFound
        => Error.NotFound("UpdateUser.UserNotFound", "No user was found with the given ID.");

    public static Error CannotEditOwnAccount
        => Error.Forbidden("UpdateUser.CannotEditOwnAccount", "You can't change your own permission overrides.");

    // The same payload would succeed once the target is no longer an Admin, so this is a Conflict (409).
    public static Error AdminOverridesNotAllowed
        => Error.Conflict(
            "UpdateUser.AdminOverridesNotAllowed",
            "Admins have every permission, so permission overrides don't apply to them.");

    public static Error PermissionNotHeld(IReadOnlyCollection<string> permissions)
        => Error.Forbidden(
            "UpdateUser.PermissionNotHeld",
            $"You can't grant or restore permissions you don't hold: {string.Join(", ", permissions)}.");
}