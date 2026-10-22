namespace Neba.Website.Server.Account.Session;

/// <summary>
/// Reads and writes the browser's auth cookie from inside a Blazor circuit.
/// </summary>
internal interface ISessionCookieSync
{
    /// <summary>Writes <paramref name="tokens"/> into the cookie. Returns false when it couldn't.</summary>
    Task<bool> PersistAsync(SessionTokens tokens, CancellationToken cancellationToken);

    /// <summary>Reads the cookie's current tokens, or null when there are none or the read failed.</summary>
    Task<SessionTokens?> ReadAsync(CancellationToken cancellationToken);
}