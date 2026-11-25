---
name: vsa-ddd
description: Design backend features in Neba.Api as vertical slices with a domain-driven design approach. Use when modeling a domain (aggregates, entities, value objects, typed IDs, ErrorOr results), deciding where a rule lives (domain, handler, validator or endpoint), or shaping a new use-case folder with Fast Endpoints. Not for Blazor pages (new-blazor-page), EF configuration and migrations (add-ef-migration), the step order for one endpoint (new-endpoint), or writing tests (write-tests).
---

# Vertical slices with DDD design

Neba.Api is **vertical slice architecture**, not clean architecture. All backend code lives in one project. Each feature is a folder under `Features/`. DDD guides how we model a feature, not how we split projects.

Read first: `CLAUDE.md` (rules), `docs/architecture/backend.md`, `docs/learnings/domain-design.md`, `docs/ubiquitous-language.md`.
Real slice to copy: `src/Neba.Api/Features/Sponsors` (command: `CreateSponsor`; query: `ListActiveSponsors`; domain: `Domain/`).

## This repo overrides (gotchas)

Generic DDD and clean-architecture advice often says otherwise, and models repeat it. Here:

- **No repositories.** Handlers inject `AppDbContext` and query it. Do not add `IFooRepository` or a generic repository.
- **No Domain, Application or Infrastructure projects.** No layer diagram. A feature folder holds its domain, use cases and errors.
- **No MediatR or mediator.** Handlers implement `ICommandHandler<,>` or `IQueryHandler<,>` from `Neba.Api.Messaging`. Reject any suggestion to add a mediator.
- **Use-case folders are the boundary.** A use case never depends on another feature's use-case code. Each use case owns its DTO. A feature's `Domain` folder may reference another feature's domain types (navigations, typed IDs). `FeatureBoundaryTests` enforces the use-case rule.
- **Shared class from another feature?** Put it in the using feature's namespace, since the architecture test treats a feature's root namespace as use-case code.
- **Routes:** no `/api` prefix, no version in the path (versioning is by header). Outside `/legacy`, use standard REST: id in the path, the verb names the action.
- **Cache:** query caching uses `IFusionCache`, not `HybridCache`.
- **Time:** inject `TimeProvider`. Never call `DateTime.UtcNow` or `DateTimeOffset.UtcNow` in domain or handler code. Use `DateTimeOffset` for points in time, `DateOnly` and `TimeOnly` for dates and times alone.
- **Collections:** never return `null` for a collection. Return an empty one.

## Design mentality

Model the domain first, then the slice.

1. **Language.** Find the domain terms and check `docs/ubiquitous-language.md`. Use the same words in types, methods and errors. Add new terms to the glossary.
2. **Boundary.** Decide what must be consistent in one transaction. That is an aggregate. Keep it small. Refer to other aggregates by typed ID.
3. **Behavior.** Put rules on the aggregate as methods that return `ErrorOr`. Avoid an anemic model with a handler full of `if` statements.
4. **Slice.** Add one folder per use case: endpoint, summary, command or query, handler, validator, DTO.

Where a rule lives:

| Rule type | Home |
|---|---|
| An invariant of one entity or value object (a score above zero) | That type's `Create` factory |
| An invariant of the aggregate (awards only on a completed season) | An aggregate method |
| A rule that needs another aggregate's live data | Aggregate method. The handler queries the fact and passes it in. |
| Input shape (required, length, format) | Validator. No DB lookups, no business rules. |
| Orchestration (load, call aggregate, save, evict cache) | Handler |
| Mapping HTTP to command and result to status code | Endpoint. Keep it thin. |

Domain complexity decides how much DDD to use. A plain lookup table needs an entity and a query, not a rich model.

## Strategic design

- **Ubiquitous language:** one name per concept, used in code, docs and conversation.
- **Bounded contexts:** a feature folder is a context with its own model. Do not share entities between features. Share typed IDs.
- **Core vs supporting:** spend design effort on the core (tournaments, awards, stats). Keep supporting features (news, sponsors) simple.

## Tactical patterns

### Entities and aggregates

- Identity by typed ID. Equality by identity.
- One root per aggregate. The root enforces invariants and exposes behavior, not setters or mutable collections.
- Back collection navigations with a `List<T>` field and expose `IReadOnlyCollection<T>` (see `docs/learnings/backend.md` for the EF rule).
- `AggregateRoot` in `Neba.Api/Domain/` gives `AddDomainEvent` (protected). No feature raises events today. Add one only when a use case needs it.

### Always-valid child entities

A child entity owns its own invariants through `internal static ErrorOr<T> Create(...)`. The aggregate's assign method calls it and adds aggregate-level checks. Never validate child fields on the aggregate, and never make `Create` public. Full rules and code: `docs/learnings/domain-design.md`.

### Cross-aggregate data

The handler queries live data owned by another aggregate and passes it as a parameter. A per-instance formula coefficient persists on the aggregate. The handler never computes the domain formula. Details: `docs/learnings/domain-design.md`.

### Value objects, typed IDs, SmartEnums

A value object that EF persists is a `sealed record class` with a private constructor and a `Create` factory (a struct would skip the constructor). Typed IDs use `[StronglyTypedId("ulid-full")]`. Domain enumerations are SmartEnums, and cached query DTOs hold primitives, never SmartEnum instances. Code and the full table: `references/value-objects-ids-enums.md`.

### Results with ErrorOr

- Commands return `ErrorOr<T>`. Do not throw for business rules.
- Queries return DTOs, never entities.
- Error codes are `Entity.Rule` or `Entity.Property.Rule` (PascalCase, dot-separated).
- Errors live in an `internal static class {Type}Errors` beside the code that returns them: in the feature folder (`Features/Sponsors/SponsorErrors.cs`) or in `Domain/` for domain types (`CertificationNumberErrors.cs`).
- `Error.Validation` maps to 422: the input is wrong. `Error.Conflict` maps to 409: the input is valid, but state blocks it. Retry test: if the same payload would succeed after a state change, it is `Conflict`. `Error.NotFound` maps to 404. A delete of a missing record returns 204.

## Use-case folder

One folder per use case under `Features/{Feature}/`: endpoint, summary, command or query, handler, and a request validator when the request has rules. Endpoint, handler and domain types are `internal`; endpoints are `sealed`. Request, Input and Response records live in `Neba.Api.Contracts/{Feature}/{UseCase}/`. Layout tree and the endpoint checklist: `references/use-case-folder.md`.

## Type design

- Seal classes and records unless built for inheritance.
- Use `FrozenDictionary` and `FrozenSet` for lookup data built once at startup.
- Put behavior in the domain. A model that is only getters and setters is a smell.

## Anti-patterns

- **Anemic model:** entities with no behavior, rules scattered in handlers.
- **Repository or generic repository:** not used here.
- **God aggregate:** too large to load and save as one unit. Split it and reference by ID.
- **Mediator:** do not add one.
- **Domain formula in a handler:** the formula belongs in the domain.
- **Business rules in a validator:** validators check shape only.
- **Cross-use-case dependency:** copy the small DTO instead of reusing another use case's.

## Testing

See `docs/learnings/testing.md` and the `test-factory-generator` skill. Never build entities by hand in tests.
