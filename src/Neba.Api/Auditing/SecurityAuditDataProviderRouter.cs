using Audit.Core;

using Neba.Api.Database;

namespace Neba.Api.Auditing;

/// <summary>
/// Routes <see cref="SecurityDbContext"/> audit events to their own data provider, separate from
/// all other audit events (EF app data, API requests), so the identity/authorization audit trail
/// can have its own Azure table with independently scoped RBAC and retention.
/// </summary>
internal sealed class SecurityAuditDataProviderRouter(
    IAuditDataProvider securityProvider,
    IAuditDataProvider defaultProvider) : AuditDataProvider
{
    private const string SecurityEventType = "EF:SecurityDbContext";

    public override object? InsertEvent(AuditEvent auditEvent)
        => Resolve(auditEvent).InsertEvent(auditEvent);

    public override Task<object?> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        => Resolve(auditEvent).InsertEventAsync(auditEvent, cancellationToken);

    public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        => Resolve(auditEvent).ReplaceEvent(eventId, auditEvent);

    public override Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        => Resolve(auditEvent).ReplaceEventAsync(eventId, auditEvent, cancellationToken);

    private IAuditDataProvider Resolve(AuditEvent auditEvent)
        => auditEvent.EventType == SecurityEventType
            ? securityProvider
            : defaultProvider;
}
