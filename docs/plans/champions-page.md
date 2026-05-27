# Champions Page — Implementation Plan

## Overview

Implement the Champions page (`/history/champions`) showing NEBA bowlers ranked by tournament title count. The page has two views ("By Titles" and "By Year") and a per-bowler modal. Tournament entries in the modal are **hyperlinks** to the tournament detail page (`/tournaments/{id}`), and the modal shows tournament name alongside date and type.

---

## Phase 1: API — Complete ✅

All contracts, handlers, endpoints, and tests are implemented and passing. `IBowlersApi` is registered in `ApiServicesConfiguration.cs`.

**API shape notes for Phase 2 mapping work:**

- `TournamentChampionResponse.TournamentDate` is `DateOnly` (not a pre-formatted string). The mapping extension formats it to display string (e.g. "Mar 2018").
- `BowlerTitleResponse.TournamentDate` is `DateOnly` (not separate `TournamentMonth`/`TournamentYear` ints). The mapping extension extracts `.Month` and `.Year` from it.
- Refit method on `ITournamentsApi` is `ListTournamentChampionsAsync` (not `ListChampionsAsync`).

---

## Phase 2: Website — Services and ViewModels (`Neba.Website.Server`)

All files go in `History/Champions/` (new folder).

### 2.1 ViewModels

| File | Properties |
| --- | --- |
| `ChampionsView.cs` | `TitleCount`, `Year` enum |
| `BowlerTitleSummaryViewModel.cs` | `BowlerId`, `BowlerName`, `TitleCount`, `HallOfFame` |
| `BowlerTitleViewModel.cs` | `BowlerId`, `BowlerName`, `TournamentId` (string), `TournamentMonth` (int), `TournamentYear` (int), `TournamentType`, `HallOfFame` |
| `TitlesByYearViewModel.cs` | `Year`, `Titles: IReadOnlyCollection<BowlerTitleViewModel>` |
| `TitleViewModel.cs` | `TournamentId` (string), `TournamentName`, `TournamentDate` (formatted string e.g. "Mar 2018"), `TournamentType` |
| `BowlerTitlesViewModel.cs` | `BowlerName`, `HallOfFame`, `Titles: IReadOnlyCollection<TitleViewModel>` |

### 2.2 Mapping Extensions

- `BowlerTitleMappingExtensions.cs` — C# 14 `extension` syntax
  - `TournamentChampionResponse` → `BowlerTitleSummaryViewModel` (used when grouping by bowler for "By Titles" view)
  - `TournamentChampionResponse` → `BowlerTitleViewModel` (used when grouping by year for "By Year" view; extract `.Month` and `.Year` from `TournamentDate`)
  - `BowlerTitlesResponse` → `BowlerTitlesViewModel` (for modal; format `TournamentDate` DateOnly → "Mar 2018" string)

### 2.3 Data Service

- `IChampionsDataService.cs` + `ChampionsDataService.cs`
  - `GetTitleSummariesAsync(CancellationToken)` → calls `ITournamentsApi.ListTournamentChampionsAsync`; flattens tournament→champions into (champion, tournament) pairs, groups by bowler, takes `HallOfFame` from any entry in the group, counts entries for `TitleCount` → `IReadOnlyCollection<BowlerTitleSummaryViewModel>`
  - `GetTitlesByYearAsync(CancellationToken)` → calls `ITournamentsApi.ListTournamentChampionsAsync`; groups tournaments by year (parsed from `TournamentDate.Year`) → `IReadOnlyCollection<TitlesByYearViewModel>` (each method fetches independently — no shared cache)
  - `GetBowlerTitlesAsync(BowlerId, CancellationToken)` → wraps `IBowlersApi.GetBowlerTitlesAsync`
  - All calls go through `ApiExecutor` for telemetry/error handling
- Register in `WebApplicationBuilder` DI extensions

---

## Phase 3: UI Components

### 3.1 Two-Step Design Process

Claude Design produces HTML/CSS only. A second prompt then converts that markup into Blazor components.

#### Step 1 — Claude Design Prompt (HTML/CSS)

