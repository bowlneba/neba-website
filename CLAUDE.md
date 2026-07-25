# Code Standards

Before implementing or reviewing code, read `.github/instructions/pull-request-review.instructions.md` for PR review guidelines that apply to all code in this repository.

For detailed architectural context:

- Backend: `docs/architecture/backend.md` (or wherever you put ARCHITECTURE.md)
- Blazor: `docs/architecture/blazor.md`

## Self-Maintenance

This file is a **living document** and should be kept current as the project evolves. Both Claude and GitHub Copilot can leverage these learnings to provide better assistance.

When you discover something important during a session, update this file to capture:

- **Learnings**: Project-specific patterns, conventions, or gotchas discovered during work
- **Common fixes**: Solutions to recurring issues or errors
- **Preferences**: User workflow preferences expressed during conversations

Before ending a session where significant discoveries were made, consider whether they should be documented here for future reference.

## Architecture Rules

### Feature Boundaries

- Feature domain folders (`Features/Bowlers/Domain`, `Features/Tournaments/Domain`, etc.) must NOT cross-reference each other's domain objects (aggregates, entities, value objects, domain services). Exception: importing a strongly-typed ID from another feature's domain (e.g., `BowlerId` from `Neba.Api.Features.Bowlers.Domain` in `HallOfFame`) is allowed — it's a typed foreign key, not a domain dependency.
- Commands return `ErrorOr<T>`, never throw for business rules
- Queries return DTOs, never domain entities
- Validators handle structural validation only (no DB lookups, no business rules)
- Use `Error.Validation` (422) when the input itself is wrong; use `Error.Conflict` (409) when the input is valid but the system's current state prevents the operation. Retry test: if the caller could resend the exact same payload and succeed after a state change, it's `Conflict`.
- Methods returning collections — whether directly (`List<T>`, `IEnumerable<T>`, etc.) or wrapped (`Task<List<T>>`) — must never return `null`. Return an empty collection instead. Nullable collection return types (`List<T>?`, `IEnumerable<T>?`, etc.) are not permitted unless there is an explicit, documented reason why `null` is semantically distinct from empty for that method.

### Always-Valid Entities and Aggregate Assignment

Child entities owned by an aggregate use `internal static ErrorOr<T> Create(...)` factory methods that validate the entity's own structural invariants. The `internal` modifier restricts construction to the same assembly (`Neba.Api`); by convention, only the owning aggregate root calls these factories — never handler or test code directly.

The aggregate root's assign methods take raw properties, call the internal factory, enforce aggregate-level invariants (e.g., `Complete == true`), and return a single `ErrorOr<Success>` to the caller:

```csharp
// Child entity owns its own invariants — internal so only Season can construct it
internal static ErrorOr<HighBlockAward> Create(BowlerId bowlerId, int blockScore)
{
    if (blockScore <= 0)
        return Error.Validation("HighBlockAward.BlockScore", "Block score must be greater than zero.");
    return new HighBlockAward { Id = SeasonAwardId.New(), BowlerId = bowlerId, BlockScore = blockScore };
}

// Aggregate enforces its own invariant and delegates entity validation to the entity
public ErrorOr<Success> AssignHighBlockAward(BowlerId bowlerId, int blockScore)
{
    if (!Complete)
        return Error.Conflict("Season.NotComplete", "Awards may only be assigned to a completed season.");
    var award = HighBlockAward.Create(bowlerId, blockScore);
    if (award.IsError) return award.Errors;
    _highBlockAwards.Add(award.Value);
    return Result.Success;
}
```

**Why this matters**: If entity validation lived on the aggregate, the aggregate would absorb invariants that have nothing to do with it. If `Create()` were public, the entity could be constructed in an invalid state outside the aggregate. The internal factory gives call-site simplicity (single `ErrorOr` chain) while keeping each invariant owned by the right type.

### Aggregate Invariants Requiring Cross-Aggregate Data

When an assign method's invariant depends on data owned by another aggregate, the handler queries that data and passes it as a parameter. The aggregate enforces the rule; the handler provides the facts.

**The deciding factor — persist on aggregate vs. pass as parameter**:

- **Live data owned by another aggregate** → pass as a parameter. The other aggregate remains the single source of truth. Duplicating it creates redundancy. Example: `statEligibleTournamentCount` for `AssignHighAverageWinner` — tournaments own this fact, not Season.
- **Per-instance formula coefficients** → persist on the aggregate, set at a lifecycle transition. The formula belongs in the domain; the coefficient may legitimately vary per instance and must be frozen with the aggregate's closed state. Example: `_minimumGamesMultiplier` is set at `Season.Close()` because an abbreviated season might use a different threshold than a regular season.

```csharp
// Application layer provides the cross-aggregate fact; aggregate enforces the rule
public ErrorOr<Success> AssignHighAverageWinner(
    BowlerId bowlerId, decimal average, int games, int? tournamentsParticipated,
    int statEligibleTournamentCount)
{
    if (!Complete)
        return SeasonErrors.SeasonNotComplete;

    var minimumGames = ComputeMinimumGames(statEligibleTournamentCount);
    if (games < minimumGames)
        return SeasonErrors.InsufficientGames(games, minimumGames);

    var award = HighAverageAward.Create(bowlerId, average, games, tournamentsParticipated);
    if (award.IsError) return award.Errors;
    _highAverageAwards.Add(award.Value);
    return Result.Success;
}

// Formula is domain logic — lives on the aggregate, not the handler
private int ComputeMinimumGames(int statEligibleTournaments) =>
    (int)Math.Floor(_minimumGamesMultiplier * statEligibleTournaments);
```

The handler orchestrates — queries the cross-aggregate fact once, then drives the aggregate:

```csharp
var statEligibleCount = await appDbContext.Tournaments
    .CountAsync(t => t.SeasonId == command.SeasonId && t.StatEligible, ct);
season.AssignHighAverageWinner(command.BowlerId, command.Average, command.Games,
    command.TournamentsParticipated, statEligibleCount);
```

**Anti-pattern**: Computing a domain formula in the handler and passing the derived result (e.g., pre-computing `minimumGames` and passing it in). When the formula changes, the fix belongs in the domain — not scattered across handlers.

### Testing Requirements

#### Mutation Testing

Mutation testing (Stryker) is **not currently in the CI pipeline** — removed May 2026. Stryker configs (`stryker-config.json`) and local tooling remain in place for manual runs. See the `## Learnings` section below for notes on known Stryker limitations.

#### .NET Testing Requirements

- All tests need `[UnitTest]` or `[IntegrationTest]` trait
- All tests need `[Component("FeatureName")]` trait
- All Facts/Theories need `DisplayName`
- All test methods must include explicit AAA section comments: `// Arrange`, `// Act`, `// Assert`
- Use `MockBehavior.Strict` for all mocks
- Use `NullLogger<T>.Instance`, never mock ILogger
- Use test factories from `Neba.TestFactory`, never manual entity instantiation
- Test factories follow a consistent pattern: `Create()` with nullable params (const defaults), `Bogus(int count, int? seed)` for collection
- **`Create()` must always produce a persistable entity with no arguments** — every default must satisfy all domain invariants and EF constraints (e.g., required complex properties). If a test fails because `Create()` produces an invalid entity when called with no arguments, fix the factory default rather than patching the test. Example: `AddressFactory.CreateUsAddress()` passes `null` coordinates, but `BowlingCenterFactory.Create()` must call it with `coordinates: AddressFactory.ValidCoordinates` so the default address satisfies EF's non-nullable `Coordinates` constraint.
- Use a seed with `Bogus` only when the specific data values matter to the assertion (e.g., snapshot tests, integration tests for reproducibility). Omit the seed when only shape/count/type matters — the test is clearer without it
- When seeds are used, each test should use a distinct seed value — don't reuse the same seed across multiple tests
- Infrastructure services wrapping external SDKs (e.g., Azure Blob Storage) use Testcontainers for integration tests, not mocks
- Use **Shouldly** for assertions only; do not use FluentAssertions
- When testing null inputs on non-nullable parameters (nullable reference types are enabled project-wide), wrap the test method with `#nullable disable` / `#nullable enable` instead of using `null!`:

  ```csharp
  #nullable disable
  [Fact(DisplayName = "...")]
  public void Method_ShouldReturnError_WhenInputIsNull()
  {
      var result = SomeMethod(null);
      // assertions
  }
  #nullable enable
  ```

