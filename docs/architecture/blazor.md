# NEBA Blazor Web App Architecture

> **For Coding Agents**: This document defines architectural patterns and decisions for the NEBA Blazor application. Follow these guidelines when generating or modifying code.
> **Living Document**: This document evolves as decisions are made. When architectural decisions are finalized or patterns are established during development, update this document to reflect current state. Keep it accurate — it's the source of truth for agents.

---

## Agent Collaboration Protocol

**Critical**: This project requires a collaborative, question-driven workflow. The developer has strong software design vocabulary and can articulate requirements clearly. Leverage this.

### Before Writing Code

1. **Ask if an existing implementation exists** before creating any component, service, or pattern mentioned in this document
2. **Ask if existing CSS styles exist** before creating new component classes
3. **Reference MudBlazor components** when designing new components — mention the relevant MudBlazor component(s) so the developer can explore their API, behavior, and UX patterns as inspiration before building a custom version
4. **Analyze Server vs Client project placement** for every component and page — discuss which project it belongs in and why
5. **Propose approaches and get confirmation** before implementing — don't assume
6. **Explain trade-offs** when multiple valid approaches exist
7. **Question your own recommendations** — if you're suggesting something, explain why it's the right choice for this context

### When Reviewing or Modifying Code

1. **Be critical of existing code** — identify violations of these architectural patterns
2. **Be critical of your own generated code** — review it against these guidelines before presenting
3. **Explain changes** — when modifying existing code, explain what's changing and why
4. **Ask clarifying questions** when requirements are ambiguous rather than making assumptions

### Communication Style

- State facts directly without unnecessary explanation
- The developer is proficient in .NET — don't over-explain fundamentals
- When asked, provide recommendations with reasoning
- Use precise terminology

---

## Technology Stack

| Layer | Technology |
| ----- | ---------- |
| Framework | .NET 10, Blazor Web App |
| Render Mode | Interactive Auto (start Server, move to WASM only when necessary) |
| Styling | Tailwind CSS, custom components |
| API Communication | Refit (interfaces defined in `Neba.Api.Contracts`) |
| Result Handling | ErrorOr |
| Authentication | JWT, ASP.NET Core Identity |
| Backend API | Fast Endpoints (separate `Neba.Api` project) |

---

## Project Structure

```
Neba.Website.Server/
├── Tournaments/                 # Feature folder
│   ├── TournamentList.razor     # Page: /tournaments
│   ├── TournamentDetail.razor   # Page: /tournaments/{id}
│   ├── NewTournament.razor      # Page: /admin/tournaments/new
│   ├── EditTournament.razor     # Page: /admin/tournaments/{id}/edit
│   └── TournamentCard.razor     # Feature-specific component
├── Bowlers/
├── Scores/
├── News/
├── Organization/
├── Components/                  # Generic, reusable components (no domain knowledge)
├── Notifications/               # Alert and toast system
├── Layout/                      # MainLayout, AdminLayout, nav, footer
├── Services/                    # Service classes wrapping Refit interfaces
└── wwwroot/

Neba.Website.Client/             # Components/pages that must run in browser
├── Components/
├── Services/
└── Program.cs
```

> **Note**: The Client project starts nearly empty. Components only move here when there's a concrete reason (offline support, browser-only APIs, latency-sensitive interaction). Evaluate placement for every component.

### Folder Organization

- **Feature folders** (`Tournaments/`, `Bowlers/`, etc.) live at the project root
- **Feature-specific components** live alongside their pages in the feature folder — no `_Components` subfolder
- **Generic components** (`Components/`) have no domain knowledge and are reusable across features
- **No `Features/` wrapper** — the feature folders themselves communicate intent

### Naming Conventions

- Pages: `NewBowler.razor` with route `/bowler/new`
- Feature-specific components: live alongside pages in feature folder (e.g., `TournamentCard.razor` in `Tournaments/`)
- Routes: `/tournaments`, `/tournaments/{id}`, `/admin/tournaments/new`

---

## Component Architecture

### Server vs Client Project Placement

Every component and page must be evaluated for placement. Default is Server — only move to Client when there's a clear reason.

**Place in Server project when:**

- Component needs direct database/file system access
- Component uses server-only dependencies
- Component handles sensitive operations (auth, secrets)
- No specific reason to run on client
- Initial page load performance matters (SSR)

**Place in Client project when:**

