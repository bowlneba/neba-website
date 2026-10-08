# Blazor and UI learnings

Moved from CLAUDE.md. Read the relevant section before changing Blazor components, forms, or pages.

### Custom Interactive Blazor Server Inputs — Keyboard Handling Must Live in JS, Not C#

When building a custom input component (`InputBase<T>` subclass) that needs synchronous per-keystroke behavior — auto-advance between segments, filtering keys, navigating on a separator character — **do not** drive that behavior with server-side C# event handlers (`@onkeydown`, `ElementReference.FocusAsync()`), even though it works fine in manual testing.

**Why**: Blazor Server round-trips every event over SignalR. A C#-driven `FocusAsync()` call to move focus to the next segment is asynchronous and network-latency-bound. Real (or automated) typing at normal speed can send the next keystroke before the previous round-trip's focus change has been applied client-side, landing digits in the wrong element. This was found building `NebaDateInput.razor` (see below): typing `9/5/2026` with a fast automated Playwright script scattered digits across the wrong segments (`day` got `20`, `year` got `26`) even though the identical logic worked correctly when each keystroke was typed slowly. It reproduces with real typing speed too, not just fast automation — the race is inherent to the round-trip, not a test artifact.

A second, related trap: even without the focus race, letting the browser's default keydown action fire (e.g. inserting a literal `/` character) while a C# `oninput` handler sanitizes it back to the *same string* as the previous render causes Blazor's virtual-DOM diff to skip updating the real DOM — the stray character stays visibly stuck in the input even though the bound C# state is correct. `preventDefault` can't be applied conditionally per-key via Razor's static `@onkeydown:preventDefault` directive (it's fixed per render, not per keystroke), so this can't be patched from the C# side either.

**Fix — do all interactive keyboard handling in a colocated JS module** (`Component.razor.js`, matching the existing `RichTextEditor.razor`/`.razor.js` pattern): attach native `keydown`/`input` listeners directly in JS, handle digit filtering/auto-advance/segment navigation/backspace synchronously with zero network round-trips, and call `preventDefault()` selectively per key inline (trivial in JS, not expressible in Razor). JS reports the final composed value back to .NET via a single `[JSInvokable]` method (e.g. `NotifySegmentsChanged`) — .NET is a passive listener that only computes/validates the resulting value, never drives focus or interaction itself.

**Testing implication**: bUnit renders the component tree but does not execute real browser JS, so bUnit tests for a component built this way cannot simulate typing via `.Change()`/`.Input()` on the DOM — call the `[JSInvokable]` method directly on the component instance instead (`cut.InvokeAsync(() => dateInput.Instance.NotifySegmentsChanged(...))`), same pattern as `RichTextEditorTests.NotifyContentChanged`. The actual keyboard-interaction logic (auto-advance, `/` navigation, filtering) needs to be covered by Jest tests against the `.razor.js` file directly (jsdom does execute real JS), not by bUnit.

Applied in `NebaDateInput.razor`/`.razor.js` (`src/Neba.Website.Server/Components/`), which replaces `InputDate` for `DateOnly?` fields — see `docs/plans/create-tournament.md`'s Components section for the full story (this started as a Safari-only bug report: WebKit's native `<input type="date">` doesn't reliably auto-advance segments when typing, unlike Chromium/Firefox).

### Dirty Form Guard — Warn Before Losing Unsaved Changes

Every data-entry page (any page with an `EditForm`, file uploads, or similar user input) must warn the user before they lose unsaved changes via Cancel, in-app navigation, or browser refresh/close/address-bar navigation. Use the shared `Components/DirtyFormGuard.razor` component — do not hand-roll this per page.

**How it works**:

