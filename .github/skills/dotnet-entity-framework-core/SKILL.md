---
name: dotnet-entity-framework-core
version: "2.0.0"
category: "Data"
description: "Set up and review EF Core DbContext configuration, migrations, data migrations, and concurrency handling. For query shape, tracking, N+1, paging, and bulk-update performance, use the dotnet-data:optimizing-ef-core-queries skill instead."
compatibility: "Requires EF Core 7+."
---

# Entity Framework Core

Generic EF Core guidance. Repo-specific rules live in `CLAUDE.md` and `docs/learnings/backend.md`; where they disagree with this file, they win.

## Use this skill for

- `DbContext` registration and entity configuration
- Creating and reviewing migrations
- Data changes inside a migration
- Concurrency tokens and conflict handling

## Use something else for

- Slow queries, tracking, N+1, projections, paging, bulk updates, indexes: `dotnet-data:optimizing-ef-core-queries`.
- Docs: [EF Core](https://learn.microsoft.com/en-us/ef/core/), [Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/).

## Workflow

1. Keep `DbContext` scoped, one per unit of work.
2. Treat migrations as code that gets reviewed, not as throwaway output.
3. Check the generated SQL for anything you change.
4. Be deliberate about provider behavior. Providers share an API but not semantics.

## DbContext and configuration

Put each entity's mapping in an `IEntityTypeConfiguration<T>` class and load them together:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
    => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();
    }
}
```

Register with the provider package for your database (`UseNpgsql` for PostgreSQL). Enable `EnableSensitiveDataLogging` and `EnableDetailedErrors` in development only. `AddDbContextPool` helps when the context holds no per-request state.

Do not wrap `DbContext` in a generic repository. It hides query composition and adds nothing that `DbSet<T>` lacks.

## Migrations

```bash
dotnet ef migrations add <Name>
dotnet ef database update
dotnet ef migrations script --idempotent -o migrate.sql
```

Review each migration before you commit it:

- The `Up` and `Down` methods match what you intended. Read renames carefully: EF can emit a drop and add, which loses data.
- No column drop or type narrowing without a plan for existing rows.
- A new required column on a populated table has a default or a backfill.
- Indexes exist for columns you filter or sort on.
- Do not edit a migration that has shipped. Add a new one.

## Data migrations

Put data changes in `migrationBuilder.Sql(...)` in the same migration, after the schema change they depend on:

```csharp
migrationBuilder.AddColumn<string>("NormalizedName", "Products", nullable: true);
migrationBuilder.Sql("UPDATE \"Products\" SET \"NormalizedName\" = UPPER(\"Name\") WHERE \"NormalizedName\" IS NULL");
```

Make the SQL safe to read twice (guard with `WHERE`). Quote identifiers the way your provider needs. Keep large backfills out of the schema migration if they could lock the table for long.

## Concurrency

Add a concurrency token to entities that two users can edit at once:

```csharp
builder.Property(p => p.RowVersion).IsRowVersion();   // or .IsConcurrencyToken() on a version column

try { await db.SaveChangesAsync(ct); }
catch (DbUpdateConcurrencyException ex)
{
    var entry = ex.Entries.Single();
    var current = await entry.GetDatabaseValuesAsync(ct);   // null if the row was deleted
    // Decide: reload and retry, merge, or return a conflict to the caller.
}
```

How a row-version column maps differs by provider. Check the provider docs.

## Validate

- Generated SQL is what you expect.
- Migration is reviewable and loses no data.
- `DbContext` lifetime is scoped.
- Concurrency is handled for entities that need it.
