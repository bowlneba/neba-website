# Copilot Instructions

Read `AGENTS.md` for the project map and `CLAUDE.md` for the rules. For PR reviews, also follow `.github/instructions/pull-request-review.instructions.md`.

Skills in `.github/skills/` carry the repo's procedures: `vsa-ddd` (slice and domain design), `new-endpoint`, `new-domain-entity`, `add-ef-migration`, `new-blazor-page`, `new-background-job`, `write-tests`, and `convention-review` (the convention checklist). They mirror `.claude/skills/`; where a skill mentions an "agent", that is a Claude Code feature and you can ignore it.

## Architecture

This project uses **Vertical Slice Architecture** with a DDD design approach. All backend logic lives in `Neba.Api`. There are no separate Domain, Application, or Infrastructure projects. Each feature is a slice under `Neba.Api/Features/{Feature}/`:

- `Domain/` — feature aggregates, entities, value objects, typed IDs
- `{UseCase}/` — one folder per use case: Endpoint, Command/Query, Handler, DTO, Validator, Summary

Shared domain base types (`AggregateRoot`, `IDomainEvent`) live in `Neba.Api/Domain/`.

## Project Guidelines

- A use-case folder must not depend on another feature's use-case code. Each use case defines its own DTO. `FeatureBoundaryTests` enforces this.
- A feature's `Domain` folder may reference another feature's domain types (EF navigations, typed IDs). The aggregate is the unit of consistency, not the folder.
- Handlers inject `AppDbContext` directly. There is no repository abstraction.
- Commands return `ErrorOr<T>` and never throw for business rules. Queries return DTOs, never entities.
- Use `Error.Validation` (422) when the input is wrong and `Error.Conflict` (409) when the state blocks a valid request.
- Endpoint, handler and domain types are `internal`. Contract types are `public`.
- Routes have no `/api` prefix and no version in the path.
