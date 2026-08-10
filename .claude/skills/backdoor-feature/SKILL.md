---
name: backdoor-feature
description: Produce an implementation plan (markdown only, no code changes) for a new `/legacy` backdoor sync action — the temporary bridge that mirrors an action taken in the Software (nebamgmt-v3) into the website's own database. Researches nebamgmt-v3 for every place the target action can happen, designs the website-side endpoint/job and the Software-side call site(s), and ends with a standalone prompt for implementing the Software-side change. Usage: /backdoor-feature <Software action, e.g. "update bowler", "delete bowler", "merge bowlers">
---

Turn a description of a Software (`nebamgmt-v3`) user action into `docs/plans/software-backdoor-{action-name}.md` — a plan detailed enough to implement from, covering both the website's `/legacy` endpoint and the Software-side call site(s) that trigger it.

**This skill produces a plan only. It does not edit `Neba.Api` source, does not edit `nebamgmt-v3` source, and does not run `dotnet build`/tests.** Implementation is a separate, later step the user drives from the finished markdown file — same separation `feature-plan` uses for regular website features.

## Required reading before starting

Read these, in order, before drafting anything:

1. `docs/api/software-backdoor-plan.md` — the standing architecture decisions (payload shape, file organization, Hangfire enqueue-not-wait, security, audit attribution, testing layers, sunset). Every plan this skill produces must be consistent with this doc, not re-litigate it.
2. `docs/plans/software-backdoor-scaffolding.md` — what already exists (`Legacy/LegacySettings.cs`, `LegacyApiKeyFilter.cs`, `LegacyConfiguration.cs`, `LegacyEndpoints.cs`, `LegacyActor.cs`). Don't re-plan scaffolding that's already built; reference it.
3. `docs/plans/software-backdoor-new-bowler.md` — the first action planned end-to-end with this process. Treat it as the worked example / template for shape, tone, and level of detail (code sketches, an explicit "Decision Recap," a "Legacy Schema Reference" table, a "Summary of what's still undecided" section, and a closing standalone prompt for the Software-side implementer). New plans should read like siblings of this one, not a different format.

If any of these three files don't exist yet, stop and tell the user — this skill assumes the scaffolding phase is already done.

## Workflow

### 1. Confirm the target action and check for prior art

Identify the specific Software user action being mirrored (e.g. "update bowler," "delete bowler," "merge two bowlers"). Check whether a plan doc already exists for it (`docs/plans/software-backdoor-*.md`) — if so, treat this as a revision, not a fresh start.

### 2. Research `nebamgmt-v3` for every entry point

Use the `Explore` agent (`nebamgmt-v3` is a large external repo not already in context — don't hand-search it inline). Ask it to find **every** code path that can trigger the mirrored write, not just the obvious one:

- The obvious direct path (a `*BO` class named for the action).
- Any implicit/side-effect path — the new-bowler precedent found that a check-in submission could *also* create a new bowler via an EF cascade insert, something no one would guess from the action's name alone. Assume similar side-effect paths exist for other actions (e.g. does anything besides `UpdateBowlerBO` ever touch bowler fields? does a merge, an import tool, or a batch job also fire the mirrored write?) and have the agent search broadly (`new {Entity}(`, `.Add(`, `.Update(`, `DataAccess.Update`, relevant repository classes) rather than stopping at the first match.
- For each entry point found, get: file/method, the exact point after which a hook call belongs (after the local commit succeeds, never before), what identifying information is available in scope at that point, and whether the path always fires or is conditional (and how the condition is detected).
- Explicitly check for paths that *look* relevant but turn out not to be (the new-bowler research found `BowlerMergeMapper` only ever updates, never inserts — confirm and exclude rather than assume). Report these as excluded-with-reason, not silently drop them.

Also research the actual entity's schema in `nebamgmt-v3` (the EF entity class backing the relevant table) if the action needs to read data back out of `neba-fwk` — same Explore-agent approach, get the full property list and the file path of any BOM↔EF mapper class. Flag explicitly (don't assume) that column names haven't been independently verified against the real database/`.ssdl` — that's an open item for the plan's undecided-items section, always.

### 3. Decide the endpoint shape

Default: **one route/file per Software entry point**, per the architecture doc's "the route is the event type" rule — this is not up for silent re-litigation per action.

The **only** time multiple entry points collapse into a single route is when every entry point can independently resolve to *the exact same request shape* before calling out (as happened for new-bowler: both `AddBowlerBO` and `CheckInRepository` could resolve their own `bowlerId` locally, so one `{ "bowlerId": id }` route served both). If an entry point can only supply a different identifier (e.g. only a check-in id, not a bowler id), that's a genuine second route with its own request DTO — resolve it on the Software side if at all possible (send the identifier the endpoint can act on directly) rather than adding a second lookup path on the website side "just in case," unless the Software genuinely cannot resolve the better identifier itself.

Never introduce a shared `eventType`/discriminator field to merge routes with different natural payloads — the architecture doc already rejected that shape once (see "Why not a shared `eventType` field?"). If the user asks for it anyway, lay out the tradeoff explicitly (as was done for new-bowler) rather than silently complying or silently refusing.

### 4. Draft the website side

Follow `Legacy/NewBowler.cs`'s shape as the template: one file, named after the action, containing the route-mapping extension method, the request record, a validator, and the Hangfire sync job — all in one file, all deleted together at sunset. Cover, explicitly:

