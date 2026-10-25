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

        // Only a real reset earns the "your password was changed" notice. Links emailed before purposes
        // existed (no purpose) cannot be told apart, so they get none.
        var isReset = command.Purpose == PasswordTokenPurpose.Reset;
        var resetToken = command.Token;

        var providerName = command.Purpose switch
        {
            PasswordTokenPurpose.Invite => InviteTokenProvider.ProviderName,
            PasswordTokenPurpose.Reset => ResetTokenProvider.ProviderName,
            _ => null
        };

        if (providerName is not null)
        {
            var valid = await userManager.VerifyUserTokenAsync(
                user, providerName, UserManager<ApplicationUser>.ResetPasswordTokenPurpose, command.Token);

            if (!valid)
            {
                return SetPasswordFromTokenErrors.InvalidOrExpiredToken;
            }

            // ResetPasswordAsync only accepts the default provider's token, so swap the verified token
            // for a fresh one and keep the password write a single Identity call.
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
            // The password has changed and the session is revoked, so a cancelled request must not skip or fail the notice.
            await notifier.NotifyAsync(user, CancellationToken.None);
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