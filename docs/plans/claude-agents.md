# Claude agents and skills rework

Status (2026-11-18): Phases 0 to 6 are built on branch `agentCleanup`, nothing pushed. Verified means run or tested here; the rest is open.

| Phase | State | Verified | Not yet verified |
|---|---|---|---|
| 0 Marketplace | Done | Plugins enabled; skills load | none |
| 1 Foundations | Done | References updated; no stale names left | none |
| 2 First-wave agents (6) | Done | Each ran a read-only trial; plugin skills load through the Skill tool | `efcore` `migrations add` command (inferred; `list` works); the postgres MCP server (it was down) |
| 3 Rewiring | Done | Headings the agents cite exist | `pull-request-prep` and `feature-plan` not run end to end |
| 4 Task skills (5) | Done | Every named file and claim checked against the code | none run on a real task |
| 5 Second-wave agents (4) | Written | none | `ui-design`, `e2e`, `docs`, `devops` have never run: they were not loaded in the session that wrote them |
| 6 Hooks | Written | The three scripts, by hand, against about 50 commands | The wiring: settings and frontmatter hooks were added after this session started, so no running agent or session used them. Hooks fail open (a missing script does not block). |

After a restart, check in this order:

1. `git commit --dry-run` with a staged use-case-to-use-case dependency (see the commit message of c140c139) should be blocked by the architecture hook.
2. Ask `convention-reviewer` to run `touch` on a scratch file. The hook should block it.
3. Ask `devops` to run `git push --dry-run origin HEAD`. The hook should block it.
4. Run a read-only trial of `ui-design`, `e2e` and `docs`, as the first six had.
5. Run `/pull-request-prep` on a branch with real C# and Razor changes.

Decisions made along the way: writers inherit the session model, and `convention-reviewer` uses `sonnet`. Subagents can nest according to the docs, but the main-session design stays. Plugin skills are named in agent bodies for on-demand use, not preloaded. `.github/skills` holds mirrored copies of the two trimmed skills; the new skills are not mirrored.


## Why

Claude writes code that compiles and passes tests but breaks the project's design: it injects `DbContext` in an endpoint, throws where we return `ErrorOr`, adds a repository, or puts a validator in the wrong folder. A longer CLAUDE.md does not fix this, because the model skims long files. Two things do:

1. A short AGENTS.md that says what the project is and where things go.
2. Small skills and focused agents that load only for the task at hand, each pointing at real code.

Source: Anton DevTips email "Claude doesn't respect your architecture" (2026-11-18). His benchmark was one run on his own project, so we judge success on our own work (see Measuring).

## Decisions made

| Topic | Decision |
|---|---|
| `/feature-plan` | Layer agents draft their own plan sections. The skill coordinates and you still confirm each stage. The commit plan names the agent that implements each commit. |
| `/pull-request-prep` | Run layer reviewer agents in parallel, chosen from the diff. The skill merges their findings into the existing Blockers / Should Fix / Suggestions report. |
| `ddd-clean-architecture` | Renamed `vsa-ddd` and rewritten as vertical slice architecture with a DDD design approach (step 1.2, done). |
| Scope | First-wave agents, skill rewiring, AGENTS.md and skill audit, second-wave agents, hooks, missing skills. |

## How the pieces fit

- A **skill** is a procedure. It runs in the main session and loads only when needed.
- An **agent** (`.claude/agents/*.md`) is a specialist with its own context, tools and model. It starts cold, so its file must say which docs and skills to read first.
- **Subagent nesting (docs, 2026-11-18; not yet tested here):** the Claude Code docs say a subagent can start subagents (default depth three, `CLAUDE_CODE_MAX_SUBAGENT_SPAWN_DEPTH` changes it), and there is no `agents:` frontmatter field, so the `dotnet-test` `agents:` list is another tool's format. Keep the main-session design anyway; it is easier to see and to stop.
- The agent files list the project docs to read and the one real example file to copy. They do not repeat rules that CLAUDE.md or `docs/learnings/` already hold.

