using Neba.Website.Server.Account.Session;

namespace Neba.Website.Server.Services;

/// <summary>
/// Token session for calls made inside a Blazor circuit. The circuit's own
/// <see cref="SessionTokenCache"/> holds the live tokens, and the browser cookie gets updated
/// through <see cref="ISessionCookieSync"/>, since the circuit can't set cookies itself.
/// </summary>
/// <param name="cache">The circuit's token cache.</param>
/// <param name="connectionContext">
/// The HttpContext of the request that opened the circuit's connection, if available. Its cookie
/// was current when the circuit opened, which is all the first fill needs.
/// </param>
/// <param name="cookieSync">Reads and writes the browser cookie.</param>
internal sealed class CircuitTokenSession(
    SessionTokenCache cache,
    HttpContext? connectionContext,
    ISessionCookieSync cookieSync)
    : ITokenSession
{
    public SessionTokenCache Cache => cache;

    public Task<SessionTokens?> ReadInitialTokensAsync(CancellationToken cancellationToken)
        => connectionContext is not null
            ? SessionTokenReader.ReadAsync(connectionContext)
            : cookieSync.ReadAsync(cancellationToken);

    // Skips connectionContext: its cookie never changes for the life of the connection.
    public Task<SessionTokens?> ReadLatestCookieTokensAsync(CancellationToken cancellationToken)
        => cookieSync.ReadAsync(cancellationToken);

    public Task<bool> PersistAsync(SessionTokens tokens, CancellationToken cancellationToken)
        => cookieSync.PersistAsync(tokens, cancellationToken);
}