- Component must work offline
- Component needs to run entirely in browser (heavy client-side computation)
- Component requires browser APIs that don't round-trip well
- Latency-sensitive interactions where server round-trip is unacceptable
- Component is used in both Server and Client contexts

**Discussion point**: When designing a component or page, explicitly discuss which project it belongs in. Don't assume Server by default without articulating why.

### Component Responsibility Layers

| Layer | Responsibility | Fetches Data? | Contains Business Logic? |
| ----- | ------------- | ------------- | ----------------------- |
| **Page** | Route handling, parameters, data orchestration, layout composition | Yes | No |
| **Feature Component** | Domain-specific display/interaction, receives data via parameters | No | No |
| **Generic Component** | Reusable UI pattern, no domain knowledge | No | No |
| **Service** | API communication, error handling, logging | Yes | Yes |

### Pages

Pages are thin orchestrators. They:

- Define the route
- Accept route parameters
- Call services to load data
- Handle the result (success, not found, error)
- Compose child components
- Make UI decisions (what to show on error, where to navigate)

Pages do NOT:

- Contain business logic
- Perform data transformations beyond simple mapping
- Have complex conditional rendering logic (extract to components)

### Feature Components

Feature components are domain-aware but data-agnostic. They:

- Receive all data via parameters
- Render domain-specific UI (tournament brackets, bowler stats)
- Emit events via `EventCallback` for user interactions

Feature components do NOT:

- Fetch data
- Inject services (other than UI services like notifications)
- Know about routing

### Generic Components

Generic components are fully reusable. They:

- Have no knowledge of tournaments, bowlers, or any domain concept
- Accept data via generic parameters or render fragments
- Handle pure UI concerns (tables, cards, pagination, modals)

---

## Service Layer

### Purpose

Services wrap Refit interfaces to:

1. **Handle errors** — try/catch around API calls
2. **Return typed results** — `ErrorOr<T>` instead of throwing
3. **Log failures** — centralized logging of API errors
4. **Abstract HTTP concerns** — components don't know about HTTP

### Pattern

```
Page → Service (ErrorOr<T>) → Refit Interface → API
```

Only pages call services. Components receive data via parameters.

Services do NOT:

- Map to ViewModels (use API contract types directly for read-only operations)
- Contain UI logic
- Access `NavigationManager` or notification services

### Error Handling Flow

| Concern | Owner |
| ------- | ----- |
| HTTP errors, network failures | Service (catch, log, return ErrorOr) |
| What notification to show | Component (UI decision) |
| Where to navigate on error | Component (UI decision) |
| Whether error is recoverable | Component (UI decision) |

This separation allows the same service error to result in different UI responses depending on context.

---

## Notifications

### Architecture

Two complementary notification mechanisms:

| Type | Purpose | Persistence | Example |
| ---- | ------- | ----------- | ------- |
| **Toast** | Ephemeral feedback for completed actions | Auto-dismisses | "Saved successfully" |
| **Alert** | Persistent, actionable issues | Until dismissed or navigated away | Validation errors, warnings |

### Behavior Requirements

**Toasts**:

- Stack vertically (multiple toasts visible simultaneously)
- Position: top-right on desktop, top-center on mobile
- Auto-dismiss after timeout
- User can dismiss early
- New toasts don't replace existing ones

**Alerts**:

- Stack vertically (multiple alerts visible simultaneously)
- Position: top of content area
- Persist until explicitly dismissed or navigation occurs
- Support validation message lists (bulleted)
- Clear non-persistent alerts on navigation

**Why stacking**: Rapid operations or partial failures can produce multiple notifications. Queuing loses information. Stacking ensures all feedback is visible.

**Why consistent positioning**: Predictable location reduces cognitive load. Users learn where to look.

### Severity Levels

| Severity | Color | Use Case |
| -------- | ----- | -------- |
| Normal | Gray | Neutral information |
| Info | Blue | Informational, no action needed |
| Success | Green | Completed successfully |
| Warning | Orange | Attention needed, not blocking |
| Error | Red | Failed, action required |

---

## Loading States

### Requirements

Loading indicators should:

- **Delay before showing** — prevent flash for fast operations (e.g., 100ms delay)
- **Minimum display time** — once shown, display for minimum duration to prevent jarring flash (e.g., 500ms)
- **Support scopes** — full screen, page content area, or section
- **Block interaction** — overlay prevents clicking underlying content during load

