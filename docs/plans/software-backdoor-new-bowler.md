# New Bowler Backdoor — Implementation Plan

Concrete plan for the first real `/legacy` action, built on the scaffolding in `docs/plans/software-backdoor-scaffolding.md` and the standing decisions in `docs/api/software-backdoor-plan.md`. This document is a **plan only** — nothing here has been applied to either repo yet. It's written so it can be handed to whoever implements it (or split into a follow-up prompt for the `nebamgmt-v3` side), not as a record of work already done.

## Decision Recap

- **Two creation sites confirmed in `nebamgmt-v3`, one endpoint on the website.** Research into `nebamgmt-v3` found exactly two places a new `Bowler` row can be inserted (see "Software Side" below for file/line detail): `AddBowlerBO` (explicit) and the check-in "quick add" path via `CheckInRepository` (implicit, cascaded through EF when a check-in carries a not-yet-persisted bowler with a synthetic negative id). A third path (`BowlerMergeMapper`) was confirmed to only ever update an existing bowler, never insert — excluded.
- **Both sites resolve their own `bowlerId` locally before calling out.** `AddBowlerBO` already has the new id in scope right after its insert. `CheckInRepository` needs a small change (see below) to surface `checkInEntity.Bowler.Id` after `Commit()`, since it isn't currently returned to any caller. Because both sites end up with a real, already-persisted bowler id in hand, **one route serves both**: `POST /legacy/bowlers/new`, body `{ "bowlerId": <id> }`. No second "resolve from check-in id" route is needed — that shape was considered and rejected once we confirmed both call sites can resolve the id themselves (see conversation: a `checkInId`-based route would only be justified if the Software couldn't resolve the bowler id itself, which isn't the case here).
- **Endpoint design follows the plan doc's per-action convention**, not a single endpoint with a discriminator field — this was compared explicitly against a "switch on which id field is present" design and rejected because it re-introduces the `eventType`-style dispatch the architecture plan already ruled out (see `docs/api/software-backdoor-plan.md`, "Why not a shared `eventType` field?"). Since there's only one route needed here, this mostly matters as precedent for the *next* action, not as a live tradeoff in this file.

---

## Legacy Schema Reference (`neba-fwk`, `Bowler` table)

From `nebamgmt-v3`'s EF6 entity (`Data/NEBA.Data/Bowler.cs`, database-first model backing `NebaEntities.Bowlers`):

| Property | Type | Notes |
|---|---|---|
| `Id` | `int` | primary key |
| `FirstName` | `string` | |
| `MiddleInitial` | `string` | initial only, not a full middle name |
| `LastName` | `string` | |
| `Suffix` | `string` | |
| `Gender` | `int` | `-1`=None, `0`=Male, `1`=Female (`BOM.Membership.Gender`) |
| `DateOfBirth` | `DateTime?` | |
| `UsbcId` | `string` | |
| *(other columns exist — email, phone, address, USBC membership, deceased flag, audit timestamps — omitted here as not needed for this action)* |

**Open item before implementing**: confirm the real SQL table/column names against `neba-fwk` directly (or the `.edmx`/`.ssdl` section) rather than trusting the EF POCO property names verbatim — database-first scaffolding usually mirrors real column names exactly, but this should be verified, not assumed, before writing the Dapper query.

No `Gender.None`/nickname equivalent exists on the website's `Gender` SmartEnum (`Male`/`Female` only) — legacy `Gender == -1` must map to a `null` `Gender?` on the website side, not an exception.

---

## Website Side (`src/Neba.Api`)

### New: `Bowler.CreateFromLegacy(...)` — extension member, not an instance/static factory on `Bowler` itself

`Bowler` (`Features/Bowlers/Domain/Bowler.cs`) currently has no factory method — it's only ever populated via migration seeding with an object initializer. This action needs one, but **not** as a `public static ErrorOr<Bowler> Create(...)` on the class itself the way `Sponsor.Create` (`Features/Sponsors/Domain/Sponsor.cs:150`) does — `Bowler.Create` is being deliberately reserved for when the website grows a "real" first-class bowler-creation feature of its own (not a legacy mirror), so that factory can own the full, real invariant set without this backdoor's narrower legacy-mapping concerns baked into it.

