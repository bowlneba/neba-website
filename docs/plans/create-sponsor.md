# Create Sponsor

Add a "Create Sponsor" feature, structurally mirroring `CreateArticle` (News). GET/List sponsors are already wired up (`GetSponsorDetail`, `ListActiveSponsors`); this plan adds the write side.

## Decisions locked in during scoping

- **UI entry point**: new admin-gated sponsor list page (`/sponsors/manage` — see Phase 2) with a `FabCreateButton` → `/sponsors/new`, mirroring `NewsList.razor`. The existing public `/sponsors` page (tiered marketing display) is untouched.
- **Admin list data source**: no new API route. The existing `GET /sponsors` (`ListActiveSponsorsEndpoint`) is amended to check the caller's permissions and branch its filter, exactly like `ListArticlesEndpoint`/`ListArticlesQuery` does with `CallerHasArticleManagementPermission` — anonymous/unpermitted callers still get active-only sponsors (today's public-page behavior, unchanged), callers holding a Sponsors-management permission get every sponsor, active and inactive. This replaces the separate `ListSponsorsForAdmin` endpoint from the earlier draft of this plan.
- **Form/factory scope**: `Sponsor.Create(...)` takes the mandatory fields as required parameters and every other `Sponsor` property as a nullable parameter defaulting to `null`, matching the existing `SponsorFactory.Create(...)` test-factory signature (`tests/Neba.TestFactory/Sponsors/SponsorFactory.cs`) — that signature is the reference for the domain factory's shape. The create UI form captures the full field set (not deferred to a future Edit Sponsor feature).
- **Slug uniqueness**: enforced with the same check-then-insert + `Error.Conflict` (409) pattern `CreateArticleCommandHandler` uses for `Article.Slug`, since `Sponsor.Slug` already has a DB alternate key (`SponsorConfiguration.cs`) that would otherwise surface as an unhandled `DbUpdateException`.
- **Business address input (UI)**: manual entry now (`UsState` dropdown + free-text street/unit/city/postal code), no address-autocomplete integration. The app has zero existing Google Maps/Places dependency anywhere (`DirectionsModal.razor` only builds a deep-link URL, no SDK/API key) — adding Places Autocomplete would be the first such dependency (new API key, billing, CSP change, JS interop). Worth it long-term, though — members will eventually be able to update their own address (a second, higher-volume address-entry form), which is exactly the case that justifies the shared integration cost. Tracked as a GitHub issue rather than scoped into this feature: [`docs/plans/address-autocomplete-issue.md`](./address-autocomplete-issue.md).

## Open assumptions to confirm at this gate

1. **Address input is US-only** for now (`Address.Create(street, unit, city, UsState, zip, coordinates)` overload), consistent with `AddressFactory.BogusUs` being the only address bogus-generator sponsors currently use. No Canadian address input in the create form.
2. **Business email/phone/address/contact validation happens in the command handler**, not the FluentValidation request validator — `Address.Create`, `EmailAddress.Create`, and `PhoneNumber.CreateNorthAmerican` all return `ErrorOr<T>` and encode business rules (regex formats, area-code rules), which per CLAUDE.md's "Validators handle structural validation only" belong in the handler/domain, not the validator. This is a new pattern for this codebase (no existing command handler calls these three `Create` methods yet) — flagging in case there's a preferred existing approach I've missed.
3. **`SponsorContact` (contact person) is all-or-nothing**: if the request supplies a contact name, phone, or email, all three must be present or the request is rejected as a validation error; otherwise `SponsorContact` stays `null`. No partial contact info.
4. **No `Priority` range validation** beyond what the type system gives (`int`, defaults to `0` if omitted) — no domain rule for priority was specified for `Sponsor`, so none is added. Flag if this needs a `> 0` constraint.
5. **Reserved slug "new"** — like `Article`, the sponsor create route is `/sponsors/new`, so `Sponsor.Create` rejects a normalized slug of `"new"` the same way `Article.Create` does, to keep the route unambiguous.

---

## Phase 1: API

### Domain (`Neba.Api.Features.Sponsors.Domain`)