### API Endpoint Checklist

- Use case folder structure: Endpoint + Summary + Validator
- Authorization explicitly configured (never implicit) - use `AllowAnonymous()`, `Roles()`, or `Policies()`
- `WithName()` in Description for OpenAPI
- `Produces()`/`ProducesProblemDetails()` for all status codes
- Request wraps Input for commands

### Bug Fixing (TDD Approach)

1. Write a failing test that demonstrates the bug FIRST
2. Choose test project based on what's broken:
   - Domain entity/aggregate (in `Features/*/Domain/`) → Unit test in `Neba.Api.Tests`
   - Handler (in `Features/*/`) → Unit test in `Neba.Api.Tests`
   - EF Core / Database (in `Database/`) → Integration test in `Neba.Api.Tests`
   - API endpoint → Integration test in `Neba.Api.Tests`
   - Blazor component → bUnit test in `Neba.Website.Tests`
   - UI interaction/flow → E2E test in `tests/e2e/`
3. Verify the test fails (proves it catches the bug)
4. Make minimal code change to fix
5. Verify test passes
6. Run full test suite for regressions

## Workflow Commands

- **Full stack**: `aspire run`
- **Unit tests**: `dotnet test --filter "Category=Unit"`
- **Integration tests**: `dotnet test --filter "Category=Integration"`
- **Specific component**: `dotnet test --filter "Component=Tournaments"`
- **E2E tests**: `npm run test:e2e`
- **CI status**: `gh run list --limit 5`
- **CI failure details**: `gh run view <run-id> --log-failed`

## Learnings

### Custom Interactive Blazor Server Inputs — Keyboard Handling Must Live in JS, Not C#

When building a custom input component (`InputBase<T>` subclass) that needs synchronous per-keystroke behavior — auto-advance between segments, filtering keys, navigating on a separator character — **do not** drive that behavior with server-side C# event handlers (`@onkeydown`, `ElementReference.FocusAsync()`), even though it works fine in manual testing.

**Why**: Blazor Server round-trips every event over SignalR. A C#-driven `FocusAsync()` call to move focus to the next segment is asynchronous and network-latency-bound. Real (or automated) typing at normal speed can send the next keystroke before the previous round-trip's focus change has been applied client-side, landing digits in the wrong element. This was found building `NebaDateInput.razor` (see below): typing `9/5/2026` with a fast automated Playwright script scattered digits across the wrong segments (`day` got `20`, `year` got `26`) even though the identical logic worked correctly when each keystroke was typed slowly. It reproduces with real typing speed too, not just fast automation — the race is inherent to the round-trip, not a test artifact.

A second, related trap: even without the focus race, letting the browser's default keydown action fire (e.g. inserting a literal `/` character) while a C# `oninput` handler sanitizes it back to the *same string* as the previous render causes Blazor's virtual-DOM diff to skip updating the real DOM — the stray character stays visibly stuck in the input even though the bound C# state is correct. `preventDefault` can't be applied conditionally per-key via Razor's static `@onkeydown:preventDefault` directive (it's fixed per render, not per keystroke), so this can't be patched from the C# side either.

**Fix — do all interactive keyboard handling in a colocated JS module** (`Component.razor.js`, matching the existing `RichTextEditor.razor`/`.razor.js` pattern): attach native `keydown`/`input` listeners directly in JS, handle digit filtering/auto-advance/segment navigation/backspace synchronously with zero network round-trips, and call `preventDefault()` selectively per key inline (trivial in JS, not expressible in Razor). JS reports the final composed value back to .NET via a single `[JSInvokable]` method (e.g. `NotifySegmentsChanged`) — .NET is a passive listener that only computes/validates the resulting value, never drives focus or interaction itself.

**Testing implication**: bUnit renders the component tree but does not execute real browser JS, so bUnit tests for a component built this way cannot simulate typing via `.Change()`/`.Input()` on the DOM — call the `[JSInvokable]` method directly on the component instance instead (`cut.InvokeAsync(() => dateInput.Instance.NotifySegmentsChanged(...))`), same pattern as `RichTextEditorTests.NotifyContentChanged`. The actual keyboard-interaction logic (auto-advance, `/` navigation, filtering) needs to be covered by Jest tests against the `.razor.js` file directly (jsdom does execute real JS), not by bUnit.

Applied in `NebaDateInput.razor`/`.razor.js` (`src/Neba.Website.Server/Components/`), which replaces `InputDate` for `DateOnly?` fields — see `docs/plans/create-tournament.md`'s Components section for the full story (this started as a Safari-only bug report: WebKit's native `<input type="date">` doesn't reliably auto-advance segments when typing, unlike Chromium/Firefox).

### Dirty Form Guard — Warn Before Losing Unsaved Changes

Every data-entry page (any page with an `EditForm`, file uploads, or similar user input) must warn the user before they lose unsaved changes via Cancel, in-app navigation, or browser refresh/close/address-bar navigation. Use the shared `Components/DirtyFormGuard.razor` component — do not hand-roll this per page.

**How it works**:

