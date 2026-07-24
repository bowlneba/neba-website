# Edit Tournament

Lets staff edit an existing tournament's fields after creation — name, type, dates, venue, entry fee/added money, registration URL, logo, and oil pattern/lane-condition categories — mirroring the Edit Sponsor / Edit Article pattern already in the app.

## Decisions locked in during scoping

- **Season re-derivation**: editing a tournament's Start/End Date re-derives `SeasonId` the same way `CreateTournamentCommandHandler` does — the handler looks up the `Season` containing the new dates and updates `SeasonId` accordingly. If no season contains the new dates, the save fails with the same `NoSeasonForDates` error `CreateTournament` already uses. This means moving a tournament's dates can silently move it onto a different season's schedule — accepted tradeoff for consistency with Create.
- **Edit form data source**: the existing `GetTournamentEndpoint` (`GET /tournaments/{id}`, `AllowAnonymous`) is extended with additional raw/editable fields, rather than adding a second dedicated endpoint. The new fields (bowling center certification number, raw logo storage fields, and — see below — nothing new needed for oil pattern) are only populated when the caller holds a `TournamentManagementPermissions` permission (same gating `OilPatternRevealDateTime` already uses for `CallerIsAuthenticated`), so the public response shape for anonymous/non-admin callers is unchanged.
- **Oil pattern re-selection on edit**: `Tournament` never persists *which* `OilPatternId` produced its current `PatternLengthCategory`/`PatternRatioCategory` — `CreateTournamentCommandHandler` only stores the two derived category enums, per the create-tournament plan (`OilPattern.Create`'s categories are transient at creation time, not a stored FK). So the edit form pre-fills the current category values as plain selects (manual-entry style) and offers the same `OilPatternPicker` as Create to *override* them by picking/creating a pattern — there's no "this came from Pattern X, re-show it selected" behavior, since that information was never kept. This matches Create's own behavior (`OilPatternSelection` is ephemeral UI state, not round-tripped).
- **Parity with Create's current behavior** (discovered while implementing — `CreateTournamentCommandHandler` had grown two things beyond what the older create-tournament.md code sample shows, from the since-shipped oil-pattern-reveal-date plan): `EditTournamentCommandHandler` also calls `tournament.AddOilPattern(oilPatternId, Qualifying, MatchPlay)` when an `OilPatternId` is submitted (same hardcoded rounds Create uses), and schedules an `EvictOilPatternRevealCacheJob` when a future `OilPatternRevealDateTime` is submitted — both for consistency with Create, so editing a tournament's oil pattern/reveal date behaves the same as setting it at creation.
  - **Known gap, not fixed here**: `Tournament.AddOilPattern` is additive/idempotent for a given `OilPatternId` — it doesn't support *replacing* which pattern is associated. If a tournament is edited more than once with a *different* `OilPatternId` each time, `TournamentOilPatterns` accumulates multiple entries (all still showing in the public "Oil Patterns" section) rather than reflecting only the latest selection. This is the same underlying "no persisted current-pattern FK" gap noted above, just surfacing differently on repeated edits vs. a single create. Left as-is for this plan; a proper fix (e.g. a `Tournament.ReplaceOilPattern(...)` method) belongs with the future lifecycle-invariants work below, not bolted on here.
- **Permission**: new `Tournaments.EditTournament` permission, added to the existing `TournamentManagementPermissions` collection (`CanManageTournaments` OR-policy already exists and picks this up automatically — no `PolicyExtensions.cs` change needed).
- **New `Tournament.Update(...)` aggregate method**: mirrors `Sponsor.Update(...)` — full-replace signature covering every editable field, re-validates the same invariants `Create` enforces (name required, date ordering, non-negative entry fee/added money).

## Phase 1: API

### Domain

- **`Tournament.cs`** (edit) — add `Update(...)`, mirroring `Sponsor.Update`'s shape: takes every editable field as a parameter (name, tournament type, dates, seasonId — passed in already resolved by the handler, statsEligible, entryFee, nebaAddedMoney, bowlingCenterId, externalRegistrationUrl, logo, patternLengthCategory, patternRatioCategory, oilPatternRevealDateTime), re-runs `Create`'s validation rules (name required, date ordering, non-negative entryFee/nebaAddedMoney), assigns all properties, returns `ErrorOr<Updated>`. `SeasonId` is passed in as an already-resolved value (the handler does the date→season lookup, same "aggregate invariants requiring cross-aggregate data" pattern `CreateTournamentCommandHandler` already follows) — the aggregate doesn't do its own season lookup.
- **`TournamentErrors.cs`** — no new errors; `NameRequired`, `EndDateBeforeStartDate`, `NoSeasonForDates`, `OilPatternNotFound`, `BowlingCenterNotFound`, `InvalidEntryFee`, `InvalidNebaAddedMoney` are all reused as-is by the edit path.

### Database

No migration needed — same columns Create already writes to.

### Application — `EditTournament/`

- **`EditTournamentCommand.cs`** — `ICommand<Updated>`, same field set as `CreateTournamentCommand` plus `TournamentId`, minus nothing (every Create field is editable).
- **`EditTournamentCommandHandler.cs`** — loads the tournament by ID (404 if missing), re-derives `Season` from the submitted dates (same lookup `CreateTournamentCommandHandler` does), validates `BowlingCenterId`/`OilPatternId` the same way Create does, snapshots `tournament.Logo` before calling `Update(...)` (needed to detect a logo swap afterward, same as `EditSponsorCommandHandler`), calls `Tournament.Update(...)`, cleans up any claimed pending upload for the new logo (`TournamentPendingUploadCleaner.RemoveClaimedAsync`), saves, evicts the `neba:tournaments:{id}` and `neba:tournaments:{oldSeasonId}`/`neba:tournaments:{newSeasonId}` cache tags (both season tags if the season actually changed), and enqueues a new `DeleteTournamentFilesJob` if the logo was replaced/removed (old logo present and different from the new one).
- **New `DeleteTournamentFilesJob.cs` / `DeleteTournamentFilesJobHandler.cs`** — mirrors `DeleteSponsorFilesJob`/`DeleteSponsorFilesJobHandler` file-for-file, scoped to tournament logo cleanup.
- **`EditTournamentEndpoint.cs`** — `PUT /tournaments/{id}`, `Policies(PermissionCatalog.EditTournament.PolicyName)`, 204 on success, 404 tournament-not-found, 409/422 split via a new `TournamentMutationResultSender`-style shared sender (reuse the existing one from the tournament-sponsors plan if already implemented) for domain/season/bowling-center/oil-pattern errors.
- **`EditTournamentRequestValidator.cs`** — same structural rules as `CreateTournamentRequestValidator` (name required/length, tournament type known+active, end date ≥ start date, entry fee/added money ≥ 0, external URL absolute, pattern category names known, oil-pattern-vs-manual-categories mutual exclusivity).
- **`EditTournamentSummary.cs`** — same shape as `EditSponsorSummary`.

### Authorization

- **`Permission.cs`** (edit) — inside the existing `#region Tournaments`, add:
  ```csharp
  public static readonly Permissions EditTournament = new("Tournaments.EditTournament", "Edit Tournament");
  ```
  and add `EditTournament` to `TournamentManagementPermissions`. No `PolicyExtensions.cs` change — `CanManageTournamentsPolicyName` already `RequireAssertion`s against the whole collection.
- `docs/policies/README.md` needs no new row — generic dynamic `Permission:{value}` row already documents this, same as `CreateTournament`.

### Contracts (`src/Neba.Api.Contracts/Tournaments/`)

- **`EditTournament/EditTournamentRequest.cs`** / **`EditTournamentInput.cs`** — same shape as `CreateTournament`'s `TournamentInput`, wrapped with the tournament's ID (same `{ Id, Tournament }` shape `EditSponsorRequest` uses).
- **`GetTournament/TournamentDetailResponse.cs`** (edit, additive only) — add fields populated only when the caller has tournament-management permission: `BowlingCenterCertificationNumber` (on `TournamentDetailBowlingCenterResponse`), `LogoContainer`/`LogoPath`/`LogoContentType`/`LogoSizeInBytes` (raw logo storage fields, alongside the existing computed `LogoUrl`). Same additive treatment as `TournamentDetailSponsorResponse` picking up `TitleSponsor`/`SponsorshipAmount` in the tournament-sponsors plan.
- **`Features/Tournaments/GetTournament/TournamentDetailDto.cs`/`TournamentDetailBowlingCenterDto.cs`** (edit) — same additive fields at the DTO layer; `GetTournamentQueryHandler`'s projection adds `tournament.BowlingCenterId` (only surfaced when `query.CallerHasTournamentManagementPermission`) and the raw `tournament.Logo` fields.
- **`ITournamentsApi.cs`** — new `EditTournamentAsync(string id, EditTournamentRequest request, ...)` method (`[Put("/tournaments/{id}")]`), same pattern as `AddTournamentSponsorAsync`.

### Tests

- `TournamentTests` — `Update` unit tests: success path (all fields assigned), name-required error, end-before-start error, negative entry fee/added money errors — mirroring existing `Create` tests plus `Sponsor.Update`'s test shape.
- `EditTournamentCommandHandlerTests` — success path (including a season change when dates move to a different season), tournament-not-found (404/`NotFound`), no-season-for-dates, bowling-center-not-found, oil-pattern-not-found, cache tags evicted (old+new season when different, same tag once if unchanged), `DeleteTournamentFilesJob` enqueued only when the logo actually changed.
- Endpoint `Configure`/`HandleAsync` tests for `EditTournamentEndpoint`, using `Factory.Create<TEndpoint>()` and the existing FastEndpoints Stryker `ignore-methods`.
- `GetTournamentQueryHandlerTests` — existing tests updated/extended to assert the new admin-gated fields are populated when `CallerHasTournamentManagementPermission` is true and absent/null otherwise.
- No new test factory needed — `TournamentFactory` already exists.

## Future work — field-level edit restrictions once results entry exists

This plan makes every `Tournament` field editable unconditionally. That's only correct because no results-entry
workflow (check-ins, scores, standings) exists yet. Once tournaments actually run through the app, some fields
will need to become locked or restricted depending on tournament lifecycle state — e.g. once bowlers have
checked in or scores exist, changing `TournamentType`, dates, or removing the bowling center could invalidate
already-recorded data; once a tournament is complete, most fields likely shouldn't change at all. Some fields
(e.g. logo, external registration URL, promotional/added-money fields) may reasonably stay editable at any
stage.

**Do not implement any of this now.** When check-in/scoring/results-entry work begins, revisit `EditTournament`
and design the actual invariants then (what counts as "started," what's locked at each stage, whether this
becomes a state check on `Tournament.Update(...)` itself or a separate guard in the handler). Leaving this here
as a flag so that work doesn't get missed.

## Phase 2: UI

*(Not yet drafted.)*
