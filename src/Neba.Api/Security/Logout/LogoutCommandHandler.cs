using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Logout;

internal sealed class LogoutCommandHandler(UserManager<ApplicationUser> userManager)
    : ICommandHandler<LogoutCommand>
{
    public async Task<ErrorOr<Success>> HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());

        if (user is not null)
        {
            await RefreshTokenStore.RemoveAsync(userManager, user);
        }

        return Result.Success;
    }
}