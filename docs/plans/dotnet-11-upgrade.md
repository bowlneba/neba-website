# .NET 11 / EF Core 11 / C# 15 Upgrade Plan

Tracks issue [#132](https://github.com/bowlneba/neba-website/issues/132) (milestone v1.2.0, priority High).
Written 2026-11-10, the .NET 11 GA date.

## Decisions

| Topic | Decision |
| --- | --- |
| Policy | NEBA tracks every annual .NET release. The upgrade happens regardless of which new features we adopt. |
| Support | .NET 11 is STS: 24 months, through November 2028. |
| Target release | **v1.2.0** (issue milestone). If packages hold the upgrade up, it moves to a later release: a 1.2.x or 1.3.0. See "Release targeting". |
| Timing | Prep now on the `dotnet11` branch. **Merge to `main` only after .NET 11.0.1** (first patch) and after every package check below passes. The 1.2.0 release does not wait for the upgrade; the upgrade waits for readiness. |
| Sequencing | Two phases. Phase 1 moves the runtime and everything that has an 11 release. Phase 2 moves EF Core and Npgsql once the Npgsql provider ships. |
| C# 15 | Separate follow-up PRs after the upgrade lands. The upgrade PR changes only what it needs to build and pass. |
| Out of scope | Preview or experimental features (unsafe memory-safety preview, vector search, DBSC), and non-upgrade refactors. |

## Release targeting

The plan is to ship with **v1.2.0**. Each phase can land in a different release, so a late package never forces a choice between holding 1.2.0 and shipping a half-checked upgrade.

| Situation at the 1.2.0 cut | What ships in 1.2.0 | What moves |
| --- | --- | --- |
| 11.0.1 out, Phase 1 and Phase 2 packages all Ready | Phase 1 and Phase 2 | Nothing |
| 11.0.1 out, Phase 1 Ready, Npgsql EF 11 not Ready | Phase 1 only (net11.0, EF Core 10) | Phase 2 to the next release (1.2.x or 1.3.0) |
| Phase 1 has a package that is not Ready and cannot be held on its current version | Nothing from this plan | Phase 1 and 2 to a later release (1.2.x or 1.3.0) |
| 11.0.1 not out yet | Nothing from this plan | Same as above |

Rules:

- **Set a decision date.** Choose a cutoff before the 1.2.0 release branch is cut (`release/v1.2.0`). On that date, record in #132 which row above applies. Do not decide at the last minute.
- **Keep the upgrade out of the release branch unless it is merged and green.** Never cherry-pick a partial upgrade into `release/v1.2.0`.
- **Version bumps follow the rules in `CLAUDE.md`.** A PR to `release/vX.Y.Z` sets `<Version>` to `X.Y.Z`. A PR to `main` must raise it above `main`'s version. `.github/scripts/check-version-bump.sh` enforces both. If the upgrade slips, its PR takes the next version number (1.2.x or 1.3.0).
- **Pick 1.2.x or 1.3.0 by risk, at the time.** A runtime and TFM change with no feature change fits a patch number if you treat it as maintenance. If Phase 2 changes SQL or the migration snapshot, or if Phase 3 changes behavior, use 1.3.0. This is your call when it happens.
- **Update the issue milestone.** If the target moves, change the milestone on #132 from v1.2.0 and note why.
- **Hotfixes stay on net10 until the net11 release is live.** A hotfix cuts from the latest release tag, so a net10 tag gives a net10 hotfix. After the net11 release ships, hotfixes cut from the new tag and use net11.

## Package availability (read first)

**Not every package has an 11 release on GA day.** Check NuGet before each step. Do not assume.

- **Npgsql EF Core provider** (`Npgsql.EntityFrameworkCore.PostgreSQL`) is the main risk. EF Core 11 needs a matching 11.x provider. We reference it only through `Aspire.Azure.Npgsql.EntityFrameworkCore.PostgreSQL` (`Neba.Api`, `Neba.MigrationService`), so the Aspire wrapper must also pick up the new provider.
- Until the provider ships, **EF Core 10 and Npgsql 10 keep working on net11.0**. That is why phase 1 leaves them alone.
- Packages that track the runtime major (`Microsoft.AspNetCore.*`, `Microsoft.Extensions.*`, `Microsoft.EntityFrameworkCore.*`) show `10.x` in `Directory.Packages.props` today. Their 11 builds should exist at GA but confirm.
- Third-party packages (list below) may lag. A package built for net10 usually runs on net11, so a lagging package blocks only its own bump, not the TFM change.

### Go/no-go check before each step

Run before phase 1 and again before phase 2:

```bash
dotnet list package --outdated --include-prerelease
dotnet list package --deprecated
dotnet list package --vulnerable --include-transitive
```

For each package in the table, record the version found and the date in the "Checked" column. A step starts only when its row says **Ready**.

| Package group | Current | Needed for | Checked |
| --- | --- | --- | --- |
| .NET 11 SDK (11.0.1 or later) | n/a | Phase 1 | |
| `Microsoft.AspNetCore.*`, `Microsoft.Extensions.*` | 10.0.14 / 10.11.0 | Phase 1 | |
| `Aspire.*` (AppHost SDK + integrations, `Aspire.Hosting.Testing`) | 13.6.4 | Phase 1 (confirm Aspire's .NET 11 support) | |
| `Npgsql.EntityFrameworkCore.PostgreSQL` (via Aspire wrapper) | 10.x (transitive) | **Phase 2 (blocker)** | |
| `Microsoft.EntityFrameworkCore.*` (incl. `.Sqlite`, `.Design`, `.Relational`) | 10.0.14 | Phase 2 | |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.14 | Phase 2 (tied to EF) | |
| `EntityFrameworkCore.Exceptions.PostgreSQL` | 10.0.1 | Phase 2 | |
| `Ardalis.SmartEnum.EFCore` | 8.2.0 | Phase 2 | |
| `Microsoft.OpenApi` | pinned `[2.13.0,3.0.0)` | Phase 1 (see OpenAPI below) | |
| `Scalar.AspNetCore` | 2.19.0 | Phase 1 | |
| `FastEndpoints`, `.Swagger`, `.AspVersioning` | 8.3.0 | Phase 1 | |
| `Hangfire.*` (`AspNetCore`, `PostgreSql`, `InMemory`, `Console`), `Audit.Hangfire`, `AspNetCore.HealthChecks.Hangfire` | 1.8.25 / 1.21.1 / 1.0.0 / 1.4.3 / 33.0.0 / 9.0.0 | Phase 1 (Hangfire.PostgreSql also touches phase 2 because it uses Npgsql) | |
| `ZiggyCreatures.FusionCache.*` | 2.9.0 | Phase 1 | |
| `Community.Microsoft.Extensions.Caching.PostgreSql` | 6.0.2 | Phase 2 (uses Npgsql) | |
| `Refit.*` | 16.4.0 | Phase 1 | |
| `Testcontainers.*` | 4.16.0 | Phase 1 | |
| `xunit.v3`, `xunit.runner.visualstudio` | 4.0.3 / 4.0.1 | Phase 1 (MTP runner stays) | |
| `Moq`, `Shouldly`, `Bogus` | 4.21.0 / 4.3.0 / 35.6.5 | Phase 1 | |
| Analyzers: `SonarAnalyzer.CSharp`, `Roslynator.Analyzers` | 10.35 / 5.0.1 | Phase 1 (new C# 15 syntax can break old analyzers) | |
| `Humanizer.Core`, `ErrorOr`, `FluentValidation` | 3.0.10 / 2.1.1 / n/a | Phase 1 | |

If a Phase 1 row is not ready, either hold that one package on its current version (if it still runs on net11) or delay the merge. Record which in the PR.

## Phase 0: Preflight (now, before touching code)

- [ ] Confirm 11.0.1 status: `dotnet --list-sdks`, then check the .NET release notes. Merge waits for it.
- [ ] Read the breaking-changes lists linked at the bottom. Walk the table in "Breaking changes that touch NEBA".
- [ ] Fill in the package table above.
- [ ] Hosting is **Azure Container Apps, deployed through Aspire** (ADR-0001). The issue's "App Service runtime/hosting bundle" item does not apply; there is no host runtime to upgrade. The .NET version ships inside our container images instead:
  - Confirm the `mcr.microsoft.com/dotnet/aspnet:11.0` base image exists. We have no Dockerfiles, so Aspire/.NET SDK container publish picks the base image from the TFM. Check the generated image after the TFM change.
  - Confirm the Aspire azd/publish flow (`azure.yaml`, `cd.yml`, `hotfix.yml`) works with the .NET 11 SDK on the runner.
  - Confirm the Azure Container Apps environment needs no change (it runs any image). Check the Aspire Container Apps hosting packages (`Aspire.Hosting.Azure.AppContainers`) support .NET 11.
- [ ] Confirm local tooling: Rider/VS version supports C# 15 and .NET 11; `dotnet-tools.json` tools (EF CLI, others) have 11 builds; the Aspire CLI and the Azurite/Docker images used by the AppHost still work.
- [ ] Confirm CI runner CPUs meet .NET 11 minimums (x86-64-v2). GitHub-hosted runners do. Check any self-hosted or container host.
- [ ] Take a baseline: full test run time, build warnings count, and a copy of the generated OpenAPI document (for diffing after the upgrade).
- [ ] Release version: bump `<Version>` in `Directory.Build.props` as `CLAUDE.md` requires. `<Version>` is already `1.2.0` on this branch. If the upgrade targets 1.2.0, merge into the 1.2.0 line; otherwise use the next version (see "Release targeting").
- [ ] Set the decision date for the 1.2.0 go/no-go and put it in #132.

## Phase 1: Runtime and everything except EF Core / Npgsql

**Hotfix note:** a hotfix branch cuts from the latest release tag, which is still net10. `hotfix.yml` and `cd.yml` hold copies of the deploy steps; change both when you edit the SDK version, and keep a hotfix deployable until the net11 release ships.

1. **SDK and TFM**
   - [ ] `Directory.Build.props`: `net10.0` → `net11.0`. Leave `LangVersion` as `latest`? The skill warns against `latest` (it varies by machine). Replace with unset (net11.0 defaults to C# 15) or `15`. Decide in the PR.
   - [ ] `global.json`: add an `sdk` block pinning 11.0.1 or later with `rollForward`. Today it has only the test runner setting.
   - [ ] CI: `dotnet-version: 10.0.x` → `11.0.x` in all 7 places: `ci.yml` (3), `pull_request.yml` (2), `sonarcloud.yml` (1), `hotfix.yml` (1).
   - [ ] `docs/architecture/backend.md:150` shows `net10.0` in an example. Update it.
2. **Runtime-aligned packages**: bump `Microsoft.AspNetCore.*` and `Microsoft.Extensions.*` to 11.x. **Do not bump `Microsoft.EntityFrameworkCore.*` or the Aspire Npgsql EF wrapper.**
3. **Aspire**: bump the AppHost SDK (`Neba.AppHost.csproj` line 1, `Aspire.AppHost.Sdk/13.6.4`) and the `Aspire.*` package versions to the release that supports .NET 11. The CI Aspire CLI version derives from that SDK line, so do not hardcode it.
4. **Third-party packages**: bump each row marked Phase 1 that has a compatible release.
5. **Fix build errors and new warnings.** New analyzer warnings from the SDK: fix or suppress with a reason in `.editorconfig`.
6. **Run everything**: unit, integration, Playwright e2e (`npm run test:e2e`). Run `aspire run` and click through the site.
7. **Compare the OpenAPI document** to the baseline (see OpenAPI below).
8. **Deploy to a non-production environment** if one exists, before merge.

Exit criteria: all tests green on net11.0, CI green, OpenAPI diff reviewed, 11.0.1 out.

## Phase 2: EF Core 11 and Npgsql (blocked until the provider ships)

**Do not start until the Npgsql EF Core 11 provider and its Aspire wrapper both show Ready in the table.** If the provider is not out when phase 1 merges, phase 1 ships on EF Core 10 and this phase waits. Recheck weekly.

- [ ] Bump `Microsoft.EntityFrameworkCore.*`, the Aspire Npgsql wrappers, `EntityFrameworkCore.Exceptions.PostgreSQL`, `Ardalis.SmartEnum.EFCore`, Identity EF, `Community.Microsoft.Extensions.Caching.PostgreSql`, and `Hangfire.PostgreSql` if it needs a matching Npgsql.
- [ ] Read the EF Core 11 breaking-changes page for provider-agnostic changes. Known from the newsletter: EF now **throws when no migrations are found** by default. Check `Neba.MigrationService` and any test that builds a context with no migrations.
- [ ] **Migrations**: run `dotnet ef migrations add` on a scratch branch and confirm the snapshot diff is empty or understood. EF 11 also writes the latest migration ID into the model snapshot, so two branches that each add a migration will now conflict in git. That is intended; mention it in the PR.
- [ ] **Validate migrations against PostgreSQL Flexible Server**, not only Testcontainers (issue #132 scope).
- [ ] Review SQL changes: EF 11 drops needless to-one joins and `ORDER BY` keys. Run the integration tests and spot-check the heaviest queries (tournament queries, stats).
- [ ] Re-test the EF collection navigation rule from `CLAUDE.md` (`List<T>` backing fields) and the `ICollection<T>.Add` regression tests.
- [ ] Re-test SmartEnum value conversions and strongly typed ID converters.
- [ ] Check `.config/dotnet-ef.json` (new in EF 11) as a convenience: it would remove the `--project` and `--startup-project` flags. Optional.

Exit criteria: migrations unchanged or reviewed, integration tests green, a staging migration run clean.

## Breaking changes that touch NEBA

From the Anton Dev Tips newsletter (2026-11-10) and the C# skill. Confirm each against Microsoft's lists before relying on it.

| Change | Why it matters here | Action |
| --- | --- | --- |
| **OpenAPI 3.2 is the default**; Microsoft.OpenApi 3.x has breaking API changes | `Microsoft.OpenApi` is hard-pinned to `[2.13.0,3.0.0)` because the XML comment generator breaks on 3.x (see the comment in `Directory.Packages.props`). `Scalar.AspNetCore` and `FastEndpoints.Swagger` read the document. | Test whether the pin still holds on net11. Check that Scalar and our own consumers handle 3.2. If a tool fails, set `options.OpenApiVersion = OpenApi3_1`. Re-check whether the pin can move to 3.x. |
| `BackgroundService` failure now makes `IHost.RunAsync`/`StopAsync` throw | Hangfire hosting and any hosted services. | Grep `BackgroundService` and `IHostedService`. Confirm shutdown paths in tests. |
| EF throws when no migrations found | `Neba.MigrationService` and test setups. | Phase 2 check. |
| CSRF now uses Fetch Metadata headers, on by default | Blazor Server forms and login pages. | Smoke-test login, logout, and every form post. Check any antiforgery code. |
| Async options validation throws if read synchronously | Only matters if we adopt async validators. | None now. |
| Float/double to small integer casts saturate instead of wrapping | Stats and scoring math. | Grep numeric casts in stats code. Run the stats snapshot tests. |
| ZIP/TAR readers validate checksums | Any archive handling (data migration, uploads). | Grep `ZipArchive`. |
| New minimum CPU (x86-64-v2) | Dev machines, CI, container hosts. | Covered in Phase 0. |
| `with(`, `union`, `closed`, `safe` are now contextual keywords | A method or type with those names breaks. | Grep for identifiers named `union`, `closed`, `safe`, or a method named `with`. Fix with `@`. |
| `Span<T>` collection expressions got a narrower scope | Loop-local span assigned to an outer span now errors. | Fix as the compiler reports. |
| `nameof(this.P)` inside attributes now errors | Rare. | Fix as reported. |

## Phase 3: C# 15 and .NET 11 adoption (separate PRs, after the upgrade merges)

Use the `csharp-15` skill (referenced in the issue comment of 2026-11-06) to review and make these changes. One small PR per item.

### Do

- **Collection expression arguments** `[with(...), ...]`: replace `new List<T>(n)` plus `AddRange`, and `new HashSet<T>(comparer) { ... }`. Never on arrays or spans. `with(...)` must be the first element.
  - Caution: `CLAUDE.md` requires `List<T>` backing fields for EF collection navigations and suppresses IDE0305/IDE0028 there. `[with(...)]` may let us remove some suppressions. Verify the `ICollection<T>.Add` regression tests still pass.
- **Labeled `break`/`continue`**: replace Boolean "found" flags and `goto` in nested loops. The IDE0410 analyzer finds them; its severity can be raised once the codebase is clean. Give every labeled loop in a method a distinct name.
- **Runtime async** (`runtime-async=on`): trial in a branch, compare test times and an API load test, then decide. Per the newsletter the runtime libraries already use it, so part of the gain is free. Opt-out is per project.
- **LINQ `LeftJoin`/`RightJoin`/`FullJoin`** with tuple results (and EF 11 translation of `FullJoin`, `MaxBy`, `MinBy`): use where a query currently uses `OrderBy...First` or manual joins. This is phase 2 dependent for EF queries.
- **Native OpenTelemetry tracing** in ASP.NET Core 11: check whether `OpenTelemetry.Instrumentation.AspNetCore` is still needed in `Neba.ServiceDefaults` (`AddSource("Microsoft.AspNetCore")`). Remove only if traces stay equivalent.
- **Zstandard response compression**: evaluate for the API.
- **`dotnet ef --context "*"` and `.config/dotnet-ef.json`**: optional dev convenience.

### Evaluate only (do not adopt without a decision)

- **Union types** (`union`): `ErrorOr<T>` stays our command result type (see `CLAUDE.md`). A union does not replace it. Candidates, if any: a closed set of unrelated types where we use marker interfaces today. Beware: value-type cases box; `default(union)` holds null, so switches need a `null` arm; prefer `null =>` over `_ =>`. System.Text.Json and OpenAPI support unions, but SmartEnum rules and the API serialization conventions in memory would apply. Write an ADR before using one.
- **`closed` hierarchies**: candidates are domain type families we own. Treat the subtype list as public API. Adding a subtype later breaks consumers' switches. We are vertical-slice, so check against the feature-boundary architecture test first.
- **Extension indexers**: low value here. They do not work in expression trees or `IQueryable` lambdas, and not on arrays or strings.
- **Async validation** (`AsyncValidationAttribute`): our rule is that validators handle structural checks only, with no DB lookups. This stays; do not adopt.
- **Blazor features** (SSR client validation, QuickGrid in SSR, virtualization with variable heights, circuit pause/resume, `SupplyParameterFromTempData`): review against `docs/learnings/blazor-ui.md`. The stats page just virtualized its modals (#240); variable-height virtualization may simplify it.
- **Unsafe memory-safety model**: skip. Preview only.
- **Vector and hybrid search, Cosmos, SQL Server items**: skip. Not relevant (PostgreSQL).

## Rollout and rollback

- Phase 1 and phase 2 each ship as their own PR, and can ship in the same release (1.2.0) or separate ones (see "Release targeting"). A bad phase 2 does not undo phase 1.
- Keep the last net10 release tag. A hotfix for production in the meantime cuts from that tag and deploys on net10 (see `docs/learnings/hotfix.md`).
- Hotfixes never run migrations. A phase 2 release with a model change ships by the normal release path only.
- Rollback = activate the previous Container Apps revision (revision mode per ADR-0001) or redeploy the previous release tag's image. No schema change is part of phase 1.
- Watch after deploy: container start time and memory (runtime changes can shift both; check the Container Apps scale and cost settings in ADR-0001), health checks, and App Insights for new exceptions.

## Docs and housekeeping to update in the upgrade PRs

- [ ] `CLAUDE.md` learnings (any new gotchas) and `docs/learnings/*.md`
- [ ] `docs/architecture/backend.md` (TFM example)
- [ ] Add an ADR if we adopt unions, `closed`, or runtime async
- [ ] Move completed items on issue #132 and close it when phase 2 ships
- [ ] Add a research-log comment to #132 summarizing this plan and linking the file

## Reference links

- Issue: https://github.com/bowlneba/neba-website/issues/132
- Newsletter: "NET 11 and C# 15 is out today!" Anton Dev Tips, 2026-11-10 (antondevtips.com/blog/new-features-in-dotnet-11-and-csharp-15)
- C# 15 skill: `csharp-15` (attached to the planning session; covers `with(...)`, unions, `closed`, extension indexers, labeled jumps, memory-safety preview)
- Microsoft: breaking changes in .NET 11, EF Core 11 breaking changes, What's new in .NET 11 / ASP.NET Core 11 / EF Core 11 / C# 15 / Aspire (learn.microsoft.com/dotnet)
- Issue comment 2026-08-25: Daniel Roth's ASP.NET Core 11 / Blazor session notes (async validation, SSR parity, virtualization, Aspire WASM)
