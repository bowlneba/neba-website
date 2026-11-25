# PR description format

Read this in step 8.

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
- When step 3b applies, end the description with a **Post-merge actions** section that holds the same unticked checklists and the variable table, so the person who merges sees them
- Keep **Summary** bullets tight — what it does, not how
- **What Changed** should be detailed enough that a reviewer can map each section to the diff; include entity names, table names, query key, route, and page path where relevant
- **Test Plan** should be actionable checkboxes — include the `dotnet test --filter` command for the component, specific test classes worth calling out, and a manual smoke-test step
- Write in the same voice as the existing PR descriptions — confident, specific, not over-explained
- Do NOT add a "Co-authored-by" or AI attribution line

Output the description inside a fenced markdown block so the user can copy it directly.
