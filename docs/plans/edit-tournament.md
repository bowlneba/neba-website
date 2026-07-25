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

### Pages

- **New `Tournaments/EditTournament.razor`** (`@page "/tournaments/{Id}/edit"`) — mirrors `CreateTournament.razor`'s form shape field-for-field (Basic Info, Venue & Entry Fee, Oil Pattern, Logo sections), but:
  - Loads the tournament via `TournamentsApi.GetTournamentAsync(Id, ct)` in `OnInitializedAsync` (same call `TournamentDetail.razor` already makes) and populates the form model from the response's admin-gated raw fields (`BowlingCenter.CertificationNumber`, `LogoContainer`/`LogoPath`/`LogoContentType`/`LogoSizeInBytes`, `PatternLengthCategory`/`PatternRatioCategory`, `OilPatternRevealDateTime`, etc.) — these are only populated for a caller with `Tournaments.EditTournament`/any tournament-management permission, which is guaranteed here since the page itself is gated by that same permission.
  - Shows a loading skeleton / not-found / load-error state, same three-way branch `EditSponsor.razor` uses (`_isLoading`, `_notFound`, load-error message).
  - Submits via `TournamentsApi.EditTournamentAsync(Id, request, ct)` (`PUT`, bodyless response through `ApiExecutor`) instead of `CreateTournamentAsync`.
  - On save, navigates back to `/tournaments/{Id}` (the detail page) instead of a newly-created ID.
  - Current logo (if any) renders with a "Remove current logo" button above the `FileUpload`, same pattern as `EditSponsor.razor`'s Logo section — `FileUpload` replaces it; clearing removes it entirely (`Logo: null` on submit).

### Components

- **`Tournaments/OilPatternPicker.razor`** (edit, additive) — add two optional parameters, `InitialPatternLengthCategory`/`InitialPatternRatioCategory` (`string?`), seeded into `_manualLengthCategory`/`_manualRatioCategory` the first time the component renders with a non-empty value, using the same one-shot "don't clobber local interaction" guard already used for the `Patterns` parameter (`_hasLocalPatternAdditions`) — call it `_hasBeenSeeded` or reuse a similar flag. This is needed because `Tournament` never persists which `OilPatternId` produced its current categories (see "Decisions locked in during scoping" above), so Edit can only pre-fill the plain category values, not re-select a picked pattern. `CreateTournament.razor` passes nothing for these two new parameters (its default of empty stays exactly as today); `EditTournament.razor` passes the loaded tournament's current category names.

### View Models / Form Model

- No new response/DTO types — `EditTournament.razor` reads directly from `TournamentDetailResponse` (already extended in Phase 1) into a private `EditTournamentFormModel` (same shape as `CreateTournamentFormModel` in `CreateTournament.razor`, plus `[Required]`/`[Range]`/`[Url]` annotations reused as-is).

### Page wiring — `Tournaments/Detail/TournamentDetail.razor`

Add an "Edit Tournament" entry point gated by the new permission, same `AuthorizeView`/`<a>` pattern `SponsorDetail.razor` already uses for `Permissions.EditSponsor`:

```razor
<AuthorizeView Policy="@Permissions.EditTournament.PolicyName">
    <Authorized>
        <a href="/tournaments/@Id/edit" class="neba-btn neba-btn-secondary">
            <span class="material-symbols-outlined">edit</span>
            Edit Tournament
        </a>
    </Authorized>
</AuthorizeView>
```

Placed in the hero content area near the title (not conditioned on `IsUpcoming`/`RegistrationUrl` — unlike the Register CTA, editing should be available regardless of tournament status). Exact placement confirmed in the mockup (Step 7).

### State / Dirty-Tracking

Same as `CreateTournament.razor`: `EditContext` created over the form model in the constructor, `OnFieldChanged` → `MarkDirty()`, `<DirtyFormGuard IsDirty="@_isDirty" />` wraps the form, reset `_isDirty = false` right before the post-save `NavigateTo`. `OilPatternPicker`'s `OnDirty` callback and the logo `FileUpload`'s `OnFileUploaded`/`OnFileRemoved` callbacks call `MarkDirty()` directly, same as Create.

