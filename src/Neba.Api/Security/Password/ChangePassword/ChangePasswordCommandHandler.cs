using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Database;
using Neba.Api.Messaging;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Infrastructure.Authorization;

namespace Neba.Api.Security.Password.ChangePassword;

internal sealed class ChangePasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    SecurityDbContext securityDbContext,
    IJwtTokenService jwtTokenService,
    TimeProvider timeProvider,
    PasswordChangedNotifier notifier)
    : ICommandHandler<ChangePasswordCommand, ChangePasswordDto>
{
    public async Task<ErrorOr<ChangePasswordDto>> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        // Compares two supplied values, so it reveals nothing about the account and needs no lookup.
        if (command.NewPassword == command.CurrentPassword)
        {
            SecurityMetrics.RecordPasswordChange("unchanged");
            return ChangePasswordErrors.PasswordUnchanged;
        }

        var user = await userManager.FindByIdAsync(command.UserId.ToString());

        if (user is null)
        {
            SecurityMetrics.RecordPasswordChange("user_not_found");
            return ChangePasswordErrors.UserNotFound;
        }

        // ChangePasswordAsync does not count failures toward lockout, so a hijacked session could guess the
        // current password forever. Check it here with lockout on, exactly as login does.
        var check = await signInManager.CheckPasswordSignInAsync(user, command.CurrentPassword, lockoutOnFailure: true);

        if (check.IsLockedOut)
        {
            SecurityMetrics.RecordPasswordChange("locked_out");
            return ChangePasswordErrors.LockedOut;
        }

        if (!check.Succeeded)
        {
            SecurityMetrics.RecordPasswordChange("incorrect_password");
            return ChangePasswordErrors.IncorrectCurrentPassword;
        }

        var change = await userManager.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);

        if (!change.Succeeded)
        {
            SecurityMetrics.RecordPasswordChange("policy_rejected");
            return
            [
                .. change.Errors.Select(error => Error.Validation($"ChangePassword.{error.Code}", error.Description))
            ];
        }

        // The refresh-token store keeps one record per user. Replacing it signs out every other device
        // (once its access token, at most 15 minutes, runs out) and gives this one a working pair.
        var roles = await userManager.GetRolesAsync(user);
        var permissions = await PermissionResolver.ResolveAsync(securityDbContext, user, roles, cancellationToken);
        var tokenPair = jwtTokenService.CreateTokenPair(user, roles.AsReadOnly(), permissions);

        await RefreshTokenStore.StoreAsync(userManager, user, tokenPair.RefreshToken, timeProvider);

        await notifier.NotifyAsync(user, cancellationToken);

        SecurityMetrics.RecordPasswordChange("success");

        return new ChangePasswordDto
        {
            AccessToken = tokenPair.AccessToken,
            RefreshToken = tokenPair.RefreshToken,
            ExpiresAt = tokenPair.ExpiresAt,
            UserId = user.Id,
            Email = user.Email!
        };
    }
}
