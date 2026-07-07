using System.Diagnostics;

using Audit.Core;
using Audit.EntityFramework;

using Neba.Api.Compliance;
using Neba.Api.Identity;

namespace Neba.Api.Auditing;

internal sealed class AuditEnrichmentAction(IHttpContextAccessor httpContextAccessor)
{
    public void OnEventSaving(AuditScope scope) => Enrich(scope.Event);

    internal void Enrich(AuditEvent auditEvent)
    {
        // Constructed manually rather than injecting ICurrentUserService: this action is
        // registered as a singleton (Audit.Core custom actions live for the app's lifetime),
        // but ICurrentUserService is scoped, so it can't be a constructor dependency here.
        // CurrentUserService itself only wraps IHttpContextAccessor, which is safe to hold as a
        // singleton, so constructing it per-event has no lifetime issue.
        var currentUser = new CurrentUserService(httpContextAccessor);

        auditEvent.CustomFields["ActorId"] = currentUser.ActorId;
        auditEvent.CustomFields["CorrelationId"] =
            Activity.Current?.TraceId.ToString()
            ?? httpContextAccessor.HttpContext?.TraceIdentifier
            ?? "none";

        if (auditEvent is not AuditEventEntityFramework efEvent)
        {
            return;
        }

        // No table-name filter needed here: `.UseOptIn().Include<T>(...)` on the
        // ForContext<AppDbContext> (and ForContext<SecurityDbContext>) configuration already
        // restricts which entities produce entries at all - anything reaching this loop was
        // already opted in.
        //
        // IncludeEntityObjects must stay true for entry.Entity to be populated so it can be
        // scrubbed below; entry.Entity is cleared immediately afterward so the raw, unscrubbed
        // entity is never itself serialized into the audit event alongside the scrubbed values.
        foreach (var entry in efEvent.EntityFrameworkEvent.Entries.Where(entry => entry.Entity is not null))
        {
            entry.ColumnValues = AuditPayloadScrubber.Scrub(entry.Entity!)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            entry.Entity = null;
        }
    }
}