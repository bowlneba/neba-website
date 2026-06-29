using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Logout;

internal sealed class LogoutCommandHandler(UserManager<ApplicationUser> userManager)
    : ICommandHandler<LogoutCommand>
{
    private const string RefreshTokenProvider = "RefreshToken";
    private const string RefreshTokenName = "RefreshToken";

    public async Task<ErrorOr<Success>> HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());

        if (user is not null)
        {
            await userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        }

        return Result.Success;
    }
}