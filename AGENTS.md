# NEBA website: project map

The website for the New England Bowlers Association (NEBA). It runs tournaments, applies NEBA and USBC rules, and manages members. It has a public site, an admin area, a REST API, and a temporary bridge (`/legacy`) that mirrors actions from the old desktop "Software" into this database. The bridge goes away when Software sunsets.

Stack: .NET 10, ASP.NET Core with Fast Endpoints, EF Core on PostgreSQL, Blazor Server, Hangfire, Aspire, xUnit v3 with Shouldly, Playwright.

Read `CLAUDE.md` first. It holds the rules. This file says where things are.

## Solution map

| Path | What it is |
|---|---|
| `src/Neba.Api` | The API and all backend logic. Vertical slices under `Features/`. No separate Domain, Application or Infrastructure projects. |
| `src/Neba.Api.Contracts` | Public request and response types shared with the website. |
| `src/Neba.Website.Server` | The Blazor Server site. Pages sit in feature folders (`Sponsors/`, `HallOfFame/`). Shared components are in `Components/`. API calls go through `Services/ApiExecutor.cs`. |
| `src/Neba.Website.Client` | The WebAssembly entry point. Almost empty. |
| `src/Neba.MigrationService` | Runs EF migrations. |
| `src/Neba.AppHost` | Aspire app model (`AppHost.cs`). |
| `src/Neba.ServiceDefaults` | Shared telemetry and health defaults. |
| `tests/Neba.Api.Tests` | Unit and integration tests for the API. Mirrors `src/Neba.Api` folders. |
| `tests/Neba.Website.Tests` | bUnit tests for Blazor. |
| `tests/Neba.TestFactory` | Test data factories. Never build entities by hand in tests. |
| `tests/e2e` | Playwright specs. `mock-api/mock-api-server.ts` fakes the API. |
| `docs/` | `architecture/`, `learnings/`, `adr/`, `help/`, `policies/`, `ubiquitous-language.md`. |

## Where things go

**A use case** is one folder: `src/Neba.Api/Features/{Feature}/{UseCase}/`.
It holds the endpoint, summary, command or query, handler, validator and its own DTO.
A use case never depends on another feature's use-case code.
Example to copy: `Features/Sponsors/CreateSponsor` (command) and `Features/Sponsors/ListActiveSponsors` (query).

**Domain types** go in `Features/{Feature}/Domain/` (aggregates, entities, value objects, typed IDs).
Shared base types are in `src/Neba.Api/Domain/`.
Example: `Features/Sponsors/Domain`.

**Cross-cutting code** (caching, email, storage, security, telemetry, background jobs) sits in the top-level folders of `src/Neba.Api`, beside `Features/`.

**Database code** (`AppDbContext`, entity configuration) is in `src/Neba.Api/Database/`.
Handlers inject `AppDbContext`. There are no repositories.

**Legacy bridge** code is in `src/Neba.Api/Legacy/`. Do not copy its route shape into new routes.

**A Blazor page** goes in a feature folder in `Neba.Website.Server`, with its `.razor.css` and `.razor.js` beside it.
Example: `Sponsors/CreateSponsor.razor`.

**Tests** mirror the source folder. A test for `Features/Sponsors/CreateSponsor` goes in `tests/Neba.Api.Tests/Features/Sponsors/`.
A new `required` field on a Contracts DTO also needs an update in `tests/e2e/mock-api/mock-api-server.ts`.

## Which skill for which task

| Task | Skill |
|---|---|
| Add an API endpoint | `new-endpoint` |
| Design a domain model, aggregate or slice | `vsa-ddd` |
| Plan a feature (API, then UI) | `feature-plan` |
| Run, debug or change the Aspire stack | `aspire` |
| Add a query cache descriptor | `cache-descriptor` |
| Add a test factory | `test-factory-generator` |
| Check changes against repo conventions | `convention-review` |
| Review for performance | `performance-review` |
| Prepare, describe and open a PR | `pull-request-prep`, `write-pr`, `create-pr` |
| Address PR review comments | `pull-request-comments` |
| Review a major dependency upgrade | `dependency-upgrade-review` |
| Ship a hotfix | `hotfix` |
| Write user help or policy docs | `help-documentation`, `policy-documentation` |
| Check domain terms against the glossary | `ubiquitous-language-review` |
| Kill surviving mutants | `mutation-score-dotnet`, `mutation-score-js` |
| Refresh plugin marketplaces | `marketplace-refresh` |

Generic .NET help (Blazor, EF Core queries, ASP.NET Core, testing) comes from the `dotnet-*` plugins. Where a plugin skill and a repo rule disagree, the repo rule wins.

## More detail

- Rules and gotchas: `CLAUDE.md` and `docs/learnings/*.md`.
- Backend design: `docs/architecture/backend.md`.
- Blazor design: `docs/architecture/blazor.md`.
- Domain terms: `docs/ubiquitous-language.md`.
- PR review rules: `.github/instructions/pull-request-review.instructions.md`.
- Design sketches in `reference/` are not production code.