**Why delay**: Network calls often complete in under 100ms. Showing and immediately hiding a spinner is worse than showing nothing.

**Why minimum display**: If the spinner appears after the delay, showing it for only 50ms before hiding creates a disorienting flash.

---

## Forms (Future Reference)

> **Note**: Forms are deferred for now as the initial build is read-only. This section establishes patterns for when forms are implemented.

### Pattern: Separate Pages, Shared Form Component

- `NewTournament.razor` — page for creating
- `EditTournament.razor` — page for editing  
- `TournamentForm.razor` — shared form component

**Why separate pages**:

- Clean URLs (`/new` vs `/{id}/edit`)
- Single responsibility per page
- No mode-switching conditional logic
- Edit page can have additional concerns (audit history, delete)

### Form Component Responsibilities

The form component:

- Renders inputs with validation
- Accepts a model via parameter
- Emits `OnValidSubmit` and `OnCancel` events
- Displays `IsSubmitting` state
- Has no knowledge of create vs edit

The page:

- Loads data (for edit)
- Provides the model
- Handles submit (calls service)
- Handles navigation on success/cancel
- Shows notifications based on result

---

## Authentication & Authorization

### Approach

- JWT tokens stored in HttpOnly cookie
- `AuthenticationStateProvider` surfaces claims to Blazor
- Tokens attached to outgoing API calls via HttpClient configuration

### Route Protection

Apply `[Authorize]` attribute directly on each admin page:

```razor
@attribute [Authorize(Roles = "Admin")]
```

**Why per-page instead of folder-level**: Explicit is clearer. Each page declares its own requirements. No hidden inheritance to trace.

### Admin Routes

All admin routes prefixed with `/admin/`:

- `/admin/tournaments`
- `/admin/tournaments/new`
- `/admin/bowlers/{id}/edit`

---

## API Communication

### Refit Interfaces

Defined in `Neba.Api.Contracts` (shared project). Services in Blazor project wrap these interfaces.

### Contract Types

Use API contract types (`TournamentDetailResponse`, `BowlerListResponse`) directly in components. Do not create separate ViewModel classes for read-only display.

**When to introduce ViewModels**:

- API response needs transformation (flattening, combining sources)
- Computed properties needed client-side
- Third-party API with awkward contract shapes

---

## Styling

### Tailwind CSS

- Utility-first approach
- Styles colocated with markup in components
- Design tokens defined in `tailwind.config.js` for consistency
- No component library — custom components for full control

### Responsive Design Approach

| Area | Primary Target | Reasoning |
| ---- | -------------- | --------- |
| **Public pages** | Mobile-first | General audience, casual browsing, checking scores on phones |
| **Admin pages** | Desktop/tablet-first | Data entry, management tasks, typically done at a desk |

**Public pages**: Start with mobile layout, enhance for larger screens. Assume touch, constrained width, and vertical scrolling as the baseline.

**Admin pages**: Start with desktop layout, adapt for tablet. Assume keyboard/mouse input, wider viewports, and multi-column layouts as the baseline. Mobile support is secondary — functional but not optimized.

### Design Direction

- **Admin area**: Professional, utilitarian, enterprise feel
- **Public area**: Can be more visually engaging while maintaining professionalism
- Both areas share design tokens (colors, typography, spacing) for cohesion

---

## JavaScript

### Philosophy

Minimize JavaScript, but be pragmatic. Use JS when:

- Blazor literally cannot do it (clipboard API, focus management, certain browser APIs)
- JS implementation is significantly cleaner/clearer than Blazor interop gymnastics
- Integrating third-party libraries that don't have Blazor wrappers (maps, specialized charts)

Do not use JS for:

- Things Blazor handles well (event handling, DOM manipulation, component state)
- Avoiding learning Blazor patterns

### Structure

```
Neba.Website.Server/
└── wwwroot/
    └── js/
        ├── maps.js              # Azure Maps integration
        ├── clipboard.js         # Clipboard utilities
        └── interop.js           # General browser API interop
```

Keep JS modules focused and small. Each file should have a single purpose.

### Interop Pattern

Wrap `IJSRuntime` in a typed service for testability and discoverability:

```csharp
public interface IClipboardService
{
    Task CopyToClipboardAsync(string text);
}

public class ClipboardService(IJSRuntime js) : IClipboardService
{
    public async Task CopyToClipboardAsync(string text)
    {
        await js.InvokeVoidAsync("clipboard.copy", text);
    }
}
```

