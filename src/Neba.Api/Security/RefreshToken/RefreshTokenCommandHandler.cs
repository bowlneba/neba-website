using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Messaging;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Infrastructure.Authorization;

namespace Neba.Api.Security.RefreshToken;

internal sealed class RefreshTokenCommandHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IJwtTokenService jwtTokenService,
    JwtSettings jwtSettings,
    TimeProvider timeProvider,
    ILogger<RefreshTokenCommandHandler> logger)
        : ICommandHandler<RefreshTokenCommand, RefreshTokenDto>
{
    // Covers the window where several requests race a refresh concurrently (e.g. a burst of
    // uploads whose access tokens all expire around the same time): the loser(s) still present the
    // token that was just rotated out from under them by the winner. Long enough to absorb that
    // race, short enough that a genuinely stolen/replayed old token isn't usable for long.
    private static readonly TimeSpan RotationGraceWindow = TimeSpan.FromSeconds(30);

    public async Task<ErrorOr<RefreshTokenDto>> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null)
        {
            return RefreshTokenErrors.InvalidRefreshToken;
        }

        var storedJson = await RefreshTokenStore.GetStoredJsonAsync(userManager, user);
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
        var now = timeProvider.GetUtcNow();

        var matchesCurrent = HashesMatch(stored.Hash, incomingHash);
        var matchesPreviousWithinGrace = !matchesCurrent
            && stored.PreviousHash is not null
            && stored.PreviousHashExpiresAt is not null
            && now <= stored.PreviousHashExpiresAt
            && HashesMatch(stored.PreviousHash, incomingHash);

        if (!matchesCurrent && !matchesPreviousWithinGrace)
        {
            return RefreshTokenErrors.InvalidRefreshToken;
        }

        if (now > stored.IssuedAt.AddDays(jwtSettings.RefreshTokenExpiryDays))
        {
            return RefreshTokenErrors.InvalidRefreshToken;
        }

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await PermissionResolver.ResolveAsync(roleManager, roles);
        var tokenPair = jwtTokenService.CreateTokenPair(user, roles.AsReadOnly(), permissions);

        // Single stored token per user, overwritten on each refresh: rotation is enforced by
        // replacement, not by tracking a token family/generation, so there's no replay signal
        // beyond "the presented token no longer matches what's stored." A request that raced in
        // presenting the just-rotated-out token (matchesPreviousWithinGrace) rotates again without
        // disturbing the existing grace slot, so other concurrent racers presenting that same old
        // token still succeed too, instead of each rotation shrinking the window for the next one.
        var (previousHash, previousHashExpiresAt) = matchesPreviousWithinGrace
            ? (stored.PreviousHash, stored.PreviousHashExpiresAt)
            : (stored.Hash, now.Add(RotationGraceWindow));

        await RefreshTokenStore.StoreAsync(userManager, user, tokenPair.RefreshToken, timeProvider, previousHash, previousHashExpiresAt);

        return new RefreshTokenDto
        {
            AccessToken = tokenPair.AccessToken,
            RefreshToken = tokenPair.RefreshToken,
            ExpiresAt = tokenPair.ExpiresAt,
            UserId = user.Id,
            Email = user.Email!
        };
    }

    private static bool HashesMatch(string storedHash, string incomingHash) =>
        CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(storedHash),
            Convert.FromHexString(incomingHash));
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