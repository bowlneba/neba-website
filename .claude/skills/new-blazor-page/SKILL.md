---
name: new-blazor-page
description: Add a routable Blazor page in Neba.Website.Server with its title, render mode, authorization, form guard, table class, bUnit test, e2e spec and mock. Use when adding a new page or a new mode of a page (create, edit, detail, list). Not for API endpoints (new-endpoint), visual design critique (ui-design agent) or an e2e-only change (e2e agent).
---

# New Blazor page

Read `docs/learnings/blazor-ui.md` and the "Blazor and UI" rules in `CLAUDE.md`. The `blazor-ui` agent does this work; this is the procedure.

## Copy this

| Page type | Copy from `src/Neba.Website.Server/` |
|---|---|
| Public page with an API call and loading state | `Sponsors/Sponsors.razor` |
| Admin create or edit form | `News/CreateArticle.razor`, `Tournaments/CreateTournament.razor` |
| List page with a "add" button | `Sponsors/Sponsors.razor`, `News/NewsList.razor` (`FabCreateButton`) |
| Table with more than three columns | `Stats/SeasonStats.razor` (`neba-data-table`) |
| Long-list picker | `Tournaments/CreateTournament.razor` (`NebaAutocomplete`) |
| bUnit test | `tests/Neba.Website.Tests/Sponsors/CreateSponsorTests.cs` |

## Steps

1. **Header.** `@page`, `@rendermode` (copy the form your neighbor uses: `@rendermode InteractiveServer` or `@rendermode @(new InteractiveServerRenderMode(prerender: false))`), and `<PageTitle>{Page} - BowlNEBA</PageTitle>`. Every routable page has both. `Login` and `Logout` are the exceptions.
2. **Authorization.** An admin page wraps its body in `<AuthorizeView Policy="@Permissions.X.PolicyName">`, as `CreateArticle.razor` does. A new policy needs the `backend-api` and `policy-documentation` work first.
3. **Data.** Call the API through `ApiExecutor` and the Refit interface in `Neba.Api.Contracts`. A missing endpoint is a `new-endpoint` task. A page stays a thin orchestrator; it does not shape data.
4. **Forms.** Use `DirtyFormGuard` on every data-entry page, and call `MarkDirty()` for anything not bound through an `InputBase`. Mark required fields with `FormLabel`, not optional ones (login forms are exempt). Per-keystroke behavior goes in a colocated `.razor.js`, never in an `@onkeydown` handler. Pickers over about 20 items use `NebaAutocomplete`.
5. **Display.** Ordinals and plurals use Humanizer (`Ordinalize`, `ToQuantity`). A table over three columns gets `class="neba-data-table"`. An admin list adds items through `FabCreateButton`.
6. **Razor `@code` limits.** No `< N =>` patterns, no `$"{x}"` interpolation, `@` on component attribute values, `[Parameter, EditorRequired]` instead of `required`.
7. **Tests.** Add a bUnit test. Build the Refit response with `StubApiResponse<T>` (`docs/learnings/testing.md`). Add a Playwright spec under `tests/e2e/` and a mock route in `tests/e2e/mock-api/mock-api-server.ts` when the page is new; the decision table is in the same learnings file.
8. **Docs.** A user-facing page gets a help doc (`help-documentation` skill).

## Gotchas

- An `HttpClient` handler never injects a scoped service. Reach circuit state through `CircuitServicesAccessor`. In a circuit, `HttpContext` holds the cookie from when the connection opened.
- Per-keystroke behavior in C# (`@onkeydown`) races on the SignalR round trip. Put it in a `.razor.js` module.
- A new `required` field on a Contracts DTO needs the e2e mock updated or Playwright fails every time.
- E2E failure tests use a 4xx (a 5xx retries past the expect timeout). `MOCK_UPCOMING_TOURNAMENTS` stays empty.
- `reference/components/` (`NebaDropdown`, `NebaSegmentedControl`) holds design sketches, not production code. Use `NebaAutocomplete` for long pickers.

## Checklist

- [ ] `@page`, `@rendermode`, `<PageTitle>`
- [ ] Authorization explicit
- [ ] `DirtyFormGuard` and `FormLabel` on forms
- [ ] `neba-data-table` and `FabCreateButton` where they apply
- [ ] bUnit test, e2e spec and mock route
- [ ] Help doc if user-facing
- [ ] `dotnet build src/Neba.Website.Server` passes