**Why wrap**:

- Components don't call `IJSRuntime` directly
- Service can be mocked in bUnit tests
- Centralizes JS function names (no magic strings scattered in components)

### Testing JavaScript

JS modules are tested with Jest. Tests live in `tests/js/`.

Test in isolation — mock browser APIs, verify function behavior. E2E tests cover the integration with Blazor.

---

## Stylesheets

### Primary Approach: Tailwind Utilities + Component Classes

Use a combination of:

1. **`@apply` component classes** for repeating visual patterns (buttons, inputs, cards)
2. **Blazor components** for interactive elements with behavior
3. **Tailwind utilities** for one-off layouts and spacing

### Component Classes with @apply

Define reusable visual patterns in CSS using `@apply`:

```css
/* app.css */
@layer components {
  .btn-primary {
    @apply px-4 py-2 rounded-md font-medium bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50;
  }
  
  .card {
    @apply bg-white rounded-lg shadow-sm border border-gray-200;
  }
  
  .input {
    @apply block w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:ring-1 focus:ring-blue-500;
  }
}
```

**Why `@apply` for primitives**:

- Single source of truth for visual patterns
- Design changes propagate everywhere
- Cleaner markup — no repeating long utility strings
- Works in any context (Blazor, raw HTML, error pages)

### Blazor Components Use the CSS Classes

Blazor components encapsulate behavior and use the CSS classes internally:

```razor
<!-- Button.razor uses .btn-primary from CSS -->
<button class="btn-primary" disabled="@IsLoading" @onclick="HandleClick">
    @if (IsLoading)
    {
        <LoadingSpinner Size="Small" />
    }
    @ChildContent
</button>
```

**The CSS class defines how it looks. The Blazor component defines how it behaves.**

### When to Use Each

| Scenario | Approach |
| -------- | -------- |
| Repeating visual pattern (buttons, inputs, cards, labels) | `@apply` component class |
| Interactive element with logic (loading states, events) | Blazor component using the CSS class |
| One-off layout, spacing, positioning | Tailwind utilities in markup |
| Complex animations, pseudo-elements | CSS isolation (`.razor.css`) |

### Existing Styles

**Before creating new CSS classes or modifying styles, ask if existing styles exist for that pattern.** The project has established CSS classes — review and adjust as appropriate rather than creating duplicates.

### When to Use CSS Isolation (.razor.css)

Use Blazor's CSS isolation only when:

- Complex animations that would be unwieldy as utilities
- Pseudo-element styling (::before, ::after) that Tailwind doesn't cover
- Third-party component styling overrides

### What to Avoid

- **Repeating long utility strings** — extract to `@apply` component class
- **Component-level `<link>` tags** — Tailwind's purging keeps the bundle small
- **Inline `<style>` blocks** — use `.razor.css` if component-specific CSS is needed
- **Fighting Tailwind** — if writing lots of custom CSS, question whether you're using Tailwind effectively

### Global Styles

```
wwwroot/
└── css/
    └── app.css                  # Tailwind directives, @apply component classes, global overrides
```

Keep global overrides minimal. Prefer Tailwind's configuration (`tailwind.config.js`) for design tokens.

---

## Observability

### Philosophy

Instrument for diagnosability, not dashboards. When something goes wrong, traces and logs should answer: what happened, where, and why.

### Logging

Use `ILogger<T>` for all logging. Inject the logger typed to the class:

```csharp
public class TournamentService(ITournamentApi api, ILogger<TournamentService> logger)
{
    public async Task<ErrorOr<TournamentDetailResponse>> GetAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var response = await api.GetAsync(id, ct);
            return response;
        }
        catch (ApiException ex)
        {
            logger.LogError(ex, "API error fetching tournament {TournamentId}: {StatusCode}", id, ex.StatusCode);
            return Error.Failure("api.error", ex.Message);
        }
    }
}
```

**Logging guidelines:**

- Use structured logging with named placeholders (`{TournamentId}`, not `{0}`)
- Log at appropriate levels:
  - `Error`: Failures that need attention (API errors, unhandled exceptions)
  - `Warning`: Recoverable issues, degraded behavior
  - `Information`: Significant business events (tournament created, score submitted)
  - `Debug`: Diagnostic detail for troubleshooting
