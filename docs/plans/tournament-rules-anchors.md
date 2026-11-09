# Tournament Rules Anchors and Detail-Page Link

Add short anchors to each tournament-type section of `/tournaments/rules` (for example `#invitational`), and add a link on the tournament detail page that takes the user to the matching section. Release: v1.1.4.

## Decisions locked in during scoping

- **Not critical.** No NFR checkpoint.
- **No new permission or role.** Both pages are public.
- **Anchor map lives in code** (no configuration, no database). Adding a special tournament type needs a code change.
- **Slug maps to rule number, with a title-keyword fallback.** The page finds the heading whose ID starts with `rule-{number}-`. If that heading's text lacks the entry's keyword (the rules were renumbered), the page falls back to the first rule heading that contains the keyword. A renumber (601 to 701) then needs no deploy. It breaks only if a retitle that drops the keyword and a renumber happen together.
- **Types with a link:** Singles (400), Doubles (401), Trios (402), Baker (403), Tournament of Champions (600), Invitational (601), Masters (602), Non-Champions (603). Other types get no link.
- **When the link shows:** only for tournaments that are not completed or truncated, because rules change and an old tournament must not point at today's rules. Cancelled tournaments also hide the link, so it shows for Scheduled tournaments only.
- **Link style: slide-out (Option 2).** A button next to the type chips opens a panel with the tournament type's rule section. The panel footer links to the full rules page at the same anchor.
- Existing `#rule-601-...` and `#h.xxxx` links keep working.

| Slug | Rule | Keyword (fallback) | Tournament type |
|---|---|---|---|
| `singles` | 400 | Singles Tournament Format | Singles |
| `doubles` | 401 | Doubles Tournament Format | Doubles |
| `trios` | 402 | Trios Tournament Format | Trios |
| `baker` | 403 | Baker Tournament Format | Baker |
| `tournament-of-champions` | 600 | Tournament of Champions | Tournament of Champions |
| `invitational` | 601 | Invitational | Invitational |
| `masters` | 602 | Masters | Masters |
| `non-champions` | 603 | Non-Champion | Non-Champions |

Keywords are full phrases where a short word would also match other rules (for example "Singles" also appears in Rule 102, "Singles Squad Times"). The fallback looks only at rule headings whose ID matches `rule-<digits>-`, so lettered sub-rules such as `703e. Invitational` never match.

## Phase 1: API

**No API changes.** Everything this feature needs already exists:

- **Heading IDs:** `HtmlProcessor.GenerateAnchorId` already gives each rule heading a stable ID built from its text. "Rule 601- Invitational Tournament Rules" becomes `rule-601-invitational-tournament-rules`, and "Rule 403: Baker Tournament Format" becomes `rule-403-baker-tournament-format`. Every rule heading starts with `rule-{number}-`, which is what the UI will match on.
- **Tournament type:** the tournament detail response already carries the type, and the detail page already shows it as a badge.
- **Status:** the detail view model already exposes `IsScheduled`, `IsTruncated` and `IsCancelled`.

| Layer | Change |
|---|---|
| Domain | None. The anchor map is a presentation concern, so it stays out of the `TournamentType` SmartEnum. |
| Application | None |
| Infrastructure | None |
| API | None |
| Contracts | None |
| Tests | None |

Why the map is not on the API: the slug is a URL detail of the website's rules page, and the API does not know about that page. Putting it in `Neba.Api.Contracts` would couple the two for no gain.

Deferred to Phase 2: the map itself, the anchor scroll, and the detail-page link.

## Phase 2: UI

All work is in `Neba.Website.Server` (Server project: pages render on the server, no browser-only need). No new Refit method: the slide-out and the rules page both call the existing `IDocumentsApi.GetDocumentAsync("tournament-rules")`. No data-entry form, so no `DirtyFormGuard`. `TournamentRules.razor` and `TournamentDetail.razor` already have `<PageTitle>` and `InteractiveServer` (prerender off). No FAB or list page.

**Chosen design: Option 2, slide-out.** The link on the detail page opens a panel with that tournament type's rule section. The panel footer links to the full rules page at the same anchor.