Verified 2026-11-18 against the docs (read through a summarizing fetch): `name` and `description` are required. `tools` is a comma list (`Read, Grep, Glob, Bash`); `disallowedTools` removes tools. `tools` cannot limit Bash to given commands; that takes a `PreToolUse` hook in the agent's `hooks:`. `skills` preloads full skill content (it does not restrict skills). `model` takes `sonnet`, `opus`, `haiku`, `fable`, a full ID, or `inherit`. Subagents load CLAUDE.md unless `omitClaudeMd: true`. **Not documented:** whether `skills` accepts plugin-namespaced names such as `dotnet-test:assertion-quality`. Test that before the `test-writer` agent relies on it.

## Phase 0: Refresh the dotnet/skills marketplace

Do this before anything else, and again right before Phase 2 starts. Every later phase builds on what this marketplace ships, so a stale copy means redoing work.

State on 2026-11-18 (checked, not assumed):

- Marketplace `dotnet-agent-skills` is the GitHub repo `dotnet/skills`. The local clone sits at `2877c71` (merged 2026-11-11). A fetch found 0 newer commits. Installed plugin versions match the clone.
- Installed for this project: `dotnet-aspnetcore` 0.1.2, `dotnet-blazor` 0.1.3, `dotnet-data` 0.1.6, `dotnet-test` 1.0.1, `dotnet-upgrade` 0.1.1, `dotnet11` 0.1.3.
- Available but not installed: `dotnet`, `dotnet-diag`, `dotnet-msbuild`, `dotnet-nuget`, `dotnet-ai`, `dotnet-advanced`, and others.
- **Done 2026-11-18:** the six installed plugins are now enabled in `.claude/settings.json`, and the marketplace is declared there (`extraKnownMarketplaces`), so a fresh clone on another machine picks it up. Before this, only `frontend-design` was enabled. Restart the session for the plugin skills to load.
- The repo's own `dotnet-aspnet-core`, `dotnet-blazor` and `dotnet-entity-framework-core` skills predate the marketplace (you wrote them before you found it). Step 1.3 decides what to do with each.

**Result of the run on 2026-11-18:** all marketplaces updated. `dotnet/skills` is unchanged (still `2877c71`, all six plugins already latest), so the plan needs no re-baseline from it. `azure` moved from 1.0.0 to 1.2.127 (restart to apply); `frontend-design` is already latest. Re-run this phase right before Phase 2, since `dotnet/skills` merges often. Commit the `.claude/settings.json` change as the Phase 0 commit.

Phase 0 is now repeatable as the `marketplace-refresh` skill (`.claude/skills/marketplace-refresh`). It updates every marketplace and plugin, reads what changed, and, when an update was applied, ends with a prompt to paste into a fresh session. Run `/marketplace-refresh` at the start of any later phase that reuses marketplace skills. The steps below are what it does.

Steps:

1. Update the marketplace (`/plugin marketplace update dotnet-agent-skills`) and update each installed plugin. Record the new commit and plugin versions in the commit message.
2. Read what changed since `2877c71` (`git log` and the plugin READMEs). List any skill or agent that overlaps with this plan.
3. Decide per plugin: install, enable in `.claude/settings.json` (shared with the team), or skip. First candidates below.
4. **Re-baseline.** If step 2 changes anything, edit this plan before starting Phase 1 (skills to reuse instead of write, agents to drop or add).
5. Also update `azure-skills` and `claude-plugins-official` while we are here; the Azure skills feed the `devops` and `aspire` agents.

First candidates from `dotnet/skills`:

| Plugin | Useful piece | Feeds |
|---|---|---|
| `dotnet-data` | `optimizing-ef-core-queries` | `efcore` agent (query review) |
| `dotnet-aspnetcore` | `dotnet-webapi` (reference only; it recommends a service layer, so do not preload it) | `backend-api` agent |
| `dotnet-blazor` | `author-component`, `plan-ui-change`, `coordinate-components`, `use-js-interop`, `support-prerendering` | `blazor-ui` agent |
| `dotnet-test` | `assertion-quality`, `test-anti-patterns`, `test-smell-detection`, `grade-tests`, `find-untested-sources`, plus agents `test-engineer` and `test-quality-auditor` | `test-writer`, `convention-reviewer`, `/pull-request-prep` test-coverage check |
| `dotnet-diag` | `optimizing-dotnet-performance` agent | `/performance-review` |
| `dotnet-nuget` | `convert-to-cpm` (we already use central package management; low value) | none |