- Include correlation context (IDs, user identifiers where appropriate)
- Don't log sensitive data (passwords, tokens, PII)

### Distributed Tracing

Use OpenTelemetry for traces and spans. The goal: trace a request from UI → API → database and back.

**What to instrument:**

| Layer | Automatic | Manual |
| ----- | --------- | ------ |
| HTTP calls (Refit/HttpClient) | ✓ (via OpenTelemetry.Instrumentation.Http) | — |
| Blazor page loads | — | Add spans for significant page operations |
| Service methods | — | Add spans for business operations |
| JS interop calls | — | Consider spans for expensive JS operations |

**When to add manual spans:**

- Operations that involve multiple steps or could be slow
- Business-critical operations (score submission, registration)
- Operations where you need to understand timing breakdown

```csharp
public class TournamentService(ITournamentApi api, ILogger<TournamentService> logger, ActivitySource activitySource)
{
    public async Task<ErrorOr<TournamentDetailResponse>> GetAsync(int id, CancellationToken ct = default)
    {
        using var activity = activitySource.StartActivity("GetTournament");
        activity?.SetTag("tournament.id", id);
        
        // ... implementation
    }
}
```

### Metrics

Capture counters and histograms for key operations:

- **Counters**: Page views, API calls by endpoint, errors by type
- **Histograms**: Response times, operation durations

**Discussion point**: Define metrics as features are built. Don't pre-define metrics speculatively.

### Configuration

Observability is configured at startup. Traces, metrics, and logs should export to your chosen backend (Application Insights for Azure hosting).

**Before adding instrumentation, discuss:**

1. What question are we trying to answer with this data?
2. Is this the right layer to capture it?
3. Will this create excessive noise?

---

## Deferred Decisions

The following are intentionally deferred until the relevant features are built:

| Topic | Status |
| ----- | ------ |
| State management (Fluxor, etc.) | Defer until cross-component state needed |
| Real-time (SignalR, SSE) | Defer until live scores feature |
| Caching | Defer until performance requires it |
| ViewModel mapping | Not needed for read-only; revisit for forms |

---

## Testing

### Test Project Structure

```
tests/
├── Neba.TestFactory/            # Shared test infrastructure (factories, fixtures, traits)
├── Neba.Website.Tests/          # Services, bUnit component tests (unit + integration)
├── e2e/                         # Playwright (TypeScript)
│   ├── fixtures/
│   │   ├── mocks/               # API response fixtures
│   │   └── handlers.ts          # Reusable route handlers
│   ├── tests/
│   │   ├── tournaments.spec.ts
│   │   ├── bowlers.spec.ts
│   │   └── auth.spec.ts
│   └── playwright.config.ts
└── js/                          # Jest tests for JS modules
    ├── maps.test.ts
    └── jest.config.ts
```

`Neba.Website.Tests` references `Neba.TestFactory` for shared factories, fixtures, and trait attributes.

### Test Traits

