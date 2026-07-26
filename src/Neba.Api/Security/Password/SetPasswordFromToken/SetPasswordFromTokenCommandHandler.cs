using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Password.SetPasswordFromToken;

internal sealed class SetPasswordFromTokenCommandHandler(UserManager<ApplicationUser> userManager)
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
        await userManager.UpdateAsync(user);

        return Result.Success;
    }
}