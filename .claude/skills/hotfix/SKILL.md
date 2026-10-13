---
name: hotfix
description: Preflight a hotfix branch before it ships through hotfix.yml. Reviews the commits since the production tag, confirms the reason for the hotfix, checks the branch is safe to fast-deploy (no migrations, small blast radius), verifies the changes have regression tests that fail without the fix, runs affected tests locally, writes the PR description and bug report, and prints the push/dispatch/PR commands. Never pushes, dispatches, or opens a PR. Usage: /hotfix [base-tag]
---

Run this on a `hotfix/*` branch after committing the fix and before pushing. It answers one question: is this change safe and tested enough to skip the normal pipeline? The fast path (`.github/workflows/hotfix.yml`) skips Playwright, Sonar, and the format check, so this review is the part a human would otherwise do by eye. Read `docs/learnings/hotfix.md` for the flow it supports.

This skill only reads, runs tests, and writes local files. It never runs `git push`, `gh workflow run`, `gh pr create`, or `git tag`. It prints those commands for the user.

## Arguments

- **base-tag** (optional) — the tag production runs. Default: the highest `v*` tag (`git tag --list 'v*' --sort=-v:refname | head -1`). Use the same value the workflow's `base_tag` input would get.

## Step 1 — Establish the state

1. Branch must match `hotfix/*`. If not, stop and say how to cut one (`git switch -c hotfix/<date>-<slug> <tag>`). Do not move commits for the user.
2. Resolve the base tag. Stop if there is none.
3. `git merge-base --is-ancestor <tag> HEAD` must pass. If not, stop: deploying this would drop code production already runs.
4. Review range is `<tag>..HEAD`. That is everything the deploy ships. Also report which of those commits are unpushed (`@{u}..HEAD`, or all of them when there is no upstream).
5. `git status --short`. Uncommitted changes are not shipped. List them and ask whether they belong in the hotfix.

## Step 2 — Read the change

`git log --reverse --format='%h %s%n%b' <tag>..HEAD`, `git diff --stat <tag>..HEAD`, then the full diff. Classify each changed file: production code, test, test factory, config, dependency, infra/workflow, docs.

## Step 3 — Reason for the hotfix

Always infer it, then confirm it. Show the user:

- **What broke** (observed symptom, in plain words) and **what the fix does**, as you read them from commits and diff.
- **Confidence**: say plainly when the diff and commit messages don't make the reason clear (vague messages, mixed concerns, a change that doesn't explain its trigger).

Then ask for what a diff can't know, in one `AskUserQuestion` or a short list:

- What did people see, and who was affected (tournament directors, bowlers, public)?
- When did it start, and is it tied to a recent deploy?
- Is any workaround in place now?
- Why can't this wait for the normal pipeline?

Do not invent answers. If the user can't say, record "unknown" rather than guessing.

## Step 4 — Preflight

Hard stops (the workflow refuses these, so say so):

- **Migrations.** Any changed path matching `migrations` (case-insensitive). The hotfix path never runs migration jobs. Point to the normal pipeline.

Findings to report (user decides):

- **Blast radius.** Files outside the fix: unrelated edits, formatting churn, dependency bumps (`Directory.Packages.props`, `package*.json`), config or `appsettings` changes, `AppHost.cs`, anything under `.github/`. Smaller is safer; recommend splitting out anything not needed for the fix.
- **Rollback.** State the revert command (`git revert --no-edit <sha>` for each commit) and whether the change is safe to revert: no schema, data, cache-shape, or contract change that older code can't read. Call out any cached DTO or API contract change.

## Step 5 — Test review (blocking)

For each changed production file, find the test that covers the changed behavior. Use the repo's mapping from CLAUDE.md:

| Changed code | Test project |
|---|---|
| `src/Neba.Api/Features/**` (domain, handler, validator, endpoint) | `tests/Neba.Api.Tests` (unit; integration for EF or endpoints) |
| `src/Neba.Api/Database/**` | `tests/Neba.Api.Tests` (integration) |
| `src/Neba.Website*/**` components | `tests/Neba.Website.Tests` (bUnit) |
| `*.razor.js`, other JS | Jest |
| UI flow | `tests/e2e` |

A change is covered only when a new or modified test in `<tag>..HEAD` asserts the fixed behavior. An existing test that happens to execute the line does not count. Report a table: file or behavior, covering test, verdict (covered, thin, missing).

Run the `convention-review` skill on the diff and fold its findings in. Tests must follow the repo rules: `[UnitTest]`/`[IntegrationTest]` and `[Component]` traits, `DisplayName`, AAA comments, `MockBehavior.Strict`, Shouldly, `Neba.TestFactory` factories.

**Blocking.** If any behavior is missing or thin, stop and list the gaps. The user either adds tests (then re-run) or gives a reason to ship without them. Record that reason verbatim in the PR block (Step 8). Do not write the tests unless the user asks.

## Step 6 — Red check (new tests must fail without the fix)

Prove each new or changed test guards the fix. Never touch the user's working tree:

1. `git worktree add <scratchpad>/hotfix-base <tag>` (detached).
2. Copy into it only the test files and test factories changed in `<tag>..HEAD`.
3. Run the matching tests there (component filter, below).
4. Remove the worktree (`git worktree remove --force`), even when a step fails.

Interpret the result:

- **Fails on an assertion** — good. The test guards the fix.
- **Passes** — the test does not prove the fix. Treat as a gap and say so.
- **Does not compile** — the test needs the new API. That proves the code is new but not the behavior. Report as "unproven" and let the user decide.

Integration tests need Docker. If Docker is down, say the red check is skipped for those tests; do not report them as passing.

## Step 7 — Local tests

Run only the components the diff touches, with the HEAD source:

```bash
dotnet test --filter-trait "Component=<FeatureName>"
```

Add `npm test` when JS changed. The MTP runner exits 8 ("Zero tests ran") for a project with no matching trait; read each project's summary line, not the overall exit code. Report failures with their output. Stop on any red test.

## Step 8 — Write the artifacts

Derive `<slug>` from the branch (`hotfix/2026-10-12-scoring-crash` becomes `scoring-crash`) and confirm it.

1. **PR block** — `docs/bug-reports/<slug>/pr-description.md`: reason and impact (from Step 3), what changed, tests added and the red-check result, blast-radius notes, rollback command, and any waiver reason for missing tests. Note that the hotfix was deployed through `hotfix.yml` ahead of this PR. End with the repo's PR attribution line.
2. **Bug report** — run the `bug-report` skill with the slug. It writes `issue.md` and the closing comment. Feed it the Step 3 answers so the symptom is not re-guessed.

## Step 9 — Verdict and commands

Open with one line: READY, READY WITH NOTES, or BLOCKED, and the one-line reason. Then print, in separate `bash` blocks, for the user to run:

```bash
git push -u origin HEAD
```

```bash
gh workflow run hotfix.yml --ref "$(git branch --show-current)"
```

```bash
gh pr create --base main --title "<title>" --body-file docs/bug-reports/<slug>/pr-description.md
```

Remind the user, in this order:

1. Approve the `Production` environment when the workflow reaches deploy.
2. Tag the deployed commit by hand once the deploy succeeds.
3. Merge the PR to `main`. If the fix never reaches `main`, the next `main` deploy reverts it.
