using System.Data.Common;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

using Neba.Infrastructure.Database.Options;

namespace Neba.Infrastructure.Database.Interceptors;

internal sealed class SlowQueryInterceptor(
    ILogger<SlowQueryInterceptor> logger,
    SlowQueryOptions options) : DbCommandInterceptor
{
    private const int MaxQueryLogLength = 2000;

    public override DbDataReader ReaderExecuted(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        LogIfSlow(command, eventData.Duration);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData.Duration);
        return new ValueTask<DbDataReader>(result);
    }

    public override int NonQueryExecuted(
        DbCommand command, CommandExecutedEventData eventData, int result)
    {
        LogIfSlow(command, eventData.Duration);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData.Duration);
        return new ValueTask<int>(result);
    }

    public override object? ScalarExecuted(
        DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        LogIfSlow(command, eventData.Duration);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData.Duration);
        return new ValueTask<object?>(result);
    }

    private void LogIfSlow(DbCommand command, TimeSpan duration)
    {
        var thresholdMs = options.ThresholdMs;
        if (duration.TotalMilliseconds >= thresholdMs)
        {
            logger.LogSlowQuery((long)duration.TotalMilliseconds, thresholdMs, Truncate(command.CommandText));
        }
    }

    private static string Truncate(string commandText) =>
        commandText.Length <= MaxQueryLogLength
            ? commandText
            : string.Concat(commandText.AsSpan(0, MaxQueryLogLength),
                $"... [{commandText.Length - MaxQueryLogLength} chars truncated]");
}

internal static partial class SlowQueryLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Slow query detected: {ElapsedMs}ms (threshold: {ThresholdMs}ms). SQL: {CommandText}")]
    public static partial void LogSlowQuery(
        this ILogger<SlowQueryInterceptor> logger,
        long elapsedMs,
        int thresholdMs,
        string commandText);
}