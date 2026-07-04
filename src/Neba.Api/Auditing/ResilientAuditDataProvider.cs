using Audit.Core;

namespace Neba.Api.Auditing;

#pragma warning disable CA1031 // Do not catch general exception types — audit failures must never fail the audited operation

/// <summary>
/// Decorates an <see cref="IAuditDataProvider"/> so that a storage outage degrades to a logged
/// warning instead of failing the caller's SaveChanges/request pipeline (guideline #7 — audit
/// failures must never fail the operation being audited).
/// </summary>
internal sealed class ResilientAuditDataProvider(IAuditDataProvider inner, ILogger<ResilientAuditDataProvider> logger)
    : AuditDataProvider
{
    public override object? InsertEvent(AuditEvent auditEvent)
    {
        try
        {
            return inner.InsertEvent(auditEvent);
        }
        catch (Exception exception)
        {
            logger.LogAuditEventInsertFailed(exception);
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
            return null;
        }
    }

    public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
    {
        try
        {
            inner.ReplaceEvent(eventId, auditEvent);
        }
        catch (Exception exception)
        {
            logger.LogAuditEventReplaceFailed(exception);
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
        }
    }
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
