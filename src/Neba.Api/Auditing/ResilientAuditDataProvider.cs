using System.Diagnostics.CodeAnalysis;

using Audit.Core;

using Neba.Api.Discord;

namespace Neba.Api.Auditing;

#pragma warning disable CA1031 // Do not catch general exception types — audit failures must never fail the audited operation

/// <summary>
/// Decorates an <see cref="IAuditDataProvider"/> so that a storage outage degrades to a logged
/// warning instead of failing the caller's SaveChanges/request pipeline (guideline #7 — audit
/// failures must never fail the operation being audited).
/// </summary>
internal sealed class ResilientAuditDataProvider(
        IAuditDataProvider inner,
        IDiscordNotifier discordNotifier,
        ILogger<ResilientAuditDataProvider> logger)
    : AuditDataProvider
{
    [SuppressMessage("Usage", "VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks",
        Justification = "AuditDataProvider.InsertEvent is a synchronous Audit.NET callback with no async overload available to the caller; ASP.NET Core has no captured SynchronizationContext, and IDiscordNotifier.NotifyAsync is guaranteed never to throw and bounded by its own short HTTP timeouts, so this cannot deadlock or hang.")]
    public override object? InsertEvent(AuditEvent auditEvent)
    {
        try
        {
            return inner.InsertEvent(auditEvent);
        }
        catch (Exception exception)
        {
            logger.LogAuditEventInsertFailed(exception);

            // No ambient cancellation token on this sync override, so the alert can't be tied to
            // the caller's cancellation the way InsertEventAsync's is.
            discordNotifier.NotifyAsync(BuildAlert("Audit event insertion failed", auditEvent, exception), CancellationToken.None)
                .GetAwaiter().GetResult();

            return null;
        }
    }

    public override async Task<object?> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            return await inner.InsertEventAsync(auditEvent, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogAuditEventInsertFailed(exception);

            await discordNotifier.NotifyAsync(BuildAlert("Audit event insertion failed", auditEvent, exception), cancellationToken);

            return null;
        }
    }

    [SuppressMessage("Usage", "VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks",
        Justification = "AuditDataProvider.ReplaceEvent is a synchronous Audit.NET callback with no async overload available to the caller; ASP.NET Core has no captured SynchronizationContext, and IDiscordNotifier.NotifyAsync is guaranteed never to throw and bounded by its own short HTTP timeouts, so this cannot deadlock or hang.")]
    public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
    {
        try
        {
            inner.ReplaceEvent(eventId, auditEvent);
        }
        catch (Exception exception)
        {
            logger.LogAuditEventReplaceFailed(exception);

            discordNotifier.NotifyAsync(BuildAlert("Audit event replacement failed", auditEvent, exception), CancellationToken.None)
                .GetAwaiter().GetResult();
        }
    }

    public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            await inner.ReplaceEventAsync(eventId, auditEvent, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogAuditEventReplaceFailed(exception);

            await discordNotifier.NotifyAsync(BuildAlert("Audit event replacement failed", auditEvent, exception), cancellationToken);
        }
    }

    // Stack trace deliberately omitted, same reasoning as GlobalExceptionHandler. Discord has none
    // of the app's PII redaction and a trace can echo argument values. The exception type and
    // message are enough to triage from here. The full trace is still available in Application
    // Insights.
    private static DiscordAlert BuildAlert(string title, AuditEvent auditEvent, Exception exception) =>
        new(
            DiscordAlertSeverity.Warning,
            title,
            exception.Message,
            new Dictionary<string, string>
            {
                ["EventType"] = auditEvent.GetType().FullName ?? "<unknown>",
                ["ExceptionType"] = exception.GetType().FullName ?? "<unknown>"
            });
}

internal static partial class ResilientAuditDataProviderLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to insert audit event; continuing without an audit trail entry.")]
    public static partial void LogAuditEventInsertFailed(
        this ILogger<ResilientAuditDataProvider> logger,
        Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to replace audit event; continuing without an audit trail update.")]
    public static partial void LogAuditEventReplaceFailed(
        this ILogger<ResilientAuditDataProvider> logger,
        Exception exception);
}