---
name: pull-request-prep
description: Review pending changes against main (or a specified branch), flag issues, and generate a copy-paste PR description. Usage: /pull-request-prep [base-branch]
---

Prepare a pull request by reviewing all changes, flagging issues against project standards, and producing a markdown PR description ready to paste into GitHub.

## Arguments

- **base-branch** (optional) — the branch this PR will merge into. Defaults to `main`.

## Steps

### 1. Establish the diff

```
git diff <base-branch>...HEAD
git log <base-branch>...HEAD --oneline
git diff <base-branch>...HEAD --name-only
```

If the diff is very large, group the review by slice (Features/{Feature} → Contracts → Blazor/Website → Tests).

### 2. Check for plans/mockups (main only)

Only when **base-branch is `main`** (or was left as the default): check `docs/plans/` and `docs/plans/mockups/` for anything new or changed on this branch —

```
git diff <base-branch>...HEAD --name-status -- docs/plans/
git status --porcelain -- docs/plans/
```

These are working documents for the feature, not project history — by default they should be deleted before the PR merges to main, so they don't carry forward once the feature ships. For each file or mockup folder found:

- Delete it, **unless** there's a concrete reason to keep it (e.g. the plan doubles as ongoing design reference for a phased/multi-PR feature, or the mockups are linked from a doc that stays). If so, keep it and call out the reason explicitly in the Pre-PR Review output (a new **📐 Plans & Mockups** section — see step 4) rather than deleting silently.
- If deleted, note it in that same section so the user can see what was removed and why.

Skip this step entirely when base-branch is not `main` (e.g. merging into a long-lived feature/integration branch, where plans may still be useful to the next PR in the chain).

### 3. Load the review guidelines

Read `.github/instructions/pull-request-review.instructions.md` in full before reviewing. Every flag in the review must be traceable to a rule in that file or in CLAUDE.md.

### 4. Review the changes

Work through the diff layer by layer. For each issue found, record:
- **File and line** (link using `[file.cs:42](path/file.cs#L42)`)
- **Rule violated** (cite the section, e.g. "API Endpoint Checklist — authorization not explicitly configured")
- **Severity** (see below)

#### Severity levels

| Level | Meaning |
|---|---|
| 🚫 **Blocker** | Violates a hard architectural rule, missing required element (auth, error handling, test trait), or introduces a security/correctness risk. Must be fixed before opening the PR. |
| ⚠️ **Should Fix** | Clearly violates a convention but is unlikely to cause a runtime failure — e.g. missing `DisplayName`, wrong extension method syntax, unsealed class. Should be fixed unless there's a deliberate reason not to. |
| 💡 **Suggestion** | Improvement that's nice to have but doesn't break rules — e.g. an opportunity for a cleaner abstraction, a missing E2E test consideration, a metrics opportunity. |

#### Checklist to work through (from the review guidelines)

**Architecture & Code Quality**
- [ ] Layer boundaries respected (no cross-domain references beyond strongly-typed IDs)
- [ ] Commands return `ErrorOr<T>`
- [ ] Queries return DTOs, not entities
- [ ] `extension()` block syntax used, not legacy `this` parameter
- [ ] `DateTimeOffset` used instead of `DateTime` for points in time
- [ ] No banned libraries (AutoMapper, Newtonsoft.Json, BinaryFormatter)

**API Endpoints** (if any endpoints changed)
- [ ] Use case folder structure (Endpoint, Summary, Validator per folder)
- [ ] Authorization explicitly configured
- [ ] `WithName()` in Description
- [ ] Tags match authorization (Public/Authenticated/Admin)
- [ ] All status codes documented — **check the endpoint group first**: `ProducesProblemDetails(500)` is declared on the group (e.g., `AwardsEndpointGroup`, `BowlingCentersEndpointGroup`) and does not need to be repeated on each endpoint. Individual endpoints only need `Produces<TResponse>(200)` and any status codes unique to that endpoint (e.g., `ProducesProblemDetails(404)` for endpoints that can 404)
- [ ] Validator present and contains only structural validation
- [ ] All errors return ProblemDetails (bare `Send.NotFoundAsync()` is acceptable for simple 404s)
- [ ] Summary class with realistic examples
- [ ] Inline mapping (no mapper classes)
- [ ] No `/api` prefix or version segment in route

