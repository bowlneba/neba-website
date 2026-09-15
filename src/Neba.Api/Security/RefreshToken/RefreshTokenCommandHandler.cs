using System.Security.Cryptography;
using System.Text.Json;

using ErrorOr;

using Microsoft.AspNetCore.Identity;

using Neba.Api.Contracts.Security;
using Neba.Api.Database;
using Neba.Api.Messaging;
using Neba.Api.Security.Domain;
using Neba.Api.Security.Infrastructure.Authorization;

namespace Neba.Api.Security.RefreshToken;

internal sealed class RefreshTokenCommandHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    SecurityDbContext securityDbContext,
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

    // Bounds the optimistic-concurrency retry loop below. Real contention - several requests
    // genuinely writing to the same user's record at the same instant - should be rare and settle
    // within a couple of retries; this just stops an unbounded loop under pathological contention.
    private const int MaxCasAttempts = 3;

    public async Task<ErrorOr<RefreshTokenDto>> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null)
        {
            return RefreshTokenErrors.InvalidRefreshToken;
        }

        var incomingHash = RefreshTokenStore.ComputeHash(command.RefreshToken);
        IReadOnlyList<string>? roles = null;
        IReadOnlyCollection<Permissions>? permissions = null;

        for (var attempt = 0; attempt < MaxCasAttempts; attempt++)
        {
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

            var now = timeProvider.GetUtcNow();

            if (now > stored.IssuedAt.AddDays(jwtSettings.RefreshTokenExpiryDays))
            {
                return RefreshTokenErrors.InvalidRefreshToken;
            }

            var matchedSlot = stored.Slots.FirstOrDefault(slot =>
                HashesMatch(slot.Hash, incomingHash)
                && (slot.GracedUntil is null || now <= slot.GracedUntil));

            if (matchedSlot is null)
            {
                return RefreshTokenErrors.InvalidRefreshToken;
            }

            // Roles/permissions don't change between CAS retries for the same user, so only
            // resolve them once - on the first attempt whose token actually validated.
            roles ??= (await userManager.GetRolesAsync(user)).AsReadOnly();
            permissions ??= await PermissionResolver.ResolveAsync(roleManager, roles);

            if (matchedSlot.GracedUntil is not null)
            {
                // Already-graced slot: this is a race replay of a token that was just rotated out
                // from under this caller (or a replay of one), not the current rotation winner.
                // Reissue a one-time bridge token so the caller isn't rejected outright, but never
                // persist a new slot for it - otherwise the same graced hash could be replayed
                // indefinitely within the grace window to mint unlimited independently long-lived
                // sessions with no reuse/theft signal.
                logger.RefreshTokenGracedSlotReplayed(command.UserId);
                var bridgeTokenPair = jwtTokenService.CreateTokenPair(user, roles, permissions);

                return new RefreshTokenDto
                {
                    AccessToken = bridgeTokenPair.AccessToken,
                    RefreshToken = bridgeTokenPair.RefreshToken,
                    ExpiresAt = bridgeTokenPair.ExpiresAt,
                    UserId = user.Id,
                    Email = user.Email!
                };
            }

            var tokenPair = jwtTokenService.CreateTokenPair(user, roles, permissions);
            var newSlots = BuildNextSlots(stored.Slots, matchedSlot, tokenPair.RefreshToken, now);
            var newValue = new StoredRefreshToken { Slots = newSlots, IssuedAt = now };

            var persisted = await RefreshTokenStore.TryStoreAsync(securityDbContext, user, storedJson, newValue);
            if (persisted)
            {
                return new RefreshTokenDto
                {
                    AccessToken = tokenPair.AccessToken,
                    RefreshToken = tokenPair.RefreshToken,
                    ExpiresAt = tokenPair.ExpiresAt,
                    UserId = user.Id,
                    Email = user.Email!
                };
            }

            // Lost the race: another request already wrote to this record between our read and
            // this write. Loop and re-evaluate against the latest state rather than assuming.
        }

        logger.RefreshTokenRotationContended(command.UserId);
        return RefreshTokenErrors.InvalidRefreshToken;
    }

    /// <summary>
    /// Computes the next slot set after redeeming <paramref name="matchedSlot"/>, which is always a
    /// fresh (not-yet-graced) slot - an already-graced match is handled separately as a one-time
    /// bridge that never reaches this method. Every other slot is left completely untouched (so a
    /// sibling caller's own valid session is never affected by this redemption) aside from pruning
    /// graced slots that have already expired. The redeemed slot is demoted to a grace slot rather
    /// than removed, so other requests racing in with that exact hash still succeed - via the
    /// one-time bridge path - until it naturally expires. A brand new, fully valid slot is appended
    /// for the freshly minted token.
    /// </summary>
    private static List<TokenSlot> BuildNextSlots(
        IReadOnlyList<TokenSlot> currentSlots,
        TokenSlot matchedSlot,
        string newRawToken,
        DateTimeOffset now)
    {
        var next = new List<TokenSlot>(currentSlots.Count + 1);

        foreach (var slot in currentSlots)
        {
            if (ReferenceEquals(slot, matchedSlot))
            {
                next.Add(slot with { GracedUntil = now.Add(RotationGraceWindow) });
                continue;
            }

            if (slot.GracedUntil is null || now <= slot.GracedUntil)
            {
                next.Add(slot);
            }
        }

        next.Add(new TokenSlot { Hash = RefreshTokenStore.ComputeHash(newRawToken), GracedUntil = null });

        return next;
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

    // Deliberately Information, not a warning/error: this is expected, handled behavior (a
    // grace-window race replay), not a fault. Logged so its frequency can be measured - a high
    // rate could indicate either heavy legitimate request racing or token replay attempts.
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Refresh token redemption for user {UserId} matched an already-graced slot; reissued a one-time bridge token without persisting a new slot")]
    public static partial void RefreshTokenGracedSlotReplayed(
        this ILogger<RefreshTokenCommandHandler> logger,
        Ulid userId);

    // Distinct from "genuinely invalid token" so ops can tell a contention storm apart from
    // ordinary invalid-token traffic. Should be rare - see MaxCasAttempts.
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Refresh token rotation for user {UserId} failed after exhausting concurrency retries")]
    public static partial void RefreshTokenRotationContended(
        this ILogger<RefreshTokenCommandHandler> logger,
        Ulid userId);
}