### How the pieces fit

- One resolver for "which heading does this slug mean" lives in `NebaDocument.razor.js` (`resolveAliasHeading`). Both the rules page scroll and the slide-out's section extraction call it, so the number-then-keyword logic exists once.
- The detail page fetches the rules document only when the user opens the panel, then caches it for that page instance. The API already caches the document.
- The browser slices out the section and sends back only that HTML. The hop server to browser sends the whole document (about 60 KB) once per open; the return trip is small, well under SignalR's 32 KB default receive limit for a single rule section. If a section ever grows past that, move the slice into C# with `HtmlAgilityPack` (already referenced by this project).

### Pages / Components

**New: `Documents/TournamentRulesAnchors.cs`**

```csharp
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Neba.Website.Server.Documents;

/// <summary>Where a short anchor points in the rules document.</summary>
/// <param name="HeadingIdPrefix">Prefix of the heading ID, such as <c>rule-601-</c>.</param>
/// <param name="TitleKeyword">Text the heading must contain; used to find the rule again after a renumber.</param>
internal sealed record AnchorAlias(string HeadingIdPrefix, string TitleKeyword);

internal sealed record TournamentRulesAnchor(string Slug, int RuleNumber, string TitleKeyword)
{
    public string Href => TournamentRulesAnchors.RulesPath + "#" + Slug;

    public AnchorAlias Alias => new(
        "rule-" + RuleNumber.ToString(CultureInfo.InvariantCulture) + "-",
        TitleKeyword);
}

internal static class TournamentRulesAnchors
{
    internal const string RulesPath = "/tournaments/rules";
    internal const string DocumentName = "tournament-rules";

    // Keyed by the tournament type display name the API returns.
    // Add an entry here when a new tournament type gets its own rule section (code change by design).
    private static readonly Dictionary<string, TournamentRulesAnchor> ByType =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Singles"] = new("singles", 400, "Singles Tournament Format"),
            ["Doubles"] = new("doubles", 401, "Doubles Tournament Format"),
            ["Trios"] = new("trios", 402, "Trios Tournament Format"),
            ["Baker"] = new("baker", 403, "Baker Tournament Format"),
            ["Tournament of Champions"] = new("tournament-of-champions", 600, "Tournament of Champions"),
            ["Invitational"] = new("invitational", 601, "Invitational"),
            ["Masters"] = new("masters", 602, "Masters"),
            ["Non-Champions"] = new("non-champions", 603, "Non-Champion"),
        };

    /// <summary>Slug to alias, handed to <c>NebaDocument</c> so <c>#invitational</c> scrolls to the right heading.</summary>
    internal static IReadOnlyDictionary<string, AnchorAlias> AnchorAliases { get; } =
        ByType.Values.ToDictionary(a => a.Slug, a => a.Alias, StringComparer.Ordinal);

    internal static bool TryGet(string tournamentType, [NotNullWhen(true)] out TournamentRulesAnchor? anchor)
        => ByType.TryGetValue(tournamentType, out anchor);
}
```

**New: `Documents/DocumentSectionSlideover.razor`** — a Blazor-driven slide-out that reuses the existing `neba-document-slideover-*` classes. Open state, Escape, focus trap and body scroll lock follow `NebaModal`.

