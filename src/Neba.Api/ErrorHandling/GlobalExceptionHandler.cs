using Microsoft.AspNetCore.Diagnostics;

namespace Neba.Api.ErrorHandling;

internal sealed partial class GlobalExceptionHandler(IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogException(exception);

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