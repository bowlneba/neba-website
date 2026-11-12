# .NET 11 Blazor Plan

Part of the [.NET 11 upgrade](dotnet-11-upgrade.md), tracked in issue [#132](https://github.com/bowlneba/neba-website/issues/132).
This file holds everything Blazor: what to test in the upgrade PR, and what to adopt afterward. Add new Blazor ideas here, not in the upgrade plan.

Sources: Daniel Roth's ASP.NET Core 11 / Blazor session notes (issue #132 comment, 2026-08-25), the Anton Dev Tips newsletter (2026-11-10), and a .NET 11 Blazor video review (2026-11-12). Confirm each item against Microsoft's What's new in ASP.NET Core 11 before relying on it.

## Current state

- Ten pages use `EditForm`. Only `Login` runs as static SSR; the rest run `@rendermode InteractiveServer`.
- No `QuickGrid`. Tables are hand-built with `neba-data-table`.
- One `Virtualize` use: `Stats/SeasonStats.razor` (modals, #240).
- No `PersistentState` or `PersistentComponentState` use. Circuits keep no state across a restart.
- Every data-entry page uses `DirtyFormGuard` (see `docs/learnings/blazor-ui.md`).
- Deploys go through Azure Container Apps revisions (ADR-0001). A revision swap drops live circuits.

## Part 1: Test in the upgrade PR (Phase 1)

The upgrade PR changes only what it needs. These checks catch Blazor breakage from the runtime change.

- [ ] **CSRF with Fetch Metadata headers** (on by default in 11). Smoke-test login, logout, and a post from every `EditForm` page. Check any custom antiforgery code. Add a Playwright e2e check for one interactive form post and for login.
- [ ] **Circuit and `HttpContext` behavior.** Our `HttpClient` handlers reach circuit state through `CircuitServicesAccessor`. Confirm API calls from a circuit still carry the right auth after the upgrade.
- [ ] **`Virtualize` on the stats page.** Open the full-standings modals (#240) and scroll. The 11 virtualizer handles changing item heights differently; confirm no jump or blank gaps.
- [ ] **`DirtyFormGuard`.** Navigate away from a dirty form on each data-entry page. Confirm the guard still prompts.
- [ ] **`.razor.js` modules.** Per-keystroke modules (auto-advance, key filtering) still load and run.
- [ ] **bUnit suite** (`Neba.Website.Tests`) passes with no new warnings.
- [ ] **Razor analyzer warnings.** New C# 15 or Razor compiler warnings: fix, or suppress with a reason in `.editorconfig`.
- [ ] **Run `aspire run`** and click through each routable page: title, render mode, form submit, table layout on a phone-width window.

Exit: bUnit and Playwright green on net11.0, manual click-through done, no new console errors in the browser.

## Part 2: Adopt after the upgrade (separate PRs)

One small PR per item. Order is by value to us.

### 1. Circuit pause and state across restarts (highest value)

**Why:** each deploy swaps the Container Apps revision and drops live circuits. A user mid-way through a data-entry form loses the work.

What .NET 11 adds, per the video: the server can ask the browser to pause a circuit before a planned restart, and an optional package can pause a circuit whose tab has been hidden for a while (this frees server memory for people who leave the app open all day).

Limits to keep in mind:

- It needs state-saving setup. We choose which data to keep.
- It helps with a planned shutdown. A crash is different; the work is still lost.
- Pausing hidden tabs touches idle-circuit memory, so check the scale and cost settings in ADR-0001.

Plan:

- [ ] Read the .NET 11 docs for circuit pause/resume and the state-saving API (`[PersistentState]`).
- [ ] Trial on **one** data-entry page (pick the one with the longest form). Persist the form model and the dirty flag so `DirtyFormGuard` stays right after a restore.
- [ ] Test a planned revision swap in a non-production environment: fill half a form, deploy, confirm the work returns.
- [ ] Decide whether to add the hidden-tab pause package. Measure circuit count and memory first.
- [ ] If adopted, write an ADR and a `docs/learnings/blazor-ui.md` rule on which pages must persist state.

### 2. `Virtualize` improvements

- [ ] Variable item heights: see whether `SeasonStats.razor` can drop any fixed-height workaround from #240.
- [ ] Start-at-item and scroll-to-item in code: candidate for jumping to a bowler in a long standings list.
- [ ] Other long lists that render every row today: check admin list pages and bowler lists before choosing.

### 3. Shareable list views (sort, page, filter in the URL)

QuickGrid in 11 keeps its page and sort order in the URL, so a link carries the view and the back button works. It also works with static SSR. We do not use QuickGrid, so there is nothing to port directly. The idea still fits our lists:

- [ ] Review the stats and tournament lists. Where a user can sort or filter, check whether the state belongs in the query string (`[SupplyParameterFromQuery]`). Do this with our own tables; do not add QuickGrid just for it.
- [ ] If we ever want a grid, trial QuickGrid on one admin list first. It must keep the `neba-data-table` phone layout.

### 4. Form validation features

- [ ] **Client-side validation for static SSR forms.** Only `Login` is static SSR, and login forms are exempt from our form rules. Low value now. Revisit if we add a public static form (contact, sign-up).
- [ ] **`SupplyParameterFromTempData`.** Review for post-redirect messages such as "Saved" after a form post.
- [ ] **Async validation (`AsyncValidationAttribute`): do not adopt.** Our rule is that validators handle structure only, with no database lookups. Uniqueness and other state checks belong in the command handler as `Error.Conflict`. If the video's username-availability example shows up as a UX request, solve it with a handler-backed check on blur, not a validator.

### 5. Results and unions (C# 15, not Blazor)

The video's saved, invalid, or conflict example is the job `ErrorOr<T>` does today (`Error.Validation` and `Error.Conflict`). `ErrorOr<T>` stays. See "Evaluate only" in the upgrade plan before using `union`.

### Skip

- **WebAssembly trimming.** We run Blazor Server. Measure first if that ever changes.
- **Auto render mode and WASM work.** Not in scope.

## Add new .NET 11 Blazor ideas here

Use this table for anything new from the What's new pages, release notes, or conference talks.

| Idea | Source | Value to us | Decision |
| --- | --- | --- | --- |
| | | | |

## Housekeeping

- [ ] Update `docs/learnings/blazor-ui.md` and the Blazor rules in `CLAUDE.md` for any new gotcha.
- [ ] Update `docs/architecture/blazor.md` if a render-mode or state pattern changes.
- [ ] Add a Blazor line to the research-log comment on #132 linking this file.
