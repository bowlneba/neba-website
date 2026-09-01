# Accessibility Report

Date: 2026-08-31
Scope: full site — every `.razor`/`.razor.js`/`.razor.css` file under `src/Neba.Website.Server` and `wwwroot/neba_theme.css`, reviewed against WCAG 2.1 AA. This supersedes the original `dark-release`-scoped pass (findings #1–4 below carried over unchanged); this revision is a whole-site audit meant to guide a dedicated compliance effort, not a single PR.

## Status

| # | Finding | Severity | Status |
|---|---|---|---|
| 1 | Form validation errors never announced or associated | High | Fixed |
| 2 | `NebaDocument` mobile TOC and slideover panel skip focus trap | High | Fixed |
| 3 | Two unlabeled `<select>` elements share one unassociated label | High | Open |
| 4 | Low-contrast gray text used for real stat values | High | Open |
| 5 | Surface tokens unset in dark mode — near-invisible autocomplete hover text | High | Open |
| 6 | Hand-rolled Stats modals skip focus trap / Escape handling | High | Fixed |
| 7 | Clickable bowling-center card is a mouse-only trap | Medium | Open |
| 8 | Ambiguous, undisambiguated action buttons in repeated list rows | Medium | Open |
| 9 | `<th>` missing `scope` in six components | Medium | Open |
| 10 | `role="img"` wraps focusable, individually-labeled children | Medium | Open |
| 11 | `AccountMenu` uses ARIA menu roles without menu keyboard behavior | Medium | Open |
| 12 | Muted gray text fails contrast in dark mode | Medium | Open |
| 13 | Invalid ARIA role (`role="note"`) | Low | Open |
| 14 | Home page skips a heading level | Low | Open |
| 15 | Weak focus indicator on timeline dots | Low | Open |
| 16 | Inconsistent `aria-live` on async-loaded list regions | Low | Open |
| 17 | Secondary button keeps light styling in dark mode | Low | Open |

## Findings

### 1. Form validation errors never announced or associated (High)

**Where**: every form using `<ValidationMessage>` — `CreateSponsor.razor`, `EditSponsor.razor`, `CreateArticle.razor`, `EditArticle.razor`, `CreateTournament.razor`, `EditTournament.razor`, `CreateUser.razor`, `Login.razor`, ~30+ call sites total.

**Problem**: each renders Blazor's default `<div class="validation-message">` with no `aria-describedby` linking it to its `<input>`, and no `role="alert"`/`aria-live` on the message. A screen-reader user who submits an invalid form gets no notice that an error appeared, and tabbing to the field never reads the error. Site-wide `grep` for `aria-describedby` returns only 2 unrelated hits.

**WCAG**: 3.3.1 Error Identification, 4.1.3 Status Messages, 1.3.1.

**Fix**: give each field's error container a generated id, set `aria-describedby` on the input to that id, and add `role="alert"` to the validation message.

### 2. `NebaDocument` mobile TOC and slideover panel skip focus trap (High)

**Where**: `Documents/NebaDocument.razor:62-85` (mobile table-of-contents modal), `:93-127` (internal-link slideover panel). Both use `role="dialog" aria-modal="true"`.

**Problem**: `Documents/NebaDocument.razor.js:85-130,290-330` implements Escape-to-close and click-outside, but neither dialog moves focus into itself on open or traps Tab — `aria-modal="true"` promises trapped focus that isn't there. Same defect class as #6 below, at a location the original narrow audit didn't cover.

**WCAG**: 2.1.1 Keyboard, 2.4.3 Focus Order.

**Fix**: route both through `Components/NebaModal.razor`, or port its JS focus-trap/initial-focus logic into `NebaDocument.razor.js`.

### 3. Two unlabeled `<select>` elements share one unassociated label (High)

**Where**: `News/CreateArticle.razor:73-101`, `News/EditArticle.razor:95-...`.

**Problem**: a bare `<label>Tournament</label>` (no `for`) sits above two `<select>` elements (season, tournament); neither has an `id`, `aria-label`, or `aria-labelledby`. A screen-reader user tabbing into either select hears no accessible name.

**WCAG**: 1.3.1 Info and Relationships, 4.1.2 Name/Role/Value, 3.3.2 Labels or Instructions.

**Fix**: give each `<select>` its own `aria-label` (e.g. "Season", "Tournament") — one outer `<label>` can't cover two controls even with a `for` fix.

### 4. Low-contrast gray text used for real stat values (High)

**Where**: `Stats/IndividualStats.razor:362`, `Stats/SeasonStats.razor:1280`:

```csharp
=> value >= 0 ? "text-[var(--neba-success)]" : "text-gray-400";
```

**Problem**: Tailwind's `text-gray-400` (`#9CA3AF`) on white is ~2.5:1 — well under the 4.5:1 AA minimum — and renders an actual stat value (a negative field average), not a hint or placeholder.

**WCAG**: 1.4.3 Contrast (Minimum).

**Fix**: swap to `text-gray-600`/`var(--neba-gray-600)` (~7:1) or another token clearing 4.5:1.

### 5. Surface tokens unset in dark mode (High)

**Where**: `wwwroot/neba_theme.css:76-79`, `.dark` overrides at `:90-94`, consumed by `NebaAutocomplete` at `:599-601`.

**Problem**: `--neba-bg`, `--neba-bg-panel`, `--neba-text`, and `--neba-border` are redefined under `.dark`, but the four `--neba-surface*` tokens alias straight to the light gray scale and are never redefined for `.dark`. `NebaAutocomplete`'s option text uses `var(--neba-text)` (light gray in dark mode) on a hover background of `var(--neba-surface-high)` (still `#F5F5F5` in dark mode) — roughly 1:1 contrast, text effectively invisible on hover/keyboard-navigate.

**WCAG**: 1.4.3 Contrast (Minimum).

**Fix**: add a `.dark` override block for the four surface tokens alongside the existing `--neba-bg`/`--neba-text`/`--neba-border` overrides.

### 6. Hand-rolled Stats modals skip accessible dialog behavior (High)

**Where**: `Stats/IndividualStats.razor:249-260`, `Stats/SeasonStats.razor:724-736`.

**Problem**: both build their own `role="dialog" aria-modal="true"` markup instead of `Components/NebaModal.razor`, which handles initial focus, focus trap, and Escape (`NebaModal.razor:170-219`). Neither hand-rolled dialog does any of that — a keyboard user can tab past the "modal" into hidden page content behind it, with no Escape-to-close.

**WCAG**: 2.1.1 Keyboard, 2.4.3 Focus Order.

**Fix**: replace both with `<NebaModal>`.

### 7. Clickable bowling-center card is a mouse-only trap (Medium)

**Where**: `BowlingCenters/BowlingCenters.razor:111-112`:

```razor
<div class="neba-card ... cursor-pointer" @onclick="@(() => HandleCenterCardClick(center))">
```

**Problem**: no `role="button"`, `tabindex="0"`, or `@onkeydown` — the card's click action (focusing the corresponding map pin) is mouse-only. Contrast with `History/Champions/YearView.razor:7-9` and `TitleCountView.razor:3-5,46-48,89-91`, which already do this correctly.

**WCAG**: 2.1.1 Keyboard.

**Fix**: add `role="button" tabindex="0"` and an Enter/Space `@onkeydown` handler matching the existing pattern.

### 8. Ambiguous, undisambiguated action buttons in repeated list rows (Medium)

**Where**: `Tournaments/Detail/ManageTournamentSponsors.razor:57`, `Sponsors/SponsorPhoneNumbersEditor.razor:24` (both: every row's button just says "Remove"); `News/CreateArticle.razor:138-140` / `News/EditArticle.razor:176-178` (each attachment row has generic "Download"/"Open"/"Remove" actions, none naming the file).

**Problem**: a screen-reader user browsing by control type hears a list of identical "Remove"/"Download"/"Open" entries with no way to tell them apart. `News/ArticleCard.razor:40` and `News/NewsList.razor:144` already use `aria-label="Delete article"` scoped per item — the fix pattern already exists elsewhere in the codebase.

**WCAG**: 2.4.6 Headings and Labels, 4.1.2 Name/Role/Value.

**Fix**: `aria-label="Remove @sponsor.Name"` / `aria-label="Remove @attachment.DisplayName"`, etc.

### 9. `<th>` missing `scope` in six components (Medium)

**Where**: `About/About.razor:104-106`, `History/Awards/HighBlock.razor:50-52`, `History/Awards/HighAverage.razor:54-58`, `History/Champions/BowlerTitlesModal.razor:58-61`, `Account/Users/Users.razor:58-61`, `History/Champions/YearView.razor:21-23`.

**Problem**: none of these set `scope="col"`. `Tournaments/Detail/ResultsTable.razor:9-12` and `Stats/SeasonStats.razor`'s tables already do this correctly — same fix, just applied inconsistently.

**WCAG**: 1.3.1 Info and Relationships.

**Fix**: add `scope="col"` to each `<th>`.

### 10. `role="img"` wraps focusable, individually-labeled children (Medium)

**Where**: `Tournaments/Schedule/SeasonTimeline.razor:3-4` (outer `role="img"`), `:37-41` (per-tournament `tabindex="0" aria-label="..."` dots inside it).

**Problem**: `role="img"` tells assistive tech to treat the whole subtree as one flat image described by the container's single label — descendant roles/labels/focusability become unreliable across browser/AT combinations, even though the dots stay tabbable for sighted keyboard users.

**WCAG**: 1.1.1 Non-text Content, 4.1.2 Name/Role/Value.

**Fix**: use `role="group"` (or a visually-hidden summary) on the container instead of `role="img"`, so the real per-dot semantics aren't suppressed.

### 11. `AccountMenu` uses ARIA menu roles without menu keyboard behavior (Medium)

**Where**: `Layout/AccountMenu.razor:18,24,48` (`role="menu"`/`role="menuitem"`), `AccountMenu.razor.js` (clipboard helper only, no keyboard handling), `AccountMenu.razor.css:59-64` (`:focus-within`/`:hover` open).

**Problem**: unlike `NavMenu.razor`/`NavMenu.razor.js`, which implements full arrow-key/Home/End/Escape menu navigation, `AccountMenu` has none of that behind its `role="menu"` markup. Tab-based access does work via `:focus-within`, but `aria-expanded="false"` on the trigger (line 18) is static and never flips to `"true"`.

**WCAG**: 4.1.2 Name/Role/Value.

**Fix**: either drop `role="menu"/"menuitem"` for a plain nav list (simplest, since `:focus-within` already works), or port `NavMenu.razor.js`'s keyboard model and keep `aria-expanded` in sync.

### 12. Muted gray text fails contrast in dark mode (Medium)

**Where**: `wwwroot/neba_theme.css:552` (`.neba-autocomplete-clear`), `:609` (`.neba-autocomplete-empty`).

**Problem**: both use `--neba-gray-500` (`#737373`) directly, with no dark-mode remap. Against the dark background (`#0D1117`) this measures ~4.0:1, under the 4.5:1 AA minimum.

**WCAG**: 1.4.3 Contrast (Minimum).

**Fix**: introduce a `--neba-text-muted` token — light mode: `--neba-gray-500`; dark mode: a lighter value (e.g. `#9AA4AF`) clearing 4.5:1 — and use it at both call sites instead of the raw gray token.

### 13. Invalid ARIA role (Low)

**Where**: `Tournaments/Schedule/MergedSeasonNote.razor:3` — `role="note"`.

**Problem**: `"note"` is not a valid WAI-ARIA role; browsers drop unrecognized role values, so the element gets no exposed role at all. Visible text is unaffected, so impact is low.

**WCAG**: 4.1.2 Name/Role/Value.

**Fix**: remove the invalid role, or use `role="status"`/plain semantic markup.

### 14. Home page skips a heading level (Low)

**Where**: `Pages/Home.razor:19` (`h1`), followed directly by four `h3`s (lines 78, 97, 116, 135 — quick-link cards) before the first `h2` at line 146.

**WCAG**: 1.3.1 Info and Relationships (heading structure best practice).

**Fix**: bump the quick-link card headings to `h2`, or add an `h2` "Quick Links" wrapper before them.

### 15. Weak focus indicator on timeline dots (Low)

**Where**: `Tournaments/Schedule/SeasonTimeline.razor.css:99-104`:

```css
.season-timeline__dot:focus-visible { transform: scale(1.5); outline: none; }
```

**Problem**: the only focus indicator is a 1.5x scale-up of a small colored dot — no outline/ring/shadow. Technically visible but weak, especially for low-vision users.

**WCAG**: 2.4.7 Focus Visible.

**Fix**: add a visible outline/box-shadow ring on `:focus-visible` alongside (or instead of) the scale transform.

### 16. Inconsistent `aria-live` on async-loaded list regions (Low)

**Where**: `Tournaments/Schedule/Tournaments.razor:62` correctly marks its list `aria-live="polite"`. `News/NewsList.razor`, `BowlingCenters/BowlingCenters.razor`, and `History/Champions/Champions.razor` load lists the same way but have no `aria-live` anywhere.

**WCAG**: 4.1.3 Status Messages.

**Fix**: apply the same `aria-live="polite"` pattern already established on the Tournaments page.

### 17. Secondary button keeps light styling in dark mode (Low)

**Where**: `wwwroot/neba_theme.css:171-179` (`.neba-btn-secondary`).

**Problem**: the button keeps its light-gray chip styling under `.dark`. Contrast inside the button is still fine (~12:1) — this is a visual-consistency issue, not a WCAG failure.

**Fix**: cosmetic only; revisit alongside any broader dark-theme visual pass, no urgency.

## Already verified as correct (no action needed)

- Every `<img>` site-wide (logo, sponsor, tournament, Hall of Fame images) carries either a descriptive `alt` or an explicit `alt=""` next to adjacent visible text.
- `Layout/MainLayout.razor` landmarks (`header[role=banner]`, `nav`, `main#main-content[role=main][tabindex=-1]`, `footer[role=contentinfo]`) are correct, unique, and the skip link target matches (`Layout/MainLayout.razor:3-6`).
- `<html lang="en">` set (`App.razor:2`).
- `NavMenu.razor`/`NavMenu.razor.js` dropdown menus: full keyboard support (arrows, Home/End, Enter/Space, Escape with focus restoration, Tab-close) — a model implementation worth reusing for finding #11.
- `History/Champions/YearView.razor` and `TitleCountView.razor` collapsible headers: correct `role="button" tabindex="0"` + Enter/Space keydown pattern.
- `Tournaments/Schedule/TournamentHero.razor` and `TournamentUpcomingCard.razor` progress bars: correct `aria-valuenow/min/max` + `aria-label`.
- `Tournaments/Detail/ResultsTable.razor` and `Stats/SeasonStats.razor` data tables: correct `scope="col"`.
- Outline removal in `neba_theme.css`, `NavMenu.razor.css`, `MainLayout.razor.css` consistently pairs with a `:focus`/`:focus-visible` replacement — no genuine focus loss found beyond finding #15.
- No `tabindex` values greater than 0 found anywhere in the codebase.
- Icon-only controls (edit/delete/dismiss/close) consistently carry `aria-label`, outside the specific list-row cases in finding #8.
- `NebaModal` itself: correct `aria-modal`, `aria-labelledby`/`aria-describedby`, Escape handling, JS focus trap.
- `NebaToast` uses `aria-live="polite"`; `NebaAlert` selects `role="alert"` vs. `role="status"` by severity.
- Form fields go through the shared `FormLabel` convention; no bypasses found (label *association* for error text is the gap — see finding #1).

## Recommended order of work

Grouped so each pass fixes one class of problem across every place it recurs, rather than one file at a time.

1. **Form error announcement (#1)** — highest count of affected files (~30+), and the most common real user journey (submitting a form) is currently silent for screen-reader users.
2. **Modal/dialog focus trap (#2, #6)** — extend `NebaModal` usage/logic to `NebaDocument` and both Stats dialogs; one fix pattern, three sites.
3. **Missing accessible names (#3, #8)** — unlabeled selects and ambiguous repeated buttons; both are "add an `aria-label`" fixes, batchable together.
4. **Contrast fixes (#4, #5, #12)** — one CSS/token pass: dark-mode surface tokens, muted-text token, and the stat-value gray swap.
5. **Structural cleanup (#7, #9, #10, #13, #14, #16)** — keyboard support on the bowling-center card, `scope="col"` sweep, `role="img"` fix, invalid role removal, heading levels, `aria-live` consistency. All small, independent, low-risk.
6. **`AccountMenu` keyboard model (#11)** — either simplify away from `role="menu"` or port `NavMenu.razor.js`'s pattern; worth deciding deliberately rather than batching.
7. **#15, #17 opportunistically** — cosmetic/low severity, no dedicated pass needed.
