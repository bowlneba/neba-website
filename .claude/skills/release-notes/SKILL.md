---
name: release-notes
description: Generate user-facing GitHub release notes (markdown file) for the changes between the latest tag and the current branch/main. Asks for the new version number, derives a release title, groups changes by feature with PR references, and appends a Full Changelog compare link. Usage: /release-notes [new-version]
---

Generate release notes for a new release and write them to `release-notes.md` at the repo root, ready to paste into a GitHub release.

## Steps

### 1. Determine the range

```
git describe --tags --abbrev=0
```

This is the base tag (the previous release). If `$1` names an explicit base tag, use that instead. The target is `main` unless the user says otherwise (e.g. the current branch, if it's ahead of main and that's what's being released).

Confirm the resolved range with the user before proceeding if there's any ambiguity (e.g. more than one plausible base tag, or the current branch isn't main).

### 2. Get the new version number

If not passed as an argument, ask the user for the new version (e.g. `v0.2.0`). Required — do not guess a version number.

### 3. Gather the changes

```
git log <base-tag>..<target> --oneline
```

Each entry from a squash-merged PR looks like `<sha> <Title> (#<number>)`. For every PR number found:

```
gh pr view <number> --json title,body,labels
```

Skip any PR labeled `dependencies` (Dependabot bumps) — these don't belong in user-facing notes.

For the remaining PRs, read the `body` in full — it's the primary source of what to write from (these repos write structured PR descriptions with Summary/What Changed sections). Use `git log <base-tag>..<target> --stat` only as a cross-check that nothing substantive was missed (e.g. a merge commit with no PR body, a direct commit to main).

### 4. Draft `release-notes.md`

Write **for a user of the site/app**, not a developer reading a diff — describe what changed in the product, not which classes or files changed. No intro/framing sentence at the top; start directly with the first section.

Structure:

```markdown
### <Feature Area> (#<pr>[, #<pr>...])

<1-3 sentences or a short bullet list describing what changed, in plain language.>

### <Feature Area 2> (#<pr>)

...

### Under the hood

- **<Item>** (#<pr>) — <what it does and why it matters, kept brief>
- ...

**Full Changelog**: https://github.com/<owner>/<repo>/compare/<base-tag>...<new-version>
```

Rules:
- One `###` section per feature area a user would recognize (a page, a workflow, a capability) — not one section per PR. If multiple PRs contributed to the same feature area, list all their numbers in that section's heading.
- Reference every included PR by number (`#123`) in the section it belongs to — GitHub auto-links these in release bodies for this repo, no need for full URLs.
- **Under the hood** is for infrastructure/compliance/tooling work that doesn't map to a user-facing feature (auditing, redaction, dependency-upgrade-review tooling, shared component groundwork, etc.) — still PR-referenced, but terser than the feature sections.
- Do not add a "What's not here yet" / deferred-items section.
- Do not add a top-of-file summary sentence — the section headings and the changelog link carry that.
- Get `<owner>/<repo>` from `gh repo view --json nameWithOwner -q .nameWithOwner`.
- Look at the tone/structure of the previous release (`gh release view <base-tag>`) if one exists, and stay consistent with it.

### 5. Derive the release title

The release title is the new version followed by a colon and a short (3-6 word) title phrase summarizing the release's theme, derived from the feature areas covered — e.g. `v0.2.0: Staff & Admin Tools`. Do not ask the user for this phrase; propose it based on the drafted content. Surface the proposed title to the user alongside the notes so they can adjust it.

### 6. Confirm

Tell the user:
- `release-notes.md` has been written at the repo root
- The proposed release title
- The file is ready to copy into a GitHub release (tag `<new-version>`, title as above) — remind them to delete `release-notes.md` after publishing (it should not be committed)
