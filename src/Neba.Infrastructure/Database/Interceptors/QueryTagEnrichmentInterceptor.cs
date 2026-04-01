using System.Data.Common;
using System.Diagnostics;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Neba.Infrastructure.Database.Interceptors;

internal sealed class QueryTagEnrichmentInterceptor(IHttpContextAccessor httpContextAccessor) : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        EnrichCommand(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        EnrichCommand(command);
        return new ValueTask<InterceptionResult<DbDataReader>>(result);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "CommandText is prepended with a diagnostic comment only. User-controlled values (userId, endpoint, traceId) are sanitized by stripping '*/' to prevent comment breakout.")]
    private void EnrichCommand(DbCommand command)
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null) return;

        var userId = Sanitize(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anon");
        var endpoint = Sanitize(ctx.GetEndpoint()?.DisplayName ?? "unknown");
        var traceId = Sanitize(Activity.Current?.TraceId.ToString() ?? ctx.TraceIdentifier);

        command.CommandText = $"/* user:{userId} endpoint:{endpoint} trace:{traceId} */\n{command.CommandText}";
    }

    private static string Sanitize(string value) => value.Replace("*/", string.Empty, StringComparison.Ordinal);
}