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
    private Task<IJSObjectReference>? _import;

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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogSessionCookieInteropTimedOut(ex);
            return false;
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogSessionCookieInteropTimedOut(ex);
            return null;
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
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

    // The import runs on its own task so a timeout can't orphan the module reference: the task keeps
    // going, and the next call awaits the same import instead of starting another.
    private async Task<IJSObjectReference> GetModuleAsync(CancellationToken cancellationToken)
    {
        _import ??= ImportAsync();
        try
        {
            return await _import.WaitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _import = null;
            throw;
        }
    }

    private async Task<IJSObjectReference> ImportAsync()
        => _module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", CancellationToken.None, ModulePath);
}

internal static partial class SessionCookieSyncLogMessages
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not write refreshed tokens to the auth cookie; the next page load may sign the user out.")]
    public static partial void LogSessionCookiePersistFailed(this ILogger<SessionCookieSync> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The browser did not answer the session-cookie call in time; the next page load may sign the user out.")]
    public static partial void LogSessionCookieInteropTimedOut(this ILogger<SessionCookieSync> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The session-token endpoint rejected refreshed tokens; the next page load may sign the user out.")]
    public static partial void LogSessionCookiePersistRejected(this ILogger<SessionCookieSync> logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not read the current auth cookie from the browser.")]
    public static partial void LogSessionCookieReadFailed(this ILogger<SessionCookieSync> logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipped disposing the session-token JS module because the circuit is already disconnected.")]
    public static partial void LogSessionCookieModuleDisposeSkipped(this ILogger<SessionCookieSync> logger, Exception exception);
}