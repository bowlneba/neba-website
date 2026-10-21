using Neba.Website.Server.Account.Session;

namespace Neba.Website.Server.Services;

/// <summary>
/// Where <see cref="BearerTokenHandler"/> gets tokens from and saves refreshed tokens to: a Blazor
/// circuit (<see cref="CircuitTokenSession"/>) or a plain HTTP request (<see cref="CookieTokenSession"/>).
/// </summary>
internal interface ITokenSession
{
    SessionTokenCache Cache { get; }

    /// <summary>Reads the tokens to fill an empty <see cref="Cache"/> with.</summary>
    Task<SessionTokens?> ReadInitialTokensAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reads the browser's current cookie, which another tab may have refreshed past this session.
    /// Null when this session can't see a newer cookie.
    /// </summary>
    Task<SessionTokens?> ReadLatestCookieTokensAsync(CancellationToken cancellationToken);

    /// <summary>Writes refreshed tokens to the browser's cookie. Returns false when it couldn't.</summary>
    Task<bool> PersistAsync(SessionTokens tokens, CancellationToken cancellationToken);
}