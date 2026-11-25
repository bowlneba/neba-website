---
name: release-notes
description: Generate user-facing GitHub release notes (markdown file) for the changes between main's HEAD and the current branch (the state that will merge into main). Asks for the new version number, derives a release title, groups changes by feature with PR references, and appends a Full Changelog compare link. For major and minor versions, infers the new user-facing features from the changes and works through them with the user before drafting, so the notes can serve as a reference for member communications. Not for PR descriptions (write-pr) or publishing the release. Usage: /release-notes [new-version]
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

Then set the **release type** by comparing the new version to the previous release tag (`git describe --tags --abbrev=0`):
- **Major or minor** (the major or minor number went up, e.g. `v1.1.5` to `v1.2.0`): use the **feature-driven flow** (steps 3a-4a). These notes are the reference point for what the new website can do, so staff can reuse them in messages to members.
- **Patch** (only the patch number went up): skip 3a and 4a and use the standard flow below.

### 3. Gather the changes

`<base>` is `origin/main` and `<target>` is the current branch's `HEAD` throughout this skill (get the branch name with `git branch --show-current` if needed for the compare link).

```
git log <base>..<target> --oneline
```

Each entry from a squash-merged PR looks like `<sha> <Title> (#<number>)`. For every PR number found:

```
gh pr view <number> --json title,body,labels
```

Skip a PR labeled `dependencies` (Dependabot bumps) when it only bumps dependencies — these don't belong in user-facing notes. Read the body first: a PR can carry that label next to real work.

For the remaining PRs, read the `body` in full — it's the primary source of what to write from (these repos write structured PR descriptions with Summary/What Changed sections). Use `git log <base>..<target> --stat` only as a cross-check that nothing substantive was missed (e.g. a merge commit with no PR body, a direct commit on the branch).

### 3a-3b. Infer the features and talk them through (major/minor only)

Read `references/feature-driven-flow.md`. Work out what members, bowlers and admins can now do, build a candidate list of headline features, smaller improvements and under-the-hood items, and agree it with the user before you draft. Do not draft first.

### 4. Draft `release-notes.md`

For a **major or minor** release, use the feature-driven structure in 4a. For a **patch**, use the standard structure below.

Write **for a user of the site/app**, not a developer reading a diff — describe what changed in the product, not which classes or files changed. The file has no title and no H1: the title goes only in the `gh release create --title` flag (step 5), so it does not repeat in the release body. The file opens directly with the first `###` section — no intro/framing sentence.

Structure:

```markdown
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
- Name anything a person must do to run this release: new secrets, variables, env vars, or a migration to run. Put it in the section it belongs to, or under a `### Setup notes` section when it spans several.
- Do not add a "What's not here yet" / deferred-items section.
- Do not add a top-of-file summary sentence — the section headings and the changelog link carry that.
- Get `<owner>/<repo>` from `gh repo view --json nameWithOwner -q .nameWithOwner`.
- The compare link's base is the previous release tag (`git describe --tags --abbrev=0`), not `origin/main` — this gives the reader a full diff since the last published release, separate from step 1's content-gathering range.
- Look at the tone/structure of the previous release (`gh release view <previous-release-tag>`) if one exists, and stay consistent with it.

### 4a. Feature-driven structure (major/minor only)

Use the list agreed in step 3b and the template in `references/feature-driven-flow.md`: headline sections first, then **Also in this release**, then **Under the hood**. Show the headline sections to the user for edits before step 6.

### 5. Derive the release title

The release title is always in the form `<new-version>: <title phrase>` — the new version, a colon, and a short (3-6 word) title phrase summarizing the release's theme, derived from the feature areas covered — e.g. `v0.2.0: Staff & Admin Tools`. Do not ask the user for this phrase; propose it based on the drafted content. For a major or minor release, base it on the headline features (e.g. `v1.2.0: Team Eligibility & Hall of Fame Points`). This full title goes in the `--title` flag of the `gh` command (step 6), never in `release-notes.md`.

### 6. Confirm

Tell the user:
- `release-notes.md` has been written at the repo root (body only, no title line)
- The file is ready for a GitHub release (tag `<new-version>`, title = the release title from step 5)
- Which ref and range the notes cover, and any commits left out and why
- Print this command in a code block, filled in, every time, for them to copy and run. Use `--target main` for a release cut after the PR merges; for a hotfix tagged by hand after deploy, the tag already exists, so omit `--target`. Add `--prerelease` when the major version is 0 or the version has an `-rc.N` suffix:

```
gh release create <new-version> --target main --title "<release title>" --notes-file release-notes.md [--prerelease]
```

- Tell them to run it only after the PR merges (and, for a hotfix, after the deploy), then delete `release-notes.md` (it should not be committed)
- Never run `gh release create` yourself. Publishing is the owner's step, and it creates the tag.

## Gotchas

- The file has no H1 and no intro sentence. The title goes only in the `--title` flag.
- The range base is `origin/main`, not the last tag. The compare link base is the last tag.
- A PR labeled `dependencies` can carry real work. Read its body before you skip it.
- Never run `gh release create`. It creates the tag, so publishing is the owner's step.
- Do not add a deferred-items section, and do not mention a feature the user dropped.
