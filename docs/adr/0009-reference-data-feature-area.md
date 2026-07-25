# ADR-0009: Reference Data as a Dedicated Feature Area

## Status

Accepted

## Context

The Sponsors feature (`features/sponsors` branch) needed a US-state dropdown on its business-address form. It reused `Neba.Api.Contracts.Contact.UsState` — a static `SmartEnum` — directly:

- `Address.Create(...)` (a `Neba.Api` **domain** value object) took a `UsState` parameter, requiring `Neba.Api`'s domain layer to reference `Neba.Api.Contracts` — the outer contracts library. `Neba.Api.Contracts` also holds every feature's request/response DTOs and Refit interfaces (`IAwardsApi`, `IBowlingCentersApi`, ...), so this pulled the domain layer's dependency graph across a boundary it has no business crossing, even though the specific type used (`UsState`) is itself just an anemic enum.
- `CreateSponsor.razor`/`EditSponsor.razor` (in `Neba.Website.Server`, a separate deployable reached only through the API's Refit contracts) read `UsState.List` directly to populate the state dropdown. This only compiled because `UsState` happened to live in `Neba.Api.Contracts`, which the website project references for its API client types — the same problem, from the other direction: the UI depends on a compiled Domain-shaped type instead of asking the API for data.

Neither direction is sound. The underlying issue is that `UsState` (and its siblings — `CanadianProvince`, `PhoneNumberType`, `Country`) are domain vocabulary owned by `Neba.Api`, but two different consumers (the website's dropdowns, and any future non-.NET client) need the *values*, not the compiled type. Moving the type to `Neba.Api`'s domain layer fixes the domain-boundary violation, but by itself reopens the UI duplication problem: without some way to reach the list from the website, either the website re-declares the same 51 states, or someone reaches back into `Neba.Api.Contracts.Contact` again next time and the cycle repeats.

## Decision

Static/rarely-changing lookup data (state and province lists, phone number types, and similar) is served through a dedicated **`Neba.Api.ReferenceData`** feature area, structured like this for each lookup list:

- **The enum stays (or moves back to) `Neba.Api.Contacts.Domain`** as a `SmartEnum`, owned by the domain layer that actually validates against it (`Address.Create`, `PhoneNumber`, etc.). It is never referenced from `Neba.Api.Contracts` or from `Neba.Website.Server`. All four types identified in Context — `UsState`, `CanadianProvince`, `Country`, `PhoneNumberType` — moved together, since they all had the same domain-boundary problem regardless of whether a UI dropdown exists for them yet.
- **`Neba.Api.ReferenceData`** is a top-level folder in `Neba.Api`, a sibling of `Features/`, not nested inside it — mirroring the existing precedent of cross-cutting top-level folders (`Compliance/`, `Auditing/`, `Geography/`) rather than being owned by one feature. Each lookup gets its own `List{Thing}` slice (`Query` + `QueryHandler` + `Endpoint` + `Summary` + a `{Thing}Dto`) under `GET /reference-data/{route}` — no validator, since there's no request body.
- **An endpoint is only added for a lookup once something actually consumes it.** `ListUsStates` and `ListPhoneNumberTypes` exist because `CreateSponsor.razor`/`EditSponsor.razor` read `UsState.List`/`PhoneNumberType.List` directly for their state and phone-type dropdowns. `CanadianProvince` and `Country` moved to the domain layer in the same pass (closing the boundary violation for them too), but got no endpoint — no Canadian-address or country-selector form exists in the website yet. Add `ListCanadianProvinces`/`ListCountries` following the same shape the day a real UI consumer needs them, not speculatively ahead of that need.
- **Each query implements `ICachedQuery<T>`** via a new `CacheDescriptors.ReferenceData` category, with a long expiry (30 days — this data changes on a scale of decades, not days).
- **`Neba.Api.Contracts.ReferenceData`** holds the wire DTOs (`UsStateResponse { Name, Code }`, `PhoneNumberTypeResponse { Name, Code }`) and one `IReferenceDataApi` Refit interface aggregating every reference-data endpoint, rather than one Refit interface per lookup.
- **`Neba.Website.Server.ReferenceData.IReferenceDataService`** wraps each Refit call with an `IMemoryCache` layer, so a page visit doesn't cost a network round-trip even though the API's own FusionCache already makes that round-trip cheap server-side. This is the first client-side cache in the website project; it establishes the pattern other rarely-changing lookups should follow rather than each inventing its own caching.
- **`CreateSponsor.razor`/`EditSponsor.razor`** inject `IReferenceDataService` and populate their state and phone-type dropdowns from the `Response` DTOs' `Code`/`Name` instead of a domain `SmartEnum`.

## Scope: not every dropdown is reference data

This ADR governs vocabulary that is either genuinely cross-cutting (shared by more than one feature domain) or that would otherwise force a `Domain → Contracts` dependency if left where it was. It does **not** cover every `SmartEnum`-backed dropdown in the app — most enums (`SponsorTier`, `SponsorCategory`, `TournamentType`, ...) are owned outright by a single feature and never cross a domain boundary the way `UsState` did.

The test for which bucket a lookup belongs in:

- **Used by more than one feature, or reachable only by crossing `Domain → Contracts`?** → `Neba.Api.ReferenceData`, per the Decision above.
- **Owned by, and only ever consumed by, one feature?** → a `List{Thing}` slice living **inside that feature's own folder** (`Features/{Owner}/List{Thing}/`), not under `ReferenceData`. It uses the identical internal shape (`Query` + `QueryHandler` + `Endpoint` + `Summary` + a `{Thing}SummaryDto`, `ICachedQuery<T>`, `AllowAnonymous()`), and the underlying reason to add it is the same one motivating this ADR — a hand-copied UI dropdown drifting out of sync with the domain enum it's supposed to mirror — but the endpoint's *location* follows ownership, not "is this a lookup list."

Two enums got two different treatments for exactly this reason, in the same feature (`Tournaments`, added when the Create Tournament flow was planned):

- `OilPattern` moved to a **top-level** `Features/OilPatterns`-style route (`/oil-patterns`, its own `IOilPatternsApi`) even though its code still lives under `Features/Tournaments` — because it's genuinely shared reference/catalog data (a pattern created once gets reused across many tournaments and, eventually, likely other consumers), the same shape as `BowlingCenter`/`Season`.
- `TournamentType` got a `ListTournamentTypes` slice that stayed **nested inside** `Features/Tournaments` (`/tournaments/types`, on the existing `ITournamentsApi`) — it's single-feature-owned vocabulary with no cross-feature reuse and no `Domain → Contracts` violation to fix, so routing it through `ReferenceData` would have been solving a boundary problem that doesn't exist here, just to reuse a caching pattern that's equally available in-feature.

`SponsorTier`/`SponsorCategory` remain hardcoded lists in `Neba.Website.Server.Sponsors` (`SponsorCategoryOptions.cs`, inline `<option>`s) as of this writing — they have the same "two spots to update" problem `ListTournamentTypes` was built to avoid, but haven't been migrated. That's an existing shortcut to clean up opportunistically, not a pattern endorsed by this ADR; don't cite `SponsorCategoryOptions` as precedent for a new hardcoded list when a `List{Thing}` slice is the correct fix.

## Consequences

### Positive

- Domain stays free of any dependency on `Neba.Api.Contracts` for this vocabulary; the dependency graph runs `Contracts → Domain`, never the reverse.
- The website (and any future client) reaches lookup data the same way it reaches every other piece of API data — through a versioned, cacheable HTTP endpoint — instead of a compile-time reference to a type that happens to live in a shared assembly.
- One UI-side caching pattern (`IReferenceDataService` + `IMemoryCache`) now exists to extend for the remaining lookups, rather than each page re-solving "how do I avoid re-fetching this."
- `ICachedQuery` + FusionCache reuse the same caching infrastructure already used everywhere else in the API — no new caching mechanism introduced server-side.

### Negative

- `CanadianProvince` and `Country` have no `ReferenceData` endpoint yet, so if a Canadian-address or country-selector UI is added later, that work isn't "just wire it up" — it needs the same `List{Thing}` slice built first. This is a deliberate trade (no speculative endpoint for data nothing reads), not an oversight, but it does mean this ADR's job isn't visibly "finished" from the API surface alone.
- A 30-day cache on both sides (server FusionCache + client `IMemoryCache`) means a correction to the reference list (unlikely for US states, more plausible for a future lookup) takes up to 30 days to reach a long-running Blazor Server circuit unless the cache is explicitly invalidated. Acceptable for data that changes on a decades timescale; a future lookup with a shorter change cycle should choose a shorter expiry rather than reusing 30 days by default.
- One more Refit interface and one more DI registration (`IReferenceDataApi`, `IReferenceDataService`) added to `ApiServicesConfiguration.cs`/`Program.cs` for what is, today, a single endpoint.

## Related Decisions

None yet — this is the first ADR covering reference/lookup data specifically.
