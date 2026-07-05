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

        foreach (var entry in efEvent.EntityFrameworkEvent.Entries)
        {
            // No table-name filter needed here: `.UseOptIn().Include<T>(...)` on the
            // ForContext<AppDbContext> (and ForContext<SecurityDbContext>) configuration
            // already restricts which entities produce entries at all — anything reaching this
            // loop was already opted in.
            entry.ColumnValues = entry.Entity is not null
                ? AuditPayloadScrubber.Scrub(entry.Entity)
                    .ToDictionary(kv => kv.Key, kv => kv.Value)
                : entry.ColumnValues;
        }
    }
}
