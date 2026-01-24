# neba-website

Centralized platform for managing the New England Bowlers Association (NEBA). Handles tournament operations, enforces NEBA and USBC rules, and streamlines governance and member management.

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

## Architecture

This application follows **Clean Architecture** with **Domain-Driven Design (DDD)** tactical patterns and **CQRS**. It is intentionally *not* a modular monolith — complexity isn't justified for current scale (~1k members, 1-2 tournaments/month).

### Project Structure

```text
src/
├── Neba.Domain/                 # Entities, aggregates, value objects, domain events, repository interfaces
│   ├── Bowlers/
│   ├── BowlingCenters/
│   ├── Tournaments/
│   ├── Content/
│   └── SharedKernel/
│
├── Neba.Application/            # Commands, queries, handlers, application services
│   ├── Bowlers/
│   │   ├── Commands/
│   │   └── Queries/
│   ├── BowlingCenters/
│   ├── Tournaments/
│   └── Common/
│       └── Behaviors/
│
├── Neba.Infrastructure/         # EF Core DbContext, repository implementations, external services
│
├── Neba.Api/                    # Fast Endpoints, validators, API host
│   ├── Tournaments/
│   │   ├── CreateTournament/
│   │   │   ├── CreateTournamentEndpoint.cs
│   │   │   └── CreateTournamentValidator.cs
│   │   ├── GetTournament/
│   │   └── ListTournaments/
│   ├── Squads/
│   ├── Bowlers/
│   └── BowlingCenters/
│
├── Neba.Api.Contracts/          # Input records, response records, Refit interfaces (shared with Blazor)
│   ├── Tournaments/
│   ├── Squads/
│   ├── Bowlers/
│   └── BowlingCenters/
│
└── Neba.Website/                # Blazor Web App (Interactive Auto mode)
    ├── Neba.Website.Server/
    │   ├── Tournaments/         # Feature folder: pages + feature-specific components
    │   ├── Bowlers/
    │   ├── Components/          # Generic, reusable components (no domain knowledge)
    │   ├── Layout/
    │   └── Services/
    └── Neba.Website.Client/     # Components requiring browser execution (starts nearly empty)
```

### Key Patterns

| Pattern | Implementation |
| ------- | -------------- |
| **Clean Architecture** | Domain at center, dependencies point inward |
| **CQRS** | Command/Query separation with distinct read and write models |
| **Aggregate Roots** | Domain entities with consistency boundaries and domain events |
| **Value Objects** | Immutable domain concepts (Address, MembershipYear) |
| **Strongly-Typed IDs** | ULID-based (BowlerId) or natural keys (BowlingCenterId from USBC certification) |
| **Hybrid Identity** | ULID for domain identity, integer shadow property for database FKs ([ADR](docs/architecture/adr-ulid-shadow-keys.md)) |
| **Result Pattern** | `ErrorOr<T>` for command results instead of exceptions |
| **Feature Folders** | Organize by domain area, treating folders as if they were modules |

### Layer Responsibilities

| Layer | Responsibility |
| ------- | -------------- |
| `Neba.Domain` | Entities, aggregates, value objects, domain events, repository interfaces |
| `Neba.Application` | Commands, queries, handlers, application services, DTOs |
| `Neba.Infrastructure` | EF Core DbContext, repository implementations, external service clients |
| `Neba.Api` | Fast Endpoints, validators, real-time hubs (SSE/WebSocket) |
| `Neba.Api.Contracts` | Input records, response records, Refit interfaces shared with Blazor |
| `Neba.Website` | Blazor Web App (Interactive Auto mode) |

### Technology Stack

| Layer | Technology |
| ------- | ---------- |
| **Runtime** | .NET 10 |
| **Backend** | ASP.NET Core Web API, Fast Endpoints |
| **Frontend** | Blazor Web App (Interactive Auto), Tailwind CSS |
| **Database** | PostgreSQL |
| **ORM** | Entity Framework Core with EF Core Identity |
| **Local Development** | .NET Aspire |
| **Production** | Azure (App Service, Monitor, Key Vault, Blob Storage, Maps) |
| **Background Jobs** | Hangfire |
| **API Documentation** | Scalar |
| **HTTP Client** | Refit |
| **Testing** | xUnit, Moq, Shouldly, Bogus, Verify, Testcontainers, Respawn, bUnit, Playwright |

### Documentation

- [Backend Architecture](docs/architecture/backend.md)
- [Blazor Architecture](docs/architecture/blazor.md)
- [ADR: ULID and Shadow Key Pattern](docs/architecture/adr-ulid-shadow-keys.md)

---

## Implementation Plan

### Public Website

- [ ] Champions (History)
- [ ] Bowler of the Year
  - [ ] Open
  - [ ] Senior
  - [ ] Super Senior
  - [ ] Woman
  - [ ] Youth
  - [ ] Rookie
- [ ] High Average
- [ ] High Block
- [ ] Organization Bylaws
- [ ] Tournament Rules
- [ ] Hall of Fame
- [ ] Bowling Centers
- [ ] Tournaments
- [ ] Tournament Documents
- [ ] Tournament Detail
- [ ] Sponsors
- [ ] News
- [ ] About
- [ ] Stats

### Website Administration

- [ ] Authentication/Authorization
- [ ] Tournament Management
- [ ] Bowler Management
- [ ] Content Management

### Platform & Operational

- [ ] API Caching
- [ ] Health Checks
- [ ] SonarCloud Integration
- [ ] Background Jobs
- [ ] Global Exception Handling
- [ ] OpenTelemetry
- [ ] Rate Limiting & Throttling
- [ ] API Documentation (Scalar)

### Documentation

- [ ] Ubiquitous Language Definitions
- [ ] Architecture Decision Records (ADRs)
- [ ] API Reference
- [ ] Administrative Website Manual
