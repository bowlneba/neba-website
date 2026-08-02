# Software Backdoor — Architecture Plan

## Purpose

Until the WinForms application ("**the Software**", repo `nebamgmt-v3`) is retired, it stays the system of record for bowler, tournament, and membership data. The website's own database needs to mirror what happens in the Software in near-real time, so that when the Software is finally sunset, the website's data already reflects every action taken in the Software — no one-shot migration required.

This is done with a set of temporary endpoints under the `/legacy` route prefix ("**the backdoor**"). The Software calls these as user actions happen (create bowler, update bowler, etc.), and the website does whatever work is needed to make its own database match.

This document is the standing set of architectural decisions for the backdoor. Individual endpoints get their own branches/PRs; each should follow this doc rather than re-deciding these questions.

---

## Payload Shape — Trigger, Not Data

The Software sends a **trigger**, not a full data payload — an id, plus whatever else that specific action needs to disambiguate it (see File Organization below: the action itself is encoded by which route/file is called, not by a shared `eventType` field):

```json
{ "id": 123 }
```

- `id` is the Software's own database identifier for the affected record (the `neba-fwk` primary key).
- The endpoint looks up everything else itself by querying the Software's database.
- Some actions need more than an id — e.g. a bowler merge needs the duplicate's id too (see `Mappers/Merge` in the Software). That extra field lives in that action's own request DTO, not in a shared generic trigger type.

**Why not send the full record?** A full-payload contract means every time the website's data needs grow, the Software's outbound call has to change too — touching code in an application we've explicitly decided not to modify. An id-only trigger keeps the Software's side fixed forever; all future changes (new fields, new derived data, bug fixes) live entirely on the website side.

**Why not a shared `eventType` field?** Earlier drafts of this plan had one route per resource with an `eventType` enum dispatching internally. Once each action gets its own file and its own route (see below), the route itself *is* the event type — `POST /legacy/bowlers/new` vs. `POST /legacy/bowlers/update` needs no extra field to say which one happened, and each action's request DTO can be shaped exactly to what it needs instead of a lowest-common-denominator trigger type.

---

## Website Side (`/legacy` routes)

### Framework

- **Minimal APIs**, not FastEndpoints. These are throwaway bridge code, not part of the long-term REPR/CQRS architecture — they don't need the ceremony (no separate Endpoint/Command/Handler/Validator files, no `ErrorOr`).
- Live in their own area of the solution, separate from `Features/*` (`src/Neba.Api/Legacy/`), so it's obvious at a glance which endpoints are permanent and which are meant to be deleted wholesale post-sunset.

### File organization — one file per action, true vertical slice

Each Software action gets its **own single file**, named after the action — `Legacy/NewBowler.cs`, `Legacy/UpdateBowler.cs`, `Legacy/DeleteBowler.cs`, `Legacy/MergeBowler.cs`, etc. Everything that action needs lives in that one file: the route-mapping extension method, the request DTO, a validator if the action warrants one, and the background job class that does the real work. Nothing here is split across folders the way `Features/*` splits Endpoint/Command/Handler — the whole slice is one file, top to bottom.

The route itself is what used to be carried by the `eventType` field — one route per file:

```csharp
// Legacy/NewBowler.cs
namespace Neba.Api.Legacy;

internal static class NewBowlerEndpoint
{
    public static void MapNewBowler(this IEndpointRouteBuilder app)
    {
        app.MapPost("/legacy/bowlers/new", (
            NewBowlerRequest request,
            IValidator<NewBowlerRequest> validator,
            IBackgroundJobClient jobs) =>
        {
            var validation = validator.Validate(request);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            jobs.Enqueue<NewBowlerSyncJob>(job => job.SyncAsync(request.Id, CancellationToken.None));

            return Results.Accepted();
        });
    }
}

internal sealed record NewBowlerRequest(int Id);

// Illustrative — most actions won't need real rules beyond "Id > 0", but this is
// where a file-specific validator lives when one is warranted.
internal sealed class NewBowlerRequestValidator : AbstractValidator<NewBowlerRequest>
{
    public NewBowlerRequestValidator() => RuleFor(x => x.Id).GreaterThan(0);
}

internal sealed class NewBowlerSyncJob(AppDbContext db, IDbConnection legacyConnection, ILogger<NewBowlerSyncJob> logger)
{
    public async Task SyncAsync(int legacyBowlerId, CancellationToken ct)
    {
        // Dapper query against neba-fwk, map, Bowler.Create(...) via AppDbContext, SaveChangesAsync.
    }
}
```

A small aggregator (`Legacy/LegacyEndpoints.cs`) calls each file's `Map*` extension so they get wired up from one place in startup — minimal APIs don't auto-register the way FastEndpoints does, so this one bit of central bookkeeping is unavoidable:

```csharp
internal static class LegacyEndpoints
{
    public static void MapLegacyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapNewBowler();
        app.MapUpdateBowler();
        app.MapDeleteBowler();
        app.MapMergeBowler();
        // ...one line per Legacy/*.cs file
    }
}
```

### Response model — enqueue, don't wait

The Software should never block a user action on the website's round-trip. Each route does the minimum synchronously — deserialize and validate the request — then **enqueues a Hangfire job and returns immediately** (`202 Accepted`). All the actual sync work (Dapper read, domain write) happens in the background job, off the request thread.

