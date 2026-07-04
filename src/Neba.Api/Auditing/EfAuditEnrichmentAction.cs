using Audit.Core;
using Audit.EntityFramework;

using Neba.Api.Compliance;
using Neba.Api.Identity;

namespace Neba.Api.Auditing;

internal sealed class EfAuditEnrichmentAction(IHttpContextAccessor httpContextAccessor)
{
    public void OnEventSaving(AuditScope scope)
    {
        var currentUser = new CurrentUserService(httpContextAccessor);

        scope.Event.CustomFields["ActorId"] = currentUser.ActorId;
        scope.Event.CustomFields["CorrelationId"] =
            System.Diagnostics.Activity.Current?.TraceId.ToString()
            ?? httpContextAccessor.HttpContext?.TraceIdentifier
            ?? "none";

        if (scope.Event is not AuditEventEntityFramework efEvent)
        {
            return;
        }

        foreach (var entry in efEvent.EntityFrameworkEvent.Entries)
        {
            // No table-name filter needed here: `.UseOptIn().Include<T>(...)` on the
            // ForContext<AppDbContext> (and ForContext<SecurityDbContext>, per 1i) configuration
            // already restricts which entities produce entries at all — anything reaching this
            // loop was already opted in.

            entry.ColumnValues = entry.Entity is not null
                ? AuditPayloadScrubber.Scrub(entry.Entity)
                    .ToDictionary(kv => kv.Key, kv => kv.Value)
                : entry.ColumnValues;
        }
    }
}