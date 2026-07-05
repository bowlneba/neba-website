# Application Auditing (Audit.NET)

GitHub issue: [#74](https://github.com/bowlneba/neba-website/issues/74)

Renamed from `issue-74-audit-net.md` once Phases 1–3 shipped — this document now tracks the
feature's as-built state plus forward-looking follow-ups, rather than a pre-implementation plan.

## Decisions

| Question | Decision |
|---|---|
| Storage backend | Azure Table Storage (3 tables: `EFAuditEvents`, `SecurityAuditEvents`, `JobAuditEvents`) |
| EF audit mechanism | `Audit.EntityFramework.Core`'s `AuditSaveChangesInterceptor`, added via `.AddInterceptors(...)` alongside `SlowQueryInterceptor`/`QueryTagEnrichmentInterceptor`/`DomainEventDispatcherInterceptor` |
| Actor identity | `ICurrentUserService` (`src/Neba.Api/Identity/`), wraps `IHttpContextAccessor`, shared by the EF interceptor, API audit middleware, and Hangfire filter |
| PII scrubbing | Compliance taxonomy (`[PublicData]`/`[PersonalData]`/`[PrivateData]`) extended to `AttributeTargets.Property`, applied via `AuditPayloadScrubber` |
| Local dev storage | Azurite table storage via Aspire `storage.AddTables("tables")` |
| API audit mechanism | `Audit.WebApi`'s `UseAuditMiddleware(...)`, not a hand-rolled middleware (superseded an earlier custom `ApiAuditMiddleware` draft) |
| Security/identity events | Routed to their own table (`SecurityAuditEvents`) via `SecurityAuditDataProviderRouter`, independent of `EFAuditEvents` |
| Job audit mechanism | Official `Audit.Hangfire` package (`AddAuditJobExecutionFilter`), not a hand-rolled `IServerFilter` |
| Failure isolation | `ResilientAuditDataProvider` wraps the configured data provider so a Table Storage outage degrades to a logged warning instead of failing the caller |

Still open (per issue, non-blocking — track as follow-up issues, not part of this implementation):
- Retention policy (issue open question #2)
- Who can read audit records / admin UI (issue open question #3)
- Blazor frontend audit scope (issue open question #5) — out of scope; today's Blazor app is read-only and all writes route through the API, so API-layer audit covers it automatically
- Canonical audit event envelope vs. per-provider schemas (issue open question #7) — deferred; each layer writes its own native event shape to its own table

---

## Implemented

### Phase 1 — Infrastructure plumbing + EF Core audit (done)

- AppHost: `storage.AddTables("tables")`, referenced by the `api` project (`src/Neba.AppHost/AppHost.cs`)
- `ICurrentUserService` / `CurrentUserService` — `src/Neba.Api/Identity/`
- Compliance taxonomy widened to `AttributeTargets.Property`; `AuditPayloadScrubber` — `src/Neba.Api/Compliance/`
- `AddAuditing()` / `AuditingConfiguration` — `src/Neba.Api/Auditing/AuditingConfiguration.cs`
  - `ForContext<AppDbContext>` opt-in list: `Bowler`, `Season`, `Tournament`, `HallOfFameInduction`, `HighAverageAward`, `HighBlockAward`, `BowlerOfTheYearAward`, `BowlingCenter`, `Sponsor`
  - `ForContext<SecurityDbContext>` opt-in list: `ApplicationUser`, `IdentityUserRole<Ulid>`
- `AuditEnrichmentAction` — attaches `ActorId`/`CorrelationId` custom fields (`src/Neba.Api/Auditing/AuditEnrichmentAction.cs`)
- `SecurityAuditDataProviderRouter` — routes `SecurityDbContext` events to `SecurityAuditEvents`, everything else to `EFAuditEvents` (`src/Neba.Api/Auditing/SecurityAuditDataProviderRouter.cs`)
- `ResilientAuditDataProvider` — swallow-and-log wrapper around the resolved data provider (`src/Neba.Api/Auditing/ResilientAuditDataProvider.cs`)
- `AddAuditing()` runs before `AddDatabase()` in `InfrastructureConfiguration.AddInfrastructure()` (ordering constraint — `AddDatabase()` resolves `AuditSaveChangesInterceptor` from DI)
- Tests: `tests/Neba.Api.Tests/Compliance/AuditPayloadScrubberTests.cs`, `tests/Neba.Api.Tests/Identity/CurrentUserServiceTests.cs`, `tests/Neba.Api.Tests/Auditing/EfAuditIntegrationTests.cs`, `tests/Neba.Api.Tests/Auditing/AuditEnrichmentActionTests.cs`, `tests/Neba.Api.Tests/Auditing/SecurityAuditDataProviderRouterTests.cs`, `tests/Neba.Api.Tests/Auditing/ResilientAuditDataProviderTests.cs`

### Phase 2 — API command endpoint audit (done)

- Uses `Audit.WebApi`'s `UseAuditMiddleware(...)` directly (`AuditingConfiguration.UseApiAuditMiddleware()`), not a first-party middleware — supersedes the original plan's hand-rolled `ApiAuditMiddleware` sketch
- Excludes GET requests and `/health`, `/scalar`, `/background-jobs`, `/debug` path prefixes
- Request/response bodies scrubbed via `ApiAuditPayloadScrubbingAction` (`src/Neba.Api/Auditing/ApiAuditPayloadScrubbingAction.cs`), same `AuditPayloadScrubber` convention as EF payloads
- Events land in `EFAuditEvents` (no separate `ApiAuditEvents` table — `SecurityAuditDataProviderRouter` only special-cases `SecurityDbContext` events; API events fall through to the default provider)
- Tests: `tests/Neba.Api.Tests/Auditing/ApiAuditMiddlewareIntegrationTests.cs`, `tests/Neba.Api.Tests/Auditing/ApiAuditPayloadScrubbingActionTests.cs`

### Phase 3 — Hangfire background job audit (done)

- Official `Audit.Hangfire` package's `AddAuditJobExecutionFilter(...)` — `src/Neba.Api/BackgroundJobs/BackgroundJobConfiguration.cs`
- `ExcludeArguments()` set — outcome only (job id, type/method, success/failure, timing), never serialized job arguments
- Own table: `JobAuditEvents`, own `AzureTableDataProvider` instance (not routed through the global `Audit.Core.Configuration` provider)
- No custom filter attribute — the package's `IServerFilter` owns scope lifecycle including exception capture

### Phase 4 — Docs, hardening, cleanup (partially done)

Done:
- Failure isolation (`ResilientAuditDataProvider`) implemented and tested
- Security/identity audit trail isolated to its own table

Still outstanding:
- `docs/architecture/backend.md` has not yet been updated with the auditing guidelines (issue's 9 guidelines) — do this before considering the feature fully closed out
- Confirm managed identity / RBAC for the audit tables is write-only (no delete) in production — infra/RBAC change outside app code, track as a deployment checklist item
- Full regression pass across unit + integration suites

---

## Future Enhancements (not yet in scope — implement when these features land)

### SignalR audit

No SignalR hubs exist in this codebase yet. When one is introduced (e.g. live tournament/bracket updates), extend auditing to cover hub invocations that mutate state (client-to-server method calls), mirroring the API middleware's "commands only" filter — do not audit pure broadcast/notification pushes from server to client.

Brief plan:
- Add an `IHubFilter` (SignalR's equivalent of the FastEndpoints/`Audit.WebApi` middleware layer) that wraps `InvokeMethodAsync`, opens an `AuditScope` per invocation using the same `ICurrentUserService`/`AuditPayloadScrubber` conventions as the API middleware
- Event type convention: `Hub:{hubName}.{methodName}`
- New table: `HubAuditEvents` (own `AzureTableDataProvider`, same partition/row key scheme as the other tables) — keep it separate rather than folding into `EFAuditEvents`, consistent with the "own table per provider" pattern already used for jobs/security
- Scrub hub method arguments the same way `ApiAuditPayloadScrubbingAction` scrubs request bodies
- Register the filter via `.AddSignalR(options => options.AddFilter<AuditHubFilter>())` alongside wherever hubs get registered

### HTTP client call audit (Challonge bracket API)

No outbound HTTP client integrations exist yet. When the Challonge bracket API (or any other outbound third-party HTTP client) is introduced, audit outbound calls that mutate remote state (POST/PUT/PATCH/DELETE) — not read-only GETs, mirroring the inbound API middleware's GET-exclusion rule.

Brief plan:
- Add a `DelegatingHandler` (e.g. `AuditingHttpMessageHandler`) registered via `.AddHttpMessageHandler<T>()` on the `HttpClient` used for Challonge, following the same request/response capture + `AuditPayloadScrubber` scrubbing pattern as the inbound API middleware
- Event type convention: `HttpClient:{clientName}:{method}:{path}`
- New table: `OutboundHttpAuditEvents` (own `AzureTableDataProvider`)
- Capture request path, method, status code, elapsed time, and scrubbed request/response bodies; never capture API keys/auth headers — treat those as `[PrivateData]` equivalents and omit entirely rather than mask
- Failure isolation still applies — wrap with the same `ResilientAuditDataProvider` pattern so an audit write failure never fails the outbound call itself
