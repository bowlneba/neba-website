using System.Diagnostics.CodeAnalysis;

using Audit.Core;

using Neba.Api.Identity;

namespace Neba.Api.Auditing;

internal sealed class ApiAuditMiddleware(
    RequestDelegate next, 
    ICurrentUserService currentUserService,
    TimeProvider timeProvider,
    ILogger<ApiAuditMiddleware> logger)
{
    private static readonly string[] ExcludedPathPrefixes =
    [
        "/health",
        "/scalar",
        "/background-jobs",
        "/debug"
    ];

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We want to log and continue the request without audit.")]
    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkip(context))
        {
            await next(context);
            return;
        }

        var startedAt = timeProvider.GetUtcNow();

        IAuditScope? scope = null;

        try
        {
            scope = await AuditScope.CreateAsync(options => options
                .EventType("Api:{verb}:{url}")
                .ExtraFields(new
                {
                    Route = context.Request.Path.Value,
                    context.Request.Method,
                    currentUserService.ActorId,
                    CorrelationId = System.Diagnostics.Activity.Current?.TraceId.ToString()
                        ?? context.TraceIdentifier,
                    StartedAt = startedAt
                }));
        }
        catch (Exception ex)
        {
            logger.LogAuditScopeCreationFailed(ex);
        }

        await next(context);

        if (scope is null)
        {
            return;
        }

        try
        {
            scope.Event.CustomFields["StatusCode"] = context.Response.StatusCode;
            scope.Event.CustomFields["ElapsedMs"] = (timeProvider.GetUtcNow() - startedAt).TotalMilliseconds;

            await scope.DisposeAsync();
        }
        catch (Exception ex)
        {
            logger.LogAuditScopeCompletionFailed(ex);
        }
    }

    private static bool ShouldSkip(HttpContext context)
        => HttpMethods.IsGet(context.Request.Method)
            || ExcludedPathPrefixes.Any(prefix => context.Request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
}

internal static partial class ApiAuditMiddlewareLogMessages
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to create API audit scope; continuing request without audit.")]
    public static partial void LogAuditScopeCreationFailed(this ILogger<ApiAuditMiddleware> logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to complete API audit scope; continuing request without audit.")]
    public static partial void LogAuditScopeCompletionFailed(this ILogger<ApiAuditMiddleware> logger, Exception ex);
}