Instead, this lives as a C# 14 extension member scoped to `Neba.Api.Legacy` (matching the codebase's extension-method convention, e.g. `LegacyConfiguration.cs`), callable as `Bowler.CreateFromLegacy(...)`:

```csharp
namespace Neba.Api.Legacy;

internal static class LegacyBowlerFactory
{
    extension(Bowler)
    {
        public static ErrorOr<Bowler> CreateFromLegacy(
            string firstName,
            string lastName,
            string? middleName = null,
            NameSuffix? suffix = null,
            int? legacyId = null,
            Gender? gender = null,
            DateOnly? dateOfBirth = null)
        {
            var name = Name.Create(firstName, lastName, middleName, suffix);
            if (name.IsError)
            {
                return name.Errors;
            }

            return new Bowler
            {
                Id = BowlerId.New(),
                Name = name.Value,
                LegacyId = legacyId,
                Gender = gender,
                DateOfBirth = dateOfBirth
            };
        }
    }
}
```

Same validation, same shape as the sketch previously in this doc — just relocated so it doesn't occupy `Bowler.Create` ahead of the real feature. Lives in `Legacy/` (own file, e.g. `Legacy/LegacyBowlerFactory.cs`, or folded into `Legacy/NewBowler.cs` since it's only used there today) so it's deleted along with the rest of the backdoor at sunset rather than lingering on the aggregate.

`BowlerConfiguration` already has a unique, nulls-distinct index on `LegacyId` (`Database/Configurations/BowlerConfiguration.cs`), so this needs no EF changes — just confirms the sync job's create-vs-update lookup by `LegacyId` is backed by a real constraint.

### New: `Legacy/NewBowler.cs`

Following the plan doc's one-file-per-action shape exactly:

