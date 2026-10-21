using Neba.Website.Server.Account.Session;

namespace Neba.Website.Server.Services;

/// <summary>
/// The current token pair for one DI scope: a Blazor circuit, or a single HTTP request. Once
/// filled, <see cref="BearerTokenHandler"/> reads and refreshes tokens here instead of the auth
/// cookie, because inside a circuit the cookie it can see is the one from when the circuit opened.
/// </summary>
internal sealed class SessionTokenCache
{
    /// <summary>True once the cache has been filled from the cookie, even if no one was signed in.</summary>
    public bool IsSeeded { get; private set; }

    public string? UserId { get; private set; }

    public string? AccessToken { get; private set; }

    public string? RefreshToken { get; private set; }

    /// <summary>
    /// Serializes seeding and refreshing within this scope, so concurrent calls don't each redeem
    /// the same refresh token (only the first would win). Not disposed: a SemaphoreSlim(1,1) holds
    /// no unmanaged resource worth tracking.
    /// </summary>
    public SemaphoreSlim RefreshLock { get; } = new(1, 1);

    // The handler is shared across users, so a per-handler expiry cache would thrash between them.
    // Caches the expiry time, not a yes/no answer, because the answer changes as time passes.
    private TokenExpiry? _lastExpiry;

    /// <summary>
    /// Returns <paramref name="jwt"/>'s expiry, calling <paramref name="readExpiry"/> only when it
    /// isn't the token this cache parsed last. Null means the token can't be parsed.
    /// </summary>
    public DateTimeOffset? ExpiryOf(string jwt, Func<string, DateTimeOffset?> readExpiry)
    {
        var cached = _lastExpiry;
        if (cached is null || cached.Token != jwt)
        {
            cached = new TokenExpiry(jwt, readExpiry(jwt));
            _lastExpiry = cached;
        }

        return cached.ExpiresAt;
    }

    public void Set(SessionTokens? tokens)
    {
        IsSeeded = true;
        UserId = tokens?.UserId;
        AccessToken = tokens?.AccessToken;
        RefreshToken = tokens?.RefreshToken;
    }
}

internal sealed record TokenExpiry(string Token, DateTimeOffset? ExpiresAt);