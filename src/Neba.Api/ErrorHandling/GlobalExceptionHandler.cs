using System.Collections.Concurrent;

using Microsoft.AspNetCore.Diagnostics;

using Neba.Api.Discord;

namespace Neba.Api.ErrorHandling;

internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService,
        IDiscordNotifier discordNotifier,
        TimeProvider timeProvider,
        ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    // Every unhandled exception in the API funnels through here, so a single misbehaving endpoint
    // under client retry could otherwise flood the channel. Debounce per exception-type/path combo
    // rather than per-alert: registered as a singleton (AddExceptionHandler<T>()), so this instance
    // is shared across every request.
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromMinutes(5);
    private readonly ConcurrentDictionary<(string ExceptionType, string RequestPath), DateTimeOffset> _lastAlertedAt = new();

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogException(exception);

        var exceptionType = exception.GetType().FullName ?? "<unknown>";
        var requestPath = httpContext.Request.Path.ToString();

        if (ShouldAlert(exceptionType, requestPath))
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

            await discordNotifier.NotifyAsync(alert, cancellationToken);
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

    private bool ShouldAlert(string exceptionType, string requestPath)
    {
        var now = timeProvider.GetUtcNow();
        var key = (exceptionType, requestPath);

        var lastAlertedAt = _lastAlertedAt.GetOrAdd(key, DateTimeOffset.MinValue);
        if (now - lastAlertedAt < DebounceWindow)
        {
            return false;
        }

        _lastAlertedAt[key] = now;
        return true;
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