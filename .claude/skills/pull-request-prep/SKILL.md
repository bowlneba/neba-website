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

### 2. Run npm audit

Run `npm audit` at the repo root. This check always runs, regardless of whether `package.json`/`package-lock.json` changed on this branch — a failing JS test can be caused by an advisory that landed upstream since the lockfile was last touched, not just by a diff on this branch.

- If `npm audit` reports no vulnerabilities, note that in the **✅ Looks Good** section (step 6) and move on.
- If it reports vulnerabilities, record each one (package, severity, advisory) for the **🚫 Blockers** or **⚠️ Should Fix** section (step 6) — severity `high`/`critical` is a 🚫 Blocker, `moderate`/`low` is ⚠️ Should Fix. State whether `npm audit fix` resolves it without a breaking major-version bump; if it does, apply the fix and note that it was applied. If a fix requires `--force` (a breaking upgrade), do not apply it — flag it instead and let the user decide, since `feedback_npm_lockfile_linux_ci.md`'s constraint (lockfile changes must be regenerated on Linux, not macOS) applies to any `npm install`/`npm audit fix` run here too.

### 3. Check for plans/mockups (main only)

Only when **base-branch is `main`** (or was left as the default): check `docs/plans/` and `docs/plans/mockups/` for anything new or changed on this branch —

```
git diff <base-branch>...HEAD --name-status -- docs/plans/
git status --porcelain -- docs/plans/
```

These are working documents for the feature, not project history — by default they should be deleted before the PR merges to main, so they don't carry forward once the feature ships. For each file or mockup folder found:

- Delete it, **unless** there's a concrete reason to keep it (e.g. the plan doubles as ongoing design reference for a phased/multi-PR feature, or the mockups are linked from a doc that stays). If so, keep it and call out the reason explicitly in the Pre-PR Review output (a new **📐 Plans & Mockups** section — see step 6) rather than deleting silently.
- If deleted, note it in that same section so the user can see what was removed and why.

A plan that spans several PRs (such as `docs/plans/claude-agents.md`) is a concrete reason to keep it. Say so in the section below, or delete it when the last PR ships.

Skip this step entirely when base-branch is not `main` (e.g. merging into a long-lived feature/integration branch, where plans may still be useful to the next PR in the chain).

### 4. Run the reviewers in parallel

Read `.github/instructions/pull-request-review.instructions.md` only as far as you need to judge a disputed finding. The agents read it for their own layer. Every flag in the final review must trace to a rule in that file, CLAUDE.md, or `docs/learnings/`.

Pick agents from the step 1 file list and start them **in one message** so they run in parallel:

| Agent | Run when the diff has |
|---|---|
| `convention-reviewer` | always |
| `test-writer` (review mode) | always: checks every changed source file has a test and the test rules hold |
| `efcore` | anything under `Database/`, a migration, an `*Configuration.cs`, or an EF query change |
| `backend-api` | anything under `src/Neba.Api/Features/` or `src/Neba.Api.Contracts/` |
| `blazor-ui` | a `.razor`, `.razor.js`, or `Neba.Website.Server` change |
| `aspire` | anything under `src/Neba.AppHost/` or `aspire.config.json` |

Give each agent the same brief: "Review mode. Review only the changes in `git diff <base-branch>...HEAD`. Change no files. Report findings only, one per line: `path:line | rule | Blocker, Should Fix or Suggestion | one-line fix`. End with `Not checked:`." If the branch is checked out, say so; the agents run their greps against the working tree.

### 5. Merge the findings

- Dedupe by file and line first, then by rule or wording. Two agents often report one issue in different words. Keep the clearer line and the more severe level.
- Map each finding into 🚫 Blockers, ⚠️ Should Fix and 💡 Suggestions below. Severity meanings:

| Level | Meaning |
|---|---|
| 🚫 **Blocker** | Violates a hard architectural rule, misses a required element (auth, error handling, test trait, a test for a changed source file), or risks security or correctness. Fix before opening the PR. |
| ⚠️ **Should Fix** | Clearly breaks a convention but is unlikely to fail at runtime (missing `DisplayName`, unsealed class). Fix unless there is a reason not to. |
| 💡 **Suggestion** | Nice to have; breaks no rule. |

