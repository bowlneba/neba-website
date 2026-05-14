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

If the diff is very large, group the review by layer (Domain → Application → Infrastructure → API → Contracts → Blazor/Website → Tests).

### 2. Load the review guidelines

Read `.github/instructions/pull-request-review.instructions.md` in full before reviewing. Every flag in the review must be traceable to a rule in that file or in CLAUDE.md.

### 3. Review the changes

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
- [ ] All status codes documented
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
- [ ] New entities/value objects/DTOs/responses have factory classes in `Neba.TestFactory` (SmartEnums, strongly-typed IDs, and input objects are exempt)
- [ ] Tests use factories, not manual instantiation
- [ ] Tests have `[UnitTest]` or `[IntegrationTest]` trait
- [ ] Tests have `[Component]` trait
- [ ] Facts and Theories have `DisplayName`
- [ ] New Domain bounded context added to `BoundedContextNamespaces` in `DomainBoundaryTests.cs`
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
- [ ] No sensitive data logged
- [ ] Business operations have activity spans

**Blazor**
- [ ] Components don't fetch data directly
- [ ] Pages are thin orchestrators

### 4. Present the review

Structure the output as:

---

## Pre-PR Review

### 🚫 Blockers
[List each blocker with file link and rule. If none: "None."]

### ⚠️ Should Fix
[List each should-fix item with file link and rule. If none: "None."]

### 💡 Suggestions
[List each suggestion. If none: "None."]

### ✅ Looks Good
[Brief note on what was done well or what was verified clean.]

---

Ask the user: **"Ready to generate the PR description, or would you like to address any of these first?"**

### 5. Generate the PR description

After the user confirms (or asks to proceed), infer the PR description format from the changes and the project's PR history. The format used in this project is:

```markdown
## Summary

[2–4 bullets covering what the PR does at a high level. Lead with the feature name bolded if it's a feature PR.]

## Context

[Optional — include when the domain problem or motivation isn't obvious from the code. 1–3 sentences.]

## What Changed

[Organized by layer. Only include layers that actually changed. Use sub-bullets for detail.]

### Domain (`Neba.Domain.*`)
### Application
### Infrastructure
### API
### Contracts
### Blazor (`Neba.Website.Server`)
### Architecture Tests
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