- `<DirtyFormGuard IsDirty="@_isDirty" />` wraps Blazor's built-in `<NavigationLock>`:
  - `ConfirmExternalNavigation="@IsDirty"` triggers the browser's **native** "leave site?" dialog for refresh/close/address-bar navigation/back-forward — this is built into Blazor and cannot be customized (no custom JS/`beforeunload` interop needed or possible).
  - `OnBeforeInternalNavigation` intercepts in-app navigation (Cancel button's `NavigationManager.NavigateTo(...)`, `NavLink` clicks, etc.), calls `context.PreventNavigation()`, and shows a custom `ConfirmActionModal` ("Discard unsaved changes?" / Leave / Stay). Confirming re-issues the navigation with a one-shot bypass flag so the guard doesn't re-intercept its own confirmed navigation.
- The **page** owns dirty-tracking and passes the result in — the guard has no opinion on how dirty state is computed. Pattern (see `CreateArticle.razor`):
  - Create the `EditContext` explicitly in the constructor (`EditForm EditContext="_editContext"` instead of `Model="_model"`) and subscribe `_editContext.OnFieldChanged += (_, _) => MarkDirty();` — this covers any field bound through an `InputBase` descendant (`InputText`, `InputSelect`, `InputDate`, etc.) for free.
  - Anything **not** wired through `EditContext` needs an explicit `MarkDirty()` call: components that aren't `InputBase` (e.g. a custom `RichTextEditor` using plain `Value`/`ValueChanged`, not `@bind-Value` through an Input component), raw `<select>`/`@onchange` bindings, file upload add/remove callbacks, etc.
  - Reset `_isDirty = false` right before navigating away after a **successful** save — otherwise the guard fires again on the post-save `NavigateTo`.
  - Unsubscribe `_editContext.OnFieldChanged` in `DisposeAsync`.
- Login/credential-only forms are excluded — losing a half-typed password isn't the kind of "lost work" this guards against.

Enforced going forward via `.github/instructions/pull-request-review.instructions.md` (Blazor section + Review Checklist).

### Required-Field Indicator — Mark Required, Not Optional

A bare asterisk next to a label is no longer the right pattern: it isn't reliably announced by screen readers, and its meaning ("required"? "important"? a footnote?) isn't self-evident without a legend. Current guidance (WCAG, GOV.UK, USWDS) is to mark the *minority* case in visible text, not a symbol.

**Decision for this app: always mark required fields with the text "(required)"**, never optional fields. This was chosen over "mark optional fields" (the more common guideline when a form is mostly required) because an audit of the app's five real forms showed the opposite is true here — Sponsor forms are ~87% optional (3 of ~23 fields required), so marking optional fields there would tag most of the form instead of the few fields that actually matter. Marking required fields instead stays cheap on every form regardless of its required/optional ratio (3–4 tags at most).

**How it works**: use the shared `Components/FormLabel.razor` component instead of a bare `<label>` — do not hand-roll labels on new form fields.

```razor
<FormLabel TargetId="name" For="@(() => _model.Name)">Name</FormLabel>
<InputText id="name" @bind-Value="_model.Name" class="neba-input" placeholder="Sponsor name" />
```

- `TargetId` renders the label's `for` attribute, same as a plain `<label>`.
- `For` is an expression identifying the bound model property (same pattern as `ValidationMessage`'s `For`). `FormLabel` reflects on it via `FieldIdentifier.Create(For)` to check for a `[Required]` `DataAnnotation`, and renders "(required)" automatically when present — there is no manual `IsRequired`/`IsOptional` parameter to set, so the label can never drift out of sync with the model's actual validation attribute.
- Labels for fields that aren't bound to a `[Required]`-annotated model property (file uploads, custom pickers with a plain `<select>`, checkboxes) stay as plain `<label>` — `FormLabel` only applies where there's a real `For` expression to reflect on.
- **Login/credential-only forms are excluded**, same rationale as the Dirty Form Guard exception above: when every field on a form is required (e.g. `Login.razor`), tagging all of them adds no information, so those forms keep plain `<label>` elements with no indicator at all.

Applied to `CreateSponsor.razor`, `EditSponsor.razor`, `CreateArticle.razor`, `EditArticle.razor`, `CreateTournament.razor`. `Login.razor` intentionally left unmarked (see exclusion above).

### Razor @code Block — Parser Limitations

Two patterns that break Razor's lexer even inside `@code { }` blocks:

1. **Relational patterns `< N =>` in switch expressions** — `<` followed by a space then a digit is misread as an HTML tag start, causing the brace tracker to prematurely close the `@code` block. Use `if`/`else` with `>=` instead (e.g., `if (pct >= 90) return "full";`). `<=` (less-than-or-equal) is NOT affected — only bare `<` followed by a space.

2. **String interpolation with `{}` inside @code** — `$"prefix:{expr}suffix"` braces inside string interpolations in `@code` can confuse the Razor brace counter. Use string concatenation instead: `"prefix:" + expr + "suffix"`.

3. **Component attribute values always need `@` for C# expressions** — `Foo="fieldName"` passes the literal string `"fieldName"`, not the field's value. Always write `Foo="@fieldName"` for fields/properties, `Foo="@(expr)"` for expressions with operators (e.g. null-forgiving `!`, null-coalescing `??`).

4. **Blazor parameters use `[EditorRequired]` not C# `required`** — the `required` keyword on Blazor `[Parameter]` properties causes compile errors (CS0246/CS7014). Always use `[Parameter, EditorRequired]` with a default initializer (`= default!;`, `= string.Empty;`, `= [];`).

### List Page "Add New" Pattern — Floating Action Button

Any admin-gated list page (News, and future Sponsors/Bowling Centers/etc. admin views) uses the shared `FabCreateButton` component (`Neba.Website.Server/Components/FabCreateButton.razor`) as its "create new" entry point — a circular button fixed to the bottom-right of the viewport (`.neba-fab` in `wwwroot/neba_theme.css`), not a button embedded in the page's gradient title bar. Usage:

```razor
<AuthorizeView Policy="@Permissions.CreateArticle.PolicyName">
    <Authorized>
        <FabCreateButton Href="/news/new" Label="Create Article" />
    </Authorized>
</AuthorizeView>
```

`Href` is the create-page route; `Label` is both the accessible name and hover tooltip (e.g. "Create Article", "Add Sponsor"). This was chosen over embedding the button in the gradient `page-title-bar` because a solid/glass button there had low, position-dependent contrast against the gradient and competed visually with the hero content below it — the FAB sits outside page content entirely, at a fixed screen position, so it doesn't fight the header for attention and its position/behavior is identical across every list page it's added to.

### Long-List Picker Pattern — `NebaAutocomplete` vs. `InputSelect`

`InputSelect` (a native `<select>`) is fine for a short, fixed list (tournament type, U.S. state) where the whole list fits on screen and scanning it is fast. It stops working once the list grows past roughly 15–20 items — a Bowling Center picker with 80+ centers turns into either scrolling a long native dropdown or typing letters to jump-search it, both of which are slow and don't let the user search by anything other than the first letter of the display text.

**Decision: use the shared `Components/NebaAutocomplete.razor` component for any picker backed by a list that can grow past ~20 items or where the natural search key isn't the start of the display string** (e.g. searching a bowling center by city, not just name). It renders a single text `<input>` that filters an in-memory `Items` collection as the user types (substring match anywhere in the display text, not just prefix), with arrow-key navigation, a "no matches" state, and a clear (×) button for optional selections. First applied to `CreateTournament.razor`'s Bowling Center field, replacing an `InputSelect` over 80+ centers.

**Usage**:

```razor
<NebaAutocomplete Id="bowling-center" TValue="string" TItem="BowlingCenterSummaryResponse"
                   Value="@_model.BowlingCenterCertificationNumber"
                   ValueChanged="HandleBowlingCenterChanged"
                   Items="@_bowlingCenters"
                   DisplayText="@(center => center.Name + " — " + center.City + ", " + center.State)"
                   ItemValue="@(center => center.CertificationNumber)"
                   Placeholder="Search bowling centers..."
                   EmptyLabel="Not yet assigned" />
```

- `TValue`/`TItem` generics mirror the existing `NebaDropdown` design sketch in `reference/components/` (never implemented, kept as a design reference only — this component is the production version, built narrower: free-text filtering rather than that sketch's toggle-to-search combobox).
- Not an `InputBase<T>` — it's a plain `Value`/`ValueChanged` component like `OilPatternPicker`/`FileUpload`, so the hosting page must call `MarkDirty()` itself from the `ValueChanged` handler (the shared `EditContext.OnFieldChanged` hook only fires for `InputBase` descendants).
- **Keyboard nav (arrow keys/Enter/Escape) is handled server-side in C#** via `@onkeydown`, unlike `NebaDateInput`'s per-keystroke JS-only handling — the race condition documented under NebaDateInput's Learnings entry is specific to multiple segment elements fighting over focus mid-type; a single text input filtering a list has no such race, so keeping this in C# (consistent with how the rest of the app's server-rendered forms already work) is fine.
- **Click-outside-to-close still needs JS** (no Blazor-native equivalent) — colocated `NebaAutocomplete.razor.js`, same `initialize(containerId, dotNetHelper)`/`dispose(containerId)` shape as `NebaDateInput.razor.js`, using a capturing `mousedown` listener on `document`.

### Page Titles (`<PageTitle>`)

Every routable page must have a `<PageTitle>` component. Sub-components (cards, modals, skeletons) do not.

**Format**: `{Page Name} - BowlNEBA` (dash separator, BowlNEBA suffix). For dynamic detail pages: `@model.Name - BowlNEBA`.

**`<HeadOutlet>` must use `@rendermode="InteractiveServer"`** in `App.razor` — without it, Safari does not update the tab title on client-side navigation (Chrome is more lenient). Static render mode means `<PageTitle>` updates never reach the browser's `document.title` in Safari.

**Every routable page must also declare `@rendermode InteractiveServer`** — if a page is static SSR (no `@rendermode`), the interactive `HeadOutlet` circuit boots with no `<PageTitle>` registered and clears the title (visible as a flash then blank tab). Pages with no async data loading use `@rendermode InteractiveServer` (prerender: true default); data-loading pages use `@rendermode @(new InteractiveServerRenderMode(prerender: false))` to avoid a flash of empty content.

**Exception — auth pages that call `SignInAsync`/`SignOutAsync` (`Login.razor`, `Logout.razor`) intentionally omit `@rendermode`.** The auth cookie write must happen in the static-SSR pipeline; inside an established `InteractiveServer` circuit, the response has already started and `SignInAsync`/`SignOutAsync` cannot write the `Set-Cookie` header. These pages accept the title-flash tradeoff in exchange for a working cookie write.

### Display text: ordinals and plurals (Humanizer)

See [ADR-0010](../adr/0010-humanizer-for-ui-display-formatting.md). Use `ToOrdinal()` (wraps `Ordinalize(DisplayCulture.English)`) for places and ranks, and `ToQuantity` for counts, instead of hand-written ternaries. `ToQuantity(n, ShowQuantityAs.None)` returns only the noun, for markup where the number sits in its own element. Do not use Humanizer for dates or currency. CA1304 fails the build on culture-less calls.