> Build a static HTML/CSS mockup for a champions page for the NEBA bowling association website. Use CSS variables `--neba-blue-700`, `--neba-blue-900`, `--neba-blue-100`, `--neba-blue-300` for NEBA branding.
>
> **Page structure**: A segmented control to toggle between two views — "By Titles" (default) and "By Year".
>
> **"By Titles" view**: Collapsible card grid grouped by title count, all expanded by default. Three visual tiers — Elite (20+ titles, gold/amber), Mid (10–19, blue gradient), Standard (1–9, white). Each card shows bowler name + a small HOF badge image if applicable. Cards are clickable.
>
> **"By Year" view**: Collapsible sections per year, most recent first. Each section contains a table with columns: Month | Tournament Type (pill/badge) | Champions (clickable names styled as links).
>
> **Bowler Titles Modal**: A modal that shows a bowler's full title history. Header shows bowler name + title count + HOF badge. Body is a table with columns: `#` | `Tournament` | `Date` | `Type`. The Tournament column is an `<a>` tag (blue hyperlink). Date shows formatted month+year (e.g. "Mar 2018"). Type shows as a small pill/badge. Include a loading spinner state and an error state. On mobile portrait, show a summary card (title count + HOF) above the table; on landscape/desktop move it inline to the modal header.
>
> Use hardcoded placeholder data throughout. Produce complete, self-contained HTML and CSS.

#### Step 2 — Blazor Conversion Prompt

After Claude Design produces the HTML/CSS, paste the output and use this prompt:

> Convert this HTML/CSS into Blazor Server components following these conventions:
>
> - Split into: `Champions.razor`, `TitleCountView.razor`, `YearView.razor`, `BowlerTitlesModal.razor` — each with a paired `.css` file
> - Use `@code` blocks for state; replace hardcoded data with the viewmodel types listed below
> - Replace the segmented control with `<NebaSegmentedControl>`, the modal wrapper with `<NebaModal>`, the loading spinner with `<NebaLoadingIndicator>`, and error banners with `<NebaAlert>`
> - Tournament links in the modal use `<a href="/tournaments/@title.TournamentId">@title.TournamentName</a>`
> - Follow Razor parser rules: avoid bare `<` in switch expressions inside `@code`; use `@` prefix on all C# attribute values; use `[Parameter, EditorRequired]` (not `required`) on component parameters
> - Scoped CSS only — no inline styles
>
> **Viewmodel types**:
>
> - `TitleViewModel`: `TournamentId` (string), `TournamentName`, `TournamentDate` (string), `TournamentType` (string)
> - `BowlerTitlesViewModel`: `BowlerName`, `HallOfFame`, `Titles: IReadOnlyCollection<TitleViewModel>`
> - `BowlerTitleSummaryViewModel`: `BowlerId`, `BowlerName`, `TitleCount`, `HallOfFame`
> - `BowlerTitleViewModel`: `BowlerId`, `BowlerName`, `TournamentId` (string), `TournamentMonth` (int), `TournamentYear` (int), `TournamentType`, `HallOfFame`
> - `TitlesByYearViewModel`: `Year`, `Titles: IReadOnlyCollection<BowlerTitleViewModel>`

### 3.2 Files to Produce

| File | Notes |
| --- | --- |
| `Champions.razor` + `.css` | Page shell, segmented control, view toggle, modal wiring |
| `TitleCountView.razor` + `.css` | Tiered collapsible card grid |
| `YearView.razor` + `.css` | Collapsible year table; champion names open modal |
| `BowlerTitlesModal.razor` + `.css` | Modal with tournament name hyperlinks |

---

## Phase 4: Tests

Following CLAUDE.md TDD conventions — write failing test first, then implement.

| Layer | Test Type | Coverage |
| --- | --- | --- |
| Mapping extensions | Unit | All response → VM mappings; `TournamentDate` DateOnly → "Mar 2018" string formatting; `TournamentMonth`/`TournamentYear` extracted from `DateOnly`; `HallOfFame` taken from first row when grouping by bowler |
| `BowlerTitlesModal` | bUnit | Hyperlinks render with correct `href`; modal open/close resets state; loading state; error state; HOF badge conditional |

All tests: `[IntegrationTest]`/`[UnitTest]` + `[Component("Champions")]` traits + `DisplayName`.

---

## Resolved Decisions

**`BowlerTitleViewModel` includes `TournamentId`** — included on `BowlerTitleViewModel` alongside the month/year/type fields. Keeps the year view data path consistent and avoids a second DTO shape.

**Champions list lives under `/tournaments/champions`** — Tournament champions are a tournament concept. Both views are served by a single `GET /tournaments/champions` endpoint returning tournament-grouped data. The data service re-groups per view.

**Response grouped by tournament, not flat rows** — `TournamentChampionResponse` contains `Champions: IReadOnlyCollection<ChampionResponse>`. Data service flattens and re-groups: "By Titles" groups by bowler; "By Year" groups by year.

**`HallOfFame` on each `ChampionResponse`** — bowler-level attribute carried on every entry. Data service reads from any entry for that bowler when building the "By Titles" view.

**`TournamentDate` kept as `DateOnly` in all responses** — both `TournamentChampionResponse` and `BowlerTitleResponse` use `DateOnly` rather than pre-formatted strings or separate int fields. Mapping extensions handle display formatting and field extraction.