```csharp
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

            jobs.Enqueue<NewBowlerSyncJob>(job => job.SyncAsync(request.BowlerId, CancellationToken.None));

            return Results.Accepted();
        });
    }
}

internal sealed record NewBowlerRequest(int BowlerId);

internal sealed class NewBowlerRequestValidator : AbstractValidator<NewBowlerRequest>
{
    public NewBowlerRequestValidator() => RuleFor(x => x.BowlerId).GreaterThan(0);
}

internal sealed class NewBowlerSyncJob(AppDbContext db, IDbConnection legacyConnection, ILogger<NewBowlerSyncJob> logger)
{
    public async Task SyncAsync(int legacyBowlerId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var row = await legacyConnection.QuerySingleOrDefaultAsync<LegacyBowlerRow>(
            "SELECT Id, FirstName, MiddleInitial, LastName, Suffix, Gender, DateOfBirth FROM Bowlers WHERE Id = @Id",
            new { Id = legacyBowlerId });

        if (row is null)
        {
            logger.LogLegacyBowlerNotFound(legacyBowlerId);
            return;
        }

        var gender = row.Gender switch
        {
            0 => Gender.Male,
            1 => Gender.Female,
            _ => (Gender?)null
        };

        var dateOfBirth = row.DateOfBirth.HasValue
            ? DateOnly.FromDateTime(row.DateOfBirth.Value)
            : (DateOnly?)null;

        var existing = await db.Set<Bowler>().SingleOrDefaultAsync(b => b.LegacyId == legacyBowlerId, ct);

        if (existing is not null)
        {
            // Decided: strictly create-only, not an upsert. A second call for the same LegacyId
            // (Hangfire's automatic retry, or an accidental double-trigger from the Software side)
            // is assumed to be a duplicate of a sync that already succeeded, and is a pure no-op —
            // it does not update the existing bowler's fields. This keeps the job simple (no
            // update method needed on Bowler for this action) and matches the idempotency
            // requirement in the Testing section below without introducing "which fields are
            // safe to overwrite from a legacy row" as a question this action has to answer.
            logger.LogLegacyBowlerAlreadySynced(legacyBowlerId, existing.Id);
            return;
        }

        var suffix = MapSuffix(row.Suffix, legacyBowlerId, logger);

        var bowler = Bowler.CreateFromLegacy(
            row.FirstName,
            row.LastName,
            middleName: row.MiddleInitial,
            suffix: suffix,
            legacyId: row.Id,
            gender: gender,
            dateOfBirth: dateOfBirth);

        if (bowler.IsError)
        {
            logger.LogLegacyBowlerCreateFailed(legacyBowlerId, string.Join("; ", bowler.Errors.Select(e => e.Description)));
            return;
        }

        db.Set<Bowler>().Add(bowler.Value);
        await db.SaveChangesAsync(ct);
    }

    // Legacy Suffix is free text (e.g. "Jr.", "Sr.", "II"); NameSuffix is a closed SmartEnum set
    // whose own Value strings inconsistently carry a trailing period ("Jr.", "Sr." vs. "II", "III").
    // Strip any trailing period from both sides before comparing so "Jr"/"Jr."/"JR." all match
    // NameSuffix.Jr. No match (including a blank/null legacy value) maps to null — logged so an
    // unrecognized suffix is visible rather than silently dropped, but never blocks the sync.
    private static NameSuffix? MapSuffix(string? legacySuffix, int legacyBowlerId, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(legacySuffix))
        {
            return null;
        }

        var normalized = legacySuffix.Trim().TrimEnd('.');

        var match = NameSuffix.List.SingleOrDefault(s =>
            string.Equals(s.Value.TrimEnd('.'), normalized, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            logger.LogLegacySuffixUnmapped(legacyBowlerId, legacySuffix);
        }

        return match;
    }
}

internal sealed record LegacyBowlerRow(int Id, string FirstName, string? MiddleInitial, string LastName, string? Suffix, int Gender, DateTime? DateOfBirth);
```

Notes / open items on this sketch:

