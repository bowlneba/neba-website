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
/// <remarks>
/// Every failure is logged. Discord alerts are limited to one per failure kind and event type per
/// <see cref="AlertCooldown"/>, so a sustained storage outage does not post one message per audited
/// operation.
/// </remarks>
internal sealed class ResilientAuditDataProvider(
        IAuditDataProvider inner,
        IDiscordNotifier discordNotifier,
        ILogger<ResilientAuditDataProvider> logger,
        TimeProvider? timeProvider = null)
    : AuditDataProvider
{
    internal static readonly TimeSpan AlertCooldown = TimeSpan.FromMinutes(5);

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly Dictionary<(string Title, string EventType), DateTimeOffset> _lastAlertAt = [];
    private readonly Lock _alertLock = new();

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
    {
        if (!TryStartAlertCooldown(title, auditEvent.EventType ?? "<unknown>"))
        {
            return;
        }

        _ = Task.Run(() => discordNotifier.NotifyAsync(BuildAlert(title, auditEvent, exception), CancellationToken.None));
    }

    // Returns true, and starts a new cooldown, when no alert for this failure kind and event type
    // went out within AlertCooldown. The key set is bounded by the app's event types.
    private bool TryStartAlertCooldown(string title, string eventType)
    {
        var now = _timeProvider.GetUtcNow();

        lock (_alertLock)
        {
            if (_lastAlertAt.TryGetValue((title, eventType), out var lastAlertAt) && now - lastAlertAt < AlertCooldown)
            {
                return false;
            }

            _lastAlertAt[(title, eventType)] = now;
            return true;
        }
    }

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