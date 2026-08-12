# New Tournament Backdoor — Implementation Plan

Concrete plan for the third `/legacy` action, built on the scaffolding in `docs/plans/software-backdoor-scaffolding.md`, the standing decisions in `docs/api/software-backdoor-plan.md`, and the already-implemented precedents in `src/Neba.Api/Legacy/Bowlers/NewBowler.cs` and `src/Neba.Api/Legacy/Bowlers/UpdateBowler.cs`. This document is a **plan only** — nothing here has been applied to either repo yet.

## Shape of this action — different from the bowler actions

The bowler actions both *create or update* a website record from a legacy row. This action does neither: the website's `Tournament` already exists (created earlier through the normal website flow, months before the Software-side tournament exists). What this action does is **link** the two records — find the matching website `Tournament` and stamp it with the Software's id, so future actions (games, squads, results — not yet planned) have something to key off of.

This changes the shape of the sync job: there is no `CreateFromLegacy`/`ApplyLegacyUpdate` mapping of descriptive fields. Instead the job is a **matching problem** — given a legacy tournament id, find the one website `Tournament` it corresponds to, using only data that already exists on both sides.

---

## Decision Recap

- **Two Software call sites, one website route, trigger-only payload.** Research into `nebamgmt-v3` found tournament creation funnels through exactly two BO entry points — `AddSinglesTournamentBO.Add` (singles) and `AddTeamTournamentBO.Add` (doubles/trios/baker/team) — both of which call down to the same single physical insert point (`TournamentRepository.Add`, confirmed as the only call site of `NebaEntities.Tournaments.Add(...)` in the whole solution). Per the architecture doc's "trigger, not data" rule, the payload is just `{ "tournamentId": <id> }` regardless of which BO fired it — the website re-derives everything it needs (name, dates, singles/team, format) by querying `neba-fwk` itself, so there's no reason for the two call sites to send different shapes. One route: `POST /legacy/tournaments/new`.
- **`Tournament.LegacyId` already exists** (`src/Neba.Api/Features/Tournaments/Domain/Tournament.cs`) — unlike `Bowler`, which needed the column added from scratch. It's `{ get; init; }` today. Same mechanical wrinkle as `Bowler.ApplyLegacyUpdate` hit: an extension member can't assign an `init`-only property on an already-constructed instance. This plan widens `LegacyId` to `{ get; internal set; }` — a real, permanent change to `Tournament.cs` (not deleted at sunset), matching the same reasoning already applied to `Bowler.Name`/`Gender`/`DateOfBirth`: the aggregate's own eventual first-class "link to Software" or general `Update` operation will need this mutability too.
- **Matching walks up to three narrowing steps, per the user's explicit description, stopping as soon as exactly one candidate remains:**
  1. Website tournaments with `EndDate == <legacy End>` and `LegacyId == null` (unclaimed — see idempotency below).
  2. If step 1 leaves more than one candidate: narrow by singles-vs-team, using `TournamentType.TeamSize` (`1` = singles-shaped, `>1` = team-shaped) against whether the legacy row has a `Tournaments_SinglesTournament` or `Tournaments_TeamTournament` subtype row.
  3. If step 2 still leaves more than one candidate: narrow by exact `TournamentType`, mapping the legacy row's `SinglesTournamentTypes` value (singles) or `TeamSize`/`IsBaker`/`OverUnder` combination (team) to the website's `TournamentType` SmartEnum (see mapping table below).
- **Any step that can't resolve to exactly one candidate — zero or still-multiple — is "cannot be derived," not just the multiple-candidates case the user described.** The user's description focuses on the ambiguous (>1) case, but a zero-candidate outcome (no website tournament has that end date at all, or the exact-type step eliminates every remaining candidate) needs the identical remedy: nothing gets linked automatically, so it's just as much a case for the "log + email" path. Both outcomes are handled by the same fallback in the job, not two separate code paths.
- **Idempotency: strict no-op, matching `NewBowlerSyncJob`'s stance, not `UpdateBowlerSyncJob`'s.** A repeat call for a legacy tournament id that's already linked (`Tournament.LegacyId == legacyTournamentId` already, found via a lookup before the matching logic runs at all) is a pure no-op — nothing about "which website tournament does this legacy id belong to" can legitimately change between calls, unlike `UpdateBowler` where re-applying edited fields every time is the entire point of the action.
- **No fallback-to-create.** Unlike `UpdateBowlerSyncJob`'s "no existing record → create one" behavior, there is no equivalent here — a website `Tournament` with no match is exactly the "cannot be derived" / manual-intervention case, never a signal to synthesize a new website tournament from a Software-only source. The website tournament is always expected to already exist; if it doesn't, that is itself the anomaly to escalate, not a race to tolerate.
- **Legacy → website `TournamentType` mapping is best-effort where the schema doesn't disambiguate outright — flagged, not guessed silently.** See "Open items" below for the two spots (legacy `Champions`, and the `OverUnder` doubles age-threshold) where the mapping can't be verified from source alone.

