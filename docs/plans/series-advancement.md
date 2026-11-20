# Series Advancement (Vincent Hall Memorial Series, Phase 2)

GitHub issue: [#259](https://github.com/bowlneba/neba-website/issues/259) (parent [#257](https://github.com/bowlneba/neba-website/issues/257))

When a tournament linked to a Stage completes, the website works out who advances: it assigns qualifiers, fills vacant slots from Bowler of the Year points (the Backfill), picks Survivor Round advancers from the designated squad, and ranks the Placement Round. It stores each Stage's output, makes computed Stages official automatically after a grace period, and lets admins repair mistakes. No payouts and no Finale results (phase 3).

## Implementing this plan

For a fresh session. The file is about 7,500 lines because it holds both code drafts. **Do not read it all.** Read the parts below, then only the sections your commit needs.

**Read first, in this order**
1. This section.
2. `## Decisions locked in during scoping` and `### Confirmed UI flow`.
3. `### Reconciliation notes` (Phase 1) and `#### Notes` under `### Code draft (blazor-ui)` (Phase 2).
4. `## Commit plan`. Pick the next unchecked commit in the progress list below.
5. The sections the map below names for that commit.

**Precedence when two parts disagree:** Decisions, then the reconciliation notes and UI notes, then the code drafts, then the text of issues #259 and #260 and `vincent-hall-series-ul.md` (those are older; the plan and `docs/ubiquitous-language.md` win). The repo wins over the plan on a name that does not match; fix the plan when you find one.

**State of the drafts.** The code is paste-ready but was never compiled or run. Expect small fixes at each first build; the unverified items are in each phase's notes. Phase 2 cannot build until commit 13 lands the Contracts. The Contract namespaces in the UI draft are guesses until then.

**For each commit**
1. Read its entry in `## Commit plan` and the sections in the map.
2. Hand it to the agent the entry names (`efcore`, `backend-api`, `blazor-ui`, `e2e`, `docs`, `test-writer`; `test-writer` writes the tests for code another agent wrote). Give the agent the plan path and the commit entry.
3. Run the build and the tests for that area (`dotnet build`; `dotnet test --filter-trait "Component=Series"` and any other component named; `npm run test:e2e` for e2e commits). Fix failures before committing.
4. If the code drifted from the plan, update the plan (the code draft, not only a note) in the same commit.
5. Stage only that commit's files by name (never `git add -A`), commit with the subject from the entry and the trailer `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`, and tick the box below.

**Do not:** add a feature flag, add outbox or inbox code (issue #56 is deferred; events go only through `IDomainEventPublisher`), add a Survivor override, call the status "Announced" (it is "Official"), or start a commit before the one it depends on.

**Before specific commits:** before 4, check staging and production for `series_stages.status = 0` on every row (the check constraints fail otherwise). Before 21, confirm the Contract names from commit 13 against the UI draft. Commit 16 uses a synthetic scenario, not 2025 data.

**Commit to section map.** Labels: *DB* is `### Database layer`, *API* is `### API layer`, *UI* is `### Code draft (blazor-ui)`. Numbers and letters are the `####` headings inside each.

| Commit | Read |
|---|---|
| 1, 2 | nothing (docs already written) |
| 3 | Decisions (the Official bullet); API §C (`StageStatus`) |
| 4 | Reconciliation notes 3 to 7; DB §0 to §7 |
| 5 | API §0, §C (`AdvancementFacts`, `StageComputation`), §D |
| 6 | Reconciliation notes 1, 2, 6, 8, 9; DB §1; API §C, §E |
| 7 | Reconciliation note 1; DB §8; API §F (`EasternTime`), §G |
| 8 | API §A (keyed lock), §F (`SeriesAdvancement`, `SeriesConcurrency`, handler edits), §G |
| 9 | API §F (make-official job, settings, registration) |
| 10 | Reconciliation note 10a; API §A (events, publisher, interceptor) |
| 11 | API §F (event jobs) |
| 12 | API §B; Phase 1 `### Tests` |
| 13 | API §H (shared validator rule and result sender), §I |
| 14 | API §G, §H uses 1, 6 and 7 |
| 15 | API §H uses 2 to 5 |
| 16 | Phase 1 `### Tests` (group 16); Decisions (synthetic scenario) |
| 17 | API §A and §I (Docs); Decisions (#56) |
| 18 | Phase 2 `### Tests` (Factories); API §I (Contracts types) |
| 19 | Phase 2 `### Tests` (Mock changes) |
| 20 | UI Notes; UI §1 to §5 |
| 21 | Phase 2 `### Decisions taken in this phase`; UI Notes; UI §6 to §10, §13 |
| 22 | UI §7 (write methods), §12 |
| 23 | UI §11 |
| 24 | UI §14 to §17 |
| 25 | Phase 2 `### Tests` (Playwright); `### Mockups` |
| 26 | Phase 2 `### Docs and hand-offs` |

**Mockups** (`docs/plans/mockups/series-advancement/`, open in a browser) are the visual source for commits 21 to 23: `series-advancement-option-2.html` (chosen), `override-modal.html`, `squad-picker-modal.html`. Options 1 and 3 were not chosen.

**Progress** (tick as each commit lands; a fresh session resumes at the first unchecked box)

- [x] 1 Add the Series advancement plan and mockups
- [x] 2 Update the Series language for advancement
- [x] 3 Rename the Stage status Announced to Official
- [ ] 4 Add the Series advancement tables and the Version token
- [ ] 5 Add the four advancement computations
- [ ] 6 Add advancement to the Series aggregate
- [ ] 7 Add the advancement readers
- [ ] 8 Add the per-Series lock and the advancement run
- [ ] 9 Make computed Stages Official after a grace period
- [ ] 10 Add the domain event publisher
- [ ] 11 Advance Series from tournament, squad and stats events
- [ ] 12 Raise the tournament and stats events
- [ ] 13 Add the advancement contracts and the Manage advancement permission
- [ ] 14 Add the advancement read endpoints
- [ ] 15 Add the advancement write endpoints
- [ ] 16 Add the synthetic 2025 replay test
- [ ] 17 Record the domain event decisions
- [ ] 18 Add the advancement response factories
- [ ] 19 Mock the advancement API for e2e
- [ ] 20 Add the advancement view models, mapping and display text
- [ ] 21 Show the season rail on the Series page
- [ ] 22 Add Recompute and Pick squad to the Series page
- [ ] 23 Add the override controls to the Series page
- [ ] 24 Let admins remove a cancelled Stage from a locked Series
- [ ] 25 Add Playwright specs for Series advancement
- [ ] 26 Add the Series advancement help doc

## Decisions locked in during scoping

- **Source of truth.** `vincent-hall-series-ul.md` (attached to #257) and `docs/ubiquitous-language.md` (`## Series`) win over #259 where they differ. Code names match them.
- **Departures from #259 (the issue needs an update before the PR):**
  - **A Stage becomes Official automatically, and "Official" replaces #259's "Announced" (decided 2026-11-20).** #259's admin Announce action is gone. A scheduled job makes a computed Qualifying or Survivor Stage Official after a grace period (setting, default 24 hours) if nothing changed it. Official means the output is settled and no longer changes on its own; only a confirmed Recompute resets it. "Announced" was dropped because it clashes with Discord announcements, with tournament "completed", with the phase 1 "Locked" badge, and because the public reads it differently. The last Qualifying Stage becomes Official only after the Backfill ran. A Placement Stage is never Official. Names follow: `StageStatus.Official`, `OfficialAt`, `OfficialDueAt`, `OfficialGraceHours`, `MakeDueStagesOfficial`. The UL, #259 and the UL source need the same word.
  - **Manual override of Qualifying advancers is new.** An admin can add any bowler with a result row in that Stage's tournament (any Place, member or not) and can clear a slot (the slot goes vacant and the Backfill refills it). Overrides are stored (bowler, Stage, add or clear, reason, who, when). They survive every recompute until an admin removes them. The Stage function is deterministic over earlier Stages, results and overrides.
  - **No feature flag** (see below).
  - **Survivor override is out of scope.** The Survivor calculation is straightforward. A future permission can add it.
- **Critical/high-stakes. NFR targets (confirmed):**
  - *Performance:* tiny volume (a few Series per season, about 11 Stages, about 100 bowlers). All work runs in Hangfire jobs. Admin reads load stored output. No pagination, no `ICachedQuery`.
  - *Concurrency:* one job at a time per Series. Each run reloads the aggregate and rechecks Stage status before writing. Admin writes use an optimistic concurrency token and return 409 `Conflict` on a stale screen.
  - *Idempotency:* redelivery re-runs a computed Stage to the same result. A Stage's output is deleted and rebuilt in one transaction. An official Stage ignores the event.
  - *Security:* no new PII. Output holds `BowlerId`, scores and positions. Logs carry `BowlerId`, never names. Audit.NET records admin writes.
  - *Degradation:* a missing prerequisite leaves the Stage pending (expected, not an error). Failures use normal Hangfire retry. The Backfill uses the stored `BackfillAsOf`, never "now".
  - *Retention:* keep output permanently. Recompute rebuilds it. Deleting a Series cascades. Bowler merge must move the new `BowlerId` columns (no merge flow exists yet; record the columns for it).
- **Prerequisites are built in this plan** (no blocking tickets): tournament completion domain event (including the legacy completion path), season-stats-generated event published by Stats, Bowler of the Year as of a date, membership on a date, squad matching by date and start time (time zone aware).
- **Permissions (confirmed).** Checked by permission, never by role name.

  | What | Gate | Held today by |
  |---|---|---|
  | See Stage status, output, markers, Backfill date | `Series.EditSeries` | Admin, Manager |
  | Pick squad | `Series.EditSeries` | Admin, Manager |
  | Recompute from a Stage | `Series.EditSeries` | Admin, Manager |
  | Add bowler to a Qualifying Stage, clear a slot, remove an override | `Series.ManageAdvancement` (new) | Admin only (via `Permissions.List`; Manager not seeded) |
- **No feature flag (decided 2026-11-20; supersedes #259's `series-advancement` flag).** This ships in the 1.3.0 release and is live from day one, so nothing is gated. Advancement runs as soon as the release deploys. A tournament completed before the deploy is not caught up automatically: an admin runs Recompute from the first Stage. The issue's flag acceptance criterion drops out.
- **Locked Series.** The phase 1 lock (any Stage not pending) applies.

- **Trigger event (confirmed, supersedes #259's "completion signal").** One event, `TournamentResultsRecorded(TournamentId)`, raised by a new `Tournament.MarkResultsRecorded()` that `SyncTournamentResultsJob` calls after its `AddResult` loop. Complete, Truncate and Cancel raise nothing, because in the legacy chain results arrive after the status flips. It fires on every re-sync, which gives the redelivery behavior. A Stage may compute when its tournament is Completed or Truncated, it has at least one result row, and the previous Stage is Computed or Official.
- **Cancelled tournament (confirmed).** Phase 1 locks Stage edits once any Stage leaves Pending, and a cancelled tournament never gets results. `ReplaceConfiguration` may remove a Pending Stage whose tournament is Cancelled even while locked. The admin then runs Recompute from the previous Stage and the target field shrinks. The Stage list shows "Tournament cancelled: remove this Stage".
- **Overrides (confirmed).** Add always adds an entry (the field may exceed the target, as it can with Backfill ties). Clear bars that bowler from the Backfill for the whole Series. Overrides apply to Qualifying Stages only, and only while the Stage is Computed (an Official Stage rejects them).
- **Backfill candidates must be members on the as-of date (confirmed, adds a rule to #259).** A non-member high in the points is not handled now. If it happens, an override screen like the Qualifying one can come later.
- **Transactional outbox (#56) is deferred, not rejected (decided 2026-11-20).** Series is the first consumer of domain events. A lost event leaves a Stage "Not computed yet" until the next event or an admin Recompute, and every event job is idempotent, so no outbox or inbox is built now. Constraint that keeps the later swap cheap: **every event is enqueued through `IDomainEventPublisher`** (the interceptor and the legacy jobs included), enforced by an architecture test, so an outbox replaces only `DomainEventPublisher`. Option C (a custom outbox table) fits that seam better than Option B, because the legacy jobs publish outside any aggregate. Revisit with each new domain event: build it or close #56 as something we won't do. A comment on #56 recording this is pending the user's go-ahead.
- **Qualifying membership date is the tournament end date (confirmed 2026-11-20; supersedes #259 and the UL's "start date").** A bowler who joins during the event still takes the slot. The Backfill checks membership on `BackfillAsOf`, which is the last Qualifying tournament's end date.
- **Open Bowler of the Year points as of a date (confirmed).** SQL over website `TournamentResult.Points` for Completed and Truncated, stats-eligible tournaments with `EndDate <= as-of`, plus the Tournament of Champions double-dip exclusion. The legacy side-cut bonus can't be reproduced from website data, so the gap is documented and a parity test checks it against the official total at season end.
- **Defaults taken from the drafts (correct any at the Phase 1 gate):** admin writes compute inline under the same per-Series lock (no 202); a Stage becomes Official only after all earlier Qualifying and Survivor Stages are official; a redelivered event to an Official Stage does a dry compute and sets `Stale` only if the result differs; one override resource (`kind` is Add or Clear); `Version` bumps in advancement mutators and `ReplaceConfiguration`, not in Rename, logo or Finale reschedule; `BackfillAsOf` is the last Qualifying tournament's `EndDate`, inclusive; every row at `Place <= N` is a slot (ties at N all count); a Truncated tournament computes like a Completed one; Placement ties break by `BowlerId`; a Survivor Stage also waits until its squad has at least one score row; squad match compares Eastern date and start time to the minute; a vanished squad reverts the designation, marks the Stage `Stale` and leaves stored output alone.

### Confirmed UI flow

All on the existing `/series/{id}` page, shown to holders of `Series.EditSeries`. Public pages stay out of scope.

1. Each Stage row shows a status badge (Pending, Computed, Official; a Placement Stage Pending or Computed) and indicators: "Needs squad", "Stale", "Becomes official {date/time}" on a computed Stage. The last Qualifying Stage shows the Backfill date.
2. Expanding a Stage shows its output, read-only. Qualifying: field entries (bowler, place, source), vacant slots, Backfill entries with points, override markers. Survivor: block totals and advanced flag. Placement: positions, bowled flag, finalist flag.
3. Actions: Add bowler, Clear slot, Remove override (`Series.ManageAdvancement`); Pick squad and Recompute from here with a confirmation listing official Stages it resets (`Series.EditSeries`). The page reloads the Stage's output after each action.
4. Data: one new advancement resource under the Series on load; the tournament's result rows for the add-bowler picker; the tournament's squads for the squad picker (reuse and project down); separate REST writes.

## Phase 1: API

Code draft, not compiled. Two agents wrote it (`efcore` for the database layer, `backend-api` for everything else) from the confirmed functional breakdown. Expect small fixes at first build. Read the reconciliation notes first: they override the drafts where the two disagree.

### Reconciliation notes (these win over the drafts below)

1. **Member date for Qualifying slots is the tournament end date (confirmed).** The draft's choice A is taken as recommended: `facts.MembersOn(tournament.EndDate)`, the same date family as `BackfillAsOf`. This differs from #259 and the UL ("start date"), so both need the change.
2. **`StageOverride.CreatedBy` is a string in the domain and the database.** The `efcore` column is `HasMaxLength(64)`, required, no foreign key. The endpoint and command keep `Ulid CallerUserId` from the existing `User.CallerUserId` extension (`UpdateUserCommand` does the same), and the handler passes `command.CallerUserId.ToString()` to the domain. `ICurrentUserService.ActorId` is not used here: it can be the ambient actor or "anonymous", and these endpoints are admin-only.
3. **Bowler foreign keys are Restrict on all four new tables** (`efcore`), not Cascade on the derived ones. No bowler delete path exists, and a bowler in official output must not vanish. `source_stage_id` is NoAction.
4. **Timestamp columns carry the repo's `_utc` suffix:** `computed_at_utc`, `official_at_utc`, `created_at_utc`. Property names stay `ComputedAt`, `OfficialAt`, `CreatedAt`. Domain code writes offset 0 through `TimeProvider.GetUtcNow()`.
5. **Rebuild uses in-place updates** (`backend-api`) for `series_survivor_outcomes` and `series_positions` (composite keys cannot delete and re-add the same key in one save). Field entries update in place within a Stage and delete or insert otherwise. Test group 8 must prove a Backfill entry becoming a Qualifying entry for the same bowler in one save. Fallback: a two-save transaction inside `CreateExecutionStrategy().ExecuteAsync`.
6. **`Version` must change on every output or status write**, including writes that touch only child rows, or EF skips the token. Every advancement mutator and `ReplaceConfiguration` bumps it (not Rename, logo or Finale reschedule). A domain test per mutator proves a bump on a real change and none on a no-op. A no-op recompute writes nothing.
7. **Names:** the feature registration class is `SeriesFeatureConfiguration` (`SeriesConfiguration` is the EF configuration). `SeriesFieldEntryId` and `StageOverrideId` need lines in `AppDbContext.ConfigureConventions`. Read responses carry `BowlerName` (public data; logs carry `BowlerId` only).
8. **Choices taken as the drafts recommended:** an override Add does not fill a vacant slot (`VacantSlotCount` counts entries with `ViaOverride` false); Backfill candidates need points above zero; `Stale` is sticky (cleared only by a forced recompute or a recompute of a Computed Stage whose output changed); `(series_id, position)` index stays non-unique; `ResolveSquad` stays private on `Series` and the stats-event job calls `Advance`.
9. **Phase 1 code changes beyond #259:** `SeriesStage.Update` keeps `SquadId` when date and time are unchanged; `StageTournamentFacts` gains a required `Status` (and `StageTournamentFactsFactory` the field); `EditSeriesCommandHandler` also reads the stored Stages' tournaments so a cancelled Stage can be removed on a locked Series; `SyncTournamentResultsJob` also calls `PublishAndClear` after the save, in case EF skips `SavedChanges` on a no-row re-sync.
10a. **Publisher is the only enqueue path (#56 constraint).** Add an architecture test that no class outside `DomainEventPublisher` calls `IBackgroundJobClient` to enqueue an `IDomainEventJob<T>` (and that there is one handler per event type). Do not add outbox or inbox code in this plan.
10. **Group 16 is the synthetic scenario** (see Decisions). Any "blocked on 2025 data" wording in the drafts is out of date.
11. **Not verified (nothing was compiled or run):** whether C# 14 `extension<T>` generic receivers compile as written in `SeriesRequestRules`; whether `Send.NotFoundAsync` converts as a method group; the `SquadExists` correlated `Any` and the optional complex property projection in `SeriesAdvancementReader` (fallback in the draft); EF's unique-index ordering for cross-source field entry moves; whether the generated migration matches `AddSeries` in style (stop if `dotnet ef` warns about tool skew); the `series_stages` check constraints need every existing row at `status = 0` (check staging and production first); the `CollectionResponse` item property name; `StatsEligible` as the mapping of legacy `YearlyStatEligible`.

### Database layer (`efcore` draft)

#### 0. Plan items that do not fit the code

1. **Five flat queries is six.** `Version` and `BackfillAsOf` live on the Series row, and the endpoint needs a 404 check. I read one header row, then five child queries. Every other plan item holds.
2. **Torn read across those six statements.** A job can commit between them, so the page could show stages from one version and entries from the next. The reader re-reads `Version` at the end and retries (3 tries). This only works if every change to output or status bumps `Version`. The write path is safe either way, because the page sends the first `Version` it read, so a mixed view just yields a 409.
3. **`Version` must change on every write, including a Stage-only or child-only write.** EF checks the token only when the `series` row is in the UPDATE. `MakeDueOfficial`, stale marking, `ResolveSquad`/`PickSquad`, override add/remove, a rebuild and `ReplaceConfiguration` all touch other tables. If a mutator skips the bump, that write is not checked and a race loses an update silently. This rule belongs to backend-api. Add a domain test per mutator (it changes `Version` on a real change, not on a no-op). A `SaveChangesInterceptor` that bumps it automatically is the alternative.
4. **"bowler_id FK Restrict so overrides survive a bowler delete" does not do that.** Restrict blocks the bowler delete. No bowler delete path exists today (I searched). I used Restrict (NoAction in EF terms) on all four bowler FKs. A bowler in official output must not vanish or cascade away. `TournamentResult` cascades from bowler, so a future bowler delete fails loudly on these tables. Confirm that is the intent.
5. **`SeriesFieldEntryId` is missing from the plan's new-type list.** The plan gives `series_field_entries` a ULID alternate key but no ID type. I added `SeriesFieldEntryId`. Alternative: drop the alternate key and use the natural key `(series_id, bowler_id)` as the primary key, as `BowlerSeasonStats` does. Both new IDs (`SeriesFieldEntryId`, `StageOverrideId`) need a line in `AppDbContext.ConfigureConventions` (section 3).
6. **Timestamp column names.** The plan says `computed_at` and `official_at`. The repo suffixes `_utc` (`official_at_utc`, `last_updated_utc`). I used `computed_at_utc`, `official_at_utc` and `created_at_utc`. The C# property names stay `ComputedAt` and `OfficialAt`. Say so if you want the plan's names. Domain code must write `DateTimeOffset` with offset 0 (use `TimeProvider.GetUtcNow()`), because Npgsql throws on other offsets.
7. **`BowlerMembership.IsValidOn(date)` checks only `date <= EndDate`.** The plan's rule is `StartDate <= date <= EndDate`. The reader applies both bounds in SQL and does not call `IsValidOn`. For a past as-of date, a membership that starts later must not count.
8. **"Skip legacy System Import" has nothing to skip.** `BowlerMembershipSyncJob` and the weekly sync never carry id 8 to the website. No filter in the reader.
9. **The plan's GET field list names no bowler names.** The page cannot show entries, overrides or outcomes with only `BowlerId`. The readers project `Name BowlerName` through the bowler nav, as `GetSeasonStatsQueryHandler` does. Logs still carry only `BowlerId`.
10. **Delete and rebuild under unique indexes.** EF orders a delete before an insert on a unique-index clash within one `SaveChanges` (group 8 tests it). If EF reports a circular dependency, the rebuild must delete, `SaveChanges`, insert, `SaveChanges`. The context uses `EnableRetryOnFailure`, so an explicit transaction must run inside `db.Database.CreateExecutionStrategy().ExecuteAsync(...)`. `DbUpdateConcurrencyException` is not retried by that strategy, which matches the plan.
11. **Positions index.** The plan says a plain index `(series_id, position)`. Since ties break by `BowlerId`, positions are unique per Series, so a unique index would work as a backstop. I kept it plain as confirmed. Say the word and I change it.
12. **The squad time zone conversion stays with backend-api.** I projected `BowlingDateTimeUtc` only, so the reader needs no conversion and does not call `AT TIME ZONE`. The plan puts conversion inside `StageSquadFactsReader`. In this draft the facts builder converts after the read, and `StageSquadsReader.ReadForTournamentAsync` is the shared source.
13. **`DeleteSeriesCommandHandler` needs no change.** It only deletes unlocked Series, which hold no output. DB cascade covers the new tables.
14. **No migration is needed for `SquadDesignation.SquadId`.** `squad_id char(26)` already exists in `AddSeries`.
15. **Blank reason.** The plan says "reason up to 500", not "non-blank". I made the column required with max length 500 and added no blank check. Backend-api decides.

#### 1. Entity shapes (`src/Neba.Api/Features/Series/Domain/`)

Shape only. Each `Create` is internal and written by backend-api.

```csharp
// SeriesFieldEntryId.cs
using StronglyTypedIds;

namespace Neba.Api.Features.Series.Domain;

/// <summary>Unique identifier for a Series Field Entry.</summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SeriesFieldEntryId;
```

```csharp
// StageOverrideId.cs
using StronglyTypedIds;

namespace Neba.Api.Features.Series.Domain;

/// <summary>Unique identifier for a Stage Override.</summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct StageOverrideId;
```

```csharp
// SeriesFieldEntry.cs
using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>A bowler in the Series Field, and where the bowler came from.</summary>
public sealed class SeriesFieldEntry
{
    /// <summary>Unique ID.</summary>
    public required SeriesFieldEntryId Id { get; init; }

    /// <summary>The bowler.</summary>
    public required BowlerId BowlerId { get; init; }

    // EF-only navigation for the real foreign key (same pattern as TournamentResult.Bowler).
    internal Bowler Bowler { get; init; } = null!;

    /// <summary>The Qualifying Stage that produced the entry. Null means the Backfill.</summary>
    public SeriesStageId? SourceStageId { get; private set; }

    /// <summary>Place in the source Stage's tournament. Null for a Backfill entry.</summary>
    public int? Place { get; private set; }

    /// <summary>Open Bowler of the Year points that earned a Backfill entry. Null for a Stage entry.</summary>
    public int? BackfillPoints { get; private set; }

    /// <summary>True when an Add override put the bowler in the field.</summary>
    public bool ViaOverride { get; private set; }

    private SeriesFieldEntry()
    { }

    // internal static ErrorOr<SeriesFieldEntry> Create(...): backend-api.
}
```

```csharp
// SurvivorOutcome.cs
using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>One bowler's Survivor Round result. Owned by a Survivor Stage.</summary>
public sealed class SurvivorOutcome
{
    /// <summary>The bowler.</summary>
    public required BowlerId BowlerId { get; init; }

    internal Bowler Bowler { get; init; } = null!;

    /// <summary>The bowler's Block Total, or null when the bowler has no full block.</summary>
    public int? BlockTotal { get; private set; }

    /// <summary>True when the bowler advanced.</summary>
    public bool Advanced { get; private set; }

    private SurvivorOutcome()
    { }
}
```

```csharp
// SeriesPosition.cs
using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>One bowler's Placement Round position. Owned by the Series.</summary>
public sealed class SeriesPosition
{
    /// <summary>The bowler.</summary>
    public required BowlerId BowlerId { get; init; }

    internal Bowler Bowler { get; init; } = null!;

    /// <summary>1-based position. Ties break by bowler ID.</summary>
    public int Position { get; private set; }

    /// <summary>True when the bowler bowled the Placement Stage.</summary>
    public bool Bowled { get; private set; }

    /// <summary>True when the bowler is a Finalist. Null when the Series has no Finale.</summary>
    public bool? IsFinalist { get; private set; }

    private SeriesPosition()
    { }
}
```

```csharp
// OverrideKind.cs (values mirror a new Contracts OverrideKindValues, as StageKind does)
using Ardalis.SmartEnum;

namespace Neba.Api.Features.Series.Domain;

/// <summary>What a Stage Override does.</summary>
public sealed class OverrideKind
    : SmartEnum<OverrideKind>
{
    /// <summary>Adds a bowler to the field.</summary>
    public static readonly OverrideKind Add = new(nameof(Add), 1);

    /// <summary>Clears a slot and bars the bowler from the Backfill.</summary>
    public static readonly OverrideKind Clear = new(nameof(Clear), 2);

    private OverrideKind(string name, int value)
        : base(name, value)
    { }
}
```

```csharp
// StageOverride.cs
using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>An admin change to a Qualifying Stage's advancers. Survives every recompute until removed.</summary>
public sealed class StageOverride
{
    /// <summary>Unique ID.</summary>
    public required StageOverrideId Id { get; init; }

    /// <summary>The bowler added or cleared.</summary>
    public required BowlerId BowlerId { get; init; }

    internal Bowler Bowler { get; init; } = null!;

    /// <summary>Add or Clear.</summary>
    public OverrideKind Kind { get; private set; } = OverrideKind.Add;

    /// <summary>Why the admin made the change. Up to 500 characters.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>The admin's user ID (ICurrentUserService), not a name. No FK: it lives in the security context.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>When the admin made the change (offset 0).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>False when the override's target changed and it no longer takes effect.</summary>
    public bool Applied { get; private set; }

    private StageOverride()
    { }
}
```

Edits to existing types, as the plan lists them (`Series` becomes `sealed partial`):

```csharp
// Series: add
public DateOnly? BackfillAsOf { get; private set; }
public int Version { get; private set; }

private readonly List<SeriesFieldEntry> _fieldEntries = [];
public IReadOnlyCollection<SeriesFieldEntry> FieldEntries => _fieldEntries.AsReadOnly();

private readonly List<SeriesPosition> _positions = [];
public IReadOnlyCollection<SeriesPosition> Positions => _positions.AsReadOnly();

// SeriesStage: add
public bool IsStale { get; private set; }
public DateTimeOffset? ComputedAt { get; private set; }
public DateTimeOffset? OfficialAt { get; private set; }   // must be set to null on Official -> Computed (the check constraint enforces it)
public int? SlotCount { get; private set; }

private readonly List<SurvivorOutcome> _survivorOutcomes = [];
public IReadOnlyCollection<SurvivorOutcome> SurvivorOutcomes => _survivorOutcomes.AsReadOnly();

private readonly List<StageOverride> _overrides = [];
public IReadOnlyCollection<StageOverride> Overrides => _overrides.AsReadOnly();
```

The `List<T>` backing field with a getter-only property follows `Series._stages`, so no `ICollection.Add` regression test is needed (`docs/learnings/backend.md`).

#### 2. Changed configuration fragments

`SeriesConfiguration.cs`: add after the `Stages` navigation:

```csharp
// date, null until the last Qualifying Stage computes.
builder.Property(series => series.BackfillAsOf);

// Optimistic concurrency token. An int, not xmin, because xmin does not change when only child rows change.
// Every advancement mutator bumps it in the domain (see plan item 3). The default fills the existing rows.
builder.Property(series => series.Version)
    .HasDefaultValue(0)
    .IsRequired()
    .IsConcurrencyToken();

builder.HasMany(series => series.FieldEntries)
    .WithOne()
    .HasForeignKey(ForeignKeyName)
    .OnDelete(DeleteBehavior.Cascade);

builder.Navigation(series => series.FieldEntries)
    .HasField("_fieldEntries")
    .UsePropertyAccessMode(PropertyAccessMode.Field);

builder.HasMany(series => series.Positions)
    .WithOne()
    .HasForeignKey(ForeignKeyName)
    .OnDelete(DeleteBehavior.Cascade);

builder.Navigation(series => series.Positions)
    .HasField("_positions")
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

`SeriesStageConfiguration.cs`: add a constant, replace the `ToTable` line, and add the properties and navigations:

```csharp
internal const string ForeignKeyName = "series_stage_id";

// in Configure, replacing builder.ToTable("series_stages", AppDbContext.DefaultSchema):
builder.ToTable("series_stages", AppDbContext.DefaultSchema, table =>
{
    // Status 0 Pending, 1 Computed, 2 Official. A forced recompute that resets Official to Computed must clear official_at_utc.
    table.HasCheckConstraint(
        "ck_series_stages_status_timestamps",
        $"(status = {StageStatus.Pending.Value} AND computed_at_utc IS NULL AND official_at_utc IS NULL) OR " +
        $"(status = {StageStatus.Computed.Value} AND computed_at_utc IS NOT NULL AND official_at_utc IS NULL) OR " +
        $"(status = {StageStatus.Official.Value} AND computed_at_utc IS NOT NULL AND official_at_utc IS NOT NULL)");

    // A Placement Stage is never official.
    table.HasCheckConstraint(
        "ck_series_stages_placement_never_official",
        $"NOT (kind = {StageKindValues.Placement} AND status = {StageStatus.Official.Value})");

    table.HasCheckConstraint(
        "ck_series_stages_slot_count_not_negative",
        "slot_count IS NULL OR slot_count >= 0");
});

// after the SquadDesignation block:
builder.Property(stage => stage.IsStale)
    .HasDefaultValue(false)   // safe: false is the CLR default too. Do NOT use HasDefaultValue(true) on a bool (EF would drop a false write).
    .IsRequired();

builder.Property(stage => stage.ComputedAt)       // timestamptz null
    .HasColumnName("computed_at_utc");

builder.Property(stage => stage.OfficialAt)      // timestamptz null
    .HasColumnName("official_at_utc");

builder.Property(stage => stage.SlotCount);       // int null

builder.HasMany(stage => stage.SurvivorOutcomes)
    .WithOne()
    .HasForeignKey(ForeignKeyName)
    .OnDelete(DeleteBehavior.Cascade);

builder.Navigation(stage => stage.SurvivorOutcomes)
    .HasField("_survivorOutcomes")
    .UsePropertyAccessMode(PropertyAccessMode.Field);

builder.HasMany(stage => stage.Overrides)
    .WithOne()
    .HasForeignKey(ForeignKeyName)
    .OnDelete(DeleteBehavior.Cascade);

builder.Navigation(stage => stage.Overrides)
    .HasField("_overrides")
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

Add `using Neba.Api.Contracts.Series;` for `StageKindValues`.

#### 3. `AppDbContext` edits

```csharp
// OnModelCreating, after SeriesStageConfiguration:
modelBuilder.ApplyConfiguration(new SeriesFieldEntryConfiguration());
modelBuilder.ApplyConfiguration(new SeriesSurvivorOutcomeConfiguration());
modelBuilder.ApplyConfiguration(new SeriesPositionConfiguration());
modelBuilder.ApplyConfiguration(new StageOverrideConfiguration());

// ConfigureConventions, after SeriesStageId:
configurationBuilder.Properties<SeriesFieldEntryId>()
    .HaveConversion<UlidTypedIdConverter<SeriesFieldEntryId>>();

configurationBuilder.Properties<StageOverrideId>()
    .HaveConversion<UlidTypedIdConverter<StageOverrideId>>();
```

No `DbSet`s. Readers reach the children through the `Series` navigations.

#### 4. New configuration classes (`src/Neba.Api/Database/Configurations/`)

```csharp
// SeriesFieldEntryConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class SeriesFieldEntryConfiguration
    : IEntityTypeConfiguration<SeriesFieldEntry>
{
    public void Configure(EntityTypeBuilder<SeriesFieldEntry> builder)
    {
        builder.ToTable("series_field_entries", AppDbContext.DefaultSchema, table =>
        {
            // A Stage entry has a source Stage and a place and no points. A Backfill entry has points and no source or place.
            // Overrides apply to Qualifying Stages only, so a Backfill entry is never via_override.
            table.HasCheckConstraint(
                "ck_series_field_entries_source_shape",
                "(source_stage_id IS NOT NULL AND place IS NOT NULL AND backfill_points IS NULL) OR " +
                "(source_stage_id IS NULL AND place IS NULL AND backfill_points IS NOT NULL AND via_override = FALSE)");

            table.HasCheckConstraint("ck_series_field_entries_place_positive", "place IS NULL OR place > 0");

            table.HasCheckConstraint(
                "ck_series_field_entries_backfill_points_not_negative",
                "backfill_points IS NULL OR backfill_points >= 0");
        });

        builder.ConfigureShadowId();

        builder.Property(entry => entry.Id)
            .IsUlid();

        builder.HasAlternateKey(entry => entry.Id);

        builder.Property<int>(SeriesConfiguration.ForeignKeyName)   // series_id int -> series.id, cascade (set in SeriesConfiguration)
            .IsRequired();

        builder.Property(entry => entry.BowlerId)
            .IsUlid(BowlerConfiguration.ForeignKeyName)             // char(26) bowler_id
            .IsRequired();

        builder.HasOne(entry => entry.Bowler)
            .WithMany()
            .HasForeignKey(entry => entry.BowlerId)
            .HasPrincipalKey(bowler => bowler.Id)
            .OnDelete(DeleteBehavior.Restrict);

        // Nullable typed ID: IsUlid needs a non-nullable struct, so configure the column directly (backend.md).
        builder.Property(entry => entry.SourceStageId)
            .HasColumnName("source_stage_id")                       // char(26) null: null means Backfill
            .HasMaxLength(26)
            .IsFixedLength();

        // NoAction, not Restrict: a Series delete cascades to both the Stages and these rows, and NoAction checks at
        // statement end. Only Pending (outputless) Stages can be removed anyway, so this is a loud backstop.
        builder.HasOne<SeriesStage>()
            .WithMany()
            .HasForeignKey(entry => entry.SourceStageId)
            .HasPrincipalKey(stage => stage.Id)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(entry => entry.Place);                     // int null
        builder.Property(entry => entry.BackfillPoints);            // int null

        builder.Property(entry => entry.ViaOverride)                // bool not null, no default
            .IsRequired();

        // A bowler is in a Series Field once. Also covers the series_id foreign key.
        builder.HasIndex(SeriesConfiguration.ForeignKeyName, nameof(SeriesFieldEntry.BowlerId))
            .IsUnique();
    }
}
```

```csharp
// SeriesSurvivorOutcomeConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class SeriesSurvivorOutcomeConfiguration
    : IEntityTypeConfiguration<SurvivorOutcome>
{
    public void Configure(EntityTypeBuilder<SurvivorOutcome> builder)
    {
        builder.ToTable("series_survivor_outcomes", AppDbContext.DefaultSchema, table =>
            table.HasCheckConstraint(
                "ck_series_survivor_outcomes_block_total_not_negative",
                "block_total IS NULL OR block_total >= 0"));

        builder.Property<int>(SeriesStageConfiguration.ForeignKeyName)   // series_stage_id int -> series_stages.id, cascade
            .IsRequired();

        builder.Property(outcome => outcome.BowlerId)
            .IsUlid(BowlerConfiguration.ForeignKeyName)
            .IsRequired();

        // Composite key, no surrogate id (precedent: BowlerSeasonStats).
        builder.HasKey(SeriesStageConfiguration.ForeignKeyName, nameof(SurvivorOutcome.BowlerId));

        builder.HasOne(outcome => outcome.Bowler)
            .WithMany()
            .HasForeignKey(outcome => outcome.BowlerId)
            .HasPrincipalKey(bowler => bowler.Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(outcome => outcome.BowlerId);                   // bowler delete and merge lookups

        builder.Property(outcome => outcome.BlockTotal);                 // int null
        builder.Property(outcome => outcome.Advanced)                    // bool not null
            .IsRequired();
    }
}
```

```csharp
// SeriesPositionConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class SeriesPositionConfiguration
    : IEntityTypeConfiguration<SeriesPosition>
{
    public void Configure(EntityTypeBuilder<SeriesPosition> builder)
    {
        builder.ToTable("series_positions", AppDbContext.DefaultSchema, table =>
            table.HasCheckConstraint("ck_series_positions_position_positive", "position > 0"));

        builder.Property<int>(SeriesConfiguration.ForeignKeyName)        // series_id int -> series.id, cascade
            .IsRequired();

        builder.Property(position => position.BowlerId)
            .IsUlid(BowlerConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasKey(SeriesConfiguration.ForeignKeyName, nameof(SeriesPosition.BowlerId));

        builder.HasOne(position => position.Bowler)
            .WithMany()
            .HasForeignKey(position => position.BowlerId)
            .HasPrincipalKey(bowler => bowler.Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(position => position.BowlerId);

        builder.Property(position => position.Position)                  // int not null
            .IsRequired();

        builder.Property(position => position.Bowled)                    // bool not null
            .IsRequired();

        builder.Property(position => position.IsFinalist);               // bool null

        builder.HasIndex(SeriesConfiguration.ForeignKeyName, nameof(SeriesPosition.Position));
    }
}
```

```csharp
// StageOverrideConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class StageOverrideConfiguration
    : IEntityTypeConfiguration<StageOverride>
{
    internal const int ReasonMaxLength = 500;
    internal const int CreatedByMaxLength = 64;

    public void Configure(EntityTypeBuilder<StageOverride> builder)
    {
        builder.ToTable("series_stage_overrides", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(stageOverride => stageOverride.Id)
            .IsUlid();

        builder.HasAlternateKey(stageOverride => stageOverride.Id);

        builder.Property<int>(SeriesStageConfiguration.ForeignKeyName)   // series_stage_id int -> series_stages.id, cascade
            .IsRequired();

        builder.Property(stageOverride => stageOverride.BowlerId)
            .IsUlid(BowlerConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne(stageOverride => stageOverride.Bowler)
            .WithMany()
            .HasForeignKey(stageOverride => stageOverride.BowlerId)
            .HasPrincipalKey(bowler => bowler.Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(stageOverride => stageOverride.Kind)            // SmartEnum stored as int by value
            .IsRequired();

        builder.Property(stageOverride => stageOverride.Reason)
            .HasMaxLength(ReasonMaxLength)
            .IsRequired();

        builder.Property(stageOverride => stageOverride.CreatedBy)       // user id string, no FK (security context)
            .HasMaxLength(CreatedByMaxLength)
            .IsRequired();

        builder.Property(stageOverride => stageOverride.CreatedAt)       // timestamptz not null
            .HasColumnName("created_at_utc")
            .IsRequired();

        // No HasDefaultValue(true): false is the CLR default, so EF would skip writing false and the default would win.
        builder.Property(stageOverride => stageOverride.Applied)
            .IsRequired();

        builder.HasIndex(SeriesStageConfiguration.ForeignKeyName, nameof(StageOverride.BowlerId))
            .IsUnique();
    }
}
```

Generated names stay under Postgres's 63-byte limit. The longest are about 57 (`fk_series_survivor_outcomes_series_stages_series_stage_id`). Check them in the migration.

#### 5. Version concurrency token

- The wiring is the `Version` block in section 2. EF emits `UPDATE series SET version = @new ... WHERE id = @id AND version = @old`. `ExecuteUpdate` and `ExecuteDelete` bypass the token, so output rebuilds use tracked deletes.
- The existing `AddSeries` rows get `DEFAULT 0` from the migration. EF reads the default back on insert, which is harmless.
- Handler rules for backend-api: catch `DbUpdateConcurrencyException`. A job rethrows (idempotent retry). An admin write returns 409. A concurrency failure also happens when another writer already deleted a row you delete (0 rows affected). After it, drop the context (it holds stale tracked state).
- Test: two contexts load the Series, both bump `Version`, the second `SaveChangesAsync` throws. A domain test per mutator proves the bump (plan item 3).

#### 6. Migration plan

Name `AddSeriesAdvancement`, newest existing is `20261117182308_AddSeries`.

```bash
cd /Users/kippermand/Projects/bowlneba/neba-website/src/Neba.Api
export ConnectionStrings__bowlneba="Host=localhost;Database=dummy;Username=dummy;Password=dummy"
dotnet ef migrations add AddSeriesAdvancement --context AppDbContext --output-dir Database/Migrations
```

Files should land in `src/Neba.Api/Database/Migrations/`, next to `AddSeries`. This matches the existing layout, but I did not run `add`. Stop and report if `dotnet ef` warns about the 11.x tool against 10.x packages, or if the generated output looks unlike `AddSeries`. Look for unrelated `AlterColumn` calls as the sign of skew.

Review the generated file line by line:

- `series`: `AddColumn<DateOnly> backfill_as_of` nullable. `AddColumn<int> version` with `nullable: false, defaultValue: 0`. A missing default would fail on populated rows.
- `series_stages`: `is_stale` bool `defaultValue: false`; `computed_at_utc` and `official_at_utc` `timestamp with time zone` nullable; `slot_count` int nullable.
- The three `AddCheckConstraint` calls come after the column adds. They validate existing rows: all must be `status = 0` with null timestamps. Before shipping, run `SELECT status, count(*) FROM app.series_stages GROUP BY 1` on staging and production. A non-zero status makes the migration fail loudly.
- No `DropColumn`, `RenameColumn` or `AlterColumn` anywhere. Existing tables get no change beyond the six columns and three checks.
- New tables: PK names; `ak_*_domain_id` on field entries and overrides; composite PKs on outcomes and positions; the unique indexes; the `(series_id, position)` index; the `bowler_id` indexes.
- Delete behaviors in the FKs: `series_id` and `series_stage_id` carry `ReferentialAction.Cascade`. Every `bowler_id` carries `Restrict`. `source_stage_id` carries no `onDelete` argument (NoAction).
- Check constraint text matches section 4 and the status numbers (0, 1, 2) and Placement (3).
- `Down` drops the four tables, then the checks, then the columns, in dependency order.
- The snapshot diff shows only these changes. Never hand-edit the snapshot.
- This is additive with no backfill, and it cannot ship through `hotfix.yml` (hotfixes never run migrations). `Neba.MigrationService` applies it.

#### 7. Tests outline (migration and configuration)

`tests/Neba.Api.Tests/Database/Migrations/AddSeriesAdvancementMigrationTests.cs`, copied from `AddArticleAnnouncedAtMigrationTests`:

```csharp
[IntegrationTest]
[Component("Series")]
public sealed class AddSeriesAdvancementMigrationTests(AppDbContextFixture fixture)
    : IClassFixture<AppDbContextFixture>
{
    private const string PreviousMigration = "20261117182308_AddSeries";

    [Fact(DisplayName = "Up should give existing Series and Stages the default advancement values")]
    public async Task Up_ShouldDefaultExistingRows()
    {
        // Arrange: migrate to PreviousMigration, then insert a season, a tournament, a series and a Pending stage with raw SQL.
        //          (I did not list the NOT NULL columns of seasons and tournaments; copy them from AddSeries.Designer.cs and
        //          the later tournament migrations. EF entities cannot insert here: the model already has the new columns.)
        // Act:     await migrator.MigrateAsync(cancellationToken: ct);
        // Assert:  series.version == 0, series.backfill_as_of IS NULL;
        //          stage.is_stale == false, computed_at_utc / official_at_utc / slot_count IS NULL, status == 0.
    }

    [Fact(DisplayName = "The status check should reject a Computed Stage with no computed_at_utc")]
    public async Task StatusCheck_ShouldReject_ComputedWithoutTimestamp()
    {
        // Act & Assert: UPDATE app.series_stages SET status = 1  -> PostgresException, SqlState "23514".
        // Repeat: status = 2 with only computed_at_utc set; kind = 3 (Placement) with status = 2 and both timestamps set.
        // Raw SQL is not wrapped by the exception processor, so assert on PostgresException.SqlState.
    }
}
```

`SeriesConfigurationTests` and `SeriesStageConfigurationTests` get new cases:
- `Version` is a concurrency token, required, default 0.
- `FieldEntries` and `Positions` cascade from Series; `SurvivorOutcomes` and `Overrides` cascade from Stage.
- `IsStale` is required; the `computed_at_utc` / `official_at_utc` columns exist.
- A real-database case for the status check, copying the `ExecuteSqlRawAsync` plus `Should.ThrowAsync<Exception>` pattern used there for `ck_series_finale_all_or_none`.
- Add a two-context `Version` race case.

One new config test class per new entity, copied from `SeriesStageConfigurationTests`, asserting:
- Table and column names, nullability and max lengths.
- Composite keys, and the unique indexes `(series_id, bowler_id)` and `(series_stage_id, bowler_id)`.
- The FK delete behaviors: bowler Restrict, `source_stage_id` NoAction, series and stage Cascade.
- The check constraints: insert one violating row each through SQL.
- Cascade: delete a Series in a real database and see all four tables empty.
- `Applied = false` and `ViaOverride = false` round-trip (guards the bool-default trap).

Model inventory test, so the future merge flow cannot miss a column:

```csharp
[Fact(DisplayName = "Every foreign key to Bowler in the model is known")]
public async Task Model_ShouldListEveryBowlerForeignKey()
{
    // Arrange
    using var context = fixture.CreateDbContext();

    // Act
    var inventory = context.Model.GetEntityTypes()
        .SelectMany(entity => entity.GetForeignKeys()
            .Where(fk => fk.PrincipalEntityType.ClrType == typeof(Bowler))
            .Select(fk => $"{entity.GetTableName()}.{fk.Properties.Single().GetColumnName()}"))
        .Order()
        .ToList();

    // Assert
    await Verify(inventory);   // first run writes the .verified file. It must list the four series_* tables.
}
```

#### 8. Read queries (`src/Neba.Api/Features/Series/`, namespace `Neba.Api.Features.Series`)

All use `AsNoTracking` and project. None caches. Rows are internal records that hold SmartEnums (uncached). The endpoints map `.Name` and their own DTOs.

```csharp
// SeriesAdvancementReader.cs
using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series;

internal sealed record AdvancementHeaderRow(int Version, DateOnly? BackfillAsOf);

internal sealed record AdvancementStageRow(
    SeriesStageId StageId,
    int Order,
    StageKind Kind,
    StageStatus Status,
    int? AdvancingCount,
    DateOnly? SquadDate,
    TimeOnly? SquadStartTime,
    SquadId? SquadId,
    bool SquadExists,
    bool IsStale,
    DateTimeOffset? ComputedAt,
    DateTimeOffset? OfficialAt,
    int? SlotCount,
    TournamentId TournamentId,
    string TournamentName,
    TournamentStatus TournamentStatus);

internal sealed record AdvancementFieldEntryRow(
    SeriesFieldEntryId EntryId, BowlerId BowlerId, Name BowlerName,
    SeriesStageId? SourceStageId, int? Place, int? BackfillPoints, bool ViaOverride);

internal sealed record AdvancementSurvivorOutcomeRow(
    SeriesStageId StageId, BowlerId BowlerId, Name BowlerName, int? BlockTotal, bool Advanced);

internal sealed record AdvancementPositionRow(
    BowlerId BowlerId, Name BowlerName, int Position, bool Bowled, bool? IsFinalist);

internal sealed record AdvancementOverrideRow(
    StageOverrideId OverrideId, SeriesStageId StageId, BowlerId BowlerId, Name BowlerName,
    OverrideKind Kind, string Reason, string CreatedBy, DateTimeOffset CreatedAt, bool Applied);

internal sealed record SeriesAdvancementRows(
    AdvancementHeaderRow Header,
    IReadOnlyList<AdvancementStageRow> Stages,
    IReadOnlyList<AdvancementFieldEntryRow> FieldEntries,
    IReadOnlyList<AdvancementSurvivorOutcomeRow> SurvivorOutcomes,
    IReadOnlyList<AdvancementPositionRow> Positions,
    IReadOnlyList<AdvancementOverrideRow> Overrides);

internal static class SeriesAdvancementReader
{
    private const int MaxAttempts = 3;

    /// <summary>Null when the Series does not exist. Retries when a write lands between the queries.</summary>
    public static async Task<SeriesAdvancementRows?> ReadAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var header = await ReadHeaderAsync(appDbContext, seriesId, cancellationToken);
            if (header is null)
            {
                return null;
            }

            var rows = new SeriesAdvancementRows(
                header,
                await ReadStagesAsync(appDbContext, seriesId, cancellationToken),
                await ReadFieldEntriesAsync(appDbContext, seriesId, cancellationToken),
                await ReadSurvivorOutcomesAsync(appDbContext, seriesId, cancellationToken),
                await ReadPositionsAsync(appDbContext, seriesId, cancellationToken),
                await ReadOverridesAsync(appDbContext, seriesId, cancellationToken));

            // Every output or status change bumps Version, so an unchanged Version means no write landed in between.
            // On the last attempt return the first Version: a write the page then sends gets a 409, which is safe.
            var after = await ReadHeaderAsync(appDbContext, seriesId, cancellationToken);
            if (after?.Version == header.Version || attempt == MaxAttempts)
            {
                return rows;
            }
        }
    }

    private static Task<AdvancementHeaderRow?> ReadHeaderAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
        => appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == seriesId)
            .Select(series => new AdvancementHeaderRow(series.Version, series.BackfillAsOf))
            .SingleOrDefaultAsync(cancellationToken);

    private static async Task<IReadOnlyList<AdvancementStageRow>> ReadStagesAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
        => await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == seriesId)
            .SelectMany(series => series.Stages)
            .Join(appDbContext.Tournaments.AsNoTracking(), stage => stage.TournamentId, tournament => tournament.Id,
                (stage, tournament) => new { stage, tournament })
            .OrderBy(x => x.stage.Order)
            .Select(x => new AdvancementStageRow(
                x.stage.Id,
                x.stage.Order,
                x.stage.Kind,
                x.stage.Status,
                x.stage.AdvancingCount,
                x.stage.SquadDesignation == null ? (DateOnly?)null : x.stage.SquadDesignation.Date,
                x.stage.SquadDesignation == null ? (TimeOnly?)null : x.stage.SquadDesignation.StartTime,
                x.stage.SquadDesignation == null ? null : x.stage.SquadDesignation.SquadId,
                // No FK on squad_id, so a vanished squad shows here as false (feeds "Needs squad").
                x.stage.SquadDesignation != null
                    && x.tournament.Squads.Any(squad => squad.Id == x.stage.SquadDesignation.SquadId),
                x.stage.IsStale,
                x.stage.ComputedAt,
                x.stage.OfficialAt,
                x.stage.SlotCount,
                x.tournament.Id,
                x.tournament.Name,
                x.tournament.Status))
            .ToListAsync(cancellationToken);

    private static async Task<IReadOnlyList<AdvancementFieldEntryRow>> ReadFieldEntriesAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
        => await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == seriesId)
            .SelectMany(series => series.FieldEntries)
            .Select(entry => new AdvancementFieldEntryRow(
                entry.Id, entry.BowlerId, entry.Bowler.Name,
                entry.SourceStageId, entry.Place, entry.BackfillPoints, entry.ViaOverride))
            .ToListAsync(cancellationToken);

    private static async Task<IReadOnlyList<AdvancementSurvivorOutcomeRow>> ReadSurvivorOutcomesAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
        => await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == seriesId)
            .SelectMany(series => series.Stages)
            .SelectMany(stage => stage.SurvivorOutcomes, (stage, outcome) => new AdvancementSurvivorOutcomeRow(
                stage.Id, outcome.BowlerId, outcome.Bowler.Name, outcome.BlockTotal, outcome.Advanced))
            .ToListAsync(cancellationToken);

    private static async Task<IReadOnlyList<AdvancementPositionRow>> ReadPositionsAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
        => await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == seriesId)
            .SelectMany(series => series.Positions)
            .OrderBy(position => position.Position)
            .Select(position => new AdvancementPositionRow(
                position.BowlerId, position.Bowler.Name, position.Position, position.Bowled, position.IsFinalist))
            .ToListAsync(cancellationToken);

    private static async Task<IReadOnlyList<AdvancementOverrideRow>> ReadOverridesAsync(
        AppDbContext appDbContext, SeriesId seriesId, CancellationToken cancellationToken)
        => await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == seriesId)
            .SelectMany(series => series.Stages)
            .SelectMany(stage => stage.Overrides, (stage, stageOverride) => new AdvancementOverrideRow(
                stageOverride.Id, stage.Id, stageOverride.BowlerId, stageOverride.Bowler.Name,
                stageOverride.Kind, stageOverride.Reason, stageOverride.CreatedBy, stageOverride.CreatedAt,
                stageOverride.Applied))
            .ToListAsync(cancellationToken);
}
```

Query-shape risks I could not test: the `SquadExists` correlated `Any` with a nullable typed-ID comparison, and projecting the optional complex property's `SquadId`. If either fails to translate, drop `SquadExists` and compute it from `StageSquadsReader` instead.

```csharp
// StageResultRowsReader.cs
using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Series;

internal sealed record StageResultRow(BowlerId BowlerId, Name BowlerName, int Place);

internal static class StageResultRowsReader
{
    /// <summary>Null when the Series or Stage does not exist. An empty list means the tournament has no results yet.</summary>
    public static async Task<IReadOnlyList<StageResultRow>?> ReadAsync(
        AppDbContext appDbContext, SeriesId seriesId, SeriesStageId stageId, CancellationToken cancellationToken)
    {
        var stage = await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == seriesId)
            .SelectMany(series => series.Stages)
            .Where(s => s.Id == stageId)
            .Select(s => new { s.TournamentId })
            .SingleOrDefaultAsync(cancellationToken);

        if (stage is null)
        {
            return null;
        }

        return await appDbContext.Tournaments.AsNoTracking()
            .Where(tournament => tournament.Id == stage.TournamentId)
            .SelectMany(tournament => tournament.Results)
            .OrderBy(result => result.Place)
            .ThenBy(result => result.Bowler.Name.LastName)
            .ThenBy(result => result.Bowler.Name.FirstName)
            .Select(result => new StageResultRow(result.BowlerId, result.Bowler.Name, result.Place))
            .ToListAsync(cancellationToken);
    }
}
```

```csharp
// StageSquadsReader.cs
using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series;

/// <summary>UTC instant only. The Eastern date and time conversion belongs to the facts builder (backend-api).</summary>
internal sealed record StageSquadRow(SquadId SquadId, DateTimeOffset BowlingDateTimeUtc, bool HasScores);

internal static class StageSquadsReader
{
    /// <summary>Shared by the squad facts reader and the ListStageSquads use case.</summary>
    public static async Task<IReadOnlyList<StageSquadRow>> ReadForTournamentAsync(
        AppDbContext appDbContext, TournamentId tournamentId, CancellationToken cancellationToken)
        => await appDbContext.Tournaments.AsNoTracking()
            .Where(tournament => tournament.Id == tournamentId)
            .SelectMany(tournament => tournament.Squads)
            .OrderBy(squad => squad.BowlingDateTimeUtc)
            .Select(squad => new StageSquadRow(
                squad.Id,
                squad.BowlingDateTimeUtc,
                appDbContext.SquadScores.Any(score => score.SquadId == squad.Id)))
            .ToListAsync(cancellationToken);

    /// <summary>Null when the Series or Stage does not exist.</summary>
    public static async Task<IReadOnlyList<StageSquadRow>?> ReadForStageAsync(
        AppDbContext appDbContext, SeriesId seriesId, SeriesStageId stageId, CancellationToken cancellationToken)
    {
        var stage = await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == seriesId)
            .SelectMany(series => series.Stages)
            .Where(s => s.Id == stageId)
            .Select(s => new { s.TournamentId })
            .SingleOrDefaultAsync(cancellationToken);

        return stage is null
            ? null
            : await ReadForTournamentAsync(appDbContext, stage.TournamentId, cancellationToken);
    }
}
```

```csharp
// MembersOnDateReader.cs
using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series;

internal static class MembersOnDateReader
{
    /// <summary>
    /// Bowlers with a membership that covers <paramref name="date"/> (both ends inclusive). About a thousand
    /// members, so it reads them all. The sync never carries the legacy System Import record, so nothing to skip.
    /// </summary>
    public static async Task<IReadOnlySet<BowlerId>> ReadAsync(
        AppDbContext appDbContext, DateOnly date, CancellationToken cancellationToken)
    {
        var bowlerIds = await appDbContext.Bowlers.AsNoTracking()
            .Where(bowler => bowler.Memberships.Any(membership => membership.StartDate <= date && date <= membership.EndDate))
            .Select(bowler => bowler.Id)
            .ToListAsync(cancellationToken);

        return bowlerIds.ToHashSet();
    }
}
```

```csharp
// OpenBowlerOfTheYearPointsReader.cs
using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series;

internal sealed record BowlerOfTheYearPointsRow(BowlerId BowlerId, int Points);

/// <param name="IsReady">False while a stats-eligible tournament ending on or before the date is still Scheduled or finalized without results.</param>
internal sealed record OpenBowlerOfTheYearPoints(bool IsReady, IReadOnlyList<BowlerOfTheYearPointsRow> Points);

/// <summary>
/// Open Bowler of the Year points as of a date, from website results. Mirrors the Bowler of the Year part of
/// LegacySeasonStatsCalculator.ComputeAwardPoints. Senior, Super Senior, Woman and Youth points are not needed.
/// </summary>
internal static class OpenBowlerOfTheYearPointsReader
{
    public static async Task<OpenBowlerOfTheYearPoints> ReadAsync(
        AppDbContext appDbContext, SeasonId seasonId, DateOnly asOf, CancellationToken cancellationToken)
    {
        var eligibleToDate = appDbContext.Tournaments.AsNoTracking()
            .Where(tournament => tournament.SeasonId == seasonId
                && tournament.StatsEligible
                && tournament.EndDate <= asOf);

        var stillOpen = await eligibleToDate
            .AnyAsync(tournament => tournament.Status == TournamentStatus.Scheduled, cancellationToken);

        var finalizedWithoutResults = await eligibleToDate
            .AnyAsync(tournament => (tournament.Status == TournamentStatus.Completed || tournament.Status == TournamentStatus.Truncated)
                && !tournament.Results.Any(), cancellationToken);

        // Tournament of Champions double-dip: the Non-Champions single-day winner's forced berth in the
        // Tournament of Champions earns no Bowler of the Year points. Identify both from every finalized
        // tournament in the season up to the date, not only stats-eligible ones, as the legacy code does.
        var finalizedToDate = appDbContext.Tournaments.AsNoTracking()
            .Where(tournament => tournament.SeasonId == seasonId
                && tournament.EndDate <= asOf
                && (tournament.Status == TournamentStatus.Completed || tournament.Status == TournamentStatus.Truncated));

        var nonChampionsIds = await finalizedToDate
            .Where(tournament => tournament.TournamentType == TournamentType.NonChampions
                && tournament.StartDate == tournament.EndDate)
            .Select(tournament => tournament.Id)
            .ToListAsync(cancellationToken);

        var championsIds = await finalizedToDate
            .Where(tournament => tournament.TournamentType == TournamentType.TournamentOfChampions)
            .Select(tournament => tournament.Id)
            .ToListAsync(cancellationToken);

        var excludedTournamentId = default(TournamentId);
        List<BowlerId> excludedBowlerIds = [];

        if (nonChampionsIds.Count == 1 && championsIds.Count == 1)
        {
            excludedTournamentId = championsIds[0];
            var nonChampionsId = nonChampionsIds[0];

            excludedBowlerIds = await appDbContext.Tournaments.AsNoTracking()
                .Where(tournament => tournament.Id == nonChampionsId)
                .SelectMany(tournament => tournament.Results)
                .Where(result => result.Place == 1)
                .Select(result => result.BowlerId)
                .ToListAsync(cancellationToken);
        }

        // An empty excludedBowlerIds makes the NOT clause true for every row, so no exclusion applies.
        var points = await eligibleToDate
            .Where(tournament => tournament.Status == TournamentStatus.Completed || tournament.Status == TournamentStatus.Truncated)
            .SelectMany(tournament => tournament.Results, (tournament, result) => new { TournamentId = tournament.Id, result.BowlerId, result.Points })
            .Where(row => row.TournamentId != excludedTournamentId || !excludedBowlerIds.Contains(row.BowlerId))
            .GroupBy(row => row.BowlerId)
            .Select(group => new BowlerOfTheYearPointsRow(group.Key, group.Sum(row => row.Points)))
            .OrderByDescending(row => row.Points)
            .ToListAsync(cancellationToken);

        return new OpenBowlerOfTheYearPoints(!stillOpen && !finalizedWithoutResults, points);
    }
}
```

The `OrderByDescending` ahead of a `GroupBy` projection may need a `ThenBy` on `BowlerId` for a stable order. The Backfill domain sorts anyway, so I left the tie order to it.

##### What I could not mirror from `ComputeAwardPoints`

1. **The side-cut finals bonus.** Legacy gives 5 points per finals reached through a side cut (`finalsViaSideCut * 5`) and leaves those rows' own `Points` out of the sum (`SideCut is null` only). `TournamentResult` has no `SideCut` column, and the website results sync never reads it. The reader sums every row's `Points`. A bowler with a side-cut row can differ from the legacy total. The legacy comment says current seasons never populate `SideCut`, so the gap should be nil there. The season-end parity test against `BowlerSeasonStats.BowlerOfTheYearPoints` is the only proof.
2. **`SingleOrDefault` throws in legacy.** It throws on two Non-Champions one-day events, two Tournaments of Champions, or two bowlers at Place 1. The reader applies no exclusion unless there is exactly one of each. If two bowlers share Place 1 in the Non-Champions event, it excludes both from the Tournament of Champions.
3. **Legacy has no as-of date.** It always runs at season end. The `EndDate <= asOf` filter is new, so the parity holds only when `asOf` is the season end.
4. **Eligibility flag mapping.** Legacy `YearlyStatEligible` becomes website `StatsEligible`. I assumed the legacy tournament sync maps one to the other. I did not check it.
5. **Status filter.** Legacy has no status filter, because only finalized events have result rows. The reader keeps Completed and Truncated, as `GetSeasonStatsQueryHandler` does.

##### Not covered

- The Survivor and Qualifying facts reader (`SeriesAdvancementFactsReader`) needs two more queries that I did not draft: squad score sums per bowler for a squad, and the Stage's tournament result rows with `Place`. `StageResultRowsReader` already supplies the rows and `StageSquadsReader.ReadForTournamentAsync` the squads.
- I wrote no handlers, endpoints, domain methods or `EasternTime`.

#### Files to create or change

Create under `/Users/kippermand/Projects/bowlneba/neba-website/src/Neba.Api/`:
- `Features/Series/Domain/`: `SeriesFieldEntryId.cs`, `StageOverrideId.cs`, `SeriesFieldEntry.cs`, `SurvivorOutcome.cs`, `SeriesPosition.cs`, `OverrideKind.cs`, `StageOverride.cs`.
- `Database/Configurations/`: `SeriesFieldEntryConfiguration.cs`, `SeriesSurvivorOutcomeConfiguration.cs`, `SeriesPositionConfiguration.cs`, `StageOverrideConfiguration.cs`.
- `Features/Series/`: `SeriesAdvancementReader.cs`, `StageResultRowsReader.cs`, `StageSquadsReader.cs`, `MembersOnDateReader.cs`, `OpenBowlerOfTheYearPointsReader.cs`.

Edit:
- `Features/Series/Domain/Series.cs` and `SeriesStage.cs` (the new properties and collections).
- `Database/Configurations/SeriesConfiguration.cs` and `SeriesStageConfiguration.cs`.
- `Database/AppDbContext.cs`.

Generated by `dotnet ef`: `Database/Migrations/<timestamp>_AddSeriesAdvancement.cs`, its `.Designer.cs`, and an updated `AppDbContextModelSnapshot.cs`.

Tests under `/Users/kippermand/Projects/bowlneba/neba-website/tests/Neba.Api.Tests/Database/`:
- `Migrations/AddSeriesAdvancementMigrationTests.cs`.
- The two config test classes to extend, plus four new config test classes.

### API layer (`backend-api` draft)

#### 0. Where the plan and the code disagree, and choices that are yours

##### Plan vs code

1. **`Series` is not `partial`.** It is `public sealed class Series`. Section C makes it `sealed partial`.
2. **`SeriesStage.Status` has `internal set`** ("phase 2 and test seam"). The plan says only `Series` methods change it. I keep `internal set`, because tests and factories use it, and add `MarkComputed` and `MarkOfficial`. Tighten it to `private set` in a later cleanup.
3. **`SeriesStage.Update` would wipe the resolved squad.** It rebuilds the designation with `SquadDesignation.Describe`, so any in-place update loses `SquadId`. The cancelled-Stage removal on a locked Series updates kept Stages in place. Section C fixes `Update` to keep `SquadId` when the date and time are unchanged.
4. **`StageTournamentFacts` has no tournament status.** The "remove a Pending Stage whose tournament is Cancelled while locked" rule needs it, so I add a required `Status`. `StageTournamentFactsReader` fills it. `StageTournamentFactsFactory` needs the field.
5. **`EditSeriesCommandHandler` reads facts only for submitted Stages.** The removed Stage's tournament is not submitted, so the handler must also read the stored Stages' tournaments (edit in section F).
6. **The plan's DB section gives `series_field_entries` a ULID alternate key, but the Domain type list has no ID type for it.** I add `SeriesFieldEntryId` (ulid-full). Section G lists the `efcore` shape.
7. **Name clash:** `SeriesConfiguration` already exists as the EF configuration class (`new SeriesConfiguration()` in `AppDbContext`). The feature registration class is `SeriesFeatureConfiguration`, not the plan's `SeriesConfiguration.AddSeries`. The method is still `AddSeries`.
8. **`SecurityRoleSeeder` needs no edit.** `Roles.Admin` is `Permissions.List`, which collects every static `Permissions` field. Adding the field gives Admin the permission, and Manager is not listed. Only the seeder test changes.
9. **`ResolveSquad` stays private on `Series`.** The plan makes it public, but `Advance` and `PickSquad` reach it and no outside caller needs it. `RunBackfill` is not a method. The stats-event job calls `Advance(lastQualifying.TournamentId)` and the fold does the Backfill.
10. **Pre-existing quirk, not fixed here:** `Tournament.ValidateSquadDateInRange` compares the UTC date to `StartDate` and `EndDate`. `SquadDesignation.Date` is an Eastern date. An evening squad on the last tournament day can fail the range check. Plan decision 74 covers the read side only.
11. **Possible gap in "fires on every re-sync".** A re-sync that adds no new rows leaves nothing tracked as changed. I could not confirm whether EF calls the `SavedChanges` interceptor when `SaveChanges` has nothing to write. `SyncTournamentResultsJob` therefore also calls `PublishAndClear(tournament)` after the save. It publishes nothing twice, because the interceptor clears the events first when it does fire.
12. **"Delete and rebuild" of Stage output** would hit identity conflicts on the composite keys of `series_survivor_outcomes` and `series_positions`. EF cannot delete and re-add the same key in one save. Outcomes and positions are therefore updated in place by `BowlerId`. Field entries are updated in place within a Stage and removed or added otherwise, with EF ordering deletes before inserts for the unique index. Test group 8 must prove the cross-Stage move (Backfill to Qualifying). If it fails, the fallback is a two-save transaction in `SeriesAdvancement`.

##### Choices for you (my recommendation first)

- **A. Which date judges Qualifying membership.** The plan fixes this for the Backfill (the as-of date) but not for slots.
  - **Recommend `EndDate`:** the same date as `BackfillAsOf`, and it admits a bowler who joins on tournament day.
  - Alternative: `StartDate` (stricter).
  - Code: `facts.MembersOn(tournament.EndDate)` in `Series.ComputeStage`. The facts reader must load that date.
- **B. An override Add does not fill a vacant slot.** The field may exceed the target, as the plan says. `VacantSlotCount = SlotCount - entries that filled a slot (ViaOverride false)`.
  - **Recommend keep.**
  - Alternative: Add fills the lowest vacancy, which changes the Backfill count.
- **C. Backfill candidates need points above zero.** Otherwise "ties at the cutoff all advance" admits every member when the cutoff is 0.
  - **Recommend keep.**
- **D. Responses carry `BowlerName`.** The plan says output holds `BowlerId` only. The UI picker and the field list cannot show a row without a name or a second lookup per row.
  - **Recommend names in the read responses.** They are already public data, and logs stay free of names.
  - Alternative: IDs only, and `blazor-ui` looks names up.
  - The code below includes `BowlerName`. Removing it is one field per record.
- **E. `Stale` is sticky.** A dry compute that matches does not clear it. Only a forced recompute, or a recompute of a Computed Stage that changed, clears it. This follows the plan text.

---

#### A. Events, publisher, keyed lock

##### `src/Neba.Api/Features/Tournaments/Domain/TournamentResultsRecorded.cs`
```csharp
using Neba.Api.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>Raised after a sync wrote a tournament's results. Fires on every re-sync.</summary>
public sealed record TournamentResultsRecorded
    : IDomainEvent
{
    /// <summary>The tournament whose results were written.</summary>
    public required TournamentId TournamentId { get; init; }
}
```

##### `src/Neba.Api/Features/Tournaments/Domain/TournamentSquadsChanged.cs`
```csharp
using Neba.Api.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>Raised when a squad is added to, changed on or removed from a tournament.</summary>
public sealed record TournamentSquadsChanged
    : IDomainEvent
{
    /// <summary>The tournament whose squads changed.</summary>
    public required TournamentId TournamentId { get; init; }
}
```

##### `src/Neba.Api/Features/Tournaments/Domain/SquadScoresReplaced.cs`
```csharp
using Neba.Api.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>Raised after a squad's scores were replaced by the legacy score sync. Published by the job, not an aggregate.</summary>
public sealed record SquadScoresReplaced
    : IDomainEvent
{
    /// <summary>The tournament the squad belongs to.</summary>
    public required TournamentId TournamentId { get; init; }

    /// <summary>The squad whose scores changed.</summary>
    public required SquadId SquadId { get; init; }
}
```

##### `src/Neba.Api/Features/Stats/Domain/SeasonStatsGenerated.cs`
```csharp
using Neba.Api.Domain;
using Neba.Api.Features.Seasons.Domain;

namespace Neba.Api.Features.Stats.Domain;

/// <summary>Raised after a season's stats were regenerated. Published by the stats job, not an aggregate.</summary>
public sealed record SeasonStatsGenerated
    : IDomainEvent
{
    /// <summary>The season whose stats were regenerated.</summary>
    public required SeasonId SeasonId { get; init; }
}
```

##### `src/Neba.Api/BackgroundJobs/IDomainEventPublisher.cs`
```csharp
using Neba.Api.Domain;

namespace Neba.Api.BackgroundJobs;

/// <summary>
/// Enqueues the Hangfire job that handles a domain event. Aggregates reach it through the save interceptor;
/// jobs that change rows no aggregate owns publish directly.
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>Enqueues the <see cref="IDomainEventJob{TEvent}"/> for the event's runtime type.</summary>
    void Publish(IDomainEvent domainEvent);

    /// <summary>Publishes every event the aggregate holds, then clears them. Safe to call twice.</summary>
    void PublishAndClear(IAggregateRoot aggregate);
}
```

##### `src/Neba.Api/BackgroundJobs/DomainEventPublisher.cs`
```csharp
using Hangfire;
using Hangfire.Common;
using Hangfire.States;

using Neba.Api.Domain;

namespace Neba.Api.BackgroundJobs;

internal sealed class DomainEventPublisher(IBackgroundJobClient backgroundJobClient)
    : IDomainEventPublisher
{
    public void Publish(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var jobType = typeof(IDomainEventJob<>).MakeGenericType(domainEvent.GetType());
        var method = jobType.GetMethod(nameof(IDomainEventJob<>.HandleAsync))!;

        backgroundJobClient.Create(new Job(jobType, method, domainEvent, CancellationToken.None), new EnqueuedState());
    }

    public void PublishAndClear(IAggregateRoot aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        foreach (var domainEvent in aggregate.DomainEvents.ToList())
        {
            Publish(domainEvent);
        }

        aggregate.ClearDomainEvents();
    }
}
```

##### `src/Neba.Api/Database/Interceptors/DomainEventDispatcherInterceptor.cs` (full replacement)
```csharp
using Microsoft.EntityFrameworkCore.Diagnostics;

using Neba.Api.BackgroundJobs;
using Neba.Api.Domain;

namespace Neba.Api.Database.Interceptors;

internal sealed class DomainEventDispatcherInterceptor(IDomainEventPublisher domainEventPublisher)
    : SaveChangesInterceptor
{
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchDomainEvents(eventData);
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        DispatchDomainEvents(eventData);
        return new ValueTask<int>(result);
    }

    private void DispatchDomainEvents(SaveChangesCompletedEventData eventData)
    {
        if (eventData.Context is null)
        {
            return;
        }

        var aggregates = eventData.Context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            domainEventPublisher.PublishAndClear(aggregate);
        }
    }
}
```

##### `src/Neba.Api/BackgroundJobs/BackgroundJobConfiguration.cs` (inside `AddBackgroundJobs`, after `AddScoped<IBackgroundJobScheduler, ...>`)
```csharp
            services.AddSingleton<IDomainEventPublisher, DomainEventPublisher>();
            services.AddScoped<IKeyedLockProvider, HangfireKeyedLockProvider>();
```

##### `src/Neba.Api/BackgroundJobs/IKeyedLockProvider.cs`
```csharp
namespace Neba.Api.BackgroundJobs;

/// <summary>A distributed lock per string key, shared by every app instance through Hangfire storage.</summary>
public interface IKeyedLockProvider
{
    /// <summary>Waits up to <paramref name="timeout"/> for the lock. Returns the handle to dispose, or null on timeout.</summary>
    Task<IDisposable?> TryAcquireAsync(string key, TimeSpan timeout, CancellationToken cancellationToken);
}
```

##### `src/Neba.Api/BackgroundJobs/HangfireKeyedLockProvider.cs`
```csharp
using Hangfire;
using Hangfire.Storage;

namespace Neba.Api.BackgroundJobs;

// The same mechanism HangfireBackgroundJobScheduler.EnqueueOnce uses. The Postgres lock blocks its thread while
// it polls, so acquisition runs on the thread pool.
internal sealed class HangfireKeyedLockProvider
    : IKeyedLockProvider
{
    public Task<IDisposable?> TryAcquireAsync(string key, TimeSpan timeout, CancellationToken cancellationToken)
        => Task.Run<IDisposable?>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var connection = JobStorage.Current.GetConnection();
            try
            {
                return new Handle(connection, connection.AcquireDistributedLock(key, timeout));
            }
            catch (DistributedLockTimeoutException)
            {
                connection.Dispose();
                return null;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }, cancellationToken);

    private sealed class Handle(IStorageConnection connection, IDisposable inner)
        : IDisposable
    {
        public void Dispose()
        {
            inner.Dispose();
            connection.Dispose();
        }
    }
}
```

---

#### B. Edits to Tournament and the legacy jobs

##### `src/Neba.Api/Features/Tournaments/Domain/Tournament.cs`

Squad methods (replace the three bodies; add the private helper). The helper keeps one pending event per save so a batch of `AddSquad` calls makes one job.
```csharp
    public ErrorOr<Success> AddSquad(DateTimeOffset bowlingDateTime, int? maxEntries = null, int? legacyId = null)
    {
        var rangeCheck = ValidateSquadDateInRange(bowlingDateTime);
        if (rangeCheck.IsError)
        {
            return rangeCheck.Errors;
        }

        if (_squads.Any(squad => squad.BowlingDateTimeUtc == bowlingDateTime))
        {
            return TournamentErrors.SquadBowlingDateTimeAlreadyUsed(bowlingDateTime);
        }

        var squad = Squad.Create(bowlingDateTime, maxEntries, legacyId);
        if (squad.IsError)
        {
            return squad.Errors;
        }

        _squads.Add(squad.Value);
        RaiseSquadsChanged();

        return Result.Success;
    }

    public ErrorOr<Updated> UpdateSquad(SquadId squadId, DateTimeOffset bowlingDateTime, int? maxEntries)
    {
        var squad = _squads.SingleOrDefault(s => s.Id == squadId);
        if (squad is null)
        {
            return TournamentErrors.SquadNotFound(squadId);
        }

        var rangeCheck = ValidateSquadDateInRange(bowlingDateTime);
        if (rangeCheck.IsError)
        {
            return rangeCheck.Errors;
        }

        if (_squads.Any(s => s.Id != squadId && s.BowlingDateTimeUtc == bowlingDateTime))
        {
            return TournamentErrors.SquadBowlingDateTimeAlreadyUsed(bowlingDateTime);
        }

        var updated = squad.UpdateDetails(bowlingDateTime, maxEntries);
        if (!updated.IsError)
        {
            RaiseSquadsChanged();
        }

        return updated;
    }

    public ErrorOr<Deleted> RemoveSquad(SquadId squadId)
    {
        var squad = _squads.SingleOrDefault(s => s.Id == squadId);
        if (squad is null)
        {
            return TournamentErrors.SquadNotFound(squadId);
        }

        _squads.Remove(squad);
        RaiseSquadsChanged();

        return Result.Deleted;
    }

    private void RaiseSquadsChanged()
    {
        if (!DomainEvents.OfType<TournamentSquadsChanged>().Any())
        {
            AddDomainEvent(new TournamentSquadsChanged { TournamentId = Id });
        }
    }
```

New method, after `AddResult`:
```csharp
    /// <summary>
    /// Signals that a sync finished writing this tournament's results. Call it after the <see cref="AddResult"/> loop on
    /// every sync, including one that adds nothing, so a re-sync redelivers the event. Complete, Truncate and Cancel raise
    /// nothing: in the legacy chain the results arrive after the status flips.
    /// </summary>
    public ErrorOr<Success> MarkResultsRecorded()
    {
        if (Status == TournamentStatus.Scheduled || Status == TournamentStatus.Cancelled)
        {
            return TournamentErrors.TournamentNotFinalized;
        }

        AddDomainEvent(new TournamentResultsRecorded { TournamentId = Id });

        return Result.Success;
    }
```

##### `src/Neba.Api/Legacy/Tournaments/Complete/SyncTournamentResultsJob.cs`
Constructor gains `IDomainEventPublisher domainEventPublisher` (add `using Neba.Api.BackgroundJobs;`):
```csharp
internal sealed class SyncTournamentResultsJob(
    AppDbContext db,
    IDbConnection legacyConnection,
    IFusionCache cache,
    IDomainEventPublisher domainEventPublisher,
    IManualInterventionNotifier manualInterventionNotifier,
    ILogger<SyncTournamentResultsJob> logger)
```
Replace the save step:
```csharp
        var unmappedLegacyBowlerIds = AddResults(
            tournament, singleResultRows, bowlerIdsByLegacyId, placeByLegacyBowlerId, legacyTournamentId);

        // Raised on every sync, even one that adds nothing: the re-sync is the Series' redelivery path.
        var recorded = tournament.MarkResultsRecorded();
        if (recorded.IsError)
        {
            logger.LogLegacyResultsNotMarkedRecorded(legacyTournamentId, recorded.FirstError.Description);
        }

        await db.SaveChangesAsync(ct);

        // A re-sync that finds every row already stored has no tracked changes, and EF may skip the SavedChanges
        // interceptor then. The interceptor clears the events when it does run, so nothing publishes twice.
        domainEventPublisher.PublishAndClear(tournament);
```
In `SyncTournamentResultsJobLogMessages.cs` add:
```csharp
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Results synced for legacy tournament {LegacyTournamentId} but the tournament cannot signal them: {Reason}")]
    public static partial void LogLegacyResultsNotMarkedRecorded(
        this ILogger<SyncTournamentResultsJob> logger, int legacyTournamentId, string reason);
```

##### `src/Neba.Api/Legacy/Tournaments/SyncSquadScores.cs`
Constructor gains `IDomainEventPublisher domainEventPublisher` (add `using Neba.Api.BackgroundJobs;`). In `SyncAsync`, after `await cache.RemoveByTagAsync($"neba:tournaments:{tournamentId}", token: ct);` and before the `unmappedRowsByLegacyBowlerId.Count == 0` check:
```csharp
        // The scores belong to no aggregate, so the job publishes the event itself.
        domainEventPublisher.Publish(new SquadScoresReplaced { TournamentId = tournamentId, SquadId = squad.Id });
```

##### `src/Neba.Api/Legacy/Tournaments/Stats/GenerateSeasonStatsJob.cs`
Constructor gains `IDomainEventPublisher domainEventPublisher` (add `using Neba.Api.BackgroundJobs;`). In `SyncAsync`, after `await cache.RemoveByTagAsync(CacheDescriptors.Stats.SeasonTag(season.Id), token: ct);`:
```csharp
        // Series subscribes (Stats never references Series). Fires on every regeneration.
        domainEventPublisher.Publish(new SeasonStatsGenerated { SeasonId = season.Id });
```

---

#### C. Series domain

##### `Domain/Series.cs` (diff)
```csharp
// 1) class declaration
public sealed partial class Series

// 2) new members, after Finale
    /// <summary>
    /// The date the Backfill ran as of (the last Qualifying tournament's end date), or null while it has not run.
    /// </summary>
    public DateOnly? BackfillAsOf { get; private set; }

    /// <summary>Concurrency token. Bumped by every advancement write and by a Stage-list change, not by rename, logo or Finale reschedule.</summary>
    public int Version { get; private set; }

    private readonly List<SeriesFieldEntry> _fieldEntries = [];

    /// <summary>The Series Field entries of every Qualifying Stage and the Backfill.</summary>
    public IReadOnlyCollection<SeriesFieldEntry> FieldEntries
        => _fieldEntries.AsReadOnly();

    private readonly List<SeriesPosition> _positions = [];

    /// <summary>The Placement Round's positions.</summary>
    public IReadOnlyCollection<SeriesPosition> Positions
        => _positions.AsReadOnly();

    /// <summary>Refuses a write made from a stale screen.</summary>
    public ErrorOr<Success> EnsureVersion(int expected)
        => Version == expected ? Result.Success : SeriesAdvancementErrors.VersionConflict;

// 3) ReplaceConfiguration: replace the lock check, bump Version when Stages change
        var stagesUnchanged = StagesMatch(stages);
        var finaleShapeUnchanged = (Finale is null) == (finale is null) && Finale?.FinalistCount == finale?.FinalistCount;
        var cancelledRemoval = !stagesUnchanged && IsOnlyCancelledPendingRemoval(stages, tournaments);

        if (IsLocked && !((stagesUnchanged || cancelledRemoval) && finaleShapeUnchanged))
        {
            return SeriesErrors.Locked;
        }
        // ... unchanged ...
        if (!stagesUnchanged)
        {
            ApplyStages(stages, candidates);
            Version++;
        }

// 4) new private helper, beside StagesMatch
    // Locked Series: the submitted list is the stored list minus Pending Stages whose tournament is Cancelled.
    private bool IsOnlyCancelledPendingRemoval(
        IReadOnlyList<SeriesStageDefinition> stages,
        IReadOnlyDictionary<TournamentId, StageTournamentFacts> tournaments)
    {
        if (stages.Any(definition => definition.StageId is null))
        {
            return false;
        }

        var keptIds = stages.Select(definition => definition.StageId!.Value).ToHashSet();
        var removed = _stages.Where(stage => !keptIds.Contains(stage.Id)).ToList();

        if (removed.Count == 0 || !removed.TrueForAll(stage => stage.Status == StageStatus.Pending
                && tournaments.TryGetValue(stage.TournamentId, out var facts)
                && facts.Status == TournamentStatus.Cancelled))
        {
            return false;
        }

        var kept = _stages.Where(stage => keptIds.Contains(stage.Id)).OrderBy(stage => stage.Order).ToList();

        return kept.Count == stages.Count
            && stages.Select((definition, index) => kept[index].Id == definition.StageId && kept[index].MatchesIgnoringOrder(definition)).All(same => same);
    }
```
Add `using Neba.Api.Features.Tournaments.Domain;` if missing. `Series.cs` already imports it.

##### `Domain/StageTournamentFacts.cs` (diff)
```csharp
    /// <summary>The tournament's status. A Cancelled tournament lets a Pending Stage be removed from a locked Series.</summary>
    public required TournamentStatus Status { get; init; }
```
`StageTournamentFactsReader`: add `Status = tournament.Status,` to the projection.

##### `Domain/SeriesStage.cs` (diff and added members)
```csharp
    /// <summary>
    /// Processing state. Set only by <see cref="MarkComputed"/> and <see cref="MarkOfficial"/>. The setter stays internal
    /// because tests and factories use it.
    /// </summary>
    public StageStatus Status { get; internal set; } = StageStatus.Pending;

    /// <summary>True when an official or unreproducible output may differ from what a recompute gives. Cleared by a forced recompute.</summary>
    public bool IsStale { get; private set; }

    /// <summary>When the output last changed (moves only when the output changes). Null while Pending.</summary>
    public DateTimeOffset? ComputedAt { get; private set; }

    /// <summary>When the Stage was official. Null unless Official.</summary>
    public DateTimeOffset? OfficialAt { get; private set; }

    /// <summary>Qualifying only: the slot total when last computed, so an official Stage's target does not shift.</summary>
    public int? SlotCount { get; private set; }

    private readonly List<SurvivorOutcome> _survivorOutcomes = [];

    /// <summary>Survivor only: one row per bowler who entered the Stage.</summary>
    public IReadOnlyCollection<SurvivorOutcome> SurvivorOutcomes
        => _survivorOutcomes;

    private readonly List<StageOverride> _overrides = [];

    /// <summary>Qualifying only: admin overrides. They survive every recompute.</summary>
    public IReadOnlyCollection<StageOverride> Overrides
        => _overrides;

    internal void MarkComputed(DateTimeOffset now)
    {
        Status = StageStatus.Computed;
        ComputedAt = now;
        OfficialAt = null;
        IsStale = false;
    }

    internal void MarkOfficial(DateTimeOffset now)
    {
        Status = StageStatus.Official;
        OfficialAt = now;
    }

    internal void MarkStale() => IsStale = true;

    internal void ClearStale() => IsStale = false;

    internal void SetSlotCount(int? slotCount) => SlotCount = slotCount;

    internal void ResolveSquad(SquadId squadId)
        => SquadDesignation = SquadDesignation?.Resolve(squadId);

    internal void UnresolveSquad()
        => SquadDesignation = SquadDesignation?.Unresolve();

    internal void AddOverride(StageOverride stageOverride) => _overrides.Add(stageOverride);

    internal void RemoveOverride(StageOverride stageOverride) => _overrides.Remove(stageOverride);

    internal void SetOverrideApplied(StageOverrideId id, bool applied)
        => _overrides.Single(o => o.Id == id).Applied = applied;

    internal void ReplaceOutcomes(IReadOnlyList<SurvivorOutcomeDraft> target)
    {
        _survivorOutcomes.RemoveAll(o => target.All(t => t.BowlerId != o.BowlerId));

        foreach (var draft in target)
        {
            var existing = _survivorOutcomes.Find(o => o.BowlerId == draft.BowlerId);
            if (existing is null)
            {
                _survivorOutcomes.Add(SurvivorOutcome.Create(draft.BowlerId, draft.BlockTotal, draft.Advanced).Value);
            }
            else
            {
                _ = existing.Update(draft.BlockTotal, draft.Advanced).Value; // computed data is valid; an error here is a bug
            }
        }
    }

// Update: keep the resolved squad when date and time are unchanged (replaces the SquadDesignation assignment)
        SquadDesignation = hasSquad
            ? SquadDesignation is { } current
                && current.Date == definition.SquadDate && current.StartTime == definition.SquadStartTime
                ? current
                : SquadDesignation.Describe(definition.SquadDate!.Value, definition.SquadStartTime!.Value)
            : null;

// Matches: split out the order
    internal bool Matches(int order, SeriesStageDefinition definition)
        => Order == order && MatchesIgnoringOrder(definition);

    internal bool MatchesIgnoringOrder(SeriesStageDefinition definition)
        => Kind == definition.Kind
            && TournamentId == definition.TournamentId
            && AdvancingCount == definition.AdvancingCount
            && SquadDesignation?.Date == definition.SquadDate
            && SquadDesignation?.StartTime == definition.SquadStartTime;
```

##### `Domain/SquadDesignation.cs` (diff)
```csharp
    /// <summary>The resolved squad, or null while only described.</summary>
    public SquadId? SquadId { get; init; }

    /// <summary>True once resolved to a squad.</summary>
    public bool IsResolved
        => SquadId is not null;

    internal SquadDesignation Resolve(SquadId squadId)
        => this with { SquadId = squadId };

    internal SquadDesignation Unresolve()
        => this with { SquadId = null };
```
Update the class summary: "resolved to a SquadId by phase 2".

##### New types in `Domain/`

```csharp
// === StageOverrideId.cs ===
using StronglyTypedIds;

namespace Neba.Api.Features.Series.Domain;

/// <summary>Unique identifier for a Stage Override.</summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct StageOverrideId;

// === SeriesFieldEntryId.cs ===
/// <summary>Unique identifier for a Series Field Entry.</summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SeriesFieldEntryId;
```

```csharp
// === Neba.Api.Contracts/Series/OverrideKindValues.cs ===
namespace Neba.Api.Contracts.Series;

/// <summary>The SmartEnum values of a Stage Override kind, as commands send them.</summary>
public static class OverrideKindValues
{
    /// <summary>The value of the Add kind.</summary>
    public const int Add = 1;

    /// <summary>The value of the Clear kind.</summary>
    public const int Clear = 2;
}

// === Domain/OverrideKind.cs ===
using Ardalis.SmartEnum;

using Neba.Api.Contracts.Series;

namespace Neba.Api.Features.Series.Domain;

/// <summary>What a Stage Override does to a Qualifying Stage's field.</summary>
public sealed class OverrideKind
    : SmartEnum<OverrideKind>
{
    /// <summary>Adds a bowler who has a result row in the Stage's tournament.</summary>
    public static readonly OverrideKind Add = new(nameof(Add), OverrideKindValues.Add);

    /// <summary>Clears a filled slot. The slot goes vacant and the bowler is barred from the Backfill.</summary>
    public static readonly OverrideKind Clear = new(nameof(Clear), OverrideKindValues.Clear);

    private OverrideKind(string name, int value)
        : base(name, value)
    { }
}
```

```csharp
// === Domain/StageOverride.cs ===
using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>An admin's manual change to a Qualifying Stage's field. Stored until an admin removes it.</summary>
public sealed class StageOverride
{
    internal const int ReasonMaxLength = 500;

    /// <summary>Unique ID.</summary>
    public required StageOverrideId Id { get; init; }

    /// <summary>The bowler added or cleared.</summary>
    public required BowlerId BowlerId { get; init; }

    internal Bowler Bowler { get; init; } = null!; // EF-only, for the Restrict foreign key

    /// <summary>Add or Clear.</summary>
    public OverrideKind Kind { get; private set; } = OverrideKind.Add;

    /// <summary>Why the admin made the change.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>The user who made the change.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>When the change was made.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>False when the target changed so the override no longer applies. Recomputed on every fold; never an error.</summary>
    public bool Applied { get; internal set; }

    internal static ErrorOr<StageOverride> Create(
        OverrideKind kind, BowlerId bowlerId, string reason, string createdBy, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return SeriesAdvancementErrors.OverrideReasonRequired;
        }

        if (reason.Trim().Length > ReasonMaxLength)
        {
            return SeriesAdvancementErrors.OverrideReasonTooLong(ReasonMaxLength);
        }

        return new StageOverride
        {
            Id = StageOverrideId.New(),
            BowlerId = bowlerId,
            Kind = kind,
            Reason = reason.Trim(),
            CreatedBy = createdBy,
            CreatedAt = createdAt,
            Applied = true,
        };
    }
}
```

```csharp
// === Domain/SeriesFieldEntry.cs ===
using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>A bowler's place in the Series Field: a Qualifying slot or a Backfill entry.</summary>
public sealed class SeriesFieldEntry
{
    /// <summary>Unique ID.</summary>
    public required SeriesFieldEntryId Id { get; init; }

    /// <summary>The bowler.</summary>
    public required BowlerId BowlerId { get; init; }

    internal Bowler Bowler { get; init; } = null!; // EF-only

    /// <summary>The Qualifying Stage that produced the entry, or null for the Backfill.</summary>
    public SeriesStageId? SourceStageId { get; private set; }

    /// <summary>Finishing place at the source Stage. Null for the Backfill.</summary>
    public int? Place { get; private set; }

    /// <summary>Open Bowler of the Year points as of <see cref="Series.BackfillAsOf"/>. Null for a Stage entry.</summary>
    public int? BackfillPoints { get; private set; }

    /// <summary>True when an admin override added the bowler.</summary>
    public bool ViaOverride { get; private set; }

    /// <summary>True for a Backfill entry.</summary>
    public bool IsBackfill
        => SourceStageId is null;

    internal static ErrorOr<SeriesFieldEntry> Create(
        BowlerId bowlerId, SeriesStageId? sourceStageId, int? place, int? backfillPoints, bool viaOverride)
    {
        var entry = new SeriesFieldEntry { Id = SeriesFieldEntryId.New(), BowlerId = bowlerId };
        var result = entry.Set(sourceStageId, place, backfillPoints, viaOverride);

        return result.IsError ? result.Errors : entry;
    }

    internal ErrorOr<Updated> Update(int? place, int? backfillPoints, bool viaOverride)
        => Set(SourceStageId, place, backfillPoints, viaOverride);

    internal FieldEntryDraft ToDraft()
        => new(BowlerId, SourceStageId, Place, BackfillPoints, ViaOverride);

    private ErrorOr<Updated> Set(SeriesStageId? sourceStageId, int? place, int? backfillPoints, bool viaOverride)
    {
        var valid = sourceStageId is null
            ? backfillPoints is > 0 && place is null && !viaOverride
            : place is > 0 && backfillPoints is null;

        if (!valid)
        {
            return SeriesAdvancementErrors.EntryInvalid;
        }

        SourceStageId = sourceStageId;
        Place = place;
        BackfillPoints = backfillPoints;
        ViaOverride = viaOverride;

        return Result.Updated;
    }
}
```

```csharp
// === Domain/SeriesPosition.cs ===
public sealed class SeriesPosition
{
    /// <summary>The bowler.</summary>
    public required BowlerId BowlerId { get; init; }

    internal Bowler Bowler { get; init; } = null!; // EF-only

    /// <summary>1-based rank in the Series. Unique.</summary>
    public int Position { get; private set; }

    /// <summary>True when the bowler has a result row in the Placement tournament. False for a no-show.</summary>
    public bool Bowled { get; private set; }

    /// <summary>True when the position is within the Finale's finalist count. Null when the Series has no Finale.</summary>
    public bool? IsFinalist { get; private set; }

    internal static ErrorOr<SeriesPosition> Create(BowlerId bowlerId, int position, bool bowled, bool? isFinalist)
    {
        var created = new SeriesPosition { BowlerId = bowlerId };
        var result = created.Update(position, bowled, isFinalist);

        return result.IsError ? result.Errors : created;
    }

    internal ErrorOr<Updated> Update(int position, bool bowled, bool? isFinalist)
    {
        if (position <= 0)
        {
            return SeriesAdvancementErrors.PositionInvalid;
        }

        Position = position;
        Bowled = bowled;
        IsFinalist = isFinalist;

        return Result.Updated;
    }

    internal PositionDraft ToDraft() => new(BowlerId, Position, Bowled, IsFinalist);
}

// === Domain/SurvivorOutcome.cs ===
public sealed class SurvivorOutcome
{
    /// <summary>The bowler who entered the Survivor Stage.</summary>
    public required BowlerId BowlerId { get; init; }

    internal Bowler Bowler { get; init; } = null!; // EF-only

    /// <summary>Total over the designated squad's games the bowler bowled. Null when the bowler did not bowl.</summary>
    public int? BlockTotal { get; private set; }

    /// <summary>True when the bowler advances.</summary>
    public bool Advanced { get; private set; }

    internal static ErrorOr<SurvivorOutcome> Create(BowlerId bowlerId, int? blockTotal, bool advanced)
    {
        var created = new SurvivorOutcome { BowlerId = bowlerId };
        var result = created.Update(blockTotal, advanced);

        return result.IsError ? result.Errors : created;
    }

    internal ErrorOr<Updated> Update(int? blockTotal, bool advanced)
    {
        if (blockTotal is < 0 || (advanced && blockTotal is null))
        {
            return SeriesAdvancementErrors.OutcomeInvalid;
        }

        BlockTotal = blockTotal;
        Advanced = advanced;

        return Result.Updated;
    }

    internal SurvivorOutcomeDraft ToDraft() => new(BowlerId, BlockTotal, Advanced);
}
```
Both files carry `using ErrorOr;` and `using Neba.Api.Features.Bowlers.Domain;`.

##### `Domain/AdvancementFacts.cs` (public: `Series` public methods take them)
```csharp
using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>Everything outside the Series that an advancement needs. The handler reads it; the Series decides with it.</summary>
public sealed record AdvancementFacts
{
    private static readonly IReadOnlySet<BowlerId> NoBowlers = new HashSet<BowlerId>();

    /// <summary>One entry for each Stage's tournament.</summary>
    public required IReadOnlyDictionary<TournamentId, StageTournamentResults> Tournaments { get; init; }

    /// <summary>For each date asked for, the bowlers with a membership on it.</summary>
    public required IReadOnlyDictionary<DateOnly, IReadOnlySet<BowlerId>> MembersOnDate { get; init; }

    /// <summary>Backfill facts, or null when the Series has no Qualifying Stage.</summary>
    public BackfillFacts? Backfill { get; init; }

    /// <summary>Sort names for the bowlers who could be in the field. Used only to order Placement no-shows; never stored or logged.</summary>
    public required IReadOnlyDictionary<BowlerId, BowlerSortName> SortNames { get; init; }

    /// <summary>No facts. For operations that read none.</summary>
    public static AdvancementFacts Empty { get; } = new()
    {
        Tournaments = new Dictionary<TournamentId, StageTournamentResults>(),
        MembersOnDate = new Dictionary<DateOnly, IReadOnlySet<BowlerId>>(),
        SortNames = new Dictionary<BowlerId, BowlerSortName>(),
    };

    /// <summary>The members on a date, or none when the date was not loaded.</summary>
    public IReadOnlySet<BowlerId> MembersOn(DateOnly date)
        => MembersOnDate.TryGetValue(date, out var members) ? members : NoBowlers;
}

/// <summary>A Stage tournament's status, results and squads.</summary>
public sealed record StageTournamentResults
{
    /// <summary>The tournament.</summary>
    public required TournamentId TournamentId { get; init; }
    /// <summary>The tournament status.</summary>
    public required TournamentStatus Status { get; init; }
    /// <summary>First day.</summary>
    public required DateOnly StartDate { get; init; }
    /// <summary>Last day.</summary>
    public required DateOnly EndDate { get; init; }
    /// <summary>One row per result. Empty until results sync.</summary>
    public required IReadOnlyList<StageResultRow> Results { get; init; }
    /// <summary>The tournament's squads. Loaded for Survivor Stages only.</summary>
    public required IReadOnlyList<StageSquadFact> Squads { get; init; }
}

/// <summary>A bowler's final place in a tournament.</summary>
public sealed record StageResultRow(BowlerId BowlerId, int Place);

/// <summary>A squad with its Eastern start and per-bowler totals.</summary>
public sealed record StageSquadFact(
    SquadId SquadId, DateOnly LocalDate, TimeOnly LocalStartTime, IReadOnlyList<SquadBowlerTotal> Totals);

/// <summary>A bowler's score total and game count on a squad.</summary>
public sealed record SquadBowlerTotal(BowlerId BowlerId, int Total, int Games);

/// <summary>Open Bowler of the Year points as of the Backfill date.</summary>
public sealed record BackfillFacts(DateOnly AsOf, bool Ready, IReadOnlyList<BowlerPointsFact> Points);

/// <summary>A bowler's open Bowler of the Year points.</summary>
public sealed record BowlerPointsFact(BowlerId BowlerId, int Points);

/// <summary>A bowler's name for ordering only.</summary>
public sealed record BowlerSortName(string LastName, string FirstName);
```

##### `Domain/StageComputation.cs`, `AdvanceResult.cs`
```csharp
namespace Neba.Api.Features.Series.Domain;

internal sealed record FieldEntryDraft(BowlerId BowlerId, SeriesStageId? SourceStageId, int? Place, int? BackfillPoints, bool ViaOverride);

internal sealed record SurvivorOutcomeDraft(BowlerId BowlerId, int? BlockTotal, bool Advanced);

internal sealed record PositionDraft(BowlerId BowlerId, int Position, bool Bowled, bool? IsFinalist);

internal sealed record OverrideEffect(StageOverrideId OverrideId, bool Applied);

/// <summary>A Stage's computed output. A pure function of earlier Stages' stored output, the results, the overrides and the facts.</summary>
internal sealed record StageComputation
{
    public IReadOnlyList<FieldEntryDraft> Entries { get; init; } = [];
    public int? SlotCount { get; init; }
    public int Vacancies { get; init; }
    public IReadOnlyList<OverrideEffect> OverrideEffects { get; init; } = [];
    public IReadOnlyList<FieldEntryDraft> BackfillEntries { get; init; } = [];
    public DateOnly? BackfillAsOf { get; init; }
    public IReadOnlyList<SurvivorOutcomeDraft> Outcomes { get; init; } = [];
    public IReadOnlyList<PositionDraft> Positions { get; init; } = [];
}

/// <summary>What an advancement call did.</summary>
public sealed record AdvanceResult
{
    /// <summary>Nothing changed.</summary>
    public static AdvanceResult None { get; } = new();

    /// <summary>True when anything was written. <see cref="Series.Version"/> moved.</summary>
    public bool Changed { get; init; }
    /// <summary>True when a Stage's status changed. The handler evicts the tournaments cache.</summary>
    public bool StatusChanged { get; init; }
    /// <summary>Stages computed or recomputed with new output.</summary>
    public IReadOnlyList<SeriesStageId> ComputedStageIds { get; init; } = [];
    /// <summary>Stages marked stale.</summary>
    public IReadOnlyList<SeriesStageId> StaleStageIds { get; init; } = [];
    /// <summary>Stages official.</summary>
    public IReadOnlyList<SeriesStageId> OfficialStageIds { get; init; } = [];
    /// <summary>The override an Add or Clear created.</summary>
    public StageOverrideId? OverrideId { get; init; }
    /// <summary>The Series' version after the call.</summary>
    public int Version { get; init; }
}
```
`FieldEntryDraft` and the other drafts are `internal`, and `SeriesFieldEntry.ToDraft()` is `internal`, so this compiles. Add `using Neba.Api.Features.Bowlers.Domain;` to this file.

##### `Domain/SeriesAdvancementErrors.cs`
```csharp
using ErrorOr;

namespace Neba.Api.Features.Series.Domain;

internal static class SeriesAdvancementErrors
{
    public const string OfficialStageOrdersKey = "OfficialStageOrders";

    public static readonly Error VersionConflict = Error.Conflict(
        "Series.Version.Conflict", "This Series changed after you loaded it. Reload and try again.");

    public static readonly Error ConcurrentChange = Error.Conflict(
        "Series.ConcurrentChange", "Another change to this Series was saved first. Reload and try again.");

    public static readonly Error Busy = Error.Conflict(
        "Series.Advancement.Busy", "Advancement for this Series is running. Try again in a moment.");

    public static Error StageNotFound(SeriesStageId id) => Error.NotFound(
        "Series.Advancement.StageNotFound", "Stage not found.",
        new Dictionary<string, object> { { "StageId", id.ToString() } });

    public static readonly Error NotQualifyingStage = Error.Validation(
        "Series.Override.StageNotQualifying", "Overrides apply to Qualifying Stages only.");

    public static Error StageNotComputed(StageStatus status) => Error.Conflict(
        "Series.Override.StageNotComputed",
        status == StageStatus.Official
            ? "This Stage is official. Recompute it from the Series page before changing it."
            : "This Stage has not been computed yet.");

    public static Error NoResultRow => Error.Validation(
        "Series.Override.NoResultRow", "The bowler has no result in this Stage's tournament.");

    public static readonly Error BowlerAlreadyInField = Error.Conflict(
        "Series.Override.BowlerAlreadyInField", "The bowler is already in the Series Field.");

    public static readonly Error OverrideExists = Error.Conflict(
        "Series.Override.Exists", "This bowler already has an override on this Stage. Remove it first.");

    public static readonly Error BowlerNotInSlot = Error.Validation(
        "Series.Override.BowlerNotInSlot", "The bowler does not hold a filled slot in this Stage.");

    public static readonly Error OverrideReasonRequired = Error.Validation(
        "Series.Override.Reason.Required", "A reason is required.");

    public static Error OverrideReasonTooLong(int maxLength) => Error.Validation(
        "Series.Override.Reason.TooLong", $"The reason must be {maxLength} characters or fewer.");

    public static Error ConfirmationRequired(IReadOnlyCollection<int> officialOrders) => Error.Conflict(
        "Series.Recompute.ConfirmationRequired",
        $"Stage {string.Join(", ", officialOrders)} {(officialOrders.Count == 1 ? "is" : "are")} official. Recomputing resets them to Computed. Confirm to continue.",
        new Dictionary<string, object> { { OfficialStageOrdersKey, officialOrders.ToArray() } });

    public static readonly Error StageNotSurvivor = Error.Validation(
        "Series.Squad.StageNotSurvivor", "Only a Survivor Stage has a squad.");

    public static readonly Error SquadNotInTournament = Error.Validation(
        "Series.Squad.NotInTournament", "The squad does not belong to this Stage's tournament.");

    // Invariants of the stored output types. Computed data should never trip these.
    public static readonly Error EntryInvalid = Error.Validation(
        "Series.FieldEntry.Invalid", "A Stage entry needs a place; a Backfill entry needs points above zero and no place.");

    public static readonly Error PositionInvalid = Error.Validation(
        "Series.Position.Invalid", "A position must be above zero.");

    public static readonly Error OutcomeInvalid = Error.Validation(
        "Series.SurvivorOutcome.Invalid", "A block total cannot be negative, and an advancing bowler needs one.");
}
```
Also shown in section 0 under "Not checked": `ConfirmationRequired` carries the official Stage orders in metadata and in the text, so the Blazor confirmation lists them.

##### `Domain/SeriesAdvancementRules.cs`
```csharp
namespace Neba.Api.Features.Series.Domain;

internal static class SeriesAdvancementRules
{
    /// <summary>
    /// When a Computed Qualifying or Survivor Stage will become Official (the grace period after the output last changed), or null
    /// when it will not. Shared by the make-official job and the advancement read. The sweep runs every 15 minutes, so the real
    /// change lands up to 15 minutes later. It ignores whether earlier Stages are official, which delays but never advances it.
    /// </summary>
    public static DateTimeOffset? OfficialDueAt(
        StageKind kind, StageStatus status, bool isStale, DateTimeOffset? computedAt, bool awaitingBackfill, TimeSpan grace)
        => kind != StageKind.Placement && status == StageStatus.Computed && !isStale && !awaitingBackfill && computedAt is { } at
            ? at + grace
            : null;
}
```

---

#### D. Computation (pure, one file each)

##### `Domain/QualifyingComputation.cs`
```csharp
using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Series.Domain;

internal static class QualifyingComputation
{
    // Slots: every result row at Place <= N (ties at N all count; a team event counts rows, so N teams give N x team size).
    // A slot that would go to a non-member, or to a bowler already in the field, stays vacant for the Backfill and is not
    // passed down the finish list. Clear overrides vacate a filled slot. Add overrides add any bowler with a result row
    // and do not fill a vacancy.
    public static StageComputation Compute(
        SeriesStage stage,
        StageTournamentResults tournament,
        IReadOnlySet<BowlerId> alreadyQualified,
        IReadOnlySet<BowlerId> membersOnEventDate,
        IReadOnlyCollection<StageOverride> overrides)
    {
        var advancing = stage.AdvancingCount ?? 0;
        var placeByBowler = tournament.Results.ToDictionary(row => row.BowlerId, row => row.Place);

        var slotRows = tournament.Results
            .Where(row => row.Place <= advancing)
            .OrderBy(row => row.Place).ThenBy(row => row.BowlerId)
            .ToList();

        var clears = overrides.Where(o => o.Kind == OverrideKind.Clear).ToDictionary(o => o.BowlerId);
        var adds = overrides.Where(o => o.Kind == OverrideKind.Add).OrderBy(o => o.CreatedAt).ThenBy(o => o.Id).ToList();

        var entries = new List<FieldEntryDraft>();
        var held = new HashSet<BowlerId>();
        var cleared = new HashSet<BowlerId>();

        foreach (var row in slotRows)
        {
            if (!membersOnEventDate.Contains(row.BowlerId) || alreadyQualified.Contains(row.BowlerId))
            {
                continue; // vacant
            }

            if (clears.ContainsKey(row.BowlerId))
            {
                cleared.Add(row.BowlerId); // vacant by override
                continue;
            }

            entries.Add(new FieldEntryDraft(row.BowlerId, stage.Id, row.Place, null, false));
            held.Add(row.BowlerId);
        }

        var effects = clears.Values.Select(c => new OverrideEffect(c.Id, cleared.Contains(c.BowlerId))).ToList();

        foreach (var add in adds)
        {
            var applies = placeByBowler.TryGetValue(add.BowlerId, out var place)
                && !alreadyQualified.Contains(add.BowlerId)
                && !held.Contains(add.BowlerId);

            if (applies)
            {
                entries.Add(new FieldEntryDraft(add.BowlerId, stage.Id, place, null, true));
                held.Add(add.BowlerId);
            }

            effects.Add(new OverrideEffect(add.Id, applies));
        }

        return new StageComputation
        {
            Entries = entries,
            SlotCount = slotRows.Count,
            Vacancies = slotRows.Count - entries.Count(e => !e.ViaOverride),
            OverrideEffects = effects,
        };
    }
}
```

##### `Domain/BackfillComputation.cs`
```csharp
namespace Neba.Api.Features.Series.Domain;

internal static class BackfillComputation
{
    // Fills vacancies from open Bowler of the Year points as of the stored date. Candidates must have points above zero, be
    // members on the as-of date, not be in the field, and not be barred by an applied Clear override. Ties at the cutoff all
    // advance; with fewer candidates than vacancies, every candidate advances and the rest stay vacant.
    public static IReadOnlyList<FieldEntryDraft> Compute(
        int vacancies,
        IReadOnlySet<BowlerId> inField,
        IReadOnlySet<BowlerId> barred,
        IReadOnlySet<BowlerId> membersAsOf,
        IReadOnlyList<BowlerPointsFact> points)
    {
        if (vacancies <= 0)
        {
            return [];
        }

        var candidates = points
            .Where(p => p.Points > 0 && membersAsOf.Contains(p.BowlerId) && !inField.Contains(p.BowlerId) && !barred.Contains(p.BowlerId))
            .OrderByDescending(p => p.Points).ThenBy(p => p.BowlerId)
            .ToList();

        if (candidates.Count == 0)
        {
            return [];
        }

        var cutoff = candidates.Count <= vacancies ? candidates[^1].Points : candidates[vacancies - 1].Points;

        return [.. candidates
            .Where(p => p.Points >= cutoff)
            .Select(p => new FieldEntryDraft(p.BowlerId, null, null, p.Points, false))];
    }
}
```

##### `Domain/SurvivorComputation.cs`
```csharp
namespace Neba.Api.Features.Series.Domain;

internal static class SurvivorComputation
{
    // Only bowlers in the field can advance, on total over the games they bowled on the designated squad (a partial block
    // counts). All ties at the cut advance. Fewer bowlers than the count: everyone who bowled advances. A bowler who did not
    // bowl is eliminated and no alternate takes the spot.
    public static StageComputation Compute(SeriesStage stage, IReadOnlyCollection<BowlerId> field, StageSquadFact squad)
    {
        var totals = squad.Totals.Where(t => t.Games > 0).ToDictionary(t => t.BowlerId);
        var count = stage.AdvancingCount ?? 0;

        var bowled = field
            .Where(totals.ContainsKey)
            .Select(bowler => (BowlerId: bowler, totals[bowler].Total))
            .OrderByDescending(x => x.Total).ThenBy(x => x.BowlerId)
            .ToList();

        var cutoff = count <= 0 ? int.MaxValue
            : bowled.Count <= count ? int.MinValue
            : bowled[count - 1].Total;

        var advanced = bowled.Where(x => x.Total >= cutoff).Select(x => x.BowlerId).ToHashSet();

        return new StageComputation
        {
            Outcomes = [.. field.OrderBy(bowler => bowler).Select(bowler => new SurvivorOutcomeDraft(
                bowler, totals.TryGetValue(bowler, out var t) ? t.Total : null, advanced.Contains(bowler)))],
        };
    }
}
```

##### `Domain/PlacementComputation.cs`
```csharp
namespace Neba.Api.Features.Series.Domain;

internal static class PlacementComputation
{
    // Field bowlers who bowled rank by Place (ties by BowlerId). Field bowlers with no result row rank after them,
    // alphabetically by last then first name (BowlerId last), and still get a position. Positions are unique. With a
    // Finale the first FinalistCount positions are finalists; without one IsFinalist is null.
    public static StageComputation Compute(
        IReadOnlyCollection<BowlerId> field,
        StageTournamentResults tournament,
        IReadOnlyDictionary<BowlerId, BowlerSortName> names,
        int? finalistCount)
    {
        var placeByBowler = tournament.Results.ToDictionary(row => row.BowlerId, row => row.Place);

        var bowled = field.Where(placeByBowler.ContainsKey)
            .OrderBy(bowler => placeByBowler[bowler]).ThenBy(bowler => bowler);

        var absent = field.Where(bowler => !placeByBowler.ContainsKey(bowler))
            .OrderBy(bowler => names.TryGetValue(bowler, out var name) ? name.LastName : string.Empty, StringComparer.InvariantCultureIgnoreCase)
            .ThenBy(bowler => names.TryGetValue(bowler, out var name) ? name.FirstName : string.Empty, StringComparer.InvariantCultureIgnoreCase)
            .ThenBy(bowler => bowler);

        var ranked = bowled.Select(bowler => (Bowler: bowler, Bowled: true))
            .Concat(absent.Select(bowler => (Bowler: bowler, Bowled: false)))
            .Select((x, index) => new PositionDraft(
                x.Bowler, index + 1, x.Bowled, finalistCount is { } n ? index + 1 <= n : (bool?)null))
            .ToList();

        return new StageComputation { Positions = ranked };
    }
}
```

---

#### E. `Series.Advancement.cs` (the fold, `Advance`, `Recompute`, overrides, squad, make official)

##### `Domain/Series.Advancement.cs`
```csharp
using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.Domain;

public sealed partial class Series
{
    private sealed class FoldTrace
    {
        public bool Changed { get; set; }
        public bool StatusChanged { get; set; }
        public StageOverrideId? OverrideId { get; set; }
        public List<SeriesStageId> Computed { get; } = [];
        public List<SeriesStageId> Stale { get; } = [];
        public List<SeriesStageId> Official { get; } = [];
    }

    /// <summary>The last Qualifying Stage. The Backfill belongs to it.</summary>
    public SeriesStage? LastQualifyingStage
        => Ordered().LastOrDefault(stage => stage.Kind == StageKind.Qualifying);

    /// <summary>True when a Survivor Stage resolved to the squad.</summary>
    public bool HasStageResolvedTo(SquadId squadId)
        => _stages.Exists(stage => stage.Kind == StageKind.Survivor && stage.SquadDesignation?.SquadId == squadId);

    // ---------- entry points ----------

    /// <summary>
    /// Event entry. Re-evaluates the Stage linked to the tournament and every later Stage in order: it resolves a Survivor
    /// squad, computes a Pending Stage that may compute, replaces a Computed Stage's output when it changed, and marks an
    /// Official Stage Stale when a dry compute differs (it never rewrites it). Writing nothing leaves <see cref="Version"/> alone.
    /// </summary>
    public AdvanceResult Advance(TournamentId tournamentId, AdvancementFacts facts, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var stage = _stages.Find(s => s.TournamentId == tournamentId);
        if (stage is null)
        {
            return AdvanceResult.None;
        }

        var trace = new FoldTrace();
        Fold(stage.Order, force: false, facts, now, trace);

        return Finish(trace);
    }

    /// <summary>
    /// Forced fold from a Stage to the end. Official Stages in the range need <paramref name="confirmResetOfficial"/>; they
    /// reset to Computed and lose Stale. A recompute that changes nothing writes nothing.
    /// </summary>
    public ErrorOr<AdvanceResult> Recompute(
        SeriesStageId fromStageId, bool confirmResetOfficial, int expectedVersion, AdvancementFacts facts, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var stage = _stages.Find(s => s.Id == fromStageId);
        if (stage is null)
        {
            return SeriesAdvancementErrors.StageNotFound(fromStageId);
        }

        var version = EnsureVersion(expectedVersion);
        if (version.IsError)
        {
            return version.Errors;
        }

        var officialOrders = _stages
            .Where(s => s.Order >= stage.Order && s.Status == StageStatus.Official)
            .Select(s => s.Order).Order().ToList();

        if (officialOrders.Count > 0 && !confirmResetOfficial)
        {
            return SeriesAdvancementErrors.ConfirmationRequired(officialOrders);
        }

        var trace = new FoldTrace();
        Fold(stage.Order, force: true, facts, now, trace);

        return Finish(trace);
    }

    /// <summary>
    /// Makes every Computed Qualifying or Survivor Stage whose grace period passed Official, in order. A Stage waits for every
    /// earlier one, is skipped while stale, and the last Qualifying Stage waits for the Backfill. A Placement Stage never becomes Official.
    /// </summary>
    public AdvanceResult MakeDueOfficial(DateTimeOffset now, TimeSpan grace)
    {
        var trace = new FoldTrace();

        foreach (var stage in Ordered().Where(s => s.Kind != StageKind.Placement))
        {
            if (stage.Status == StageStatus.Official)
            {
                continue;
            }

            if (OfficialDueAt(stage, grace) is not { } due || due > now)
            {
                break;
            }

            stage.MarkOfficial(now);
            trace.Changed = true;
            trace.StatusChanged = true;
            trace.Official.Add(stage.Id);
        }

        return Finish(trace);
    }

    /// <summary>When the Stage will become Official, or null. See <see cref="SeriesAdvancementRules.OfficialDueAt"/>.</summary>
    public DateTimeOffset? OfficialDueAt(SeriesStage stage, TimeSpan grace)
    {
        ArgumentNullException.ThrowIfNull(stage);

        return SeriesAdvancementRules.OfficialDueAt(
            stage.Kind, stage.Status, stage.IsStale, stage.ComputedAt,
            awaitingBackfill: LastQualifyingStage?.Id == stage.Id && BackfillAsOf is null,
            grace);
    }

    // ---------- squad ----------

    /// <summary>
    /// Sets the Survivor Stage's squad, then re-evaluates from that Stage. Allowed in any status: an Official Stage that
    /// lost its squad needs this before an admin recompute can reset it.
    /// </summary>
    public ErrorOr<AdvanceResult> PickSquad(
        SeriesStageId stageId, SquadId squadId, int expectedVersion, AdvancementFacts facts, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var stage = _stages.Find(s => s.Id == stageId);
        if (stage is null)
        {
            return SeriesAdvancementErrors.StageNotFound(stageId);
        }

        var version = EnsureVersion(expectedVersion);
        if (version.IsError)
        {
            return version.Errors;
        }

        if (stage.Kind != StageKind.Survivor)
        {
            return SeriesAdvancementErrors.StageNotSurvivor;
        }

        if (!facts.Tournaments.TryGetValue(stage.TournamentId, out var tournament)
            || !tournament.Squads.Any(s => s.SquadId == squadId))
        {
            return SeriesAdvancementErrors.SquadNotInTournament;
        }

        if (stage.SquadDesignation?.SquadId == squadId)
        {
            return AdvanceResult.None;
        }

        stage.ResolveSquad(squadId);

        var trace = new FoldTrace { Changed = true };
        Fold(stage.Order, force: false, facts, now, trace);

        return Finish(trace);
    }

    // ---------- overrides ----------

    /// <summary>Adds a bowler who has a result row in the Stage's tournament, then recomputes inline.</summary>
    public ErrorOr<AdvanceResult> AddOverride(
        SeriesStageId stageId, BowlerId bowlerId, string reason, string createdBy,
        int expectedVersion, AdvancementFacts facts, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var target = OverrideTarget(stageId, expectedVersion);
        if (target.IsError)
        {
            return target.Errors;
        }

        var stage = target.Value;

        if (!facts.Tournaments.TryGetValue(stage.TournamentId, out var tournament)
            || tournament.Results.All(row => row.BowlerId != bowlerId))
        {
            return SeriesAdvancementErrors.NoResultRow;
        }

        if (_fieldEntries.Exists(e => e.BowlerId == bowlerId))
        {
            return SeriesAdvancementErrors.BowlerAlreadyInField;
        }

        if (stage.Overrides.Any(o => o.BowlerId == bowlerId))
        {
            return SeriesAdvancementErrors.OverrideExists;
        }

        return ApplyOverride(stage, OverrideKind.Add, bowlerId, reason, createdBy, facts, now);
    }

    /// <summary>Vacates the slot a bowler holds in the Stage and bars the bowler from the Backfill, then recomputes inline.</summary>
    public ErrorOr<AdvanceResult> ClearSlot(
        SeriesStageId stageId, BowlerId bowlerId, string reason, string createdBy,
        int expectedVersion, AdvancementFacts facts, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var target = OverrideTarget(stageId, expectedVersion);
        if (target.IsError)
        {
            return target.Errors;
        }

        var stage = target.Value;

        if (stage.Overrides.Any(o => o.BowlerId == bowlerId))
        {
            return SeriesAdvancementErrors.OverrideExists;
        }

        if (!_fieldEntries.Exists(e => e.SourceStageId == stage.Id && !e.ViaOverride && e.BowlerId == bowlerId))
        {
            return SeriesAdvancementErrors.BowlerNotInSlot;
        }

        return ApplyOverride(stage, OverrideKind.Clear, bowlerId, reason, createdBy, facts, now);
    }

    /// <summary>
    /// Removes an override and recomputes inline. A missing override returns success before the version check, so a retry
    /// never conflicts.
    /// </summary>
    public ErrorOr<AdvanceResult> RemoveOverride(
        SeriesStageId stageId, StageOverrideId overrideId, int expectedVersion, AdvancementFacts facts, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var stage = _stages.Find(s => s.Id == stageId);
        if (stage is null)
        {
            return SeriesAdvancementErrors.StageNotFound(stageId);
        }

        var existing = stage.Overrides.FirstOrDefault(o => o.Id == overrideId);
        if (existing is null)
        {
            return AdvanceResult.None;
        }

        var target = OverrideTarget(stageId, expectedVersion);
        if (target.IsError)
        {
            return target.Errors;
        }

        stage.RemoveOverride(existing);

        var trace = new FoldTrace { Changed = true };
        Fold(stage.Order, force: false, facts, now, trace);

        return Finish(trace);
    }

    private ErrorOr<AdvanceResult> ApplyOverride(
        SeriesStage stage, OverrideKind kind, BowlerId bowlerId, string reason, string createdBy,
        AdvancementFacts facts, DateTimeOffset now)
    {
        var created = StageOverride.Create(kind, bowlerId, reason, createdBy, now);
        if (created.IsError)
        {
            return created.Errors;
        }

        stage.AddOverride(created.Value);

        var trace = new FoldTrace { Changed = true, OverrideId = created.Value.Id };
        Fold(stage.Order, force: false, facts, now, trace);

        return Finish(trace);
    }

    // Stage must exist (404), be Qualifying (422) and Computed (409), and the version must match (409).
    private ErrorOr<SeriesStage> OverrideTarget(SeriesStageId stageId, int expectedVersion)
    {
        var stage = _stages.Find(s => s.Id == stageId);
        if (stage is null)
        {
            return SeriesAdvancementErrors.StageNotFound(stageId);
        }

        var version = EnsureVersion(expectedVersion);
        if (version.IsError)
        {
            return version.Errors;
        }

        if (stage.Kind != StageKind.Qualifying)
        {
            return SeriesAdvancementErrors.NotQualifyingStage;
        }

        return stage.Status == StageStatus.Computed ? stage : SeriesAdvancementErrors.StageNotComputed(stage.Status);
    }

    // ---------- the fold ----------

    private List<SeriesStage> Ordered()
        => [.. _stages.OrderBy(stage => stage.Order)];

    private AdvanceResult Finish(FoldTrace trace)
    {
        if (trace.Changed)
        {
            Version++;
        }

        return new AdvanceResult
        {
            Changed = trace.Changed,
            StatusChanged = trace.StatusChanged,
            ComputedStageIds = trace.Computed,
            StaleStageIds = trace.Stale,
            OfficialStageIds = trace.Official,
            OverrideId = trace.OverrideId,
            Version = Version,
        };
    }

    // Walks the Stages from fromOrder in order. Each Stage reads the stored output of the Stages before it, which this
    // walk keeps current, so the result is a function of earlier output, results, overrides and facts.
    private void Fold(int fromOrder, bool force, AdvancementFacts facts, DateTimeOffset now, FoldTrace trace)
    {
        var ordered = Ordered();
        var previous = ordered.LastOrDefault(stage => stage.Order < fromOrder);

        foreach (var stage in ordered.Where(stage => stage.Order >= fromOrder))
        {
            if (!EvaluateStage(stage, previous, force, facts, now, trace))
            {
                return; // a Pending Stage that cannot compute leaves every later Stage Pending
            }

            previous = stage;
        }
    }

    // Returns false when the walk must stop.
    private bool EvaluateStage(
        SeriesStage stage, SeriesStage? previous, bool force, AdvancementFacts facts, DateTimeOffset now, FoldTrace trace)
    {
        facts.Tournaments.TryGetValue(stage.TournamentId, out var tournament);

        if (stage.Kind == StageKind.Survivor)
        {
            ResolveSquad(stage, tournament, trace);
        }

        if (!MayCompute(stage, previous, tournament))
        {
            if (stage.Status == StageStatus.Pending)
            {
                return false;
            }

            MarkStale(stage, trace); // stored output can no longer be reproduced; it stays as it is
            return true;
        }

        var computation = ComputeStage(stage, facts, tournament!);
        var same = SameOutput(stage, computation);

        if (stage.Status == StageStatus.Official && !force)
        {
            if (!same)
            {
                MarkStale(stage, trace);
            }

            return true;
        }

        if (stage.Status == StageStatus.Pending || !same || (force && stage.Status == StageStatus.Official))
        {
            var statusBefore = stage.Status;

            ApplyComputation(stage, computation);
            stage.MarkComputed(now);

            trace.Changed = true;
            trace.StatusChanged |= statusBefore != StageStatus.Computed;
            trace.Computed.Add(stage.Id);
        }
        else if (force && stage.IsStale)
        {
            stage.ClearStale();
            trace.Changed = true;
        }

        return true;
    }

    // A Stage may compute when its tournament is Completed or Truncated with at least one result row, the previous Stage is
    // not Pending, a non-Qualifying Stage has the Backfill behind it, and a Survivor Stage has a resolved squad that exists
    // and has at least one score row.
    private bool MayCompute(SeriesStage stage, SeriesStage? previous, StageTournamentResults? tournament)
    {
        if (tournament is null
            || tournament.Results.Count == 0
            || (tournament.Status != TournamentStatus.Completed && tournament.Status != TournamentStatus.Truncated))
        {
            return false;
        }

        if (previous is not null && previous.Status == StageStatus.Pending)
        {
            return false;
        }

        if (stage.Kind != StageKind.Qualifying && BackfillAsOf is null)
        {
            return false;
        }

        return stage.Kind != StageKind.Survivor
            || (stage.SquadDesignation?.SquadId is { } squadId
                && tournament.Squads.FirstOrDefault(s => s.SquadId == squadId) is { Totals.Count: > 0 });
    }

    private static void ResolveSquad(SeriesStage stage, StageTournamentResults? tournament, FoldTrace trace)
    {
        if (stage.SquadDesignation is not { } designation || tournament is null)
        {
            return;
        }

        var wasResolved = designation.SquadId is not null;

        if (designation.SquadId is { } current)
        {
            if (tournament.Squads.Any(s => s.SquadId == current))
            {
                return;
            }

            stage.UnresolveSquad(); // the squad vanished; fall through in case one with the same date and time exists now
            trace.Changed = true;
        }

        var matches = tournament.Squads
            .Where(s => s.LocalDate == designation.Date && ToMinute(s.LocalStartTime) == ToMinute(designation.StartTime))
            .ToList();

        if (matches.Count == 1)
        {
            stage.ResolveSquad(matches[0].SquadId);
            trace.Changed = true;
        }
        else if (wasResolved)
        {
            MarkStale(stage, trace);
        }
    }

    private static TimeOnly ToMinute(TimeOnly time)
        => new(time.Hour, time.Minute);

    private static void MarkStale(SeriesStage stage, FoldTrace trace)
    {
        if (stage.IsStale || stage.Status == StageStatus.Pending)
        {
            return;
        }

        stage.MarkStale();
        trace.Changed = true;
        trace.Stale.Add(stage.Id);
    }

    // ---------- compute ----------

    private StageComputation ComputeStage(SeriesStage stage, AdvancementFacts facts, StageTournamentResults tournament)
    {
        if (stage.Kind == StageKind.Qualifying)
        {
            var qualifying = QualifyingComputation.Compute(
                stage, tournament, BowlersQualifiedBefore(stage.Order), facts.MembersOn(tournament.EndDate), stage.Overrides);

            return LastQualifyingStage?.Id == stage.Id
                ? WithBackfill(stage, qualifying, facts, tournament)
                : qualifying;
        }

        if (stage.Kind == StageKind.Survivor)
        {
            var squad = tournament.Squads.Single(s => s.SquadId == stage.SquadDesignation!.SquadId!.Value);

            return SurvivorComputation.Compute(stage, FieldEnteringStage(stage), squad);
        }

        return PlacementComputation.Compute(FieldEnteringStage(stage), tournament, facts.SortNames, Finale?.FinalistCount);
    }

    private StageComputation WithBackfill(
        SeriesStage stage, StageComputation qualifying, AdvancementFacts facts, StageTournamentResults tournament)
    {
        var asOf = tournament.EndDate;

        var inField = BowlersQualifiedBefore(stage.Order);
        inField.UnionWith(qualifying.Entries.Select(e => e.BowlerId));

        var earlier = Ordered().Where(s => s.Kind == StageKind.Qualifying && s.Order < stage.Order).ToList();
        var vacancies = qualifying.Vacancies + earlier.Sum(VacanciesOf);

        if (vacancies == 0)
        {
            return qualifying with { BackfillAsOf = asOf }; // nothing to fill, so the Backfill has "run"
        }

        if (facts.Backfill is not { Ready: true } backfill || backfill.AsOf != asOf)
        {
            // Not ready. A Backfill that already ran stays (minus anyone who now qualifies outright); otherwise it has not run.
            return BackfillAsOf is null
                ? qualifying
                : qualifying with
                {
                    BackfillAsOf = BackfillAsOf,
                    BackfillEntries = [.. StoredDrafts(null).Where(d => !inField.Contains(d.BowlerId))],
                };
        }

        var appliedClearIds = qualifying.OverrideEffects.Where(e => e.Applied).Select(e => e.OverrideId).ToHashSet();
        var barred = earlier.SelectMany(s => s.Overrides)
            .Where(o => o.Kind == OverrideKind.Clear && o.Applied)
            .Concat(stage.Overrides.Where(o => o.Kind == OverrideKind.Clear && appliedClearIds.Contains(o.Id)))
            .Select(o => o.BowlerId)
            .ToHashSet();

        return qualifying with
        {
            BackfillAsOf = asOf,
            BackfillEntries = BackfillComputation.Compute(vacancies, inField, barred, facts.MembersOn(asOf), backfill.Points),
        };
    }

    private int VacanciesOf(SeriesStage stage)
        => (stage.SlotCount ?? 0) - _fieldEntries.Count(e => e.SourceStageId == stage.Id && !e.ViaOverride);

    private HashSet<BowlerId> BowlersQualifiedBefore(int order)
    {
        var earlierStageIds = _stages
            .Where(s => s.Kind == StageKind.Qualifying && s.Order < order)
            .Select(s => s.Id).ToHashSet();

        return [.. _fieldEntries
            .Where(e => e.SourceStageId is { } id && earlierStageIds.Contains(id))
            .Select(e => e.BowlerId)];
    }

    // Bowlers alive entering a non-Qualifying Stage: the previous Survivor Stage's advancers, else the whole Series Field.
    private List<BowlerId> FieldEnteringStage(SeriesStage stage)
    {
        var previousSurvivor = Ordered().LastOrDefault(s => s.Kind == StageKind.Survivor && s.Order < stage.Order);

        return previousSurvivor is null
            ? [.. _fieldEntries.Select(e => e.BowlerId)]
            : [.. previousSurvivor.SurvivorOutcomes.Where(o => o.Advanced).Select(o => o.BowlerId)];
    }

    // ---------- compare and write ----------

    private IEnumerable<FieldEntryDraft> StoredDrafts(SeriesStageId? source)
        => _fieldEntries.Where(e => e.SourceStageId == source).Select(e => e.ToDraft());

    private static bool SameSet<T>(IEnumerable<T> stored, IEnumerable<T> computed, Func<T, BowlerId> key)
        => stored.OrderBy(key).SequenceEqual(computed.OrderBy(key));

    private bool SameOutput(SeriesStage stage, StageComputation computed)
    {
        if (stage.Kind == StageKind.Survivor)
        {
            return SameSet(stage.SurvivorOutcomes.Select(o => o.ToDraft()), computed.Outcomes, d => d.BowlerId);
        }

        if (stage.Kind == StageKind.Placement)
        {
            return SameSet(_positions.Select(p => p.ToDraft()), computed.Positions, d => d.BowlerId);
        }

        var sameStage = stage.SlotCount == computed.SlotCount
            && SameSet(StoredDrafts(stage.Id), computed.Entries, d => d.BowlerId)
            && stage.Overrides.OrderBy(o => o.Id).Select(o => new OverrideEffect(o.Id, o.Applied))
                .SequenceEqual(computed.OverrideEffects.OrderBy(e => e.OverrideId));

        return sameStage
            && (LastQualifyingStage?.Id != stage.Id
                || (BackfillAsOf == computed.BackfillAsOf
                    && SameSet(StoredDrafts(null), computed.BackfillEntries, d => d.BowlerId)));
    }

    private void ApplyComputation(SeriesStage stage, StageComputation computed)
    {
        if (stage.Kind == StageKind.Survivor)
        {
            stage.ReplaceOutcomes(computed.Outcomes);
        }
        else if (stage.Kind == StageKind.Placement)
        {
            SyncPositions(computed.Positions);
        }
        else
        {
            stage.SetSlotCount(computed.SlotCount);
            SyncEntries(stage.Id, computed.Entries);

            foreach (var effect in computed.OverrideEffects)
            {
                stage.SetOverrideApplied(effect.OverrideId, effect.Applied);
            }

            if (LastQualifyingStage?.Id == stage.Id)
            {
                BackfillAsOf = computed.BackfillAsOf;
                SyncEntries(null, computed.BackfillEntries);
            }
        }
    }

    // Updates in place within the source (same bowler, same row), removes the rest, adds new ones. Never delete and re-add one
    // bowler inside a source, which the unique (series, bowler) index would punish.
    private void SyncEntries(SeriesStageId? source, IReadOnlyList<FieldEntryDraft> target)
    {
        var scope = _fieldEntries.Where(e => e.SourceStageId == source).ToList();

        foreach (var gone in scope.Where(e => target.All(d => d.BowlerId != e.BowlerId)))
        {
            _fieldEntries.Remove(gone);
        }

        foreach (var draft in target)
        {
            var existing = scope.Find(e => e.BowlerId == draft.BowlerId);
            if (existing is null)
            {
                _fieldEntries.Add(SeriesFieldEntry.Create(
                    draft.BowlerId, draft.SourceStageId, draft.Place, draft.BackfillPoints, draft.ViaOverride).Value);
            }
            else
            {
                _ = existing.Update(draft.Place, draft.BackfillPoints, draft.ViaOverride).Value;
            }
        }
    }

    // Composite key (series, bowler): update in place, so a bowler whose position changes is one UPDATE, not a delete and re-add.
    private void SyncPositions(IReadOnlyList<PositionDraft> target)
    {
        _positions.RemoveAll(p => target.All(d => d.BowlerId != p.BowlerId));

        foreach (var draft in target)
        {
            var existing = _positions.Find(p => p.BowlerId == draft.BowlerId);
            if (existing is null)
            {
                _positions.Add(SeriesPosition.Create(draft.BowlerId, draft.Position, draft.Bowled, draft.IsFinalist).Value);
            }
            else
            {
                _ = existing.Update(draft.Position, draft.Bowled, draft.IsFinalist).Value;
            }
        }
    }
}
```

Status rules, checked against the plan:
- Pending to Computed happens in `EvaluateStage`.
- Computed to Official happens in `MakeDueOfficial`.
- Official to Computed happens only on a forced recompute.
- A Stage never returns to Pending.
- `ComputedAt` moves only when output changes or on a forced reset.
- A no-op recompute writes nothing and leaves `Version` alone.

---

#### F. Handlers and jobs

##### `Features/Series/SeriesAdvancement.cs`
```csharp
using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.BackgroundJobs;
using Neba.Api.Caching;
using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

using ZiggyCreatures.Caching.Fusion;

using SeriesAggregate = Neba.Api.Features.Series.Domain.Series;

namespace Neba.Api.Features.Series;

internal delegate ErrorOr<AdvanceResult> AdvancementOperation(SeriesAggregate series, AdvancementFacts facts, DateTimeOffset now);

/// <summary>
/// The single entry point for advancement. Every event job and every admin write runs through <see cref="RunAsync"/>.
/// It must be the first load of the Series in its DbContext scope.
/// </summary>
internal sealed class SeriesAdvancement(
    AppDbContext appDbContext,
    ISeriesAdvancementFactsReader factsReader,
    IKeyedLockProvider keyedLocks,
    IFusionCache cache,
    TimeProvider timeProvider,
    ILogger<SeriesAdvancement> logger)
{
    internal static readonly TimeSpan AdminLockWait = TimeSpan.FromSeconds(10);
    internal static readonly TimeSpan JobLockWait = TimeSpan.FromSeconds(60);

    public async Task<ErrorOr<AdvanceResult>> RunAsync(
        SeriesId seriesId, AdvancementOperation operation, TimeSpan lockWait, bool needsFacts, CancellationToken cancellationToken)
    {
        using var held = await keyedLocks.TryAcquireAsync($"series-advancement:{seriesId}", lockWait, cancellationToken);
        if (held is null)
        {
            return SeriesAdvancementErrors.Busy;
        }

        // Loaded after the lock, so a run that queued behind another sees what that run saved.
        var series = await appDbContext.Series
            .Include(s => s.Stages).ThenInclude(stage => stage.SurvivorOutcomes)
            .Include(s => s.Stages).ThenInclude(stage => stage.Overrides)
            .Include(s => s.FieldEntries)
            .Include(s => s.Positions)
            .AsSplitQuery()
            .SingleOrDefaultAsync(s => s.Id == seriesId, cancellationToken);

        if (series is null)
        {
            return SeriesErrors.NotFound(seriesId);
        }

        var facts = needsFacts ? await factsReader.ReadAsync(series, cancellationToken) : AdvancementFacts.Empty;

        var outcome = operation(series, facts, timeProvider.GetUtcNow());
        if (outcome.IsError || !outcome.Value.Changed)
        {
            return outcome;
        }

        // One SaveChanges is one transaction: output is rebuilt or not at all.
        var saved = await SeriesConcurrency.SaveOrConflictAsync(appDbContext, cancellationToken);
        if (saved.IsError)
        {
            return saved.Errors;
        }

        if (outcome.Value.StatusChanged)
        {
            await cache.RemoveByTagAsync(CacheDescriptors.Tournaments.Tag, token: cancellationToken);
        }

        logger.LogAdvancementApplied(
            seriesId, outcome.Value.ComputedStageIds.Count, outcome.Value.StaleStageIds.Count, outcome.Value.Version);

        return outcome;
    }

    /// <summary>Job entry. Finds the Series the tournament belongs to (most have none) and runs the operation.</summary>
    public async Task RunForTournamentAsync(TournamentId tournamentId, AdvancementOperation operation, CancellationToken cancellationToken)
    {
        var seriesId = await appDbContext.Series.AsNoTracking()
            .Where(s => s.Stages.Any(stage => stage.TournamentId == tournamentId))
            .Select(s => (SeriesId?)s.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (seriesId is { } id)
        {
            EnsureJobSucceeded(await RunAsync(id, operation, JobLockWait, needsFacts: true, cancellationToken));
        }
    }

    /// <summary>
    /// A deleted Series is fine. Anything else throws so Hangfire retries; a rerun is idempotent, and a
    /// <c>DbUpdateConcurrencyException</c> becomes the conflict error that lands here.
    /// </summary>
    internal static void EnsureJobSucceeded(ErrorOr<AdvanceResult> result)
    {
        if (result.IsError && result.FirstError.Type != ErrorType.NotFound)
        {
            throw new InvalidOperationException(
                $"Series advancement failed: {string.Join(", ", result.Errors.Select(e => e.Code))}");
        }
    }
}

internal static partial class SeriesAdvancementLogMessages
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Series {SeriesId} advancement saved: {ComputedStages} Stage(s) computed, {StaleStages} marked stale, version {Version}.")]
    public static partial void LogAdvancementApplied(
        this ILogger<SeriesAdvancement> logger, SeriesId seriesId, int computedStages, int staleStages, int version);
}
```

##### `Features/Series/SeriesConcurrency.cs`
```csharp
using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Series;

internal static class SeriesConcurrency
{
    /// <summary>Saves, turning a race with an advancement job (the Version token) into a 409 instead of a 500.</summary>
    public static async Task<ErrorOr<Success>> SaveOrConflictAsync(AppDbContext appDbContext, CancellationToken cancellationToken)
    {
        try
        {
            await appDbContext.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return SeriesAdvancementErrors.ConcurrentChange;
        }
    }
}
```

##### Phase 1 handler edits

`EditSeries/EditSeriesCommandHandler.cs`
```csharp
// read facts for the submitted Stages and the stored ones (a removed Stage's tournament decides the cancelled-removal rule)
        var tournamentIds = command.Stages.Select(s => s.TournamentId)
            .Concat(series.Stages.Select(s => s.TournamentId))
            .Distinct()
            .ToList();

        var tournaments = await StageTournamentFactsReader.ReadAsync(
            appDbContext, tournamentIds, command.SeriesId, cancellationToken);

// replace "await appDbContext.SaveChangesAsync(cancellationToken);"
        var saved = await SeriesConcurrency.SaveOrConflictAsync(appDbContext, cancellationToken);
        if (saved.IsError)
        {
            return saved.Errors;
        }
```
`EditSeriesLogo/EditSeriesLogoCommandHandler.cs`: replace its `SaveChangesAsync` with the same `saved` block. Both handlers load `Series` tracked, so a job's bump makes the update conflict.

Edge to note: a sibling check in `ReplaceConfiguration` uses `tournaments.GetValueOrDefault` on stored Stages when unchanged, so the larger fact set is harmless.

##### Event jobs (usings first file: `Neba.Api.BackgroundJobs`, `Neba.Api.Features.Tournaments.Domain`)

```csharp
// === Features/Series/AdvanceOnResultsRecorded/AdvanceOnResultsRecorded.cs ===
namespace Neba.Api.Features.Series.AdvanceOnResultsRecorded;

internal sealed class AdvanceOnResultsRecorded(SeriesAdvancement advancement)
    : IDomainEventJob<TournamentResultsRecorded>
{
    public Task HandleAsync(TournamentResultsRecorded domainEvent, CancellationToken ct)
        => advancement.RunForTournamentAsync(
            domainEvent.TournamentId, (series, facts, now) => series.Advance(domainEvent.TournamentId, facts, now), ct);
}

// === Features/Series/AdvanceOnSquadsChanged/AdvanceOnSquadsChanged.cs ===
internal sealed class AdvanceOnSquadsChanged(SeriesAdvancement advancement)
    : IDomainEventJob<TournamentSquadsChanged>
{
    // Advance resolves the Survivor squad first, so a new or changed squad resolves here.
    public Task HandleAsync(TournamentSquadsChanged domainEvent, CancellationToken ct)
        => advancement.RunForTournamentAsync(
            domainEvent.TournamentId, (series, facts, now) => series.Advance(domainEvent.TournamentId, facts, now), ct);
}

// === Features/Series/AdvanceOnSquadScoresReplaced/AdvanceOnSquadScoresReplaced.cs ===
internal sealed class AdvanceOnSquadScoresReplaced(SeriesAdvancement advancement)
    : IDomainEventJob<SquadScoresReplaced>
{
    // Acts only when a Survivor Stage resolved to that squad.
    public Task HandleAsync(SquadScoresReplaced domainEvent, CancellationToken ct)
        => advancement.RunForTournamentAsync(
            domainEvent.TournamentId,
            (series, facts, now) => series.HasStageResolvedTo(domainEvent.SquadId)
                ? series.Advance(domainEvent.TournamentId, facts, now)
                : AdvanceResult.None,
            ct);
}

// === Features/Series/BackfillOnSeasonStatsGenerated/BackfillOnSeasonStatsGenerated.cs ===
using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Stats.Domain;

namespace Neba.Api.Features.Series.BackfillOnSeasonStatsGenerated;

internal sealed class BackfillOnSeasonStatsGenerated(AppDbContext appDbContext, SeriesAdvancement advancement)
    : IDomainEventJob<SeasonStatsGenerated>
{
    // The event is a signal to try. The reader checks readiness itself, and the fold runs the Backfill as part of
    // the last Qualifying Stage. A re-sent event on an official Stage is a dry compute that can only set Stale.
    public async Task HandleAsync(SeasonStatsGenerated domainEvent, CancellationToken ct)
    {
        var seriesIds = await appDbContext.Series.AsNoTracking()
            .Where(s => s.SeasonId == domainEvent.SeasonId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        foreach (var seriesId in seriesIds)
        {
            SeriesAdvancement.EnsureJobSucceeded(await advancement.RunAsync(
                seriesId,
                (series, facts, now) => series.LastQualifyingStage is { } last
                    ? series.Advance(last.TournamentId, facts, now)
                    : AdvanceResult.None,
                SeriesAdvancement.JobLockWait,
                needsFacts: true,
                ct));

            appDbContext.ChangeTracker.Clear();
        }
    }
}
```

##### Make-official job

```csharp
// === Features/Series/MakeDueStagesOfficial/MakeDueStagesOfficialJob.cs ===
using Neba.Api.BackgroundJobs;

namespace Neba.Api.Features.Series.MakeDueStagesOfficial;

internal sealed record MakeDueStagesOfficialJob
    : IBackgroundJob
{
    public string JobName
        => "Make Due Series Stages Official";
}

// === Features/Series/MakeDueStagesOfficial/MakeDueStagesOfficialJobHandler.cs ===
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Neba.Api.BackgroundJobs;
using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Series.MakeDueStagesOfficial;

internal sealed class MakeDueStagesOfficialJobHandler(
    AppDbContext appDbContext,
    SeriesAdvancement advancement,
    IOptions<SeriesAdvancementSettings> settings)
    : IBackgroundJobHandler<MakeDueStagesOfficialJob>
{
    public async Task ExecuteAsync(MakeDueStagesOfficialJob job, CancellationToken cancellationToken)
    {
        var grace = settings.Value.OfficialGrace;

        var seriesIds = await appDbContext.Series.AsNoTracking()
            .Where(s => s.Stages.Any(stage => stage.Status == StageStatus.Computed && stage.Kind != StageKind.Placement))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var failed = new List<string>();

        foreach (var seriesId in seriesIds)
        {
            var result = await advancement.RunAsync(
                seriesId,
                (series, _, now) => series.MakeDueOfficial(now, grace),
                SeriesAdvancement.JobLockWait,
                needsFacts: false,
                cancellationToken);

            if (result.IsError && result.FirstError.Type != ErrorType.NotFound)
            {
                failed.Add(seriesId.ToString()); // one Series must not block the others
            }

            appDbContext.ChangeTracker.Clear();
        }

        if (failed.Count > 0)
        {
            // Hangfire retries the sweep; making a Stage Official is idempotent.
            throw new InvalidOperationException($"Making Stages Official failed for Series {string.Join(", ", failed)}.");
        }
    }
}
```
Add `using ErrorOr;` to the handler.

##### Settings, registration, recurring job

```csharp
// === Features/Series/SeriesAdvancementSettings.cs ===
using System.ComponentModel.DataAnnotations;

namespace Neba.Api.Features.Series;

internal sealed class SeriesAdvancementSettings
{
    public const string SectionName = "SeriesAdvancement";

    /// <summary>Hours between a Stage's output last changing and it becoming Official automatically. 24 in production.</summary>
    [Range(0, 168)]
    public int OfficialGraceHours { get; init; } = 24;

    public TimeSpan OfficialGrace
        => TimeSpan.FromHours(OfficialGraceHours);
}

// === Features/Series/SeriesFeatureConfiguration.cs ===
using Neba.Api.BackgroundJobs;
using Neba.Api.Features.Series.MakeDueStagesOfficial;

namespace Neba.Api.Features.Series;

internal static class SeriesFeatureConfiguration
{
    private const string MakeOfficialRecurringJobId = "series-make-official";

    extension(IServiceCollection services)
    {
        public IServiceCollection AddSeries(IConfiguration configuration)
        {
            services.AddOptions<SeriesAdvancementSettings>()
                .Bind(configuration.GetSection(SeriesAdvancementSettings.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddScoped<SeriesAdvancement>();
            services.AddScoped<ISeriesAdvancementFactsReader, SeriesAdvancementFactsReader>();
            services.AddScoped<IMemberOnDateReader, MemberOnDateReader>();
            services.AddScoped<IBoyPointsReader, BoyPointsReader>();
            services.AddScoped<IBowlerSortNameReader, BowlerSortNameReader>();
            services.AddScoped<IAdvancementOutputReader, AdvancementOutputReader>();
            services.AddScoped<IStageResultReader, StageResultReader>();
            services.AddScoped<IStageSquadReader, StageSquadReader>();

            return services;
        }
    }

    extension(WebApplication app)
    {
        public void UseSeriesMakeOfficialJob()
        {
            using var scope = app.Services.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<IBackgroundJobScheduler>();

            // Every 15 minutes, so the change lands up to 15 minutes after the grace period ("Becomes official about {time}").
            scheduler.AddOrUpdateRecurring(MakeOfficialRecurringJobId, new MakeDueStagesOfficialJob(), "*/15 * * * *");
        }
    }
}
```
Wire-up in `src/Neba.Api/InfrastructureConfiguration.cs`:
```csharp
            builder.Services.AddTournaments(builder.Configuration);
            builder.Services.AddSeries(builder.Configuration);   // new
...
            app.UseSeasonRolloverJob();
            app.UseSeriesMakeOfficialJob();                          // new
```
`src/Neba.Api/appsettings.json` (after `OilPatternAnnouncement`):
```json
  "SeriesAdvancement": {
    "OfficialGraceHours": 24
  },
```
The `appsettings.Development.json` override is optional, for example 0.

##### `Features/Series/EasternTime.cs`
```csharp
namespace Neba.Api.Features.Series;

// Squad times are stored in UTC; bowlers and the Series designation use Eastern wall-clock time. Non-legacy copy of
// LegacyTimeZones.Eastern, since Features must not depend on /legacy.
internal static class EasternTime
{
    private static readonly TimeZoneInfo Zone = Find();

    public static (DateOnly Date, TimeOnly Time) ToLocal(DateTimeOffset instant)
    {
        var local = TimeZoneInfo.ConvertTime(instant, Zone).DateTime;

        return (DateOnly.FromDateTime(local), TimeOnly.FromDateTime(local));
    }

    private static TimeZoneInfo Find()
    {
        foreach (var id in new[] { "America/New_York", "Eastern Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // try the next id
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("Eastern (fixed)", TimeSpan.FromHours(-5), "Eastern (fixed)", "Eastern (fixed)");
    }
}
```
The domain compares to the minute (`ToMinute`).

---

#### G. Reader contracts (for `efcore`; the interfaces and records are mine, the SQL is theirs)

All live in the Series namespace, use `AppDbContext`, `AsNoTracking`, flat projections. Note the shapes the domain needs.

##### Entity shapes the readers and `Database/Configurations` must fit

| Type | Key and columns |
|---|---|
| `Series` | add `BackfillAsOf` (`DateOnly?`) and `Version` (`int`, concurrency token). Navigations `Stages`, `FieldEntries`, `Positions` use the backing fields `_stages`, `_fieldEntries`, `_positions` (`PropertyAccessMode.Field`; no `= []` auto-property, per the learnings). |
| `SeriesStage` | add `IsStale` (`bool`), `ComputedAt` and `OfficialAt` (`DateTimeOffset?`), `SlotCount` (`int?`). Navigations `SurvivorOutcomes` and `Overrides` use the backing fields `_survivorOutcomes` and `_overrides`. |
| `SeriesFieldEntry` | `Id` is `SeriesFieldEntryId` (ULID alternate key; shadow int PK per the repo pattern). Columns `BowlerId`, `SourceStageId` (nullable typed id, so `HasColumnName`/`HasMaxLength`/`IsFixedLength` rather than `IsUlid`), `Place`, `BackfillPoints`, `ViaOverride`. Unique `(series, BowlerId)`. |
| `SurvivorOutcome` | key `(stage, BowlerId)`; `BlockTotal` (`int?`), `Advanced`. |
| `SeriesPosition` | key `(series, BowlerId)`; `Position`, `Bowled`, `IsFinalist` (`bool?`). The `(series, position)` index must be **non-unique**: an in-place swap of two positions would otherwise cycle. |
| `StageOverride` | `Id` is `StageOverrideId` (ULID alternate key); `BowlerId`, `Kind` (SmartEnum, store the value), `Reason` (max 500), `CreatedBy` (string, max 64, the actor ID), `CreatedAt`, `Applied`. Unique `(stage, BowlerId)`; `BowlerId` FK Restrict. |
| `SquadDesignation` | the nullable complex type keeps `SquadId` (nullable typed id); no FK. |

Each bowler-keyed type has `internal Bowler Bowler { get; init; } = null!;` for the real FK, like `TournamentResult.Bowler`.

##### `ISeriesAdvancementFactsReader` (composes the rest; used by `SeriesAdvancement`)
```csharp
internal interface ISeriesAdvancementFactsReader
{
    Task<AdvancementFacts> ReadAsync(SeriesAggregate series, CancellationToken cancellationToken);
}
```
Must return:
- **`Tournaments`**: one `StageTournamentResults` per Stage tournament. `Results` holds every `TournamentResult` row (`BowlerId`, `Place`) when the status is Completed or Truncated, and is empty otherwise. `Squads` is filled only for Survivor Stage tournaments: each squad's `LocalDate` and `LocalStartTime` from `EasternTime.ToLocal(Squad.BowlingDateTimeUtc)`, and `Totals` (one row per bowler: `Total = SUM(Score)`, `Games = COUNT(*)`) from `SquadScore`.
- **`MembersOnDate`**: for the `EndDate` of every Qualifying Stage tournament (this includes the Backfill as-of date). Each value is the set of bowlers with a membership where `StartDate <= date <= EndDate`, skipping legacy System Import.
- **`Backfill`**: null when the Series has no Qualifying Stage. Otherwise `AsOf` is the last Qualifying Stage's tournament `EndDate` (use `series.LastQualifyingStage`). `Ready` is true when no stats-eligible tournament up to `AsOf` is still Scheduled and the finalized ones have results. `Points` holds one row per bowler with open Bowler of the Year points above zero for the Series' season as of `AsOf`: website `TournamentResult.Points` over Completed and Truncated, stats-eligible tournaments with `EndDate <= AsOf`, plus the Tournament of Champions double-dip exclusion.
- **`SortNames`**: last and first name for every bowler who could be in the field. That is every bowler with a result row in any Stage tournament, every bowler in `Backfill.Points`, and every bowler in the stored `FieldEntries`.

##### Collaborators it uses
```csharp
internal interface IMemberOnDateReader
{
    Task<IReadOnlyDictionary<DateOnly, IReadOnlySet<BowlerId>>> ReadAsync(IReadOnlyCollection<DateOnly> dates, CancellationToken cancellationToken);
}

internal interface IBoyPointsReader
{
    /// <summary>One row per bowler with points above zero. Computes Ready as described above.</summary>
    Task<BackfillFacts> ReadAsync(SeasonId seasonId, DateOnly asOf, CancellationToken cancellationToken);
}

internal interface IBowlerSortNameReader
{
    Task<IReadOnlyDictionary<BowlerId, BowlerSortName>> ReadAsync(IReadOnlyCollection<BowlerId> bowlerIds, CancellationToken cancellationToken);
}
```

##### `IAdvancementOutputReader` (for `GetSeriesAdvancement`; five flat queries plus the header, no cache)
```csharp
internal interface IAdvancementOutputReader
{
    /// <summary>Null when the Series does not exist. That is the only reason for null.</summary>
    Task<AdvancementOutputRows?> ReadAsync(SeriesId seriesId, CancellationToken cancellationToken);
}

internal sealed record AdvancementOutputRows(
    int Version,
    DateOnly? BackfillAsOf,
    IReadOnlyList<AdvancementStageRow> Stages,
    IReadOnlyList<AdvancementEntryRow> Entries,
    IReadOnlyList<AdvancementOutcomeRow> Outcomes,
    IReadOnlyList<AdvancementOverrideRow> Overrides,
    IReadOnlyList<AdvancementPositionRow> Positions);

internal sealed record AdvancementStageRow(
    SeriesStageId StageId, int Order, StageKind Kind, StageStatus Status, bool IsStale,
    DateTimeOffset? ComputedAt, DateTimeOffset? OfficialAt, int? SlotCount, int? AdvancingCount, int TeamSize,
    TournamentId TournamentId, TournamentStatus TournamentStatus,
    SquadId? ResolvedSquadId, bool ResolvedSquadExists);

internal sealed record AdvancementEntryRow(
    SeriesStageId? SourceStageId, BowlerId BowlerId, string BowlerName, int? Place, int? BackfillPoints, bool ViaOverride);

internal sealed record AdvancementOutcomeRow(SeriesStageId StageId, BowlerId BowlerId, string BowlerName, int? BlockTotal, bool Advanced);

internal sealed record AdvancementOverrideRow(
    SeriesStageId StageId, StageOverrideId OverrideId, BowlerId BowlerId, string BowlerName, OverrideKind Kind,
    string Reason, string CreatedBy, DateTimeOffset CreatedAt, bool Applied);

internal sealed record AdvancementPositionRow(BowlerId BowlerId, string BowlerName, int Position, bool Bowled, bool? IsFinalist);
```
`TeamSize` comes from `Tournament.TournamentType.TeamSize`. `ResolvedSquadExists` is a lookup against `squads`, because there is no FK.

##### Picker readers
```csharp
internal interface IStageResultReader
{
    /// <summary>NotFound(series) or StageNotFound; otherwise every result row of the Stage's tournament, ordered by place then name.</summary>
    Task<ErrorOr<IReadOnlyList<StageResultRow>>> ReadAsync(SeriesId seriesId, SeriesStageId stageId, CancellationToken cancellationToken);
}

internal sealed record StageResultRow(BowlerId BowlerId, string BowlerName, int Place);

internal interface IStageSquadReader
{
    /// <summary>NotFound(series) or StageNotFound; otherwise the Stage tournament's squads, ordered by start.</summary>
    Task<ErrorOr<IReadOnlyList<StageSquadRow>>> ReadAsync(SeriesId seriesId, SeriesStageId stageId, CancellationToken cancellationToken);
}

internal sealed record StageSquadRow(SquadId SquadId, DateOnly LocalDate, TimeOnly LocalStartTime, bool HasScores, bool IsSelected);
```
This `StageResultRow` is the Series-namespace reader row. It does not clash with the domain's `StageResultRow(BowlerId, Place)`, which lives in `...Series.Domain`. Rename the reader's to `StageResultPickRow` if the compiler prefers.

---

#### H. Use cases (all under `src/Neba.Api/Features/Series/`)

Usings, first file of each kind, then "as above".

##### Shared validator rule and result sender

```csharp
// === SeriesRequestRules.cs ===
using FluentValidation;

namespace Neba.Api.Features.Series;

internal static class SeriesRequestRules
{
    extension<T>(IRuleBuilder<T, string> rule)
    {
        public IRuleBuilderOptions<T, string> IsUlid(string label, string codePrefix)
            => rule.NotEmpty().WithErrorCode($"{codePrefix}Required").WithMessage($"{label} is required.")
                .Length(26).WithErrorCode($"{codePrefix}InvalidLength").WithMessage($"{label} must be a 26-character ULID.");
    }
}

// === SeriesAdvancementResultSender.cs ===
using ErrorOr;

using FluentValidation.Results;

namespace Neba.Api.Features.Series;

internal static class SeriesAdvancementResultSender
{
    // NotFound is 404; Conflict is 409; everything else is 422 (SeriesMutationResultSender decides 409 vs 422).
    public static Task SendAsync(
        IReadOnlyList<Error> errors,
        Action<ValidationFailure> addError,
        Func<int, CancellationToken, Task> sendErrorsAsync,
        Func<CancellationToken, Task> sendNotFoundAsync,
        CancellationToken ct)
        => errors[0].Type == ErrorType.NotFound
            ? sendNotFoundAsync(ct)
            : SeriesMutationResultSender.SendAsync(errors, addError, sendErrorsAsync, ct);
}
```

##### 1. `GetSeriesAdvancement` (`GET /series/{id}/advancement`, `Series.EditSeries`, 200 / 401 / 403 / 404)

```csharp
// === GetSeriesAdvancement/GetSeriesAdvancementRequest.cs ===
using FastEndpoints;

namespace Neba.Api.Features.Series.GetSeriesAdvancement;

internal sealed class GetSeriesAdvancementRequest
{
    [BindFrom("id")]
    public required string SeriesId { get; set; }
}

// === GetSeriesAdvancement/GetSeriesAdvancementRequestValidator.cs ===
using FastEndpoints;

namespace Neba.Api.Features.Series.GetSeriesAdvancement;

internal sealed class GetSeriesAdvancementRequestValidator : Validator<GetSeriesAdvancementRequest>
{
    public GetSeriesAdvancementRequestValidator()
        => RuleFor(r => r.SeriesId).IsUlid("Series ID", "GetSeriesAdvancementRequest.SeriesId");
}

// === GetSeriesAdvancement/GetSeriesAdvancementQuery.cs ===
using ErrorOr;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.GetSeriesAdvancement;

internal sealed record GetSeriesAdvancementQuery
    : IQuery<ErrorOr<SeriesAdvancementDto>>
{
    public required SeriesId SeriesId { get; init; }
}

// === GetSeriesAdvancement/SeriesAdvancementDto.cs ===
internal sealed record SeriesAdvancementDto(
    int Version, DateOnly? BackfillAsOf, int TargetField,
    IReadOnlyList<StageAdvancementDto> Stages, IReadOnlyList<SeriesPositionDto> Positions);

internal sealed record StageAdvancementDto(
    SeriesStageId StageId, int Order, string Kind, string Status, bool IsStale, bool NeedsSquad,
    DateTimeOffset? ComputedAt, DateTimeOffset? OfficialDueAt, DateTimeOffset? OfficialAt,
    int? SlotCount, int? VacantSlotCount, TournamentId TournamentId, string TournamentStatus, SquadId? ResolvedSquadId,
    IReadOnlyList<FieldEntryDto> Entries, IReadOnlyList<StageOverrideDto> Overrides, IReadOnlyList<SurvivorOutcomeDto> SurvivorOutcomes);

internal sealed record FieldEntryDto(BowlerId BowlerId, string BowlerName, SeriesStageId? SourceStageId, int? Place, int? BackfillPoints, bool ViaOverride);

internal sealed record StageOverrideDto(
    StageOverrideId OverrideId, BowlerId BowlerId, string BowlerName, string Kind, string Reason, string CreatedBy, DateTimeOffset CreatedAt, bool Applied);

internal sealed record SurvivorOutcomeDto(BowlerId BowlerId, string BowlerName, int? BlockTotal, bool Advanced);

internal sealed record SeriesPositionDto(BowlerId BowlerId, string BowlerName, int Position, bool Bowled, bool? IsFinalist);

// === GetSeriesAdvancement/GetSeriesAdvancementQueryHandler.cs ===
using ErrorOr;

using Microsoft.Extensions.Options;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.GetSeriesAdvancement;

internal sealed class GetSeriesAdvancementQueryHandler(
    IAdvancementOutputReader reader, IOptions<SeriesAdvancementSettings> settings)
    : IQueryHandler<GetSeriesAdvancementQuery, ErrorOr<SeriesAdvancementDto>>
{
    public async Task<ErrorOr<SeriesAdvancementDto>> HandleAsync(GetSeriesAdvancementQuery query, CancellationToken cancellationToken)
    {
        var rows = await reader.ReadAsync(query.SeriesId, cancellationToken);
        if (rows is null)
        {
            return SeriesErrors.NotFound(query.SeriesId);
        }

        var grace = settings.Value.OfficialGrace;
        var lastQualifying = rows.Stages.Where(s => s.Kind == StageKind.Qualifying).MaxBy(s => s.Order);

        var stages = rows.Stages.OrderBy(s => s.Order).Select(stage =>
        {
            var isLastQualifying = stage.StageId == lastQualifying?.StageId;

            // The Backfill belongs to the last Qualifying Stage.
            var entries = rows.Entries
                .Where(e => e.SourceStageId == stage.StageId || (isLastQualifying && e.SourceStageId is null))
                .OrderBy(e => e.Place ?? int.MaxValue).ThenBy(e => e.BowlerName)
                .Select(e => new FieldEntryDto(e.BowlerId, e.BowlerName, e.SourceStageId, e.Place, e.BackfillPoints, e.ViaOverride))
                .ToList();

            int? vacant = stage.SlotCount is { } slots
                ? slots - rows.Entries.Count(e => e.SourceStageId == stage.StageId && !e.ViaOverride)
                : null;

            return new StageAdvancementDto(
                stage.StageId, stage.Order, stage.Kind.Name, stage.Status.Name, stage.IsStale,
                NeedsSquad: stage.Kind == StageKind.Survivor && (stage.ResolvedSquadId is null || !stage.ResolvedSquadExists),
                stage.ComputedAt,
                SeriesAdvancementRules.OfficialDueAt(
                    stage.Kind, stage.Status, stage.IsStale, stage.ComputedAt,
                    awaitingBackfill: isLastQualifying && rows.BackfillAsOf is null, grace),
                stage.OfficialAt, stage.SlotCount, vacant, stage.TournamentId, stage.TournamentStatus.Name, stage.ResolvedSquadId,
                entries,
                [.. rows.Overrides.Where(o => o.StageId == stage.StageId).OrderBy(o => o.CreatedAt)
                    .Select(o => new StageOverrideDto(o.OverrideId, o.BowlerId, o.BowlerName, o.Kind.Name, o.Reason, o.CreatedBy, o.CreatedAt, o.Applied))],
                [.. rows.Outcomes.Where(o => o.StageId == stage.StageId).OrderByDescending(o => o.BlockTotal).ThenBy(o => o.BowlerName)
                    .Select(o => new SurvivorOutcomeDto(o.BowlerId, o.BowlerName, o.BlockTotal, o.Advanced))]);
        }).ToList();

        // Target field: a computed Stage's stored slot total, else advancing count times team size.
        var targetField = rows.Stages.Where(s => s.Kind == StageKind.Qualifying)
            .Sum(s => s.SlotCount ?? (s.AdvancingCount ?? 0) * s.TeamSize);

        return new SeriesAdvancementDto(
            rows.Version, rows.BackfillAsOf, targetField, stages,
            [.. rows.Positions.OrderBy(p => p.Position)
                .Select(p => new SeriesPositionDto(p.BowlerId, p.BowlerName, p.Position, p.Bowled, p.IsFinalist))]);
    }
}
```
Add `using Neba.Api.Features.Bowlers.Domain;` and `using Neba.Api.Features.Tournaments.Domain;` to the DTO and handler files.

```csharp
// === GetSeriesAdvancement/GetSeriesAdvancementEndpoint.cs ===
using Asp.Versioning;

using ErrorOr;

using FastEndpoints;
using FastEndpoints.AspVersioning;

using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

using PermissionCatalog = Neba.Api.Contracts.Security.Permissions;

namespace Neba.Api.Features.Series.GetSeriesAdvancement;

internal sealed class GetSeriesAdvancementEndpoint(
    IQueryHandler<GetSeriesAdvancementQuery, ErrorOr<SeriesAdvancementDto>> queryHandler)
    : Endpoint<GetSeriesAdvancementRequest, SeriesAdvancementResponse>
{
    private readonly IQueryHandler<GetSeriesAdvancementQuery, ErrorOr<SeriesAdvancementDto>> _queryHandler = queryHandler;

    public override void Configure()
    {
        Get("{id}/advancement");
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditSeries.PolicyName);

        Description(description => description
            .WithName("GetSeriesAdvancement")
            .WithTags("Admin")
            .Produces<SeriesAdvancementResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(GetSeriesAdvancementRequest req, CancellationToken ct)
    {
        var result = await _queryHandler.HandleAsync(new GetSeriesAdvancementQuery { SeriesId = new SeriesId(req.SeriesId) }, ct);

        if (result.IsError)
        {
            await Send.NotFoundAsync(ct); // NotFound is the only error the query returns
            // Stryker disable once Statement
            return;
        }

        var dto = result.Value;

        await Send.OkAsync(new SeriesAdvancementResponse
        {
            Version = dto.Version,
            BackfillAsOf = dto.BackfillAsOf,
            TargetField = dto.TargetField,
            Stages = [.. dto.Stages.Select(s => new StageAdvancementResponse
            {
                StageId = s.StageId.ToString(),
                Order = s.Order,
                Kind = s.Kind,
                Status = s.Status,
                IsStale = s.IsStale,
                NeedsSquad = s.NeedsSquad,
                ComputedAt = s.ComputedAt,
                OfficialDueAt = s.OfficialDueAt,
                OfficialAt = s.OfficialAt,
                SlotCount = s.SlotCount,
                VacantSlotCount = s.VacantSlotCount,
                TournamentId = s.TournamentId.ToString(),
                TournamentStatus = s.TournamentStatus,
                ResolvedSquadId = s.ResolvedSquadId?.ToString(),
                Entries = [.. s.Entries.Select(e => new FieldEntryResponse
                {
                    BowlerId = e.BowlerId.ToString(),
                    BowlerName = e.BowlerName,
                    SourceStageId = e.SourceStageId?.ToString(),
                    Place = e.Place,
                    BackfillPoints = e.BackfillPoints,
                    ViaOverride = e.ViaOverride,
                })],
                Overrides = [.. s.Overrides.Select(o => new StageOverrideResponse
                {
                    OverrideId = o.OverrideId.ToString(),
                    BowlerId = o.BowlerId.ToString(),
                    BowlerName = o.BowlerName,
                    Kind = o.Kind,
                    Reason = o.Reason,
                    CreatedBy = o.CreatedBy,
                    CreatedAt = o.CreatedAt,
                    Applied = o.Applied,
                })],
                SurvivorOutcomes = [.. s.SurvivorOutcomes.Select(o => new SurvivorOutcomeResponse
                {
                    BowlerId = o.BowlerId.ToString(),
                    BowlerName = o.BowlerName,
                    BlockTotal = o.BlockTotal,
                    Advanced = o.Advanced,
                })],
            })],
            Positions = [.. dto.Positions.Select(p => new SeriesPositionResponse
            {
                BowlerId = p.BowlerId.ToString(),
                BowlerName = p.BowlerName,
                Position = p.Position,
                Bowled = p.Bowled,
                IsFinalist = p.IsFinalist,
            })],
        }, ct);
    }
}

// === GetSeriesAdvancement/GetSeriesAdvancementSummary.cs ===
using FastEndpoints;

namespace Neba.Api.Features.Series.GetSeriesAdvancement;

internal sealed class GetSeriesAdvancementSummary : Summary<GetSeriesAdvancementEndpoint>
{
    public GetSeriesAdvancementSummary()
    {
        Summary = "Gets a Series' advancement.";
        Description = "Returns each Stage's status, output and markers, the Backfill date, the target field size, the positions and the Version that writes must send. Kind, status and tournament status are names. Requires the Series.EditSeries permission.";

        Response<SeriesAdvancementResponse>(200, "The Series' advancement.");
        Response(400, "The request is structurally invalid.");
        Response(401, "The caller is not authenticated.");
        Response(403, "The caller lacks Series.EditSeries.");
        Response(404, "Series not found.");
    }
}
```
Add `using Neba.Api.Contracts.Series.GetSeriesAdvancement;` to the summary file.

##### 2. `RecomputeSeriesAdvancement` (`POST /series/{id}/stages/{stageId}/recomputations`, `Series.EditSeries`, 204 / 400 / 401 / 403 / 404 / 409)

```csharp
// === RecomputeSeriesAdvancement/RecomputeSeriesAdvancementCommand.cs ===
using ErrorOr;

using Neba.Api.Features.Series.Domain;
using Neba.Api.Messaging;

namespace Neba.Api.Features.Series.RecomputeSeriesAdvancement;

internal sealed record RecomputeSeriesAdvancementCommand
    : ICommand<Updated>
{
    public required SeriesId SeriesId { get; init; }
    public required SeriesStageId StageId { get; init; }
    public required int Version { get; init; }
    public required bool ConfirmResetOfficial { get; init; }
}

// === RecomputeSeriesAdvancement/RecomputeSeriesAdvancementCommandHandler.cs ===
internal sealed class RecomputeSeriesAdvancementCommandHandler(SeriesAdvancement advancement)
    : ICommandHandler<RecomputeSeriesAdvancementCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(RecomputeSeriesAdvancementCommand command, CancellationToken cancellationToken)
    {
        var result = await advancement.RunAsync(
            command.SeriesId,
            (series, facts, now) => series.Recompute(command.StageId, command.ConfirmResetOfficial, command.Version, facts, now),
            SeriesAdvancement.AdminLockWait,
            needsFacts: true,
            cancellationToken);

        return result.IsError ? result.Errors : Result.Updated;
    }
}

// === RecomputeSeriesAdvancement/RecomputeSeriesAdvancementRequestValidator.cs ===
using FastEndpoints;

using FluentValidation;

using Neba.Api.Contracts.Series.RecomputeSeriesAdvancement;

namespace Neba.Api.Features.Series.RecomputeSeriesAdvancement;

internal sealed class RecomputeSeriesAdvancementRequestValidator : Validator<RecomputeSeriesAdvancementRequest>
{
    public RecomputeSeriesAdvancementRequestValidator()
    {
        RuleFor(r => r.Id).IsUlid("Series ID", "RecomputeSeriesAdvancementRequest.Id");
        RuleFor(r => r.StageId).IsUlid("Stage ID", "RecomputeSeriesAdvancementRequest.StageId");
        RuleFor(r => r.Recomputation.Version).GreaterThanOrEqualTo(0)
            .WithErrorCode("RecomputeSeriesAdvancementRequest.VersionInvalid").WithMessage("Version must be zero or more.");
    }
}

// === RecomputeSeriesAdvancement/RecomputeSeriesAdvancementEndpoint.cs ===
internal sealed class RecomputeSeriesAdvancementEndpoint(Messaging.ICommandHandler<RecomputeSeriesAdvancementCommand, Updated> commandHandler)
    : Endpoint<RecomputeSeriesAdvancementRequest>
{
    public override void Configure()
    {
        Post("{id}/stages/{stageId}/recomputations");
        Group<SeriesEndpointGroup>();

        Options(options => options.WithVersionSet("Series").MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.EditSeries.PolicyName);

        Description(description => description
            .WithName("RecomputeSeriesAdvancement")
            .WithTags("Admin")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict));
    }

    public override async Task HandleAsync(RecomputeSeriesAdvancementRequest req, CancellationToken ct)
    {
        var result = await commandHandler.HandleAsync(new RecomputeSeriesAdvancementCommand
        {
            SeriesId = new SeriesId(req.Id),
            StageId = new SeriesStageId(req.StageId),
            Version = req.Recomputation.Version,
            ConfirmResetOfficial = req.Recomputation.ConfirmResetOfficial,
        }, ct);

        if (result.IsError)
        {
            await SeriesAdvancementResultSender.SendAsync(result.Errors, f => AddError(f), Send.ErrorsAsync, Send.NotFoundAsync, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.NoContentAsync(ct);
    }
}

// === RecomputeSeriesAdvancement/RecomputeSeriesAdvancementSummary.cs ===
internal sealed class RecomputeSeriesAdvancementSummary : Summary<RecomputeSeriesAdvancementEndpoint>
{
    public RecomputeSeriesAdvancementSummary()
    {
        Summary = "Recomputes a Series' advancement from a Stage.";
        Description = "Recomputes the Stage and every later Stage from stored results and overrides. A recompute that reaches official Stages needs ConfirmResetOfficial; it resets them to Computed and clears Stale. Requires the Series.EditSeries permission.";

        Response(204, "The Series was recomputed (or nothing changed).");
        Response(400, "The request is structurally invalid.");
        Response(401, "The caller is not authenticated.");
        Response(403, "The caller lacks Series.EditSeries.");
        Response(404, "Series or Stage not found.");
        Response(409, "The Version is stale, advancement is running, or official Stages need confirmation (the error lists them).");
    }
}
```
Add the same usings as `EditSeriesEndpoint`, plus `Neba.Api.Contracts.Series.RecomputeSeriesAdvancement`. Use `Messaging.ICommandHandler` as the existing endpoints do. `Send.NotFoundAsync` and `Send.ErrorsAsync` pass as method groups.

##### 3. `PickStageSquad` (`PUT /series/{id}/stages/{stageId}/squad`, `Series.EditSeries`, 204 / 400 / 401 / 403 / 404 / 409 / 422)

```csharp
// Command: PickStageSquadCommand : ICommand<Updated> { SeriesId, StageId, SquadId (SquadId), Version }
// Handler (usings as above, plus Neba.Api.Features.Tournaments.Domain)
internal sealed class PickStageSquadCommandHandler(SeriesAdvancement advancement)
    : ICommandHandler<PickStageSquadCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(PickStageSquadCommand command, CancellationToken cancellationToken)
    {
        var result = await advancement.RunAsync(
            command.SeriesId,
            (series, facts, now) => series.PickSquad(command.StageId, command.SquadId, command.Version, facts, now),
            SeriesAdvancement.AdminLockWait, needsFacts: true, cancellationToken);

        return result.IsError ? result.Errors : Result.Updated;
    }
}

// Validator
internal sealed class PickStageSquadRequestValidator : Validator<PickStageSquadRequest>
{
    public PickStageSquadRequestValidator()
    {
        RuleFor(r => r.Id).IsUlid("Series ID", "PickStageSquadRequest.Id");
        RuleFor(r => r.StageId).IsUlid("Stage ID", "PickStageSquadRequest.StageId");
        RuleFor(r => r.Squad.SquadId).IsUlid("Squad ID", "PickStageSquadRequest.SquadId");
        RuleFor(r => r.Squad.Version).GreaterThanOrEqualTo(0)
            .WithErrorCode("PickStageSquadRequest.VersionInvalid").WithMessage("Version must be zero or more.");
    }
}

// Endpoint Configure(): Put("{id}/stages/{stageId}/squad"); Policies(EditSeries); WithName("PickStageSquad");
//   Produces 204; ProducesProblemDetails 400, 401, 403, 404, 409, 422.
// HandleAsync: map req -> new PickStageSquadCommand { SeriesId = new SeriesId(req.Id), StageId = new SeriesStageId(req.StageId),
//   SquadId = new SquadId(req.Squad.SquadId), Version = req.Squad.Version }; the error and success branches match
//   RecomputeSeriesAdvancementEndpoint.
// Summary: "Picks a Survivor Stage's squad." 204 saved; 404 Series or Stage; 409 stale Version or running;
//   422 the Stage is not a Survivor Stage or the squad is not in its tournament.
```

##### 4. `CreateStageOverride` (`POST /series/{id}/stages/{stageId}/overrides`, `Series.ManageAdvancement`, 201 / 400 / 401 / 403 / 404 / 409 / 422)

```csharp
// === CreateStageOverride/CreateStageOverrideCommand.cs ===
internal sealed record CreateStageOverrideCommand
    : ICommand<CreatedStageOverride>
{
    public required SeriesId SeriesId { get; init; }
    public required SeriesStageId StageId { get; init; }
    public required OverrideKind Kind { get; init; }
    public required BowlerId BowlerId { get; init; }
    public required string Reason { get; init; }
    public required Ulid CallerUserId { get; init; }
    public required int Version { get; init; }
}

internal sealed record CreatedStageOverride(StageOverrideId OverrideId, int Version);

// === CreateStageOverride/CreateStageOverrideCommandHandler.cs ===
internal sealed class CreateStageOverrideCommandHandler(SeriesAdvancement advancement)
    : ICommandHandler<CreateStageOverrideCommand, CreatedStageOverride>
{
    public async Task<ErrorOr<CreatedStageOverride>> HandleAsync(CreateStageOverrideCommand command, CancellationToken cancellationToken)
    {
        var result = await advancement.RunAsync(
            command.SeriesId,
            (series, facts, now) => command.Kind == OverrideKind.Add
                ? series.AddOverride(command.StageId, command.BowlerId, command.Reason, command.CallerUserId.ToString(), command.Version, facts, now)
                : series.ClearSlot(command.StageId, command.BowlerId, command.Reason, command.CallerUserId.ToString(), command.Version, facts, now),
            SeriesAdvancement.AdminLockWait, needsFacts: true, cancellationToken);

        return result.IsError
            ? result.Errors
            : new CreatedStageOverride(result.Value.OverrideId!.Value, result.Value.Version);
    }
}

// === CreateStageOverride/CreateStageOverrideRequestValidator.cs ===
internal sealed class CreateStageOverrideRequestValidator : Validator<CreateStageOverrideRequest>
{
    public CreateStageOverrideRequestValidator()
    {
        RuleFor(r => r.Id).IsUlid("Series ID", "CreateStageOverrideRequest.Id");
        RuleFor(r => r.StageId).IsUlid("Stage ID", "CreateStageOverrideRequest.StageId");
        RuleFor(r => r.StageOverride.BowlerId).IsUlid("Bowler ID", "CreateStageOverrideRequest.BowlerId");

        RuleFor(r => r.StageOverride.Kind)
            .Must(kind => kind is OverrideKindValues.Add or OverrideKindValues.Clear)
            .WithErrorCode("CreateStageOverrideRequest.KindInvalid").WithMessage("Kind must be Add (1) or Clear (2).");

        RuleFor(r => r.StageOverride.Reason)
            .NotEmpty().WithErrorCode("CreateStageOverrideRequest.ReasonRequired").WithMessage("A reason is required.")
            .MaximumLength(StageOverride.ReasonMaxLength).WithErrorCode("CreateStageOverrideRequest.ReasonTooLong")
            .WithMessage($"The reason must be {StageOverride.ReasonMaxLength} characters or fewer.");

        RuleFor(r => r.StageOverride.Version).GreaterThanOrEqualTo(0)
            .WithErrorCode("CreateStageOverrideRequest.VersionInvalid").WithMessage("Version must be zero or more.");
    }
}
```
`StageOverride.ReasonMaxLength` is `internal const`, which is accessible in this assembly.

```csharp
// === CreateStageOverride/CreateStageOverrideEndpoint.cs ===
internal sealed class CreateStageOverrideEndpoint(Messaging.ICommandHandler<CreateStageOverrideCommand, CreatedStageOverride> commandHandler)
    : Endpoint<CreateStageOverrideRequest, CreatedStageOverrideResponse>
{
    public override void Configure()
    {
        Post("{id}/stages/{stageId}/overrides");
        Group<SeriesEndpointGroup>();

        Options(options => options.WithVersionSet("Series").MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.ManageAdvancement.PolicyName);

        Description(description => description
            .WithName("CreateStageOverride")
            .WithTags("Admin")
            .Produces<CreatedStageOverrideResponse>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status404NotFound)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(CreateStageOverrideRequest req, CancellationToken ct)
    {
        if (User.CallerUserId is not { } callerUserId)
        {
            await Send.UnauthorizedAsync(ct);
            // Stryker disable once Statement
            return;
        }

        var input = req.StageOverride;

        var result = await commandHandler.HandleAsync(new CreateStageOverrideCommand
        {
            SeriesId = new SeriesId(req.Id),
            StageId = new SeriesStageId(req.StageId),
            Kind = OverrideKind.FromValue(input.Kind), // commands parse by value
            BowlerId = new BowlerId(input.BowlerId),
            Reason = input.Reason,
            CallerUserId = callerUserId,
            Version = input.Version,
        }, ct);

        if (result.IsError)
        {
            await SeriesAdvancementResultSender.SendAsync(result.Errors, f => AddError(f), Send.ErrorsAsync, Send.NotFoundAsync, ct);
            // Stryker disable once Statement
            return;
        }

        // Stryker disable once Statement
        await Send.CreatedAtAsync(
            "GetSeriesAdvancement",
            routeValues: new { id = req.Id },
            responseBody: new CreatedStageOverrideResponse { OverrideId = result.Value.OverrideId.ToString(), Version = result.Value.Version },
            cancellation: ct);
    }
}
```
Summary: "Adds a bowler to a Qualifying Stage or clears a slot." Description: "Kind Add puts any bowler with a result row in the Stage's tournament in the field (the field may exceed the target). Kind Clear vacates the slot the bowler holds, so the Backfill refills it, and bars the bowler from the Backfill. The Stage must be Computed. The Series recomputes before the response. Requires the Series.ManageAdvancement permission." Responses: 201, 400, 401, 403 (lacks `Series.ManageAdvancement`), 404, 409 (stale Version, Stage not Computed, bowler already in field, override exists), 422 (not a Qualifying Stage, no result row, bowler holds no filled slot).

##### 5. `RemoveStageOverride` (`DELETE /series/{id}/stages/{stageId}/overrides/{overrideId}?version=n`, `Series.ManageAdvancement`, 204 / 400 / 401 / 403 / 404 / 409)

```csharp
// === RemoveStageOverride/RemoveStageOverrideRequest.cs ===
using FastEndpoints;

namespace Neba.Api.Features.Series.RemoveStageOverride;

internal sealed class RemoveStageOverrideRequest
{
    [BindFrom("id")] public required string Id { get; set; }
    [BindFrom("stageId")] public required string StageId { get; set; }
    [BindFrom("overrideId")] public required string OverrideId { get; set; }
    [BindFrom("version")] public int? Version { get; set; }
}

// Validator: Id, StageId, OverrideId use IsUlid; Version NotNull then GreaterThanOrEqualTo(0)
//   (codes RemoveStageOverrideRequest.VersionRequired / VersionInvalid).

// Command: RemoveStageOverrideCommand : ICommand<Deleted> { SeriesId, StageId, OverrideId (StageOverrideId), Version }
internal sealed class RemoveStageOverrideCommandHandler(SeriesAdvancement advancement)
    : ICommandHandler<RemoveStageOverrideCommand, Deleted>
{
    public async Task<ErrorOr<Deleted>> HandleAsync(RemoveStageOverrideCommand command, CancellationToken cancellationToken)
    {
        var result = await advancement.RunAsync(
            command.SeriesId,
            (series, facts, now) => series.RemoveOverride(command.StageId, command.OverrideId, command.Version, facts, now),
            SeriesAdvancement.AdminLockWait, needsFacts: true, cancellationToken);

        return result.IsError ? result.Errors : Result.Deleted;
    }
}

// Endpoint Configure(): Delete("{id}/stages/{stageId}/overrides/{overrideId}"); Policies(ManageAdvancement);
//   WithName("RemoveStageOverride"); Produces 204; ProducesProblemDetails 400, 401, 403, 404, 409.
// HandleAsync: same shape as Recompute; Version = req.Version!.Value.
// Summary: 204 "Removed, or it was already gone." 409 "The Version is stale (checked only when the override exists),
//   or the Stage is official."
```
A missing Series or Stage is a 404. A missing override is a 204 before the Version check.

##### 6. `ListStageResults` (`GET /series/{id}/stages/{stageId}/results`, `Series.ManageAdvancement`, 200 / 401 / 403 / 404) and 7. `ListStageSquads` (`GET /series/{id}/stages/{stageId}/squads`, `Series.EditSeries`, same codes)

Both are thin. Each has a request with `[BindFrom("id")]` and `[BindFrom("stageId")]`, a validator with `IsUlid` on both, a query `ErrorOr<IReadOnlyList<...Dto>>`, and its own DTO.
```csharp
internal sealed record StageResultSummaryDto(BowlerId BowlerId, string BowlerName, int Place);
internal sealed record StageSquadSummaryDto(SquadId SquadId, DateOnly LocalDate, TimeOnly LocalStartTime, bool HasScores, bool IsSelected);

internal sealed class ListStageResultsQueryHandler(IStageResultReader reader)
    : IQueryHandler<ListStageResultsQuery, ErrorOr<IReadOnlyList<StageResultSummaryDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<StageResultSummaryDto>>> HandleAsync(ListStageResultsQuery query, CancellationToken cancellationToken)
    {
        var rows = await reader.ReadAsync(query.SeriesId, query.StageId, cancellationToken);

        return rows.IsError
            ? rows.Errors
            : rows.Value.Select(r => new StageResultSummaryDto(r.BowlerId, r.BowlerName, r.Place)).ToList();
    }
}
```
`ListStageSquadsQueryHandler` is the same shape over `IStageSquadReader`. The endpoint returns `Send.OkAsync(new CollectionResponse<...Response> { Items = ... })`. Copy the item property name from `ListSeriesEndpoint`, which I did not open.

Reuse check: the plan states existing tournament reads cannot serve these (`GetTournament` has no `BowlerId` and no squad times), and the DTOs are named `*Summary` per `docs/learnings/backend.md`, with the routes named after the resource.

---

#### I. Contracts, permission, docs

##### `src/Neba.Api.Contracts/Security/Permission.cs` (in `#region Series`, after `EditSeriesLogo`)
```csharp
    /// <summary>
    /// Permission to add a bowler to a Qualifying Stage, clear a slot, or remove such an override. Admin only.
    /// </summary>
    public static readonly Permissions ManageAdvancement = new("Series.ManageAdvancement", "Manage Series Advancement");
```
`SecurityRoleSeeder.cs` needs no edit (see section 0, item 8). Add a seeder test: Admin has it and Manager does not.

##### `src/Neba.Api.Contracts/Series/ISeriesApi.cs` (add usings for the new request folders)
```csharp
    /// <summary>Gets each Stage's status, output and markers, and the Version writes must send. Requires Series.EditSeries.</summary>
    [Get("/series/{seriesId}/advancement")]
    Task<IApiResponse<SeriesAdvancementResponse>> GetSeriesAdvancementAsync(string seriesId, CancellationToken cancellationToken = default);

    /// <summary>Recomputes from a Stage to the end. Requires Series.EditSeries.</summary>
    [Post("/series/{seriesId}/stages/{stageId}/recomputations")]
    Task<IApiResponse> RecomputeSeriesAdvancementAsync(string seriesId, string stageId, RecomputeSeriesAdvancementRequest request, CancellationToken cancellationToken = default);

    /// <summary>Picks a Survivor Stage's squad. Requires Series.EditSeries.</summary>
    [Put("/series/{seriesId}/stages/{stageId}/squad")]
    Task<IApiResponse> PickStageSquadAsync(string seriesId, string stageId, PickStageSquadRequest request, CancellationToken cancellationToken = default);

    /// <summary>Adds a bowler to a Qualifying Stage or clears a slot. Requires Series.ManageAdvancement.</summary>
    [Post("/series/{seriesId}/stages/{stageId}/overrides")]
    Task<IApiResponse<CreatedStageOverrideResponse>> CreateStageOverrideAsync(string seriesId, string stageId, CreateStageOverrideRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes an override. Requires Series.ManageAdvancement.</summary>
    [Delete("/series/{seriesId}/stages/{stageId}/overrides/{overrideId}")]
    Task<IApiResponse> RemoveStageOverrideAsync(string seriesId, string stageId, string overrideId, [Query] int version, CancellationToken cancellationToken = default);

    /// <summary>Lists the result rows of a Stage's tournament, for the add-bowler picker. Requires Series.ManageAdvancement.</summary>
    [Get("/series/{seriesId}/stages/{stageId}/results")]
    Task<IApiResponse<CollectionResponse<StageResultSummaryResponse>>> ListStageResultsAsync(string seriesId, string stageId, CancellationToken cancellationToken = default);

    /// <summary>Lists the squads of a Stage's tournament, for the squad picker. Requires Series.EditSeries.</summary>
    [Get("/series/{seriesId}/stages/{stageId}/squads")]
    Task<IApiResponse<CollectionResponse<StageSquadSummaryResponse>>> ListStageSquadsAsync(string seriesId, string stageId, CancellationToken cancellationToken = default);
```

##### Contracts types (`src/Neba.Api.Contracts/Series/{UseCase}/`, XML summary on each public member as the existing records do)
```csharp
// GetSeriesAdvancement/SeriesAdvancementResponse.cs
public sealed record SeriesAdvancementResponse
{
    /// <summary>Concurrency token. Writes send the value from the last GET.</summary>
    public required int Version { get; init; }
    /// <summary>The date the Backfill ran as of. Null until it ran.</summary>
    public DateOnly? BackfillAsOf { get; init; }
    /// <summary>Target field size: computed Qualifying slots, else advancing count times team size.</summary>
    public required int TargetField { get; init; }
    /// <summary>The Stages, in order.</summary>
    public required IReadOnlyList<StageAdvancementResponse> Stages { get; init; }
    /// <summary>The Placement Round's positions. Empty until it computes.</summary>
    public required IReadOnlyList<SeriesPositionResponse> Positions { get; init; }
}

// GetSeriesAdvancement/StageAdvancementResponse.cs
public sealed record StageAdvancementResponse
{
    public required string StageId { get; init; }
    public required int Order { get; init; }
    /// <summary>The kind name: Qualifying, Survivor or Placement.</summary>
    public required string Kind { get; init; }
    /// <summary>The status name: Pending, Computed or Official.</summary>
    public required string Status { get; init; }
    public required bool IsStale { get; init; }
    /// <summary>True for a Survivor Stage with no squad, or whose squad no longer exists.</summary>
    public required bool NeedsSquad { get; init; }
    public DateTimeOffset? ComputedAt { get; init; }
    /// <summary>When a Computed Stage will become Official (up to 15 minutes later). Null when it will not.</summary>
    public DateTimeOffset? OfficialDueAt { get; init; }
    public DateTimeOffset? OfficialAt { get; init; }
    public int? SlotCount { get; init; }
    public int? VacantSlotCount { get; init; }
    public required string TournamentId { get; init; }
    /// <summary>The tournament status name. Cancelled means the Stage can be removed.</summary>
    public required string TournamentStatus { get; init; }
    public string? ResolvedSquadId { get; init; }
    /// <summary>Field entries. The last Qualifying Stage's list includes the Backfill (null SourceStageId).</summary>
    public required IReadOnlyList<FieldEntryResponse> Entries { get; init; }
    public required IReadOnlyList<StageOverrideResponse> Overrides { get; init; }
    public required IReadOnlyList<SurvivorOutcomeResponse> SurvivorOutcomes { get; init; }
}

// FieldEntryResponse { BowlerId, BowlerName, string? SourceStageId (null = Backfill), int? Place, int? BackfillPoints, bool ViaOverride }
// StageOverrideResponse { OverrideId, BowlerId, BowlerName, Kind (name), Reason, CreatedBy, DateTimeOffset CreatedAt, bool Applied }
// SurvivorOutcomeResponse { BowlerId, BowlerName, int? BlockTotal, bool Advanced }
// SeriesPositionResponse { BowlerId, BowlerName, int Position, bool Bowled, bool? IsFinalist }

// RecomputeSeriesAdvancement/RecomputeSeriesAdvancementRequest.cs
public sealed record RecomputeSeriesAdvancementRequest
{
    public required string Id { get; init; }
    public required string StageId { get; init; }
    public required RecomputeSeriesAdvancementInput Recomputation { get; init; }
}
public sealed record RecomputeSeriesAdvancementInput
{
    /// <summary>The Version from the last GET.</summary>
    public required int Version { get; init; }
    /// <summary>True to reset official Stages in the range to Computed.</summary>
    public bool ConfirmResetOfficial { get; init; }
}

// PickStageSquad/PickStageSquadRequest.cs
public sealed record PickStageSquadRequest { string Id; string StageId; PickStageSquadInput Squad }
public sealed record PickStageSquadInput { string SquadId; int Version }

// CreateStageOverride/CreateStageOverrideRequest.cs
public sealed record CreateStageOverrideRequest { string Id; string StageId; CreateStageOverrideInput StageOverride }
public sealed record CreateStageOverrideInput
{
    /// <summary>The kind value: 1 Add, 2 Clear (see OverrideKindValues).</summary>
    public required int Kind { get; init; }
    public required string BowlerId { get; init; }
    /// <summary>Why, up to 500 characters.</summary>
    public required string Reason { get; init; }
    public required int Version { get; init; }
}
public sealed record CreatedStageOverrideResponse { string OverrideId; int Version }  // the Version after the write

// ListStageResults/StageResultSummaryResponse { string BowlerId, string BowlerName, int Place }
// ListStageSquads/StageSquadSummaryResponse { string SquadId, DateOnly Date, TimeOnly StartTime (Eastern), bool HasScores, bool IsSelected }
```
(The records written as `{ string Id; ... }` are shorthand; each property is `required ... { get; init; }` with an XML summary.)

##### Docs
- **`docs/architecture/permission-based-security.md`** (before the policies row): add `Series.ManageAdvancement | Add a bowler to a Qualifying Stage, clear a slot, remove an override | Admin only`.
- **`docs/policies/README.md`** row:
  `| Permission:Series.ManageAdvancement | Dynamic (single permission) | Caller holds Series.ManageAdvancement; Admin only (through Permissions.List; Manager is not seeded) | CreateStageOverrideEndpoint, RemoveStageOverrideEndpoint, ListStageResultsEndpoint; the Series page shows the override actions only to holders | (help doc with the UI phase) |`
- **`docs/learnings/backend.md`**, new section "Domain events":
  - One `IDomainEventJob<T>` per event type, because the DI activator returns a single implementation. An architecture test enforces it.
  - Each event needs a serialization round-trip test (Hangfire serializes typed IDs through their `TypeConverter`).
  - `IDomainEventPublisher` is the only place that builds the job. The save interceptor and the jobs that change rows no aggregate owns both use it.
  - There is no outbox: a crash between commit and enqueue loses the event, and admin Recompute heals it.
  - `IKeyedLockProvider` (Hangfire `AcquireDistributedLock`) serializes work per key. `[DisableConcurrentExecution]` cannot key on an argument, and a Postgres advisory lock fights the retrying execution strategy.
- **`docs/ubiquitous-language.md` `## Series`**, new entries (keep the XML comments short):
  - *Slot*: a Qualifying result row at Place N or better. Ties at N all count. In a team event the rows are the team members.
  - *Vacant Slot*: a Slot that went to a non-member, to a bowler already in the field, or was cleared. The Backfill fills it.
  - *Series Field Entry*: a bowler's place in the field, from a Qualifying slot, an override, or the Backfill.
  - *Stage Override*: an admin Add or Clear on a Qualifying Stage. It lasts until removed. An override whose target changed stops applying and is never an error.
  - *Stale*: the stored output may differ from a recompute. Cleared by a forced recompute.
  - *Grace Period*: hours between output last changing and the automatic change to Official (`SeriesAdvancement:OfficialGraceHours`, default 24).
  - *Survivor Outcome*: a field bowler's block total and advanced flag at a Survivor Stage.
  - *Block Total*: the sum of a bowler's scores over the games bowled on the designated squad.
  - *Needs Squad*: a Survivor Stage with no resolved squad, or whose squad vanished.
  - Update *Stage Status*, *Squad Designation*, *Backfill* (candidates are members with points above zero on the as-of date; a Clear override bars a bowler for the whole Series), *Series Computation*, and the rules gaps (Qualifying membership is judged on the tournament's end date; no-shows rank alphabetically; the override Add does not fill a vacancy).

---

### Tests (from `test-writer`; grouped by commit, one line each)

1. Results-recorded event and legacy chain (unit and integration). 2. Stats-generated event. 3. Open BOY points as of a date (integration, includes parity with `BowlerSeasonStats`). 4. Member on a date. 5. Squad matcher in Eastern time (DST edges, vanished squad). 6. Domain stored shape and state machine. 7. The four computation functions (ties, team slots, partial squad, overrides, determinism). 8. Orchestration handler (pending until predecessor, redelivery, cascade, stale, same-bowler rebuild under unique indexes, rollback). 9. Per-Series lock and stale-write guard. 10. Backfill handler. 11. Make-official job. 12. Squad resolution and Pick squad. 13. Admin commands (confirmation, `Version` conflicts, racing writes). 14. Endpoint tests (Verify snapshots, 401/403 matrix, seeder test). 15. EF configuration and migration. 16. Synthetic 2025-layout replay through Recompute from the first Stage.
- Factories to add or extend: `TournamentFactory` (results), `SquadFactory.CreateEastern`, `SquadScoreFactory.CreateGames`, `BowlerFactory` (memberships), `SeriesFactory.CreateWithAdvancement`, an override factory, input and DTO factories, `Series2025ReferenceFactory.CreateWithResults`. All `Create()` calls persist with no arguments.
- `FeatureBoundaryTests` stays clean: events live in the publishing feature's Domain namespace, readers and DTOs live in Series.
- **Group 16 uses a synthetic scenario (decided 2026-11-20; supersedes #259's "2025 reference configuration replay").** The test builds invented bowlers on the 2025 Stage layout and runs the full chain through Recompute from the first Stage, then asserts hand-computed field, advancers and positions. It can't catch a mismatch with real 2025 history. The 2027 season is the real tuning run: the gaps between events leave room to correct rules and fix code between Stages. The issue's replay criterion changes to match.
- No e2e mock change in this phase beyond what the UI phase calls.

### Deferred

Survivor override; an outbox or sweep that heals lost events (it also would catch tournaments completed before deploy, which conflicts with "no automatic catch-up"); payouts and Finale results (phase 3); public pages; notifications when a Stage becomes Official; per-center time zones; extracting the BOY open-points formula into one shared helper (the Series reader duplicates it for now); the bowler merge flow.

## Phase 2: UI

Functional draft (`blazor-ui` for pages and components, `test-writer` for tests and the mock). Everything lives in `src/Neba.Website.Server/` (nothing in the Client project). No API redesign: the Phase 1 `ISeriesApi` methods are used as written, through `ApiExecutor` with API name `"SeriesApi"`.

### Decisions taken in this phase

- **One page, no new routes.** `/series/{Id}` stays the page. `@page`, `@rendermode InteractiveServerRenderMode(prerender: false)` and `<PageTitle>` are already there and stay. Prerender stays off because the permission check needs the real circuit user. `/series`, the tournament list and tournament detail need nothing.
- **Permission check in code.** The page injects `AuthenticationStateProvider` and `IAuthorizationService` and computes `_canView` (`Series.EditSeries`) and `_canManage` (`Series.ManageAdvancement`) after `GetSeries` succeeds. An `AuthorizeView` can't gate a fetch, so all new markup and the advancement call use these two booleans and can't disagree. Anonymous users and users without `EditSeries` get the phase 1 page, no advancement call, no 401 noise. A Manager never calls `ListStageResultsAsync` (it would 403).
- **`Version` handling.** One `_version` field, set by every successful `GetSeriesAdvancementAsync`. Every write sends it (the Remove override call takes it as the `version` query parameter). After any 2xx the page reloads advancement and ignores the `Version` in `CreatedStageOverrideResponse`, so the rule is "the last GET is the truth". `_isWriting` disables every action during a write so a second click can't send a stale `Version`.
- **`DirtyFormGuard` (confirmed):** on the override modal only, copying `EditLogoModal` (`IsDirty` is `IsOpen && IsDirty`, discard confirm on close). The squad picker and recompute confirm have nothing typed, so no guard.
- **Recompute (confirmed):** shows on every Stage row for `EditSeries` holders, Pending included, because catch-up after the 1.3.0 deploy is "Recompute from the first Stage" and that Stage may be Pending. It runs directly when no official Stage is in range. When official Stages would reset, it opens `ConfirmActionModal` with the list and sends `ConfirmResetOfficial = true` only after the user confirms.
- **Pick squad** shows on every Survivor Stage for `EditSeries` holders. On an Official Stage it can mark the Stage Stale without changing output.
- **Entry scope.** A Stage's `Entries` are the rows with that Stage as `SourceStageId`; the Backfill rows (null source) belong to the last Qualifying Stage. Clear slot shows only on rows sourced by that Stage and not `ViaOverride`; Backfill rows get no Clear.

### Code draft (`blazor-ui`)

Code draft, not compiled. `blazor-ui` wrote it from the confirmed functional breakdown and the option 2 mockup. Phase 1's Contracts are not in the repo yet, so the project cannot build until they land. Notes first; they override the code where they disagree.

#### Notes

1. **Contract names are assumed.** One namespace per use case under `Neba.Api.Contracts.Series.*` (`GetSeriesAdvancement`, `RecomputeSeriesAdvancement`, `PickStageSquad`, `CreateStageOverride`, `ListStageResults`, `ListStageSquads`), and `OverrideKindValues` (`Add = 1`, `Clear = 2`) beside `StageKindValues`. Fix the usings at first build. `CollectionResponse<T>` exposes `Items` (checked in source).
2. **403 on the advancement GET.** `ApiExecutor` maps a 403 with a body to `Validation` and without one to `Failure`, so the error type can't say "forbidden". The page captures `response.StatusCode` inside the lambda for that one call and hides the section on 403. A 403 on a write shows a generic error.
3. **409 and 422 text** arrive as the API's reasons joined by spaces ("This Stage is official. Recompute it from the Series page before changing it."), with no code. Defaults taken: the 409 toast shows the API text with the plan's fixed sentence as fallback; the status pill uses the mockup's tinted style on mapped site tokens (`StatusPillClass` is the one function to swap for solid `neba-badge-*`).
4. **Public view models.** Component parameter types must be public, so the advancement view models are public records with `<param>` docs (`TreatWarningsAsErrors`, `GenerateDocumentationFile`, `AnalysisMode All`). `SeriesStageViewModel` stays internal; the card takes the tournament name, link and meta line as strings.
5. **Scoped CSS.** Elements from child components don't carry the page's scope attribute, so the strip, card, output and modals each get their own `.razor.css`. `SeriesDetail.razor.css` holds the status tokens (on `.series-detail`) and the rail. The modals sit outside `.series-detail`, so they use theme tokens directly. Dark mode uses `.dark .series-detail` (the existing `:global(.dark)` rule in `SeriesFieldChart.razor.css` may be dead; not checked).
6. **Load.** `LoadAdvancementAsync` runs from `OnParametersSetAsync`, not inside `LoadAsync`, so a logo-save reload doesn't refetch it, and the page shows as soon as `GetSeries` returns with skeleton rows meanwhile. On a failed load a holder gets the alert with Retry above the phase 1 Stage lists, so they never see an empty page.
7. **Code-behind.** The repo keeps `@code` inside `.razor`. The new logic is about 300 lines, so it lives in `SeriesDetail.razor.cs` as a partial class. Move it into `@code` if you prefer.
8. **Mockup wins over the earlier plan text:** a remove-override confirm exists; Source reads "Qualifier" (the Place column already shows the number); "Version N" is dropped from the progress line (a review aid); `ComputedAt` is dropped from the view model; Pick squad says "No squad matches it exactly" only when `NeedsSquad`, otherwise "Choose the squad this round uses."; the toast says "Squad saved." because the Stage may stay Pending. `TargetField`, `ResolvedSquadId` and `TournamentId` stay in the view model but nothing shows them; drop them if you want.
9. **Entry scope.** The page uses `stage.Entries` as given (the Phase 1 handler already puts the Backfill rows on the last Qualifying Stage). Clear shows only when `SourceStageId == stage.Id && !ViaOverride`.
10. **Companion edit.** `EditSeries` loads advancement only when the Series is locked, to mark removable Stages. If that load fails it shows a warning alert instead of silently hiding the remove button.
11. **Test traps.** `SeriesDetail` now injects three more services (`AuthenticationStateProvider`, `IAuthorizationService`, `IClientTimeZoneService`); register the time zone service in every `SeriesDetail` test. Every `SeriesDetail` and locked-`EditSeries` case needs a `SetupAdvancement`. Check how `ApiExecutor` reads ProblemDetails from a `StubApiResponse` for 409 and 422. Extra pure units worth tests: `AddCandidates`, `OfficialOrdersFrom`, `Instants`, `MapStage` flags (including `CanClear` and Backfill rows), the `Headline` per kind, `StageWordList` with 1, 2 and 3 items, `OfficialResetMessage`, and the removable-row grid.
12. **Mock.** The page also calls `GET .../stages/{id}/squads` and `.../results`. One fixture needs `tournamentStatus: 'Cancelled'` for the locked-editor removal flow.
13. **Not verified:** compilation of any of this; the Humanizer `"Stage".ToQuantity(n, ShowQuantityAs.None, formatProvider: ...)` signature; whether Razor accepts `@if` directly after `else if (...) {` in `SeriesStageOutput.razor` (drop the `@` if not); the look of `.dark .series-detail`, the `color-mix` foregrounds (contrast), and `NebaAutocomplete`'s inline panel inside the modal; the `neba-data-table` phone layout on the Actions columns; any browser behavior (the stack was not running).
14. **No changes** to `Program.cs` or `_Imports.razor`. No `.razor.js`: the reason box uses `@oninput` only to keep the counter and Save button live.

#### 1. New file `src/Neba.Website.Server/Series/StageStatuses.cs`

```csharp
using Neba.Api.Contracts.Series;

namespace Neba.Website.Server.Series;

/// <summary>Stage status names as the API returns them.</summary>
internal static class StageStatuses
{
    public const string Pending = "Pending";
    public const string Computed = "Computed";
    public const string Official = "Official";
}

/// <summary>Tournament status names the Series page reads.</summary>
internal static class TournamentStatuses
{
    public const string Cancelled = "Cancelled";
}

/// <summary>Override kind names as the API returns them. Commands take the matching value.</summary>
internal static class OverrideKinds
{
    public const string Add = "Add";
    public const string Clear = "Clear";

    /// <summary>The API's value for a kind name. An unknown name maps to 0, which the API rejects by name.</summary>
    public static int ToValue(string kind)
        => kind switch
        {
            Add => OverrideKindValues.Add,
            Clear => OverrideKindValues.Clear,
            _ => 0,
        };
}
```

#### 2. New file `src/Neba.Website.Server/Series/SeriesAdvancementViewModel.cs`

```csharp
namespace Neba.Website.Server.Series;

/// <summary>What the signed-in user may do in the advancement section.</summary>
/// <param name="CanView">Holds Series.EditSeries.</param>
/// <param name="CanManage">Holds Series.EditSeries and Series.ManageAdvancement.</param>
internal readonly record struct AdvancementAccess(bool CanView, bool CanManage);

/// <summary>The advancement read, shaped for the page.</summary>
internal sealed record SeriesAdvancementViewModel
{
    public required int Version { get; init; }
    public DateOnly? BackfillAsOf { get; init; }
    public required int TargetField { get; init; }
    public required IReadOnlyList<StageAdvancementViewModel> Stages { get; init; }
    public required IReadOnlyList<PositionViewModel> Positions { get; init; }

    /// <summary>True when any position has a Finalist flag, so the Series has a Finale.</summary>
    public bool HasFinalists => Positions.Any(p => p.IsFinalist is not null);

    public StageAdvancementViewModel? StageById(string stageId)
        => Stages.FirstOrDefault(s => s.Id == stageId);

    /// <summary>The Official Stages a recompute from <paramref name="order"/> would reset.</summary>
    public IReadOnlyList<int> OfficialOrdersFrom(int order)
        => [.. Stages.Where(s => s.Order >= order && s.Status == StageStatuses.Official).Select(s => s.Order)];

    /// <summary>Result rows an Add override can take: not in the Series field, and no override on that Stage.</summary>
    public IReadOnlyList<StageResultViewModel> AddCandidates(string stageId, IReadOnlyList<StageResultViewModel> results)
    {
        var taken = Stages.SelectMany(s => s.Entries).Select(e => e.BowlerId).ToHashSet(StringComparer.Ordinal);
        if (StageById(stageId) is { } stage)
        {
            taken.UnionWith(stage.Overrides.Select(o => o.BowlerId));
        }

        return [.. results.Where(r => !taken.Contains(r.BowlerId))];
    }
}

/// <summary>One Stage on the season rail.</summary>
/// <param name="Id">The Stage ID.</param>
/// <param name="Order">Position in the season, from 1.</param>
/// <param name="Kind">Qualifying, Survivor or Placement.</param>
/// <param name="Status">Pending, Computed or Official.</param>
/// <param name="IsStale">The stored output may differ from a recompute.</param>
/// <param name="NeedsSquad">A Survivor Stage with no squad, or whose squad vanished.</param>
/// <param name="OfficialDueAt">When a Computed Stage becomes Official, in the viewer's time zone.</param>
/// <param name="SlotCount">Qualifying slots, once computed.</param>
/// <param name="VacantSlotCount">Slots not filled by a qualifier.</param>
/// <param name="TournamentId">The Stage's tournament.</param>
/// <param name="TournamentStatus">The tournament status name.</param>
/// <param name="ResolvedSquadId">The squad a Survivor Stage uses, when known.</param>
/// <param name="IsLastQualifying">True for the last Qualifying Stage, which owns the Backfill.</param>
/// <param name="Entries">Field entries for this Stage (the last Qualifying Stage also holds the Backfill).</param>
/// <param name="Overrides">Admin overrides on this Stage.</param>
/// <param name="Outcomes">Survivor results.</param>
/// <param name="CanAddOverride">Admin may add a bowler.</param>
/// <param name="CanRemoveOverride">Admin may remove an override.</param>
/// <param name="CanRecompute">Holder may recompute from this Stage.</param>
/// <param name="CanPickSquad">Holder may pick this Survivor Stage's squad.</param>
public sealed record StageAdvancementViewModel(
    string Id,
    int Order,
    string Kind,
    string Status,
    bool IsStale,
    bool NeedsSquad,
    DateTimeOffset? OfficialDueAt,
    int? SlotCount,
    int? VacantSlotCount,
    string TournamentId,
    string TournamentStatus,
    string? ResolvedSquadId,
    bool IsLastQualifying,
    IReadOnlyList<FieldEntryViewModel> Entries,
    IReadOnlyList<StageOverrideViewModel> Overrides,
    IReadOnlyList<SurvivorOutcomeViewModel> Outcomes,
    bool CanAddOverride,
    bool CanRemoveOverride,
    bool CanRecompute,
    bool CanPickSquad)
{
    /// <summary>The tournament was cancelled, so the Stage can be removed.</summary>
    public bool IsTournamentCancelled => TournamentStatus == TournamentStatuses.Cancelled;
}

/// <summary>A bowler's place in the Series field.</summary>
/// <param name="BowlerId">The bowler.</param>
/// <param name="BowlerName">The bowler's name.</param>
/// <param name="SourceStageId">The Qualifying Stage that produced it; null for the Backfill.</param>
/// <param name="Place">Place in that Stage's tournament; null for the Backfill.</param>
/// <param name="BackfillPoints">Points that earned a Backfill entry.</param>
/// <param name="ViaOverride">An admin added the bowler.</param>
/// <param name="CanClear">Admin may clear this slot.</param>
public sealed record FieldEntryViewModel(
    string BowlerId, string BowlerName, string? SourceStageId, int? Place, int? BackfillPoints, bool ViaOverride, bool CanClear)
{
    /// <summary>The entry came from the Backfill.</summary>
    public bool IsBackfill => SourceStageId is null;
}

/// <summary>An admin Add or Clear on a Qualifying Stage.</summary>
/// <param name="OverrideId">The override.</param>
/// <param name="BowlerId">The bowler added or cleared.</param>
/// <param name="BowlerName">The bowler's name.</param>
/// <param name="Kind">Add or Clear.</param>
/// <param name="Reason">Why the admin made the change.</param>
/// <param name="CreatedAt">When, in the viewer's time zone.</param>
/// <param name="Applied">False once the override's target changed.</param>
public sealed record StageOverrideViewModel(
    string OverrideId, string BowlerId, string BowlerName, string Kind, string Reason, DateTimeOffset CreatedAt, bool Applied);

/// <summary>A bowler's Survivor Round result.</summary>
/// <param name="BowlerId">The bowler.</param>
/// <param name="BowlerName">The bowler's name.</param>
/// <param name="BlockTotal">Null when the bowler has no full block.</param>
/// <param name="Advanced">The bowler advanced.</param>
public sealed record SurvivorOutcomeViewModel(string BowlerId, string BowlerName, int? BlockTotal, bool Advanced);

/// <summary>A bowler's Placement Round position.</summary>
/// <param name="BowlerId">The bowler.</param>
/// <param name="BowlerName">The bowler's name.</param>
/// <param name="Position">1-based position.</param>
/// <param name="Bowled">The bowler bowled the Placement Stage.</param>
/// <param name="IsFinalist">Null when the Series has no Finale.</param>
public sealed record PositionViewModel(string BowlerId, string BowlerName, int Position, bool Bowled, bool? IsFinalist);

/// <summary>A result row the Add bowler picker offers.</summary>
/// <param name="BowlerId">The bowler.</param>
/// <param name="BowlerName">The bowler's name.</param>
/// <param name="Place">Place in the Stage's tournament.</param>
/// <param name="Label">Picker text, such as "Marcus Alvarez (12th)".</param>
public sealed record StageResultViewModel(string BowlerId, string BowlerName, int Place, string Label);

/// <summary>A squad the Pick squad dialog offers.</summary>
/// <param name="SquadId">The squad.</param>
/// <param name="Label">"9:30 AM on 7/12".</param>
/// <param name="Detail">"Has scores · Current".</param>
/// <param name="HasScores">The squad has score rows.</param>
/// <param name="IsSelected">The Stage uses this squad now.</param>
public sealed record StageSquadViewModel(string SquadId, string Label, string Detail, bool HasScores, bool IsSelected);

/// <summary>What the override dialog hands the page.</summary>
/// <param name="BowlerId">The bowler to add or clear.</param>
/// <param name="BowlerName">For the toast.</param>
/// <param name="Reason">Trimmed.</param>
public sealed record StageOverrideSubmission(string BowlerId, string BowlerName, string Reason);
```

#### 3. New file `src/Neba.Website.Server/Series/StageOverrideFormModel.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace Neba.Website.Server.Series;

/// <summary>The override dialog's form. The bowler picker is not bound through the EditContext.</summary>
public sealed class StageOverrideFormModel
{
    /// <summary>Most characters a reason may have.</summary>
    public const int ReasonMaxLength = 500;

    /// <summary>Why the admin made the change.</summary>
    [Required(ErrorMessage = "A reason is required.")]
    [StringLength(ReasonMaxLength, ErrorMessage = "The reason must be 500 characters or fewer.")]
    public string Reason { get; set; } = string.Empty;
}
```

#### 4. Edit `src/Neba.Website.Server/Series/SeriesMappingExtensions.cs`

Add the usings, add these blocks inside `SeriesMappingExtensions` after the existing two, and add the private helpers.

```csharp
using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.Api.Contracts.Series.ListStageResults;
using Neba.Api.Contracts.Series.ListStageSquads;
```

```csharp
    extension(SeriesAdvancementResponse response)
    {
        /// <summary>Every UTC instant the page shows in local time, once each. The page converts them, then calls ToViewModel.</summary>
        public IReadOnlyList<DateTimeOffset> Instants()
            => [.. response.Stages.Select(s => s.OfficialDueAt).OfType<DateTimeOffset>()
                .Concat(response.Stages.SelectMany(s => s.Overrides).Select(o => o.CreatedAt))
                .Distinct()];

        public SeriesAdvancementViewModel ToViewModel(AdvancementAccess access, IReadOnlyDictionary<DateTimeOffset, DateTimeOffset> localTimes)
        {
            var ordered = response.Stages.OrderBy(s => s.Order).ToList();
            var lastQualifyingId = ordered.LastOrDefault(s => s.Kind == StageKinds.Qualifying)?.StageId;

            return new SeriesAdvancementViewModel
            {
                Version = response.Version,
                BackfillAsOf = response.BackfillAsOf,
                TargetField = response.TargetField,
                Stages = [.. ordered.Select(s => MapStage(s, s.StageId == lastQualifyingId, access, localTimes))],
                Positions = [.. response.Positions.OrderBy(p => p.Position)
                    .Select(p => new PositionViewModel(p.BowlerId, p.BowlerName, p.Position, p.Bowled, p.IsFinalist))],
            };
        }
    }

    extension(StageResultSummaryResponse response)
    {
        public StageResultViewModel ToViewModel()
            => new(response.BowlerId, response.BowlerName, response.Place, SeriesDisplay.ResultOption(response.BowlerName, response.Place));
    }

    extension(StageSquadSummaryResponse response)
    {
        public StageSquadViewModel ToViewModel()
            => new(response.SquadId, SeriesDisplay.SquadOption(response.Date, response.StartTime),
                SeriesDisplay.SquadDetail(response.HasScores, response.IsSelected), response.HasScores, response.IsSelected);
    }

    private static StageAdvancementViewModel MapStage(
        StageAdvancementResponse stage, bool isLastQualifying, AdvancementAccess access,
        IReadOnlyDictionary<DateTimeOffset, DateTimeOffset> localTimes)
    {
        // Overrides apply to Qualifying Stages while they are Computed, and only Series.ManageAdvancement holders may change them.
        var canChangeOutput = access.CanManage && stage.Kind == StageKinds.Qualifying && stage.Status == StageStatuses.Computed;

        return new StageAdvancementViewModel(
            Id: stage.StageId,
            Order: stage.Order,
            Kind: stage.Kind,
            Status: stage.Status,
            IsStale: stage.IsStale,
            NeedsSquad: stage.NeedsSquad,
            OfficialDueAt: stage.OfficialDueAt is { } due ? ToLocal(due, localTimes) : null,
            SlotCount: stage.SlotCount,
            VacantSlotCount: stage.VacantSlotCount,
            TournamentId: stage.TournamentId,
            TournamentStatus: stage.TournamentStatus,
            ResolvedSquadId: stage.ResolvedSquadId,
            IsLastQualifying: isLastQualifying,
            Entries: [.. stage.Entries.Select(e => new FieldEntryViewModel(
                e.BowlerId, e.BowlerName, e.SourceStageId, e.Place, e.BackfillPoints, e.ViaOverride,
                CanClear: canChangeOutput && e.SourceStageId == stage.StageId && !e.ViaOverride))],
            Overrides: [.. stage.Overrides.Select(o => new StageOverrideViewModel(
                o.OverrideId, o.BowlerId, o.BowlerName, o.Kind, o.Reason, ToLocal(o.CreatedAt, localTimes), o.Applied))],
            Outcomes: [.. stage.SurvivorOutcomes.Select(o => new SurvivorOutcomeViewModel(o.BowlerId, o.BowlerName, o.BlockTotal, o.Advanced))],
            CanAddOverride: canChangeOutput,
            CanRemoveOverride: canChangeOutput,
            CanRecompute: access.CanView,
            CanPickSquad: access.CanView && stage.Kind == StageKinds.Survivor);
    }

    private static DateTimeOffset ToLocal(DateTimeOffset utc, IReadOnlyDictionary<DateTimeOffset, DateTimeOffset> localTimes)
        => localTimes.TryGetValue(utc, out var local) ? local : utc;
```

#### 5. Edit `src/Neba.Website.Server/Series/SeriesDisplay.cs`

Add these records at file top, beside `AdvancingLabel`:

```csharp
/// <summary>A bold lead and the plain text after it, for summary lines.</summary>
internal sealed record SummaryLine(string Lead, string Detail);
```

Add these members inside `SeriesDisplay`. There is no `$""` and no `< N =>`.

```csharp
    // ---- Advancement section (Series.EditSeries holders) ----

    public const string DefaultConflictMessage = "This Series changed after you loaded it. Your change was not saved. Review the Stage and try again.";
    public const string AdvancementReloadFailed = "The Stage status could not refresh. Reload the page to see the latest.";
    public const string StaleNote = "An earlier Stage changed after this Stage was official. Its output has not changed. Recompute from here to refresh it.";
    public const string StaleHint = "An earlier Stage changed after this Stage was official.";
    public const string NoFieldEntries = "No bowlers in the field from this Stage.";
    public const string NoSquads = "No squads yet. They arrive when the desktop software sends this tournament's squads.";
    public const string SlotClearedMessage = "Slot cleared. The Backfill will refill it.";

    private static string Num(int n) => n.ToString(Culture);

    /// <summary>"series-pill series-pill--computed". A Placement Stage never reaches Official.</summary>
    public static string StatusPillClass(string status)
        => status switch
        {
            StageStatuses.Official => "series-pill series-pill--official",
            StageStatuses.Computed => "series-pill series-pill--computed",
            _ => "series-pill series-pill--pending",
        };

    public static string SegmentClass(string status)
        => status switch
        {
            StageStatuses.Official => "series-strip__seg series-strip__seg--official",
            StageStatuses.Computed => "series-strip__seg series-strip__seg--computed",
            _ => "series-strip__seg",
        };

    /// <summary>"Stage 3: Official".</summary>
    public static string SegmentTitle(int order, string status) => "Stage " + Num(order) + ": " + status;

    /// <summary>The rail node's class: its status, plus cancelled when the tournament was.</summary>
    public static string NodeClass(string status, bool isCancelled)
        => "series-node series-node--" + status.ToLowerInvariant() + (isCancelled ? " series-node--cancelled" : string.Empty);

    /// <summary>"Qualifying · Stage 3".</summary>
    public static string StageChip(string kind, int order) => kind + " · Stage " + Num(order);

    /// <summary>"7/12 · 3-bowler teams · Top 2 teams (6 bowlers)".</summary>
    public static string RailMeta(SeriesStageViewModel stage, int? finaleCount)
    {
        var advancing = Advancing(stage.Kind, stage.AdvancingCount, stage.TeamSize, finaleCount);
        var parts = new List<string> { TournamentDates(stage.TournamentStartDate, stage.TournamentEndDate) };
        if (stage.TeamSize > 1)
        {
            parts.Add(Num(stage.TeamSize) + "-bowler teams");
        }

        parts.Add(advancing.Secondary is null ? advancing.Primary : advancing.Primary + " " + advancing.Secondary);
        return string.Join(" · ", parts);
    }

    /// <summary>"7 of 8 slots filled", "40 advance", "20 ranked"; null while Pending.</summary>
    public static string? Headline(StageAdvancementViewModel stage, int positionCount)
    {
        if (stage.Status == StageStatuses.Pending)
        {
            return null;
        }

        if (stage.Kind == StageKinds.Qualifying)
        {
            var slots = stage.SlotCount ?? 0;
            var filled = slots - (stage.VacantSlotCount ?? 0);
            return Num(filled) + " of " + "slot".ToQuantity(slots, formatProvider: DisplayCulture.English) + " filled";
        }

        return stage.Kind == StageKinds.Survivor
            ? Num(stage.Outcomes.Count(o => o.Advanced)) + " advance"
            : Num(positionCount) + " ranked";
    }

    /// <summary>"Becomes official about 3:15 PM on 7/12". The sweep runs every 15 minutes, so it says "about".</summary>
    public static string? OfficialDueLabel(DateTimeOffset? local)
        => local is { } at ? "Becomes official about " + at.ToString("h:mm tt", Culture) + " on " + at.ToString("M/d", Culture) : null;

    /// <summary>"Backfill as of 6/14/2025"; null until the Backfill ran.</summary>
    public static string? BackfillLabel(DateOnly? asOf)
        => asOf is { } date ? "Backfill as of " + date.ToString("M/d/yyyy", Culture) : null;

    /// <summary>"4 of 11 Stages" + " computed or official (3 official). Backfill has not run."</summary>
    public static SummaryLine Progress(IReadOnlyList<StageAdvancementViewModel> stages, DateOnly? backfillAsOf)
    {
        var done = stages.Count(s => s.Status != StageStatuses.Pending);
        var official = stages.Count(s => s.Status == StageStatuses.Official);
        var backfill = backfillAsOf is { } date
            ? "Backfill ran as of " + date.ToString("M/d/yyyy", Culture) + "."
            : "Backfill has not run.";

        return new SummaryLine(
            Num(done) + " of " + "Stage".ToQuantity(stages.Count, formatProvider: DisplayCulture.English),
            " computed or official (" + Num(official) + " official). " + backfill);
    }

    /// <summary>"Hide output for Stage 3".</summary>
    public static string ExpandLabel(bool isExpanded, int order)
        => (isExpanded ? "Hide" : "Show") + " output for Stage " + Num(order);

    /// <summary>Why a Pending Stage shows no output.</summary>
    public static string PendingReason(StageAdvancementViewModel stage)
    {
        if (stage.IsTournamentCancelled)
        {
            return "This tournament was cancelled. Remove this Stage on the Edit page, then recompute from the Stage before it.";
        }

        if (stage.NeedsSquad)
        {
            return "Waiting for a squad. Pick the squad this round uses and the Stage computes.";
        }

        return stage.Kind == StageKinds.Placement
            ? "Positions appear when the Placement Round computes."
            : "Not computed yet. This Stage computes when its tournament completes.";
    }

    /// <summary>"8 slots" (the bold lead of the Qualifying summary).</summary>
    public static string SlotsLead(int slots) => "slot".ToQuantity(slots, formatProvider: DisplayCulture.English);

    /// <summary>", 2 vacant slots. Vacant slots go to the Backfill." or ", none vacant."</summary>
    public static string VacantLabel(int vacant)
        => vacant > 0
            ? ", " + "vacant slot".ToQuantity(vacant, formatProvider: DisplayCulture.English) + ". Vacant slots go to the Backfill."
            : ", none vacant.";

    /// <summary>"Field entries from this Stage, then the Backfill".</summary>
    public static string EntriesHeading(bool isLastQualifying)
        => "Field entries from this Stage" + (isLastQualifying ? ", then the Backfill" : string.Empty);

    /// <summary>"3rd", or a dash for a Backfill entry.</summary>
    public static string OrdinalPlace(int? place) => place is { } p ? p.ToOrdinal() : "—";

    public static string EntrySource(FieldEntryViewModel entry)
        => entry.ViaOverride ? "Added by admin" : entry.IsBackfill ? "Backfill" : "Qualifier";

    public static string SourceClass(FieldEntryViewModel entry)
        => entry.ViaOverride ? "series-src--admin" : entry.IsBackfill ? "series-src--backfill" : string.Empty;

    public static string Points(int? points) => points is { } p ? Num(p) : "—";

    public static string BlockTotal(int? total) => total is { } t ? Num(t) : "No full block";

    public static string OverrideChange(string kind) => kind == OverrideKinds.Add ? "Add bowler" : "Clear slot";

    public static string OverrideStatus(bool applied) => applied ? "Applied" : "No longer applies";

    /// <summary>"3/23/2025".</summary>
    public static string AddedDate(DateTimeOffset local) => local.ToString("M/d/yyyy", Culture);

    /// <summary>"12 advance" + " of 40 in the field. 38 bowled; 2 did not."</summary>
    public static SummaryLine SurvivorSummary(IReadOnlyList<SurvivorOutcomeViewModel> outcomes)
    {
        var bowled = outcomes.Count(o => o.BlockTotal is not null);
        return new SummaryLine(
            Num(outcomes.Count(o => o.Advanced)) + " advance",
            " of " + Num(outcomes.Count) + " in the field. " + Num(bowled) + " bowled; " + Num(outcomes.Count - bowled) + " did not.");
    }

    /// <summary>"20 bowlers ranked." + the Finale note.</summary>
    public static SummaryLine PlacementSummary(int ranked, bool hasFinalists)
        => new("bowler".ToQuantity(ranked, formatProvider: DisplayCulture.English) + " ranked.",
            hasFinalists ? " Finalist positions are provisional until the Finale is recorded." : " These positions are final.");

    /// <summary>"Recompute from Stage 3?"</summary>
    public static string RecomputeTitle(int order) => "Recompute from Stage " + Num(order) + "?";

    /// <summary>"1, 2 and 3".</summary>
    public static string StageWordList(IReadOnlyList<int> orders)
    {
        if (orders.Count == 0)
        {
            return string.Empty;
        }

        var words = orders.Select(Num).ToList();
        return words.Count == 1
            ? words[0]
            : string.Join(", ", words.Take(words.Count - 1)) + " and " + words[^1];
    }

    /// <summary>"Stages 3 and 4 are official. Recomputing resets them to Computed."</summary>
    public static string OfficialResetMessage(IReadOnlyList<int> orders)
    {
        var one = orders.Count == 1;
        return "Stage".ToQuantity(orders.Count, ShowQuantityAs.None, formatProvider: DisplayCulture.English)
            + " " + StageWordList(orders) + (one ? " is" : " are") + " official. Recomputing resets " + (one ? "it" : "them") + " to Computed.";
    }

    public static string RecomputedMessage(int order) => "Recomputed from Stage " + Num(order) + ".";

    public static string RemoveOverrideMessage(StageOverrideViewModel item, int order)
        => item.Kind == OverrideKinds.Add
            ? item.BowlerName + " leaves the field for Stage " + Num(order) + "."
            : item.BowlerName + " takes the slot back, and the Backfill no longer refills it.";

    public static string OverrideAddedMessage(string bowlerName, int order) => bowlerName + " added to Stage " + Num(order) + ".";

    /// <summary>The API's own text when it has some (it names the cause), else the default.</summary>
    public static string ConflictMessage(string? description)
        => string.IsNullOrWhiteSpace(description) ? DefaultConflictMessage : description;

    public static string OverrideTitle(string kind) => kind == OverrideKinds.Add ? "Add bowler" : "Clear slot";

    public static string OverrideLead(string kind, int order, string tournamentName)
        => "Stage " + Num(order) + ", " + tournamentName + ". "
            + (kind == OverrideKinds.Add
                ? "Pick any bowler with a result in this tournament, whatever the place or membership. The field can then be larger than the target."
                : "The slot goes vacant and the Backfill fills it. This bowler can't come back through the Backfill.");

    /// <summary>"Marcus Alvarez (12th)".</summary>
    public static string ResultOption(string name, int place) => name + " (" + place.ToOrdinal() + ")";

    /// <summary>"7th place".</summary>
    public static string ClearPlace(int? place) => place is { } p ? p.ToOrdinal() + " place" : "Backfill";

    /// <summary>"120 / 500".</summary>
    public static string ReasonCount(int length) => Num(length) + " / " + Num(StageOverrideFormModel.ReasonMaxLength);

    /// <summary>"9:30 AM on 7/12" (the API already sends Eastern time).</summary>
    public static string SquadOption(DateOnly date, TimeOnly startTime)
        => startTime.ToString("h:mm tt", Culture) + " on " + date.ToString("M/d", Culture);

    /// <summary>"Has scores · Current".</summary>
    public static string SquadDetail(bool hasScores, bool isSelected)
        => (hasScores ? "Has scores" : "No scores yet") + (isSelected ? " · Current" : string.Empty);

    /// <summary>The Pick squad lead. <paramref name="expected"/> is the Series squad, such as "9:00 AM squad on 7/12".</summary>
    public static string SquadLead(int order, string tournamentName, string? expected, bool needsSquad)
        => "Stage " + Num(order) + ", " + tournamentName + ". The Series expects "
            + (expected is null ? "a squad" : "a " + expected) + "."
            + (needsSquad ? " No squad matches it exactly, so pick the one this round uses." : " Choose the squad this round uses.");

    /// <summary>The locked editor's banner. A cancelled Stage can still go.</summary>
    public static string LockedMessage(bool hasRemovableStage)
        => "Processing has begun for this Series. Its Stages and whether it has a Finale are fixed. The name, the logo, and the Finale's date, time and center can still change."
            + (hasRemovableStage
                ? " A Stage whose tournament was cancelled can still be removed. After you save, run Recompute from the Stage before it on the Series page."
                : string.Empty);
```

#### 6. Edit `src/Neba.Website.Server/Series/SeriesDetail.razor` (full replacement)

```razor
@page "/series/{Id}"
@using Microsoft.AspNetCore.Authorization
@using Neba.Api.Contracts.Security
@using Neba.Api.Contracts.Series
@using Neba.Website.Server.Services
@using Neba.Website.Server.Time
@rendermode @(new InteractiveServerRenderMode(prerender: false))

@inject ApiExecutor ApiExecutor
@inject ISeriesApi SeriesApi
@inject NavigationManager Navigation
@inject ToastService ToastService
@inject AuthenticationStateProvider AuthenticationStateProvider
@inject IAuthorizationService AuthorizationService
@inject IClientTimeZoneService ClientTimeZoneService

<PageTitle>@PageTitleText - BowlNEBA</PageTitle>

@if (_isLoading)
{
    <div class="series-detail-loading"><div class="neba-spinner"></div></div>
}
else if (_model is null)
{
    <main class="neba-content-wrapper series-detail">
        <p class="series-detail__message">@(_notFound ? "This Series doesn't exist. It may have been deleted." : "Something went wrong while loading this Series. Try again later.")</p>
        <a href="/series" class="neba-link">Back to Series</a>
    </main>
}
else
{
    <div class="neba-space-y-6 series-detail">
        <div class="page-title-bar">
            <div class="series-detail__logo">
                <SeriesLogoTile LogoUrl="@_model.LogoUrl" Name="@_model.Name" Size="@SeriesLogoTile.Large" />
                <LogoEditButton Policy="@Permissions.EditSeriesLogo.PolicyName" OwnerName="@_model.Name" OnClick="@(() => _isLogoEditorOpen = true)" />
            </div>
            <div class="page-title-inner">
                <h1>@_model.Name</h1>
                <p>@_model.SeasonDateRange</p>
            </div>
            <div class="series-detail__actions">
                <AuthorizeView Policy="@Permissions.EditSeries.PolicyName">
                    <a href="@EditHref" class="neba-btn neba-btn-secondary">Edit</a>
                </AuthorizeView>
                <AuthorizeView Policy="@Permissions.DeleteSeries.PolicyName">
                    <button type="button" class="neba-btn neba-btn-danger" disabled="@_model.IsLocked" @onclick="@(() => _isDeleteConfirmOpen = true)">Delete</button>
                    <HelpButton DocName="delete-series" Light="true" />
                    @if (_model.IsLocked)
                    {
                        <p class="series-detail__lock-note">Processing has begun, so this Series can't be deleted.</p>
                    }
                </AuthorizeView>
            </div>
        </div>

        @if (_steps.Count > 0)
        {
            <SeriesFieldChart Steps="@_steps" HasFinale="@_model.HasFinale" HasPlacementStage="@HasPlacementStage" />
        }

        @if (_canView && _advancementState == AdvancementState.Failed)
        {
            <section aria-labelledby="series-progress-failed-title">
                <h2 class="series-group-title" id="series-progress-failed-title">Season progress</h2>
                <NebaAlert Severity="NotifySeverity.Error" Title="Stage status could not load" Message="Something went wrong while loading Stage status. The Stages below have no status yet." />
                <button type="button" class="neba-btn neba-btn-secondary series-detail__retry" @onclick="RetryAdvancementAsync">Retry</button>
            </section>
        }

        @if (ShowRail)
        {
            <section aria-labelledby="series-progress-title">
                <h2 class="series-group-title" id="series-progress-title">Season progress</h2>

                @if (_advancement is null)
                {
                    <div class="series-rail-loading" aria-busy="true">
                        @for (var row = 0; row < SkeletonRowCount; row++)
                        {
                            <NebaSkeletonLoader Type="SkeletonType.Card" />
                        }
                    </div>
                }
                else if (_advancement.Stages.Count == 0)
                {
                    <p class="series-detail__message">This Series has no Stages yet.</p>
                }
                else
                {
                    <SeriesProgressStrip Stages="@_advancement.Stages" BackfillAsOf="@_advancement.BackfillAsOf" />

                    <ol class="series-rail">
                        @foreach (var stage in _advancement.Stages)
                        {
                            var detail = _model.Stages.FirstOrDefault(s => s.Id == stage.Id);
                            if (detail is null)
                            {
                                continue; // no matching Stage in GetSeries (a stale read): no card
                            }

                            <li @key="stage.Id" class="@SeriesDisplay.NodeClass(stage.Status, stage.IsTournamentCancelled)">
                                <SeriesStageCard Stage="@stage"
                                                 TournamentName="@detail.TournamentName"
                                                 TournamentHref="@TournamentHref(detail)"
                                                 Meta="@SeriesDisplay.RailMeta(detail, _model.Finale?.FinalistCount)"
                                                 PositionCount="@_advancement.Positions.Count"
                                                 BackfillLabel="@(stage.IsLastQualifying ? SeriesDisplay.BackfillLabel(_advancement.BackfillAsOf) : null)"
                                                 EditHref="@EditHref"
                                                 IsExpanded="@_expandedStageIds.Contains(stage.Id)"
                                                 IsBusy="@_isWriting"
                                                 OnToggle="@(() => ToggleStage(stage.Id))"
                                                 OnRecompute="@(() => StartRecomputeAsync(stage))"
                                                 OnPickSquad="@(() => OpenSquadPickerAsync(stage))">
                                    <SeriesStageOutput Stage="@stage"
                                                       Positions="@_advancement.Positions"
                                                       CanManage="@_canManage"
                                                       IsBusy="@_isWriting"
                                                       OnAddBowler="@(() => OpenAddOverrideAsync(stage))"
                                                       OnClearSlot="@(entry => OpenClearOverride(stage, entry))"
                                                       OnRemoveOverride="@(item => OpenRemoveOverride(stage, item))" />
                                </SeriesStageCard>
                            </li>
                        }
                    </ol>
                }
            </section>
        }
        else
        {
            foreach (var kind in StageKinds.All)
            {
                var stages = StagesOf(kind);
                if (stages.Count == 0)
                {
                    continue;
                }

                <section aria-labelledby="@GroupId(kind)">
                    <h2 class="series-group-title" id="@GroupId(kind)">@SeriesDisplay.GroupHeading(kind) <span>@SeriesDisplay.GroupNote(kind)</span></h2>
                    <ol class="neba-card series-stage-list">
                        @foreach (var stage in stages)
                        {
                            var advancing = SeriesDisplay.Advancing(stage.Kind, stage.AdvancingCount, stage.TeamSize, _model.Finale?.FinalistCount);
                            <li @key="stage.Id">
                                <span class="series-stage__order">@stage.Order</span>
                                <span class="series-stage__tournament">
                                    <a href="@TournamentHref(stage)">@stage.TournamentName</a>
                                    <small>@TournamentMeta(stage)</small>
                                </span>
                                <span class="series-stage__advancing">
                                    @advancing.Primary
                                    @if (advancing.Secondary is not null)
                                    {
                                        <small class="series-stage__advancing-sub">@advancing.Secondary</small>
                                    }
                                </span>
                                <span class="series-stage__squad">@SeriesDisplay.Squad(stage.SquadDate, stage.SquadStartTime)</span>
                            </li>
                        }
                    </ol>
                </section>
            }
        }

        @if (_model.Finale is { } finale)
        {
            <section aria-labelledby="series-finale-title">
                <h2 class="series-group-title" id="series-finale-title">Finale <span>@SeriesDisplay.FinaleCaption(finale.FinalistCount)</span></h2>
                <div class="neba-card series-finale">
                    <span class="series-finale__when">@SeriesDisplay.FinaleWhen(finale.Date, finale.StartTime)</span>
                    <span class="series-finale__host">@(finale.Host ?? "Host center to be announced")</span>
                </div>
            </section>
        }
    </div>

    <SeriesLogoEditor IsOpen="@_isLogoEditorOpen"
                      SeriesId="@Id"
                      SeriesName="@_model.Name"
                      CurrentLogoUrl="@_model.LogoUrl"
                      OnClose="@(() => _isLogoEditorOpen = false)"
                      OnSaved="HandleLogoSavedAsync" />

    <ConfirmActionModal IsOpen="@_isDeleteConfirmOpen"
                        Title="Delete Series?"
                        Message="@DeleteMessage"
                        IsBusy="@_isDeleteBusy"
                        OnConfirm="ConfirmDeleteAsync"
                        OnCancel="@(() => _isDeleteConfirmOpen = false)" />

    @if (_canView)
    {
        <StageSquadModal IsOpen="@(_squadStageId is not null)"
                         StageOrder="@(SquadStage?.Order ?? 0)"
                         TournamentName="@(SquadDetail?.TournamentName ?? string.Empty)"
                         ExpectedSquad="@SeriesDisplay.Squad(SquadDetail?.SquadDate, SquadDetail?.SquadStartTime)"
                         NeedsSquad="@(SquadStage?.NeedsSquad ?? false)"
                         Squads="@_squads"
                         IsLoading="@_isSquadsLoading"
                         LoadError="@_squadsError"
                         OnClose="CloseSquadPicker"
                         OnSubmitRequestedAsync="SubmitSquadAsync" />

        @if (_canManage)
        {
            <StageOverrideModal IsOpen="@(_overrideTarget is not null)"
                                Kind="@(_overrideTarget?.Kind ?? OverrideKinds.Add)"
                                StageOrder="@(OverrideStage?.Order ?? 0)"
                                TournamentName="@(OverrideDetail?.TournamentName ?? string.Empty)"
                                BowlerId="@_overrideTarget?.Entry?.BowlerId"
                                BowlerName="@(_overrideTarget?.Entry?.BowlerName ?? string.Empty)"
                                Place="@_overrideTarget?.Entry?.Place"
                                Choices="@_addChoices"
                                IsLoadingChoices="@_isResultsLoading"
                                ChoicesError="@_resultsError"
                                OnClose="CloseOverrideModal"
                                OnSubmitRequestedAsync="SubmitOverrideAsync" />
        }

        <ConfirmActionModal IsOpen="@(_recomputeTarget is not null)"
                            Title="@RecomputeTitle"
                            Message="@RecomputeMessage"
                            ConfirmLabel="Recompute"
                            BusyLabel="Recomputing..."
                            IsBusy="@_isWriting"
                            OnConfirm="ConfirmRecomputeAsync"
                            OnCancel="CancelRecompute" />

        <ConfirmActionModal IsOpen="@(_removeTarget is not null)"
                            Title="Remove this override?"
                            Message="@RemoveMessage"
                            ConfirmLabel="Remove override"
                            BusyLabel="Removing..."
                            IsBusy="@_isWriting"
                            OnConfirm="ConfirmRemoveAsync"
                            OnCancel="CancelRemove" />
    }
}

@code {
    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    private const int SkeletonRowCount = 4;

    private bool _isLoading = true;
    private bool _notFound;
    private SeriesDetailViewModel? _model;
    private IReadOnlyList<FieldStep> _steps = [];
    private bool _isDeleteConfirmOpen;
    private bool _isDeleteBusy;
    private bool _isLogoEditorOpen;

    private bool HasPlacementStage => _model!.Stages.Any(s => s.Kind == StageKinds.Placement);

    private async Task HandleLogoSavedAsync()
    {
        _isLogoEditorOpen = false;
        await LoadAsync(); // reload so the hero shows the new logo, without the page-wide spinner
    }

    private string PageTitleText => _model?.Name ?? "Series";
    private string EditHref => "/series/" + Id + "/edit";
    private string DeleteMessage => "\"" + (_model?.Name ?? string.Empty) + "\" and its Stages will be deleted. The tournaments stay on the schedule.";

    protected override async Task OnParametersSetAsync()
    {
        ResetAdvancementFor(Id);
        _isLoading = true;
        await LoadAsync();
        _isLoading = false;
        await LoadAdvancementForViewerAsync(); // after the page shows: holders see skeleton rows meanwhile
    }

    private async Task LoadAsync()
    {
        var result = await ApiExecutor.ExecuteAsync("SeriesApi", "GetSeries", ct => SeriesApi.GetSeriesAsync(Id, ct));

        if (result.IsError)
        {
            // A reload after a logo save keeps the page it has and tells the admin; a first load has nothing to keep.
            if (_model is not null && Id == _model.Id && result.FirstError.Type != ErrorOr.ErrorType.NotFound)
            {
                ToastService.Show("Reload Failed", "The logo was saved, but the page could not refresh. Reload the page to see it.", NotifySeverity.Warning);
                return;
            }

            _notFound = result.FirstError.Type == ErrorOr.ErrorType.NotFound;
            _model = null;
        }
        else
        {
            _model = result.Value.ToViewModel();
            _steps = SeriesDisplay.FieldSteps(_model.Stages, _model.Finale);
        }
    }

    private List<SeriesStageViewModel> StagesOf(string kind) => [.. _model!.Stages.Where(s => s.Kind == kind)];

    private static string GroupId(string kind) => "series-group-" + SeriesDisplay.KindClass(kind);

    private static string TournamentHref(SeriesStageViewModel stage) => "/tournaments/" + stage.TournamentId;

    private static string TournamentMeta(SeriesStageViewModel stage)
    {
        var dates = SeriesDisplay.TournamentDates(stage.TournamentStartDate, stage.TournamentEndDate);
        return stage.TeamSize > 1 ? dates + " · " + stage.TeamSize + "-bowler teams" : dates;
    }

    private async Task ConfirmDeleteAsync()
    {
        _isDeleteBusy = true;
        var result = await ApiExecutor.ExecuteAsync("SeriesApi", "DeleteSeries", ct => SeriesApi.DeleteSeriesAsync(Id, ct));
        _isDeleteBusy = false;
        _isDeleteConfirmOpen = false;

        if (result.IsError)
        {
            ToastService.Show("Delete Failed", result.FirstError.Description, NotifySeverity.Error);
            return;
        }

        ToastService.Show("Series Deleted", "\"" + _model!.Name + "\" was deleted.", NotifySeverity.Success);
        Navigation.NavigateTo("/series");
    }
}
```

#### 7. New file `src/Neba.Website.Server/Series/SeriesDetail.razor.cs`

```csharp
using System.Net;

using ErrorOr;

using Microsoft.AspNetCore.Authorization;

using Neba.Api.Contracts.Security;
using Neba.Api.Contracts.Series;
using Neba.Api.Contracts.Series.CreateStageOverride;
using Neba.Api.Contracts.Series.GetSeriesAdvancement;
using Neba.Api.Contracts.Series.PickStageSquad;
using Neba.Api.Contracts.Series.RecomputeSeriesAdvancement;
using Neba.Website.Server.Notifications;

using Refit;

namespace Neba.Website.Server.Series;

/// <summary>The Series page: the advancement section for Series.EditSeries holders.</summary>
public partial class SeriesDetail
{
    private enum AdvancementState { Hidden, Loading, Loaded, Failed }

    private enum WriteOutcome { Saved, Conflict, Failed }

    private sealed record OverrideTarget(string StageId, string Kind, FieldEntryViewModel? Entry);

    private sealed record RecomputeTarget(string StageId, int Order, IReadOnlyList<int> OfficialOrders);

    private sealed record RemoveTarget(string StageId, int Order, StageOverrideViewModel Override);

    private string? _advancementSeriesId;
    private bool _canView;
    private bool _canManage;
    private AdvancementState _advancementState = AdvancementState.Hidden;
    private SeriesAdvancementViewModel? _advancement;
    private int _version; // set by every successful GET; every write sends it
    private bool _isWriting;
    private readonly HashSet<string> _expandedStageIds = [];
    private readonly Dictionary<string, IReadOnlyList<StageResultViewModel>> _stageResults = [];

    private OverrideTarget? _overrideTarget;
    private IReadOnlyList<StageResultViewModel> _addChoices = [];
    private bool _isResultsLoading;
    private string? _resultsError;

    private string? _squadStageId;
    private IReadOnlyList<StageSquadViewModel> _squads = [];
    private bool _isSquadsLoading;
    private string? _squadsError;

    private RecomputeTarget? _recomputeTarget;
    private RemoveTarget? _removeTarget;

    private bool ShowRail => _canView && _advancementState is AdvancementState.Loading or AdvancementState.Loaded;

    private StageAdvancementViewModel? SquadStage => _squadStageId is { } id ? _advancement?.StageById(id) : null;

    private SeriesStageViewModel? SquadDetail => _squadStageId is { } id ? _model?.Stages.FirstOrDefault(s => s.Id == id) : null;

    private StageAdvancementViewModel? OverrideStage => _overrideTarget is { } target ? _advancement?.StageById(target.StageId) : null;

    private SeriesStageViewModel? OverrideDetail => _overrideTarget is { } target ? _model?.Stages.FirstOrDefault(s => s.Id == target.StageId) : null;

    private string RecomputeTitle => _recomputeTarget is { } target ? SeriesDisplay.RecomputeTitle(target.Order) : string.Empty;

    private string RecomputeMessage => _recomputeTarget is { } target ? SeriesDisplay.OfficialResetMessage(target.OfficialOrders) : string.Empty;

    private string RemoveMessage => _removeTarget is { } target ? SeriesDisplay.RemoveOverrideMessage(target.Override, target.Order) : string.Empty;

    private void ResetAdvancementFor(string seriesId)
    {
        if (_advancementSeriesId == seriesId)
        {
            return;
        }

        _advancementSeriesId = seriesId;
        _advancement = null;
        _advancementState = AdvancementState.Hidden;
        _canView = false;
        _canManage = false;
        _version = 0;
        _expandedStageIds.Clear();
        _stageResults.Clear();
        _overrideTarget = null;
        _squadStageId = null;
        _recomputeTarget = null;
        _removeTarget = null;
    }

    /// <summary>Checks the two permissions in code (an AuthorizeView cannot gate a fetch), then loads once.</summary>
    private async Task LoadAdvancementForViewerAsync()
    {
        if (_model is null)
        {
            return;
        }

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var view = await AuthorizationService.AuthorizeAsync(authState.User, Permissions.EditSeries.PolicyName);
        _canView = view.Succeeded;
        if (!_canView)
        {
            _canManage = false;
            _advancementState = AdvancementState.Hidden;
            return;
        }

        var manage = await AuthorizationService.AuthorizeAsync(authState.User, Permissions.ManageAdvancement.PolicyName);
        _canManage = manage.Succeeded;

        if (_advancement is null)
        {
            _advancementState = AdvancementState.Loading;
            StateHasChanged(); // show the skeleton rows while the call runs
        }

        await LoadAdvancementAsync();
    }

    private async Task RetryAdvancementAsync()
    {
        _advancementState = AdvancementState.Loading;
        await LoadAdvancementAsync();
    }

    /// <summary>
    /// A reload after a write keeps the old data on screen until the new data arrives. ApiExecutor cannot tell a 403
    /// from a 500, so the status is read inside the call.
    /// </summary>
    private async Task LoadAdvancementAsync()
    {
        HttpStatusCode? status = null;
        Func<CancellationToken, Task<IApiResponse<SeriesAdvancementResponse>>> call = async ct =>
        {
            var response = await SeriesApi.GetSeriesAdvancementAsync(Id, ct);
            status = response.StatusCode;
            return response;
        };

        var result = await ApiExecutor.ExecuteAsync("SeriesApi", "GetSeriesAdvancement", call);

        if (result.IsError)
        {
            HandleAdvancementLoadError(status);
            return;
        }

        var response = result.Value;
        var localTimes = new Dictionary<DateTimeOffset, DateTimeOffset>();
        foreach (var utc in response.Instants())
        {
            localTimes[utc] = await ClientTimeZoneService.ToLocalAsync(utc);
        }

        _advancement = response.ToViewModel(new AdvancementAccess(_canView, _canManage), localTimes);
        _version = _advancement.Version;
        _advancementState = AdvancementState.Loaded;
    }

    private void HandleAdvancementLoadError(HttpStatusCode? status)
    {
        if (status == HttpStatusCode.Forbidden)
        {
            // The viewer lost the permission after the circuit opened: hide the section, no toast.
            _canView = false;
            _canManage = false;
            _advancement = null;
            _advancementState = AdvancementState.Hidden;
            return;
        }

        if (_advancement is not null)
        {
            ToastService.Show("Reload Failed", SeriesDisplay.AdvancementReloadFailed, NotifySeverity.Warning);
            return;
        }

        _advancementState = AdvancementState.Failed;
    }

    private void ToggleStage(string stageId)
    {
        if (!_expandedStageIds.Remove(stageId))
        {
            _expandedStageIds.Add(stageId);
        }
    }

    // ---- Settling a write ----

    /// <summary>
    /// 2xx: toast and reload. 409: toast the API's reason and reload (the caller's dialog closes). Anything else is
    /// <see cref="WriteOutcome.Failed"/>, and the caller shows it.
    /// </summary>
    private async Task<WriteOutcome> SettleWriteAsync(ErrorOr<Success> result, string successTitle, string successMessage)
    {
        if (!result.IsError)
        {
            ToastService.Show(successTitle, successMessage, NotifySeverity.Success);
            await LoadAdvancementAsync();
            return WriteOutcome.Saved;
        }

        if (result.FirstError.Type == ErrorType.Conflict)
        {
            ToastService.Show("Series Changed", SeriesDisplay.ConflictMessage(result.FirstError.Description), NotifySeverity.Warning);
            await LoadAdvancementAsync();
            return WriteOutcome.Conflict;
        }

        return WriteOutcome.Failed;
    }

    // ---- Add bowler and Clear slot ----

    private async Task OpenAddOverrideAsync(StageAdvancementViewModel stage)
    {
        if (_isWriting)
        {
            return;
        }

        _overrideTarget = new OverrideTarget(stage.Id, OverrideKinds.Add, null);
        _addChoices = [];
        _resultsError = null;

        if (!_stageResults.ContainsKey(stage.Id))
        {
            await LoadStageResultsAsync(stage.Id);
        }

        if (_stageResults.TryGetValue(stage.Id, out var results) && _advancement is not null)
        {
            _addChoices = _advancement.AddCandidates(stage.Id, results);
        }
    }

    private async Task LoadStageResultsAsync(string stageId)
    {
        _isResultsLoading = true;
        var result = await ApiExecutor.ExecuteAsync("SeriesApi", "ListStageResults", ct => SeriesApi.ListStageResultsAsync(Id, stageId, ct));
        _isResultsLoading = false;

        if (result.IsError)
        {
            _resultsError = result.FirstError.Description; // not cached, so reopening the dialog tries again
            return;
        }

        _stageResults[stageId] = [.. result.Value.Items.Select(r => r.ToViewModel())];
    }

    private void OpenClearOverride(StageAdvancementViewModel stage, FieldEntryViewModel entry)
    {
        if (!_isWriting)
        {
            _overrideTarget = new OverrideTarget(stage.Id, OverrideKinds.Clear, entry);
        }
    }

    private void CloseOverrideModal() => _overrideTarget = null;

    /// <summary>The dialog stays open on any error except a 409, which the page handles (toast and reload).</summary>
    private async Task<ErrorOr<Success>> SubmitOverrideAsync(StageOverrideSubmission submission)
    {
        if (_overrideTarget is not { } target)
        {
            return Result.Success;
        }

        var order = _advancement?.StageById(target.StageId)?.Order ?? 0;
        _isWriting = true;
        try
        {
            var request = new CreateStageOverrideRequest
            {
                Id = Id,
                StageId = target.StageId,
                StageOverride = new CreateStageOverrideInput
                {
                    Kind = OverrideKinds.ToValue(target.Kind),
                    BowlerId = submission.BowlerId,
                    Reason = submission.Reason,
                    Version = _version,
                },
            };

            var created = await ApiExecutor.ExecuteAsync("SeriesApi", "CreateStageOverride",
                ct => SeriesApi.CreateStageOverrideAsync(Id, target.StageId, request, ct));

            // The reload that follows decides the next Version, so the Version in the response is ignored.
            ErrorOr<Success> result = created.IsError ? created.Errors : Result.Success;
            var (title, message) = target.Kind == OverrideKinds.Add
                ? ("Bowler Added", SeriesDisplay.OverrideAddedMessage(submission.BowlerName, order))
                : ("Slot Cleared", SeriesDisplay.SlotClearedMessage);

            var outcome = await SettleWriteAsync(result, title, message);
            return outcome == WriteOutcome.Failed ? result : Result.Success;
        }
        finally
        {
            _isWriting = false;
        }
    }

    // ---- Remove override ----

    private void OpenRemoveOverride(StageAdvancementViewModel stage, StageOverrideViewModel item)
    {
        if (!_isWriting)
        {
            _removeTarget = new RemoveTarget(stage.Id, stage.Order, item);
        }
    }

    private void CancelRemove()
    {
        if (!_isWriting)
        {
            _removeTarget = null;
        }
    }

    private async Task ConfirmRemoveAsync()
    {
        if (_removeTarget is not { } target || _isWriting)
        {
            return;
        }

        _isWriting = true;
        try
        {
            var result = await ApiExecutor.ExecuteAsync("SeriesApi", "RemoveStageOverride",
                ct => SeriesApi.RemoveStageOverrideAsync(Id, target.StageId, target.Override.OverrideId, _version, ct));

            var outcome = await SettleWriteAsync(result, "Override Removed", "Override removed.");
            if (outcome == WriteOutcome.Failed)
            {
                ToastService.Show("Remove Failed", result.FirstError.Description, NotifySeverity.Error);
            }
        }
        finally
        {
            _removeTarget = null;
            _isWriting = false;
        }
    }

    // ---- Recompute ----

    /// <summary>Runs at once when no Official Stage is in range; otherwise asks first, listing the Stages it resets.</summary>
    private async Task StartRecomputeAsync(StageAdvancementViewModel stage)
    {
        if (_isWriting || _advancement is null)
        {
            return;
        }

        var official = _advancement.OfficialOrdersFrom(stage.Order);
        if (official.Count == 0)
        {
            await RecomputeAsync(stage.Id, stage.Order, confirmed: false);
            return;
        }

        _recomputeTarget = new RecomputeTarget(stage.Id, stage.Order, official);
    }

    private Task ConfirmRecomputeAsync()
        => _recomputeTarget is { } target
            ? RecomputeAsync(target.StageId, target.Order, confirmed: true)
            : Task.CompletedTask;

    private void CancelRecompute()
    {
        if (!_isWriting)
        {
            _recomputeTarget = null;
        }
    }

    private async Task RecomputeAsync(string stageId, int order, bool confirmed)
    {
        if (_isWriting)
        {
            return;
        }

        _isWriting = true;
        try
        {
            var request = new RecomputeSeriesAdvancementRequest
            {
                Id = Id,
                StageId = stageId,
                Recomputation = new RecomputeSeriesAdvancementInput { Version = _version, ConfirmResetOfficial = confirmed },
            };

            var result = await ApiExecutor.ExecuteAsync("SeriesApi", "RecomputeSeriesAdvancement",
                ct => SeriesApi.RecomputeSeriesAdvancementAsync(Id, stageId, request, ct));

            _recomputeTarget = null;
            var outcome = await SettleWriteAsync(result, "Recomputed", SeriesDisplay.RecomputedMessage(order));

            if (outcome == WriteOutcome.Failed)
            {
                ToastService.Show("Recompute Failed", result.FirstError.Description, NotifySeverity.Error);
            }
            else if (outcome == WriteOutcome.Conflict && _advancement is not null)
            {
                // A Stage became official since the page loaded: ask again with the fresh list. ApiExecutor drops the
                // API's OfficialStageOrders metadata, so the list comes from the reloaded data.
                var official = _advancement.OfficialOrdersFrom(order);
                if (official.Count > 0)
                {
                    _recomputeTarget = new RecomputeTarget(stageId, order, official);
                }
            }
        }
        finally
        {
            _isWriting = false;
        }
    }

    // ---- Pick squad ----

    private async Task OpenSquadPickerAsync(StageAdvancementViewModel stage)
    {
        if (_isWriting)
        {
            return;
        }

        _squadStageId = stage.Id;
        _squads = [];
        _squadsError = null;
        _isSquadsLoading = true;

        var result = await ApiExecutor.ExecuteAsync("SeriesApi", "ListStageSquads", ct => SeriesApi.ListStageSquadsAsync(Id, stage.Id, ct));
        _isSquadsLoading = false;

        if (result.IsError)
        {
            _squadsError = result.FirstError.Description;
            return;
        }

        _squads = [.. result.Value.Items.Select(s => s.ToViewModel())];
    }

    private void CloseSquadPicker() => _squadStageId = null;

    private async Task<ErrorOr<Success>> SubmitSquadAsync(string squadId)
    {
        if (_squadStageId is not { } stageId)
        {
            return Result.Success;
        }

        _isWriting = true;
        try
        {
            var request = new PickStageSquadRequest
            {
                Id = Id,
                StageId = stageId,
                Squad = new PickStageSquadInput { SquadId = squadId, Version = _version },
            };

            var result = await ApiExecutor.ExecuteAsync("SeriesApi", "PickStageSquad",
                ct => SeriesApi.PickStageSquadAsync(Id, stageId, request, ct));

            var outcome = await SettleWriteAsync(result, "Squad Saved", "Squad saved.");
            return outcome == WriteOutcome.Failed ? result : Result.Success;
        }
        finally
        {
            _isWriting = false;
        }
    }
}
```

#### 8. New file `src/Neba.Website.Server/Series/SeriesProgressStrip.razor`

```razor
<div class="series-strip" aria-hidden="true">
    @foreach (var stage in Stages)
    {
        <span @key="stage.Id" class="@SeriesDisplay.SegmentClass(stage.Status)" title="@SeriesDisplay.SegmentTitle(stage.Order, stage.Status)"></span>
    }
</div>
<div class="series-strip-legend">
    <span>Open circle: pending</span>
    <span class="series-strip-legend__computed">Blue: computed</span>
    <span class="series-strip-legend__official">Green: official</span>
</div>
<p class="series-progress"><strong>@Progress.Lead</strong>@Progress.Detail</p>

@code {
    [Parameter, EditorRequired]
    public IReadOnlyList<StageAdvancementViewModel> Stages { get; set; } = [];

    [Parameter]
    public DateOnly? BackfillAsOf { get; set; }

    private SummaryLine Progress => SeriesDisplay.Progress(Stages, BackfillAsOf);
}
```

`SeriesProgressStrip.razor.css`:

```css
.series-strip {
    display: grid;
    grid-auto-flow: column;
    grid-auto-columns: 1fr;
    gap: 0.25rem;
    margin: 0 0 0.5rem;
}

.series-strip__seg {
    display: block;
    height: 0.625rem;
    border-radius: 3px;
    background: var(--series-pending-bg);
    border: 1px solid var(--neba-border);
}

.series-strip__seg--computed {
    background: var(--series-computed-fg);
    border-color: var(--series-computed-fg);
}

.series-strip__seg--official {
    background: var(--series-official-fg);
    border-color: var(--series-official-fg);
}

.series-strip-legend {
    display: flex;
    flex-wrap: wrap;
    gap: 1rem;
    font-size: 0.8125rem;
    color: var(--neba-text-muted);
    margin-bottom: 1.375rem;
}

.series-strip-legend__computed { color: var(--series-computed-fg); }
.series-strip-legend__official { color: var(--series-official-fg); }

.series-progress {
    color: var(--neba-text-muted);
    font-size: 0.875rem;
    margin: 0 0 1.25rem;
}

.series-progress strong { color: var(--neba-text); }
```

#### 9. New file `src/Neba.Website.Server/Series/SeriesStageCard.razor`

```razor
<div class="neba-card series-card">
    <div class="series-card__top">
        <div>
            <span class="series-card__kind @SeriesDisplay.KindClass(Stage.Kind)">@SeriesDisplay.StageChip(Stage.Kind, Stage.Order)</span>
            <h3 class="series-card__title"><a href="@TournamentHref">@TournamentName</a></h3>
            <p class="series-card__meta">@Meta</p>
        </div>
        <div class="series-card__right">
            @if (Headline is not null)
            {
                <span class="series-card__fill">@Headline</span>
            }
            <span class="@SeriesDisplay.StatusPillClass(Stage.Status)">@Stage.Status</span>
            <button type="button" class="series-expander"
                    aria-expanded="@(IsExpanded ? "true" : "false")"
                    aria-controls="@PanelId"
                    aria-label="@SeriesDisplay.ExpandLabel(IsExpanded, Stage.Order)"
                    @onclick="OnToggle">
                <svg width="12" height="12" viewBox="0 0 12 12" aria-hidden="true"><path d="M4 2l4 4-4 4" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" /></svg>
            </button>
        </div>
    </div>

    <div class="series-card__sub">
        @if (HasFlags)
        {
            <div class="series-flags">
                @if (Stage.IsTournamentCancelled)
                {
                    <span class="series-flag series-flag--error">Tournament cancelled: <a href="@EditHref">remove this Stage</a></span>
                }
                @if (Stage.NeedsSquad)
                {
                    <span class="series-flag series-flag--warn">Needs squad</span>
                }
                @if (Stage.IsStale)
                {
                    <span class="series-flag series-flag--warn" title="@SeriesDisplay.StaleHint">Stale</span>
                }
                @if (DueLabel is not null)
                {
                    <span class="series-flag series-flag--info">@DueLabel</span>
                }
                @if (BackfillLabel is not null)
                {
                    <span class="series-flag">@BackfillLabel</span>
                }
            </div>
        }
        else
        {
            <span></span>
        }

        <div class="series-acts">
            @if (Stage.CanPickSquad)
            {
                <button type="button" class="series-act" disabled="@IsBusy" @onclick="OnPickSquad">Pick squad</button>
            }
            @if (Stage.CanRecompute)
            {
                <button type="button" class="series-act" disabled="@IsBusy" @onclick="OnRecompute">Recompute from here</button>
            }
        </div>
    </div>

    @if (IsExpanded)
    {
        <div class="series-card__panel" id="@PanelId">
            @ChildContent
        </div>
    }
</div>

@code {
    [Parameter, EditorRequired]
    public StageAdvancementViewModel Stage { get; set; } = default!;

    [Parameter, EditorRequired]
    public string TournamentName { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string TournamentHref { get; set; } = string.Empty;

    /// <summary>"7/12 · 3-bowler teams · Top 2 teams (6 bowlers)".</summary>
    [Parameter, EditorRequired]
    public string Meta { get; set; } = string.Empty;

    /// <summary>For the Placement headline ("20 ranked").</summary>
    [Parameter]
    public int PositionCount { get; set; }

    /// <summary>Set on the last Qualifying Stage once the Backfill ran.</summary>
    [Parameter]
    public string? BackfillLabel { get; set; }

    [Parameter, EditorRequired]
    public string EditHref { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public bool IsExpanded { get; set; }

    [Parameter, EditorRequired]
    public bool IsBusy { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnToggle { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnRecompute { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnPickSquad { get; set; }

    /// <summary>The output panel, shown when expanded.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string? Headline => SeriesDisplay.Headline(Stage, PositionCount);

    private string? DueLabel => Stage.Status == StageStatuses.Computed ? SeriesDisplay.OfficialDueLabel(Stage.OfficialDueAt) : null;

    private string PanelId => "series-output-" + Stage.Id;

    private bool HasFlags => Stage.IsTournamentCancelled || Stage.NeedsSquad || Stage.IsStale || DueLabel is not null || BackfillLabel is not null;
}
```

`SeriesStageCard.razor.css`:

```css
.series-card {
    padding: 0.875rem 1.125rem;
}

.series-card__top {
    display: flex;
    flex-wrap: wrap;
    gap: 0.75rem;
    align-items: flex-start;
    justify-content: space-between;
}

.series-card__kind {
    display: inline-flex;
    align-items: center;
    gap: 0.375rem;
    font-size: 0.8125rem;
    font-weight: 600;
    white-space: nowrap;
}

.series-card__kind::before {
    content: "";
    width: 0.5rem;
    height: 0.5rem;
    border-radius: 2px;
    background: currentColor;
}

.series-card__kind.kind-qualifying { color: var(--neba-blue-600); }
.series-card__kind.kind-survivor { color: var(--neba-gold-deep); }
.series-card__kind.kind-placement { color: var(--neba-blue-brand); }
.dark .series-card__kind.kind-qualifying,
.dark .series-card__kind.kind-placement { color: var(--neba-blue-300); }

.series-card__title {
    font: 700 1rem/1.3 var(--neba-font-display);
    margin: 0.125rem 0 0;
}

.series-card__title a { color: var(--neba-text); text-decoration: none; }
.series-card__title a:hover { text-decoration: underline; }

.series-card__meta {
    color: var(--neba-text-muted);
    font-size: 0.84375rem;
    margin: 0.125rem 0 0;
}

.series-card__right {
    display: flex;
    flex-wrap: wrap;
    gap: 0.625rem;
    align-items: center;
}

.series-card__fill { font-weight: 600; font-size: 0.875rem; }

.series-card__sub {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem 1.125rem;
    justify-content: space-between;
    align-items: center;
    margin-top: 0.625rem;
}

.series-card__panel {
    border-top: 1px dashed var(--neba-border);
    margin-top: 0.625rem;
    padding-top: 0.625rem;
}

/* Status pill: every pill carries text, so color is never the only signal. */
.series-pill {
    display: inline-flex;
    align-items: center;
    gap: 0.375rem;
    font: 600 0.78125rem/1 var(--neba-font-base);
    padding: 0.3125rem 0.625rem;
    border-radius: 999px;
    white-space: nowrap;
}

.series-pill::before {
    content: "";
    width: 0.4375rem;
    height: 0.4375rem;
    border-radius: 50%;
    background: currentColor;
}

.series-pill--pending { background: var(--series-pending-bg); color: var(--series-pending-fg); }
.series-pill--pending::before { background: transparent; border: 1.5px solid currentColor; }
.series-pill--computed { background: var(--series-computed-bg); color: var(--series-computed-fg); }
.series-pill--official { background: var(--series-official-bg); color: var(--series-official-fg); }

.series-flags { display: flex; flex-wrap: wrap; gap: 0.375rem 0.5rem; }

.series-flag {
    display: inline-flex;
    align-items: center;
    gap: 0.375rem;
    font-size: 0.8125rem;
    font-weight: 500;
    padding: 0.1875rem 0.625rem;
    border-radius: var(--neba-radius);
    background: var(--series-pending-bg);
    color: var(--neba-text);
}

.series-flag--warn { background: var(--series-warn-bg); color: var(--series-warn-fg); }
.series-flag--error { background: var(--series-error-bg); color: var(--series-error-fg); }
.series-flag--info { background: var(--series-computed-bg); color: var(--series-computed-fg); }
.series-flag a { color: inherit; font-weight: 600; }

.series-acts { display: flex; flex-wrap: wrap; gap: 0.25rem 0.875rem; align-items: center; }

.series-act {
    background: none;
    border: 0;
    padding: 0.375rem 0;
    min-height: 2rem;
    font: 600 0.84375rem/1.2 var(--neba-font-base);
    color: var(--neba-blue-600);
    cursor: pointer;
}

.dark .series-act { color: var(--neba-blue-300); }
.series-act:hover { text-decoration: underline; }
.series-act:focus-visible { outline: 2px solid var(--neba-blue-500); outline-offset: 2px; border-radius: 2px; }
.series-act:disabled { color: var(--neba-text-muted); cursor: not-allowed; text-decoration: none; }

.series-expander {
    width: 2rem;
    height: 2rem;
    border-radius: var(--neba-radius);
    border: 1px solid var(--neba-border);
    background: transparent;
    color: var(--neba-text-muted);
    cursor: pointer;
    display: inline-grid;
    place-items: center;
}

.series-expander svg { transition: transform 0.15s; }
.series-expander[aria-expanded="true"] svg { transform: rotate(90deg); }
.series-expander:hover { color: var(--neba-text); }
.series-expander:focus-visible { outline: 2px solid var(--neba-blue-500); outline-offset: 2px; }

@media (max-width: 767px) {
    .series-act { min-height: 2.75rem; }
    .series-expander { width: 2.75rem; height: 2.75rem; }
}
```

#### 10. New file `src/Neba.Website.Server/Series/SeriesStageOutput.razor`

The Actions column shows for `CanManage` holders only. Its header text is never empty. Every row carries the cell, so the cell count matches the header count.

```razor
@if (Stage.Status == StageStatuses.Pending)
{
    <div class="series-output">
        <p class="series-output__empty">@SeriesDisplay.PendingReason(Stage)</p>
    </div>
}
else
{
    <div class="series-output">
        @if (Stage.IsStale)
        {
            <p class="series-output__summary series-output__summary--warn">@SeriesDisplay.StaleNote</p>
        }

        @if (Stage.Kind == StageKinds.Qualifying)
        {
            <p class="series-output__summary"><strong>@SeriesDisplay.SlotsLead(Stage.SlotCount ?? 0)</strong>@SeriesDisplay.VacantLabel(Stage.VacantSlotCount ?? 0)</p>
            <div class="series-output__tools">
                <span class="series-output__hint">@SeriesDisplay.EntriesHeading(Stage.IsLastQualifying)</span>
                @if (Stage.CanAddOverride)
                {
                    <button type="button" class="neba-btn neba-btn-secondary neba-btn-sm" disabled="@IsBusy" @onclick="OnAddBowler">Add bowler</button>
                }
            </div>

            @if (Stage.Entries.Count == 0)
            {
                <p class="series-output__empty">@SeriesDisplay.NoFieldEntries</p>
            }
            else
            {
                <div class="series-output__scroll series-output__scroll--free">
                    <table class="neba-table neba-data-table">
                        <thead>
                            <tr>
                                <th scope="col">Place</th>
                                <th scope="col">Bowler</th>
                                <th scope="col">Source</th>
                                <th scope="col">Backfill points</th>
                                @if (CanManage)
                                {
                                    <th scope="col">Actions</th>
                                }
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var entry in Stage.Entries)
                            {
                                <tr @key="entry.BowlerId">
                                    <td class="series-output__num">@SeriesDisplay.OrdinalPlace(entry.Place)</td>
                                    <td>@entry.BowlerName</td>
                                    <td class="@SeriesDisplay.SourceClass(entry)">@SeriesDisplay.EntrySource(entry)</td>
                                    <td class="series-output__num">@SeriesDisplay.Points(entry.BackfillPoints)</td>
                                    @if (CanManage)
                                    {
                                        <td>
                                            @if (entry.CanClear)
                                            {
                                                <button type="button" class="series-act" disabled="@IsBusy" @onclick="@(() => OnClearSlot.InvokeAsync(entry))">Clear slot</button>
                                            }
                                        </td>
                                    }
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            }

            @if (Stage.Overrides.Count > 0)
            {
                <h4 class="series-output__heading">Admin overrides</h4>
                <div class="series-output__scroll series-output__scroll--free">
                    <table class="neba-table neba-data-table">
                        <thead>
                            <tr>
                                <th scope="col">Change</th>
                                <th scope="col">Bowler</th>
                                <th scope="col">Reason</th>
                                <th scope="col">Added</th>
                                <th scope="col">Status</th>
                                @if (CanManage)
                                {
                                    <th scope="col">Actions</th>
                                }
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var item in Stage.Overrides)
                            {
                                <tr @key="item.OverrideId">
                                    <td>@SeriesDisplay.OverrideChange(item.Kind)</td>
                                    <td>@item.BowlerName</td>
                                    <td class="series-output__reason">@item.Reason</td>
                                    <td>@SeriesDisplay.AddedDate(item.CreatedAt)</td>
                                    <td>
                                        @if (item.Applied)
                                        {
                                            @SeriesDisplay.OverrideStatus(true)
                                        }
                                        else
                                        {
                                            <span class="series-output__inactive">@SeriesDisplay.OverrideStatus(false)</span>
                                        }
                                    </td>
                                    @if (CanManage)
                                    {
                                        <td>
                                            @if (Stage.CanRemoveOverride)
                                            {
                                                <button type="button" class="series-act" disabled="@IsBusy" @onclick="@(() => OnRemoveOverride.InvokeAsync(item))">Remove override</button>
                                            }
                                        </td>
                                    }
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            }
        }
        else if (Stage.Kind == StageKinds.Survivor)
        {
            @if (Stage.Outcomes.Count == 0)
            {
                <p class="series-output__empty">@SeriesDisplay.NoFieldEntries</p>
            }
            else
            {
                var summary = SeriesDisplay.SurvivorSummary(Stage.Outcomes);
                <p class="series-output__summary"><strong>@summary.Lead</strong>@summary.Detail</p>
                <div class="series-output__scroll">
                    <table class="neba-table">
                        <thead>
                            <tr>
                                <th scope="col">Bowler</th>
                                <th scope="col">Block total</th>
                                <th scope="col">Advanced</th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var outcome in Stage.Outcomes)
                            {
                                <tr @key="outcome.BowlerId" class="@(outcome.Advanced ? null : "series-output__out")">
                                    <td>@outcome.BowlerName</td>
                                    <td class="series-output__num">@SeriesDisplay.BlockTotal(outcome.BlockTotal)</td>
                                    <td>@(outcome.Advanced ? "Yes" : "No")</td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            }
        }
        else
        {
            @if (Positions.Count == 0)
            {
                <p class="series-output__empty">Positions appear when the Placement Round computes.</p>
            }
            else
            {
                var hasFinalists = Positions.Any(p => p.IsFinalist is not null);
                var summary = SeriesDisplay.PlacementSummary(Positions.Count, hasFinalists);
                <p class="series-output__summary"><strong>@summary.Lead</strong>@summary.Detail</p>
                <div class="series-output__scroll">
                    <table class="@(hasFinalists ? "neba-table neba-data-table" : "neba-table")">
                        <thead>
                            <tr>
                                <th scope="col">Position</th>
                                <th scope="col">Bowler</th>
                                <th scope="col">Bowled</th>
                                @if (hasFinalists)
                                {
                                    <th scope="col">Finalist</th>
                                }
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var position in Positions)
                            {
                                <tr @key="position.BowlerId">
                                    <td class="series-output__num">@position.Position.ToOrdinal()</td>
                                    <td>@position.BowlerName</td>
                                    <td>@(position.Bowled ? "Yes" : "No")</td>
                                    @if (hasFinalists)
                                    {
                                        <td>@(position.IsFinalist == true ? "Yes" : "No")</td>
                                    }
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            }
        }
    </div>
}

@code {
    [Parameter, EditorRequired]
    public StageAdvancementViewModel Stage { get; set; } = default!;

    /// <summary>The Series' Placement positions (only the Placement Stage shows them).</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<PositionViewModel> Positions { get; set; } = [];

    /// <summary>Holds Series.ManageAdvancement: show the Actions columns.</summary>
    [Parameter, EditorRequired]
    public bool CanManage { get; set; }

    [Parameter, EditorRequired]
    public bool IsBusy { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnAddBowler { get; set; }

    [Parameter, EditorRequired]
    public EventCallback<FieldEntryViewModel> OnClearSlot { get; set; }

    [Parameter, EditorRequired]
    public EventCallback<StageOverrideViewModel> OnRemoveOverride { get; set; }
}
```

Razor note: inside `else if (...) { @if ...` and `else { @if ... }` I start the block with `@if`. Razor can reject an `@`-prefixed keyword directly after a code-block brace (RZ1010). If the build complains, drop the `@` on those two lines (`Stage.Outcomes.Count == 0` and `Positions.Count == 0`). The `var summary` lines are then plain statements and compile as written.

`SeriesStageOutput.razor.css`:

```css
.series-output { padding: 0.25rem 0 0.125rem; }

.series-output__heading {
    font: 700 0.875rem/1.3 var(--neba-font-display);
    margin: 1.125rem 0 0.5rem;
}

.series-output__summary { margin: 0.25rem 0 0.75rem; color: var(--neba-text-muted); font-size: 0.875rem; }
.series-output__summary strong { color: var(--neba-text); }
.series-output__summary--warn { color: var(--series-warn-fg); }
.series-output__empty { margin: 0.25rem 0; color: var(--neba-text-muted); font-size: 0.875rem; max-width: 62ch; }

.series-output__tools {
    display: flex;
    flex-wrap: wrap;
    justify-content: space-between;
    align-items: center;
    gap: 0.75rem;
    margin-bottom: 0.5rem;
}

.series-output__hint { color: var(--neba-text-muted); font-size: 0.875rem; }

.series-output__scroll {
    max-height: 22.5rem;
    overflow: auto;
    border: 1px solid var(--neba-border);
    border-radius: var(--neba-radius-lg);
}

.series-output__scroll--free { max-height: none; overflow: visible; }

.series-output__scroll thead th {
    position: sticky;
    top: 0;
    background: var(--neba-surface-high);
    color: var(--neba-text-muted);
    font-size: 0.8125rem;
}

.series-output__scroll td { font-size: 0.875rem; vertical-align: top; }
.series-output__num { font-variant-numeric: tabular-nums; }
.series-output__out td { color: var(--neba-text-muted); }
.series-output__reason { max-width: 46ch; }
.series-output__inactive { color: var(--series-warn-fg); font-weight: 600; }
.series-src--admin { color: var(--series-computed-fg); font-weight: 600; }
.series-src--backfill { color: var(--neba-gold-deep); font-weight: 600; }

.series-act {
    background: none;
    border: 0;
    padding: 0.375rem 0;
    min-height: 2rem;
    font: 600 0.84375rem/1.2 var(--neba-font-base);
    color: var(--neba-blue-600);
    cursor: pointer;
}

.dark .series-act { color: var(--neba-blue-300); }
.series-act:hover { text-decoration: underline; }
.series-act:focus-visible { outline: 2px solid var(--neba-blue-500); outline-offset: 2px; border-radius: 2px; }
.series-act:disabled { color: var(--neba-text-muted); cursor: not-allowed; text-decoration: none; }

@media (max-width: 767px) {
    .series-act { min-height: 2.75rem; }
}
```

#### 11. New file `src/Neba.Website.Server/Series/StageOverrideModal.razor`

```razor
@using ErrorOr
@implements IDisposable

<DirtyFormGuard IsDirty="@(IsOpen && _isDirty)" />

<ConfirmActionModal IsOpen="@_isDiscardConfirmOpen"
                    Title="Discard changes?"
                    Message="You have not saved this change. Closing now loses what you typed."
                    ConfirmLabel="Discard"
                    CancelLabel="Keep editing"
                    OnConfirm="ConfirmDiscardAsync"
                    OnCancel="@(() => _isDiscardConfirmOpen = false)" />

<NebaModal IsOpen="@IsOpen" OnClose="@HandleCancelRequestedAsync" MaxWidth="520px" CompactSize="true"
           AriaLabelledById="stage-override-title">
    <HeaderContent>
        <h2 class="neba-modal-title" id="stage-override-title">@SeriesDisplay.OverrideTitle(Kind)</h2>
    </HeaderContent>
    <ChildContent>
        <div class="stage-override">
            <p class="stage-override__lead">@SeriesDisplay.OverrideLead(Kind, StageOrder, TournamentName)</p>

            @if (_errorMessage is not null)
            {
                <NebaAlert Severity="NotifySeverity.Error" Title="Couldn't save the change" Message="@_errorMessage"
                           Dismissible="true" OnDismiss="@(() => _errorMessage = null)" />
            }

            @* Enter never submits: the autocomplete uses Enter to pick a bowler. The Save button validates and sends. *@
            <EditForm @key="_formKey" EditContext="_editContext" OnSubmit="IgnoreFormSubmit">
                <DataAnnotationsValidator />

                <div class="stage-override__field">
                    @if (Kind == OverrideKinds.Add)
                    {
                        <label for="override-bowler" class="stage-override__label">Bowler</label>
                        @if (IsLoadingChoices)
                        {
                            <NebaSkeletonLoader Type="SkeletonType.Text" Rows="2" />
                        }
                        else if (ChoicesError is not null)
                        {
                            <NebaAlert Severity="NotifySeverity.Error" Title="Couldn't load the bowlers" Message="@ChoicesError" />
                        }
                        else
                        {
                            <NebaAutocomplete Id="override-bowler" TValue="string" TItem="StageResultViewModel"
                                              Value="@_bowlerId"
                                              ValueChanged="HandleBowlerChanged"
                                              Items="@Choices"
                                              DisplayText="@(choice => choice.Label)"
                                              ItemValue="@(choice => choice.BowlerId)"
                                              Placeholder="Search by name"
                                              EmptyLabel="Search by name"
                                              NoResultsText="No bowler with a result matches. Bowlers already in the field are not listed." />
                        }
                    }
                    else
                    {
                        <span class="stage-override__label">Bowler</span>
                        <div class="stage-override__readonly"><strong>@BowlerName</strong> · @SeriesDisplay.ClearPlace(Place)</div>
                    }
                </div>

                <div class="stage-override__field">
                    <FormLabel TargetId="override-reason" For="@(() => _model.Reason)">Reason</FormLabel>
                    <textarea id="override-reason" class="neba-textarea stage-override__reason" maxlength="600"
                              value="@_model.Reason" @oninput="HandleReasonInput"
                              aria-describedby="override-reason-hint override-reason-error"></textarea>
                    <NebaValidationMessage For="@(() => _model.Reason)" Id="override-reason-error" Class="stage-override__error" />
                    <div class="stage-override__hint" id="override-reason-hint">
                        <span>Shown to other admins. Say who confirmed it.</span>
                        <span class="@CountClass">@SeriesDisplay.ReasonCount(_model.Reason.Length)</span>
                    </div>
                </div>
            </EditForm>
        </div>
    </ChildContent>
    <FooterContent>
        <div class="stage-override__actions">
            <button type="button" class="neba-btn neba-btn-secondary" @onclick="HandleCancelRequestedAsync" disabled="@_isSubmitting">Cancel</button>
            <button type="button" class="neba-btn neba-btn-primary" @onclick="SubmitAsync" disabled="@(!CanSave)">
                @(_isSubmitting ? "Saving..." : SeriesDisplay.OverrideTitle(Kind))
            </button>
        </div>
    </FooterContent>
</NebaModal>

@code {
    [Parameter, EditorRequired]
    public bool IsOpen { get; set; }

    /// <summary>Add or Clear (<see cref="OverrideKinds"/>).</summary>
    [Parameter, EditorRequired]
    public string Kind { get; set; } = OverrideKinds.Add;

    [Parameter, EditorRequired]
    public int StageOrder { get; set; }

    [Parameter, EditorRequired]
    public string TournamentName { get; set; } = string.Empty;

    /// <summary>Clear only: the bowler whose slot clears.</summary>
    [Parameter]
    public string? BowlerId { get; set; }

    /// <summary>Clear only.</summary>
    [Parameter]
    public string BowlerName { get; set; } = string.Empty;

    /// <summary>Clear only: the bowler's place.</summary>
    [Parameter]
    public int? Place { get; set; }

    /// <summary>Add only: result rows not already in the field and with no override on this Stage.</summary>
    [Parameter]
    public IReadOnlyList<StageResultViewModel> Choices { get; set; } = [];

    [Parameter]
    public bool IsLoadingChoices { get; set; }

    [Parameter]
    public string? ChoicesError { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnClose { get; set; }

    /// <summary>
    /// Sends the change. An error keeps the dialog open and shows its text; success closes it.
    /// The page handles a 409 itself (toast and reload) and returns success.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<StageOverrideSubmission, Task<ErrorOr<Success>>> OnSubmitRequestedAsync { get; set; } = default!;

    private StageOverrideFormModel _model = new();
    private EditContext _editContext = default!;
    private FieldIdentifier _reasonField;
    private Guid _formKey = Guid.NewGuid();
    private string? _bowlerId;
    private bool _isDirty;
    private bool _isSubmitting;
    private bool _isDiscardConfirmOpen;
    private bool _wasOpen;
    private string? _errorMessage;

    private string? SelectedBowlerId => Kind == OverrideKinds.Add ? _bowlerId : BowlerId;

    private string SelectedBowlerName
        => Kind == OverrideKinds.Add
            ? Choices.FirstOrDefault(choice => choice.BowlerId == _bowlerId)?.BowlerName ?? string.Empty
            : BowlerName;

    private bool CanSave
        => !_isSubmitting
           && SelectedBowlerId is not null
           && !string.IsNullOrWhiteSpace(_model.Reason)
           && _model.Reason.Length <= StageOverrideFormModel.ReasonMaxLength;

    private string CountClass
        => _model.Reason.Length > StageOverrideFormModel.ReasonMaxLength
            ? "stage-override__count stage-override__count--over"
            : "stage-override__count";

    protected override void OnInitialized() => ResetForm();

    protected override void OnParametersSet()
    {
        if (IsOpen && !_wasOpen)
        {
            ResetForm(); // each open starts clean
        }

        _wasOpen = IsOpen;
    }

    public void Dispose() => _editContext.OnFieldChanged -= HandleFieldChanged;

    private void ResetForm()
    {
        if (_editContext is not null)
        {
            _editContext.OnFieldChanged -= HandleFieldChanged;
        }

        _model = new StageOverrideFormModel();
        _editContext = new EditContext(_model);
        _editContext.OnFieldChanged += HandleFieldChanged;
        _reasonField = _editContext.Field(nameof(StageOverrideFormModel.Reason));
        _formKey = Guid.NewGuid(); // a new key rebuilds the form, so old validation messages go
        _bowlerId = null;
        _isDirty = false;
        _isSubmitting = false;
        _isDiscardConfirmOpen = false;
        _errorMessage = null;
    }

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs e) => MarkDirty();

    private void MarkDirty() => _isDirty = true;

    private static Task IgnoreFormSubmit(EditContext _) => Task.CompletedTask;

    private void HandleBowlerChanged(string? bowlerId)
    {
        _bowlerId = bowlerId;
        MarkDirty(); // the picker is not an InputBase, so the EditContext never hears about it
    }

    private void HandleReasonInput(ChangeEventArgs e)
    {
        _model.Reason = e.Value?.ToString() ?? string.Empty;
        _editContext.NotifyFieldChanged(_reasonField); // validates the field and marks the form dirty
    }

    private Task HandleCancelRequestedAsync()
    {
        if (_isSubmitting)
        {
            return Task.CompletedTask;
        }

        if (_isDirty)
        {
            _isDiscardConfirmOpen = true;
            return Task.CompletedTask;
        }

        return OnClose.InvokeAsync();
    }

    private Task ConfirmDiscardAsync()
    {
        _isDiscardConfirmOpen = false;
        _isDirty = false;
        return OnClose.InvokeAsync();
    }

    private async Task SubmitAsync()
    {
        if (!CanSave || !_editContext.Validate())
        {
            return;
        }

        _isSubmitting = true;
        _errorMessage = null;

        var result = await OnSubmitRequestedAsync(
            new StageOverrideSubmission(SelectedBowlerId!, SelectedBowlerName, _model.Reason.Trim()));

        _isSubmitting = false;

        if (result.IsError)
        {
            _errorMessage = result.FirstError.Description; // a 422 stays here, with the dialog open
            return;
        }

        _isDirty = false; // before the close, so the guard does not fire
        await OnClose.InvokeAsync();
    }
}
```

`StageOverrideModal.razor.css`:

```css
.stage-override__lead { margin: 0 0 1rem; color: var(--neba-text-muted); font-size: 0.875rem; }
.stage-override__field { margin-bottom: 0.875rem; }
.stage-override__label { display: block; font-weight: 600; font-size: 0.875rem; margin-bottom: 0.375rem; }

.stage-override__readonly {
    background: var(--neba-surface-high);
    border-radius: var(--neba-radius);
    padding: 0.625rem 0.75rem;
    font-size: 0.875rem;
}

.stage-override__reason { min-height: 5.5rem; resize: vertical; }

.stage-override__hint {
    display: flex;
    justify-content: space-between;
    gap: 0.75rem;
    font-size: 0.8125rem;
    color: var(--neba-text-muted);
    margin-top: 0.25rem;
}

.stage-override__count--over { color: var(--neba-accent-red); font-weight: 600; }
.stage-override__error { display: block; font-size: 0.875rem; color: var(--neba-accent-red); margin-top: 0.25rem; }
.stage-override__actions { display: flex; justify-content: flex-end; gap: 0.5rem; flex-wrap: wrap; }

/* The list opens inline so the modal body never clips it (matters on a phone). The panel belongs to
   NebaAutocomplete, so ::deep reaches it from the .stage-override wrapper, which carries this component's scope. */
.stage-override ::deep .neba-autocomplete-panel {
    position: static;
    margin-top: 0.375rem;
    max-height: 12.5rem;
}
```

#### 12. New file `src/Neba.Website.Server/Series/StageSquadModal.razor`

```razor
@using ErrorOr

<NebaModal IsOpen="@IsOpen" OnClose="@HandleCloseAsync" MaxWidth="520px" CompactSize="true" Title="Pick squad">
    <ChildContent>
        <div class="stage-squad">
            <p class="stage-squad__lead">@SeriesDisplay.SquadLead(StageOrder, TournamentName, ExpectedSquad, NeedsSquad)</p>

            @if (_errorMessage is not null)
            {
                <NebaAlert Severity="NotifySeverity.Error" Title="Couldn't save the squad" Message="@_errorMessage"
                           Dismissible="true" OnDismiss="@(() => _errorMessage = null)" />
            }

            @if (IsLoading)
            {
                <NebaSkeletonLoader Type="SkeletonType.Text" Rows="2" />
            }
            else if (LoadError is not null)
            {
                <NebaAlert Severity="NotifySeverity.Error" Title="Couldn't load the squads" Message="@LoadError" />
            }
            else if (Squads.Count == 0)
            {
                <p class="stage-squad__empty">@SeriesDisplay.NoSquads</p>
            }
            else
            {
                <fieldset class="stage-squad__list" aria-label="Squads">
                    @foreach (var squad in Squads)
                    {
                        <label class="stage-squad__row" @key="squad.SquadId">
                            <input type="radio" name="stage-squad" value="@squad.SquadId"
                                   checked="@(_selectedId == squad.SquadId)"
                                   @onchange="@(() => _selectedId = squad.SquadId)" />
                            <span><b>@squad.Label</b><small>@squad.Detail</small></span>
                        </label>
                    }
                </fieldset>
            }
        </div>
    </ChildContent>
    <FooterContent>
        <div class="stage-squad__actions">
            <button type="button" class="neba-btn neba-btn-secondary" @onclick="HandleCloseAsync" disabled="@_isSubmitting">Cancel</button>
            <button type="button" class="neba-btn neba-btn-primary" @onclick="SubmitAsync" disabled="@(!CanSave)">
                @(_isSubmitting ? "Saving..." : "Use this squad")
            </button>
        </div>
    </FooterContent>
</NebaModal>

@code {
    [Parameter, EditorRequired]
    public bool IsOpen { get; set; }

    [Parameter, EditorRequired]
    public int StageOrder { get; set; }

    [Parameter, EditorRequired]
    public string TournamentName { get; set; } = string.Empty;

    /// <summary>The squad the Series expects, such as "9:00 AM squad on 7/12"; null when it names none.</summary>
    [Parameter]
    public string? ExpectedSquad { get; set; }

    [Parameter]
    public bool NeedsSquad { get; set; }

    [Parameter, EditorRequired]
    public IReadOnlyList<StageSquadViewModel> Squads { get; set; } = [];

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public string? LoadError { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnClose { get; set; }

    /// <summary>Saves the chosen squad. An error keeps the dialog open; success closes it. The page handles a 409.</summary>
    [Parameter, EditorRequired]
    public Func<string, Task<ErrorOr<Success>>> OnSubmitRequestedAsync { get; set; } = default!;

    private string? _selectedId;
    private string? _initialId;
    private bool _isSubmitting;
    private bool _wasOpen;
    private string? _errorMessage;
    private IReadOnlyList<StageSquadViewModel>? _seenSquads;

    private bool CanSave => !_isSubmitting && _selectedId is not null && _selectedId != _initialId;

    protected override void OnParametersSet()
    {
        var opened = IsOpen && !_wasOpen;
        if (opened)
        {
            _isSubmitting = false;
            _errorMessage = null;
        }

        // Preselect the current squad whenever the page hands in a fresh list (it loads on every open).
        if (opened || !ReferenceEquals(_seenSquads, Squads))
        {
            _selectedId = Squads.FirstOrDefault(squad => squad.IsSelected)?.SquadId;
            _initialId = _selectedId;
            _seenSquads = Squads;
        }

        _wasOpen = IsOpen;
    }

    private Task HandleCloseAsync() => _isSubmitting ? Task.CompletedTask : OnClose.InvokeAsync();

    private async Task SubmitAsync()
    {
        if (!CanSave)
        {
            return;
        }

        _isSubmitting = true;
        _errorMessage = null;

        var result = await OnSubmitRequestedAsync(_selectedId!);

        _isSubmitting = false;

        if (result.IsError)
        {
            _errorMessage = result.FirstError.Description;
            return;
        }

        await OnClose.InvokeAsync();
    }
}
```

`StageSquadModal.razor.css`:

```css
.stage-squad__lead { margin: 0 0 1rem; color: var(--neba-text-muted); font-size: 0.875rem; }
.stage-squad__empty { margin: 0.25rem 0; color: var(--neba-text-muted); font-size: 0.875rem; max-width: 62ch; }
.stage-squad__list { display: grid; gap: 0.5rem; margin: 0; padding: 0; border: 0; }

.stage-squad__row {
    display: flex;
    gap: 0.75rem;
    align-items: flex-start;
    border: 1px solid var(--neba-border);
    border-radius: var(--neba-radius-lg);
    padding: 0.75rem 0.875rem;
    cursor: pointer;
}

.stage-squad__row:has(input:checked) {
    border-color: var(--neba-blue-600);
    background: color-mix(in srgb, var(--neba-blue-600) 12%, transparent);
}

.stage-squad__row input { margin-top: 0.25rem; }
.stage-squad__row b { display: block; font-weight: 600; }
.stage-squad__row small { color: var(--neba-text-muted); font-size: 0.8125rem; }
.stage-squad__actions { display: flex; justify-content: flex-end; gap: 0.5rem; flex-wrap: wrap; }
```

#### 13. Append to `src/Neba.Website.Server/Series/SeriesDetail.razor.css`

```css
.series-detail__retry {
    margin-top: 0.75rem;
}

/* Advancement section. The mockup's local tokens map onto the theme. Child components read these through inheritance.
   Pending is neutral, Computed primary, Official success, Stale and Needs squad warning, cancelled error.
   The foreground mixes keep text readable on the tinted background in both modes. */
.series-detail {
    --series-pending-bg: var(--neba-surface-high);
    --series-pending-fg: var(--neba-text-muted);
    --series-computed-bg: color-mix(in srgb, var(--neba-blue-600) 12%, transparent);
    --series-computed-fg: var(--neba-blue-600);
    --series-official-bg: color-mix(in srgb, var(--neba-success) 12%, transparent);
    --series-official-fg: color-mix(in srgb, var(--neba-success) 75%, black);
    --series-warn-bg: color-mix(in srgb, var(--neba-warning) 12%, transparent);
    --series-warn-fg: color-mix(in srgb, var(--neba-warning) 75%, black);
    --series-error-bg: color-mix(in srgb, var(--neba-accent-red) 10%, transparent);
    --series-error-fg: color-mix(in srgb, var(--neba-accent-red) 85%, black);
}

.dark .series-detail {
    --series-computed-fg: var(--neba-blue-300);
    --series-official-fg: color-mix(in srgb, var(--neba-success) 55%, white);
    --series-warn-fg: color-mix(in srgb, var(--neba-warning) 55%, white);
    --series-error-fg: color-mix(in srgb, var(--neba-accent-red) 45%, white);
}

.series-rail-loading {
    display: grid;
    gap: 0.875rem;
}

.series-rail {
    list-style: none;
    margin: 0;
    padding: 0 0 0 2.125rem;
    position: relative;
}

.series-rail::before {
    content: "";
    position: absolute;
    left: 0.6875rem;
    top: 0.875rem;
    bottom: 0.875rem;
    width: 2px;
    background: var(--neba-border);
}

.series-node {
    position: relative;
    margin-bottom: 0.875rem;
}

.series-node::before {
    content: "";
    position: absolute;
    left: -1.8125rem;
    top: 1.25rem;
    width: 0.875rem;
    height: 0.875rem;
    border-radius: 50%;
    background: var(--neba-bg-panel);
    border: 2px solid var(--neba-border);
}

.series-node--computed::before {
    background: var(--series-computed-fg);
    border-color: var(--series-computed-fg);
}

.series-node--official::before {
    background: var(--series-official-fg);
    border-color: var(--series-official-fg);
}

.series-node--cancelled::before {
    background: var(--series-error-fg);
    border-color: var(--series-error-fg);
}
```

#### 14. Edit `src/Neba.Website.Server/Series/EditSeries.razor` (full replacement)

```razor
@page "/series/{Id}/edit"
@using Neba.Api.Contracts.Security
@using Neba.Api.Contracts.Series
@using Neba.Website.Server.Services
@rendermode @(new InteractiveServerRenderMode(prerender: false))

@inject ApiExecutor ApiExecutor
@inject ISeriesApi SeriesApi

<PageTitle>Edit Series - BowlNEBA</PageTitle>

<AuthorizeView Policy="@Permissions.EditSeries.PolicyName" Context="authContext">
    <Authorized>
        <div class="neba-space-y-6">
            <div class="page-title-bar">
                <div class="page-title-inner">
                    <h1>Edit Series</h1>
                    <p>Set the Stages in order. Each Stage is one tournament's role in the Series.</p>
                </div>
                <div class="page-title-help"><HelpButton DocName="edit-series" Light="true" /></div>
            </div>

            @if (_loadError is not null)
            {
                <NebaAlert Severity="NotifySeverity.Error" Title="Unable to load Series" Message="@_loadError" />
            }
            else if (_model is null || _detail is null)
            {
                <NebaSkeletonLoader Type="SkeletonType.Card" />
            }
            else
            {
                @if (_cancelledCheckWarning is not null)
                {
                    <NebaAlert Severity="NotifySeverity.Warning" Title="Cancelled tournaments not checked" Message="@_cancelledCheckWarning" />
                }

                <SeriesEditor Model="@_model"
                              SeriesId="@Id"
                              SeasonLabel="@SeasonLabel"
                              FinaleHost="@_detail.Finale?.Host"
                              IsCreate="false"
                              IsLocked="@_detail.IsLocked" />
            }
        </div>
    </Authorized>
    <NotAuthorized>
        <p class="neba-content-wrapper">You don't have permission to edit this Series.</p>
    </NotAuthorized>
</AuthorizeView>

@code {
    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    private SeriesDetailViewModel? _detail;
    private SeriesFormModel? _model;
    private string? _loadError;
    private string? _cancelledCheckWarning;

    private string SeasonLabel => _detail is null ? string.Empty : _detail.SeasonDescription + " (" + _detail.SeasonDateRange + ")";

    protected override async Task OnInitializedAsync()
    {
        var result = await ApiExecutor.ExecuteAsync("SeriesApi", "GetSeries", ct => SeriesApi.GetSeriesAsync(Id, ct));

        if (result.IsError)
        {
            _loadError = result.FirstError.Type == ErrorOr.ErrorType.NotFound
                ? "This Series doesn't exist. It may have been deleted."
                : "Something went wrong while loading this Series. Try again later.";
            return;
        }

        _detail = result.Value.ToViewModel();
        _model = SeriesInputMapping.FromDetail(_detail);

        if (_detail.IsLocked)
        {
            await MarkRemovableStagesAsync();
        }
    }

    /// <summary>
    /// A locked Series fixes its Stages, except a Pending Stage whose tournament was cancelled. GetSeries carries no
    /// tournament status, so the advancement read tells which rows the grid may let go.
    /// </summary>
    private async Task MarkRemovableStagesAsync()
    {
        var result = await ApiExecutor.ExecuteAsync("SeriesApi", "GetSeriesAdvancement", ct => SeriesApi.GetSeriesAdvancementAsync(Id, ct));

        if (result.IsError)
        {
            _cancelledCheckWarning = "A Stage whose tournament was cancelled can't be removed until this check works. Reload the page to try again.";
            return;
        }

        var removable = result.Value.Stages
            .Where(stage => stage.Status == StageStatuses.Pending && stage.TournamentStatus == TournamentStatuses.Cancelled)
            .Select(stage => stage.StageId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var row in _model!.Stages)
        {
            row.RemovableWhileLocked = row.StageId is { } stageId && removable.Contains(stageId);
        }
    }
}
```

#### 15. Edit `src/Neba.Website.Server/Series/SeriesStageRowModel.cs`

Add this property after `StageId`:

```csharp
    /// <summary>
    /// A locked Series' Pending Stage whose tournament was cancelled: the grid still lets the admin remove it.
    /// Never sent to the API; the save just leaves the row out.
    /// </summary>
    public bool RemovableWhileLocked { get; set; }
```

#### 16. Edit `src/Neba.Website.Server/Series/SeriesStageGrid.razor`

In the `ReadOnly` branch, replace the trailing `<span></span>` with this:

```razor
                    @if (row.RemovableWhileLocked)
                    {
                        <div class="series-grid__actions">
                            <button type="button" class="neba-btn-icon" @onclick="@(() => Remove(i))" aria-label="@AriaLabel("remove", order)">✕</button>
                        </div>
                        <p class="series-grid__note">Tournament cancelled: remove this Stage.</p>
                    }
                    else
                    {
                        <span></span>
                    }
```

Guard `Remove` in `@code`:

```csharp
    private Task Remove(int index)
    {
        // A locked Series fixes its Stages; only a cancelled Pending Stage may go.
        if (ReadOnly && !Rows[index].RemovableWhileLocked)
        {
            return Task.CompletedTask;
        }

        Rows.RemoveAt(index);
        ClearErrors();
        return OnChanged.InvokeAsync();
    }
```

Append to `SeriesStageGrid.razor.css`:

```css
.series-grid__note {
    grid-column: 1 / -1;
    margin: 0;
    color: var(--neba-accent-red);
    font-size: 0.875rem;
}
```

#### 17. Edit `src/Neba.Website.Server/Series/SeriesEditor.razor`

Replace the locked alert:

```razor
            @if (IsLocked)
            {
                <NebaAlert Severity="NotifySeverity.Warning" Title="This Series is locked" Message="@SeriesDisplay.LockedMessage(HasRemovableStage)" />
            }
```

Add to `@code`:

```csharp
    private bool HasRemovableStage => Model.Stages.Any(row => row.RemovableWhileLocked);
```

`HandleSaveAsync` and `SeriesInputMapping` need no change. The save already sends the remaining rows, with their IDs, in order. A removed row simply goes missing from that list.

### Tensions with the Phase 1 API (no redesign; accepted workarounds)

1. The locked editor can't remove the cancelled Stage (see the companion change above). A cleaner fix is tournament status on `GetSeries`; the plan takes the companion change.
2. All 409 variants look the same to the page (`ApiExecutor` keeps no error code). The workaround above avoids parsing text. Surfacing the problem-details `code` in `ApiExecutor` would affect every caller, so it isn't assumed.
3. `CreatedBy` is an actor ID, not a name, so the overrides table shows the date and reason only. Audit.NET holds the who.
4. A Pending Stage gives no reason beyond `NeedsSquad` and `TournamentStatus` (it could also be waiting for the previous Stage or for results).
5. Vacant slots arrive as a count only. A Clear override row explains a cleared slot; a non-member vacancy is not explained.

### Tests (from `test-writer`)

Traps in the current repo:

1. Existing bUnit `SeriesDetailTests` fail under strict mocks once the page calls `GetSeriesAdvancementAsync`. Add a `SetupAdvancement` helper to every `EditSeries` case.
2. The existing admin e2e specs (`Series.spec.ts` "Series (admin)") break unless the mock answers the new GET for every Series `mockSeriesDetail` returns. Use an all-Pending fallback built from the detail's own Stages with `version: 0`.
3. The mock's `resolveSeriesRoute` (`startsWith('/series/')`) and the generic `PUT` and `DELETE /series/` handlers swallow the new paths. Put the new matchers before them.

bUnit groups (`tests/Neba.Website.Tests/Series/`; `[UnitTest]`, `[Component("Series")]`, strict mocks, `StubApiResponse<T>`, Shouldly):
1. Mapping (response to view model, flags, empty lists never null).
2. Status badge per status; a Placement Stage never shows Official.
3. Indicators alone and combined; Backfill date only on the last Qualifying Stage.
4. Qualifying output (entries, Backfill rows with points, override marker, vacant slots, override list with inactive marker, read-only for a Manager).
5. Survivor output ("No full block", empty state).
6. Placement output (finalist column with and without a Finale).
7. Load and gate (anonymous and logged-in without `EditSeries` make no call; Manager and Admin call once; loading, 404 and failure with Retry; empty Stage list).
8. Override controls gated to Admin; hidden on Official, Survivor and Placement Stages.
9. Add bowler dialog (loads results once, `NebaAutocomplete`, sends kind 1 with trimmed reason and the page's `Version`, blank and over-500 reasons blocked, cancel makes no call, dirty guard).
10. Clear slot and Remove override (kind 2; `RemoveStageOverrideAsync` with the `Version`).
11. Pick squad dialog (Admin and Manager; squads from `ListStageSquadsAsync`; preselect; empty and failed states).
12. Recompute dialog (plain run when no official Stage in range, `ConfirmResetOfficial == false`; confirm listing official Stages and `true`; cancel makes no call).
13. Write lifecycle (2xx reloads once and the next write sends the new `Version`; 409 reload and toast; Recompute 409 reopens confirm; 422 stays in the modal; double submit ignored). Check how `ApiExecutor` reads ProblemDetails from a stub before writing.
14. Stale view (two writes use the `Version` from the reload).
15. Existing `SeriesDetailTests` repair, plus a Manager-versus-Admin control set case.
16. Locked-editor removal (`SeriesStageGridTests`, `SeriesEditorTests`).
17. `SeriesDisplayTests` for the new labels.

Playwright (`tests/e2e/Series.spec.ts`, only real browser plus HTTP flows):
- Manager (`permissions=Series.EditSeries`): badges, indicators, Backfill date, an expanded Qualifying output; no Add, Clear or Remove controls; Pick squad and Recompute visible.
- Admin (`permissions=Series.EditSeries,Series.ManageAdvancement`): add an override (assert the body through `/__mock/last-request`), remove it.
- Recompute confirm lists official Stage orders; confirming sends `confirmResetOfficial: true`; cancel sends nothing.
- Pick squad clears "Needs squad".
- Failure: 409 message and reload; 422 message under the dialog (4xx only, no 5xx).
- Anonymous: unchanged page, no advancement request.

Mock changes (`tests/e2e/mock-api/mock-api-server.ts`):
- Constants for per-project Series ids (states 520, override 521, squad 522, recompute 523, failure 524, plus 525 if the Placement variants need their own), using `perProjectTournamentId` because the three projects share one mock. None go in the `/series` list.
- State map keyed by Series id with a `version` that each write bumps, and `POST /__mock/series-advancement/reset?seriesId=` for `beforeEach`.
- Routes before the generic handlers: GET advancement (unknown Series 404; known without a fixture gets the all-Pending fallback), GET stage results and squads, POST recomputations (409 naming official orders unless `confirmResetOfficial`), PUT squad, POST overrides (`201 { overrideId, version }`), DELETE override (`version` from the query). Writes call `sendMockOverrideErrorIfSet` first, and a mismatched `version` returns 409.
- Fixture states: Pending, Computed with a fixed 2030 `officialDueAt`, Official, Stale, last Qualifying with Backfill date and Backfill entries, Needs squad, Computed Survivor with outcomes, cancelled-tournament Pending, an override marker, Placement with and without finalists.
- Every `required` Contracts property needs its mock key (`stageId`, `order`, `kind`, `status`, `isStale`, `needsSquad`, `tournamentId`, `tournamentStatus`, `entries`, `overrides`, `survivorOutcomes`, and `version`, `targetField`, `stages`, `positions` on the response), or Playwright fails the same way every time. Enum values go out as names, `DateOnly` as `YYYY-MM-DD`, `TimeOnly` as `HH:mm:ss`. Do not touch `MOCK_UPCOMING_TOURNAMENTS`, and do not copy the existing `status: 'Completed'` on `mockSeriesStages`.

Factories (`Neba.TestFactory`, `Create()` persistable with no args, `Bogus(count, seed)` with `Ulid.BogusString`): Response factories for the advancement response, Stage, field entry, override, survivor outcome, position, created override, stage result and stage squad; matching internal ViewModel factories. Rules in `Bogus`: Computed implies `ComputedAt`; Official implies both timestamps and null `OfficialDueAt`; Pending implies no timestamps and empty output; a Placement Stage is never Official; `NeedsSquad` only on Survivor; a Backfill entry has null source and place and `ViaOverride` false.

### Docs and hand-offs

- Help doc `docs/help/series-advancement.md` through `help-documentation` (needs screenshots from a running stack: `aspire` agent, `aspire start --isolated`) and the `Series.ManageAdvancement` policy row ("the Series page shows the override actions only to holders").
- Check `CollectionResponse<T>`'s item property name, the e2e admin login helper for Series, and the `NebaAlert` and `NebaSkeletonLoader` parameters when writing the code.
- Nothing was compiled or run.

### Mockups

Static HTML in `docs/plans/mockups/series-advancement/`, built on the phase 1 styling (`../series-management/mockup.css`, site tokens, Inter and Manrope). They use invented bowlers on the 2025 Stage layout. The funnel chart and Finale block from phase 1 are unchanged and left out. Open any file in a browser. The top bar switches role (Admin or Manager), scenario (season under way, a Survivor round that needs a squad, all Stages computed), a loading state, and a "next save fails (422)" test. Shared files: `advancement.css`, `advancement-data.js`, `advancement-ui.js`.

- `series-advancement-option-1.html`: **expanding rows.** The phase 1 grouped lists with a status pill, flags, actions and a chevron on each row. The output opens under its row. Not chosen. Closest to phase 1 and the smallest change.
- `series-advancement-option-2.html`: **season rail.** Every Stage in season order on one rail, with a strip of 11 segments up top showing pending, computed and official. Each card shows "7 of 8 slots filled". **Chosen (2026-11-20), for `Series.EditSeries` holders only.** Reads the season as a path.
- `series-advancement-option-3.html`: **list and detail.** A compact Stage list beside one Stage's full output, so a Stage opens without moving the list. Not chosen. Stacks on a phone.
- `override-modal.html`: **Add bowler and Clear slot** (single design, Admin only). Bowler picker, required reason with a 500-character count, a discard guard when you press Escape with text typed, and the 422 error.
- `squad-picker-modal.html`: **Pick squad** (single design). Radio list of squads with "Has scores", "Current" and the empty state. The recompute confirm (it lists official Stages that reset) is in all three page options through the Recompute from here action.

## Commit plan

Rules for every commit: it builds and passes its own tests; the code and its tests land together (the code's agent implements, `test-writer` writes the tests); stage files by name, never `git add -A`; subjects are short, imperative, no prefix; the message body says why, not what; every message ends with `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`. The work branches from `release/v1.3.0` and the PR targets it. `<Version>` stays `1.3.0`, so there is no version commit. If the work drifts, update this list rather than merging commits. Group numbers refer to the Phase 1 and Phase 2 test lists.

Docs and the rename

1. **Add the Series advancement plan and mockups.** Agent: main session. Files: `docs/plans/series-advancement.md`, `docs/plans/mockups/series-advancement/*`. Why: the plan is the source for every commit below.
2. **Update the Series language for advancement.** Agent: `docs`. Files: `docs/ubiquitous-language.md`. Why: it records the rules and the word "Official" before the code uses them.
3. **Rename the Stage status Announced to Official.** Agent: `backend-api`; `test-writer` updates the tests. Files: `Features/Series/Domain/StageStatus.cs` (and XML text), every use in `src/` and `tests/`, `Series2025ReferenceFactory` if it names it, `tests/e2e/mock-api/mock-api-server.ts` if the mock names it. Why: later commits (check constraints, jobs, UI) use the final word, and the rename stays reviewable on its own.

Schema and domain

4. **Add the Series advancement tables and the Version token.** Agent: `efcore`; `test-writer` for groups 15. Files: new `SeriesFieldEntry`, `SeriesPosition`, `SurvivorOutcome`, `StageOverride`, `OverrideKind` and the two ID types (shape only), property additions on `Series.cs` and `SeriesStage.cs`, `SeriesConfiguration.cs`, `SeriesStageConfiguration.cs`, four new configuration classes, `AppDbContext.cs`, migration `AddSeriesAdvancement` with snapshot, migration and configuration tests, the bowler foreign key inventory test. Why: the schema is additive and needs to land and be reviewed alone. Check staging and production `series_stages.status = 0` before it ships.
5. **Add the four advancement computations.** Agent: `backend-api`; `test-writer` for group 7. Files: `QualifyingComputation`, `BackfillComputation`, `SurvivorComputation`, `PlacementComputation`, `AdvancementFacts`, `StageComputation`, factories for the facts. Why: the formulas are pure functions, so they get reviewed and tested without the aggregate.
6. **Add advancement to the Series aggregate.** Agent: `backend-api`; `test-writer` for group 6. Files: `Series.Advancement.cs`, `Series.cs` and `SeriesStage.cs` methods and `Version` bumps, `SquadDesignation` resolve and unresolve, `StageOverride` and field entry `Create` factories, `SeriesAdvancementErrors`, `StageTournamentFacts` status, `SeriesStage.Update` keeping `SquadId`, cancelled-Stage removal in `ReplaceConfiguration`, the matching test factories. Why: the state machine, overrides and cascade are one idea and depend on commits 4 and 5.
7. **Add the advancement readers.** Agent: `efcore`; `test-writer` for groups 3, 4 and 5. Files: `SeriesAdvancementReader`, `StageResultRowsReader`, `StageSquadsReader`, `MembersOnDateReader`, `OpenBowlerOfTheYearPointsReader`, `EasternTime`. Why: the reads are reviewed apart from the writes, and the BOY parity test proves the points formula early.

Orchestration and jobs

8. **Add the per-Series lock and the advancement run.** Agent: `backend-api`; `test-writer` for groups 8 and 9. Files: `IKeyedLockProvider`, `HangfireKeyedLockProvider` and its registration, `SeriesAdvancement`, `SeriesAdvancementFactsReader`, `SeriesConcurrency`, the `EditSeriesCommandHandler` and `EditSeriesLogoCommandHandler` edits. Why: every trigger and admin write goes through this one entry point, so it lands before its callers.
9. **Make computed Stages Official after a grace period.** Agent: `backend-api`; `test-writer` for group 11. Files: `MakeDueStagesOfficial` job and handler, `SeriesAdvancementSettings`, `SeriesFeatureConfiguration.AddSeries`, `UseSeriesMakeOfficialJob`, the `InfrastructureConfiguration` call, `appsettings.json`. Why: the scheduled job and its setting are one idea.
10. **Add the domain event publisher.** Agent: `backend-api`; `test-writer`. Files: `IDomainEventPublisher`, `DomainEventPublisher`, `DomainEventDispatcherInterceptor` (now calls the publisher), the four event records, the registration, serialization round-trip tests, the architecture test that only the publisher enqueues an event job. Why: it is the #56 seam, and nothing raises these events yet.
11. **Advance Series from tournament, squad and stats events.** Agent: `backend-api`; `test-writer` for groups 10 and 12. Files: `AdvanceOnResultsRecorded`, `AdvanceOnSquadsChanged`, `AdvanceOnSquadScoresReplaced`, `BackfillOnSeasonStatsGenerated`. Why: the handlers exist before anything raises their events.
12. **Raise the tournament and stats events.** Agent: `backend-api`; `test-writer` for groups 1 and 2. Files: `Tournament.cs` (`MarkResultsRecorded`, squad changes), `SyncTournamentResultsJob`, `SyncSquadScores`, `GenerateSeasonStatsJob`, the one-handler-per-event architecture test. Why: events are raised last so no job is queued without a handler.

API surface

13. **Add the advancement contracts and the Manage advancement permission.** Agent: `backend-api`; `test-writer` for the seeder test. Files: `Neba.Api.Contracts/Series/*` request and response types, `OverrideKindValues`, `ISeriesApi` methods, `Permission.cs`, `docs/architecture/permission-based-security.md`, the `docs/policies/README.md` row (`policy-documentation` skill). Why: the contract and the permission are reviewed before any endpoint uses them, and the UI needs the Refit methods.
14. **Add the advancement read endpoints.** Agent: `backend-api`; `test-writer` for the GET parts of group 14. Files: `GetSeriesAdvancement`, `ListStageResults`, `ListStageSquads` (endpoint, summary, query, handler, validator, DTO each), Verify snapshots, the 401, 403 and 404 tests. Why: reads carry no write risk and unblock the UI.
15. **Add the advancement write endpoints.** Agent: `backend-api`; `test-writer` for groups 12 (Pick squad), 13 and the write parts of 14. Files: `RecomputeSeriesAdvancement`, `PickStageSquad`, `CreateStageOverride`, `RemoveStageOverride`, `SeriesRequestRules`, the result sender changes. Why: the writes carry the `Version` and confirmation rules and are reviewed together.
16. **Add the synthetic 2025 replay test.** Agent: `test-writer`. Files: `Series2025ReferenceFactory.CreateWithResults`, the expected-output fixture, the replay integration test through Recompute from the first Stage, with and without a Finale. Why: it proves the whole chain once the API is complete.
17. **Record the domain event decisions.** Agent: `docs`. Files: `docs/learnings/backend.md`. Why: one handler per event, serialization, the publisher seam, the missing outbox (#56) and the keyed lock need to be findable.

UI

18. **Add the advancement response factories.** Agent: `test-writer`. Files: `Neba.TestFactory` response factories for the advancement response, Stage, field entry, override, survivor outcome, position, created override, stage result and stage squad. Why: UI tests and the e2e mock use them, and they depend only on commit 13.
19. **Mock the advancement API for e2e.** Agent: `e2e`. Files: `tests/e2e/mock-api/mock-api-server.ts` (routes before the generic `/series/` handlers, fixture Series 520 to 525, the reset route, every `required` field, one Cancelled fixture), repairs to the existing admin Series specs. Why: the page calls the new GET, so the mock must answer first or the existing admin specs fail.
20. **Add the advancement view models, mapping and display text.** Agent: `blazor-ui`; `test-writer` for groups 1 and 17. Files: `StageStatuses`, `SeriesAdvancementViewModel`, `StageOverrideFormModel`, `SeriesMappingExtensions`, `SeriesDisplay`, the ViewModel factories. Why: pure code with no markup, tested alone.
21. **Show the season rail on the Series page.** Agent: `blazor-ui`; `test-writer` for groups 2 to 7 and the `SeriesDetailTests` repair. Files: `SeriesDetail.razor`, `.razor.cs` and `.razor.css` (load, permission check, read-only), `SeriesProgressStrip`, `SeriesStageCard`, `SeriesStageOutput` and their CSS. Why: holders see status and output with the actions hidden, so the rail is reviewed before any write. Trim the draft's action buttons out of this commit and add them in 22 and 23.
22. **Add Recompute and Pick squad to the Series page.** Agent: `blazor-ui`; `test-writer` for groups 11 to 14. Files: `StageSquadModal`, the recompute confirm wiring, the page write lifecycle (`Version`, 409, 422, `_isWriting`). Why: both are `Series.EditSeries` actions and share the write lifecycle.
23. **Add the override controls to the Series page.** Agent: `blazor-ui`; `test-writer` for groups 8 to 10. Files: `StageOverrideModal`, Add bowler, Clear slot, Remove override with its confirm, the dirty guard. Why: the Admin-only controls are the riskiest UI and get their own review.
24. **Let admins remove a cancelled Stage from a locked Series.** Agent: `blazor-ui`; `test-writer` for group 16. Files: `EditSeries.razor`, `SeriesEditor.razor`, `SeriesStageGrid.razor`, `SeriesStageRowModel.cs`. Why: it completes the cancelled-tournament path from phase 1's lock rule.
25. **Add Playwright specs for Series advancement.** Agent: `e2e`. Files: `tests/e2e/Series.spec.ts` (Manager, Admin, recompute confirm, Pick squad, failures, anonymous). Why: it checks the real browser plus HTTP flows once the UI is complete.
26. **Add the Series advancement help doc.** Agent: `docs`, with `aspire` for a running stack. Files: `docs/help/series-advancement.md` and screenshots. Why: it needs the finished UI. It uses the `help-documentation` skill.

The full plan is in `docs/plans/series-advancement.md`.
