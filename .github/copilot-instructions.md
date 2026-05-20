# Copilot Instructions

## Architecture

This project uses **Vertical Slice Architecture**. All backend logic lives in `Neba.Api` — there are no separate Domain, Application, or Infrastructure projects. Each feature is a self-contained slice under `Neba.Api/Features/{Feature}/`:

- `Domain/` — feature-specific aggregates, value objects, domain events
- `{UseCase}/` — one folder per use case: Endpoint, Command/Query, Handler, DTO, Validator, Summary

Shared cross-cutting domain base types (`AggregateRoot`, `IDomainEvent`) live in `Neba.Api/Domain/`.

## Project Guidelines

- Each use case folder defines its own DTO — never reuse DTOs across feature slice boundaries, to avoid coupling and shape-change breakage
- Handlers inject `AppDbContext` directly; there is no repository abstraction
- Feature domain types must not reference domain types from other features (importing a typed ID like `BowlerId` is the only allowed exception)
- Handler and domain types are `internal`; only endpoint and contract types are `public`