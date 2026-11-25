# Wire up the in-app help button (step 6)

Read this in step 6. It applies only when step 3 found a UI.

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