**Contracts** (if any contracts changed)
- [ ] Request wraps Input for commands
- [ ] XML documentation on public types and properties
- [ ] `{ get; init; }` not `{ get; set; }`
- [ ] Refit interface updated

**Testing**
- [ ] **Every new or meaningfully-changed source file has a corresponding test file in the diff.** Do this check explicitly, don't infer it from the presence of *other* tests in the diff:
  1. From the step 1 file list, build the set of new/changed non-test source files that carry logic — `.cs` files under `src/` with a handler, endpoint, domain type, or non-trivial method body; `.razor` files with an `@code` block containing more than a trivial parameter passthrough.
  2. For each, check whether the diff also touches a matching test file (`tests/Neba.Api.Tests/**/{Name}Tests.cs`, `tests/Neba.Website.Tests/**/{Name}Tests.cs`, or an e2e spec covering it per the Playwright table below).
  3. A `.razor` **page** (has `@page`) or any component with event handlers, conditional rendering, or API calls is not covered by "some other file in this PR has tests" — it needs its own test file. A pure layout/presentational component with no `@code` logic is exempt.
  4. Flag any source file with no matching test file as a 🚫 **Blocker** — name the file and state plainly that it has zero test coverage, don't downgrade this to a suggestion.
- [ ] New entities/value objects/DTOs/responses have factory classes in `Neba.TestFactory` (SmartEnums, strongly-typed IDs, and input objects are exempt)
- [ ] Tests use factories, not manual instantiation
- [ ] Tests have `[UnitTest]` or `[IntegrationTest]` trait
- [ ] Tests have `[Component]` trait
- [ ] Facts and Theories have `DisplayName`
- [ ] No `.Verify()` calls when using `MockBehavior.Strict`
- [ ] No `null!` for null-argument tests — uses `#nullable disable`/`#nullable enable`

**Playwright E2E tests** (if Blazor pages added or changed)

Playwright is the right tool when the behavior involves the real browser + real HTTP stack together — things bUnit cannot exercise:

| Add a Playwright test when… | Skip it when… |
|---|---|
| New page with API-backed rendering (verify end-to-end data flow) | Internal component logic or rendering — use bUnit |
| Navigation flow between pages (link → URL change) | Pure UI state within one component — use bUnit |
| URL query parameter drives page behavior | Data transformation or business logic — use unit tests |
| Modal / overlay lifecycle (open, close via button or backdrop) | Static-only page with no API or interactions |
| Redirect / not-found state triggered by API 404 | Page that is covered by an existing Playwright test already |
| Cross-page state persistence (e.g. season preserved across nav) | |
| Keyboard accessibility for interactive widgets | |

When adding Playwright tests:
- [ ] New mock API endpoint added to `tests/e2e/mock-api/mock-api-server.ts` for any new API route the page calls
- [ ] Spec file added under `tests/e2e/` (group by page; combine closely related pages in one file)
- [ ] Tests anchor on stable CSS class selectors (BEM `.block__element--modifier`), not text content for structural assertions
- [ ] `page.waitForSelector()` used in `beforeEach` (not arbitrary sleeps) to wait for data-driven content to appear

