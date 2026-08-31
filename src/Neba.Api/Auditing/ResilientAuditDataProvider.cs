using Audit.Core;

using Neba.Api.Compliance;
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
    public override object? InsertEvent(AuditEvent auditEvent)
    {
        try
        {
            return inner.InsertEvent(auditEvent);
        }
        catch (Exception exception)
        {
            logger.LogAuditEventInsertFailed(exception);
            NotifyDiscordFireAndForget("Audit event insertion failed", auditEvent, exception);

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
            NotifyDiscordFireAndForget("Audit event insertion failed", auditEvent, exception);

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
            NotifyDiscordFireAndForget("Audit event replacement failed", auditEvent, exception);
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
            NotifyDiscordFireAndForget("Audit event replacement failed", auditEvent, exception);
        }
    }

    // Fire-and-forget rather than awaited/blocking, same reasoning as DiscordJobFailureFilter:
    // this class's whole purpose is to keep an audit failure cheap for the audited operation, so
    // blocking every audit write on Discord's own timeout/retry policy during a sustained storage
    // outage (all four overrides previously awaited or GetAwaiter().GetResult()'d this call)
    // defeats that purpose. NotifyAsync already swallows every non-cancellation failure
    // internally, so there's nothing here to observe or retry. CancellationToken.None, not any
    // caller-supplied token - the alert must outlive the audited operation's own cancellation.
    private void NotifyDiscordFireAndForget(string title, AuditEvent auditEvent, Exception exception)
        => _ = Task.Run(() => discordNotifier.NotifyAsync(BuildAlert(title, auditEvent, exception), CancellationToken.None));

    // Stack trace deliberately omitted, same reasoning as GlobalExceptionHandler. Discord has none
    // of the app's PII redaction and a trace can echo argument values. The exception type and
    // message are enough to triage from here. The full trace is still available in Application
    // Insights. DiscordMessageRedactor masks any embedded email address in the message itself,
    // same reasoning as GlobalExceptionHandler.
    private static DiscordAlert BuildAlert(string title, AuditEvent auditEvent, Exception exception) =>
        new(
            DiscordAlertSeverity.Warning,
            title,
            DiscordMessageRedactor.Redact(exception.Message),
            new Dictionary<string, string>
            {
                ["EventType"] = auditEvent.EventType ?? "<unknown>",
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