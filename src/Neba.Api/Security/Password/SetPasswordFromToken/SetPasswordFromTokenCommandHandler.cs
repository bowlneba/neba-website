using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Contracts.Security;
using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Password.SetPasswordFromToken;

internal sealed class SetPasswordFromTokenCommandHandler(
    UserManager<ApplicationUser> userManager,
    PasswordChangedNotifier notifier,
    ILogger<SetPasswordFromTokenCommandHandler> logger)
        : ICommandHandler<SetPasswordFromTokenCommand>
{
    public async Task<ErrorOr<Success>> HandleAsync(SetPasswordFromTokenCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());

        if (user is null)
        {
            return SetPasswordFromTokenErrors.InvalidOrExpiredToken;
        }

        // An unconfirmed email means this is the first time the user sets a password (an invite). Only a
        // real reset earns the "your password was changed" notice.
        var isReset = user.EmailConfirmed;
        var resetToken = command.Token;

        if (command.Purpose == PasswordTokenPurpose.Invite)
        {
            var valid = await userManager.VerifyUserTokenAsync(
                user, InviteTokenProvider.ProviderName, UserManager<ApplicationUser>.ResetPasswordTokenPurpose, command.Token);

            if (!valid)
            {
                return SetPasswordFromTokenErrors.InvalidOrExpiredToken;
            }

            // ResetPasswordAsync only accepts the default provider's token, so swap the verified invite
            // token for a fresh one and keep the password write a single Identity call.
            resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        }

        var resetResult = await userManager.ResetPasswordAsync(user, resetToken, command.NewPassword);

        if (!resetResult.Succeeded)
        {
            return resetResult.Errors.Any(error => error.Code == "InvalidToken")
                ? SetPasswordFromTokenErrors.InvalidOrExpiredToken
                :
                [
                    .. resetResult.Errors
                        .Select(error => Error.Validation($"SetPasswordFromToken.{error.Code}", error.Description))
                ];
        }

        user.EmailConfirmed = true;
        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
            logger.LogEmailConfirmationUpdateFailed(user.Id, errors);
        }

        // Whoever held the old password (or a stolen session) must not keep a refresh token.
        await RefreshTokenStore.RemoveAsync(userManager, user);

        if (isReset)
        {
            await notifier.NotifyAsync(user, cancellationToken);
        }

        return Result.Success;
    }
}

internal static partial class SetPasswordFromTokenCommandHandlerLogMessages
{
    /// <summary>
    /// The password was already reset successfully by this point — this failure only means the
    /// user's EmailConfirmed flag didn't get set, which must stay visible rather than silently
    /// swallowed since it affects any email-confirmation-gated behavior later.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to confirm email for user {UserId} after a successful password reset. Errors: {Errors}")]
    public static partial void LogEmailConfirmationUpdateFailed(
        this ILogger<SetPasswordFromTokenCommandHandler> logger, Ulid userId, string errors);
}