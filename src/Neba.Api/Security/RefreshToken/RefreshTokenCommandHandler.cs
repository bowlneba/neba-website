using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Login;

namespace Neba.Api.Security.RefreshToken;

internal sealed class RefreshTokenCommandHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService,
    JwtSettings jwtSettings,
    TimeProvider timeProvider,
    ILogger<RefreshTokenCommandHandler> logger)
        : ICommandHandler<RefreshTokenCommand, LoginDto>
{
    private const string RefreshTokenProvider = "RefreshToken";
    private const string RefreshTokenName = "RefreshToken";

    public async Task<ErrorOr<LoginDto>> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null)
        {
            return RefreshTokenErrors.InvalidRefreshToken;
        }

        var storedJson = await userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        if (storedJson is null)
        {
            return RefreshTokenErrors.InvalidRefreshToken;
        }

        StoredRefreshToken stored;

        try
        {
            stored = JsonSerializer.Deserialize<StoredRefreshToken>(storedJson)
                ?? throw new InvalidOperationException("Null deserialization result");
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            logger.RefreshTokenDeserializationFailed(ex, command.UserId);
            return RefreshTokenErrors.InvalidRefreshToken;
        }

        var incomingHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(command.RefreshToken)));

        if (!CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(stored.Hash),
            Convert.FromHexString(incomingHash)))
        {
            return RefreshTokenErrors.InvalidRefreshToken;
        }

        if (timeProvider.GetUtcNow() > stored.IssuedAt.AddDays(jwtSettings.RefreshTokenExpiryDays))
        {
            return RefreshTokenErrors.InvalidRefreshToken;
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

internal static partial class RefreshTokenLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to deserialize stored refresh token for user {UserId}")]
    public static partial void RefreshTokenDeserializationFailed(
        this ILogger<RefreshTokenCommandHandler> logger,
        Exception ex,
        Ulid userId);
}