This also covers part of the reconciliation question below for free: Hangfire's global `AutomaticRetryAttribute` (already registered for every job via `AddBackgroundJobs`) retries a failed sync automatically without any extra plumbing.

**Skip the existing job ceremony on purpose.** The codebase's standing background-job pattern (`IBackgroundJob` + `IBackgroundJobHandler<TJob>`, dispatched through `HangfireBackgroundJobScheduler.ExecuteJobAsync` for OpenTelemetry activities and `HangfireMetrics`) is right for permanent, first-class jobs — it's not worth it for bridge code that gets deleted at sunset. Instead, enqueue directly against Hangfire's own API from inside the same file (see `NewBowlerSyncJob` above, enqueued via `IBackgroundJobClient.Enqueue<T>(...)`). No `IBackgroundJob` record type, no separate handler interface. It still picks up the global `[AuditJobExecutionFilter]` audit trail (the global registration only skips jobs that already carry that attribute explicitly), just not the custom activity/metrics wrapping — an acceptable trade here.

Two different data sources are in play inside each `*SyncJob`, and they get different treatment:

- **Reading from the Software's database** (`neba-fwk`) — plain **Dapper** against a raw `IDbConnection`. This is a foreign, external data source with no domain model of its own; a lightweight query for exactly the columns needed is appropriate. No repository abstraction, no EF model for the legacy schema.
- **Writing to the website's own database** — still goes through the **website's own `AppDbContext` and domain aggregates** (`Bowler.Create(...)`, `Season.AssignX(...)`, etc.), not raw SQL. Skipping FastEndpoints/CQRS ceremony is about cutting boilerplate for temporary code, not about bypassing the domain's own invariants — the aggregates are what keep the website's data valid, and that doesn't stop mattering just because the trigger came from `/legacy` instead of a normal endpoint.

### Example — `Legacy/NewBowler.cs`

1. `POST /legacy/bowlers/new` receives `{ id }`, validates it, enqueues `NewBowlerSyncJob.SyncAsync(id, ct)`, returns `202 Accepted`.
2. *(in the background job)* Dapper query against `neba-fwk` for the bowler row (and whatever related rows are needed — address, membership, etc.) by `id`.
3. Map the legacy row(s) into whatever the website's domain needs.
4. Look up whether the website already has a record for this Software id via its `LegacyId`/`SoftwareId` column.
5. Call `Bowler.Create(...)` on `AppDbContext` and `SaveChangesAsync`.

### Security

All `/legacy` routes sit behind a single shared **API key**, checked via a Minimal API endpoint filter (or route-group filter) on the `/legacy` group — not the website's normal cookie/policy-based auth, since the caller is a machine, not a logged-in user. Key lives in Key Vault like other secrets.

---

## Software Side (WinForms, `nebamgmt-v3`)

### Where the outbound call is wired in

Each Software `*BO` class (`AddBowlerBO`, `UpdateBowlerBO`, etc.) already funnels through a `DataAccess.Add(...)` / `.Update(...)` call inside a `try`/`catch(DatabaseCommitException)` block (see `BaseAdd.vb`, `AddBowlerBO.cs`). The backdoor call goes **after** that local database commit succeeds — never before, and never in a way that can make the local operation fail because the website was unreachable.

### Failure philosophy

The Software already has a precedent for "call an external HTTP service, don't blow up if it's unreachable": `Adapters/HttpPostAdapter.vb`'s `SmartyStreetsAdapter` checks connectivity first and calls `SetError(...)` (which the BO's existing `Errors`/warning plumbing surfaces to the presenter) rather than throwing. The backdoor call follows the same shape:

- Fire the request after the local commit.
- On failure (network, non-2xx, timeout), log/record it and surface a **non-blocking warning** through the existing `SetWarning`/`Errors` mechanism — the user's action in the Software still succeeded locally.
- No retry queue on the Software side. This is bridge code; a dropped sync is expected to be rare and is a website-side reconciliation problem (see Open Questions), not something worth building durable messaging for on a 4.8 WinForms app that's going away.

### New adapter, not a new pattern

A new small adapter (parallel to `HttpPostAdapter`) wraps the outbound call: builds the JSON body, sets the API key header, posts to the configured `/legacy` base URL (per-environment, same way `App.{Config}.config` already varies other settings), and returns success/failure to the calling `*BO`.

---

## Decisions

- **Legacy DB reachability**: `neba-fwk` is Azure SQL Database, already open to all IP addresses (multiple users connect from different locations). No networking work needed — the website's Dapper connection just needs a connection string in Key Vault.
- **Legacy-id mapping**: each website aggregate that mirrors a Software entity carries a nullable `LegacyId`/`SoftwareId` column holding the `neba-fwk` primary key. A `*SyncJob` uses it to decide create-vs-update against the website's own data. These columns get dropped once the Software is sunset and the mapping is no longer needed.
- **Reconciliation safety net**: no diff/backfill job for now. Hangfire's automatic retry covers transient failures; permanent failures surface in the Hangfire dashboard's failed-jobs list and get handled manually if/when they show up. Revisit only if manual intervention turns out to be frequent enough to be worth automating.

---

## Sunset

When the Software is retired, the entire `src/Neba.Api/Legacy/` folder, the API key, and the Software-side adapter all get deleted together — none of this is meant to outlive the migration.
