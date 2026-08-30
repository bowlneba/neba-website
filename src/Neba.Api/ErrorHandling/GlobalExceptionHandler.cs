using Microsoft.AspNetCore.Diagnostics;

using Neba.Api.Discord;

using ZiggyCreatures.Caching.Fusion;

namespace Neba.Api.ErrorHandling;

internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService,
        IDiscordNotifier discordNotifier,
        IFusionCache cache,
        ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    // Every unhandled exception in the API funnels through here, so a single misbehaving endpoint
    // under client retry could otherwise flood the channel. Debounce per exception-type/path combo
    // rather than per-alert, via IFusionCache rather than a hand-rolled dictionary: entries expire
    // on their own (routes with a ULID segment can't grow the debounce state forever), and
    // GetOrSetAsync's cache-stampede protection makes the check-and-set atomic (no race between two
    // concurrent requests both reading a stale "not yet alerted" state right as the window expires).
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromMinutes(5);

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogException(exception);

        var exceptionType = exception.GetType().FullName ?? "<unknown>";
        var requestPath = httpContext.Request.Path.ToString();

        if (await ShouldAlertAsync(exceptionType, requestPath))
        {
            // Stack trace deliberately omitted. It can echo interpolated argument values (a raw
            // SQL parameter, a validation message embedding user input) into an external channel
            // that has none of the app's PII redaction. The exception type, path, and message are
            // enough to triage from Discord. The full trace is still one click away in Application
            // Insights.
            var alert = new DiscordAlert(
                DiscordAlertSeverity.Critical,
                "Unhandled exception occurred",
                exception.Message,
                new Dictionary<string, string>
                {
                    ["ExceptionType"] = exceptionType,
                    ["RequestPath"] = requestPath
                });

            // CancellationToken.None, not the ambient token: DiscordNotifier.NotifyAsync only
            // swallows non-cancellation exceptions, so a caller-canceled token here would let
            // OperationCanceledException propagate out of this handler before the 500
            // ProblemDetails response below is ever written — the exact outcome this handler
            // exists to prevent.
            await discordNotifier.NotifyAsync(alert, CancellationToken.None);
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unhandled exception occurred while processing the request."
            }
        });
    }

    private async ValueTask<bool> ShouldAlertAsync(string exceptionType, string requestPath)
    {
        var key = $"exception-alert:{exceptionType}:{requestPath}";

        // The factory only runs on a genuine cache miss (no entry, or the previous one expired),
        // and FusionCache guarantees at most one factory execution per key even under concurrent
        // callers - so this closure flips to true at most once per debounce window, regardless of
        // how many requests hit ShouldAlertAsync for the same key at the same moment.
        var isFirstAlertInWindow = false;

        var options = cache.DefaultEntryOptions.Duplicate();
        options.Duration = DebounceWindow;

        await cache.GetOrSetAsync<bool>(
            key,
            (_, _) =>
            {
                isFirstAlertInWindow = true;
                return Task.FromResult(true);
            },
            options: options);

        return isFirstAlertInWindow;
    }
}

internal static partial class GlobalExceptionHandlerLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "An unhandled exception occurred while processing the request."
    )]
    public static partial void LogException(
        this ILogger<GlobalExceptionHandler> logger,
        Exception exception);
}