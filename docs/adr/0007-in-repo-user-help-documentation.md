# ADR-0007: In-Repo User Help Documentation

## Status

Proposed

## Context

The application is gaining user-facing administrative commands (the first being article deletion, on the `articles-delete` branch). As more commands ship, admins and eventually end users will need instructions on how to use each one. Two structural questions needed to be settled before writing the first help doc:

1. **Where does help documentation live?**
2. **How do we make sure every new command actually gets a help doc, instead of this becoming an aspiration that quietly lapses?**

### Option considered: GitHub Wiki as a submodule

A GitHub repository wiki is itself a separate git repository (`<repo>.wiki.git`). It's possible to add it to the main repo as a git submodule, so wiki content is checked out alongside the code.

This was rejected:

- **Wiki edits don't flow through PR review.** Someone can edit the wiki directly through GitHub's web UI, bypassing the repo's PR process entirely. Even if edits are made through the submodule, the wiki repo and the main repo are two separate commit histories — the submodule pointer bump is a second, easy-to-forget commit that can drift out of sync with the code it documents.
- **Enforcement becomes impossible.** The goal is "every new command ships with a help file," enforced via `pull-request-review.instructions.md`. That instruction file can only reason about the diff of the PR it's reviewing — it has no visibility into whether a separate wiki repo was also updated.
- **Docs and code version independently.** A wiki has no concept of "as of commit X" — it's just whatever the latest page says. Help content should move in lockstep with the feature it documents, reviewed in the same PR.

### Option chosen: docs directory in the main repo

The repo already has an established pattern for structured documentation living alongside code: `docs/adr/`, `docs/architecture/`, `docs/api/`. Help documentation follows the same convention.

## Decision

### Location

Help docs live at `docs/help/<feature-or-command-name>.md`, one file per user-facing command or feature (e.g., `docs/help/delete-article.md`). Screenshots live alongside, in `docs/help/images/<feature-or-command-name>/`.

This is source-of-truth documentation reviewed in the same PR as the code it describes. It is **not** currently rendered anywhere public-facing — see Deferred, below.

### Required content per file

Each help doc should contain, in order:

1. **Title and purpose** — one line: who this is for and what it lets them do.
2. **Prerequisites** — required role/permission to perform the action (link to the relevant row/file in `docs/policies/` — see [ADR-0008](0008-policy-documentation-structure.md)).
3. **Steps** — numbered, concrete steps a user follows in the UI to complete the task.
4. **Screenshots** — at least one screenshot per meaningfully distinct UI state encountered during the steps (see below).
5. **Troubleshooting / edge cases** — errors a user might hit (e.g., permission denied, validation failure) and what they mean.
6. **Related** — links to other help docs for closely related features.

### Screenshots

Screenshots are generated with Playwright, since the project already has E2E coverage (`tests/e2e/*.spec.ts`) that drives these same flows.

Screenshot generation is **kept separate from assertion specs**:

- A dedicated script (e.g. `tests/e2e/help-screenshots.spec.ts` or an `npm run docs:screenshots` task) navigates each documented flow and writes `page.screenshot()` output into `docs/help/images/<feature>/`.
- This does **not** run as part of normal CI. Screenshots are a documentation artifact, not a correctness check — gating merges on pixel diffs is a different, noisier problem than "does the doc still match the UI," and would create flaky, unrelated CI failures.
- Regenerating screenshots is a manual step, prompted by the PR review checklist (see below) whenever a documented feature's UI changes.

### Enforcement

`pull-request-review.instructions.md` is updated to require: any PR introducing a new user-facing command, or changing the UI of an already-documented one, must include a corresponding update under `docs/help/`.

## Deferred

**Public-facing rendering.** Help docs are repo-side only for now (viewed on GitHub). Before go-live, this content needs to be surfaced to actual end users somewhere in the app (e.g. a `/help` section). This ADR does not decide that mechanism — it only ensures the underlying content exists, is versioned with the code, and is written in plain Markdown, so no rework is needed to point a future renderer at `docs/help/`. Tracked as a pre-go-live follow-up.

## Consequences

### Positive

- Help docs are reviewed with the same rigor as code, in the same PR, by the same reviewers.
- No risk of docs and code drifting apart across two separate repositories.
- Screenshot generation reuses existing E2E test infrastructure instead of a new tool.
- Plain Markdown with a stable location (`docs/help/`) means a future public-facing renderer (Blazor markdown component, static site generator, wiki mirror) can consume this content without a rewrite.

### Negative

- No public-facing help exists yet — purely internal until the deferred work above is scheduled.
- Screenshots are only as fresh as the last manual regeneration; nothing currently fails CI if a screenshot goes stale. This is accepted as a checklist/process gap rather than an automated one, to avoid noisy pixel-diff CI failures.

## Related Decisions

None yet.
