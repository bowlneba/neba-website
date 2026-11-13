# .NET 11 EF Core Plan

Part of the [.NET 11 upgrade](dotnet-11-upgrade.md), tracked in issue [#132](https://github.com/bowlneba/neba-website/issues/132).
This file holds everything EF Core: what to check in Phase 2, and what to adopt afterward. Add new EF ideas here, not in the upgrade plan.

Source: the .NET Conf 2026 session "EF Core 11" (2026-11-13). The talk used SQL Server and Cosmos for its demos, and we use PostgreSQL through Npgsql, so many items do not apply. Confirm each item against Microsoft's What's new in EF Core 11 and the EF Core 11 breaking changes page, and against the Npgsql 11 release notes, before relying on it.

## Current state

- Provider: PostgreSQL through `Npgsql.EntityFrameworkCore.PostgreSQL`, reached only through the Aspire Npgsql wrappers.
- Two contexts: `AppDbContext` and `SecurityDbContext`. `Neba.MigrationService` runs `MigrateAsync()` on both.
- Retry: `EnableRetryOnFailure(5, 10s)` in `DatabaseConfiguration.cs`.
- Complex properties are in use, including on owned entities. `OwnedNavigationComplexPropertyExtension.cs` works around an EF Core 10 snapshot generator gap.
- Design-time factories exist for both contexts (`AppDbContextDesignTimeFactory`, `SecurityDbContextDesignTimeFactory`).
- `.config/` holds only `dotnet-tools.json` (husky). No `dotnet-ef.json`.

## Part 1: Check in Phase 2 (the upgrade PR)

- [ ] **Owned-entity complex property workaround.** `OwnedNavigationComplexPropertyExtension.cs` exists because EF 10's snapshot generator emits a call that does not compile. Generate a scratch migration on EF 11. If the snapshot compiles without the extension, delete the extension in its own PR. If not, keep it.
- [ ] **Migration retry.** The talk says `Migrate` and `MigrateAsync` now use the configured retry execution strategy. Our `MigrateAsync()` calls in `Neba.MigrationService` then retry on transient errors. Check that a retried migration run leaves no partial state, and that migrations hold no code that breaks when it runs twice. Run the service against a throttled or restarted database once.
- [ ] **Package bumps.** Add `Microsoft.EntityFrameworkCore.Sqlite` (used in tests) to the Phase 2 bump list. Per the talk, the Sqlite provider 11 drops `netstandard2.0` and needs .NET 10 or later; we are fine on net11. SQL Server and Cosmos provider changes (SqlClient 7, `_jObject`, Newtonsoft to System.Text.Json) do not touch us.
- [ ] **Join pruning and SQL changes.** EF 11 removes joins and `ORDER BY` keys the query does not need. Diff the SQL for the heaviest queries (tournament, stats, bowler search) before and after, on the same data. Expect shorter SQL. Any query that gets slower or returns different rows is a bug to report.
- [ ] **`int.Parse` and other newly translated methods.** Queries that fell back to client evaluation or a workaround may now translate. Grep for `AsEnumerable()` and `ToList()` calls placed only to get past a translation gap. Do not change them in the upgrade PR.
- [ ] **New analyzer warnings.** EF 11 flags more raw SQL misuse (`FromSqlRaw`, `SqlQueryRaw`) and incorrect `IAsyncEnumerable` use. Fix real hits. Suppress false positives in `.editorconfig` with a reason.
- [ ] **`--no-build` for Package Manager Console and migration bundles with central package management.** We use central package management (`Directory.Packages.props`). If we ship a migration bundle, confirm it builds.
- [ ] **No-op column alters.** EF 11 skips `ALTER COLUMN` when only the CLR type changes and the database type stays the same. Confirm the scratch migration has no spurious alters. (This overlaps the empty-snapshot-diff check in the upgrade plan.)
- [ ] Items already in the upgrade plan: throw on no migrations, latest migration ID in the snapshot, `List<T>` backing field rule, SmartEnum and typed ID converters.

## Part 2: Adopt afterward (separate PRs)

Do:

- **`.config/dotnet-ef.json`**: set `project` and `startupProject` once so `dotnet ef migrations add` needs no flags. We have two contexts, so check how it handles `--context` (the talk showed `--context "*"` for all contexts; confirm). Low risk, do first.
- **`FullJoin`, `LeftJoin`, `RightJoin`, `MaxBy`, `MinBy` translation**: use where a query uses `GroupJoin` plus `DefaultIfEmpty` or `OrderBy...First`. Also in the Phase 3 list of the upgrade plan. Add a query test per change.
- **Unconstrained foreign keys (`IsConstraint(false)`)**: relevant to the Software (legacy) data sync. Where synced rows can arrive before their parents, a relationship that EF knows about but the database does not enforce may remove sync-order fragility. Evaluate only when a sync fails for that reason. We do not weaken constraints for convenience.
- **Indexes on complex properties**: where a complex property holds columns we filter on, declare the index in the model, not in raw SQL. Check Npgsql 11 support first.

Evaluate only:

- **JSON index and JSON complex types with inheritance (TPT/TPC)**: no current need. Revisit if a feature needs JSON columns. PostgreSQL JSON support depends on Npgsql, not on the SQL Server items in the talk.
- **Temporal property mapping**: SQL Server only. Skip.

Skip (SQL Server, Cosmos, or SQLite only):

- **Vector search** (`VectorSearch`, `WITH APPROXIMATE`, DiskANN) and the "not loaded by default" vector column change. We use PostgreSQL and the plan lists vector search as out of scope. If it returns, look at pgvector support in Npgsql, not this talk.
- **Full-text search** (`FreeTextTable`, `ContainsTable`): SQL Server only. PostgreSQL full-text search is a separate Npgsql feature.
- **`EF.Functions.JsonContains` / `JsonPathExists`, compatibility level 170**: SQL Server. Check Npgsql's own JSON containment support if we ever query `jsonb`.
- **Cosmos**: transactional batches, bulk execution, session tokens. We do not use Cosmos.
- **SQLite `half` type**: not used.

## Docs to update when Phase 2 lands

- [ ] `docs/learnings/backend.md`: any EF 11 gotchas, and whether the owned-entity extension is gone.
- [ ] `CLAUDE.md`: one-line rule for anything that bites twice.
- [ ] Delete rows here as each item is checked or decided.
