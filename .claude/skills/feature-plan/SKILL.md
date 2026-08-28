---
name: feature-plan
description: Produce a two-phase (API, then UI) implementation plan for a new feature as a markdown file the user can code from. Reviews the repo for existing related code, asks clarifying questions, then drafts a functional-level plan followed by a code-level plan per phase, pausing for confirmation at each stage. Usage: /feature-plan <feature description>
---

Turn a feature description into `docs/plans/{feature-name}.md` — a plan detailed enough to code from, built up in confirmed stages rather than dumped in one shot.

## Arguments

- **description** — free-text description of the feature. If missing or too vague to scope (no clear entity/behavior), ask the user before doing anything else.

## Ground rules

- **Always two phases, in order: Phase 1 = API, Phase 2 = UI.** Even if the description sounds UI-only or API-only, still frame the plan this way — note explicitly in Phase 2 if there's genuinely no UI work, rather than skipping the phase heading.
- **Always these stages, gated on confirmation**, in order:
  1. NFR checkpoint — critical/high-stakes features only, skipped otherwise
  2. Permission/role determination
  3. UI flow sketch (lightweight) — skipped only if the feature has no UI surface
  4. Phase 1 functional draft
  5. Phase 1 code draft
  6. Phase 2 functional draft
  7. Phase 2 UI mockups (HTML) — skipped only if Step 9 concluded there is genuinely no UI surface
  8. Phase 2 code draft
  Do not start a stage until the user has confirmed the previous one. "Confirmed" means an explicit yes/approval — proceeding on silence or an unrelated reply is not a confirmation.
- **Permissions/roles are decided before any route is drafted, and UI flow is sketched before any route is drafted.** Phase 1's routes exist to serve real screens/actions and a settled authorization model — not the other way around. This does not relax the API/UI phase order below: the *plan document* still presents Phase 1 (API) before Phase 2 (UI); only the lightweight flow/permission discovery happens early, ahead of both phases' drafts.
- **Pragmatism guardrail**: the UI flow sketch determines *which* endpoints exist and what each returns — never their shape. Every endpoint still has to stand on its own as a coherent REST resource per CLAUDE.md's API conventions. No UI-specific response DTOs, no one-endpoint-per-screen-widget. If a screen wants data from multiple resources, prefer multiple calls or reuse-and-project-down (per CLAUDE.md's "Lightweight Collection Projections" convention) over a bespoke aggregate endpoint built just for that screen. If screen convenience and REST cleanliness genuinely pull in different directions, flag the tension to the user rather than silently resolving it either way.
- **A "functional" draft describes changes at the level of: what files get created/edited, what each does, and why** — no method bodies, no full class listings. Think PR-description depth, not diff depth.
- **A "code" draft shows the actual code changes** — real class/method signatures, real field names, real routes, close enough to paste into the editor and adjust. It replaces the functional bullets for that phase in the markdown file, it doesn't just append below them.
- **The plan file's full skeleton — both phase headings, all four stage placeholders — is written on first creation** (Step 5, before any content is drafted), not built up heading-by-heading as stages are confirmed. From then on the file is updated incrementally in place: each stage replaces its own section's placeholder/prior content, so `docs/plans/{feature-name}.md` always reflects the latest confirmed + in-progress state if the session is interrupted, and the reader can see the shape of the whole plan (including not-yet-drafted phases) from the first write onward.
- This skill produces a plan. It does not write feature code, run `dotnet build`, or create branches — that happens afterward, driven by the user from the finished markdown file.

## Step 1 — Scope and clarify

Read the feature description. Before touching the repo, identify anything genuinely ambiguous: which existing feature/domain it extends vs. a new one, what entities/aggregates are involved, whether it needs new authorization policies, whether it's read-only (query) or mutating (command), and any UI surface implied (new page vs. addition to an existing page).

Do a first-pass skim (not the full targeted review below — just enough to ask informed questions) of:
- `docs/architecture/backend.md` and `docs/architecture/blazor.md` for relevant existing patterns
- `src/Neba.Api/Features/` folder names, to see if this feature extends an existing domain or needs a new one

Also determine whether this feature is **critical/high-stakes** — it handles money, PII beyond what's already covered by the existing redaction taxonomy, is a compliance/legal requirement, or a failure would have an outsized blast radius (data loss, a broken season/tournament result, an integration another team depends on). If it's not obvious from the description, ask directly via `AskUserQuestion` rather than guessing — this determines whether Step 2 runs. Default to "not critical" when genuinely unclear; routine CRUD additions to an existing feature are not critical by default.