Caution: `dotnet-test` leans on MSTest examples in places. We use xUnit v3 with Shouldly and strict mocks. Read each skill before relying on it, and let our `testing.md` win on any conflict.

## Phase 1: Foundations

Do these after Phase 0. The agents depend on them.

### 1.1 Rewrite AGENTS.md as a project map

Today AGENTS.md is 70 lines of Aspire text headed "Copilot instructions". Replace it with 80 to 100 lines:

- What the product is (NEBA website: public site, admin, API, legacy Software bridge).
- Solution map: `src/Neba.Api` (vertical slices under `Features/`), `Neba.Api.Contracts`, `Neba.Website.Server`, `Neba.MigrationService`, `Neba.AppHost`, `tests/`.
- Where things go: use-case folder shape, domain folder, Blazor pages, tests, e2e mocks.
- Which skill or agent to use for which task (one table).
- Pointers to `CLAUDE.md`, `docs/architecture/*`, `docs/learnings/*`, `docs/ubiquitous-language.md`.

Move the Aspire text into `.claude/skills/aspire` (check what it already repeats and cut the duplicate). The `aspire` agent (Phase 2) preloads that skill. Fold the `.claude/commands/aspire-debug.md` steps into the agent and delete the command, so one place holds the debug steps. Keep `.github/copilot-instructions.md` pointing at AGENTS.md.

### 1.2 Rename and rewrite `ddd-clean-architecture` as `vsa-ddd`

The skill is 939 lines and teaches clean-architecture layers and repositories. Our rules say vertical slices, no repository layer, and use-case folders that never depend on each other.

Keep (adjust wording to match CLAUDE.md):
- Strategic design: ubiquitous language, bounded contexts as feature folders.
- Aggregates, entities, value objects, strongly-typed IDs.
- Always-valid child entities and cross-aggregate invariants (match `docs/learnings/domain-design.md`).
- `ErrorOr` result pattern, domain events if we use them.
- Endpoint, Summary, Validator folder shape and the endpoint checklist.

Drop or replace:
- The four clean-architecture layers and the project structure section.
- The Repository Pattern section. State plainly: handlers use `DbContext`; readers and mappers live in the using feature's namespace.
- The Testing section (about 200 lines). Link `docs/learnings/testing.md` instead.
- Duplicated NuGet and DI lists that the codebase already shows.

Add: a short "design mentality" section (model the domain first, then the slice; where a rule lives: domain, handler, validator or endpoint) and a "this repo overrides" list.

Target 300 lines or fewer. Update every reference to the old name (search `.claude/`, `.github/`, `docs/`, CLAUDE.md).

### 1.3 Retire or trim the generic skills (decided)

Decision: the repo's `dotnet-*` skills predate the marketplace. They must stay **generic**; repo-specific design lives elsewhere. I compared them with the `dotnet/skills` plugins:

- All three contain **no repo-specific content** (no mention of Neba, `ErrorOr`, Fast Endpoints or repositories), so nothing needs splitting out of them. The repo-specific design is all in `ddd-clean-architecture` (58 hits), which step 1.2 already rewrites.
- The generic text still gets cut where the marketplace covers the same ground.

| Repo skill | Marketplace match | Decision |
|---|---|---|
| `dotnet-blazor` (247 lines, render modes, components, state, Auto data access, JS interop) | `dotnet-blazor`: `author-component`, `coordinate-components`, `fetch-and-send-data`, `support-prerendering`, `use-js-interop`, `configure-auth`, `collect-user-input`, `plan-ui-change` cover all of it, in more depth | **Delete** the repo copy, including its `references/` folder. Repo Blazor rules stay in `docs/learnings/blazor-ui.md` and the `blazor-ui` agent. |
| `dotnet-entity-framework-core` (289 lines) | `dotnet-data:optimizing-ef-core-queries` covers queries, N+1, tracking, bulk updates and index use. It does **not** cover DbContext setup, migrations, data migrations or concurrency | **Trim** to those four gaps (about 120 lines), and link the plugin skill for query and performance work. Keep it generic. |
| `dotnet-aspnet-core` (202 lines: middleware order, options, auth, perf, anti-patterns) | `dotnet-aspnetcore:dotnet-webapi` is a build-an-API walkthrough that recommends a service layer, which conflicts with our slices. It has no middleware, options or security guidance | **Keep** as a generic skill, nothing repo-specific. Trim anything the CLAUDE.md rules contradict. Do not preload `dotnet-webapi` in `backend-api` (it pushes a service layer); use it only for OpenAPI and error-handling ideas, read with care. |

