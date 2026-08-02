---
name: help-documentation
description: Generate or refresh the user help doc (docs/help/*.md) for a command, given its Command, CommandHandler, Query, QueryHandler, or Endpoint file. Regenerates screenshots via Playwright when a UI exists. Usage: /help-documentation <File>
---

Generate (or, on re-run, validate and update) the user-facing help doc for a single command/use-case, per [ADR-0007](../../../docs/adr/0007-in-repo-user-help-documentation.md). If the use case has a Blazor UI, also (re)generate its screenshots via the project's dedicated Playwright config.

## Arguments

- **File** — path or name of a Command, CommandHandler, Query, QueryHandler, or Endpoint file belonging to the use case (e.g. `DeleteArticleCommand.cs`, `DeleteArticleEndpoint.cs`, or just `DeleteArticle`). If omitted, ask the user which use case to document.

## Step 1 — Resolve the use-case folder and gather facts

Endpoints live in use-case folders at `src/Neba.Api/Features/{Feature}/{UseCase}/` (Endpoint + Summary + Validator + Command/Query + Handler, per `CLAUDE.md`). From whichever file the user gave, find the folder (`find src/Neba.Api/Features -type d -iname "*{UseCase}*"` if given just a name) and read every file in it. Extract:

- **Feature** — the parent folder name (e.g. `News`).
- **Use case / command name** — the folder name (e.g. `DeleteArticle`).
- **HTTP verb + route** — from the `Endpoint`'s `Configure()` (e.g. `Delete("{id}")` under `Group<NewsEndpointGroup>()`).
- **Authorization requirement** — from `Configure()`, exactly one of:
  - `Policies(Permissions.X.PolicyName)` → requires the single permission `X` via the dynamic `Permission:{value}` mechanism.
  - `Policies(Permissions.SomePolicyNameConstant)` where the constant isn't a `Permissions` instance's `.PolicyName` (e.g. a bespoke policy like `CanManageArticlesPolicyName`) → requires that named policy.
  - `Roles(...)` → requires one of the listed roles directly.
  - `AllowAnonymous()` → no prerequisite.
  - If none of these appear, stop and ask the user — do not guess at authorization.
- **Responses** — status codes from `Produces()`/`ProducesProblemDetails()` in `Configure()`, and any `ErrorOr` error cases the handler returns (for the Troubleshooting section).

## Step 2 — Resolve the prerequisites/policy reference

Read `docs/policies/README.md` ([ADR-0008](../../../docs/adr/0008-policy-documentation-structure.md)):

- **`Permission:{value}` requirement** → this is always covered by the generic `Permission:{value}` row. Name the specific permission in prose (e.g. "requires the `News.DeleteArticle` permission") and link to `docs/policies/README.md`. Do not expect or look for a per-permission file.
- **Named-policy requirement** → look for a row matching that policy name.
  - **Found** → link to its dedicated file if the row has one, otherwise link the README row directly.
  - **Not found** → this policy isn't documented yet. Note it in your final report and ask the user: *"`{PolicyName}` has no entry in docs/policies/README.md — want me to run the `policy-documentation` skill for it before finishing this doc?"* Do not write policy docs yourself — that's out of scope for this skill.
  - **Verify the policy is actually registered and enforced**, not just defined as a constant — grep `src/Neba.Api/Security/SecurityConfiguration.cs` and `src/Neba.Website.Server/Account/AccountConfiguration.cs` for an `AddPolicy(...)` call with that name, and grep the UI for a matching `<AuthorizeView Policy="...">`. If the constant exists in `Permission.cs` but there's no `AddPolicy(...)` registration and no `<AuthorizeView>`/`.Policies(...)` reference anywhere, the endpoint is almost certainly using something else to gate access — **re-check the endpoint's actual `Configure()` call**, don't assume the plausibly-named policy is the one in effect. (This exact mistake was made once for `DeleteArticleEndpoint`: `CanManageArticlesPolicyName` exists as a constant but is unregistered and unused — the endpoint and UI both gate on `Permissions.DeleteArticle.PolicyName` instead. Trust the `Configure()`/`AuthorizeView` call sites, never a similarly-named constant.)
- **Roles requirement** → name the role(s) directly; still check `docs/policies/README.md` for a matching row and link it if one exists.
- **AllowAnonymous** → Prerequisites section states "None — open to all users."

## Step 3 — Determine whether a UI exists

The Refit API contract lives at `src/Neba.Api.Contracts/{Feature}/I{Feature}Api.cs`. Find the method whose route matches this use case (e.g. `DeleteArticleAsync` for `DELETE /news/{id}`).

Grep `src/Neba.Website.Server` and `src/Neba.Website.Client` for calls to that method (e.g. `grep -rn "DeleteArticleAsync(" src/Neba.Website.Server src/Neba.Website.Client`).

- **No call sites** → this is an API-only command. Skip to Step 6 (no-UI path).
- **One or more call sites** → a UI exists. For each `.razor`/`.razor.cs` file found, extract:
  - **Page route** — the `@page "..."` directive of the containing component (or its parent page, if the call is in a child component).
  - **Trigger element** — the button/link that invokes the action (look for the CSS class(es) on the clickable element and any `<AuthorizeView Policy="...">` wrapping it).
  - **Confirmation step** — check for a shared confirmation component (e.g. `ConfirmActionModal`) between the trigger and the actual API call. Note its title/message and confirm/cancel selectors.
  - **Post-action feedback** — toast calls (`ToastService.Show(...)`) and any `NavigationManager.NavigateTo(...)` redirect, for both success and failure paths.
  - If a call site is ambiguous (e.g. the method is called from a shared helper used in more than one unclear flow), ask the user to confirm the page/trigger rather than guessing.

`docs/help/delete-article.md` and `tests/e2e/docs-screenshots/delete-article.spec.ts` are the reference example for what this step should produce — read them if you want a concrete model of the target shape.

## Step 4 — Write or update `docs/help/{kebab-case-use-case-name}.md`

Convert the use-case name to kebab-case for the filename (e.g. `DeleteArticle` → `delete-article`).

**If the file does not exist**, create it with this structure (per ADR-0007):

1. `# {Title}` + one-line purpose.
2. `## Prerequisites` — per Step 2.
3. `## Steps` — numbered instructions per entry point found in Step 3 (one subsection per page/trigger if there's more than one way to invoke the action). Insert screenshot placeholders as `![{alt text}](images/{use-case-kebab}/{step-slug}.png)` at each meaningfully distinct UI state — name these paths to match what Step 5 will generate.
4. `## What happens after you confirm` (or an equivalent heading matching the action's nature) — success and failure feedback from Step 3.
5. `## Troubleshooting` — a table covering: no access (prerequisite not met), the failure toast/error path, and any other `ErrorOr` cases from Step 1.
6. `## Related` — links from Step 2, plus any adjacent help docs worth cross-referencing.

**If the file already exists**, read it fully, then reconcile section-by-section instead of rewriting wholesale:

- Prerequisites, route-derived facts, and screenshot filenames: **update to match current code** — these are verifiable, so stale values are bugs, not style choices.
- Steps prose, Troubleshooting entries, and other hand-written wording: **keep as-is unless it now contradicts what Step 1–3 found** (e.g. a button moved, a new failure mode was added, the confirmation dialog text changed). Only touch what's actually stale.
- If something in the existing doc can't be verified either way (e.g. a troubleshooting entry describing a scenario that isn't obviously derivable from the code), leave it and flag it in your final report rather than deleting it.

## Step 5 — Screenshots

**If a UI exists (Step 3 found call sites):**

Create or update `tests/e2e/docs-screenshots/{use-case-kebab}.spec.ts`, modeled on `tests/e2e/docs-screenshots/delete-article.spec.ts`:

- Log in with the required permission via `page.request.post('/__test/login?permissions={permission}')` if Step 1/2 found a permission requirement (skip if the flow is anonymous).
- Navigate to each page found in Step 3, waiting on a stable selector before each screenshot.
- Capture one screenshot per placeholder inserted in Step 4, writing to `path.join('docs', 'help', 'images', '{use-case-kebab}', '{step-slug}.png')`.
- **Prefer non-mutating capture paths.** If the flow has a natural "cancel"/"back out" step (like the confirm/cancel modal in the delete-article example), end the test there instead of completing the action, so the script is safe to re-run against the same mock data without a reset step. If the action has no natural undo (e.g. a create form), capture the pre-submit state and stop short of submitting; note in a comment why the flow doesn't go further.
- The Blazor server in `playwright.docs.config.ts`'s `webServer` block runs `dotnet run --configuration Release` — Debug builds surface debug-only UI chrome (e.g. a debug toolbar/buttons) that must never appear in a user-facing help screenshot. If this config's `dotnet run` command is ever changed, keep `--configuration Release` in it.
- Regardless of whether the doc or spec already existed, **always run the screenshots this pass** — do not skip regeneration just because files with the right names already exist:
  ```
  npm run docs:screenshots -- tests/e2e/docs-screenshots/{use-case-kebab}.spec.ts
  ```
- Report whether the run passed. If it failed (e.g. selectors no longer match, or the local stack didn't come up), say so plainly and leave the previous screenshots in place rather than deleting them.

**If no UI exists (Step 3 found no call sites):**

- Do not create a spec file, and do not run `docs:screenshots`.
- In the doc's `## Steps` section, still include the same `![...](images/{use-case-kebab}/{step-slug}.png)` placeholder syntax for any UI-shaped steps that might exist once a UI is built (there usually won't be any for a pure API command — in that case, omit the Screenshots-style placeholders and instead describe the command as an API-only operation).
- Add a note directly under the affected heading: *"No screenshots were generated — this command has no UI today. The placeholders above are left in place so a UI, if one is added later, can be documented without restructuring this file."*

## Step 6 — Wire up the in-app help button

The rendered doc is surfaced in the app via the shared `HelpButton` component (`src/Neba.Website.Server/Components/HelpButton.razor`), which reads the embedded `docs/help/{name}.md` at runtime (see ADR-0007's "Deferred" section — now implemented). Only applies to the UI path (Step 3 found call sites); skip entirely for the no-UI path.

**No `.csproj` change is needed for the doc file or its images to ship** — `Neba.Website.Server.csproj` embeds `docs/help/*.md` and `docs/help/images/**/*.png` via glob (`LogicalName="Help.Docs.%(Filename)%(Extension)"` / `"Help.Images.%(RecursiveDir)%(Filename)%(Extension)"`), so a new file under either path is picked up on the next build automatically.

For each page found in Step 3:

1. Check whether `<HelpButton DocName="{kebab-case-use-case-name}" />` is already present near that page's title/heading. If present and the `DocName` still matches, leave it.
2. If missing, add it using whichever pattern fits the page's existing header markup:
   - **`page-title-bar` pages** (the gradient header with `.page-title-inner`) — add a `<div class="page-title-help"><HelpButton DocName="..." Light="true" /></div>` as a sibling of `.page-title-inner`, inside `.page-title-bar`. Use `Light="true"` — the button sits on a dark gradient background.
   - **Other headings** (section headers, detail-page heroes, inline next to an action button) — drop `<HelpButton DocName="..." />` directly next to the relevant heading or trigger element. Check the surrounding CSS for a dark background (gradient hero, dark card) and pass `Light="true"` in that case; omit it (defaults to the neutral gray style) on light backgrounds.
   - If a page hosts multiple related actions with separate docs (e.g. a detail page with both an edit link and a delete button), attach each doc's `HelpButton` next to its own trigger rather than picking one to represent the whole page.
   - Gate the button behind the same `AuthorizeView`/policy as the action it documents, when the action itself is permission-gated (see `Tournaments/Detail/TournamentDetail.razor`'s `delete-tournament` wiring or `News/NewsDetail.razor`'s `delete-article` wiring for the pattern) — don't show a help button for an action the current user can't perform. **Verify the policy name against the actual `Configure()`/`AuthorizeView` call site for that action, same caution as Step 2** — do not assume a plausibly-named `Permissions` constant is the one actually wired up.

**Update every bUnit test that renders the page you just touched.** `HelpButton` injects `HelpDocumentService` (`src/Neba.Website.Server/Help/HelpDocumentService.cs`); any existing `BunitContext`-based test for that page will throw a DI resolution error the moment it renders unless the test also registers it. Add `_ctx.Services.AddSingleton<HelpDocumentService>();` alongside that test file's other `_ctx.Services.AddSingleton(...)` calls (and `using Neba.Website.Server.Help;` to the usings) — do this for every test file covering the page, not just one. After wiring, run the full `Neba.Website.Tests` suite (not just the touched page's tests) to confirm nothing else regressed: `dotnet test tests/Neba.Website.Tests/Neba.Website.Tests.csproj`.

## Step 7 — Report

Summarize:

- Whether the doc was created or updated, and which sections changed.
- Whether a UI was found, and if so, whether `docs:screenshots` ran and passed.
- If no UI was found, confirm no screenshot spec was created.
- Whether `HelpButton` was added or already present on each page from Step 3, and which bUnit test files got the `HelpDocumentService` DI registration added — and confirm the full `Neba.Website.Tests` suite was run and passed afterward.
- Any missing `docs/policies/` entry, and whether the user wants `policy-documentation` run next.
- Anything left unresolved for the user to confirm by hand (ambiguous call sites, troubleshooting entries that couldn't be verified, etc.).

## Rules

- Never state an authorization requirement without confirming it against the actual `Configure()`/`AuthorizeView` call site — a plausibly-named `Permissions` constant is not proof it's wired up (see the `CanManageArticles` caution in Step 2).
- Never invent UI steps, selectors, or toast text — derive them from the actual `.razor`/`.razor.cs` source, and ask the user if a call site is ambiguous.
- Never run `docs:screenshots` for an API-only command.
- Never delete an existing doc's hand-written prose to "simplify" it — only replace what Step 1–3 can concretely verify as stale.
- Screenshot specs must default to non-mutating flows (cancel out of confirmations) unless the user explicitly asks for the completed-action state to be captured.
- Never add a `HelpButton` without also updating that page's bUnit test(s) to register `HelpDocumentService` — an unregistered dependency fails every test that renders the page, not just new ones.