---

## Legacy Schema Reference (`neba-fwk`)

Confirmed via `nebamgmt-v3`'s EF6 SSDL/entity classes during this session's research — **table-per-type (TPT)**, not a single flat table: `Tournaments` is the base row, and exactly one of `Tournaments_SinglesTournament` / `Tournaments_TeamTournament` has a matching row (same `Id`, no separate FK column — the subtype table's `Id` *is* the `Tournaments.Id`).

### `Tournaments` (base table)

| Column | Type | In scope for this action? |
|---|---|---|
| `Id` | `int`, identity, PK | yes — the sync key |
| `Name` | `nvarchar(52)` | no — not needed for matching, and the website already has its own `Name` |
| `BowlingCenterId` | `int` | no |
| `Start` | `datetime` | no — matching is on end date only, per the user's explicit instruction |
| `End` | `datetime` | **yes — the sole matching key at step 1** |
| `EntryFee`, `GamesPerSquad`, `OilPattern_*`, `CutRatio`, `FinalsRatio`, `LiveScoringUrl`, `HighGameCreditRatio`, `EntryPoints`, `YearlyStatEligible`, `Completed`, `Audit_*` | mixed | no |

### `Tournaments_SinglesTournament` (1:1 subtype, PK/FK = `Tournaments.Id`)

| Column | Type | In scope? |
|---|---|---|
| `Id` | `int` (shared with `Tournaments.Id`) | yes — presence of a row here means "this is a singles tournament" |
| `TournamentType` | `int` (`SinglesTournamentTypes` enum) | yes — step 3 exact-type mapping |
| `FinalsFormat` | `int` (`SinglesTournamentFinalsFormats` enum) | no |

`SinglesTournamentTypes` (`BOM/NEBA.BOM/Tournaments/Singles/SinglesTournamentTypes.cs`): `Standard = 0`, `NonChampions = 1`, `Senior = 2`, `Women = 3`, `Champions = 4`, `Invitational = 5`, `Masters = 6`, `Youth = 7`, `SeniorWithWomen = 8`.

### `Tournaments_TeamTournament` (1:1 subtype, PK/FK = `Tournaments.Id`)

| Column | Type | In scope? |
|---|---|---|
| `Id` | `int` (shared with `Tournaments.Id`) | yes — presence of a row here means "this is a team tournament" |
| `TeamSize` | `int` | yes — step 3 exact-type mapping |
| `OverUnder` | `bit` | yes — step 3 exact-type mapping |
| `FinalsFormat` | `int` (`TeamFinalsFormats` enum) | no |
| `IsBaker` | `bit` | yes — step 3 exact-type mapping |

**Open item, same caveat every prior plan in this series has carried**: these are EF POCO property names, not independently re-verified against the real `.ssdl`/database this session. `IsBaker`'s exact column mapping inside the EDMX's `EntityTypeMapping` block specifically wasn't captured verbatim during research (it's confirmed to exist on the `Data.TeamTournament` C# class, `Data/NEBA.Data/TeamTournament.cs:27`, but the SSDL column list snippet didn't itemize it) — confirm the literal column name before writing the Dapper query.

### Legacy → website `TournamentType` mapping

| Legacy source | Website `TournamentType` | Confidence |
|---|---|---|
| Singles `Standard` | `Singles` | confirmed |
| Singles `NonChampions` | `NonChampions` | confirmed |
| Singles `Senior` | `Senior` | confirmed |
| Singles `Women` | `Women` | confirmed |
| Singles `Invitational` | `Invitational` | confirmed |
| Singles `Masters` | `Masters` | confirmed |
| Singles `Youth` | `Youth` | confirmed |
| Singles `SeniorWithWomen` | `SeniorAndWomen` | confirmed |
| Singles `Champions` | `TournamentOfChampions` | **unverified — see Open items** |
| Team, `TeamSize == 2`, `IsBaker == false`, `OverUnder == false` | `Doubles` | confirmed |
| Team, `TeamSize == 3`, `IsBaker == false` | `Trios` | confirmed |
| Team, `IsBaker == true` | `Baker` | confirmed |
| Team, `TeamSize == 2`, `OverUnder == true` | `OverUnderFiftyDoubles` (not `OverUnderFortyDoubles`) | **unverified — see Open items** |
| Team, any other `TeamSize`/flag combination | *(no mapping — treated as "type unknown," see job logic below)* | n/a |

Website `TournamentType` values with **no legacy source found at all** (`HighRoller`, `OverForty`, `FortyToFortyNine`, `Eliminator`) — these simply never win the step-3 exact-type filter, since no legacy row can ever map to them. Not an error condition; just means a website tournament of one of these types can only ever be matched at step 1 or step 2, never disambiguated further by type. All four are also `ActiveFormat: false` on the website side (no longer offered), consistent with there being no live Software-side path that creates them.

---

## Website Side (`src/Neba.Api`)

### Change: `Tournament.cs` — widen `LegacyId` to `internal set`

```csharp
public int? LegacyId { get; internal set; }
```

`internal` (not `private`) so `Tournament.ApplyLegacyId` — an extension member in `Legacy/Tournaments/`, same assembly — can assign it directly. No other property on `Tournament` changes; this action never touches `Name`, dates, `TournamentType`, or anything else describing the tournament — those already exist on the website's own record and are not overwritten from the Software side.

### New: `Legacy/Tournaments/NewTournament.cs`

Following `NewBowler.cs`'s exact shape — endpoint, request, validator, sync job, row DTO, extension member, and log messages all in one file:

```csharp
using System.Data;

using Dapper;

using ErrorOr;

using FluentValidation;

using Hangfire;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Email;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Identity;
using Neba.Api.Legacy.Tournaments.Emails;

namespace Neba.Api.Legacy.Tournaments;

internal static class NewTournamentEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapNewTournament()
        {
            app.MapPost("/tournaments/new", (
                NewTournamentRequest request,
                IValidator<NewTournamentRequest> validator,
                IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                jobs.Enqueue<NewTournamentSyncJob>(job => job.SyncAsync(request.TournamentId, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}

internal sealed record NewTournamentRequest(int TournamentId);

internal sealed class NewTournamentRequestValidator
    : AbstractValidator<NewTournamentRequest>
{
    public NewTournamentRequestValidator()
    {
        RuleFor(request => request.TournamentId)
            .GreaterThan(0);
    }
}

internal static class LegacyTournamentLinkExtensions
{
    extension(Tournament tournament)
    {
        public void ApplyLegacyId(int legacyTournamentId) => tournament.LegacyId = legacyTournamentId;
    }
}

internal sealed class NewTournamentSyncJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    IEmailSender emailSender,
    ILogger<NewTournamentSyncJob> logger)
{
    public async Task SyncAsync(int legacyTournamentId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

        var alreadyLinked = await db.Set<Tournament>()
            .AnyAsync(t => t.LegacyId == legacyTournamentId, ct);
        if (alreadyLinked)
        {
            logger.LogLegacyTournamentAlreadyLinked(legacyTournamentId);
            return;
        }

        // See NewBowlerSyncJob.SyncAsync for the rationale on suppressing DAP005 here.
#pragma warning disable DAP005
        var row = await legacyConnection.QuerySingleOrDefaultAsync<LegacyTournamentRow>(
            """
            SELECT
                t.Id,
                t.End,
                s.TournamentType AS SinglesTournamentType,
                tm.TeamSize AS TeamSize,
                tm.IsBaker AS IsBaker,
                tm.OverUnder AS OverUnder
            FROM
                Tournaments t
            LEFT JOIN Tournaments_SinglesTournament s ON s.Id = t.Id
            LEFT JOIN Tournaments_TeamTournament tm ON tm.Id = t.Id
            WHERE
                t.Id = @Id
            """, new
            {
                Id = legacyTournamentId
            }
        );
#pragma warning restore DAP005

        if (row is null)
        {
            logger.LogLegacyTournamentNotFound(legacyTournamentId);
            return;
        }

        var endDate = DateOnly.FromDateTime(row.End);

        var candidates = await db.Set<Tournament>()
            .Where(t => t.EndDate == endDate && t.LegacyId == null)
            .ToListAsync(ct);

        var isTeam = row.TeamSize.HasValue;

        if (candidates.Count > 1)
        {
            candidates = isTeam
                ? candidates.Where(t => t.TournamentType.TeamSize > 1).ToList()
                : candidates.Where(t => t.TournamentType.TeamSize == 1).ToList();
        }

        if (candidates.Count > 1)
        {
            var mappedType = MapLegacyTournamentType(row);
            if (mappedType is not null)
            {
                candidates = candidates.Where(t => t.TournamentType == mappedType).ToList();
            }
        }

        if (candidates.Count != 1)
        {
            logger.LogLegacyTournamentCannotBeDerived(legacyTournamentId, candidates.Count);

            await emailSender.SendAsync(new EmailMessage
            {
                To = "website@bowlneba.com",
                Subject = "Manual intervention needed: tournament link",
                HtmlBody = new TournamentLinkCannotBeDerivedEmail(legacyTournamentId, endDate, candidates.Count).ToHtmlBody()
            }, ct);

            return;
        }

        candidates[0].ApplyLegacyId(legacyTournamentId);
        await db.SaveChangesAsync(ct);
    }

    // Maps the legacy row's singles-type enum or team shape to the website's TournamentType.
    // Returns null when the legacy row can't be confidently mapped to an exact type (an unrecognized
    // SinglesTournamentTypes value, or a team combination with no website equivalent) - callers treat
    // a null mapping as "type unknown," meaning the exact-type narrowing step is skipped rather than
    // incorrectly eliminating every remaining candidate.
    private static TournamentType? MapLegacyTournamentType(LegacyTournamentRow row)
    {
        if (row.SinglesTournamentType.HasValue)
        {
            return row.SinglesTournamentType.Value switch
            {
                0 => TournamentType.Singles,           // Standard
                1 => TournamentType.NonChampions,
                2 => TournamentType.Senior,
                3 => TournamentType.Women,
                4 => TournamentType.TournamentOfChampions, // Champions - see Open items
                5 => TournamentType.Invitational,
                6 => TournamentType.Masters,
                7 => TournamentType.Youth,
                8 => TournamentType.SeniorAndWomen,     // SeniorWithWomen
                _ => null
            };
        }

        if (!row.TeamSize.HasValue)
        {
            return null;
        }

        if (row.IsBaker == true)
        {
            return TournamentType.Baker;
        }

        return (row.TeamSize.Value, row.OverUnder) switch
        {
            (2, true) => TournamentType.OverUnderFiftyDoubles, // see Open items - Forty variant unreachable
            (2, false or null) => TournamentType.Doubles,
            (3, false or null) => TournamentType.Trios,
            _ => null
        };
    }
}

internal sealed record LegacyTournamentRow(
    int Id,
    DateTime End,
    int? SinglesTournamentType,
    int? TeamSize,
    bool? IsBaker,
    bool? OverUnder);

internal static partial class NewTournamentSyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Legacy tournament {LegacyTournamentId} is already linked; skipping.")]
    public static partial void LogLegacyTournamentAlreadyLinked(this ILogger<NewTournamentSyncJob> logger, int legacyTournamentId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No tournament found in neba-fwk for legacy id {LegacyTournamentId}; skipping link sync.")]
    public static partial void LogLegacyTournamentNotFound(this ILogger<NewTournamentSyncJob> logger, int legacyTournamentId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not derive a unique website tournament for legacy id {LegacyTournamentId} ({CandidateCount} candidate(s) remaining after narrowing); manual intervention email sent.")]
    public static partial void LogLegacyTournamentCannotBeDerived(this ILogger<NewTournamentSyncJob> logger, int legacyTournamentId, int candidateCount);
}
```

No `[PersonalData]`/`[PrivateData]` needed on any logged parameter — every value logged is an id, a date, or a count, never anything describing a person.

### New: `Legacy/Tournaments/Emails/TournamentLinkCannotBeDerivedEmail.cs`

Following the `{Feature}/Emails/{Name}Email.cs` pattern from CLAUDE.md — `internal sealed class`, `EmailLayout.Wrap(...)`:

```csharp
using System.Net;

namespace Neba.Api.Legacy.Tournaments.Emails;

internal sealed class TournamentLinkCannotBeDerivedEmail(int legacyTournamentId, DateOnly endDate, int candidateCount)
{
    public string ToHtmlBody()
    {
        var body = $"""
            <p>Tournament with legacy id <strong>{WebUtility.HtmlEncode(legacyTournamentId.ToString())}</strong> cannot be derived and needs manual intervention.</p>
            <p>End date: {WebUtility.HtmlEncode(endDate.ToString("yyyy-MM-dd"))}<br/>
            Candidate website tournaments remaining after narrowing: {WebUtility.HtmlEncode(candidateCount.ToString())}</p>
            <p>Use the tournament's <code>LegacyId</code> column to link it manually once the correct match is identified.</p>
            """;

        return EmailLayout.Wrap(body);
    }
}
```

Kept under `Legacy/Tournaments/Emails/` (not `Features/Tournaments/Emails/`) so it's deleted along with the rest of `Legacy/` at sunset — this email exists solely to support the backdoor's own failure mode, not a permanent product notification.

### Update: `Legacy/LegacyEndpoints.cs`

```csharp
extension(IEndpointRouteBuilder app)
{
    public void MapLegacyEndpoints()
    {
        app.MapNewBowler();
        app.MapUpdateBowler();
        app.MapNewTournament();
    }
}
```

### Tests

Per the architecture doc's five testing layers, collapsed into one file: `tests/Neba.Api.Tests/Legacy/Tournaments/NewTournamentTests.cs`, mirroring `NewBowlerTests.cs`/`UpdateBowlerTests.cs`'s multi-class-single-file shape (`NewTournamentRequestValidatorTests`, `NewTournamentEndpointTests`, `LegacyTournamentTypeMappingTests`, `NewTournamentSyncJobTests`).

1. **`NewTournamentRequestValidatorTests`** — same shape as the bowler actions: `TournamentId > 0`.
2. **`NewTournamentEndpointTests`** — same shape as the bowler actions: `401` (filter wired, not re-asserted beyond that), `400` (invalid `TournamentId`), `202` + right job/args enqueued via `Mock<IBackgroundJobClient>(MockBehavior.Strict)`.
3. **`LegacyTournamentTypeMappingTests`** (unit, exercising `NewTournamentSyncJob.MapLegacyTournamentType` — make it `internal static`, not `private static`, with `[assembly: InternalsVisibleTo]` already covering the test project per the existing convention, so it's directly testable without going through the full job): `Theory` cases for every row in the mapping table above (all 9 singles values, `Doubles`/`Trios`/`Baker`/`OverUnderFiftyDoubles` team combinations), plus explicit cases proving an unrecognized singles value and an unmapped team combination (e.g. `TeamSize == 4`) both return `null`.
4. **`NewTournamentSyncJobTests`** (integration) — same Postgres-temp-table-as-legacy-connection pattern as `NewBowlerSyncJobTests`/`UpdateBowlerSyncJobTests`, with **two** temp tables this time (`Tournaments` and a combined stand-in — see note below) to model the TPT join. Covers:
   - Already-linked (`Tournament.LegacyId == legacyTournamentId` pre-seeded) → no-op, info logged, no email sent (`Mock<IEmailSender>(MockBehavior.Strict)` with no setups — `VerifyNoOtherCalls()`).
   - Not-found in `neba-fwk` → no link applied, warning logged, no email sent.
   - Exactly one website `Tournament` with matching `EndDate` and `LegacyId == null` → linked directly (step 1 alone resolves it), regardless of type — seed a legacy singles row and a website tournament of a *different* type at the same end date to prove step 1 short-circuits before the type checks ever run.
   - Two website tournaments share the `EndDate`, one singles-shaped (`TournamentType.TeamSize == 1`) and one team-shaped — legacy row is a team row → the singles-shaped one is correctly excluded, team-shaped one is linked (step 2 resolves it).
   - Two website tournaments share the `EndDate` and are both team-shaped (e.g. `Doubles` and `Trios`) — legacy row is `TeamSize == 3`, `IsBaker == false` → `Trios` one is linked (step 3 resolves it).
   - Two website tournaments share the `EndDate`, same singles-vs-team bucket, and the legacy row's type maps to neither (or maps to `null`, e.g. `TeamSize == 4`) → still ambiguous after all three steps → warning logged, email sent with the right `To`/`Subject`, no `LegacyId` assigned to either candidate.
   - Zero website tournamts share the legacy row's `EndDate` at all → same cannot-derive path (warning + email), not a silent no-op — this is the "0, not just >1" case called out in the Decision Recap.
   - A website tournament already has a *different* non-null `LegacyId` at the same `EndDate` → correctly excluded from candidates by the `LegacyId == null` filter (proves an already-claimed tournament is never re-claimed by a second legacy id).

   **Temp-table note**: `NewBowlerSyncJobTests`' single `CREATE TEMP TABLE Bowlers (...)` doesn't have an equivalent multi-table join to model. For this job, either (a) create three temp tables (`Tournaments`, `Tournaments_SinglesTournament`, `Tournaments_TeamTournament`) and `LEFT JOIN` them exactly as production does, or (b) if that proves awkward with Postgres temp-table scoping across the test's helper methods, create one flattened temp table with all the columns the production query's `SELECT` list produces and adjust the query text conditionally per test setup. Prefer (a) — it exercises the actual join logic, not just the row-shape the query happens to produce, and stays consistent with the "schema-real, not mocked" rationale in the architecture doc's testing section.

---

## Software Side (`nebamgmt-v3`)

### Call site 1 — `AddSinglesTournamentBO.Add`

File: `Tournaments/NEBA.Tournaments.BusinessLogic/Singles/AddSinglesTournamentBO.cs`, method `AddSingles.Add(BOM.Tournaments.Singles.Tournament tournament)` (line ~28), right after the id-returning call succeeds:

```csharp
public int Add(BOM.Tournaments.Singles.Tournament tournament)
{
    var id = _addTournament.Add(tournament);
    Errors = _addTournament.Errors;

    if (id > 0 && !Errors.Any())
    {
        // NEW: fire-and-forget backdoor call here, after the local commit succeeds.
        NEBA.Common.Adapters.WebsiteSyncAdapter.NotifyNewTournament(id);
    }

    return id;
}
```

### Call site 2 — `AddTeamTournamentBO.Add`

File: `Tournaments/NEBA.Tournaments.BusinessLogic/Team/AddTeamTournamentBO.cs`, method `AddTeam.Add(BOM.Tournaments.Team.Tournament tournament)` (line ~27), same shape:

```csharp
public int Add(BOM.Tournaments.Team.Tournament tournament)
{
    var id = _addTournament.Add(tournament);
    Errors = _addTournament.Errors;

    if (id > 0 && !Errors.Any())
    {
        NEBA.Common.Adapters.WebsiteSyncAdapter.NotifyNewTournament(id);
    }

    return id;
}
```

Both call sites reuse the **same** adapter method with the **same** payload shape (`{ "tournamentId": id }`) — per the "trigger, not data" rule, the adapter doesn't need to know or send whether the tournament is singles or team; the website re-derives that itself from `neba-fwk`.

Every UI path that creates a tournament (the singles Add form, and all four team forms — Doubles, Trios, Baker, and the general Team form with an arbitrary `TeamSize`) funnels through one of these two BO methods, confirmed during research by tracing all four team forms to the same `Presenters.Team.Add` → `AddTeamTournamentPresenter` → `AddTeam.Add` chain, and confirming `NebaEntities.Tournaments.Add(...)` is called from exactly one place in the entire solution (`TournamentRepository.cs:43`). No copy/clone/import path exists for tournaments.

### Reuse the existing `WebsiteSyncAdapter` — new sibling method, no new adapter

`Common/NEBA.Common/Adapters/WebsiteSyncAdapter.cs` already has `NotifyNewBowler` and `NotifyBowlerUpdated`, both thin wrappers around a shared private `Send(url, apiKey, jsonBody)` helper. Add a third sibling:

```csharp
public static void NotifyNewTournament(int tournamentId)
{
    var baseUrl = ConfigurationManager.AppSettings["WebsiteSyncUrl"];
    var apiKey = ConfigurationManager.AppSettings["WebsiteSyncApiKey"].Decrypt();

    if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
        return;

    Send(baseUrl.TrimEnd('/') + "/legacy/tournaments/new", apiKey, $"{{\"tournamentId\":{tournamentId}}}");
}
```

Same config keys, same shared static `HttpClient` (5-second timeout), same fire-and-forget `Task.Run` dispatch, same non-blocking failure logging — no new adapter, no new config keys, no new threading decision.

### Open items on the software side

- Same open item every prior plan in this series has carried and never fully closed: do `AddSinglesTournamentBO.Add`/`AddTeamTournamentBO.Add`'s commit points run inside any wider transaction/rollback scope where firing the HTTP call immediately after the local commit could still end up "sent" even if something later in the same user operation rolls back? Given `TournamentRepository.Add` does *two* separate `Commit()` calls (tournament row, then squads), it's also worth confirming the hook fires only after **both** commits succeed, not just the first — placing the hook at the BO level (after `_addTournament.Add(...)` returns) rather than inside `TournamentRepository.Add` itself already achieves this, since `_addTournament.Add(...)` doesn't return until `TournamentRepository.Add` has finished both commits, but this wasn't independently traced line-by-line through `BaseAdd.vb`'s exact return timing this session.
- The `IsBaker` column's literal name/mapping inside `Tournaments_TeamTournament` wasn't independently confirmed against the EDMX's `EntityTypeMapping` block (see Legacy Schema Reference above) — confirm before finalizing the Dapper query.

### Prompt for the `nebamgmt-v3` implementation

Everything above, condensed into a standalone prompt — self-contained, so it can be pasted to an agent working directly in `nebamgmt-v3` with no access to this conversation or this file:

> You're working in `nebamgmt-v3` (WinForms, .NET Framework), the legacy management application for NEBA. A previous change already added a "backdoor" sync mechanism that notifies a separate website whenever certain local actions happen, via static methods on `Common/NEBA.Common/Adapters/WebsiteSyncAdapter.cs` (`NotifyNewBowler(int bowlerId)`, `NotifyBowlerUpdated(int bowlerId)`), each a thin wrapper around a shared private `Send(url, apiKey, jsonBody)` helper. This task adds a third event to that same mechanism: notifying the website whenever a **new tournament** is created (singles or team).
>
> **Step 1 — add a new method to the existing `WebsiteSyncAdapter`** (do not create a new adapter, do not add a second `HttpClient`):
>
> ```csharp
> public static void NotifyNewTournament(int tournamentId)
> {
>     var baseUrl = ConfigurationManager.AppSettings["WebsiteSyncUrl"];
>     var apiKey = ConfigurationManager.AppSettings["WebsiteSyncApiKey"].Decrypt();
>
>     if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
>         return;
>
>     Send(baseUrl.TrimEnd('/') + "/legacy/tournaments/new", apiKey, $"{{\"tournamentId\":{tournamentId}}}");
> }
> ```
>
> **Step 2 — wire the call into both places a new tournament is created:**
>
> 1. **`Tournaments/NEBA.Tournaments.BusinessLogic/Singles/AddSinglesTournamentBO.cs`**, class `AddSingles`, method `Add(BOM.Tournaments.Singles.Tournament tournament)`. Right after `var id = _addTournament.Add(tournament);` and its `Errors = _addTournament.Errors;` line, guard on `id > 0 && !Errors.Any()` (a failed add returns `0` with populated `Errors` — do not notify on failure) and call `NEBA.Common.Adapters.WebsiteSyncAdapter.NotifyNewTournament(id);`.
> 2. **`Tournaments/NEBA.Tournaments.BusinessLogic/Team/AddTeamTournamentBO.cs`**, class `AddTeam`, method `Add(BOM.Tournaments.Team.Tournament tournament)`. Identical shape — same guard, same call.
>
> Both call sites pass only the tournament's own new id — do not send tournament type, dates, or any other field in the JSON body. The website looks all of that up itself from its own copy of the database once it receives the id; this mirrors the existing `NotifyNewBowler`/`NotifyBowlerUpdated` pattern exactly.
>
> **Do not** add a hook anywhere else — every tournament-creation UI path (the singles Add form, and all Doubles/Trios/Baker/general-Team forms) already funnels through one of these two `Add` methods; there is no copy/clone/import path for tournaments to worry about.
>
> **Before you start, resolve these open questions** (don't guess silently — ask, or make the decision explicit in a comment/commit message with your reasoning):
>
> 1. Do `AddSinglesTournamentBO.Add`/`AddTeamTournamentBO.Add` run inside any wider transaction/rollback scope where firing the HTTP call immediately after the local commit could still end up "sent" even if something later in the same user operation rolls back?
> 2. Confirm the base URL/API key config keys already added for `NotifyNewBowler`/`NotifyBowlerUpdated` are what `NotifyNewTournament` should reuse (they should be — verify, don't assume).
>
> Do not change `AddSinglesTournamentBO.Add`'s or `AddTeamTournamentBO.Add`'s public method signatures — the notification is a side effect fired from inside the existing method body, not a new parameter/return value any caller needs to know about.

---

## Summary of what's still undecided

1. ~~Whether this action should update an existing website tournament's descriptive fields (name, dates, etc.) the way `UpdateBowlerSyncJob` does, or purely link ids.~~ **Decided** — purely a link. The website's own tournament data is the source of truth for everything describing the tournament; the Software side only ever contributes its own id.
2. ~~What happens when the matching narrows to zero candidates rather than the user's described "multiple candidates" case.~~ **Decided** — treated identically to the multiple-candidates cannot-derive case: logged and emailed, no link applied.
3. ~~Whether a repeat call for an already-linked legacy tournament id should re-run the matching logic.~~ **Decided** — strict no-op, checked before any matching logic runs, mirroring `NewBowlerSyncJob`'s create-only idempotency stance rather than `UpdateBowlerSyncJob`'s always-reapply stance.
4. ~~How `Tournament.LegacyId` should be mutated given it's `init`-only today.~~ **Decided** — widen to `internal set` (a real, permanent aggregate change, not deleted at sunset), matching the same reasoning already applied to `Bowler.Name`/`Gender`/`DateOfBirth`.
5. **Legacy `SinglesTournamentTypes.Champions` → website `TournamentType.TournamentOfChampions` mapping** — plausible given the naming and that both represent "past title winners only" restrictions, but not independently confirmed against any NEBA rules documentation or a real legacy data sample this session. **Could not confirm this from within this session.** If wrong, the practical effect is limited: this mapping only matters at step 3 (exact-type narrowing), which only runs when steps 1–2 leave multiple same-bucket candidates sharing an end date — a genuinely rare case per the user's own "95% of the time" framing.
6. **Legacy `Tournaments_TeamTournament.OverUnder == true` → `OverUnderFiftyDoubles` vs. `OverUnderFortyDoubles`** — the legacy schema found has only a single `OverUnder` bit with no accompanying age-threshold column, and the website's `OverUnderFortyDoubles` is marked `ActiveFormat: false` (no longer offered), so defaulting every legacy `OverUnder` team row to `OverUnderFiftyDoubles` is reasonable but **could not be independently confirmed** — if the Software still allows creating a Forty-variant tournament through some path not surfaced by the `OverUnder` bit alone, this mapping would misclassify it. Flagged for confirmation before implementation, not silently assumed correct.
7. **Real `neba-fwk` column names, including whether `IsBaker` is the actual mapped column name on `Tournaments_TeamTournament`** — not independently re-verified against the actual database/`.ssdl` this session, same caveat every prior plan in this series has carried.
8. **Whether `AddSinglesTournamentBO.Add`/`AddTeamTournamentBO.Add`'s commit points run inside any wider transaction/rollback scope.** **Could not confirm this from within this session** — flagged explicitly in the implementation prompt above as something the implementer must check, not assume. Also unconfirmed: that `TournamentRepository.Add`'s *two* separate `Commit()` calls (tournament row, then squads) both complete before `_addTournament.Add(...)` returns to the BO layer — placing the hook at the BO layer should already guarantee this by construction, but the exact return-timing wasn't traced line-by-line through `BaseAdd.vb` this session.