Use the same trait attributes defined in `Neba.TestFactory` (see [backend.md](backend.md#test-traits)):

```csharp
[UnitTest]
[Component("Tournaments")]
public class TournamentServiceTests
{
    [Fact]
    public async Task GetAsync_Returns_Error_When_Api_Fails() { }
}

[UnitTest]
[Component("Notifications")]
public class ToastComponentTests : TestContext
{
    [Fact]
    public void Should_Auto_Dismiss_After_Timeout() { }
}
```

### What to Test Where

| Layer | Test Type | Trait | Approach |
| ----- | --------- | ----- | -------- |
| Services | Unit | `[UnitTest]` | Mock Refit interface, verify ErrorOr mapping, error handling |
| JS interop services | Unit | `[UnitTest]` | Mock IJSRuntime, verify correct calls |
| JS modules | Jest | — | Test in isolation, mock browser APIs |
| Complex components | bUnit | `[UnitTest]` | Interactive behavior, conditional rendering, event callbacks |
| Simple display components | Skip | — | Not worth the ceremony |
| Pages | Skip | — | Thin orchestrators, tested implicitly via E2E |
| Critical user flows | Playwright E2E | — | Happy paths, common error scenarios |

### Test Naming & Display Names

All tests must have explicit display names. See [backend.md](backend.md#test-naming--display-names) for full examples.

```csharp
[Fact(DisplayName = "Should show error toast when API returns failure")]
public async Task Should_Show_Error_Toast_When_Api_Fails() { }

[Theory(DisplayName = "Should validate form field")]
[InlineData("", false, DisplayName = "Empty name is invalid")]
[InlineData("Valid Name", true, DisplayName = "Non-empty name is valid")]
public void Should_Validate_Name(string name, bool expected) { }
```

### Unit Tests (Neba.Website.Tests)

**Services**: Mock the Refit interface, verify:

- Successful responses map correctly
- API errors return appropriate `ErrorOr` failures
- Network errors are handled gracefully
- Logging occurs on failures

**bUnit components**: Test components with meaningful interaction logic. Skip simple display-only components.

### JavaScript Tests (Jest)

JS modules are tested in isolation with Jest. Tests live in `tests/js/`.

```typescript
// tests/js/clipboard.test.ts
import { copy } from '../../src/Neba.Website.Server/wwwroot/js/clipboard';

describe('clipboard', () => {
  it('calls navigator.clipboard.writeText', async () => {
    const mockWriteText = jest.fn().mockResolvedValue(undefined);
    Object.assign(navigator, {
      clipboard: { writeText: mockWriteText }
    });

    await copy('test text');

    expect(mockWriteText).toHaveBeenCalledWith('test text');
  });
});
```

**What to test**: Function behavior, edge cases, error handling.

**What not to test**: Blazor interop integration — that's covered by E2E.

### E2E Tests (Playwright)

**Scope**: Common user journeys, not exhaustive coverage.

- Critical happy paths (view tournament, search bowlers, login flow)
- Common error scenarios (invalid credentials → retry → success)
- Flows involving multiple pages/steps

**API Mocking Strategy**:

Use `page.route()` with centralized fixtures:

```typescript
// fixtures/mocks/tournaments.ts
export const tournamentList = [
  { id: 1, name: 'Summer Classic', date: '2025-07-15' },
  { id: 2, name: 'Fall Championship', date: '2025-10-20' }
];

// fixtures/handlers.ts
export async function mockTournamentApi(page: Page) {
  await page.route('**/api/tournaments', route => {
    route.fulfill({ json: tournamentList });
  });
}

// tests/tournaments.spec.ts
test('displays tournament list', async ({ page }) => {
  await mockTournamentApi(page);
  await page.goto('/tournaments');
  await expect(page.getByText('Summer Classic')).toBeVisible();
});
```

**Why mock at API boundary**:

- Tests run fast and reliably (no real API dependency)
- Blazor app runs fully — tests real rendering, routing, state
- Mock data is centralized and reusable
- Can test error scenarios by returning error responses

**Generating types**: The API exposes an OpenAPI spec at runtime. Generate TypeScript types from this spec to keep mock data typed and catch drift at build time.

### E2E Test Flakiness Prevention

| Cause | Prevention |
| ----- | ---------- |
| Timing issues | Use Playwright's auto-waiting; avoid manual `waitForTimeout` |
| Test data assumptions | Use unique data per test or reset state |
| Mock drift from API | Generate types from OpenAPI spec |
| Coupled to DOM structure | Test user-visible behavior, use accessible selectors |

### When to Add E2E Tests

**This should be a discussion point when adding functionality.**

Consider E2E coverage when the feature involves:

- Multi-step user flows (wizards, checkout-style processes)
- Authentication or authorization boundaries
- Critical business operations (score entry, registration)
- Complex form validation with error recovery
- Integration between multiple pages

Skip E2E for:

- Simple CRUD with no special flows
- Read-only display pages (unless critical)
- Features already well-covered by unit tests

---

## Checklist: Before Implementing

When implementing any component or feature, verify:

- [ ] Asked if existing implementation exists
- [ ] Asked if existing CSS styles exist for this pattern
- [ ] Referenced MudBlazor for component design inspiration
- [ ] Analyzed Server vs Client project placement
- [ ] Proposed approach and got confirmation
- [ ] Page is thin (orchestration only, no business logic)
- [ ] Components receive data via parameters, don't fetch
- [ ] Service handles errors and returns `ErrorOr<T>`
- [ ] Notifications follow toast vs alert guidelines
- [ ] Loading states have delay and minimum display time
- [ ] Route has appropriate `[Authorize]` attribute if admin
- [ ] No unnecessary ViewModel mapping for read-only data
- [ ] Discussed whether E2E tests are warranted