- `<DirtyFormGuard IsDirty="@_isDirty" />` wraps Blazor's built-in `<NavigationLock>`:
  - `ConfirmExternalNavigation="@IsDirty"` triggers the browser's **native** "leave site?" dialog for refresh/close/address-bar navigation/back-forward — this is built into Blazor and cannot be customized (no custom JS/`beforeunload` interop needed or possible).
  - `OnBeforeInternalNavigation` intercepts in-app navigation (Cancel button's `NavigationManager.NavigateTo(...)`, `NavLink` clicks, etc.), calls `context.PreventNavigation()`, and shows a custom `ConfirmActionModal` ("Discard unsaved changes?" / Leave / Stay). Confirming re-issues the navigation with a one-shot bypass flag so the guard doesn't re-intercept its own confirmed navigation.
- The **page** owns dirty-tracking and passes the result in — the guard has no opinion on how dirty state is computed. Pattern (see `CreateArticle.razor`):
  - Create the `EditContext` explicitly in the constructor (`EditForm EditContext="_editContext"` instead of `Model="_model"`) and subscribe `_editContext.OnFieldChanged += (_, _) => MarkDirty();` — this covers any field bound through an `InputBase` descendant (`InputText`, `InputSelect`, `InputDate`, etc.) for free.
  - Anything **not** wired through `EditContext` needs an explicit `MarkDirty()` call: components that aren't `InputBase` (e.g. a custom `RichTextEditor` using plain `Value`/`ValueChanged`, not `@bind-Value` through an Input component), raw `<select>`/`@onchange` bindings, file upload add/remove callbacks, etc.
  - Reset `_isDirty = false` right before navigating away after a **successful** save — otherwise the guard fires again on the post-save `NavigateTo`.
  - Unsubscribe `_editContext.OnFieldChanged` in `DisposeAsync`.
- Login/credential-only forms are excluded — losing a half-typed password isn't the kind of "lost work" this guards against.

Enforced going forward via `.github/instructions/pull-request-review.instructions.md` (Blazor section + Review Checklist).

### Required-Field Indicator — Mark Required, Not Optional

A bare asterisk next to a label is no longer the right pattern: it isn't reliably announced by screen readers, and its meaning ("required"? "important"? a footnote?) isn't self-evident without a legend. Current guidance (WCAG, GOV.UK, USWDS) is to mark the *minority* case in visible text, not a symbol.

**Decision for this app: always mark required fields with the text "(required)"**, never optional fields. This was chosen over "mark optional fields" (the more common guideline when a form is mostly required) because an audit of the app's five real forms showed the opposite is true here — Sponsor forms are ~87% optional (3 of ~23 fields required), so marking optional fields there would tag most of the form instead of the few fields that actually matter. Marking required fields instead stays cheap on every form regardless of its required/optional ratio (3–4 tags at most).

**How it works**: use the shared `Components/FormLabel.razor` component instead of a bare `<label>` — do not hand-roll labels on new form fields.

```razor
<FormLabel TargetId="name" For="@(() => _model.Name)">Name</FormLabel>
<InputText id="name" @bind-Value="_model.Name" class="neba-input" placeholder="Sponsor name" />
```

- `TargetId` renders the label's `for` attribute, same as a plain `<label>`.
- `For` is an expression identifying the bound model property (same pattern as `ValidationMessage`'s `For`). `FormLabel` reflects on it via `FieldIdentifier.Create(For)` to check for a `[Required]` `DataAnnotation`, and renders "(required)" automatically when present — there is no manual `IsRequired`/`IsOptional` parameter to set, so the label can never drift out of sync with the model's actual validation attribute.
- Labels for fields that aren't bound to a `[Required]`-annotated model property (file uploads, custom pickers with a plain `<select>`, checkboxes) stay as plain `<label>` — `FormLabel` only applies where there's a real `For` expression to reflect on.
- **Login/credential-only forms are excluded**, same rationale as the Dirty Form Guard exception above: when every field on a form is required (e.g. `Login.razor`), tagging all of them adds no information, so those forms keep plain `<label>` elements with no indicator at all.

Applied to `CreateSponsor.razor`, `EditSponsor.razor`, `CreateArticle.razor`, `EditArticle.razor`, `CreateTournament.razor`. `Login.razor` intentionally left unmarked (see exclusion above).

### Lightweight Collection Projections — Naming Convention

When a UI need (e.g. a picker/dropdown) only requires a reduced projection of an existing collection (a few scalar fields instead of the full aggregate graph), check whether an existing query already returns a superset of that data before adding a new query/endpoint. Reuse-and-project-down at the consuming layer is preferred over a parallel lightweight endpoint — e.g. a tournament-linking picker in the news create form reuses `ListTournamentsInSeasonQuery`/`ISeasonsApi.ListTournamentsInSeasonAsync` (already consumed via `ITournamentApiService.GetTournamentsForSeasonAsync` → `SeasonTournamentViewModel`) rather than adding a second, near-duplicate "just Id/Name/StartDate" query — the existing one already returns everything a picker needs, and a second query with the same route shape only invites drift between two sources of truth for the same data.

**If a genuinely new lightweight query/endpoint turns out to be justified** (the existing query is too expensive to call just for a picker, or scoped differently), follow this naming split so the "reduced projection" distinction lives in exactly one place:

- **The DTO/response type** is named with `Summary`/`Summaries` (e.g. `TournamentSummaryDto`, `TournamentSummaryResponse`) — this is the one place that signals "deliberately reduced fields."
- **The query, handler, and endpoint class names — and the route — stay named after the resource itself**, with no `Summary`/`Summaries` suffix (e.g. `ListTournamentsInSeasonQuery`, route `{seasonId}/tournaments`) — matching whatever the "full" operation for that resource would be named, not a variant name. Do not let the operation name double up on the same "reduced" signal the DTO name already carries.

This means a new lightweight query for an existing resource **cannot reuse the same class/route names as an existing heavier query for that resource** without a genuine rename of one of them — check for an existing `List{Resource}` query/endpoint first, since a collision here means either reusing the existing one (preferred, see above) or deliberately renaming the existing "full" variant to something more specific (a larger, higher-risk change touching its existing consumers) rather than inventing a suffix on the new one.

### API Route Conventions

- **No `/api` prefix** — the API is served from `api.bowlneba.com`, so routes start directly with the resource (e.g. `/documents/{DocumentName}`, not `/api/documents/{DocumentName}`)
- **No version in path** — API versioning is handled via request headers, not URL segments (no `/v1/`, `/api/v1/`, etc.)

### Process-Wide Static State Leaks Between Integration Tests (MTP Shared-Process Test Runner)

Microsoft.Testing.Platform (MTP) runs the **entire test assembly in one shared process**. Any integration test that mutates a library's process-wide static/global configuration — rather than something scoped to its own DI container — leaks that mutation into every other test for the rest of the run, including tests in completely unrelated feature areas. This class of bug is especially nasty because:

- It only manifests when the polluting test happens to run (or finish initializing) before the affected test, so it's **order/parallelism-dependent** and can look like flakiness that comes and goes between local runs, CI runs, and Debug vs. Release builds (different JIT/thread-scheduling timing changes which interleaving actually occurs).
- The affected test's failure has nothing to do with its own code, making it look like an unrelated regression.

**Confirmed instances found (both fixed by removing/disabling the global mutation in the polluting test's `DisposeAsync()`/setup, not by touching the affected test):**

1. **FastEndpoints `ValidatorOptions.Global.PropertyNameResolver`** — `ApiAuditMiddlewareIntegrationTests` (`tests/Neba.Api.Tests/Auditing/`) called `_app.UseFastEndpoints()`, which — since FastEndpoints' `Config.Validation.UsePropertyNamingPolicy` defaults to `true` — overwrites FluentValidation's static `ValidatorOptions.Global.PropertyNameResolver` to run every validator's `PropertyName` through the app's JSON naming policy. This converted PascalCase property names (e.g. `"SeasonId"`) to camelCase (`"seasonId"`) for every other validator's bare `.Validate()` call in the process for the rest of the run, breaking dozens of unrelated validator tests across the suite. **Fix**: `_app.UseFastEndpoints(c => c.Validation.UsePropertyNamingPolicy = false);` — disable the option at the point of use rather than saving/restoring the resolver in `DisposeAsync()` (restore-after-the-fact still leaves a race window for tests running concurrently during the polluting test's lifetime).
   - **This same bug recurred** in `DeleteArticleEndpointAuthorizationTests` (`tests/Neba.Api.Tests/Features/News/DeleteArticle/`), a new integration test that builds its own real `WebApplication` and called `_app.UseFastEndpoints()` bare, plus a second, related static: FastEndpoints' `Factory.Create<TEndpoint>()` (used by every endpoint unit test's `Configure`/`HandleAsync` tests) lazily sets a process-wide static `ServiceResolver.Instance` the first time any real app calls `UseFastEndpoints()` — decompiling `FastEndpoints.dll` (`Factory.AddTestServices`) confirms it's a `static` set once (`if (ServiceResolver.InstanceNotSet) ...`) and never reset. Disposing that test's `WebApplication` in `DisposeAsync()` (via `_app.DisposeAsync()`) left `ServiceResolver.Instance` pointing at a disposed `IServiceProvider`, so every subsequent unrelated `Factory.Create<TEndpoint>()` call in the process (sponsors, tournaments, awards, bowling centers, hall of fame endpoint tests, etc.) threw `ObjectDisposedException: Cannot access a disposed object. Object name: 'IServiceProvider'`. Symptom was flaky — the failure count varied per run (68, then 35) depending on test interleaving. **Fix**: same as above for the naming-policy half — `_app.UseFastEndpoints(c => c.Validation.UsePropertyNamingPolicy = false);` — plus, in `DisposeAsync()`, stop the host with `await _app.StopAsync();` instead of `await _app.DisposeAsync();` (never dispose the `WebApplication`/its `IServiceProvider`), matching the pattern already used by `ApiAuditMiddlewareIntegrationTests`.
2. **Hangfire `GlobalJobFilters.Filters`/`JobActivator.Current` leaks + `[AuditJobExecutionFilter]` double-application — a CI-only flake that took four rounds of fixes to fully resolve.** `AuditJobExecutionIntegrationTests.Execute_ShouldProduceAuditEvent_WhenJobSucceeds` intermittently failed in CI (never locally reproduced despite 8+ repeated full-suite and targeted-collection runs) with `auditEvent.JobExecution.IsSuccess` `False` for a job whose body provably succeeded. This entry documents all four rounds, in order, because the first three were each real, verified bugs that still weren't the (sole) cause — the actual root cause (d) was only found by adding a temporary diagnostic dump of the raw exception into the test's failure message:
   - **(a) `GlobalJobFilters.Filters` leak — initially fixed incompletely.** `HangfireGlobalAuditFilterIntegrationTests` calls `services.AddBackgroundJobs(configuration)` (production code, `BackgroundJobConfiguration.cs`), which registers **three** filters into Hangfire's static, process-wide `GlobalJobFilters.Filters` in one `AddHangfire(...)` call: `AutomaticRetryAttribute`, `HangfireJobExpirationFilterAttribute`, and (via `.AddAuditJobExecutionFilter(...)` → Hangfire's `UseFilter<T>()`) `AuditJobExecutionFilterAttribute`. None of this is scoped to that test's own storage — it applies to **every** job in the process, including `AuditJobExecutionIntegrationTests`'s `AuditableTestJob` in a different test class. The first fix only removed `AuditJobExecutionFilterAttribute` (`GlobalJobFilters.Filters.Remove<AuditJobExecutionFilterAttribute>();`), which is why the flake persisted through two more rounds of fixes below — the leftover `AutomaticRetryAttribute` was still live, so *any* transient exception from *anything* in the filter chain silently retried the job rather than failing it outright, producing the second/duplicate-event symptoms (b) and (c) were built around. **Final fix**: `GlobalJobFilters.Filters.Clear();` in `DisposeAsync()`'s `finally` block — this test is the *only* one in the suite that ever populates `GlobalJobFilters.Filters`, so clearing the whole collection (rather than removing specific known types one by one) is safe and doesn't risk missing a filter added later by a future change to `AddHangfireInfrastructure`.
   - **(b) Double-filter-application when both a global registration and a method-level `[AuditJobExecutionFilter]` attribute apply to the same job** — `AuditJobExecutionFilterAttribute.OnPerforming`/`OnPerformed` (`Audit.Hangfire`) key their `IAuditScope` into `PerformContext.Items` under **fixed strings**, not per-instance keys, so two simultaneously-active instances clobber each other's entry. Decompiling `Audit.Hangfire`/`Hangfire.Core` with `ilspycmd` showed `HangfireJobExecutionEvent.IsSuccess => Exception == null`, and `Exception` is only set inside `OnPerformed` from `context.Exception` — Hangfire propagates **any** filter's thrown exception to the rest of the chain, so if one filter instance's own write throws, the *other* filter's `OnPerformed` can read that borrowed exception and mark an otherwise-successful job `IsSuccess: false`. This is a real hazard in the production Hangfire config, not just test leakage. **Fix**: added `.AuditWhen(context => context.BackgroundJob.Job.Method.GetCustomAttribute<AuditJobExecutionFilterAttribute>() is null && ...DeclaringType?.GetCustomAttribute<...>() is null)` to the global filter registration in `BackgroundJobConfiguration.cs`, making double-application structurally impossible regardless of test ordering/cleanup timing. Verified locally (3 full-suite runs, all green) — but **CI still failed with the identical symptom afterward**, meaning this specific mechanism, while real and worth fixing, was not the (sole) cause of the observed flake.
   - **(c) `WaitForEventAsync` picks the wrong event on retry** — the polling helper used `.FirstOrDefault(e => e.EventType == eventType && e.EndDate.HasValue)`. Hangfire has automatic retry behavior; if a job's first execution attempt fails for *any* reason (transient storage contention, filter interference, etc.), the retried (successful) attempt produces a **second**, separate completed audit event with the same `EventType` — and `FirstOrDefault` returns the stale first (failed) attempt by insertion order, not the actual final outcome. **Fix**: changed to `.Where(...).MaxBy(e => e.EndDate)` so the poll always resolves to the most recently completed event for that type, regardless of how many attempts occurred.
   - **(c) revealed a fourth, confirmatory bug**: switching to `.MaxBy(...)` (which must fully enumerate) turned up `InvalidOperationException: Collection was modified; enumeration operation may not execute.` on `Execute_ShouldProduceAuditEvent_WhenJobFails`. `Audit.Core.Providers.InMemoryDataProvider.GetAllEvents()` returns `_events.AsReadOnly()` — a **live wrapper over its internal mutable list, not a snapshot**. Enumerating it while the Hangfire worker thread concurrently calls `InsertEvent`/`ReplaceEvent` on a *different* job attempt races the test's own read. This is also **direct proof that (c)'s retry theory is real**: `List<T>`'s enumeration-version check only trips on structural changes (`Add`/`Remove`/`Clear`), not on same-index element replacement (`list[i] = x`) — so hitting this exception means a **new** event was being `Add`ed concurrently, i.e. a genuine retry was in flight, not just a replace. **Fix**: wrapped the query in `try { ... } catch (InvalidOperationException) { match = null; }` inside the poll loop, treating a torn read as "try again next iteration" rather than failing the test.
   - **(d) — the actual root cause, found via the diagnostic dump added in the "Status" note below**: once the dump surfaced the real exception, it was `System.ObjectDisposedException: Cannot access a disposed object. Object name: 'IServiceProvider'` at `Hangfire.AspNetCore.AspNetCoreJobActivator.BeginScope` → `Hangfire.JobActivator.BeginScope` → `CoreBackgroundJobPerformer.Perform`. `Hangfire.AspNetCore`'s DI wiring (triggered by `AddHangfire`/`AddHangfireServer` in `AddBackgroundJobs`) sets a **fourth** piece of Hangfire global state beyond `GlobalJobFilters.Filters`/`JobStorage.Current`/`LogProvider`: the static `JobActivator.Current`, pointed at an `AspNetCoreJobActivator` bound to `HangfireGlobalAuditFilterIntegrationTests`'s own `_serviceProvider`. That provider is disposed in `DisposeAsync()`, but `JobActivator.Current` was never reset — so every job on every Hangfire server in the process afterward calls the ambient (disposed) activator to construct the job instance, throwing **before the job body ever runs**. This is what was actually producing `IsSuccess: false` for `AuditableTestJob.Succeed`: `BeginScope` failed pre-execution, and — before (a) was fixed — the leftover `AutomaticRetryAttribute` retried it, but every retry hit the same disposed provider and failed identically, so even the "final" attempt by `EndDate` recorded a failure. **Fix**: `JobActivator.Current = new JobActivator();` in the same `finally` block, resetting Hangfire back to constructing job instances via `Activator.CreateInstance` instead of the disposed DI container.
   - **Status: resolved and confirmed green in CI.** `HangfireGlobalAuditFilterIntegrationTests.DisposeAsync()` now resets all three Hangfire statics it touches — `GlobalJobFilters.Filters.Clear()`, `Hangfire.Logging.LogProvider.SetCurrentLogProvider(null!)`, and `JobActivator.Current = new JobActivator();` — inside one `try`/`finally`, so none can be skipped by an earlier step throwing. `BackgroundJobConfiguration.cs`'s `AuditWhen` guard stays too, since it defends against a real (if not, here, root-cause) hazard. `AuditJobExecutionIntegrationTests.WaitForEventAsync`'s `MaxBy`/`InvalidOperationException` hardening was also kept — Hangfire's automatic retry is real production behavior, not unique to this bug, so picking the latest completed event and tolerating a torn read on a live in-memory collection are correct regardless of what triggers a retry. The temporary diagnostic dump added to surface the actual exception (which is what broke this investigation open) was removed from `Execute_ShouldProduceAuditEvent_WhenJobSucceeds` once the root cause was confirmed — if a similar unexplained flake shows up elsewhere, embedding the matching events' `Exception`/`EndDate` into the assertion's failure message (via Shouldly's message parameter) is the fastest way to get CI to hand you the actual answer instead of guessing.
   - **Lesson**: when two independently-registered filters/handlers/interceptors of the same library type can apply to the same unit of work (a job, a request, an event), check whether the library scopes its own per-execution state (context items, ambient scope, etc.) by *instance* or by a *fixed key* — a fixed key means at most one such filter can safely be active per execution. Separately: any test that polls for "the" event/record produced by an operation should assume the operation might produce more than one (retries, at-least-once delivery, etc.) and select the most recent/final one rather than the first match — and if the source being polled is a third-party in-memory collection, don't assume `GetAllEvents()`-style accessors return a stable snapshot; a live view enumerated against a concurrently-mutating background thread can throw, so poll loops reading such sources should tolerate a transient `InvalidOperationException` as "read again next iteration," not a real failure.

