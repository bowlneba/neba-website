# .NET 11 Blazor Plan

Part of the [.NET 11 upgrade](dotnet-11-upgrade.md), tracked in issue [#132](https://github.com/bowlneba/neba-website/issues/132).
This file holds everything Blazor: what to test in the upgrade PR, and what to adopt afterward. Add new Blazor ideas here, not in the upgrade plan.

Sources: Daniel Roth's ASP.NET Core 11 / Blazor session notes (issue #132 comment, 2026-08-25), the Anton Dev Tips newsletter (2026-11-10), a .NET 11 Blazor video review (2026-11-12), and Daniel Roth's two .NET Conf sessions: web development overview and Blazor Server (2026-11-13). Confirm each item against Microsoft's What's new in ASP.NET Core 11 before relying on it.

## Current state

- Ten pages use `EditForm`. Only `Login` runs as static SSR; the rest run `@rendermode InteractiveServer`.
- No `QuickGrid`. Tables are hand-built with `neba-data-table`.
- One `Virtualize` use: `Stats/SeasonStats.razor` (modals, #240).
- `Program.cs` already calls `AddOutputCache()` and `UseOutputCache()`. No `CacheView`, no session state, no `TempData` use.
- No `PersistentState` or `PersistentComponentState` use. Circuits keep no state across a restart.
- Every data-entry page uses `DirtyFormGuard` (see `docs/learnings/blazor-ui.md`).
- Deploys go through Azure Container Apps revisions (ADR-0001). A revision swap drops live circuits.

## Part 1: Test in the upgrade PR (Phase 1)

The upgrade PR changes only what it needs. These checks catch Blazor breakage from the runtime change.

- [ ] **CSRF with Fetch Metadata headers** (on by default in 11). Smoke-test login, logout, and a post from every `EditForm` page. Check any custom antiforgery code. Add a Playwright e2e check for one interactive form post and for login.
- [ ] **Circuit and `HttpContext` behavior.** Our `HttpClient` handlers reach circuit state through `CircuitServicesAccessor`. Confirm API calls from a circuit still carry the right auth after the upgrade.
- [ ] **`Virtualize` on the stats page.** Open the full-standings modals (#240) and scroll. The 11 virtualizer handles changing item heights differently; confirm no jump or blank gaps.
- [ ] **Output cache.** Confirm cached pages still vary correctly (no logged-in content served to another user). Check each `[OutputCache]` use and any policy.
- [ ] **Double load on pre-render.** Interactive pages pre-render, then load again on the circuit. Note which pages query the API twice today (`PersistentState` in Part 2 fixes this). No change needed in the upgrade PR.
- [ ] **`DirtyFormGuard`.** Navigate away from a dirty form on each data-entry page. Confirm the guard still prompts.
- [ ] **`.razor.js` modules.** Per-keystroke modules (auto-advance, key filtering) still load and run.
- [ ] **bUnit suite** (`Neba.Website.Tests`) passes with no new warnings.
- [ ] **Razor analyzer warnings.** New C# 15 or Razor compiler warnings: fix, or suppress with a reason in `.editorconfig`.
- [ ] **Run `aspire run`** and click through each routable page: title, render mode, form submit, table layout on a phone-width window.

Exit: bUnit and Playwright green on net11.0, manual click-through done, no new console errors in the browser.

## Part 2: Adopt after the upgrade (separate PRs)

One small PR per item. Order is by value to us.

### 1. Persist state across pre-render, drops, and deploys (highest value)

**Why:** each deploy swaps the Container Apps revision and drops live circuits. A user mid-way through a data-entry form loses the work. Pre-render also loads each interactive page's data twice.

The Blazor Server session shows one mechanism with four uses. Mark serializable properties `[PersistentState]` and set them only when no value was restored:

1. **Pre-render to interactive** (works since .NET 10): the data loads once, not twice. The demo proved it with one log line instead of two.
2. **Lost connection**: before the server evicts a disconnected circuit, Blazor saves its state. The user reconnects later and gets a new circuit with the same state. Eviction time is two minutes by default.
3. **Planned shutdown**: a circuit handler tracks live circuits; on shutdown, call `RequestCircuitPauseAsync` for each, wait a short time for the pause to finish (a request accepted is not a pause done), then stop. The state can live on the client, not the server.
4. **Auto-pause** (extra package): pause circuits whose tab has been hidden for a set time, and resume when the tab shows again. This frees server memory.

Limits:

- We choose which data to keep. Every kept property must serialize.
- A crash still loses the work. Only a drop, an eviction, or a planned stop is covered.
- State saved on the client leaves the server. Never persist a secret, a token, or personal data we would not send to the browser anyway.
- Hidden-tab pause changes idle-circuit memory, so check the scale and cost settings in ADR-0001.
- Revision swaps on Container Apps: confirm we can run the pause step before traffic stops (a pre-stop hook or the shutdown token). Test it; do not assume.

Plan:

- [ ] Read the .NET 11 docs for `[PersistentState]`, circuit persistence, `RequestCircuitPauseAsync`, and the auto-pause package.
- [ ] **Step A (pre-render, no deploy risk):** apply `[PersistentState]` to the loaded data on the two or three heaviest interactive pages. Check App Insights or logs show one API call per page view, not two.
- [ ] **Step B (forms):** trial on **one** data-entry page (the one with the longest form). Persist the form model and the dirty flag so `DirtyFormGuard` stays right after a restore.
- [ ] **Step C (drops):** test a lost connection longer than the eviction time: fill half a form, go offline, wait, go online, confirm the work returns.
- [ ] **Step D (deploys):** add a circuit handler and a shutdown hook that pauses circuits. Test a revision swap in a non-production environment.
- [ ] **Step E (auto-pause):** decide whether to add the package. Measure active circuits and memory first (Aspire dashboard has circuit counts).
- [ ] If adopted, write an ADR and a `docs/learnings/blazor-ui.md` rule on which pages must persist state.

### 2. `Virtualize` improvements

- [ ] Variable item heights: see whether `SeasonStats.razor` can drop any fixed-height workaround from #240.
- [ ] Start-at-item and scroll-to-item in code: candidate for jumping to a bowler in a long standings list.
- [ ] Other long lists that render every row today: check admin list pages and bowler lists before choosing.

### 3. Shareable list views (sort, page, filter in the URL)

QuickGrid in 11 keeps its page and sort order in the URL, so a link carries the view and the back button works. It also works with static SSR. We do not use QuickGrid, so there is nothing to port directly. The idea still fits our lists:

- [ ] Review the stats and tournament lists. Where a user can sort or filter, check whether the state belongs in the query string (`[SupplyParameterFromQuery]`). Do this with our own tables; do not add QuickGrid just for it.
- [ ] If we ever want a grid, trial QuickGrid on one admin list first. It must keep the `neba-data-table` phone layout.

### 4. Form validation and form components

- [ ] **Client-side validation for static SSR forms.** Only `Login` is static SSR, and login forms are exempt from our form rules. Low value now. Revisit if we add a public static form (contact, sign-up).
- [ ] **`Label<T>` and `DisplayName<T>` components.** They read the model's display name and write the `<label>` or the text. Compare to our `FormLabel` (which adds "(required)"). Adopt only if they replace code without losing the required marker; they work in static and interactive modes.
- [ ] **`SupplyParameterFromTempData`.** Review for post-redirect messages such as "Saved" after a form post.
- [ ] **Async validation (`AsyncValidationAttribute`): do not adopt.** Our rule is that validators handle structure only, with no database lookups. Uniqueness and other state checks belong in the command handler as `Error.Conflict`. If the video's username-availability example shows up as a UX request, solve it with a handler-backed check on blur, not a validator.

### 5. Static SSR for public pages, with caching

.NET 11 makes more pages possible without a circuit: client-side validation, `TempData` and session parameters, `QuickGrid` sort and page via URL links, and enhanced navigation. Two caches help public content:

- `[OutputCache]` on a page caches the whole response (we already run the middleware).
- The new `CacheView` component caches one region (the demo set vary-by query parameters and a two-minute expiry). It uses `HybridCache` if configured, otherwise memory. Our API has `HybridCache`; the website does not. Per our memory note, query caching in the API uses FusionCache; do not add a second cache stack to the website without a decision.

For us this matters because public pages (stats, tournaments, Hall of Fame, documents) read mostly static data, yet each visit opens a circuit if the page is interactive.

- [ ] List the public routable pages and mark which need interaction. A page that only reads and links can be static SSR (drop `@rendermode InteractiveServer`). This conflicts with the current rule "every routable page has `@rendermode InteractiveServer`", so change the rule in `docs/learnings/blazor-ui.md` and `CLAUDE.md` if we adopt this.
- [ ] Trial one read-only public page as static SSR with `[OutputCache]`. Measure circuit count and time to first content before and after.
- [ ] Check what breaks without a circuit: `DirtyFormGuard`, `.razor.js` modules, `NebaAutocomplete`, and anything using JS interop.
- [ ] Try `CacheView` on one expensive region only if output caching a whole page does not fit (for example a page with a per-user header).

### 6. Authentication refresh on live circuits

SignalR can now reauthenticate an existing connection on a timer, and Blazor Server wires this up. The demo removed a permission from a user and the page dropped the refund button within one refresh interval, with the form still intact. The check is timed, not instant.

- [ ] Find out how our cookie or session-token flow (`Account/Session`, `MapSessionTokenEndpoints`) interacts with it. Today a circuit keeps the principal from the cookie that opened the connection (see `CLAUDE.md`).
- [ ] If a role change in the admin pages should take effect on a live circuit, trial the refresh. Document the interval and what happens when the refresh fails.

### 7. Results and unions (C# 15, not Blazor)

The video's saved, invalid, or conflict example is the job `ErrorOr<T>` does today (`Error.Validation` and `Error.Conflict`). `ErrorOr<T>` stays. See "Evaluate only" in the upgrade plan before using `union`.

### Skip

- **Blazor gateway, client defaults, web workers, and WebAssembly debugging.** We run Blazor Server.
- **Free Ignite UI and Syncfusion Blazor components.** Not needed now; look if we want a grid or chart.
- **Razor tooling features** (Emmet, `~/` paths, editor config formatting): see the upgrade plan.

- **WebAssembly trimming.** We run Blazor Server. Measure first if that ever changes.
- **Auto render mode and WASM work.** Not in scope.

## Add new .NET 11 Blazor ideas here

Use this table for anything new from the What's new pages, release notes, or conference talks.

| Idea | Source | Value to us | Decision |
| --- | --- | --- | --- |
| `[PersistentState]` and circuit pause | Blazor Server session | High: deploys drop work | Part 2, item 1 |
| `CacheView` and output caching | Blazor Server session | Medium: public pages | Part 2, item 5 |
| Static SSR client validation, `TempData` | Blazor Server session | Low now | Part 2, item 4 |
| Auth refresh on circuits | Both sessions | Medium | Part 2, item 6 |
| `Label` and `DisplayName` components | Blazor Server session | Low | Part 2, item 4 |
| Async validation | Both sessions | None (our rule) | Do not adopt |

## Housekeeping

- [ ] Update `docs/learnings/blazor-ui.md` and the Blazor rules in `CLAUDE.md` for any new gotcha.
- [ ] Update `docs/architecture/blazor.md` if a render-mode or state pattern changes.
- [ ] Add a Blazor line to the research-log comment on #132 linking this file.