Each deleted or trimmed skill's commit message lists what was dropped. Any generic point a plugin does not cover and we still want goes in the kept skill; any point that is about this repo goes in the agent file or `docs/learnings/`. Examples to look for: repositories, `/api` or versioned routes, `HybridCache` (we use `IFusionCache`), `InputSelect` for long lists, `= []` on EF navigations.

### 1.4 Convert `/new-endpoint` to a skill

`.claude/commands/new-endpoint.md` is 435 lines with code templates inline. Make `.claude/skills/new-endpoint/SKILL.md` short (under 120 lines):
- The steps and the checklist.
- One named real endpoint per case to copy (for example a command like `CreateSponsor` and a query like `ListActiveSponsors`; confirm the best examples when building).
- Status code rules (400 / 422 / 409, delete returns 204) and SmartEnum serialization, linked to their docs.

Delete the command once the skill works.

## Phase 2: First-wave agents (six)

Files go in `.claude/agents/`. Each gets a one-line `description` that says when to use it, so the main session picks it correctly.

| Agent | Writes code | Reads first | Copies from | Tools |
|---|---|---|---|---|
| `backend-api` | yes | CLAUDE.md, `architecture/backend.md`, `learnings/backend.md`, `learnings/domain-design.md`, `vsa-ddd`, `new-endpoint` | a real feature slice | Read, Edit, Write, Grep, Glob, Bash (build, test) |
| `efcore` | yes | `learnings/backend.md`, `learnings/hangfire-postgres.md`, EF skill | an existing entity configuration and migration | same, plus `dotnet ef`, read-only Postgres MCP |
| `blazor-ui` | yes | `architecture/blazor.md`, `learnings/blazor-ui.md`, `dotnet-blazor` | an existing admin page and a public page | Read, Edit, Write, Grep, Glob, Bash, Playwright MCP |
| `test-writer` | yes | `learnings/testing.md`, `test-factory-generator` | an existing handler test and bUnit test | Read, Edit, Write, Grep, Glob, Bash (`dotnet test`) |
| `convention-reviewer` | no | CLAUDE.md, all `learnings/*`, `convention-review` | none | Read, Grep, Glob, Bash (git diff only) |
| `aspire` | yes (AppHost only) | `aspire` skill, `aspire-debug` command, `src/Neba.AppHost/AppHost.cs` | the existing resource definitions in `AppHost.cs` | Read, Edit, Grep, Glob, Bash (`aspire` CLI, `dotnet build`), Aspire MCP tools |

### Scope of each

- **`aspire`**: run and stop the app (`aspire start`, `--isolated` in worktrees), check resource state, read console logs, structured logs and traces, and find the cause of an unhealthy resource before anyone edits code. Edits `AppHost.cs` and the AppHost project when you add or change a resource (use the list-integrations and docs tools first, match the version to `Aspire.AppHost.Sdk`). Rules it must keep: never install the Aspire workload, avoid persistent containers early in development, restart after AppHost changes, and Azure resources stay conditional on `IsPublishMode`. Other agents ask it to start the stack when they need the running app (`blazor-ui` for Playwright, `test-writer` for integration checks); it does not edit feature code. It cannot call other agents, so the main session passes it the failing resource and any trace id.

- **`backend-api`**: endpoints, handlers, validators, DTOs, domain aggregates, query caching descriptors, route and status code rules. Does not touch migrations or entity configuration (hands those to `efcore`).
- **`efcore`**: entity configuration, migrations in the migration service, query shape and projection, collection navigation rule, `ILike` escape character, indexes, data port from Software tables. Reviews for N+1 and missing `AsNoTracking`.
- **`blazor-ui`**: pages and components in `Neba.Website.Server`, `DirtyFormGuard`, `NebaAutocomplete`, `FabCreateButton`, render mode and `PageTitle`, colocated `.razor.js`, the `neba-data-table` class, Refit client use through `ApiExecutor`.
- **`test-writer`**: unit, integration and bUnit tests plus `Neba.TestFactory` factories. Enforces traits, `DisplayName`, AAA comments, strict mocks, Shouldly, `FakeLogger`. Also writes failing-test-first for bug fixes.
- **`convention-reviewer`**: read-only, fresh context. Wraps the existing `convention-review` skill so a reviewer that did not write the code does the check.