**When adding a new integration test that spins up a real host/library configuration (`WebApplication`, `IGlobalConfiguration`, `IHostBuilder`, etc.) for the first time in the test suite**: check whether the library being configured exposes any `static`/global mutable state (options classes reached via a static singleton, `GlobalJobFilters.Filters`, `ValidatorOptions.Global`, `Audit.Core.Configuration`, or Hangfire's static `LogProvider`/`JobActivator.Current`/`JobStorage.Current`, etc.). A single `AddHangfire(...)`/`AddHangfireServer(...)` call can touch *several* of these at once — don't assume you've found them all after fixing the first one; enumerate every static field the library's DI extension methods are documented (or observed via decompilation) to set. If so, either avoid touching the option that mutates it, or explicitly reverse/disable the mutation in `DisposeAsync()` **inside a `try`/`finally`** so the reversal isn't skipped if an earlier teardown step throws. Don't assume `[Collection("...Sequential")]` grouping is sufficient protection — it only serializes tests *within* the same collection; it does nothing to stop the static state leaking into tests in *other* collections/classes, which is where these bugs actually surfaced. If a test's failure cause resists a couple of rounds of source-level theorizing, stop guessing and embed the raw exception/state into the failing assertion's message (e.g. Shouldly's message parameter) — the next CI failure will hand you the actual answer.

### API Layer Mutation Testing — FastEndpoints Unit Test Limitations

When writing Configure tests with `Factory.Create<TEndpoint>()`, several categories of mutations are permanently unkillable:

1. **`Get(...)` calls** — FastEndpoints source generation pre-registers route templates at compile time via `SelfRegisteredExtensions.cs`. Even when `Get(...)` is removed from `Configure()`, `Definition.Routes` still contains the route template. Assert routes using `ShouldContain()`, but the route mutation will always survive. Add `"Get"` to `ignore-methods`.

2. **`Version(...)` calls** — `endpoint.Definition.Version.Current` is always 0 when using `FastEndpoints.AspVersioning` (version is applied via `MapToApiVersion` in an `Options()` delegate, not via direct `Version()` call). Add `"Version"` to `ignore-methods`.

3. **`Description(...)` and `Options(...)` calls** — Both store `Action<RouteHandlerBuilder>` delegates that are only invoked during real app startup (not in `Factory.Create<>()` unit tests). `EndpointMetadata` is always empty in unit tests, so `TagsAttribute` lookups return 0 items. Add `"Description"` and `"Options"` to `ignore-methods`.

4. **`return;` after `Send.NotFoundAsync()`** — FastEndpoints base class swallows exceptions thrown after the response has been set. Even if `result.Value` throws (ErrorOr v2), the 404 status remains and assertions pass. Use `// Stryker disable once Statement` before these `return;` guards.

5. **`await Send.OkAsync(...)` at the end of `HandleAsync`** — When this is the last statement, removing it is equivalent (no assertion fails on a void-like call with no state side-effects visible to unit tests). Use `// Stryker disable once Statement` before the final `Send.OkAsync` call.

6. **`Send.CreatedAtAsync(...)` throws `InvalidOperationException` in `Factory.Create<>()` unit tests** — `CreatedAtAsync` resolves a `LinkGenerator` from the endpoint's `HttpContext.RequestServices` to build the `Location` header, and `Factory.Create<>()` does not register one. Rather than skip testing the success path, assert that the call throws with `"LinkGenerator"` in the message — this proves the success branch was reached (the `ErrorOr` mapping and `Strict` mock setup already verify the command was dispatched with the right arguments before the throw). See `RegisterEndpointTests.HandleAsync_ShouldMapRequestToCommandAndTakeSuccessBranch_WhenRegistrationSucceeds` for the pattern.

**`ignore-methods` for API layer** (all five categories above): `"Description"`, `"Options"`, `"Get"`, `"Version"` — add all four to stryker-config.json. Use `// Stryker disable once Statement` inline for the `return;` and `Send.OkAsync` guards.

### API Layer Mutation Testing — `static readonly Lazy<>` Limitation

When a class uses `private static readonly Lazy<T>` (e.g., for a cached dictionary built via reflection), the MTP runner shares the same process across mutant runs. Once the `Lazy<>` is initialized by the first mutant run, subsequent mutant runs for methods that only execute during initialization never re-trigger the factory. All mutations inside those methods survive regardless of test quality.

**Fix — two-step inline disable required per init-only method**:

1. `// Stryker disable all` **inside** the method body (as its first statement) — disables all inline mutations (logical, equality, boolean, statement, bitwise, etc.) scoped to that method body. Place in EVERY init-only method.
2. `// Stryker disable once Block` **before** the method declaration — disables the block removal mutation, which operates at the declaration level and is NOT covered by the body-level disable. Place before EVERY init-only method declaration.

```csharp
// Stryker disable once Block : see BuildEnumNamesByTypeName
private static IEnumerable<Assembly> GetDomainAssemblies()
{
    // Stryker disable all : see BuildEnumNamesByTypeName
    EnsureNebaAssembliesLoaded();
    return AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => a.GetName().Name?.StartsWith("Neba.Domain", StringComparison.Ordinal) == true);
}
```

**Scope rules for inline disable comments** (confirmed empirically):

- `// Stryker disable all` inside a method body: scoped to that method body only. Does NOT span to sibling methods even if `// Stryker restore all` is in a later method. Both disable and restore must be in the SAME method body, or disable placed before a method declaration only covers that immediate method.
- `// Stryker disable once Block` before a method declaration: covers the NEXT block mutation (the method body block removal). One comment = one method.
- `// Stryker disable all` / `// Stryker restore all` at CLASS scope (between method declarations): only covers the immediately following method declaration, NOT a range.

This applies to any `ISchemaProcessor`, startup-cached registries, or other static initialization patterns.

### Log-Content Testing with FakeLogger

- Use `FakeLogger<T>` from the `Microsoft.Extensions.Diagnostics.Testing` NuGet package (version `10.7.0` in `Directory.Packages.props`) when a class's primary behavior involves logging and you need to assert on log level, message content, or structured attributes.
- Add `using Microsoft.Extensions.Logging.Testing;` — that is the namespace `FakeLogger<T>` lives in (the NuGet package name and the namespace differ).
- `FakeLogger<T>` is a real `ILogger<T>` implementation — not a mock — so it satisfies the "never mock ILogger" rule.
- Assert via `logger.Collector.GetSnapshot()` which returns `IReadOnlyList<FakeLogRecord>`, each with `.Level` and `.Message`.
- Each test project that uses `FakeLogger<T>` needs `<PackageReference Include="Microsoft.Extensions.Diagnostics.Testing" />` in its `.csproj`.

All classes that use `[LoggerMessage]` source-generated log methods have dedicated log-assertion tests using `FakeLogger<T>`. When adding a new class that logs, add `Microsoft.Extensions.Diagnostics.Testing` to its test project (if not already present) and add log-assertion tests covering every log level/path.

### PII Redaction in Logs

- Taxonomy: `Neba.Api.Compliance.DataTaxonomy` (`src/Neba.Api/Compliance/DataTaxonomy.cs`) — three `DataClassification`s: `Public` (not sensitive, no redaction), `Personal` (identifying but low-risk, partially masked), `Private` (sensitive PII, fully redacted). Extend this taxonomy rather than inventing a parallel one when a new category is needed.
- Attributes: `[PublicData]`, `[PersonalData]`, `[PrivateData]` (`Neba.Api.Compliance.*Attribute`, each wraps `DataClassificationAttribute` for its classification) — apply directly to any `[LoggerMessage]` parameter carrying a bowler's name/email/phone/address or similar. This is the whole convention: no manual masking helpers.
  - Use `[PrivateData]` for values that should never appear even partially (SSNs, payment info).
  - Use `[PersonalData]` for values that are useful to partially see for debugging/support (email addresses, names) — masked to first-character-plus-stars via `StarMaskingRedactor`.
  - Use `[PublicData]` only when you want to document that a parameter was deliberately reviewed and found non-sensitive (it's a no-op redaction-wise — `NullRedactor` passes the value through unchanged); omitting any attribute has the same runtime effect.
- Redactors registered per classification in `src/Neba.Api/Compliance/RedactionConfiguration.cs`, `AddRedaction()`: `NullRedactor` → `Public`, `StarMaskingRedactor` → `Personal` (custom, `src/Neba.Api/Compliance/StarMaskingRedactor.cs` — keeps the first character, stars out the rest), `ErasingRedactor` → `Private`. Called from `InfrastructureConfiguration.AddInfrastructure()`.
- **Gotcha — `builder.Services.AddRedaction(...)` alone does nothing.** It only registers `IRedactorProvider`/`IRedactor` in the container. The `[LoggerMessage]` source generator (`Microsoft.Gen.Logging`, from the `Microsoft.Extensions.Telemetry` package) emits code that reads `state.RedactedTagArray`, which is only populated when the logger itself is an `ExtendedLogger` — and that wrapper is only installed by calling **`builder.Logging.EnableRedaction()`** (from `Microsoft.Extensions.Telemetry`'s `LoggingRedactionExtensions`). Both calls are required; `AddRedaction()` in this codebase wires up both.
- Confirmed empirically: redaction applies to **both** the formatted `Message` string and the structured state tags (`FakeLogRecord.StructuredState`) — there's no separate code path to wire for Application Insights or other sinks, since they all consume the same `ILogger` state.
- `ErasingRedactor.Redact(...)` replaces the value with an **empty string**, not a placeholder token like `<redacted>`. E.g. a `[PrivateData]` parameter with value `"x@example.com"` produces an empty structured tag and an empty substitution in the formatted message.
- **`LoggerRedactionOptions.ApplyDiscriminator` (default `true`) folds the tag name into the value before redacting**, to prevent correlating redacted values across differently-named tags. This means a length-preserving redactor like `StarMaskingRedactor` produces more stars than the source value's own length (source + tag name length) — don't assert on an exact expected length; assert on the pattern instead (first char kept, rest starred, `ShouldNotContain` the original value/substrings).
- **Testing gotcha**: `FakeLogger<T>` constructed directly via `new FakeLogger<T>()` bypasses the DI logging pipeline entirely and never redacts anything, even with a classification attribute on the parameter — because it isn't wrapped by `ExtendedLogger`. Tests asserting on redaction must build a small DI container instead: `new ServiceCollection().AddLogging(l => l.AddFakeLogging().EnableRedaction()).AddRedaction(...).BuildServiceProvider()`, then resolve `ILogger<T>` and `IServiceProvider.GetFakeLogCollector()` from it. See `GoogleWorkspaceEmailSenderTests.SendAsync_ShouldMaskRecipientAddress_InFormattedMessageAndStructuredState` for the pattern. Tests that don't assert on log content (e.g. constructed with a plain `new FakeLogger<T>()`) are unaffected and don't need this.
- `RefitSettings.ExceptionRedactor` in `src/Neba.Website.Server/Services/ApiServicesConfiguration.cs` is an unrelated, pre-existing HTTP-header-scrubbing mechanism — do not confuse it with this feature despite the similar name.

### FusionCache Deserialization Recovery

- Cached query DTOs should use serialization-safe types; do not store domain `SmartEnum` instances directly in cached DTO properties.
- Map SmartEnum values to primitives in query projections (for example, `Status.Name` as `string`) before caching.
- `CachedQueryHandlerDecorator` catches cache deserialization failures on plain cached queries, logs a warning, executes the inner handler, and rewrites the cache entry.
- Keep the cache key stable unless explicitly directed otherwise; deserialization fallback handles stale entry recovery.

### EF Core Navigation Fixup — `= []` Collection Initializers Cause `Collection was of a fixed size`

When a domain entity initializes a collection navigation property with `= []` (C# 12 collection expression), the CLR resolves `IReadOnlyCollection<T> Prop { get; init; } = []` to `T[]` (a fixed-size array). EF Core 10's `ClrCollectionAccessorFactory` picks up this array type as `TCollection`, and when navigation fixup tries to call `AddStandalone(array, value)`, it hits `SZArrayHelper.Add` which throws `System.NotSupportedException: Collection was of a fixed size`.

This affects **both sides** of a relationship: adding a `TournamentSponsor` with a concrete `SponsorId` set causes EF to fix up `Sponsor.TournamentsSponsored` (also `= []`), even if you never set `Tournament = tournament` on the dependent.

**Symptom**: `NotSupportedException: Collection was of a fixed size` in the EF Core navigation fixup stack during integration test seeding.

**Fix in tests**: After saving the principal entities, call `_dbContext.ChangeTracker.Clear()` before adding dependent entities. With no tracked principals in the change tracker, EF has nothing to fixup against.

```csharp
await _dbContext.SaveChangesAsync(ct);

var tournamentDbId = _dbContext.Entry(tournament)
    .Property<int>(ShadowIdConfiguration.DefaultPropertyName).CurrentValue;

_dbContext.ChangeTracker.Clear(); // prevents fixup against tracked sponsors/tournaments

var ts = _dbContext.Set<TournamentSponsor>().Add(new TournamentSponsor { SponsorId = sponsorId, ... });
ts.Property<int>(TournamentConfiguration.ForeignKeyName).CurrentValue = tournamentDbId;

await _dbContext.SaveChangesAsync(ct);
```

**Note**: `PropertyAccessMode.Field` / `Navigation().HasField("_sponsors")` does NOT help — EF still determines `TCollection` from the property type, not the backing field type.

**Ordering constraint when combining TournamentSponsors + other dependents in the same test**: Any entities added via navigation properties to already-saved aggregates (e.g. `HistoricalTournamentChampion { Tournament = tournament }`) must be added and saved **before** `ChangeTracker.Clear()`. After the clear, detached entities passed as navigation properties are re-tracked as `Added`, causing a unique constraint violation on re-insert. The required save order for a fully-populated tournament test is:

1. Save all principals (season, bowling center, tournament, sponsors, bowlers)
2. Add `HistoricalTournamentChampion` entries (tournament + bowlers still tracked) → `SaveChangesAsync`
3. Read `tournamentDbId` from shadow property
4. `ChangeTracker.Clear()`
5. Add `TournamentSponsor` entries via shadow FK → `SaveChangesAsync`

**Stable Verify snapshots for tournaments**: Use explicit IDs via the source-generated `TournamentId(string)` constructor (the `ulid-full.typedid` template generates `public PLACEHOLDERID(string value)`). All-numeric ULID strings are valid (e.g. `"01000000000000000000000001"`). Apply the same to `SeasonId`, `BowlerId`, `SponsorId` — any ID that will appear in the snapshot output.

**Root-cause fix, preferred over the `ChangeTracker.Clear()` workaround above**: the `ChangeTracker.Clear()` workaround only helps when a test controls exactly when fixup runs against a *write*. It does nothing for the *read* path — any query that materializes the owner entity directly (e.g. `.SingleAsync(...)` returning the full `Sponsor`) triggers the same fixup when EF auto-includes the owned collection, throwing even with zero writes involved, as long as the collection is non-empty. The actual fix is to stop the property from ever holding an array:

- **Auto-property default** (`{ get; init; } = []`) → initialize with `new List<T>()` instead: `public IReadOnlyCollection<PhoneNumber> PhoneNumbers { get; init; } = new List<PhoneNumber>();`
- **Any fallback assigned into that property** (e.g. `PhoneNumbers = phoneNumbers ?? []` in a `Create()` factory or test factory) needs the same treatment: `phoneNumbers ?? new List<PhoneNumber>()`.
- Prefer the **backing-field pattern** already used by `Tournament._sponsors`/`_articles`/`_oilPatterns` and `SideCut._criteriaGroups` for any collection navigation that also needs mutator methods on the aggregate: `private readonly List<T> _field = [];` (here `[]` is fine — it's target-typed to the concrete `List<T>` field, not an interface, so the compiler doesn't synthesize an array) with `public IReadOnlyCollection<T> Prop => _field;`.

Confirmed and fixed for `Sponsor.PhoneNumbers`, `Sponsor.TournamentsSponsored`, and `BowlingCenter.PhoneNumbers` (all three were `{ get; init; } = []` auto-properties), plus the matching `?? []` fallbacks in `Sponsor.Create()`, `SponsorFactory.Create()`, and `BowlingCenterFactory.Create()`. After the fix, direct materialization of the owner entity with a populated collection (no `ChangeTracker.Clear()`, no projection workaround) works normally — see `CreateSponsorCommandHandlerTests.HandleAsync_ShouldPersistPhoneNumbers_WhenProvided`.

**This fix will not survive `dotnet format`/SonarQube unattended** — `dotnet_diagnostic.IDE0305.severity = warning` in `.editorconfig` ("Simplify collection initialization") actively suggests turning `new List<T>()` back into `[]`, and the pre-push Husky hook runs `dotnet format` and auto-commits any changes it makes. **IDE0028** ("Simplify collection initialization" — the non-target-typed sibling rule, also set to `warning` in `.editorconfig`) makes the identical suggestion and must be suppressed alongside IDE0305 at every one of these sites; suppressing only IDE0305 leaves IDE0028 free to flag (and `dotnet format` free to auto-fix) the same line. Every site fixed above therefore needs **both** of the following, not just one:

1. A `[SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "...")]` **and** a matching `[SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "...")]` on the containing property/method, with the justification (or a preceding `//` comment) explaining the fixed-size-array hazard — see `Sponsor.PhoneNumbers`, `Sponsor.TournamentsSponsored`, `Sponsor.Create()`, `BowlingCenter.PhoneNumbers`, `SponsorFactory.Create()`, `BowlingCenterFactory.Create()` for the pattern.
2. A regression test that casts the default collection instance to `ICollection<T>` and asserts `Add` doesn't throw (`Should.NotThrow(() => mutable.Add(...))`) — this is the only check that actually fails if someone (or a tool) reverts the fix, since a plain equality/count assertion can't distinguish a `List<T>` from a `T[]` with the same contents. See `SponsorTests.PhoneNumbers_DefaultInstance_ShouldSupportAdd_ForEfFixup` / `PhoneNumbers_PropertyInitializerDefault_ShouldSupportAdd_ForEfFixup` / `TournamentsSponsored_PropertyInitializerDefault_ShouldSupportAdd_ForEfFixup` and `BowlingCenterTests.PhoneNumbers_PropertyInitializerDefault_ShouldSupportAdd_ForEfFixup`. Verified by manually reverting `BowlingCenter.PhoneNumbers` to `[]` and confirming the test fails with `NotSupportedException`, then restoring it.

**When introducing a new `= []`-defaulted `IReadOnlyCollection<T>`/`IEnumerable<T>`/`IReadOnlyList<T>` navigation property on any EF-mapped entity** (owned collection or `HasMany`), apply both of the above immediately rather than waiting to hit the exception — the backing-field pattern (point 3 above) sidesteps the whole problem and needs neither suppression nor this style of test, so prefer it for any new collection navigation that also needs mutator methods.

### Razor @code Block — Parser Limitations

Two patterns that break Razor's lexer even inside `@code { }` blocks:

1. **Relational patterns `< N =>` in switch expressions** — `<` followed by a space then a digit is misread as an HTML tag start, causing the brace tracker to prematurely close the `@code` block. Use `if`/`else` with `>=` instead (e.g., `if (pct >= 90) return "full";`). `<=` (less-than-or-equal) is NOT affected — only bare `<` followed by a space.

2. **String interpolation with `{}` inside @code** — `$"prefix:{expr}suffix"` braces inside string interpolations in `@code` can confuse the Razor brace counter. Use string concatenation instead: `"prefix:" + expr + "suffix"`.

3. **Component attribute values always need `@` for C# expressions** — `Foo="fieldName"` passes the literal string `"fieldName"`, not the field's value. Always write `Foo="@fieldName"` for fields/properties, `Foo="@(expr)"` for expressions with operators (e.g. null-forgiving `!`, null-coalescing `??`).

4. **Blazor parameters use `[EditorRequired]` not C# `required`** — the `required` keyword on Blazor `[Parameter]` properties causes compile errors (CS0246/CS7014). Always use `[Parameter, EditorRequired]` with a default initializer (`= default!;`, `= string.Empty;`, `= [];`).

### List Page "Add New" Pattern — Floating Action Button

Any admin-gated list page (News, and future Sponsors/Bowling Centers/etc. admin views) uses the shared `FabCreateButton` component (`Neba.Website.Server/Components/FabCreateButton.razor`) as its "create new" entry point — a circular button fixed to the bottom-right of the viewport (`.neba-fab` in `wwwroot/neba_theme.css`), not a button embedded in the page's gradient title bar. Usage:

```razor
<AuthorizeView Policy="@Permissions.CreateArticle.PolicyName">
    <Authorized>
        <FabCreateButton Href="/news/new" Label="Create Article" />
    </Authorized>
</AuthorizeView>
```

`Href` is the create-page route; `Label` is both the accessible name and hover tooltip (e.g. "Create Article", "Add Sponsor"). This was chosen over embedding the button in the gradient `page-title-bar` because a solid/glass button there had low, position-dependent contrast against the gradient and competed visually with the hero content below it — the FAB sits outside page content entirely, at a fixed screen position, so it doesn't fight the header for attention and its position/behavior is identical across every list page it's added to.

### Long-List Picker Pattern — `NebaAutocomplete` vs. `InputSelect`

`InputSelect` (a native `<select>`) is fine for a short, fixed list (tournament type, U.S. state) where the whole list fits on screen and scanning it is fast. It stops working once the list grows past roughly 15–20 items — a Bowling Center picker with 80+ centers turns into either scrolling a long native dropdown or typing letters to jump-search it, both of which are slow and don't let the user search by anything other than the first letter of the display text.

**Decision: use the shared `Components/NebaAutocomplete.razor` component for any picker backed by a list that can grow past ~20 items or where the natural search key isn't the start of the display string** (e.g. searching a bowling center by city, not just name). It renders a single text `<input>` that filters an in-memory `Items` collection as the user types (substring match anywhere in the display text, not just prefix), with arrow-key navigation, a "no matches" state, and a clear (×) button for optional selections. First applied to `CreateTournament.razor`'s Bowling Center field, replacing an `InputSelect` over 80+ centers.

**Usage**:

```razor
<NebaAutocomplete Id="bowling-center" TValue="string" TItem="BowlingCenterSummaryResponse"
                   Value="@_model.BowlingCenterCertificationNumber"
                   ValueChanged="HandleBowlingCenterChanged"
                   Items="@_bowlingCenters"
                   DisplayText="@(center => center.Name + " — " + center.City + ", " + center.State)"
                   ItemValue="@(center => center.CertificationNumber)"
                   Placeholder="Search bowling centers..."
                   EmptyLabel="Not yet assigned" />
```

- `TValue`/`TItem` generics mirror the existing `NebaDropdown` design sketch in `reference/components/` (never implemented, kept as a design reference only — this component is the production version, built narrower: free-text filtering rather than that sketch's toggle-to-search combobox).
- Not an `InputBase<T>` — it's a plain `Value`/`ValueChanged` component like `OilPatternPicker`/`FileUpload`, so the hosting page must call `MarkDirty()` itself from the `ValueChanged` handler (the shared `EditContext.OnFieldChanged` hook only fires for `InputBase` descendants).
- **Keyboard nav (arrow keys/Enter/Escape) is handled server-side in C#** via `@onkeydown`, unlike `NebaDateInput`'s per-keystroke JS-only handling — the race condition documented under NebaDateInput's Learnings entry is specific to multiple segment elements fighting over focus mid-type; a single text input filtering a list has no such race, so keeping this in C# (consistent with how the rest of the app's server-rendered forms already work) is fine.
- **Click-outside-to-close still needs JS** (no Blazor-native equivalent) — colocated `NebaAutocomplete.razor.js`, same `initialize(containerId, dotNetHelper)`/`dispose(containerId)` shape as `NebaDateInput.razor.js`, using a capturing `mousedown` listener on `document`.

### Page Titles (`<PageTitle>`)

Every routable page must have a `<PageTitle>` component. Sub-components (cards, modals, skeletons) do not.

**Format**: `{Page Name} - BowlNEBA` (dash separator, BowlNEBA suffix). For dynamic detail pages: `@model.Name - BowlNEBA`.

**`<HeadOutlet>` must use `@rendermode="InteractiveServer"`** in `App.razor` — without it, Safari does not update the tab title on client-side navigation (Chrome is more lenient). Static render mode means `<PageTitle>` updates never reach the browser's `document.title` in Safari.

**Every routable page must also declare `@rendermode InteractiveServer`** — if a page is static SSR (no `@rendermode`), the interactive `HeadOutlet` circuit boots with no `<PageTitle>` registered and clears the title (visible as a flash then blank tab). Pages with no async data loading use `@rendermode InteractiveServer` (prerender: true default); data-loading pages use `@rendermode @(new InteractiveServerRenderMode(prerender: false))` to avoid a flash of empty content.

**Exception — auth pages that call `SignInAsync`/`SignOutAsync` (`Login.razor`, `Logout.razor`) intentionally omit `@rendermode`.** The auth cookie write must happen in the static-SSR pipeline; inside an established `InteractiveServer` circuit, the response has already started and `SignInAsync`/`SignOutAsync` cannot write the `Set-Cookie` header. These pages accept the title-flash tradeoff in exchange for a working cookie write.

### Email Template Pattern

- Each email is an `internal sealed class` in `{Feature}/Emails/{Name}Email.cs` — primary constructor takes email-specific values, exposes `ToHtmlBody()`.
- `EmailLayout.Wrap(innerHtml)` in `Neba.Api.Email` provides the branded chrome: NEBA blue (`#1a3a6e`) header with logo, white content area, and gray footer.
- The NEBA logo is served as a hosted URL (`https://bowlneba.com/images/neba-logo.png`) — **never use base64-embedded images** in email. Gmail Desktop/iOS/Android and many other clients have zero support for base64 images and strip them entirely. The `Email/Resources/neba-logo.png` embedded resource is no longer used.
- **Use inline styles only** — Gmail strips `<style>` blocks, so all styles must be on the elements themselves.
- **Always `WebUtility.HtmlEncode` user-supplied values** (links, codes) before embedding them in `href` attributes and visible text — prevents broken HTML when the value contains `&`, and guards against injection.
- Brand constants: header/button bg `#1a3a6e`, body text `#444`, muted/footer `#999`, page bg `#e8e8e8`.
- **Adding a new email**: create `{Feature}/Emails/{Name}Email.cs`, take constructor params, call `EmailLayout.Wrap(...)` in `ToHtmlBody()`. No infrastructure changes needed.
- **Mock-verification tests** (`IdentityEmailSenderAdapterTests`): always add `.Verifiable()` to the `Setup` and call `_sender.VerifyAll()` in the Assert block — the SonarAnalyzer (S2699) requires at least one explicit assertion per test.

#### Email HTML Compatibility Rules (from caniemail.com audit)

- **Use `<table>` for layout, not `<div>`** — `max-width` and `margin:0 auto` centering on `<div>` elements don't work in Outlook Windows. Use nested `<table role="presentation">` with `width` attribute and `align="center"` on the outer `<td>` instead.
- **Never use `overflow:hidden`** — only 54% email client support. For rounded corners on the outer container, accept that corners will be square in most clients (cosmetic only).
- **Logo image must be `display:block;margin:0 auto`** — `display:inline-block` has only 57% support (Outlook Windows doesn't support it except `display:none`). `display:block` is safe; centering within a `text-align:center` cell works everywhere.
- **`border-radius` is cosmetic only** — 64% support; buttons and boxes lose rounded corners in Outlook Windows and others. Acceptable degradation.
- **`<body>` has only 40% full support** — 34% of clients strip it entirely (Outlook Windows, Apple Mail, Samsung Email); another 26% replace it with a `<div>`. Any styles on `<body>` (background color, font-family) must be duplicated on the outer `<table>` as a fallback. Background color AND font-family both need to be on the outer table, not just the body.
- **`text-align:center`** is safe; avoid flow-relative values (`start`, `end`) which have ~38% less support.
