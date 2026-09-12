---
name: release-notes
description: Generate user-facing GitHub release notes (markdown file) for the changes between main's HEAD and the current branch (the state that will merge into main). Asks for the new version number, derives a release title, groups changes by feature with PR references, and appends a Full Changelog compare link. Usage: /release-notes [new-version]
---

Generate release notes for a new release and write them to `release-notes.md` at the repo root, ready to paste into a GitHub release.

## Steps

### 1. Determine the range

The base is always **main's HEAD** (`git fetch origin main --quiet` then use `origin/main`), not the previous release tag — the current checked-out branch is the state that will actually merge into main, so the range must show only what's new relative to main. If `$1` names an explicit base ref, use that instead. The target is always the **current branch's HEAD**.

Confirm the resolved range with the user before proceeding if there's any ambiguity.

### 2. Get the new version number

If not passed as an argument, ask the user for the new version. Required — do not guess a version number.

Accept either `X.Y.Z` or `vX.Y.Z`. Normalize to a canonical form and use it consistently for the rest of the run:
- Tag/version references (title, compare link) use the `v` prefix (e.g. `v0.2.0`), matching this repo's tag convention.
- If the input already has a `v` prefix, keep it; if not, add one.

### 3. Gather the changes

`<base>` is `origin/main` and `<target>` is the current branch's `HEAD` throughout this skill (get the branch name with `git branch --show-current` if needed for the compare link).

```
git log <base>..<target> --oneline
```

Each entry from a squash-merged PR looks like `<sha> <Title> (#<number>)`. For every PR number found:

```
gh pr view <number> --json title,body,labels
```

Skip any PR labeled `dependencies` (Dependabot bumps) — these don't belong in user-facing notes.

For the remaining PRs, read the `body` in full — it's the primary source of what to write from (these repos write structured PR descriptions with Summary/What Changed sections). Use `git log <base>..<target> --stat` only as a cross-check that nothing substantive was missed (e.g. a merge commit with no PR body, a direct commit on the branch).

### 4. Draft `release-notes.md`

Write **for a user of the site/app**, not a developer reading a diff — describe what changed in the product, not which classes or files changed. The file opens with the release title as an H1 (see step 5 for how the title phrase is derived) — no other intro/framing sentence.

Structure:

```markdown
# <new-version>: <title phrase>

### <Feature Area> (#<pr>[, #<pr>...])

<1-3 sentences or a short bullet list describing what changed, in plain language.>

### <Feature Area 2> (#<pr>)

...

### Under the hood

- **<Item>** (#<pr>) — <what it does and why it matters, kept brief>
- ...

**Full Changelog**: https://github.com/<owner>/<repo>/compare/<previous-release-tag>...<new-version>
```

Rules:
- One `###` section per feature area a user would recognize (a page, a workflow, a capability) — not one section per PR. If multiple PRs contributed to the same feature area, list all their numbers in that section's heading.
- Reference every included PR by number (`#123`) in the section it belongs to — GitHub auto-links these in release bodies for this repo, no need for full URLs.
- **Under the hood** is for infrastructure/compliance/tooling work that doesn't map to a user-facing feature (auditing, redaction, dependency-upgrade-review tooling, shared component groundwork, etc.) — still PR-referenced, but terser than the feature sections.
- Do not add a "What's not here yet" / deferred-items section.
- Do not add a top-of-file summary sentence — the section headings and the changelog link carry that.
- Get `<owner>/<repo>` from `gh repo view --json nameWithOwner -q .nameWithOwner`.
- The compare link's base is the previous release tag (`git describe --tags --abbrev=0`), not `origin/main` — this gives the reader a full diff since the last published release, separate from step 1's content-gathering range.
- Look at the tone/structure of the previous release (`gh release view <previous-release-tag>`) if one exists, and stay consistent with it.

### 5. Derive the release title

The release title is always in the form `<new-version>: <title phrase>` — the new version, a colon, and a short (3-6 word) title phrase summarizing the release's theme, derived from the feature areas covered — e.g. `v0.2.0: Staff & Admin Tools`. Do not ask the user for this phrase; propose it based on the drafted content. This full title is what goes in the H1 at the top of `release-notes.md` (step 4) — draft it before or alongside the rest of the file, not as an afterthought.

### 6. Confirm

Tell the user:
- `release-notes.md` has been written at the repo root, with the release title as its first line
- The file is ready to copy into a GitHub release (tag `<new-version>`, title = the H1 line minus the `# `) — remind them to delete `release-notes.md` after publishing (it should not be committed)