**Observability**
- [ ] Logging present with appropriate levels
- [ ] No sensitive data logged unredacted — check every new/changed `[LoggerMessage]` parameter for PII (bowler name, email, phone, address, or similar). If found:
  - Apply `[PrivateData]` (`Neba.Api.Compliance.PrivateDataAttribute`) to the parameter — do not hand-roll masking helpers. See CLAUDE.md's "PII Redaction in Logs" learning for the full pattern and the `EnableRedaction()` gotcha.
  - If the PII category doesn't fit the existing `DataTaxonomy.PrivateData` classification, flag it as a 💡 suggestion to extend the taxonomy rather than inventing a parallel mechanism.
  - Flag raw SQL/query text or full request/response bodies logged at any level as a 🚫 blocker or ⚠️ should-fix (depending on log level and whether it's gated behind `IsEnabled` checks) — these can carry parameter values that bypass classification entirely.
- [ ] Business operations have activity spans

**Blazor**
- [ ] Components don't fetch data directly
- [ ] Pages are thin orchestrators

**README**
- [ ] `README.md`'s Project Structure reflects any new/removed top-level `Features/{Feature}` folders, projects, or render-mode changes
- [ ] `README.md`'s Technology Stack reflects any new/removed package that changes what's user-visible (new datastore, new client library, new background job engine, etc.) — not every `Directory.Packages.props` bump, just ones that change the stack story
- [ ] `README.md`'s Implementation Plan checkboxes reflect features this PR completes or starts (check off finished items, leave partial work unchecked)

### 5. Present the review

Structure the review as:

---

## Pre-PR Review

### 🚫 Blockers
[List each blocker with file link and rule. If none: "None."]

### ⚠️ Should Fix
[List each should-fix item with file link and rule. If none: "None."]

### 💡 Suggestions
[List each suggestion. If none: "None."]

### 📐 Plans & Mockups
[Only present when base-branch is `main`. List each file/folder under `docs/plans/` deleted in step 2, and each one kept along with its stated reason. If step 2 found nothing under `docs/plans/`, omit this section entirely.]

### 📄 README Updates
[List each stale/missing spot in README.md found via the **README** checklist above, with the proposed change. If none: "None — README is current."]

### ✅ Looks Good
[Brief note on what was done well or what was verified clean.]

---

Write this review verbatim to `pr-review.md` at the repo root (overwrite if it already exists) so it's available for reference when addressing the findings later. Then show the same content in the chat response.

Ask the user: **"Ready to generate the PR description, or would you like to address any of these first?"**

### 6. Apply README updates

If step 5 found any README Updates, apply them directly to `README.md` now (unless the user said they'd handle findings themselves) — these are typically small, mechanical (a checkbox, a stack line, a folder in the structure diagram) and don't warrant a separate round-trip. Show a brief summary of what changed. Skip this step entirely if the README Updates list was empty.

### 7. Generate the PR description

After the user confirms (or asks to proceed), infer the PR description format from the changes and the project's PR history. The format used in this project is:

```markdown
## Summary

[2–4 bullets covering what the PR does at a high level. Lead with the feature name bolded if it's a feature PR.]

## Context

[Optional — include when the domain problem or motivation isn't obvious from the code. 1–3 sentences.]

## What Changed

[Organized by slice/area. Only include sections that actually changed. Use sub-bullets for detail.]

### Features/{Feature} (`Neba.Api.Features.*`)
[Domain, handlers, endpoints — group by feature if multiple features touched.]
### Contracts
### Blazor (`Neba.Website.Server`)
### Tests
### Docs

## Test Plan

[Checkbox list. Include component filter commands where applicable:]
- [ ] `dotnet test --filter "Component=<Feature>"` — all unit and integration tests pass
- [ ] [specific test class or scenario worth calling out]
- [ ] Navigate to `/<route>` in the running app and verify [behaviour]

## Deferred

[Optional — list anything explicitly left out of scope. If nothing is deferred, omit this section.]
```

Rules for the description:
- Keep **Summary** bullets tight — what it does, not how
- **What Changed** should be detailed enough that a reviewer can map each section to the diff; include entity names, table names, query key, route, and page path where relevant
- **Test Plan** should be actionable checkboxes — include the `dotnet test --filter` command for the component, specific test classes worth calling out, and a manual smoke-test step
- Write in the same voice as the existing PR descriptions — confident, specific, not over-explained
- Do NOT add a "Co-authored-by" or AI attribution line

Output the description inside a fenced markdown block so the user can copy it directly.