- **`Sponsor.cs`** — add a static factory:
  ```
  public static ErrorOr<Sponsor> Create(
      string name, bool isCurrentSponsor, int priority, SponsorTier tier, SponsorCategory category,
      string? slug = null, StoredFile? logo = null, Uri? websiteUrl = null, string? tagPhrase = null,
      string? description = null, string? liveReadText = null, string? promotionalNotes = null,
      Uri? facebookUrl = null, Uri? instagramUrl = null, Address? businessAddress = null,
      EmailAddress? businessEmail = null, IReadOnlyCollection<PhoneNumber>? phoneNumbers = null,
      ContactInfo? sponsorContact = null, SponsorId? id = null)
  ```
  Validates `Name` required, normalizes `Slug` from `Name` when blank (reusing the same slug-normalization approach as `Article.NormalizeSlug` — likely worth extracting to a shared helper at this point since two aggregates now need it), rejects empty-after-normalization and the reserved value `"new"`. All properties currently `{ get; init; }` stay that way — `Create` returns a fully-initialized `Sponsor` via object initializer, same shape as `Article.Create`.
- **`SponsorErrors.cs`** — add `NameRequired`, `SlugInvalid`, `SlugReserved` (validation errors, mirroring `ArticleErrors`), and `SlugAlreadyExists(string slug)` (conflict error, mirroring `ArticleErrors.SlugAlreadyExists`).
- **Shared slug normalizer** — extract `Article`'s private `NormalizeSlug` into a small shared internal helper (e.g. `Neba.Api.Domain.SlugNormalizer` or similar existing home) so `Sponsor.Create` doesn't duplicate the character-filtering logic. Flag at the code-draft gate if there's a preferred existing location.

### Application (`Neba.Api.Features.Sponsors.CreateSponsor/`, new folder)

- **`CreateSponsorCommand.cs`** — `internal sealed record CreateSponsorCommand : ICommand<CreatedSponsor>` carrying raw scalar/DTO fields (not yet-validated value objects): `Name`, `Slug?`, `IsCurrentSponsor`, `Priority`, `Tier` (string, converted via `SponsorTier.FromName` in the endpoint — mirrors `PublicationStatus.FromName`), `Category` (string, `SponsorCategory.FromName`), `Logo` (`StoredFile?`), `WebsiteUrl` (`Uri?`), `TagPhrase`, `Description`, `LiveReadText`, `PromotionalNotes`, `FacebookUrl`, `InstagramUrl` — plus raw business-address fields (`BusinessStreet`, `BusinessUnit`, `BusinessCity`, `BusinessState`, `BusinessPostalCode`), `BusinessEmailAddress` (string?), `PhoneNumbers` (collection of raw `{ Type, Number, Extension }`), and raw contact fields (`ContactName`, `ContactPhoneType/Number/Extension`, `ContactEmail`).
- **`CreatedSponsor.cs`** — `public sealed record CreatedSponsor { required SponsorId Id; required string Slug; }`, mirrors `CreatedArticle`.
- **`CreateSponsorCommandHandler.cs`** — mirrors `CreateArticleCommandHandler`'s shape:
  1. Build `Address?` via `Address.Create(...)` if `BusinessStreet` is present — short-circuit on error.
  2. Build `EmailAddress?` via `EmailAddress.Create(...)` if `BusinessEmailAddress` is present — short-circuit on error.
  3. Build `IReadOnlyCollection<PhoneNumber>` via `PhoneNumber.CreateNorthAmerican(...)` per entry — short-circuit on first error.
  4. Build `ContactInfo?` (all-or-nothing per assumption 3) via nested `PhoneNumber.CreateNorthAmerican` + `EmailAddress.Create` — short-circuit on error.
  5. Call `Sponsor.Create(...)` with the now-validated value objects — short-circuit on error.
  6. Check slug availability (`appDbContext.Sponsors.AnyAsync(s => s.Slug == sponsor.Slug)`) → `SponsorErrors.SlugAlreadyExists` if taken (same check-then-insert comment/caveat as `CreateArticleCommandHandler.EnsureSlugIsAvailableAsync`).
  7. `AddAsync` + `SaveChangesAsync`.
  8. `cache.RemoveByTagAsync("neba:sponsors", ...)` (existing tag shared by `ListActiveSponsors` + `GetSponsorDetail` — no new cache descriptor needed).
  9. Return `CreatedSponsor { Id, Slug }`.
  - No pending-uploads cleanup needed (unlike Article) unless the logo upload flow reuses the same pending-upload pattern as article header images — confirm during Phase 2 UI planning whether sponsor logo upload goes through `PendingUploads` the same way.