### `<PageTitle>` / Render Mode

`<PageTitle>@("Edit " + (_model?.Name ?? "Tournament") + " - BowlNEBA")</PageTitle>`, same conditional-name pattern `EditSponsor.razor` uses. `@rendermode @(new InteractiveServerRenderMode(prerender: false))` — this page loads async data before rendering the form, same as `EditSponsor.razor`, not `CreateTournament.razor` (which has no data to load and uses plain `@rendermode InteractiveServer`).

### FAB / List-Page Entry Point

Not applicable — this isn't a creatable list page; the entry point is the "Edit Tournament" link on the existing Tournament Detail page (added above), matching how Sponsors' edit flow works.

### API Client

`ITournamentsApi.GetTournamentAsync`/`EditTournamentAsync` (both already exist from Phase 1 / the existing detail endpoint) are injected directly into `EditTournament.razor` — no separate Website-side service wrapper, consistent with `CreateTournament.razor`'s direct `ITournamentsApi` injection.

### Tests

- **bUnit** (`Neba.Website.Tests`) — `EditTournament` component tests: populates form fields from a loaded `TournamentDetailResponse` (including admin-gated fields), submits and calls `EditTournamentAsync` with the expected request shape, handles not-found/load-error states, current-logo remove/replace flow, dirty-guard marks dirty on field change and clears on successful save. `OilPatternPicker` tests: new cases for `InitialPatternLengthCategory`/`InitialPatternRatioCategory` pre-filling the manual selects on first render without clobbering a later parameter update once the user has interacted (mirrors the existing `Patterns`-seeding test, if one exists — add if not).
- **Playwright** (`tests/e2e/`) — one flow: sign in as a user with `Tournaments.EditTournament`, open a tournament's detail page, click "Edit Tournament," change a field (e.g. name or entry fee), save, and confirm the change reflects on the detail page. A second, short test confirms the "Edit Tournament" link isn't rendered for a user without the permission.
- No new test factories needed — `TournamentDetailResponseFactory`/`TournamentInputFactory`/`EditTournamentInputFactory` (from Phase 1) already exist; extend `TournamentDetailResponseFactory.Create()` (if it doesn't already) to accept the admin-gated raw fields as nullable params, defaulting per the "Create() must produce a valid instance" convention.

### Mockups

- `docs/plans/mockups/edit-tournament/edit-tournament.html` — single data-capture mockup (no real layout tradeoff to weigh, per the same treatment as `CreateTournament.razor`'s own mockup). Reuses the app's actual theme tokens/classes from `neba_theme.css`/`app.css` (colors, `.neba-card`/`.neba-input`/`.neba-select`/`.neba-btn`/`.neba-segmented-control`/`.neba-badge`, the gradient `page-title-bar`) rather than inventing new styling — this is an admin form inside an existing app, not a new visual identity. Shows:
  - The same section layout as `CreateTournament.razor` (Basic Info, Venue & Entry Fee, Oil Pattern, Logo), pre-filled with a realistic edited tournament ("NEBA Fall Classic").
  - A small "Unsaved changes" pill in the title bar that appears once any field changes — a mockup-only stand-in for `DirtyFormGuard`'s real behavior (the actual guard intercepts navigation; it has no persistent visible pill in the real app, so **do not carry this pill into the real implementation** — it's here purely so the mockup can demonstrate the dirty-state concept interactively).
  - The current logo shown with a thumbnail, filename/size, and a "Remove current logo" text action above the upload dropzone, matching `EditSponsor.razor`'s pattern.
  - The Oil Pattern section's "No Pattern" mode pre-selected with the tournament's existing categories (Long / Sport) and an inline note explaining why they show as plain values rather than a re-selected pattern (the "no persisted current-pattern FK" gap noted above) — switching to "Pick Existing" or "Create New" via the segmented control is simulated with inline JS to demonstrate the mode-switch interaction.
