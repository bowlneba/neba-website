# NFR checkpoint (critical features only)

Read this in Step 2, only when Step 1 marked the feature critical.

Most non-functional requirements for this app are already fixed system-wide (hosting, uptime posture, tech stack) and documented in `docs/architecture/backend.md`/`blazor.md` — re-litigating those per feature would be pure ceremony. This checkpoint only covers the NFRs that genuinely vary **per feature** and change what gets drafted in Phase 1/Phase 2:

- **Performance target** — expected data volume and request rate for this feature's endpoint(s)/page(s). Does a list endpoint need pagination or a page-size cap from day one? Does a query need to be a `ICachedQuery` behind `IFusionCache` (per CLAUDE.md's caching convention) rather than hitting the database on every request?
- **Concurrency/consistency** — can two users act on the same record at once (e.g. two admins editing the same tournament)? If so, does this need an optimistic concurrency check, or is last-write-wins acceptable?
- **Security sensitivity** — does this feature touch PII beyond what's already classified in `Neba.Api.Compliance.DataTaxonomy`? Does it need a new `[PersonalData]`/`[PrivateData]` classification, or an audit trail beyond what's already logged?
- **Availability/degradation** — if a dependency this feature relies on is down (cache, external API, background job), should the feature fail the request outright or degrade gracefully (e.g. the `ResilientAuditDataProvider` pattern — log and continue)?
- **Data retention** — does this feature introduce data that needs a retention/deletion policy, or is it covered by an existing one?

Present the answers (inferred from the description plus repo conventions where obvious, flagged for the user to confirm/correct) as a short list — one page's worth, not a document. Ask: **"Do these NFR targets look right for this feature? I'll use them to shape the Phase 1/Phase 2 drafts."**

Do not proceed until confirmed. Record the confirmed targets under "Decisions locked in during scoping" in Step 6 — each target should visibly inform a specific decision later (e.g. the caching target drives whether Phase 1 lists the query as an `ICachedQuery`, the concurrency target drives whether the aggregate needs a concurrency token).
