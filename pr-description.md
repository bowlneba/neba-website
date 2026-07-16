## Summary

- **Create Sponsor**: staff can create a sponsor (name, tier, category, contact info, business address, phone numbers, social links, logo) from a new admin-only Blazor form, backed by a new `CreateSponsor` command/endpoint.
- **Upload Sponsor Logo**: standalone endpoint for uploading a sponsor logo image ahead of/independent from sponsor creation, following the pending-upload/claim pattern used elsewhere.
- Sponsor detail and sponsor list now distinguish public vs. management-scoped visibility, so admins can preview an inactive/non-current sponsor while anonymous callers still only ever see the current sponsor(s).
- Slug normalization logic shared between News (Article) and Sponsors is extracted into a common `SlugNormalizer` domain helper, with a matching client-side `SlugPreviewGenerator` for live slug preview in both create forms.

## Context

Only one sponsor may hold the `TitleSponsor` tier at a time — enforced both as a fast pre-check in `Sponsor.Create` (via an `isTitleSponsorshipAvailable` flag the handler computes) and as the actual guarantee via a filtered unique database index, since the pre-check alone can't rule out a concurrent create.

## What Changed

### Domain
- `Sponsor.Create(...)` factory (`Features/Sponsors/Domain/Sponsor.cs`): validates name, normalizes/validates slug (rejects empty-after-normalization and the reserved value `"new"`), enforces single-Title-sponsor invariant, builds the always-valid aggregate.
- `SponsorErrors`: `NameRequired`, `SlugInvalid`, `SlugReserved`, `SlugAlreadyExists`, `TitleSponsorshipUnavailable`.
- New shared `Neba.Api.Domain.SlugNormalizer` — extracted from `Article`'s private `NormalizeSlug`, now used by both `Article.Create` and `Sponsor.Create`.
- `Sponsor.PhoneNumbers` / `Sponsor.TournamentsSponsored` and `BowlingCenter.PhoneNumbers` changed from `= []` to `= new List<T>()` defaults (with `IDE0305` suppressions) — the collection-expression form resolves to a fixed-size array and throws `NotSupportedException` during EF owned-collection navigation fixup. Guarded by new `..._ForEfFixup` regression tests.

### Application / API
- `CreateSponsorCommand` / `CreateSponsorCommandHandler` — builds business address/email/phone numbers/contact info, checks Title-sponsor availability, enforces slug uniqueness, claims any pending logo upload, invalidates the `neba:sponsors` cache tag.
- `CreateSponsorEndpoint` — `POST /sponsors`, gated by new `Permissions.CreateSponsor` policy, `201`/`400`/`401`/`403`/`409`/`422` responses.
- `UploadSponsorLogoEndpoint` — `POST /sponsors/logo`, same permission gate.
- `GetSponsorDetailQueryHandler` / `ListActiveSponsorsQueryHandler` — now take `CallerHasSponsorManagementPermission`; non-management callers only ever see `IsCurrentSponsor` sponsors (an inactive sponsor returns the same "not found" as a nonexistent slug for unpermitted callers).
- `CacheDescriptors.Sponsors.Detail(...)` / `ListActiveSponsors(...)` — cache key now includes a `management`/`public` scope segment so a management-scoped response is never served from cache to an anonymous/unpermitted caller.
- New `Permissions.CreateSponsor` + `SponsorManagementPermissions` collection.
- `DatabaseConfiguration` / `SecurityConfiguration` — added `EnableRetryOnFailure` to both Npgsql contexts.

### Contracts
- `Neba.Api.Contracts.Sponsors`: `CreateSponsorRequest`, `SponsorInput`, `SponsorContactInput`, `SponsorLogoInput`, `SponsorPhoneNumberInput`, `SponsorResponse`, `ISponsorsApi` additions.
- `UploadSponsorLogoRequest`.
- New shared `PhoneNumberInput` contract (`Neba.Api/Contacts/PhoneNumberInput.cs`).

### Blazor (`Neba.Website.Server`)
- New `Sponsors/CreateSponsor.razor` (+ `.razor.css`) — full create form (name, tier, category, contact, address, phone numbers, social links, logo upload, live slug preview), gated behind `Permissions.CreateSponsor`, wired to the shared `DirtyFormGuard`.
- `Sponsors.razor` — adds `FabCreateButton` entry point for admins.
- `SponsorDetail.razor` / `SponsorDetailViewModel` — reflects management-scoped visibility for inactive sponsors.
- New `Services/SlugPreviewGenerator.cs` — client-side mirror of the domain `SlugNormalizer`, cosmetic preview only (server always computes the real slug).
- `News/CreateArticle.razor` updated to use the shared slug preview generator instead of a local implementation.

### Tests
- New unit tests: `SponsorTests` (aggregate factory, invariants, EF-fixup regression), `SlugNormalizerTests`, `SlugPreviewGeneratorTests`, `CreateSponsorCommandHandlerTests`, `CreateSponsorRequestValidatorTests`, `CreateSponsorEndpointTests` + authorization tests, `CreateSponsorSummaryTests`, `UploadSponsorLogoEndpointTests` + authorization tests, `UploadSponsorLogoRequestValidatorTests`, `UploadSponsorLogoSummaryTests`, `PermissionsTests` addition, `BowlingCenterTests` EF-fixup regression.
- Updated: `GetSponsorDetailQueryHandlerTests`, `GetSponsorDetailQueryTests`, `ListActiveSponsorsQueryHandlerTests`, `ListActiveSponsorsQueryTests`, `ListActiveSponsorsEndpointTests` for management-scoped visibility.
- New test factories: `PhoneNumberInputFactory`, `SponsorInputFactory`, `CreateSponsorRequestFactory`, `CreateSponsorResponseFactory`, `CreatedSponsorFactory`; updates to `SponsorFactory`, `BowlingCenterFactory`.
- New Blazor tests: `CreateSponsorTests`, `SponsorDetailTests` update, `SponsorsTests` addition.
- New E2E doc-screenshot spec: `tests/e2e/docs-screenshots/create-sponsor.spec.ts`.

### Docs
- `docs/help/create-sponsor.md` (+ screenshots) — new help doc for the Create Sponsor flow.
- `docs/plans/create-sponsor.md` — feature plan.
- `docs/ubiquitous-language.md` updated.
- `.claude/skills/feature-plan/SKILL.md` added; `CLAUDE.md` updated with EF navigation-fixup learnings.

## Test Plan

- [ ] `dotnet test --filter "Category=Unit"` — all unit tests pass
- [ ] `dotnet test --filter "Category=Integration"` — all integration tests pass
- [ ] `dotnet test --filter "Component=Sponsors"` — component tests pass
- [ ] `npm run test:e2e` — `create-sponsor.spec.ts` passes
- [ ] Navigate to `/sponsors` as an admin, use the FAB to open `/sponsors/new`, create a sponsor with a logo and phone numbers, and confirm it appears in the list and detail page
- [ ] Confirm a second `TitleSponsor` create attempt while one is already current returns a 409
- [ ] Confirm an anonymous/unpermitted caller cannot see an inactive sponsor's detail page or see it in the list

## Deferred

- Dynamic per-document management noted elsewhere in the backlog is unrelated to this PR; no sponsor-specific deferrals beyond what's tracked in `docs/plans/create-sponsor.md`.
