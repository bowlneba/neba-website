# NEBA Website

Platform for managing the New England Bowlers Association (NEBA). Handles tournament operations, enforces NEBA and USBC rules, and simplifies governance and member management.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)

## Code Status

### Quality Scans

[![CodeQL](https://github.com/bowlneba/neba-website/workflows/CodeQL/badge.svg)](https://github.com/bowlneba/neba-website/security/code-scanning)

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=bowlneba-website&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=bowlneba-website)

[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=bowlneba-website&metric=bugs)](https://sonarcloud.io/summary/new_code?id=bowlneba-website)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=bowlneba-website&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=bowlneba-website)

[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=bowlneba-website&metric=coverage)](https://sonarcloud.io/summary/new_code?id=bowlneba-website)
[![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=bowlneba-website&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=bowlneba-website)

[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=bowlneba-website&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=bowlneba-website)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=bowlneba-website&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=bowlneba-website)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=bowlneba-website&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=bowlneba-website)

[![Mutation testing badge](https://img.shields.io/endpoint?style=plastic&url=https%3A%2F%2Fbadge-api.stryker-mutator.io%2Fgithub.com%2Fbowlneba%2Fneba-website%2Fmain)](https://dashboard.stryker-mutator.io/reports/github.com/bowlneba/neba-website/main)

## Architecture

This application follows **Vertical Slice Architecture (VSA)** with **Domain-Driven Design (DDD)** tactical patterns and **CQRS**. Each feature is a self-contained slice that co-locates its domain types, handlers, and data access in one folder — there are no separate Domain/Application/Infrastructure projects. It is intentionally *not* a modular monolith — that complexity isn't justified for current scale (~1k members, 1-2 tournaments/month, ~10k visits/month).

### Project Structure

```text
src/
├── Neba.Api/                    # All backend logic: Fast Endpoints, domain, handlers, EF Core
│   ├── Domain/                  # Shared cross-cutting base types (AggregateRoot, IDomainEvent, ...)
│   ├── Features/
│   │   ├── Tournaments/
│   │   │   ├── Domain/                  # Tournament aggregate and domain types
│   │   │   ├── CreateTournament/        # One folder per use case: Endpoint, Command/Query,
│   │   │   │   ├── CreateTournamentEndpoint.cs   # Handler, DTO, Validator, Summary
│   │   │   │   ├── CreateTournamentCommand.cs
│   │   │   │   ├── CreateTournamentCommandHandler.cs
│   │   │   │   └── CreateTournamentRequestValidator.cs
│   │   │   ├── GetTournament/
│   │   │   ├── ListTournamentsInSeason/
│   │   │   └── TournamentsEndpointGroup.cs
│   │   ├── Bowlers/
│   │   ├── BowlingCenters/
│   │   ├── Sponsors/
│   │   ├── HallOfFame/
│   │   ├── Awards/
│   │   ├── Seasons/
│   │   ├── Stats/
│   │   ├── News/
│   │   └── Documents/
│   ├── Database/                # AppDbContext, EF Core entity configurations, migrations
│   ├── Messaging/                # IQueryHandler<,> / ICommandHandler<,> + handler scanning
│   ├── Caching/                  # FusionCache setup and decorators
│   ├── BackgroundJobs/           # Hangfire job definitions
│   ├── Storage/                  # Azure Blob Storage
│   └── Security/, Identity/      # Auth, policies, current-user resolution
│
├── Neba.Api.Contracts/           # Request/Input/Response records + Refit interfaces (shared with Blazor)
│   ├── Tournaments/
│   ├── Bowlers/
│   ├── BowlingCenters/
│   └── ...
│
├── Neba.Website.Server/          # Blazor Web App — all pages, all Interactive Server today
│   ├── Tournaments/               # Feature folder: pages + feature-specific components
│   ├── Sponsors/
│   ├── Account/                   # Login/Logout, admin auth
│   ├── Components/                # Generic, reusable components (no domain knowledge)
│   ├── Layout/
│   └── Services/
├── Neba.Website.Client/          # Blazor WebAssembly project — scaffolded, unused (no components yet need Interactive Auto)
├── Neba.AppHost/                 # .NET Aspire AppHost
└── Neba.ServiceDefaults/         # .NET Aspire service defaults
```

### Key Patterns

| Pattern | Implementation |
| ------- | -------------- |
| **Vertical Slice Architecture** | Each feature co-locates domain, handlers, and data access in `Features/{Feature}/` — no separate layer projects |
| **CQRS** | Command/Query separation, with handlers injecting `AppDbContext` directly — no repository abstraction |
| **Aggregate Roots** | Domain entities with consistency boundaries and domain events |
| **Value Objects** | Immutable domain concepts (Address, MembershipYear) |
| **Strongly-Typed IDs** | ULID-based (`BowlerId`) or natural keys (`BowlingCenterId` from USBC certification) |
| **Hybrid Identity** | ULID for domain identity, integer shadow property for database FKs ([ADR](docs/adr/0005-shadow-db-pk-for-natural-key-aggregates.md)) |
| **Result Pattern** | `ErrorOr<T>` for command results instead of exceptions |
| **Feature Isolation** | Feature `Domain/` folders never cross-reference each other; cross-feature needs use shared IDs or handler-level orchestration |

### Layer Responsibilities

| Location | Responsibility |
| ------- | -------------- |
| `Neba.Api/Features/{Feature}/Domain/` | Aggregate, entity, value object, domain event, and error types for that feature |
| `Neba.Api/Features/{Feature}/{UseCase}/` | Endpoint, Query/Command, Handler, DTO, Validator, Summary for one use case |
| `Neba.Api/Database/` | `AppDbContext`, EF Core entity configurations, migrations |
| `Neba.Api/Messaging/` | `IQueryHandler<,>`, `ICommandHandler<,>` and handler registration |
| `Neba.Api.Contracts` | Input/Response records and Refit interfaces shared between API and Blazor |
| `Neba.Website.Server` | Blazor Web App — hosts every page today, all rendered Interactive Server |
| `Neba.Website.Client` | Blazor WebAssembly project, wired up for Interactive Auto but not yet hosting any components — scaffolded so client-rendered components are a drop-in when a use case needs them |

### Technology Stack

| Layer | Technology |
| ------- | ---------- |
| **Runtime** | .NET 10 |
| **Backend** | ASP.NET Core Web API, Fast Endpoints |
| **Frontend** | Blazor Web App (Interactive Server today; WebAssembly project scaffolded for Interactive Auto), Tailwind CSS |
| **Database** | PostgreSQL |
| **ORM** | Entity Framework Core with EF Core Identity |
| **Local Development** | .NET Aspire |
| **Production** | Azure (App Service, Monitor, Key Vault, Blob Storage, Maps) |
| **Background Jobs** | Hangfire |
| **Caching** | FusionCache |
| **API Documentation** | Scalar |
| **HTTP Client** | Refit |
| **Testing** | xUnit, Moq, Shouldly, Bogus, Verify, Testcontainers, Respawn, bUnit, Playwright |

### Documentation

- [Backend Architecture](docs/architecture/backend.md)
- [Blazor Architecture](docs/architecture/blazor.md)
- [Ubiquitous Language](docs/ubiquitous-language.md)
- [Architecture Decision Records](docs/adr/README.md)

### Local EF Core Migrations

For local-only `dotnet ef` operations, do not hardcode or commit database credentials.

1. Set the local connection string in user-secrets (Infrastructure project):

  ```bash
  dotnet user-secrets --project src/Neba.Infrastructure set "ConnectionStrings:bowlneba" "Host=localhost;Port=52502;Database=bowlneba;Username=postgres;Password=<local-password>"
  ```

2. Run migrations from infrastructure:

  ```bash
  cd src/Neba.Infrastructure
  dotnet ef database update
  ```

Optional one-off (without user-secrets):

```bash
ConnectionStrings__bowlneba='Host=localhost;Port=52502;Database=bowlneba;Username=postgres;Password=<local-password>' dotnet ef database update
```

---

## Implementation Plan

### Public Website

- [x] Champions (History)
- [x] Bowler of the Year
  - [x] Open
  - [x] Senior
  - [x] Super Senior
  - [x] Woman
  - [x] Youth
  - [x] Rookie
- [x] High Average
- [x] High Block
- [x] Organization Bylaws
- [x] Tournament Rules
- [x] Hall of Fame
- [x] Bowling Centers
- [x] Tournaments
- [ ] Tournament Documents
- [x] Tournament Detail
- [x] Sponsors
- [x] News
- [x] About
- [x] Stats

### Website Administration

- [x] Authentication/Authorization
- [ ] Tournament Management
  - [x] Create Tournament
  - [x] Oil Patterns
  - [x] Tournament Sponsors
  - [x] Edit Tournament
  - [x] Delete Tournament
- [ ] Bowler Management
- [x] Content Management

### Platform & Operational

- [x] API Caching
- [x] Health Checks
- [x] SonarCloud Integration
- [x] Background Jobs
- [x] Global Exception Handling
- [x] OpenTelemetry
- [x] Rate Limiting & Throttling
- [x] API Documentation (Scalar)

### Documentation

- [x] Ubiquitous Language Definitions
- [x] Architecture Decision Records (ADRs)
- [ ] API Reference
- [ ] Administrative Website Manual