### Reuse from the marketplace

Agents preload `dotnet/skills` skills by name (see the Phase 0 table) instead of carrying their own generic advice. Our agent file holds only the repo rules and the "copy this real file" pointer. For testing, decide in Phase 0 whether `test-writer` wraps the marketplace `test-engineer` agent or stays our own. Default: our own, because our trait, factory and strict-mock rules are specific, and we borrow the marketplace skills for smell and assertion checks.

### Open choice: model per agent

Suggestion: writers inherit the session model. `convention-reviewer` can use a cheaper model since it applies a checklist. Decide when writing the files.

## Phase 3: Rewire the skills you use most

### 3.1 `/feature-plan`

Add an "Agent routing" section and change these steps:

| Step | Change |
|---|---|
| 3 Targeted repository review | Can ask `Explore` to survey related code for large features. |
| 7 / 8 Phase 1 functional and code draft | Main session splits the work by layer and asks each agent to draft its section. A new table goes to `efcore` (entity, configuration, migration, query shape). Endpoints, handlers, validators and the domain go to `backend-api`. Main session merges the drafts, resolves overlap, and shows you one draft. |
| 9 / 11 Phase 2 functional and code draft | `blazor-ui` drafts pages and components. Mockups (step 10) stay in the main session. |
| New: test section | `test-writer` drafts the test list per commit, including factories and e2e mock changes. |
| 12 Commit plan | Each commit line names its owning agent, for example `efcore: add Series table and migration`. When you later say "implement the plan", the main session hands each commit to that agent, in plan order. |

Rules to keep: the stage gates and your confirmations stay in the main session. Agents never write to the plan file; they return text and the main session writes it. Agents draft only; no feature code is written during planning.

Routing table to include in the skill:

| The feature has... | Agent |
|---|---|
| New or changed table, column, index, migration | `efcore` |
| New endpoint, handler, validator, domain type | `backend-api` |
| New page or component | `blazor-ui` |
| Any new logic | `test-writer` |
| A dependency between two of the above (new table plus endpoint) | Start with `efcore`, pass its entity shape to `backend-api` |
| A new resource, container, or setting in the AppHost, or a stack that will not start | `aspire` |

### 3.2 `/pull-request-prep`

Replace the long layer-by-layer checklist in step 5 with agent routing, and keep the report format.

1. Step 1 builds the changed file list (unchanged).
2. New step: pick agents from the diff and run them in parallel in one message.
   - `convention-reviewer`: always.
   - `efcore`: any file under `Database/`, any migration, any `*Configuration.cs`, any `DbContext` query change.
   - `backend-api`: any file under `src/Neba.Api/Features/` or Contracts.
   - `blazor-ui`: any `.razor`, `.razor.js`, or `Neba.Website.Server` change.
   - `aspire`: any change under `src/Neba.AppHost/` or `aspire.config.json` (checks resource definitions, conditional Azure resources, version match).
   - `test-writer` (review mode): always, to check that every changed source file has a test in the diff and the test rules hold.
3. Each agent returns findings as: file and line, rule, severity (Blocker, Should Fix, Suggestion). The skill dedupes and merges them into the existing report sections.
4. Keep in the skill itself: `npm audit`, plans and mockups cleanup, README checks, the follow-up review recommendations, and the PR description. These are not layer work.
5. The agents reviewing must be told to review only, not edit.