Then use `AskUserQuestion` for anything else still unresolved (max 4 questions at a time, mark a recommended default when there is an obvious one from repo conventions — the criticality question above can share a batch with these). Do not ask questions the repo already answers unambiguously (e.g. don't ask "should commands return ErrorOr<T>?" — CLAUDE.md already says yes). Skip the non-criticality questions entirely if the description is already unambiguous.

## Step 2 — NFR checkpoint (critical features only)

Skip this step entirely (and say so) if Step 1 determined the feature is not critical/high-stakes.

Most non-functional requirements for this app are already fixed system-wide (hosting, uptime posture, tech stack) and documented in `docs/architecture/backend.md`/`blazor.md` — re-litigating those per feature would be pure ceremony. This checkpoint only covers the NFRs that genuinely vary **per feature** and change what gets drafted in Phase 1/Phase 2:

- **Performance target** — expected data volume and request rate for this feature's endpoint(s)/page(s). Does a list endpoint need pagination or a page-size cap from day one? Does a query need to be a `ICachedQuery` behind `IFusionCache` (per CLAUDE.md's caching convention) rather than hitting the database on every request?
- **Concurrency/consistency** — can two users act on the same record at once (e.g. two admins editing the same tournament)? If so, does this need an optimistic concurrency check, or is last-write-wins acceptable?
- **Security sensitivity** — does this feature touch PII beyond what's already classified in `Neba.Api.Compliance.DataTaxonomy`? Does it need a new `[PersonalData]`/`[PrivateData]` classification, or an audit trail beyond what's already logged?
- **Availability/degradation** — if a dependency this feature relies on is down (cache, external API, background job), should the feature fail the request outright or degrade gracefully (e.g. the `ResilientAuditDataProvider` pattern — log and continue)?
- **Data retention** — does this feature introduce data that needs a retention/deletion policy, or is it covered by an existing one?

Present the answers (inferred from the description plus repo conventions where obvious, flagged for the user to confirm/correct) as a short list — one page's worth, not a document. Ask: **"Do these NFR targets look right for this feature? I'll use them to shape the Phase 1/Phase 2 drafts."**

Do not proceed until confirmed. Record the confirmed targets under "Decisions locked in during scoping" in Step 6 — each target should visibly inform a specific decision later (e.g. the caching target drives whether Phase 1 lists the query as an `ICachedQuery`, the concurrency target drives whether the aggregate needs a concurrency token).

## Step 3 — Targeted repository review

Scope the review to what this feature plausibly touches — do not scan the whole repo. Use `Explore` (or direct `grep`/`find`) for:

- The specific `Features/{Domain}/` folder(s) implicated by the feature, if they exist — read the domain model, existing commands/queries, and endpoint group.
- One or two **similar existing features** as a structural reference (e.g. a feature with a similar CRUD shape, similar authorization pattern, or similar UI list/detail/create flow) — pick by resemblance to what's being built, not by proximity in the folder tree.
- `src/Neba.Api.Contracts/` for existing response/request shapes that might be reused per the "Lightweight Collection Projections" convention in CLAUDE.md (reuse-and-project-down over a parallel endpoint).
- `src/Neba.Website.Server/` for existing pages/components in the same area, if Phase 2 is expected to extend an existing page rather than create a new one.
- `docs/policies/README.md` if the feature implies a new or existing authorization policy.

Keep this focused — a handful of targeted lookups, not an exhaustive audit. The goal is to know what already exists so the plan proposes extending it, not duplicating it.

## Step 4 — Permission/role determination

Before scoping any routes, settle who can do this. Check `docs/policies/README.md` and the existing role set for a fit:

- Does an existing authorization policy already cover this action? If so, which role(s) hold it — reuse it, don't create a near-duplicate.
- If no existing policy fits, does the action belong on an **existing role**, or does its scope call for a **new role**? Don't default to bolting a new permission onto the nearest existing role just because it's convenient — check whether the people who should get this permission are actually the same set of people who hold any existing role. The `Journalist` role (introduced for article-authoring permissions) is the precedent: a new role was warranted because "can write articles" wasn't a good fit for any existing role's scope, not because a new table was easy to add.
- If it's genuinely ambiguous, ask via `AskUserQuestion` rather than deciding silently — this is exactly the kind of call that's the user's to make, not a repo convention to infer.