### Application (amendment) — `Features/Sponsors/ListActiveSponsors/`

Existing files, amended (not replaced) to support the admin list page's data needs, mirroring `ListArticlesQuery`/`ListArticlesEndpoint`'s permission-branching pattern exactly:

- **`ListActiveSponsorsQuery.cs`** — add `public required bool CallerHasSponsorManagementPermission { get; init; }`. `Cache` becomes `CacheDescriptors.Sponsors.ListActiveSponsors(CallerHasSponsorManagementPermission)` (was a static property; becomes a method taking the flag, same shape as `CacheDescriptors.News.ListArticles(page, pageSize, callerHasArticleManagementPermission)`).
- **`CacheDescriptors.cs`** — `Sponsors.ListActiveSponsors` becomes a method: `Key = $"neba:sponsors:list:scope:{(callerHasSponsorManagementPermission ? "management" : "public")}"`, same `Tags = ["neba", "neba:sponsors"]` (both scopes still invalidate together via `CreateSponsorCommandHandler`'s existing `cache.RemoveByTagAsync("neba:sponsors", ...)` — no handler change needed).
- **`ListActiveSponsorsQueryHandler.cs`** — wrap the existing `.Where(sponsor => sponsor.IsCurrentSponsor)` in `if (!query.CallerHasSponsorManagementPermission)`; management callers get the unfiltered set. No DTO shape change needed — `SponsorSummaryDto`/`SponsorSummaryResponse` already carry `IsCurrentSponsor` and `Priority`, so the admin list has everything it needs today.
- **`ListActiveSponsorsEndpoint.cs`** — stays `AllowAnonymous()` (matches `ListArticlesEndpoint` — the endpoint itself is public, the *data* varies by caller), sets `CallerHasSponsorManagementPermission = User.HasAnyPermission(PermissionsScope.SponsorManagementPermissions)` when building the query.
- **Naming flag**: `ListActiveSponsorsEndpoint`/`ListActiveSponsorsQuery` keeps its current name for this plan (minimal-diff choice), even though "active" is no longer strictly accurate once it can return inactive sponsors too. `ListArticlesEndpoint` avoided this by already having a generic name. Flag at the code-draft gate if a rename to `ListSponsors`/`ListSponsorsQuery`/`ListSponsorsEndpoint` (route stays `/sponsors`) is preferred instead — it's a larger diff (Refit method rename, OpenAPI operation ID, existing test file renames) so calling it out rather than assuming.

### Security (amendment)

- **`Permission.cs`** — add `SponsorManagementPermissions` alongside `CreateSponsor`, mirroring `ArticleManagementPermissions`: `public static readonly IReadOnlyCollection<Permissions> SponsorManagementPermissions = [CreateSponsor];` — a one-item collection today, ready to grow when Edit/Delete Sponsor permissions are added later without another endpoint change.

### Infrastructure

- No new EF configuration needed — `SponsorConfiguration.cs` already maps every field `Sponsor.Create` will populate. No migration needed.

### API (`Neba.Api.Features.Sponsors.CreateSponsor/`)

- **`CreateSponsorEndpoint.cs`** — `Post(string.Empty)`, `Group<SponsorsEndpointGroup>()`, `Policies(PermissionCatalog.CreateSponsor.PolicyName)`, tags `"Admin"`, produces 201/400/401/403/409/422 — mirrors `CreateArticleEndpoint` exactly, including the `Send.CreatedAtAsync("GetSponsorDetail", routeValues: new { slug }, ...)` call (endpoint name already exists on `GetSponsorDetailEndpoint`).
- **`CreateSponsorSummary.cs`** — mirrors `CreateArticleSummary`, documents the 201/401/403/409/422 responses.
- **`CreateSponsorRequestValidator.cs`** — structural-only rules: `Name` not empty + max length (match `SponsorConfiguration`'s `HasMaxLength(63)`), `Slug` max length 63 when supplied, `Tier`/`Category` must be one of the known `SmartEnum` names (mirrors `PublicationStatus` check in `CreateArticleRequestValidator`), `WebsiteUrl`/`FacebookUrl`/`InstagramUrl` must be valid absolute URIs when supplied, contact all-or-nothing structural shape check (assumption 3) if it can be expressed without business rules, otherwise deferred to the handler.

### Contracts (`Neba.Api.Contracts.Sponsors.CreateSponsor/`, new folder)

- **`CreateSponsorRequest.cs`** — wraps `SponsorInput`, mirrors `CreateArticleRequest`.
- **`SponsorInput.cs`** — mirrors `ArticleInput`'s shape but with the full Sponsor field set (scalars + nested `PhoneNumberInput[]` + nested `ContactInput?`).
- **`PhoneNumberInput.cs`**, **`ContactInput.cs`** — small nested input records.
- **`SponsorResponse.cs`** — `{ SponsorId (string), Slug }`, mirrors `ArticleResponse`.
- **`ISponsorsApi.cs`** — add `[Post("/sponsors")] Task<IApiResponse<SponsorResponse>> CreateSponsorAsync(CreateSponsorRequest request, CancellationToken cancellationToken = default);`

### Security

- **`Permission.cs`** — add a `#region Sponsors` block: `CreateSponsor = new("Sponsors.CreateSponsor", "Create Sponsor")`. No `AddPolicy(...)` registration needed — the dynamic `Permission:{value}` policy provider (`PermissionPolicyProvider`) handles it automatically, same as `CreateArticle`.
- **`docs/policies/README.md`** — no new entry needed (generic `Permission:{value}` row already documents this mechanism); note the specific permission name in the endpoint's help doc if one is generated later.

### Test Factories (`Neba.TestFactory.Sponsors/`, extending existing folder)

- `SponsorFactory.cs` already exists with the exact field set `Sponsor.Create` needs — no changes required unless `Create`'s validation rejects any of its current defaults (it shouldn't, since `ValidName`/`ValidSlug` are already sane).
- **New**: `CreatedSponsorFactory.cs` (mirrors an equivalent `CreatedArticle` factory pattern, if one exists — otherwise a small `Create(SponsorId? id, string? slug)` factory).
- **New**: `SponsorResponseFactory.cs` (mirrors `SponsorDetailResponseFactory.cs`/`SponsorSummaryResponseFactory.cs` already in the folder).
- **New**: `CreateSponsorRequestFactory.cs` / `SponsorInputFactory.cs` for endpoint-test request bodies.

### Tests (`Neba.Api.Tests`)

- `CreateSponsorEndpointTests.cs` — Verify-snapshot happy path, empty/edge cases, `Configure` route+auth test, 409/422 error-path tests — same structure as the `new-endpoint` skill's endpoint-test template and `DeleteArticleEndpointAuthorizationTests` conventions.
- `CreateSponsorCommandHandlerTests.cs` (unit) — slug-conflict path, each value-object validation failure path (bad email, bad phone, bad address), success path with/without optional fields populated.
- `SponsorTests.cs` (domain, extend existing if present) — `Create` validation: name required, slug normalization/reserved/invalid, and the happy path building a fully-populated `Sponsor`.
- `CreateSponsorRequestValidatorTests.cs` — structural validation rules only.
- `ListActiveSponsorsQueryHandlerTests.cs` (amend existing) — add a case for `CallerHasSponsorManagementPermission = true` returning inactive sponsors too, alongside the existing active-only case.
- `ListActiveSponsorsEndpointTests.cs` (amend existing) — add a case asserting `CallerHasSponsorManagementPermission` is populated from `User.HasAnyPermission(...)` correctly for both an authenticated management-permission caller and an anonymous/unpermitted caller.

### Deferred to later (explicitly out of scope for this feature)

- Edit/Delete Sponsor (not requested).
- Sponsor logo upload flow details (endpoint reuse vs. new upload endpoint) — resolved in Phase 2 since it's UI-driven, same as Article's header image.
- Canadian business addresses (assumption 1).

---

## Phase 2: UI

### Pages (`src/Neba.Website.Server/Sponsors/`)

- **New — `SponsorsManage.razor`** (`@page "/sponsors/manage"`) — admin-gated list of sponsors, calling the existing (now permission-aware, per Phase 1's amendment) `ISponsorsApi.ListActiveSponsorsAsync`. Since the page is only reachable behind `Permissions.CreateSponsor.PolicyName`, the same authenticated call that renders the page also carries the claim the API checks — the caller gets every sponsor back, active and inactive, in one request. No separate admin endpoint or view model needed; reuses `SponsorSummaryViewModel`/`SponsorMappingExtensions` as-is.
  - **Active/inactive split**: rendered as two sections, not one flat list — an **Active Sponsors** section on top (grouped by tier the same way the public `Sponsors.razor` page does: Title / Premier / Standard) and a separate **Inactive Sponsors** section below it. This keeps a former Title Sponsor that's now inactive from visually competing with the current Title Sponsor for the single "top slot" styling — the inactive section renders as a plain list (no title/premier tier treatment), since tier styling on an inactive record would be misleading. Computed client-side in `@code`: `ActiveSponsors => _sponsors.Where(s => s.IsCurrentSponsor)`, `InactiveSponsors => _sponsors.Where(s => !s.IsCurrentSponsor)`.
  - Structurally mirrors `NewsList.razor` otherwise: title bar, `<AuthorizeView Policy="@Permissions.CreateSponsor.PolicyName">`-gated `FabCreateButton Href="/sponsors/new" Label="Create Sponsor"`, each row showing Name, Tier, Category, Priority.
  - `<PageTitle>Manage Sponsors - BowlNEBA</PageTitle>`, `@rendermode @(new InteractiveServerRenderMode(prerender: false))` (loads data in `OnInitializedAsync`, same reasoning as `NewsList.razor`).
- **New — `CreateSponsor.razor`** (`@page "/sponsors/new"`) — mirrors `CreateArticle.razor`'s structure end-to-end: `EditContext`-based `EditForm` + `DirtyFormGuard`, sections for:
  - Core fields: Name, Slug (optional override + auto-generated placeholder preview, same `NormalizeSlug` JS-free client mirror as `CreateArticle.razor`), IsCurrentSponsor (checkbox), Priority (number input), Tier (`InputSelect` over `SponsorTier.List`), Category (`InputSelect` over `SponsorCategory.List`).
  - Logo: single-file `FileUpload` (image only), same upload-then-attach pattern as Article's header image (`UploadHeaderImageAsync`-equivalent hitting a new-or-reused sponsor logo upload endpoint — see Open Question below).
  - Optional promo fields: WebsiteUrl, TagPhrase, Description, LiveReadText, PromotionalNotes, FacebookUrl, InstagramUrl — plain `InputText`/`InputTextArea`.
  - Business address block: Street/Unit/City `InputText`, `UsState` `InputSelect`, PostalCode `InputText` — manual entry only, per the scoping decision above.
  - Business email: `InputText` (validated client-side as an email format; server does the authoritative `EmailAddress.Create` validation).
  - Phone numbers: repeatable rows (Type `InputSelect` over `PhoneNumberType`, Number, Extension) with add/remove — see `PhoneNumberListEditor` component below.
  - Contact info: Name/Phone/Email fields, enforced all-or-nothing client-side (disable submit / show a validation message if exactly 1–2 of the 3 are filled) to match the handler's all-or-nothing rule from Phase 1's assumption 3.
  - `<PageTitle>Create Sponsor - BowlNEBA</PageTitle>`. No async initial data load (Tier/Category are static `SmartEnum` lists, no API dependency for dropdown population) — default `@rendermode InteractiveServer` (prerender true) is sufficient, no flash risk.
- **`Sponsors.razor`** (existing public page) — untouched.

### Components (`src/Neba.Website.Server/Sponsors/`, new)

- **New — `PhoneNumberListEditor.razor`** — small reusable repeatable-row editor for a `List<PhoneNumberInput>`-shaped binding (Type/Number/Extension per row, add/remove buttons). Built as a standalone component now (not inlined in `CreateSponsor.razor`) since the same editor will be needed by a future Edit Sponsor page — avoids inlining logic that's already known to be reused.

### API Client (`src/Neba.Website.Server/Sponsors/`)

- No new `ISponsorsApi` methods and no new view model — `SponsorsManage.razor` reuses `CreateSponsorAsync` (Phase 1) and the existing `ListActiveSponsorsAsync` + `SponsorSummaryViewModel`/`ToViewModel()` unchanged.

### Dirty tracking / guard

`DirtyFormGuard` applies to `CreateSponsor.razor` (data-entry form) — same wiring as `CreateArticle.razor`: `EditContext` created in the constructor, `OnFieldChanged += MarkDirty`, explicit `MarkDirty()` calls for anything not routed through an `InputBase` descendant (the Logo `FileUpload` callbacks, `PhoneNumberListEditor` add/remove callbacks, and the Tier/Category selects if they end up as plain `<select>`/`@onchange` rather than `InputSelect` — prefer `InputSelect` bound through `EditContext` where possible so `OnFieldChanged` covers it for free, matching CLAUDE.md's guidance). Reset `_isDirty = false` right before navigating away after a successful create. `SponsorsManage.razor` is a list/display page, not a data-entry form — no guard needed.

### Tests (Phase 2)

- **bUnit** (`tests/Neba.Website.Tests/Sponsors/`): `CreateSponsorPageTests.cs` (form validation, dirty-tracking marks, contact-info all-or-nothing client check, submit success → navigates to `/sponsors/{slug}`, submit failure → shows `_errorMessage`), `SponsorsManagePageTests.cs` (active sponsors render in the top section grouped by tier, inactive sponsors render in a separate bottom section without tier styling, FAB visibility gated on permission, empty state). Mock `ISponsorsApi` per CLAUDE.md's `StubApiResponse<T>` convention (not `Mock<IApiResponse<T>>`).
- **Playwright** (`tests/e2e/`): extend `Sponsors.spec.ts` or add `SponsorsManage.spec.ts` — this qualifies per the new-page-with-API-backed-rendering and navigation-flow rows of the Playwright-vs-bUnit decision table (list page → create page → back, real HTTP + real browser). The existing `MOCK_SPONSOR_OLD_SPONSOR` fixture (`isCurrentSponsor: false`) is already usable for the inactive-section case — extend the mock `GET /sponsors` handler to return the full set (including it) when the request carries the management permission, matching the amended endpoint's real behavior, plus add a `POST /sponsors` handler. Cover: navigate to manage page, confirm active/inactive sections render separately, click FAB, fill form, submit, land on detail page; and a validation-failure path (duplicate slug → 409 → inline error shown, no navigation).

### Open question for this gate

**Sponsor logo upload endpoint** — `CreateArticle.razor` uploads the header image via `NewsApi.UploadArticleHeaderImageAsync` (a News-scoped upload endpoint) before the article itself is created, tracking it as a `PendingUpload` that's claimed on `CreateArticleCommandHandler`'s success path. Does Sponsor logo upload need its own equivalent (`ISponsorsApi.UploadSponsorLogoAsync` + matching `PendingUploads`-claim logic in `CreateSponsorCommandHandler`, which Phase 1's functional draft didn't include), or is there a shared/generic upload endpoint already usable across features that this should call instead? Flagging since Phase 1 didn't scope this — confirm before the code draft so the endpoint list is right in both phases.

### Deferred to later (explicitly out of scope for this feature)

- Edit/Delete Sponsor pages (not requested — `PhoneNumberListEditor` is still built as a standalone component in anticipation, per above).
- Google Places (or equivalent) address autocomplete — tracked as [`docs/plans/address-autocomplete-issue.md`](./address-autocomplete-issue.md), to revisit once member self-service address updates (or another second address-entry form) exist to justify the shared integration cost.
- A dedicated `Sponsors.View`/`Sponsors.Manage` permission distinct from `Sponsors.CreateSponsor`, if the single-permission `SponsorManagementPermissions` collection turns out to be too coarse once Edit/Delete Sponsor exist (same shape as `ArticleManagementPermissions` growing to include `EditArticle`/`DeleteArticle`).
