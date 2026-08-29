using Microsoft.AspNetCore.Diagnostics;

using Neba.Api.Discord;

namespace Neba.Api.ErrorHandling;

internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService,
        IDiscordNotifier discordNotifier,
        ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogException(exception);

        var alert = new DiscordAlert(
            DiscordAlertSeverity.Critical,
            "Unhandled exception occurred",
            exception.Message,
            new Dictionary<string, string>
            {
                ["ExceptionType"] = exception.GetType().FullName ?? "<unknown>",
                ["RequestPath"] = httpContext.Request.Path,
                ["StackTrace"] = exception.StackTrace ?? "<no stack trace>"
            });

        await discordNotifier.NotifyAsync(alert, cancellationToken);

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