Record the decision (policy name — new or existing; role(s) it's granted to; whether a new role was introduced and why) so it can be captured under "Decisions locked in during scoping" in Step 6.

Do not proceed until the user has confirmed the permission/role decision.

## Step 5 — UI flow sketch (lightweight)

Skip this step entirely (and say so) if the feature has no UI surface at all — a pure API/background feature.

Before drafting any Phase 1 routes, sketch the flow at a lightweight level — not the HTML mockups from Step 9, just enough to know what the API needs to support:

- What screen(s)/state(s) are involved, in what order.
- What action(s) the user takes on each screen.
- What data each screen needs, and when (on load vs. after an action).

A bullet list or short numbered flow is enough here — no HTML, no visual design. Show it in chat and ask: **"Does this flow look right? I'll use it to figure out what API routes Phase 1 needs."**

Do not proceed until confirmed. Once confirmed, use it to inform (not dictate) the Phase 1 functional draft — apply the pragmatism guardrail above when translating a screen's data needs into actual endpoints.

## Step 6 — Initialize the plan file

Before drafting any content, create `docs/plans/{feature-name}.md` (kebab-case the feature name from the description; create `docs/plans/` if it doesn't exist) with the full skeleton for both phases:

```markdown
# {Feature Title}

{One- or two-sentence restatement of what the feature does.}

## Phase 1: API

*(Not yet drafted.)*

## Phase 2: UI

*(Not yet drafted.)*
```

If Step 1/Step 2 (if run)/Step 3/Step 4/Step 5 surfaced decisions, assumptions, or things the user explicitly ruled in/out while scoping — including the NFR targets from Step 2, the permission/role decision from Step 4, and the confirmed flow from Step 5 — capture them under a short `## Decisions locked in during scoping` section between the title and `## Phase 1` — this is where cross-phase context that isn't specific to either phase belongs, so it doesn't get lost or duplicated across both phase sections.

## Step 7 — Phase 1 functional draft (API)

Draft, at functional level:
- New files to create (path + one-line purpose each), following the use-case folder structure (Endpoint + Summary + Validator + Command/Query + Handler) per CLAUDE.md's API Endpoint Checklist.
- Existing files to edit (path + what changes and why).
- Domain layer changes, if any — new aggregate/entity/value object, or a new method on an existing aggregate — framed per CLAUDE.md's "Always-Valid Entities and Aggregate Assignment" and "Aggregate Invariants Requiring Cross-Aggregate Data" sections (call out explicitly whether a new invariant needs cross-aggregate data passed in as a parameter).
- Database schema changes, if any (table/column additions, migration needed).
- Authorization approach (policy name, new or existing).
- Test factories needed (new types always need one, per CLAUDE.md).
- Anything this phase deliberately defers to Phase 2 or out of scope entirely.
- If Step 2 ran: how each confirmed NFR target is satisfied here — e.g. the query is listed as an `ICachedQuery` because of the performance target, a concurrency token is added to the aggregate because of the concurrency target, a new `[PrivateData]` classification is applied because of the security target. Don't restate the targets — show the concrete decision each one produced.

If `ddd-clean-architecture`, `dotnet-aspnet-core`, or `dotnet-entity-framework-core` skills are relevant to decisions being made in this draft (e.g. aggregate boundaries, EF Core modeling choices), invoke them to inform the draft rather than guessing at conventions — don't just cite them by name without applying what they say.

Replace the `*(Not yet drafted.)*` placeholder under `## Phase 1: API` (written in Step 6) with this draft, structured as a checklist-style breakdown by layer (Domain / Application / Infrastructure / API / Contracts / Tests), mirroring the "What Changed" layer grouping used in `pull-request-prep`.

Show the same content in chat and ask: **"Does this functional breakdown for Phase 1 look right, or should anything change before I draft the actual code?"**

Do not proceed until confirmed.

## Step 8 — Phase 1 code draft (API)

Once Step 7 is confirmed, expand each item in the Phase 1 section into actual code: real class/record signatures, method bodies where the logic isn't obvious boilerplate, route strings, validator rules, DI registration snippets, and factory method signatures. Use the patterns and templates in the `new-endpoint` command and `ddd-clean-architecture` skill as the baseline shape, adapted to this feature's actual types and rules — don't reproduce their placeholder syntax verbatim.

Replace the Step 7 functional bullets in `docs/plans/{feature-name}.md` under `## Phase 1: API` with this code-level detail (keep the layer-grouped structure, but each item is now a fenced code block instead of a one-line bullet).

Show the update in chat and ask: **"Does the Phase 1 code look right? Once you confirm, I'll move on to Phase 2 (UI)."**

Do not proceed until confirmed.

## Step 9 — Phase 2 functional draft (UI)

Same shape as Step 7, but for the Blazor side. The screens/actions/data needs were already sketched and confirmed in Step 5's flow — this step fleshes that flow out into concrete files and components, it doesn't re-litigate what the flow is:
- New pages/components to create (path, route if a page, one-line purpose).
- Existing pages/components to edit.
- New `I{Domain}Api` Refit method(s) needed (should already exist from Phase 1's Contracts work — reference, don't redesign).
- State/dirty-tracking needs — call out explicitly if `DirtyFormGuard` applies (per CLAUDE.md's "Dirty Form Guard" learning) for any new data-entry form.
- `<PageTitle>` requirement and render mode, per CLAUDE.md's "Page Titles" learning.
- Any FAB / list-page entry point needed, per CLAUDE.md's "List Page Add New Pattern", if this feature adds a creatable list.
- Playwright/bUnit test needs, using the decision table from `new-endpoint`/`pull-request-prep` (bUnit for internal component logic, Playwright for real browser + HTTP flows).
- If the feature has no UI surface at all, state that explicitly here rather than fabricating one.

If `dotnet-blazor` skill guidance is relevant to a decision in this draft, apply it rather than guessing.

Replace the `*(Not yet drafted.)*` placeholder under `## Phase 2: UI` (written in Step 6) with this draft (same layer/checklist style, adapted to Blazor: Pages / Components / API Client / Tests).

Show it in chat and ask: **"Does this functional breakdown for Phase 2 look right, or should anything change before I draft the actual code?"**

Do not proceed until confirmed.

## Step 10 — Phase 2 UI mockups (HTML)

Once Step 9 is confirmed, and before writing any Blazor code, produce static HTML mockups of the new/changed pages for the user to review. Skip this step entirely (and say so) only if Step 9 concluded the feature has no real UI surface. These mockups give the flow confirmed in Step 5 its actual visual design — they should not change what screens/actions exist, only how they look.

Invoke the `frontend-design` skill first, to calibrate visual direction (typography, color, layout, what reads as templated vs. intentional) before building anything — don't guess at aesthetic choices the skill already has an opinion on.

For each new or substantially-changed page identified in Step 9, decide which kind of page it is:

- **Data capture** (a create/edit form — one obvious input layout, no real layout tradeoff to weigh): produce a **single** HTML mockup.
- **Data display** (a list/detail/dashboard page where density, hierarchy, and layout genuinely have multiple reasonable answers): produce **2–3 distinct mockup options** (different layouts or visual emphasis) so the user can compare and pick, per `frontend-design`'s guidance on avoiding templated defaults. Don't multiply options for their own sake — only produce more than one when the page's design actually has a meaningful decision to make.

Mockup rules:

- Save each as a plain static HTML file under `docs/plans/mockups/{feature-name}/` (create the directory if needed) — e.g. `docs/plans/mockups/create-tournament/create-tournament.html` for a single data-capture mockup, or `option-1.html`/`option-2.html`/`option-3.html` per page for data-display comparisons. **Do not** use the `Artifact` tool for these — they're local working files reviewed as part of the plan, not something to publish.
- Use realistic sample data and the feature's actual field names/labels/categories (pulled from the Step 9 draft) — a mockup should read as this feature, not a generic template.
- For interactions that only make sense at runtime (modals, mode switches, dependent dropdowns, an inline "create new X" flow, tab switching), include lightweight inline `<script>` that simulates the interaction (toggling classes/visibility) — just enough to click through the flow. This is throwaway scripting for review purposes, not a preview of the real implementation.
- Record each mockup file's path in `docs/plans/{feature-name}.md` under `## Phase 2: UI`, in a `### Mockups` subsection, with a one-line note on what each shows.

Tell the user the mockup file path(s) so they can open and review them (do not attempt to render the HTML inline in chat), and ask: **"Here's the mockup for [page]. Does this look right, or should anything change before I draft the actual code?"**

Do not proceed until confirmed. If the user asks for changes, update the mockup file(s) in place and re-confirm before moving to Step 11 — the same "don't silently fold unconfirmed changes into the next stage" rule that applies to every other gate applies here too.

## Step 11 — Phase 2 code draft (UI)

Once Step 10 is confirmed (or skipped, per Step 10's own condition), expand each item into actual `.razor`/`.razor.cs` code: markup, `@code` blocks, event handlers, DirtyFormGuard wiring if applicable, and the Refit client call. Use the confirmed mockup(s) as the source of truth for markup structure/classes/layout, adapted into Razor. Replace the Step 9 functional bullets under `## Phase 2: UI` with this code-level detail, same fenced-code-per-item structure as Step 8.

Show the update in chat and report the plan is complete: **"Phase 2 code is in. The full plan is in `docs/plans/{feature-name}.md` — let me know if you want any adjustments, or you're ready to start implementing from it."**

## Rules

- Never skip a confirmation gate, even if the feature seems simple — a "looks good, go ahead" from the user still counts as confirmation, just don't assume it without asking.
- Never write Phase 2 content before Phase 1 is fully confirmed (both stages), even if the user's description leads with UI details — reorder into the plan's fixed phase order regardless of how the request was phrased.
- If repo review in Step 3 surfaces an existing, reusable piece (a query, a component, a contract type) that the feature could extend instead of duplicating, propose reusing it in the functional draft rather than silently planning a duplicate — flag the choice to the user if it's not obvious which is better.
- If the user requests changes at a confirmation gate, update that stage's section in the plan file and re-show it before moving on — don't silently fold unconfirmed changes into the next stage.
- The plan file is the deliverable of this skill. Don't create additional scratch files for it — everything lives in the one `docs/plans/{feature-name}.md`, updated in place stage by stage.
