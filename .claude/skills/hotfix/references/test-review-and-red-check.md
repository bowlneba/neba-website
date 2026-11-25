# Test review and red check (steps 5 and 6)

Read this at steps 5 and 6. Step 5 blocks the hotfix until each fixed behavior has a test.

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