- A new or changed source file with no test is a 🚫 Blocker. Name the file and say it has zero coverage.
- Collect every agent's `Not checked:` line. Report anything that matters under Suggestions as "not reviewed: ...", so a gap is visible.
- If an agent fails or returns nothing, say so in the review. Do not treat silence as a pass.
- Check the findings yourself before you report them. An agent can be wrong; if a finding contradicts the review instructions, drop it.

These stay in the skill because they are not layer work: `npm audit` (step 2), the plans and mockups cleanup (step 3), the README checks and follow-up review recommendations below, and the PR description.

**Follow-up review recommendations**

`code-review`, `security-review`, and `performance-review` are separate slash commands — this skill doesn't run them, it recommends whether to. Decide each independently; a PR can warrant more than one, or none.

- **Code review** — recommend `/code-review` (pick an effort level: `medium` for a routine single-feature PR, `high`/`xhigh` for a large diff spanning multiple layers or a critical/high-stakes feature per `feature-plan`'s criticality determination) whenever the diff includes non-trivial logic — new/changed handlers, domain code, or components. Skip the recommendation only for diffs that are entirely docs/README/config, pure test additions with no production code change, or trivial copy/label fixes — the fixed checklist above already covers those well enough that an open-ended pass adds little.
- **Security review** — recommend `/security-review` when the diff includes any of: new or changed authentication/authorization logic (policies, roles, claims); a new or changed endpoint accepting user input; secrets/connection-string/config handling; a new external integration or third-party API call; file upload/download handling; new or changed PII/sensitive-data handling; a new or upgraded dependency. Don't recommend it for pure UI styling, test-only changes, docs-only changes, or an internal refactor that touches none of the above surfaces.
- **Performance review** — recommend `/performance-review` when the diff includes any of: a new or changed EF Core query, especially over a collection, join, or list endpoint; a new or changed cached query (`ICachedQuery`/`IFusionCache` usage added, removed, or its key/invalidation logic touched); a new background job (Hangfire) or a loop making repeated external calls; a new Blazor component rendering a collection, or new per-keystroke/high-frequency interactive behavior; any change to a known hot path (an endpoint or page called on every page load, or by every user). Don't recommend it for pure copy/label changes, test-only changes, docs/config-only changes, or a bounded single-entity create/edit form with no list rendering.

**README**
- [ ] `README.md`'s Project Structure reflects any new/removed top-level `Features/{Feature}` folders, projects, or render-mode changes
- [ ] `README.md`'s Technology Stack reflects any new/removed package that changes what's user-visible (new datastore, new client library, new background job engine, etc.) — not every `Directory.Packages.props` bump, just ones that change the stack story
- [ ] `README.md`'s Implementation Plan checkboxes reflect features this PR completes or starts (check off finished items, leave partial work unchecked)

### 6. Present the review

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
[Only present when base-branch is `main`. List each file/folder under `docs/plans/` deleted in step 3, and each one kept along with its stated reason. If step 3 found nothing under `docs/plans/`, omit this section entirely.]

### 📄 README Updates
[List each stale/missing spot in README.md found via the **README** checklist above, with the proposed change. If none: "None — README is current."]

### 🔍 Recommended Follow-up Reviews
[For each of Code Review, Security Review, and Performance Review: state **Recommended** or **Not recommended**, then one to two sentences of reasoning tied to the matching bullet in the **Follow-up review recommendations** checklist above — name the specific file(s)/pattern(s) that triggered (or didn't trigger) the call. For each recommended one, name the exact command to run (e.g. `/code-review high`, `/security-review`, `/performance-review`).]

### ✅ Looks Good
[Brief note on what was done well or what was verified clean.]

---

Write this review verbatim to `pr-review.md` at the repo root (overwrite if it already exists) so it's available for reference when addressing the findings later. Then show the same content in the chat response.

Ask the user: **"Ready to generate the PR description, or would you like to address any of these first?"**

### 7. Apply README updates

If step 6 found any README Updates, apply them directly to `README.md` now (unless the user said they'd handle findings themselves) — these are typically small, mechanical (a checkbox, a stack line, a folder in the structure diagram) and don't warrant a separate round-trip. Show a brief summary of what changed. Skip this step entirely if the README Updates list was empty.

### 8. Generate the PR description

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
