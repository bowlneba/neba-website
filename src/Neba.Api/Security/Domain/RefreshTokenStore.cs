using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;

namespace Neba.Api.Security.Domain;

// Centralizes the refresh-token storage format and Identity token-provider/name pair so
// Login, RefreshToken, and Logout handlers stay in sync if the format or naming changes.
internal static class RefreshTokenStore
{
    public const string Provider = "RefreshToken";
    public const string Name = "RefreshToken";

    /// <summary>Computes the hash used to identify a raw refresh token in storage.</summary>
    public static string ComputeHash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    /// <summary>
    /// Replaces any existing record with a single, fully-valid slot for <paramref name="rawToken"/>.
    /// Used only at login, which always starts a session with no grace state to preserve.
    /// </summary>
    public static Task StoreAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string rawToken,
        TimeProvider timeProvider)
    {
        var stored = new StoredRefreshToken
        {
            Slots = [new TokenSlot { Hash = ComputeHash(rawToken), GracedUntil = null }],
            IssuedAt = timeProvider.GetUtcNow()
        };

        return userManager.SetAuthenticationTokenAsync(user, Provider, Name, JsonSerializer.Serialize(stored));
    }

    /// <summary>
    /// Writes <paramref name="newValue"/> only if the currently-stored value still matches
    /// <paramref name="expectedCurrentJson"/> — an optimistic-concurrency guard against two
    /// requests that both read the same stored record and would otherwise clobber each other
    /// (<c>AspNetUserTokens</c> has no concurrency token of its own, so the whole stored value is
    /// used as the version check). Returns <see langword="false"/> when another write already
    /// happened in between — the caller should re-read and retry against the latest state.
    /// </summary>
    public static async Task<bool> TryStoreAsync(
        SecurityDbContext dbContext,
        ApplicationUser user,
        string expectedCurrentJson,
        StoredRefreshToken newValue)
    {
        var json = JsonSerializer.Serialize(newValue);

        var rowsAffected = await dbContext.Set<IdentityUserToken<Ulid>>()
            .Where(t =>
                t.UserId == user.Id
                && t.LoginProvider == Provider
                && t.Name == Name
                && t.Value == expectedCurrentJson)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Value, json));

        // ExecuteUpdateAsync writes straight to the database and bypasses the change tracker, so
        // if this row is already tracked on this same DbContext (e.g. UserManager loaded it
        // earlier via GetAuthenticationTokenAsync, which resolves through DbSet.FindAsync's local
        // identity-map lookup), that tracked instance's Value is now stale - regardless of whether
        // THIS write won or lost the CAS. On a win, our own write bypassed the tracker. On a loss,
        // some other write committed underneath us. Either way, the next read in a CAS retry loop
        // must not silently return the pre-write value via the identity map - detach unconditionally
        // so it goes back to the database.
        var tracked = dbContext.ChangeTracker.Entries<IdentityUserToken<Ulid>>()
            .FirstOrDefault(e =>
                e.Entity.UserId == user.Id
                && e.Entity.LoginProvider == Provider
                && e.Entity.Name == Name);

        tracked?.State = EntityState.Detached;

        return rowsAffected > 0;
    }

    public static Task<string?> GetStoredJsonAsync(UserManager<ApplicationUser> userManager, ApplicationUser user)
        => userManager.GetAuthenticationTokenAsync(user, Provider, Name);

    public static Task RemoveAsync(UserManager<ApplicationUser> userManager, ApplicationUser user)
        => userManager.RemoveAuthenticationTokenAsync(user, Provider, Name);
}