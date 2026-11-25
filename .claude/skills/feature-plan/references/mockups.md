# Phase 2 UI mockups (HTML)

Read this in Step 10, only when the feature has a UI surface.

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