```razor
@implements IAsyncDisposable
@inject IJSRuntime JsRuntime
@inject ILogger<DocumentSectionSlideover> Logger

<link rel="stylesheet" href="/neba-document.css" />

@if (IsOpen)
{
    <div class="neba-document-slideover active" role="dialog" aria-modal="true"
         aria-labelledby="@_titleId" tabindex="-1" @ref="_dialog"
         @onkeydown="HandleKeyDown" @onkeydown:stopPropagation="true">
        <div class="neba-document-slideover-overlay" @onclick="HandleClose"></div>
        <div class="neba-document-slideover-panel">
            <div class="neba-document-slideover-header">
                <h2 id="@_titleId" class="neba-document-slideover-title">@Title</h2>
                <button type="button" class="neba-document-slideover-close" @onclick="HandleClose" aria-label="Close rules">
                    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true">
                        <path d="M2.146 2.854a.5.5 0 1 1 .708-.708L8 7.293l5.146-5.147a.5.5 0 0 1 .708.708L8.707 8l5.147 5.146a.5.5 0 0 1-.708.708L8 8.707l-5.146 5.147a.5.5 0 0 1-.708-.708L7.293 8 2.146 2.854Z" />
                    </svg>
                </button>
            </div>

            <div class="neba-document-slideover-content">
                @if (IsLoading)
                {
                    <div class="neba-document-slideover-status">Loading rules...</div>
                }
                else if (!string.IsNullOrWhiteSpace(ErrorMessage))
                {
                    <div class="neba-document-slideover-status">@ErrorMessage</div>
                }
                else if (Content.HasValue)
                {
                    @Content
                }
            </div>

            <div class="neba-document-slideover-footer">
                @if (LastUpdated is { } updated)
                {
                    <span class="neba-document-slideover-updated">Last updated: @updated.ToString("MMMM d, yyyy")</span>
                }
                <a class="neba-btn neba-btn-primary" href="@FullDocumentHref">Open full rules page</a>
            </div>
        </div>
    </div>
}

@code {
    private IJSObjectReference? _module;
    private readonly string _titleId = "neba-section-slideover-title-" + Guid.NewGuid().ToString("N");
    private ElementReference _dialog;
    private bool _wasOpen;

    [Parameter, EditorRequired] public bool IsOpen { get; set; }
    [Parameter, EditorRequired] public EventCallback OnClose { get; set; }
    [Parameter, EditorRequired] public string Title { get; set; } = default!;
    [Parameter, EditorRequired] public string FullDocumentHref { get; set; } = default!;
    [Parameter] public MarkupString? Content { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public DateTimeOffset? LastUpdated { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/NebaModal.razor.js");
        }

        if (_module is not null)
        {
            await _module.InvokeVoidAsync("setBodyOverflow", IsOpen);

            if (IsOpen && !_wasOpen)
            {
                await _module.InvokeVoidAsync("focusElement", _dialog);
                await _module.InvokeVoidAsync("enableFocusTrap", _dialog);
            }
            else if (!IsOpen && _wasOpen)
            {
                await _module.InvokeVoidAsync("disableFocusTrap");
            }
        }

        _wasOpen = IsOpen;
    }

    private Task HandleClose() => OnClose.InvokeAsync();

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is "Escape" or "Esc")
        {
            await HandleClose();
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_module is null) return;

        try
        {
            await _module.InvokeVoidAsync("disableFocusTrap");
            await _module.InvokeVoidAsync("setBodyOverflow", false);
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException ex)
        {
            Logger.LogInformation(ex, "[DocumentSectionSlideover] JS module disposal skipped because the circuit disconnected.");
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "[DocumentSectionSlideover] Failed to dispose JS module.");
        }
    }
}
```

**New: `Tournaments/Detail/TournamentRulesLink.razor`** — renders the button and the slide-out. Renders nothing for a type with no rule section.

