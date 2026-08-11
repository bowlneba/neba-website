# Update Bowler Backdoor — Implementation Plan

Concrete plan for the second `/legacy` action, built on the scaffolding in `docs/plans/software-backdoor-scaffolding.md`, the standing decisions in `docs/api/software-backdoor-plan.md`, and the already-implemented precedent in `src/Neba.Api/Legacy/Bowlers/NewBowler.cs` (the file the "new bowler" plan doc described — that plan doc itself was deleted post-implementation, per the skill's own convention of not keeping a stale plan around once its target file exists). This document is a **plan only** — nothing here has been applied to either repo yet.

## Decision Recap

- **Two Software call sites, one website route.** Research into `nebamgmt-v3` found three places an existing bowler's full record can be committed, but they resolve to only two distinct call sites once traced to their actual commit points:
  - `UpdateBowlerBO.Update` — the "Edit Bowler" screen, the obvious direct path.
  - `RenewBowlerMembershipBO.Update` — reached from **two** different screens (the check-in "Verify Bowler" screen, and the merge branch inside `AddBowlerBO.Add` when a duplicate is found and merged into an existing bowler) but landing on the exact same commit point both times, so it only needs one hook, not two.

  Both call sites already have the target bowler's `Id` in scope locally (never a different identifier), so — same reasoning as `NewBowler` — **one route serves both**: `POST /legacy/bowlers/update`, body `{ "bowlerId": <id> }`.
- **Scope is deliberately narrower than the full legacy record.** `UpdateBowlerBO`/`RenewBowlerMembershipBO` commit the *entire* `Bowlers` row (name, gender, DOB, USBC id, email, phone, address, SSN, opt-in flags, hand, etc. — see schema table below), but the website's `Bowler` aggregate only models `Name`, `Gender`, and `DateOfBirth` today (per `Bowler.cs`'s own remarks: "Additional properties for member management... will be added when migrating"). This plan's sync job reads and maps only those same fields `NewBowlerSyncJob` already maps — it does not grow the aggregate to absorb the rest of the legacy schema. `SSN` in particular should never be read into this job regardless of aggregate scope.
- **Deceased-marking and auto-Champion are explicitly excluded from this action.** Research also found `DeceasedBO`/`BowlerRepository.Deceased` (flips a `Deceased` flag + scrubs PII) and `BowlerRepository.SetAsChampion` (flips a `Champion` flag when a bowler wins a tournament) as real bowler-row mutations in `nebamgmt-v3`. Neither maps to anything the website's `Bowler` aggregate currently tracks, and each has its own distinct commit point and partial field set — under the architecture doc's "route is the event type" rule, folding them into `UpdateBowler` would mean one route serving two structurally different events, which is exactly the shape the architecture doc already rejected (see "Why not a shared `eventType` field?"). They're left as candidates for their own future `/backdoor-feature` runs, not implemented here.
- **Missing-record idempotency: falls back to create, not skip.** If `UpdateBowlerSyncJob` runs for a `LegacyId` the website hasn't seen yet (`NewBowler`'s call never landed, or `UpdateBowler`'s call happens to arrive first), the job falls back to the same `Bowler.CreateFromLegacy(...)` path `NewBowlerSyncJob` uses, rather than logging and no-op'ing. This keeps the website eventually consistent regardless of which of the two events arrives first or whether a `NewBowler` call was ever dropped — the alternative (skip) would leave a bowler edited in the Software with no website record at all until someone notices and manually re-triggers a sync.
- **Nickname-in-quotes parsing is a website-side rule with no Software-side precedent.** Research confirmed `nebamgmt-v3` stores/edits `FirstName` as one plain free-text field (`nvarchar(20)`, no existing quote/nickname parsing anywhere in that codebase). The rule — `Shawn "Ditto"` → `FirstName = "Shawn"`, `Nickname = "Ditto"` — is implemented entirely on the website side, in a shared helper used by both `NewBowlerSyncJob` and this action's `UpdateBowlerSyncJob` (see below), since a bowler synced by either event should get the same nickname-extraction behavior.
- **`Bowler.ApplyLegacyUpdate` follows the same extension-member convention as `Bowler.CreateFromLegacy`, but requires a real, permanent change to the aggregate's property setters.** `Name`, `Gender`, and `DateOfBirth` are `{ get; init; }` today — `init` accessors can only run during construction (`new`/object-initializer), never against an already-existing instance, and `Bowler` is a plain `sealed class` (not a record), so it has no `with` support either. An extension member therefore cannot mutate an existing `Bowler`'s properties unless those setters are widened. This plan widens them to `{ get; internal set; }` — a genuine, permanent change to `Bowler.cs` itself (not something that gets deleted at sunset), because the aggregate's own eventual first-class `Update` method will need exactly the same mutability. Only the legacy-shaped mapping/validation logic in `Bowler.ApplyLegacyUpdate` (the extension member itself) is temporary and deleted with the rest of `Legacy/` at sunset — the `internal set` accessors stay and become the real update method's to use. (This same clarification was folded into the `backdoor-feature` skill itself, so future actions don't have to re-derive it.)

---

## Legacy Schema Reference (`neba-fwk`, `Bowlers` table)

Full committed schema (from `nebamgmt-v3`'s EF6 SSDL/entity, confirmed during this session's research — supersedes the partial table in the original `NewBowler` plan, which only listed the columns needed for that action):

| Column | Type | In scope for this action? |
|---|---|---|
| `Id` | `int`, identity, PK | yes — the sync key |
| `FirstName` | `nvarchar(20)`, not null | yes (also source of the quoted-nickname extraction) |
| `MiddleInitial` | `nchar(1)`, not null | yes |
| `LastName` | `nvarchar(25)`, not null | yes |
| `Suffix` | `nvarchar(3)`, not null | yes (mapped via the existing `MapSuffix` logic) |
| `Gender` | `int`, not null (`-1`=None, `0`=Male, `1`=Female) | yes |
| `DateOfBirth` | `datetime`, nullable | yes |
| `UsbcId` | `nvarchar(13)`, not null | no — not modeled on `Bowler` today |
| `Hand` | `int`, not null (`BowlingHand` enum) | no |
| `MailingAddress_*` (Street/City/State/Zip/Verified/Latitude/Longitude) | mixed | no |
| `Email`, `EmailVerified`, `HomePhone`, `CellPhone` | mixed | no |
| `HomeCenter`, `OtherCenter` | `nvarchar(50)`, not null | no |
| `USBCMembership` | `int`, not null (membership type enum) | no |
| `EmailList`, `TextList`, `Newsletter`, `Twitter`, `Facebook`, `Website` | `bit`, nullable | no |
| `SSN` | `nvarchar(max)`, nullable | **no — deliberately never read by this job, sensitive** |
| `Champion`, `Deceased` | `bit`, not null | no (see Decision Recap — separate future actions) |
| `Audit_*` | mixed | no — Software's own audit trail |

**Open item, same as the `NewBowler` plan already flagged**: these are EF POCO property names, not independently re-verified against the real `.ssdl`/database for this session. Confirm before writing the Dapper query — the `NewBowler` job's existing query already trusts these same property names for the columns it shares with this action, so if that query works in production, this one's shared columns (`Id`, `FirstName`, `MiddleInitial`, `LastName`, `Suffix`, `Gender`, `DateOfBirth`) can be trusted the same way.

---

## Website Side (`src/Neba.Api`)

### Change: `Bowler.cs` — widen `Name`, `Gender`, `DateOfBirth` to `internal set`

```csharp
public sealed class Bowler
    : AggregateRoot
{
    public required BowlerId Id { get; init; }

    public required Name Name { get; internal set; }

    public int? WebsiteId { get; init; }

    public int? LegacyId { get; init; }

    public Gender? Gender { get; internal set; }

    public DateOnly? DateOfBirth { get; internal set; }

    // ...SeasonStats unchanged
}
```

`Id`, `WebsiteId`, and `LegacyId` stay `init` — none of the found Software update paths ever change a bowler's identity or its legacy/website linkage, only its descriptive fields. `internal` (not `private`) so `Bowler.ApplyLegacyUpdate` — an extension member in a different class, but the same `Neba.Api` assembly — can assign them directly.

### New: shared `Legacy/Bowlers/LegacyNameParsing.cs` — quoted-nickname extraction, used by both actions

```csharp
namespace Neba.Api.Legacy.Bowlers;

// Shared by NewBowlerSyncJob and UpdateBowlerSyncJob: the Software stores FirstName as one plain
// free-text field with no nickname concept of its own (confirmed - no quote/nickname parsing exists
// anywhere in nebamgmt-v3). A bowler entered as `Shawn "Ditto"` in the Software's FirstName field is
// split here into FirstName "Shawn" / Nickname "Ditto" before either sync job maps it into Name.Create.
internal static class LegacyNameParsing
{
    extension(string firstName)
    {
        public (string FirstName, string? Nickname) ExtractQuotedNickname()
        {
            var firstQuote = firstName.IndexOf('"');
            if (firstQuote < 0)
            {
                return (firstName.Trim(), null);
            }

            var secondQuote = firstName.IndexOf('"', firstQuote + 1);
            if (secondQuote < 0)
            {
                // Unbalanced quote - treat the whole field as the first name rather than guessing.
                return (firstName.Trim(), null);
            }

            var nickname = firstName[(firstQuote + 1)..secondQuote].Trim();

            var remainder = string.Concat(
                firstName.AsSpan(0, firstQuote),
                firstName.AsSpan(secondQuote + 1)).Trim();

            return (remainder, string.IsNullOrWhiteSpace(nickname) ? null : nickname);
        }
    }
}
```

Notes on this sketch:

- Only the *first* quoted segment is extracted — legacy `FirstName` is `nvarchar(20)`, so a second quoted segment is not a realistic case, but if one exists it stays untouched in `remainder` rather than being silently dropped.
- An unbalanced quote (one `"` with no matching second one) is treated as "not a nickname pattern" and the field passes through trimmed but otherwise unchanged — this avoids guessing at intent from malformed legacy data.
- `remainder` is built by removing exactly the matched `"..."` span (including the quotes) and trimming, which correctly collapses cases like `Shawn "Ditto" ` (trailing space before the quote) or `"Ditto" Shawn` (nickname-first) to a clean `FirstName`.
- **Retrofit into `NewBowlerSyncJob`**: `NewBowlerSyncJob`'s existing `Bowler.CreateFromLegacy(...)` call passes `row.FirstName` straight through today with no `Nickname` argument at all. This plan updates that call site to `var (firstName, nickname) = row.FirstName.ExtractQuotedNickname();` and passes both `firstName` and `nickname` into `CreateFromLegacy`/`Name.Create` (which already accepts an optional `nickname` parameter — see `Name.Create`'s signature; it's simply never been populated by either sync job before now). This is a one-time retrofit of already-shipped code, done as part of this action's implementation, not a separate follow-up.

### Change: `Bowler.CreateFromLegacy` (in `NewBowler.cs`) — accept and pass through `nickname`

```csharp
public static ErrorOr<Bowler> CreateFromLegacy(
    string firstName,
    string lastName,
    string? middleName = null,
    NameSuffix? suffix = null,
    string? nickname = null,
    int? legacyId = null,
    Gender? gender = null,
    DateOnly? dateOfBirth = null)
{
    var name = Name.Create(firstName, lastName, middleName, suffix, nickname);

    return name.IsError
        ? name.Errors
        : new Bowler
        {
            Id = BowlerId.New(),
            Name = name.Value,
            LegacyId = legacyId,
            Gender = gender,
            DateOfBirth = dateOfBirth
        };
}
```

### New: `Bowler.ApplyLegacyUpdate` extension member (in `Legacy/Bowlers/UpdateBowler.cs`)

```csharp
namespace Neba.Api.Legacy.Bowlers;

internal static class LegacyBowlerUpdateExtensions
{
    extension(Bowler bowler)
    {
        public ErrorOr<Success> ApplyLegacyUpdate(
            string firstName,
            string lastName,
            string? middleName,
            NameSuffix? suffix,
            string? nickname,
            Gender? gender,
            DateOnly? dateOfBirth)
        {
            var name = Name.Create(firstName, lastName, middleName, suffix, nickname);
            if (name.IsError)
            {
                return name.Errors;
            }

            bowler.Name = name.Value;
            bowler.Gender = gender;
            bowler.DateOfBirth = dateOfBirth;

            return Result.Success;
        }
    }
}
```

An instance extension member (not `Bowler.CreateFromLegacy`'s static shape) since it mutates an existing tracked entity rather than constructing a new one — EF picks up the in-place property changes on `SaveChangesAsync` the same way it would for any other tracked entity mutation, no `Update(...)`/`Attach(...)` call needed since the entity was already loaded via `db.Set<Bowler>().SingleOrDefaultAsync(...)` in the same job.

### New: `Legacy/Bowlers/UpdateBowler.cs`

Following `NewBowler.cs`'s exact shape:

```csharp
namespace Neba.Api.Legacy.Bowlers;

internal static class UpdateBowlerEndpoint
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapUpdateBowler()
        {
            app.MapPost("/bowlers/update", (
                UpdateBowlerRequest request,
                IValidator<UpdateBowlerRequest> validator,
                IBackgroundJobClient jobs) =>
            {
                var validation = validator.Validate(request);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                jobs.Enqueue<UpdateBowlerSyncJob>(job => job.SyncAsync(request.BowlerId, CancellationToken.None));

                return Results.Accepted();
            });
        }
    }
}

internal sealed record UpdateBowlerRequest(int BowlerId);

internal sealed class UpdateBowlerRequestValidator
    : AbstractValidator<UpdateBowlerRequest>
{
    public UpdateBowlerRequestValidator()
    {
        RuleFor(x => x.BowlerId)
            .GreaterThan(0);
    }
}

internal sealed class UpdateBowlerSyncJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    ILogger<UpdateBowlerSyncJob> logger)
{
    public async Task SyncAsync(int legacyBowlerId, CancellationToken ct)
    {
        using var _ = AmbientActorContext.SetActor(LegacyActor.Id);

#pragma warning disable DAP005
        var row = await legacyConnection.QuerySingleOrDefaultAsync<LegacyBowlerRow>(
            """
            SELECT
                Id,
                FirstName,
                MiddleInitial,
                LastName,
                Suffix,
                Gender,
                DateOfBirth
            FROM
                Bowlers
            WHERE
                Id = @Id
            """, new
            {
                Id = legacyBowlerId
            }
        );
#pragma warning restore DAP005

        if (row is null)
        {
            logger.LogLegacyBowlerNotFoundForUpdate(legacyBowlerId);
            return;
        }

        var gender = row.Gender switch
        {
            0 => Gender.Male,
            1 => Gender.Female,
            _ => null
        };

        var dateOfBirth = row.DateOfBirth.HasValue
            ? DateOnly.FromDateTime(row.DateOfBirth.Value)
            : (DateOnly?)null;

        var suffix = MapSuffix(row.Suffix, legacyBowlerId, logger);
        var (firstName, nickname) = row.FirstName.ExtractQuotedNickname();

        var existing = await db.Set<Bowler>().SingleOrDefaultAsync(b => b.LegacyId == legacyBowlerId, ct);

        if (existing is null)
        {
            // Decided: fall back to create rather than skip. A missing record here means either the
            // NewBowler call for this legacy id never landed, or this Update event arrived before it -
            // either way, the website should end up with a bowler record either way, not silently drop
            // the update because create-and-update happened to race.
            logger.LogLegacyBowlerNotSyncedYetForUpdate(legacyBowlerId);

            var created = Bowler.CreateFromLegacy(
                firstName,
                row.LastName,
                middleName: row.MiddleInitial,
                suffix: suffix,
                nickname: nickname,
                legacyId: row.Id,
                gender: gender,
                dateOfBirth: dateOfBirth);

            if (created.IsError)
            {
                logger.LogLegacyBowlerUpdateFailed(legacyBowlerId, string.Join("; ", created.Errors.Select(e => e.Description)));
                return;
            }

            await db.Set<Bowler>().AddAsync(created.Value, ct);
            await db.SaveChangesAsync(ct);
            return;
        }

        var updated = existing.ApplyLegacyUpdate(
            firstName,
            row.LastName,
            middleName: row.MiddleInitial,
            suffix: suffix,
            nickname: nickname,
            gender: gender,
            dateOfBirth: dateOfBirth);

        if (updated.IsError)
        {
            logger.LogLegacyBowlerUpdateFailed(legacyBowlerId, string.Join("; ", updated.Errors.Select(e => e.Description)));
            return;
        }

        await db.SaveChangesAsync(ct);
    }

    // Identical to NewBowlerSyncJob.MapSuffix - see that file's comment for the full rationale.
    // Not yet extracted into the shared LegacyNameParsing.cs file since suffix mapping isn't part of
    // the nickname-parsing concern that file owns; if a third action needs it, that's the trigger to
    // extract a LegacySuffixParsing.cs alongside it rather than duplicating a third copy.
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
            logger.LogLegacySuffixUnmappedForUpdate(legacyBowlerId, legacySuffix);
        }

        return match;
    }
}
```

Notes / open items on this sketch:

- **`MapSuffix` is duplicated from `NewBowlerSyncJob`, deliberately, for now.** The plan doc's file-organization rule keeps each action's sync job self-contained in one file; extracting a shared suffix-mapping helper is a reasonable future cleanup but isn't forced by this action alone (unlike nickname parsing, which the user explicitly asked to share). If a third bowler-touching action needs suffix mapping, that's the signal to extract `LegacySuffixParsing.cs` next to `LegacyNameParsing.cs` rather than a third copy-paste.
- **Logging** — new `[LoggerMessage]` entries, added to the same `NewBowlerSyncJobLogMessages`-style partial class but scoped to this file (`UpdateBowlerSyncJobLogMessages`), matching the existing shape:

```csharp
internal static partial class UpdateBowlerSyncJobLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No bowler found in neba-fwk for legacy id {LegacyBowlerId}; skipping update sync.")]
    public static partial void LogLegacyBowlerNotFoundForUpdate(this ILogger logger, int legacyBowlerId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Legacy bowler {LegacyBowlerId} has no existing website record; creating instead of updating.")]
    public static partial void LogLegacyBowlerNotSyncedYetForUpdate(this ILogger logger, int legacyBowlerId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not map legacy suffix '{LegacySuffix}' (bowler {LegacyBowlerId}) to a known NameSuffix; leaving suffix blank.")]
    public static partial void LogLegacySuffixUnmappedForUpdate(this ILogger logger, int legacyBowlerId, string legacySuffix);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to apply legacy update for bowler {LegacyBowlerId}: {Errors}.")]
    public static partial void LogLegacyBowlerUpdateFailed(this ILogger logger, int legacyBowlerId, string errors);
}
```

  No `[PersonalData]`/`[PrivateData]` needed — same reasoning as `NewBowlerSyncJobLogMessages`: every logged parameter is an id, a suffix string, or an error description, never the bowler's actual name/DOB.

### Update: `Legacy/LegacyEndpoints.cs`

```csharp
extension(IEndpointRouteBuilder app)
{
    public void MapLegacyEndpoints()
    {
        app.MapNewBowler();
        app.MapUpdateBowler();
    }
}
```

### Tests

Per the architecture doc's five testing layers, collapsed into one file: `tests/Neba.Api.Tests/Legacy/Bowlers/UpdateBowlerTests.cs`, mirroring `NewBowlerTests.cs`'s multi-class-single-file shape exactly (`UpdateBowlerRequestValidatorTests`, `UpdateBowlerEndpointTests`, `LegacyBowlerUpdateExtensionsTests`, `UpdateBowlerSyncJobTests`). Plus a small, targeted addition to the *existing* `NewBowlerTests.cs` for the nickname retrofit (not a new file — the retrofit changes `NewBowlerSyncJob`'s own behavior, so its own test file gets the new case).

1. **`UpdateBowlerRequestValidatorTests`** — same shape as `NewBowlerRequestValidatorTests`: `BowlerId > 0`.
2. **`UpdateBowlerEndpointTests`** — same shape as `NewBowlerEndpointTests`: `401` (missing/wrong key — not re-asserted beyond confirming the filter is wired, per `LegacyApiKeyFilterTests` already covering the filter itself), `400` (invalid `BowlerId`), `202` + right job/args enqueued (`UpdateBowlerSyncJob.SyncAsync`, right `bowlerId`) via `Mock<IBackgroundJobClient>(MockBehavior.Strict)`.
3. **`LegacyNameParsingTests`** (new, standalone — this helper is shared by both actions, so its own tests aren't scoped to either action's file; lives at `tests/Neba.Api.Tests/Legacy/Bowlers/LegacyNameParsingTests.cs`, deleted alongside `Legacy/Bowlers/LegacyNameParsing.cs` at sunset): `Theory` cases —
   - `Shawn "Ditto"` → `("Shawn", "Ditto")`
   - `"Ditto" Shawn` → `("Shawn", "Ditto")`
   - `Shawn` (no quotes) → `("Shawn", null)`
   - `Shawn "Ditto` (unbalanced quote) → `("Shawn \"Ditto", null)` (passthrough, trimmed)
   - `""` / whitespace-only quoted content, e.g. `Shawn "  "` → `("Shawn", null)` (blank nickname treated as no nickname)
4. **`LegacyBowlerUpdateExtensionsTests`** — `ApplyLegacyUpdate` mutates `Name`/`Gender`/`DateOfBirth` in place and returns `Result.Success` on a valid mapped name; returns the mapping error (and leaves the bowler's fields untouched) when the mapped name is invalid.
5. **`UpdateBowlerSyncJobTests`** — same Postgres-temp-table-as-legacy-connection pattern as `NewBowlerSyncJobTests`. Covers:
   - Not-found in `neba-fwk` → no bowler created/updated, warning logged.
   - Existing website bowler (seeded via `BowlerFactory.Create(legacyId: ...)`) → `Name`/`Gender`/`DateOfBirth` updated to match the legacy row; `Id`/`WebsiteId`/`LegacyId` unchanged.
   - **No existing website bowler for this `LegacyId`** → falls back to create (mirrors `NewBowlerSyncJobTests`'s "should persist a bowler mapped from the legacy row" case, but via the update route) — this is the one behavior genuinely new to this action versus `NewBowlerSyncJob`'s strict-no-op-on-duplicate stance, so it needs its own explicit assertion, not just a reused case.
   - Gender/DateOfBirth/suffix mapping — same theory-style cases as `NewBowlerSyncJobTests`, since the mapping logic is duplicated (suffix) or shared (gender/DOB pattern, same inline switch).
   - Nickname extraction end-to-end through the job (in addition to `LegacyNameParsingTests`' unit coverage of the helper itself) — one case confirming a legacy `FirstName` of `Shawn "Ditto"` results in a persisted `Bowler` with `Name.FirstName == "Shawn"` and `Name.Nickname == "Ditto"`, for both the create-fallback and true-update branches.
   - Update-failure path (mapped name invalid, e.g. blank `FirstName` after nickname extraction) → error logged, no `SaveChangesAsync` side effect (existing bowler's fields left as they were — this needs an explicit "unchanged" assertion, since `ApplyLegacyUpdate` mutates in place, so a naive implementation could partially apply fields before validating; the sketch above validates via `Name.Create` before touching `bowler.Name`/`Gender`/`DateOfBirth` at all, so this test should pass, but the update-in-place shape makes this worth asserting explicitly rather than assuming).
6. **Retrofit case added to existing `NewBowlerTests.cs`** (`NewBowlerSyncJobTests`): one new case confirming `SyncAsync` extracts a quoted nickname from the legacy `FirstName` the same way `UpdateBowlerSyncJob` does — e.g. legacy `FirstName = "Shawn \"Ditto\""` → persisted `Bowler.Name.FirstName == "Shawn"`, `Name.Nickname == "Ditto"`. This is the test-side half of the "retrofit `NewBowlerSyncJob`" decision above — the retrofit isn't real until this proves it.

---

## Software Side (`nebamgmt-v3`)

### Call site 1 — `UpdateBowlerBO.Update`

File: `Membership/NEBA.Membership.BusinessLogic/Bowlers/UpdateBowlerBO.cs`, method `UpdateBowler.Update(BOM.Membership.Bowler bowler)` (~lines 56–84), inside the existing `try` block, right after `DataAccess.Update(bowler)` (line ~75) succeeds:

```csharp
try
{
    DataAccess.Update(bowler);
    // NEW: fire-and-forget backdoor call here, after the local commit succeeds.
}
catch (BOM.Exceptions.DatabaseCommitException ex) { ... }
```

Bowler id in scope: `bowler.Id` (always an existing bowler on this path).

### Call site 2 — `RenewBowlerMembershipBO.Update`

File: `Membership/NEBA.Membership.BusinessLogic/BowlerMembership/RenewBowlerMembershipBO.cs`, method `RenewBowlerMembership.Update(BOM.Membership.Bowler bowler)` (~lines 67–103), right after `DataAccess.Update(bowler)` (line ~90) succeeds. This one hook covers **both** of its callers automatically — no separate hook needed at either:

- `VerifyBowlerPresenter.Save()` (`Membership/NEBA.Membership.UI.Presenters/Bowlers/VerifyBowlerPresenter.cs`) — the check-in "Verify Bowler" screen, which reuses the same fully-editable `BowlerControl` as the standalone edit screen, so a user can silently edit any name/gender/DOB field here too, not just renew membership.
- `AddBowlerBO.Add`'s merge branch (`Membership/NEBA.Membership.BusinessLogic/Bowlers/AddBowlerBO.cs`, ~lines 93–108) — when a duplicate-check finds an existing bowler and the user chooses to merge, `AddBowlerBO.Add` calls `UpdateBowler.Update(BowlerMerge.Execute(bowler, existingBowler))` where `UpdateBowler` here is the same `IRenewBowlerMembership` instance. Bowler id in scope: `result.Value` (the *existing* bowler's id being merged into, not the return value of the outer `Add` call in this branch).

Bowler id in scope at the hook: `bowler.Id`, same as call site 1 (`RenewBowlerMembershipBO.Update`'s own parameter, already resolved by the time it reaches the commit point regardless of which of its two callers is in play).

### Reuse the existing `WebsiteSyncAdapter` — confirmed, no new adapter needed

Confirmed by reading `Common/NEBA.Common/Adapters/WebsiteSyncAdapter.cs` directly: it's a fully static class (no instance state besides one `private static readonly HttpClient` with a 5-second timeout) with a general-purpose private `Send(string url, string apiKey, string jsonBody)` helper that `NotifyNewBowler` already calls — `Send` has no "new bowler" specifics baked in, so a sibling public method reuses it as-is:

```csharp
// Common/NEBA.Common/Adapters/WebsiteSyncAdapter.cs
public static void NotifyBowlerUpdated(int bowlerId)
{
    var baseUrl = ConfigurationManager.AppSettings["WebsiteSyncUrl"];
    var apiKey = ConfigurationManager.AppSettings["WebsiteSyncApiKey"].Decrypt();

    if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
        return;

    Send(baseUrl.TrimEnd('/') + "/legacy/bowlers/update", apiKey, $"{{\"bowlerId\":{bowlerId}}}");
}
```

This is a straight sibling of the existing `NotifyNewBowler` (same file, same class, same `Send`, same static `HttpClient`, same config keys, same failure-swallowing/`Trace.TraceError` logging, same `Task.Run` fire-and-forget dispatch) — only the route suffix and the method name differ. The config-lookup-plus-guard-clause block is duplicated between the two public methods rather than factored into a shared helper; that's pre-existing duplication style in this class (not introduced by this change), worth a follow-up cleanup but not a blocker for adding the second method.

No new config keys, no new `HttpClient`, no new threading/timeout decisions — `WebsiteSyncUrl`/`WebsiteSyncApiKey` (`Main/NEBA.UI/App.config` dev defaults, `App.Release.config` transform) already cover any route under the same base URL/key.

### Confirmed call sites

- **`Membership/NEBA.Membership.BusinessLogic/Bowlers/AddBowlerBO.cs`**, class `AddBowler` (file is named `AddBowlerBO.cs`; the class itself is `AddBowler` — same file the existing `NotifyNewBowler(id)` call already lives in, line 117, inside `Add(BOM.Membership.Bowler bowler)`). This file needs no new hook for its main (non-merge) branch — that's the existing new-bowler notification. Its merge branch (~lines 93–108, calling into `RenewBowlerMembershipBO`/`IRenewBowlerMembership.Update`) is covered transitively by the `RenewBowlerMembershipBO` hook below, not by anything added directly in this file.
- **`Membership/NEBA.Membership.BusinessLogic/Bowlers/UpdateBowlerBO.cs`**, `UpdateBowler.Update(BOM.Membership.Bowler bowler)` — new hook, call site 1 above.
- **`Membership/NEBA.Membership.BusinessLogic/BowlerMembership/RenewBowlerMembershipBO.cs`**, `RenewBowlerMembership.Update(BOM.Membership.Bowler bowler)` — new hook, call site 2 above; covers both its callers (`VerifyBowlerPresenter.Save()` and `AddBowlerBO`'s merge branch) without any changes in either of those two files.

### Open items on the software side

- Same open item the `NewBowler` plan already flagged and never fully closed: does `RenewBowlerMembershipBO.Update`'s call from the `AddBowlerBO` merge branch run inside any wider transaction/rollback scope where firing the HTTP call immediately after `Commit()` could still end up "sent" even if something later in the same user operation rolls back?
- `CheckInRepository.cs` also already calls `WebsiteSyncAdapter.NotifyNewBowler` (via its own `NotifyNewBowlers()`, for the quick-add-bowler check-in path from the `NewBowler` action) — confirm this action's changes don't need any corresponding touch there. Research found no bowler-field-mutation path through `CheckInRepository` for *existing* bowlers (see Decision Recap in the original `NewBowler` research), so this should stay untouched, but worth a final glance given it's the one file in this app that already imports `WebsiteSyncAdapter` from `NEBA.Data` rather than a `BusinessLogic` project.

### Prompt for the `nebamgmt-v3` implementation

Everything above, condensed into a standalone prompt — self-contained, so it can be pasted to an agent working directly in `nebamgmt-v3` with no access to this conversation or this file:

> You're working in `nebamgmt-v3` (WinForms, .NET Framework), the legacy management application for NEBA. A previous change already added a "backdoor" sync mechanism that notifies a separate website whenever certain local actions happen, via `WebsiteSyncAdapter.NotifyNewBowler(int bowlerId)` (already wired up and called from `AddBowlerBO.Add`). This task adds a second event to that same mechanism: notifying the website whenever an *existing* bowler's information is edited.
>
> **Step 1 — add a new method to the existing `Common/NEBA.Common/Adapters/WebsiteSyncAdapter.cs`** (a fully static class; do not create a new adapter). It already has a general-purpose private `Send(string url, string apiKey, string jsonBody)` helper that its existing `NotifyNewBowler(int bowlerId)` method calls — add a sibling public method reusing that same helper:
>
> ```csharp
> public static void NotifyBowlerUpdated(int bowlerId)
> {
>     var baseUrl = ConfigurationManager.AppSettings["WebsiteSyncUrl"];
>     var apiKey = ConfigurationManager.AppSettings["WebsiteSyncApiKey"].Decrypt();
>
>     if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
>         return;
>
>     Send(baseUrl.TrimEnd('/') + "/legacy/bowlers/update", apiKey, $"{{\"bowlerId\":{bowlerId}}}");
> }
> ```
>
> Same config keys (`WebsiteSyncUrl`/`WebsiteSyncApiKey`), same shared `HttpClient`/timeout/fire-and-forget dispatch/failure logging as `NotifyNewBowler` — do not build a second `HttpClient`, a second timeout, or a second dispatch mechanism.
>
> **Step 2 — wire the call into both places an existing bowler's info can be edited:**
>
> 1. **`Membership/NEBA.Membership.BusinessLogic/Bowlers/UpdateBowlerBO.cs`**, class `UpdateBowler`, method `Update(BOM.Membership.Bowler bowler)`. Right after `DataAccess.Update(bowler)` succeeds (inside the existing `try` block — mirror how `AddBowlerBO.cs`'s `AddBowler.Add` calls `WebsiteSyncAdapter.NotifyNewBowler(id)` right after its own `DataAccess.Add(bowler)`/`SystemGeneratedTasks(bowler, id)`, at line 117 of that file), call `NEBA.Common.Adapters.WebsiteSyncAdapter.NotifyBowlerUpdated(bowler.Id)`.
> 2. **`Membership/NEBA.Membership.BusinessLogic/BowlerMembership/RenewBowlerMembershipBO.cs`**, class `RenewBowlerMembership`, method `Update(BOM.Membership.Bowler bowler)`. Right after its own `DataAccess.Update(bowler)` succeeds, call the same `WebsiteSyncAdapter.NotifyBowlerUpdated(bowler.Id)`. **This one hook is sufficient** — do not add separate hooks in `VerifyBowlerPresenter.Save()` (`Membership/NEBA.Membership.UI.Presenters/Bowlers/VerifyBowlerPresenter.cs`) or in `AddBowlerBO.cs`'s merge branch (`AddBowler.Add`, ~lines 93–108); both of those already funnel into this exact `RenewBowlerMembership.Update` method before reaching its commit point, so hooking here covers both automatically. Confirm this by tracing both call paths before assuming it — if either caller turns out to bypass this method under some condition, flag that instead of silently adding a redundant hook.
>
> **Do not** add a hook to `AddBowlerBO.cs`'s non-merge branch (a genuinely new bowler) — that's already covered by the existing `NotifyNewBowler` call at line 117. Only the merge branch (which becomes an *update* of the pre-existing duplicate bowler) is relevant here, and it's covered transitively via `RenewBowlerMembershipBO.Update` per point 2 above.
>
> **Before you start, resolve these open questions** (don't guess silently — ask, or make the decision explicit in a comment/commit message with your reasoning):
>
> 1. Does `RenewBowlerMembershipBO.Update`'s call from `AddBowlerBO.Add`'s merge branch run inside any wider transaction/rollback scope where firing the HTTP call immediately after `Commit()` could still end up "sent" even if something later in the same user operation rolls back?
> 2. Confirm the base URL/API key config keys already added for `NotifyNewBowler` are what `NotifyBowlerUpdated` should reuse (they should be, since it's the same adapter/config — just verify, don't assume).
>
> Do not change `UpdateBowlerBO`'s or `RenewBowlerMembershipBO`'s public method signatures — the notification is a side effect fired from inside the existing method body, not a new parameter/return value any caller needs to know about.

---

## Summary of what's still undecided

1. ~~Whether `UpdateBowlerSyncJob` should update an existing bowler on a repeat call, or strictly no-op (mirroring `NewBowlerSyncJob`).~~ **Decided** — this action's entire purpose is to update, so a repeat call for the same `LegacyId` re-applies the mapped fields every time (idempotent by construction, since the same legacy row always maps to the same result — not a no-op like `NewBowlerSyncJob`, which was avoiding "which fields are safe to overwrite" for a strictly-create action).
2. ~~What happens when `UpdateBowlerSyncJob` runs for a `LegacyId` with no existing website record.~~ **Decided** — falls back to create via `Bowler.CreateFromLegacy(...)`, per the user's explicit choice during planning.
3. ~~Whether the quoted-nickname parsing rule should also apply to `NewBowlerSyncJob`, not just this action.~~ **Decided** — yes, via a shared `LegacyNameParsing.ExtractQuotedNickname()` helper used by both, with `NewBowlerSyncJob` retrofitted as part of this action's implementation (see Tests section, item 6).
4. ~~How `Bowler.ApplyLegacyUpdate` should be modeled given `Name`/`Gender`/`DateOfBirth` are `init`-only today.~~ **Decided** — widen those three properties to `internal set` (a real, permanent aggregate change, not deleted at sunset) and add `Bowler.ApplyLegacyUpdate` as an instance extension member in `Legacy/`, matching `Bowler.CreateFromLegacy`'s convention. This clarification was also folded into the `backdoor-feature` skill itself.
5. ~~Whether Deceased-marking and auto-Champion mutations belong in this action.~~ **Decided** — excluded; left as candidates for their own future `/backdoor-feature` runs, since neither maps to a field the website's `Bowler` aggregate models today and each has a distinct commit point/payload shape.
6. **Real `neba-fwk` column names** — not independently re-verified against the actual database/`.ssdl` this session (same caveat the `NewBowler` plan carried). The columns this action shares with `NewBowler` (`FirstName`, `MiddleInitial`, `LastName`, `Suffix`, `Gender`, `DateOfBirth`) can be trusted to the same extent `NewBowlerSyncJob`'s already-working query trusts them; nothing new is being assumed here.
7. ~~`WebsiteSyncAdapter`'s exact current internals.~~ **Decided/confirmed** — read directly (`Common/NEBA.Common/Adapters/WebsiteSyncAdapter.cs`): fully static, one shared `HttpClient` (5s timeout), a reusable private `Send(url, apiKey, jsonBody)` helper already factored out of `NotifyNewBowler`. `NotifyBowlerUpdated` is a straight sibling method reusing `Send` — no new adapter, no new config keys (`WebsiteSyncUrl`/`WebsiteSyncApiKey` already cover any `/legacy` route under the same base URL).
8. **Whether `RenewBowlerMembershipBO.Update`'s merge-branch caller runs inside a wider transaction/rollback scope** — not independently verified through the full call stack, same open item the `NewBowler` plan carried for its own check-in call site. **Could not confirm this from within this session — flagged explicitly in the implementation prompt above as something the implementer must check, not assume.**
