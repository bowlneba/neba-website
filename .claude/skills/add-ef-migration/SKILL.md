---
name: add-ef-migration
description: Create and review an EF Core migration in Neba.Api - schema change, data backfill, migration test, and the hotfix and Software-port rules. Use when a table, column, index or constraint changes, or when data must be moved or ported from the legacy Software tables. Not for query tuning or N+1 review (performance-review, dotnet-data:optimizing-ef-core-queries) or entity design (new-domain-entity). A hotfix cannot carry a migration.
---

# Add an EF migration

Read `docs/learnings/backend.md` (the EF navigation section) and the `dotnet-entity-framework-core` skill. The `efcore` agent owns this work; this is the procedure.

## Steps

1. **Change the model first.** Edit the entity and its configuration. Copy `src/Neba.Api/Database/Configurations/SponsorConfiguration.cs` (ids, alternate keys, `ToTable("...", AppDbContext.DefaultSchema)`); for an owned type copy `AddressConfiguration.cs`. A collection navigation never uses `= []` on an `IReadOnlyCollection<T>` property.
2. **Generate the migration.** From `src/Neba.Api`, with a `ConnectionStrings__bowlneba` value set (a dummy is enough; no database is needed):

   ```
   dotnet ef migrations add <Name> --context AppDbContext --output-dir Database/Migrations
   ```

   The security context uses `--context SecurityDbContext --output-dir Database/Migrations/Security`. This `add` form is inferred from the file layout and `migrations list` was verified; check where the files land. The global `dotnet ef` tool is 11.x and the EF packages are 10.x, so stop and report a version warning or odd output.
3. **Review the generated code line by line.** Look for a rename emitted as drop plus add, a column drop or type narrowing, and a new required column on a populated table (it needs a default or a backfill). Never hand-edit the model snapshot, and never edit a migration that has shipped. Add a new one.
4. **Backfill data in the same migration** with `migrationBuilder.Sql(...)`, after the schema change it depends on, and safe to run twice (guard with `WHERE`). Quote identifiers for PostgreSQL (snake case).
5. **Test a data change.** Copy `tests/Neba.Api.Tests/Database/Migrations/AddArticleAnnouncedAtMigrationTests.cs`: it migrates to the previous migration, inserts rows, applies the new one, and asserts the result. Add a configuration test beside `tests/Neba.Api.Tests/Database/Configurations/`.
6. **Run** `dotnet build` and `dotnet test --filter-trait "Component=<Feature>"` (integration tests need Docker).

## Rules

- **Hotfixes never run migrations.** A change that needs one cannot ship through `hotfix.yml` (`docs/learnings/hotfix.md`).
- `Neba.MigrationService` applies migrations (`app` or `security`); do not apply them from the API.
- **Porting a Software table:** read the website's own copy afterwards, never the legacy database, and move the jobs that still query that legacy table (stats, awards) onto the website tables in the same change. Ports live in `data-migration/*.linq`. Run the membership port or sync before the stats job. See "Software data migration" in `CLAUDE.md`.
- A removed or renamed column may still be read by a `/legacy` sync. Search `src/Neba.Api/Legacy` before you drop it.

## Checklist

- [ ] Configuration matches the copied pattern
- [ ] Migration reviewed for data loss
- [ ] Backfill is re-runnable and has a migration test
- [ ] Snapshot untouched by hand
- [ ] No hotfix depends on it
- [ ] Software port moves its dependent jobs