- **`Suffix` mapping is now wired in** via the `MapSuffix` helper above — strips a trailing period from both the legacy value and each `NameSuffix.Value` before comparing, so `"Jr"`/`"Jr."`/`"JR."` all resolve to `NameSuffix.Jr`, and `"II"` (no period in the SmartEnum's own value) still matches. An unrecognized value logs a warning and maps to `null` rather than failing the whole sync.
- **Error/not-found/duplicate/unmapped-suffix logging** uses `[LoggerMessage]` source-generated methods, matching `LegacyApiKeyFilterLogMessages`'s pattern in the existing scaffolding (`Legacy/LegacyApiKeyFilter.cs`) — same file, same `internal static partial class {Type}LogMessages` shape, extension methods on `ILogger`:

```csharp
internal static partial class NewBowlerSyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No bowler found in neba-fwk for legacy id {LegacyBowlerId}; skipping sync.")]
    public static partial void LogLegacyBowlerNotFound(this ILogger logger, int legacyBowlerId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Legacy bowler {LegacyBowlerId} already synced as {BowlerId}; treating as a duplicate call and skipping.")]
    public static partial void LogLegacyBowlerAlreadySynced(this ILogger logger, int legacyBowlerId, BowlerId bowlerId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not map legacy suffix '{LegacySuffix}' (bowler {LegacyBowlerId}) to a known NameSuffix; leaving suffix blank.")]
    public static partial void LogLegacySuffixUnmapped(this ILogger logger, int legacyBowlerId, string legacySuffix);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to create bowler from legacy id {LegacyBowlerId}: {Errors}.")]
    public static partial void LogLegacyBowlerCreateFailed(this ILogger logger, int legacyBowlerId, string errors);
}
```

`LogLegacyBowlerCreateFailed`'s call site needs `string.Join(...)` over `bowler.Errors` (e.g. `.Select(e => e.Description)`) when calling it — `[LoggerMessage]` needs a loggable parameter type, not the raw `List<Error>`. No `[PersonalData]`/`[PrivateData]` attributes are needed on any of these — every parameter here is an id or a short structural string (a suffix, an error description), not bowler PII (name/DOB/etc. are never logged).

### Update: `Legacy/LegacyEndpoints.cs`

Add one line to the existing (currently-empty) aggregator:

```csharp
public static void MapLegacyEndpoints(this IEndpointRouteBuilder app)
{
    app.MapNewBowler();
}
```

### Tests

Per `docs/api/software-backdoor-plan.md`'s Testing section, all five layers apply:

1. **Validator unit test** — `NewBowlerRequestValidatorTests`, standard FluentValidation shape.
2. **Endpoint + auth integration test** — `NewBowlerEndpointTests`, `WebApplicationFactory<Program>`, asserts `401`/`400`/`202` + the right job enqueued with the right `bowlerId`, per the plan's mock `IBackgroundJobClient` pattern.
3. **Sync job mapping unit test** — `NewBowlerSyncJobTests`, in-memory/SQLite `AppDbContext` + a fake `IDbConnection` query result, asserting a given `LegacyBowlerRow` produces the right `Bowler.Create(...)` call/result (including the `Gender == -1 → null` and not-found cases).
4. **Legacy query integration test** — new `Testcontainers.MsSql`-backed fixture (`Neba.TestFactory.Infrastructure.MsSqlFixture`, doesn't exist yet — new package `Testcontainers.MsSql`, per the scaffolding doc's "explicitly out of scope" note), seeded with a `Bowlers` table shaped like the real schema, asserting the Dapper query returns the right row for a known id and `null` for an unknown one.
5. **Idempotency integration test** — run `SyncAsync` twice for the same legacy id against a real (Testcontainers) `AppDbContext`, assert the second run doesn't create a duplicate `Bowler` row and leaves the first run's `Bowler` row untouched (per the strictly-create-only decision above, not an upsert).

---

## Software Side (`nebamgmt-v3`)

This section is written so it can become the standalone prompt for wiring up `nebamgmt-v3`, once the website endpoint above actually exists to call.

### Call site 1 — `AddBowlerBO`

File: `Membership/NEBA.Membership.BusinessLogic/Bowlers/AddBowlerBO.cs`, method `AddBowler.Add(BOM.Membership.Bowler bowler)`, inside the existing try block (lines ~111–123):

```csharp
try
{
    var id = DataAccess.Add(bowler);
    SystemGeneratedTasks(bowler, id);
    // NEW: fire-and-forget backdoor call here, after the local commit succeeds.
    return id;
}
catch (BOM.Exceptions.DatabaseCommitException ex) { ... }
```

### Call site 2 — `CheckInRepository`

File: `Data/NEBA.Data/Repositories/Tournaments/CheckInRepository.cs`, private `Add(ICheckIn checkIn, ...)` (lines ~44–79), called from both `CheckIn.Add(IEnumerable<ICheckIn>)` (line ~28) and `CheckIn.Add(int squadId, Bowler bowler, string laneAssignment)` (line ~41).

Two things need to happen here that don't happen today:

1. **Detect that a new bowler was actually created** — only true when `checkIn.Bowler.Id < 0` going in (the synthetic "quick add" temp id convention from `QuickAddBowlerForm`).
2. **Surface the real id back to the caller** — right now `Add(IEnumerable<...>)` returns only `_failedCheckIns`, and `Add(int, Bowler, string)` returns `void`. After `Commit()`, `checkInEntity.Bowler.Id` holds the real, EF-generated id — that needs to reach whichever code fires the backdoor call. Simplest shape: fire the outbound call directly from inside `CheckInRepository` right after `Commit()`, rather than plumbing the id up through `AddCheckInBO`'s public signature (keeps the change smaller and avoids touching two BO method signatures that other callers already depend on).

### New adapter — same failure philosophy as `HttpPostAdapter`, but deliberately NOT copying its threading/timeout behavior

Research into `Adapters/HttpPostAdapter.vb` (the `SmartyStreetsAdapter` precedent cited in `docs/api/software-backdoor-plan.md`) turned up a real hazard worth flagging before this gets built: that adapter sets **no explicit timeout anywhere** — it uses raw `HttpWebRequest`/`WebClient`, so the runtime default applies (`HttpWebRequest.Timeout` = 100s, `ReadWriteTimeout` = 5 min), and the whole call chain from adapter → BO → presenter → WinForms `ButtonSave_Click` is fully **synchronous** with no async dispatch anywhere. If the website is unreachable (true on day one of this deploy, before the endpoint exists — and true again on any future network blip, not just at initial rollout), a call built the same way would freeze the Software's UI thread for up to ~100 seconds on every single bowler-add and check-in submission. (This risk is currently dormant for `SmartyStreetsAdapter` itself — its one real caller, `AddressPresenter.Verify`, is presently short-circuited/commented out — but the new adapter has no such luck; it fires on a live, frequently-hit path.)

So the new adapter keeps `HttpPostAdapter`'s **failure philosophy** (log + non-blocking warning via `SetWarning`/`Errors`, no retry queue, no throw) but deliberately deviates on two points it left undefined:

1. **Explicit short timeout** — a few seconds (exact value TBD, but well under the ~100s default), set directly on whatever HTTP mechanism is used (`HttpClient.Timeout` if this adapter is built on `HttpClient` rather than raw `HttpWebRequest` — worth preferring `HttpClient` here specifically so `Timeout` is a first-class settable property instead of the two separate `HttpWebRequest.Timeout`/`ReadWriteTimeout` knobs).
2. **Fire off the UI thread, don't block the caller's synchronous action** — `Task.Run` (or a genuinely async call awaited from an already-async caller, if one exists at either call site) so `AddBowlerBO.Add(...)` and `CheckInRepository.Add(...)` return to their own callers immediately regardless of how the network call is going. This mirrors the same "never block the user's action on the website's round-trip" principle the architecture plan already applies to the *website* side (enqueue-and-return-202 instead of synchronous processing) — it should apply symmetrically on the Software side of the same call, not just one side of it.

Net effect: a Save/check-in action in the Software completes at its normal local speed regardless of whether the website endpoint exists yet, is slow, or is down — the backdoor call becomes a true side effect the user never waits on, rather than a hidden dependency on `/legacy`'s availability.

**Lifetime hazard from firing off the UI thread — needs explicit handling, not just "wrap in `Task.Run`".** The form instantiates the presenter, which calls the BO, which would call this new adapter — a chain of objects all scoped to that form's lifetime. A `Task.Run` delegate itself is rooted by the thread pool's work queue while it's running, so it will *not* be garbage-collected out from under itself just because the form that triggered it gets disposed — but two related things absolutely can go wrong if this isn't built carefully:

1. **The adapter/`HttpClient` must not be owned by the presenter or form.** If the adapter (or an `HttpClient` it holds) is instantiated per-presenter and gets disposed when the form/presenter is disposed (e.g. in the form's `Dispose(bool)` override, or a `using` further up the chain), a background call still in flight at that moment throws `ObjectDisposedException` on whatever it touches next. Fix: the adapter's `HttpClient` needs a lifetime independent of any single form — a `static readonly HttpClient` (standard .NET guidance anyway, to avoid socket exhaustion from creating one per call) or a singleton resolved from wherever this app does composition, never a field owned by the presenter/BO instance the form constructs.
2. **The `Task.Run` closure must not capture the form, presenter, or any `Control`/`IDisposable` owned by them** — only plain values needed for the call (`bowlerId`, the API key, the base URL as strings/ints). Capturing `this` (the presenter) or a UI control reference means that if anything in the closure ever touches that control after the form is disposed, it throws — and more subtly, it keeps the *whole* form's object graph alive in memory for as long as the background call is pending, which for a slow/hanging call could be the entire timeout window. Passing only primitives in avoids both problems.
3. **Process-exit is still a real gap, and that's expected, not a bug to fix.** `Task.Run` schedules onto a thread-pool thread, which is a background thread (`IsBackground = true`) — if the user closes the whole WinForms app while a sync call is still in flight, .NET does not wait for it; the call is simply abandoned mid-request. This is consistent with the plan's already-accepted stance ("no retry queue on the Software side... a dropped sync is expected to be rare and is a website-side reconciliation problem") — but it's worth stating explicitly here as a known, accepted consequence of moving to fire-and-forget, not something the timeout/threading fix is expected to also solve.

### Open items on the software side

- Confirm `AddCheckInBO`'s current method signatures (`Add`, `AddReservation`) don't need to change at all if the call is fired from `CheckInRepository` directly — need to double check nothing downstream of `CheckInRepository.Add` already assumes it has no side effects beyond the DB write (e.g. is it called inside a larger transaction/rollback path where firing an HTTP call at this point could run even if an outer operation later rolls back?). This matters more now that the call is fire-and-forget on a background thread — a rollback happening *after* the background call has already fired can't be un-sent.
- Confirm per-environment `/legacy` base URL config keys/naming to match existing `App.{Config}.config` conventions.
- Pick the exact timeout value (proposed starting point: 5 seconds — long enough to tolerate normal latency, short enough that even several stacked failures per user session are unnoticeable since none of them block anything).
- Decide whether unhandled exceptions inside the `Task.Run`-dispatched call need explicit `.ContinueWith`/try-catch handling to avoid an unobserved task exception — fire-and-forget from a WinForms app needs this swallowed deliberately (logged, not silently lost, but also never rethrown onto a finalizer thread).

### Prompt for the `nebamgmt-v3` implementation

Everything above, condensed into a standalone prompt — self-contained, so it can be pasted to an agent working directly in `nebamgmt-v3` with no access to this conversation or this file:

> You're working in `nebamgmt-v3` (WinForms, .NET Framework), the legacy management application for NEBA. The website (a separate, modern repo) is building a set of temporary "backdoor" sync endpoints under `/legacy` that this application needs to call after certain local actions, so the website's database can mirror what happens here. This task wires up the first one: notifying the website whenever a brand-new bowler is created.
>
> **Goal**: after a new `Bowler` row is successfully committed to this app's own database, fire an HTTP `POST` to the website's `/legacy/bowlers/new` endpoint with body `{ "bowlerId": <the new bowler's int Id> }` and header `X-Api-Key: <configured key>`. Do this at both places a new bowler can be created:
>
> 1. **`Membership/NEBA.Membership.BusinessLogic/Bowlers/AddBowlerBO.cs`**, method `AddBowler.Add(BOM.Membership.Bowler bowler)`. Inside the existing `try` block, right after `var id = DataAccess.Add(bowler);` succeeds (and after `SystemGeneratedTasks(bowler, id)`), fire the call with `id`.
> 2. **`Data/NEBA.Data/Repositories/Tournaments/CheckInRepository.cs`**, the private `Add(ICheckIn checkIn, ...)` method (called from both `CheckIn.Add(IEnumerable<ICheckIn>)` and `CheckIn.Add(int squadId, Bowler bowler, string laneAssignment)`). This path only creates a new bowler when the incoming `checkIn.Bowler.Id` was negative going in (this app's convention for a not-yet-persisted "quick add" bowler, from `QuickAddBowlerForm`) — after `Commit()`, EF populates the real generated id onto `checkInEntity.Bowler.Id`. Fire the call with that id, but **only** when the pre-commit id was negative (an existing bowler reused on a check-in must not re-trigger this).
>
> **New adapter** — add a new class parallel to the existing `Adapters/HttpPostAdapter.vb`/`SmartyStreetsAdapter` pattern (same directory/layer), but do **not** copy its threading or timeout behavior — that adapter has no explicit timeout (defaults to ~100s via raw `HttpWebRequest`) and calls synchronously all the way up to the UI thread, which is fine for its own one-off, currently-dormant use but wrong here because this fires on every bowler add and every check-in submission, a live and frequent path. Build the new adapter instead as:
>
> - Uses `HttpClient`, not raw `HttpWebRequest`, specifically so an explicit `Timeout` is a first-class property. Set it to a few seconds (start with 5s) — short enough that even a fully unreachable website (true, for instance, on day one of this rollout before the endpoint exists yet) never meaningfully delays the user.
> - The `HttpClient` instance itself must have a lifetime independent of any single form/presenter — a `static readonly HttpClient`, not one constructed per-adapter-instance and disposed when the calling form closes.
> - The actual call is dispatched via `Task.Run(...)` (or better, a real `async`/non-blocking path if one is reachable from the call site) so `AddBowlerBO.Add(...)` and the `CheckInRepository.Add(...)` methods return to their own callers immediately, never waiting on the network round-trip. The `Task.Run` closure must capture **only plain values** needed for the call (the bowler id, the api key, the base URL) — never `this`, the presenter, the form, or any `Control`/`IDisposable` owned by them, since those can be disposed by the time the background call runs.
> - On failure (timeout, non-2xx, network error): log it and, matching the existing `HttpPostAdapter`/`SmartyStreetsAdapter` philosophy, surface a **non-blocking** warning through this app's existing `SetWarning`/`Errors` mechanism if there's a natural place to route it to — but this must never throw back up into the caller's normal flow, and must never block. No retry loop, no queue — a dropped sync call is expected to be rare and is handled by the website side, not here.
> - Wrap the `Task.Run` body so an unhandled exception inside it is caught and logged, not left to become an unobserved task exception.
> - Base URL and API key come from this app's per-environment config (`App.{Config}.config`), matching how other environment-specific settings are already wired — pick config key names consistent with the existing convention in that file.
>
> **Before you start, resolve these open questions** (don't guess silently — ask, or make the decision explicit in a comment/commit message with your reasoning):
>
> 1. Does `CheckInRepository.Add(...)` run inside any wider transaction/rollback scope where firing this HTTP call immediately after `Commit()` could still end up "sent" even if something later in the same user operation rolls back? If so, where is the actually-safe point to fire from?
> 2. Confirm the exact per-environment config key names to use for the base URL and API key.
> 3. Confirm 5 seconds is an acceptable timeout value, or adjust.
>
> Do not change `AddCheckInBO`'s public method signatures (`Add`, `AddReservation`) — fire the call from inside `CheckInRepository` directly so other callers of those BO methods are unaffected.

---

## Summary of what's still undecided

1. ~~Exact `NameSuffix` mapping from the legacy free-text `Suffix` column.~~ **Decided** — strip a trailing period from both sides, case-insensitive match against `NameSuffix.List`; no match logs a warning and maps to `null` (see `MapSuffix` above).
2. ~~Whether `NewBowlerSyncJob` should update an existing bowler on a repeat call, or strictly no-op.~~ **Decided** — strictly no-op: a `LegacyId` match is assumed to be a duplicate/retry of an already-successful sync and is logged and skipped, never updated. `Bowler` does not need an update method for this action.
3. **Real `neba-fwk` table/column names** — not independently verified against the actual database or `.ssdl`; this plan's Dapper query is written against the EF POCO's property names as a best guess. **Could not confirm this from within this session — whoever implements this needs to check the real schema (or `.ssdl`) directly before trusting the query in this doc**, since database-first scaffolding *usually* mirrors real column names but isn't guaranteed to.
4. **Whether firing the backdoor call from inside `CheckInRepository.Add` is safe with respect to any surrounding transaction scope in `nebamgmt-v3`** — not independently verified; this plan assumes firing immediately after `Commit()` is safe (no wrapping transaction that could later roll back), but that assumption was not traced through the full call stack. **Could not confirm this from within this session — flagged explicitly in the implementation prompt above as something the implementer must check before writing the call site, not something to assume from this plan alone.**