The checklist text moves into the agents (each owns its layer's items), so `pull-request-prep` shrinks from about 235 lines to about 120.

Note: `pull-request-prep` step 3 deletes `docs/plans/*` before merge to `main` unless there is a reason to keep it. This plan spans several PRs, so record that reason when it comes up, or delete the file after the work ships.

## Phase 4: Missing task skills

Small skills (under 80 lines each), each pointing at a real example:

| Skill | Covers |
|---|---|
| `add-ef-migration` | Create and review a migration, no data loss, hotfix rule (hotfixes never run migrations), Software data port. |
| `new-blazor-page` | `@page`, `PageTitle`, `InteractiveServer`, `DirtyFormGuard`, `FormLabel`, table class, bUnit test, e2e mock. |
| `write-tests` | Unit vs integration vs bUnit choice, factories, traits, strict mocks. |
| `new-background-job` | Hangfire job, transaction enlistment rule, one-worker dev note. |
| `new-domain-entity` | Aggregate or child entity with `Create` factory, typed ID, ubiquitous language entry. |

## Phase 5: Second-wave agents

Build after Phase 2 has run on real work for a few features.

| Agent | Scope | Notes |
|---|---|---|
| `ui-design` | Layout, Tailwind, mobile table layout, accessibility, screenshots through Playwright | Uses the `frontend-design` plugin and `docs/accessibility-report.md`. Mostly advice and review; hands edits to `blazor-ui`. |
| `e2e` | Playwright specs and `tests/e2e/mock-api/mock-api-server.ts` | Known trap: a new required Contracts field needs a mock update, or e2e fails every time. |
| `docs` | Help docs, policy docs, ubiquitous language | Wraps `help-documentation`, `policy-documentation`, `ubiquitous-language-review`. |
| `devops` | Workflows, hotfix, Key Vault, Azure deploy | Never pushes or dispatches; prints commands. Local Aspire work belongs to the `aspire` agent; `devops` covers only CI and deploy use of Aspire (CLI install in `cd.yml` and `hotfix.yml`). |

## Phase 6: Hooks

`.claude/settings.json` has only `block-secrets.sh` today. Candidates:

- After an edit to a `.cs` file: `dotnet format` on that file, or a build, if fast enough.
- Before `git commit`: run the architecture tests (`FeatureBoundaryTests`).
- On stop: nothing heavy; leave review to the reviewer agent.

Check the Anton lesson #9 body for more before deciding. Prefer analyzers and architecture tests over hooks where a rule can live there.

## Order and commits

Work on a new branch off `main` (not the current `vhcs-phase1`). One commit per line:

0. Phase 0: update the `dotnet/skills` marketplace and plugins, enable the chosen ones in `.claude/settings.json`, re-baseline this plan
1. `AGENTS.md`: project map, Aspire text moved to the skill
2. `vsa-ddd`: rename and rewrite, references updated
3. Generic skills: delete `dotnet-blazor`, trim `dotnet-entity-framework-core`, trim `dotnet-aspnet-core` (one commit each)
4. `new-endpoint` skill, command removed
5. Agents: `convention-reviewer`
6. Agents: `test-writer`
7. Agents: `efcore`
8. Agents: `backend-api`
9. Agents: `blazor-ui`
10. Agents: `aspire` (and delete `aspire-debug` command)
11. `feature-plan` routing
12. `pull-request-prep` routing
13. Task skills (one commit each)
14. Second-wave agents, then hooks

Agents go in before the skills that call them. Trial each agent on a small real task before moving on (see Measuring).

## Measuring

Anton's numbers do not carry over to us. Check our own:

- Pick two recent features (one with a new table, one UI-only). Replay each with and without the agents on a scratch branch.
- Record: turns, minutes, whether the result needed a redo, and which CLAUDE.md rules it broke.
- After each agent lands, note any rule it still broke and fix the agent file, not CLAUDE.md.

## Risks

- **Agents drift from CLAUDE.md.** Agents link to the docs instead of copying rules, so there is one place to edit.
- **Parallel reviewers give duplicate findings.** The skill must dedupe by file, line and rule.
- **Cold start cost.** Each agent reads its docs every run. Keep the read list short (three to five files).
- **Agent descriptions overlap**, so the main session picks the wrong one. Write each description as "use when X; not for Y".
- **The DDD rewrite loses a rule someone relied on.** Diff the old and new skill and list every dropped rule in the commit message.

## Open questions

- Does a subagent in Claude Code start other subagents? (Affects Phase 3 design; see "How the pieces fit".)
- Model per agent (see Phase 2).
- Which real endpoint, page and test are the best copy examples? Choose while writing each agent.
- Do we keep a Copilot-compatible copy of the skills (`.github/skills` exists)? Not covered here.
