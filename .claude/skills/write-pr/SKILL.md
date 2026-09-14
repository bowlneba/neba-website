---
name: write-pr
description: After pull-request-prep has been run and any blockers addressed, generate a final PR description and write it to pr-description.md at the repo root. Usage: /write-pr
---

Generate the final PR description for the current branch and write it to `pr-description.md` at the repo root so it can be pasted directly into GitHub.

Run this after `/pull-request-prep` has been completed and any blockers have been addressed.

## Steps

### 1. Gather context

```
git log main...HEAD --oneline
git diff main...HEAD --stat
```

If the diff is large, group by layer when reading: Domain → Application → Infrastructure → API → Contracts → Blazor/Website → Tests → CI/Tooling → Docs.

### 2. Check available labels

```
gh label list --limit 100
```

Compare the branch's actual changes (commits + diff) against the repo's existing labels and pick the ones that genuinely fit (e.g. `bug`, `enhancement`, a feature-area label). Don't force a label onto a PR it doesn't fit.

If nothing in the existing label set adequately captures what this PR is, recommend creating a new one — but only when there's a real, recurring case for it (e.g. this is the second or third PR touching a distinct area/theme with no matching label), not for a one-off. State the proposed label name and color/description if suggesting creation. Do not create the label yourself — this is a recommendation for the user to act on.

### 3. Write `pr-description.md`

Write the file to the repo root using the format below. Overwrite if it already exists.

```markdown
## Summary

[2–4 bullets at a high level. Lead with the feature name bolded for feature PRs. Keep bullets tight — what it does, not how.]

## Context

[Optional — only include if the domain problem or motivation isn't obvious from the code. 1–3 sentences. Omit if unnecessary.]

## What Changed

[Organized by layer. Only include layers that actually changed. Use sub-bullets for detail. Include entity names, table names, query key, route, and page path where relevant.]

### Domain
### Application
### Infrastructure
### API
### Contracts
### Blazor (`Neba.Website.Server`)
### Tests
### CI / Tooling
### Docs

## Test Plan

- [ ] `dotnet test --filter "Category=Unit"` — all unit tests pass
- [ ] `dotnet test --filter "Category=Integration"` — all integration tests pass
- [ ] `dotnet test --filter "Component=<Feature>"` — component tests pass
- [ ] [any specific test class or scenario worth calling out]
- [ ] Navigate to `/<route>` in the running app and verify [behaviour]

## Deferred

[Optional — list anything explicitly left out of scope. Omit if nothing is deferred.]
```

Rules:
- **Summary** bullets: what it does, not how
- **What Changed**: detailed enough to map each section to the diff; be specific about names
- **Test Plan**: actionable checkboxes; include the `dotnet test --filter` command for affected components
- Do NOT add a "Co-authored-by" or AI attribution line
- Omit any section that has no content

### 4. Confirm

Tell the user:
- `pr-description.md` has been written
- The file is ready to copy and paste into GitHub
- Remind them to delete `pr-description.md` after opening the PR (it should not be committed)
- The label(s) recommended from step 2, with a one-line reason each; if recommending a new label, say so explicitly and note it needs to be created first
