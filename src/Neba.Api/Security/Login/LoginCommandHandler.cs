using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Infrastructure.Authorization;

namespace Neba.Api.Security.Login;

internal sealed class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenService jwtTokenService,
    TimeProvider timeProvider)
        : ICommandHandler<LoginCommand, LoginDto>
{
    public async Task<ErrorOr<LoginDto>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(command.Email);
        if (user is null)
        {
            return LoginErrors.InvalidCredentials;
        }

        // SignInManager enforces lockout and confirmed-email policy; the endpoint always
        // returns a generic 401 regardless of reason to avoid leaking account state (see LoginSummary).
        var signInResult = await signInManager.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            return LoginErrors.InvalidCredentials;
        }

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await PermissionResolver.ResolveAsync(roleManager, roles);
        var tokenPair = jwtTokenService.CreateTokenPair(user, roles.AsReadOnly(), permissions);

        await RefreshTokenStore.StoreAsync(userManager, user, tokenPair.RefreshToken, timeProvider);

        return new LoginDto
        {
            AccessToken = tokenPair.AccessToken,
            RefreshToken = tokenPair.RefreshToken,
            ExpiresAt = tokenPair.ExpiresAt,
            UserId = user.Id,
            Email = user.Email!
        };
    }
}