```razor
@implements IAsyncDisposable
@using Neba.Api.Contracts.Documents
@using Neba.Website.Server.Documents
@using Neba.Website.Server.Services
@using Neba.Website.Server.Time

@inject ApiExecutor ApiExecutor
@inject IDocumentsApi DocumentsApi
@inject IClientTimeZoneService ClientTimeZoneService
@inject IJSRuntime JsRuntime
@inject ILogger<TournamentRulesLink> Logger

@if (_anchor is not null)
{
    <button type="button" class="td-rules-link" aria-haspopup="dialog" @onclick="OpenAsync">
        <span class="material-symbols-outlined" aria-hidden="true">menu_book</span>
        @(TournamentType + " rules")
    </button>

    <DocumentSectionSlideover IsOpen="@_isOpen"
                              OnClose="@Close"
                              Title="@(TournamentType + " rules")"
                              FullDocumentHref="@_anchor.Href"
                              Content="@_content"
                              IsLoading="@_isLoading"
                              ErrorMessage="@_errorMessage"
                              LastUpdated="@_lastUpdated" />
}

@code {
    private IJSObjectReference? _module;
    private TournamentRulesAnchor? _anchor;
    private bool _isOpen;
    private bool _isLoading;
    private bool _loaded;
    private MarkupString? _content;
    private string? _errorMessage;
    private DateTimeOffset? _lastUpdated;

    [Parameter, EditorRequired]
    public string TournamentType { get; set; } = default!;

    protected override void OnParametersSet()
        => _anchor = TournamentRulesAnchors.TryGet(TournamentType, out var anchor) ? anchor : null;

    private void Close() => _isOpen = false;

    private async Task OpenAsync()
    {
        _isOpen = true;

        if (_loaded || _isLoading || _anchor is null)
        {
            return;
        }

        _isLoading = true;
        _errorMessage = null;

        var result = await ApiExecutor.ExecuteAsync(
            "Documents",
            "GetTournamentRules",
            ct => DocumentsApi.GetDocumentAsync(TournamentRulesAnchors.DocumentName, ct));

        if (result.IsError)
        {
            _errorMessage = "We couldn't load the rules. Open the full rules page instead.";
        }
        else
        {
            var section = await ExtractSectionAsync(result.Value.Html, _anchor.Alias);

            if (section is null)
            {
                _errorMessage = "We couldn't find this section. Open the full rules page instead.";
            }
            else
            {
                _content = new MarkupString(section);
                _lastUpdated = result.Value.LastUpdated is { } utc
                    ? await ClientTimeZoneService.ToLocalAsync(utc)
                    : null;
                _loaded = true;
            }
        }

        _isLoading = false;
    }

    private async Task<string?> ExtractSectionAsync(string html, AnchorAlias alias)
    {
        try
        {
            _module ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./Documents/NebaDocument.razor.js");
            return await _module.InvokeAsync<string?>("extractSectionHtml", html, alias, TournamentRulesAnchors.RulesPath);
        }
        catch (JSDisconnectedException ex)
        {
            Logger.LogInformation(ex, "[TournamentRulesLink] Section extraction skipped because the circuit disconnected.");
            return null;
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "[TournamentRulesLink] Failed to extract the {TournamentType} rule section.", TournamentType);
            return null;
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_module is null) return;

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException ex)
        {
            Logger.LogInformation(ex, "[TournamentRulesLink] JS module disposal skipped because the circuit disconnected.");
        }
    }
}
```

