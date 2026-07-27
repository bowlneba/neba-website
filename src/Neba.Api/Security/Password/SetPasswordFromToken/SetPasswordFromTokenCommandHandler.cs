using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Password.SetPasswordFromToken;

internal sealed class SetPasswordFromTokenCommandHandler(
    UserManager<ApplicationUser> userManager,
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

        var resetResult = await userManager.ResetPasswordAsync(user, command.Token, command.NewPassword);

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