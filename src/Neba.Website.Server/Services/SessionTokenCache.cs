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
    // A refresh compares the old and the new token, so one slot would miss on every alternation.
    private const int MaxCachedExpiries = 4;
    private readonly Dictionary<string, DateTimeOffset?> _expiries = [];

    /// <summary>
    /// Returns <paramref name="jwt"/>'s expiry, calling <paramref name="readExpiry"/> only when this
    /// cache hasn't parsed the token yet. Null means the token can't be parsed.
    /// </summary>
    public DateTimeOffset? ExpiryOf(string jwt, Func<string, DateTimeOffset?> readExpiry)
    {
        if (_expiries.TryGetValue(jwt, out var cached))
            return cached;

        if (_expiries.Count >= MaxCachedExpiries)
            _expiries.Clear();

        var expiry = readExpiry(jwt);
        _expiries[jwt] = expiry;
        return expiry;
    }

    public void Set(SessionTokens? tokens)
    {
        IsSeeded = true;
        UserId = tokens?.UserId;
        AccessToken = tokens?.AccessToken;
        RefreshToken = tokens?.RefreshToken;
    }
}
