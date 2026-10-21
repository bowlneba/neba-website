using Microsoft.JSInterop;

namespace Neba.Website.Server.Account.Session;

/// <summary>
/// Circuit-scoped <see cref="ISessionCookieSync"/> that asks the browser to call
/// <see cref="SessionTokenEndpoints"/>. Each browser call gives up after <see cref="InteropTimeout"/>,
/// because callers hold the token refresh lock while they wait.
/// </summary>
internal sealed class SessionCookieSync(
    IJSRuntime jsRuntime,
    SessionTokenProtector protector,
    TimeProvider timeProvider,
    ILogger<SessionCookieSync> logger)
    : ISessionCookieSync, IAsyncDisposable
{
    private const string ModulePath = "./js/session-tokens.js";

    // Blazor's own JS interop timeout is a minute, and a stalled tab would hold the refresh lock that long.
    internal static readonly TimeSpan InteropTimeout = TimeSpan.FromSeconds(5);

    private IJSObjectReference? _module;

    public async Task<bool> PersistAsync(SessionTokens tokens, CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = new CancellationTokenSource(InteropTimeout, timeProvider);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

            var module = await GetModuleAsync(linked.Token);
            var persisted = await module.InvokeAsync<bool>("persist", linked.Token, protector.Protect(tokens));

            if (!persisted)
                logger.LogSessionCookiePersistRejected();

            return persisted;
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException or InvalidOperationException)
        {
            logger.LogSessionCookiePersistFailed(ex);
            return false;
        }
    }

    public async Task<SessionTokens?> ReadAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = new CancellationTokenSource(InteropTimeout, timeProvider);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

            var module = await GetModuleAsync(linked.Token);
            var blob = await module.InvokeAsync<string?>("read", linked.Token);

            return blob is null ? null : protector.Unprotect(blob);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException or InvalidOperationException)
        {
            logger.LogSessionCookieReadFailed(ex);
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null)
            return;

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException ex)
        {
            // Expected when the circuit closes because the browser already went away.
            logger.LogSessionCookieModuleDisposeSkipped(ex);
        }
    }

    private async Task<IJSObjectReference> GetModuleAsync(CancellationToken cancellationToken)
        => _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, ModulePath);
}

internal static partial class SessionCookieSyncLogMessages
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not write refreshed tokens to the auth cookie; the next page load may sign the user out.")]
    public static partial void LogSessionCookiePersistFailed(this ILogger<SessionCookieSync> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The session-token endpoint rejected refreshed tokens; the next page load may sign the user out.")]
    public static partial void LogSessionCookiePersistRejected(this ILogger<SessionCookieSync> logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not read the current auth cookie from the browser.")]
    public static partial void LogSessionCookieReadFailed(this ILogger<SessionCookieSync> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipped disposing the session-token JS module because the circuit is already disconnected.")]
    public static partial void LogSessionCookieModuleDisposeSkipped(this ILogger<SessionCookieSync> logger, Exception exception);
}