using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;

namespace Neba.Api.Security.Login;

internal sealed class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService,
    TimeProvider timeProvider)
        : ICommandHandler<LoginCommand, LoginDto>
{
    private const string RefreshTokenProvider = "RefreshToken";
    private const string RefreshTokenName = "RefreshToken";

    public async Task<ErrorOr<LoginDto>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(command.Email);
        if (user is null)
        {
            return LoginErrors.InvalidCredentials;
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, command.Password);
        if (!passwordValid)
        {
            return LoginErrors.InvalidCredentials;
        }

        var roles = await userManager.GetRolesAsync(user);
        var tokenPair = jwtTokenService.CreateTokenPair(user, roles.AsReadOnly());

        await StoreRefreshTokenAsync(user, tokenPair.RefreshToken);

        return new LoginDto
        {
            AccessToken = tokenPair.AccessToken,
            RefreshToken = tokenPair.RefreshToken,
            ExpiresAt = tokenPair.ExpiresAt,
            UserId = user.Id,
            Email = user.Email!
        };
    }

    private async Task StoreRefreshTokenAsync(ApplicationUser user, string rawToken)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var stored = new StoredRefreshToken
        {
            Hash = hash,
            IssuedAt = timeProvider.GetUtcNow()
        };
        var json = JsonSerializer.Serialize(stored);

        await userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName, json);
    }
}