**New: `Tournaments/Detail/TournamentRulesLink.razor.css`** (isolated; the button is this component's own markup)

```css
.td-rules-link {
    display: inline-flex;
    align-items: center;
    gap: 0.4rem;
    padding: 0.25rem 0;
    background: none;
    border: 0;
    color: white;
    font: inherit;
    font-size: 0.9375rem;
    font-weight: 500;
    text-decoration: underline;
    text-underline-offset: 3px;
    text-decoration-color: rgba(255, 255, 255, 0.5);
    cursor: pointer;
}

.td-rules-link:hover {
    text-decoration-color: white;
}

.td-rules-link:focus-visible {
    outline: 3px solid var(--tournaments-gold-light);
    outline-offset: 3px;
}
```

**Edit: `wwwroot/neba-document.css`** — footer, status and updated-text classes for the slide-out (global file, per the note at the top of `NebaDocument.razor`).

```css
.neba-document-slideover-footer {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    flex-wrap: wrap;
    padding: 1rem 1.5rem;
    border-top: 1px solid var(--neba-gray-200);
    flex-shrink: 0;
}

.neba-document-slideover-status {
    padding: 2rem;
    text-align: center;
    color: var(--neba-gray-600);
}

.neba-document-slideover-updated {
    font-size: 0.875rem;
    color: var(--neba-gray-600);
}
```

**Edit: `Documents/NebaDocument.razor`** — new parameter, passed to `scrollToHash`.

```csharp
/// <summary>
/// Short anchors (the part after <c>#</c>) and the heading each one points to. Optional.
/// </summary>
[Parameter]
public IReadOnlyDictionary<string, AnchorAlias>? AnchorAliases { get; set; }
```

```csharp
// InitializeTocAsync: was InvokeVoidAsync("scrollToHash", ContentId, TocListId)
await _module.InvokeVoidAsync("scrollToHash", ContentId, TocListId, AnchorAliases);
```

**Edit: `Documents/NebaDocument.razor.js`**

```js
/**
 * Finds the heading a short anchor points to.
 * Tries the heading whose ID starts with the alias prefix (e.g. "rule-601-"). If that heading's
 * text lacks the keyword (the rules were renumbered), takes the first rule heading that has it.
 * Only headings whose ID looks like "rule-<digits>-" count, so lettered sub-rules never match.
 * Returns null, and warns, when nothing matches.
 * @param {Iterable<HTMLElement>} headings
 * @param {{headingIdPrefix: string, titleKeyword: string}} alias
 * @returns {HTMLElement|null}
 */
function resolveAliasHeading(headings, alias) {
    const ruleHeadings = [...headings].filter(h => /^rule-\d+-/.test(h.id));
    const keyword = alias.titleKeyword.toLowerCase();
    const hasKeyword = h => h.textContent.toLowerCase().includes(keyword);

    const byNumber = ruleHeadings.find(h => h.id.startsWith(alias.headingIdPrefix));
    if (byNumber && hasKeyword(byNumber)) {
        return byNumber;
    }

    const byKeyword = ruleHeadings.find(hasKeyword);
    if (byKeyword) {
        return byKeyword;
    }

    console.warn('[NebaDocument] No heading found for anchor alias', alias);
    return null;
}
```

```js
// scrollToHash(contentId, tocListId, anchorAliases = null)
const aliasHeading = anchorAliases && Object.hasOwn(anchorAliases, targetId)
    ? resolveAliasHeading(headings, anchorAliases[targetId])
    : null;

const resolvedId = aliasHeading?.id ?? anchorLookup.get(targetId) ?? targetId;
const targetElement = document.getElementById(resolvedId);
// ...scroll code unchanged...

// Keep a short alias in the URL; only rewrite to the heading ID for non-alias hashes.
const newHash = aliasHeading ? hash : `#${resolvedId}`;
```

```js
/**
 * Returns the HTML of one rule section from a full document, for the detail-page slide-out.
 * The section runs from the matched heading up to the next heading of the same or higher level.
 * Same-document links (#...) are rewritten to point at the rules page. Returns null if no heading matches.
 * @param {string} html
 * @param {{headingIdPrefix: string, titleKeyword: string}} alias
 * @param {string} rulesPath
 * @returns {string|null}
 */
export function extractSectionHtml(html, alias, rulesPath) {
    const doc = new DOMParser().parseFromString(html, 'text/html');
    const start = resolveAliasHeading(doc.body.querySelectorAll('h1, h2, h3, h4, h5, h6'), alias);
    if (!start) return null;

    const level = Number(start.tagName[1]);
    const section = doc.createElement('div');

    // Google Docs list numbering lives in <style> blocks, so carry them over.
    doc.querySelectorAll('style').forEach(style => section.appendChild(style.cloneNode(true)));

    for (let node = start; node; node = node.nextElementSibling) {
        const isHeading = /^H[1-6]$/.test(node.tagName);
        if (node !== start && isHeading && Number(node.tagName[1]) <= level) break;
        section.appendChild(node.cloneNode(true));
    }

    section.querySelectorAll('a[href^="#"]').forEach(a => {
        a.setAttribute('href', rulesPath + a.getAttribute('href'));
    });

    return section.innerHTML;
}
```

Assumption to check while implementing: the rule headings and their content are siblings in the processed HTML (not nested in wrappers). The Playwright test and one JS test use real processed HTML to confirm.

**Edit: `Documents/TournamentRules.razor`**

```razor
<NebaDocument Content="@_content"
              ...
              AnchorAliases="@TournamentRulesAnchors.AnchorAliases"
              OnSlideoverLinkClicked="@HandleSlideoverLinkClicked"
              ... />
```

**Edit: `Tournaments/Detail/TournamentDetailViewModel.cs`**

```csharp
/// <summary>
/// True for a scheduled tournament whose type has its own rule section. Completed, truncated and
/// cancelled tournaments get no link, because the rules may have changed since they ran.
/// </summary>
public bool ShowRulesLink =>
    IsScheduled && TournamentRulesAnchors.TryGet(TournamentType, out _);
```

**Edit: `Tournaments/Detail/TournamentDetail.razor`** (the chips row in the hero)

```razor
<div class="td-hero__chips">
    <TournamentTypeBadge TournamentType="@_model.TournamentType" />
    @if (_model.PatternLengthCategory is not null)
    {
        <span class="td-hero__chip">@_model.PatternLengthCategory</span>
    }
    @if (_model.ShowRulesLink)
    {
        <TournamentRulesLink TournamentType="@_model.TournamentType" />
    }
</div>
```

**Edit: `Directory.Build.props`**: `<Version>1.1.4</Version>`.

**Edit (one line): `docs/learnings/blazor-ui.md`**: a new special tournament type needs an entry in `TournamentRulesAnchors`.

### API Client

No change.

### Tests

All tests carry `[UnitTest]`, `[Component("...")]`, `DisplayName`, explicit AAA comments, strict mocks and Shouldly, per CLAUDE.md.

`tests/Neba.Website.Tests/Documents/TournamentRulesAnchorsTests.cs`

```csharp
[UnitTest]
[Component("Website.Documents.TournamentRulesAnchors")]
public sealed class TournamentRulesAnchorsTests
{
    [Theory(DisplayName = "TryGet maps each type with its own rule section to its slug and rule number")]
    [InlineData("Singles", "singles", "rule-400-")]
    [InlineData("Doubles", "doubles", "rule-401-")]
    [InlineData("Trios", "trios", "rule-402-")]
    [InlineData("Baker", "baker", "rule-403-")]
    [InlineData("Tournament of Champions", "tournament-of-champions", "rule-600-")]
    [InlineData("Invitational", "invitational", "rule-601-")]
    [InlineData("Masters", "masters", "rule-602-")]
    [InlineData("Non-Champions", "non-champions", "rule-603-")]
    public void TryGet_ShouldReturnAnchor_WhenTypeHasRuleSection(string type, string slug, string prefix)
    {
        // Arrange / Act
        var found = TournamentRulesAnchors.TryGet(type, out var anchor);

        // Assert
        found.ShouldBeTrue();
        anchor!.Slug.ShouldBe(slug);
        anchor.Href.ShouldBe("/tournaments/rules#" + slug);
        anchor.Alias.HeadingIdPrefix.ShouldBe(prefix);
    }

    [Theory(DisplayName = "TryGet returns false for types without their own rule section")]
    [InlineData("Senior")]
    [InlineData("Women")]
    [InlineData("Youth")]
    [InlineData("Senior / Women")]
    [InlineData("Over/Under 50 Doubles")]
    [InlineData("High Roller")]
    public void TryGet_ShouldReturnFalse_WhenTypeHasNoRuleSection(string type) { /* ... */ }

    [Fact(DisplayName = "AnchorAliases has one entry per slug, and every prefix ends in a hyphen")]
    public void AnchorAliases_ShouldHaveUniqueSlugsAndHyphenatedPrefixes() { /* ... */ }
}
```

`TournamentDetailViewModelTests.cs`: a theory over status (`Scheduled` true; `Completed`, `Truncated`, `Cancelled` false) for Invitational, and one test that Scheduled Senior is false. Use `TournamentDetailViewModelFactory` (check for an existing factory in `Neba.TestFactory`; extend it if `Status` and `TournamentType` are not parameters).

`TournamentRulesLinkTests.cs` (bUnit)

```csharp
[Fact(DisplayName = "Opening the panel fetches the rules once and shows the extracted section")]
public async Task Open_ShouldShowSection_WhenExtractionSucceeds()
{
    // Arrange
    _mockDocumentsApi
        .Setup(x => x.GetDocumentAsync("tournament-rules", It.IsAny<CancellationToken>()))
        .ReturnsAsync(DocumentFactory.Create(html: "<h3 id=\"rule-601-invitational-tournament-rules\">Rule 601</h3>"));
    var module = _ctx.JSInterop.SetupModule("./Documents/NebaDocument.razor.js");
    module.Setup<string?>("extractSectionHtml", _ => true).SetResult("<h3>Rule 601</h3><p>Eligibility</p>");
    _ctx.JSInterop.SetupModule("./Components/NebaModal.razor.js").Mode = JSRuntimeMode.Loose;

    var cut = _ctx.Render<TournamentRulesLink>(p => p.Add(x => x.TournamentType, "Invitational"));

    // Act
    await cut.Find("button.td-rules-link").ClickAsync(new MouseEventArgs());
    await cut.Find("button.td-rules-link").ClickAsync(new MouseEventArgs());

    // Assert
    cut.Find(".neba-document-slideover-content").TextContent.ShouldContain("Eligibility");
    _mockDocumentsApi.Verify(x => x.GetDocumentAsync("tournament-rules", It.IsAny<CancellationToken>()), Times.Once);
}
```

More cases in the same file: renders nothing for an unmapped type; API error shows the "couldn't load" message and the full-page link; `extractSectionHtml` returning `null` shows the "couldn't find this section" message; the footer link `href` is `/tournaments/rules#invitational`; `JSException` from the module is logged (`FakeLogger<T>`) and shows the not-found message.

`DocumentSectionSlideoverTests.cs` (bUnit): closed renders nothing; open renders title, content, footer link; the close button, overlay click and Escape each call `OnClose`; loading and error states render their text; last-updated text shows only when set.

`TournamentDetailTests.cs`: link renders for a Scheduled Invitational and is absent for a Completed one.

`TournamentRulesTests.cs` / `NebaDocumentTests.cs`: `AnchorAliases` reaches the `scrollToHash` call (verify the JS invocation arguments).

`NebaDocument.razor.tests.js`

- `scrollToHash` with alias `invitational` scrolls to the `rule-601-` heading and leaves `#invitational` in the URL.
- A non-alias hash still rewrites to the heading ID.
- `rule-60-` does not match `rule-601-`.
- Renumbered doc (Invitational now `rule-701-`): keyword fallback finds it.
- Lettered sub-rule `rule-703e-invitational` is never chosen.
- No match: warns, does not throw, does not scroll.
- `extractSectionHtml`: returns the heading plus its content up to the next heading of the same or higher level; includes sub-headings; copies `<style>` blocks; rewrites `#...` links to `/tournaments/rules#...`; returns null when nothing matches.

Playwright (`tests/e2e`)

- `Documents.spec.ts`: `/tournaments/rules#invitational` scrolls the Invitational heading into view.
- `TournamentDetail.spec.ts`: on an upcoming Invitational, the "Invitational rules" button opens the panel; the panel shows "Rule 601" text; Escape closes it and returns focus to the button; "Open full rules page" goes to `/tournaments/rules#invitational`. On a completed tournament there is no button. Check `mock-api/mock-api-server.ts` for a `tournament-rules` document containing `Rule 6xx` headings and an upcoming Invitational, and add either if missing.

### Mockups

Detail page for an upcoming Invitational. Each file has a bar at the top to switch between a Scheduled Invitational, a Completed Invitational (no link) and a Scheduled Senior tournament (no rule, no link). Shared styles are in `shared.css` and reuse the site's own tokens.

- `docs/plans/mockups/tournament-rules-anchors/option-1.html`: plain link under the type chips (not chosen).
- `docs/plans/mockups/tournament-rules-anchors/option-2.html`: **chosen.** Button that opens a slide-out with the rule section; footer has "Open full rules page".
- `docs/plans/mockups/tournament-rules-anchors/option-3.html`: "Rules for this event" card in the page body (not chosen).