- **Idempotency semantics, decided explicitly, not left as a stub.** Does a repeat call for the same legacy id update the existing website record, or strictly no-op? Pick one and say why — don't leave "TODO: decide" in the plan. (New-bowler landed on strict no-op: simpler, and it avoids inventing "which fields are safe to overwrite from a legacy row" for a bridge that's getting deleted anyway. A different action might genuinely need real updates — e.g. an `UpdateBowler` action's whole point is to update — so this isn't a fixed answer, just a question that must get a stated answer.)
- **Domain construction placement.** If the aggregate doesn't already have a factory/mutator suited to this, don't bolt a legacy-shaped `Create`/`Update` directly onto the aggregate if the aggregate is reasonably expected to grow a "real" first-class version of that operation later — instead add a C# 14 extension member scoped to `Neba.Api.Legacy` (e.g. `{Aggregate}.CreateFromLegacy`, `{Aggregate}.ApplyLegacyUpdate`), so it's deleted with the rest of the backdoor at sunset instead of permanently occupying the aggregate's real API surface. If the aggregate already has a durable method that does exactly what's needed, reuse it — don't duplicate logic just to keep it inside `Legacy/`.
- **Field-mapping edge cases**, mapped explicitly rather than assumed 1:1 — enum/lookup value translation (legacy `int`/sentinel values → website SmartEnum, including a "no match" fallback that logs rather than throws or silently drops), free-text-to-closed-set mapping (e.g. suffix stripping/matching), nullable/sentinel conventions on the legacy side.
- **Logging** via `[LoggerMessage]` source-generated partial extension methods, `internal static partial class {Action}SyncJobLogMessages`, matching `LegacyApiKeyFilterLogMessages`'s shape — write these out in full in the plan, don't just say "add logging." Check whether any logged value needs `[PersonalData]`/`[PrivateData]` (per CLAUDE.md's PII redaction convention) — ids and structural strings don't; names/emails/DOB do.
- **Tests**, per the architecture doc's five layers, but **collapsed into one file** mirroring the action's own one-file source shape (e.g. `tests/Neba.Api.Tests/Legacy/{Feature}/{Action}Tests.cs`, multiple test classes in that one file) — this keeps the entire test surface deletable in the same sweep as the source at sunset. Use `Microsoft.AspNetCore.TestHost` directly for the endpoint test (not `WebApplicationFactory<Program>`) since these are standalone minimal-API delegates with no dependency on the app's `Program`. For the legacy-read-side integration test, prefer a scoped `CREATE TEMP TABLE` against the project's existing Postgres Testcontainers fixture (shaped like the real legacy table) over adding a new same-vendor container package — reach for `Testcontainers.MsSql` only if a specific test genuinely needs MSSQL type-fidelity that Postgres can't stand in for (this was tried and reversed once already: SQLite was rejected for dynamic-typing mismatches with Dapper's record-constructor mapping, then a same-Postgres temp table was preferred over adding an MSSQL container).

### 5. Draft the Software side

For each entry point from step 2: exact file/method/line, exactly where the hook call goes (after the local commit, never before).

**Before sketching the new adapter, check the actual timeout/threading behavior of whatever existing adapter is being cited as precedent** (e.g. `HttpPostAdapter`) — don't assume it's safe to copy just because the architecture doc references it for failure *philosophy*. The new-bowler research found the precedent has no explicit timeout (defaults to ~100s) and is fully synchronous to the UI thread, which was fine for its own dormant, one-off caller but would freeze the UI on every hit for the new adapter, since backdoor calls fire on live, frequent paths. Unless research shows otherwise for this specific action, carry forward the same deviations:

- `HttpClient` (not raw `HttpWebRequest`/`WebClient`) with an explicit short timeout (a few seconds).
- The `HttpClient` itself has a lifetime independent of any calling form/presenter — static/singleton, never constructed-and-disposed per form.
- The call dispatches off the UI thread (`Task.Run` or a real async path) so the local Software action never waits on the network round-trip; the dispatched closure captures **only plain values** (ids, strings), never `this`, a presenter, a form, or any `Control`/`IDisposable` owned by them.
- Non-blocking failure handling (log + existing warning mechanism if one fits, never throw back into the caller's flow), no retry queue on the Software side (retries are Hangfire's job via the website's own automatic retry, once the call lands).
- State plainly that abandoning an in-flight call on full process exit is an accepted consequence of this design, not a gap this step is expected to close.

Flag, don't guess, anything not traceable from research alone — typically: whether the hook point sits inside a wider transaction/rollback scope, and the exact per-environment config key names to use. List these as open items.

### 6. Close with a standalone implementation prompt

End the Software-side section with a "Prompt for the `nebamgmt-v3` implementation" subsection: a self-contained prompt (readable with zero access to this conversation or the rest of the plan file) that an agent could be handed directly to make the Software-side change. It should restate the goal, both/all call sites with file references, the adapter's required shape (timeout, threading, lifetime — don't make the implementer re-derive this from the discussion above), and explicitly list the open items from step 5 as questions the implementer must resolve or flag, not assume.

### 7. Close the whole document with a "Summary of what's still undecided" list

One numbered list, covering both sides. For anything genuinely decided during this planning session, mark it `~~struck through~~` with **Decided** and the one-line reason. For anything not verifiable from within the session (real schema/column names, transaction scope, config conventions), say so explicitly — "could not confirm this from within this session" — rather than presenting an assumption as settled fact.

## Output

One file: `docs/plans/software-backdoor-{action-name}.md` (kebab-case action name, e.g. `software-backdoor-update-bowler.md`). If the plan later needs revisiting after real implementation surfaces a discrepancy (as happened once already — two bugs found reviewing implemented code against the new-bowler plan), append a dated note to the relevant section rather than silently rewriting the original sketch out from under it.
