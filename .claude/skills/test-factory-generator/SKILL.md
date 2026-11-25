---
name: test-factory-generator
description: Generate a Neba.TestFactory class for a given C# class, record, DTO, viewmodel, or response type. Produces a Create() method with nullable parameters and valid constant defaults, and a Bogus() method using Bogus/Faker with appropriate correlation logic for ranked or related collections. Not for writing the tests (write-tests) or building domain entities (new-domain-entity). Usage: /test-factory-generator <TypeName>
---

The user wants to generate a test factory for a C# type in this project.

## Step 1 — Locate the type

Get the type name from the user's argument. If not provided, ask for it.

Search for the type definition:

```
grep -r "class <TypeName>\|record <TypeName>" src/ --include="*.cs" -l
```

Read the file and identify:
- All public properties and their types (especially which are nullable vs required)
- The namespace
- Which assembly the type lives in (Domain, Application, Api.Contracts, Website.Server, etc.)

## Step 2 — Determine the output location

The factory goes in `tests/Neba.TestFactory/` under a subfolder matching the domain area:

| Source assembly              | Factory subfolder         |
|------------------------------|---------------------------|
| `Neba.Domain.<Area>`         | `<Area>/`                 |
| `Neba.Application.<Area>`    | `<Area>/`                 |
| `Neba.Api.Contracts.<Area>`  | `<Area>/`                 |
| `Neba.Website.Server.<Area>` | `<Area>/`                 |

Namespace: `Neba.TestFactory.<Area>`
Class name: `<TypeName>Factory`

Check whether the subfolder already exists and whether a factory for this type already exists.

## Step 3 — Gather context before generating

**Correlation.** If properties must agree across a generated collection (rank and average, wins and loses, finals and entries, a distinct ID per row), pick the strategy before you write code, and ask the user when the type does not make it obvious. Rules and examples: `references/correlation-and-constants.md`.

**Valid constants.** Define a `ValidXxx` constant for every primitive-typed property (`const` where the compiler allows it, `static readonly` for `DateOnly`, `DateTimeOffset` and SmartEnum members). IDs, nullable optional properties, complex-type properties and collections get none. Details: same file.

## Step 4 — Generate the factory

### Factory structure

```csharp
using Bogus;

using <SourceTypeNamespace>;
// Add any other domain namespaces needed (e.g., Neba.Domain.Bowlers for BowlerId)

namespace Neba.TestFactory.<Area>;

public static class <TypeName>Factory
{
    // Valid constants for every primitive-typed property
    public const string ValidXxx = "...";          // string
    public const int ValidXxx = ...;               // int
    public const decimal ValidXxx = ...m;          // decimal
    public const bool ValidXxx = ...;              // bool
    public static readonly DateOnly ValidXxx = ...; // DateOnly (not const-able)
    public static readonly DateTimeOffset ValidXxx = ...; // DateTimeOffset
    public static readonly SomeEnum ValidXxx = SomeEnum.Member; // SmartEnum

    public static <TypeName> Create(
        <NullableParam1>? param1 = null,
        <NullableParam2>? param2 = null,
        ...)
        => new()
        {
            Prop1 = param1 ?? ValidProp1,      // or SomeId.New() for IDs
            Prop2 = param2 ?? OtherFactory.Create(),  // for complex types
            ...
        };

    internal static IReadOnlyCollection<<TypeName>> Bogus(int count, Faker faker)
    {
        ArgumentNullException.ThrowIfNull(faker);
        // Pre-compute correlation variables here if needed (e.g. UniquePool, rank counter)

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            // compute per-row correlation inside selector if needed

            return new <TypeName>
            {
                ...
            };
        })];
    }

    public static IReadOnlyCollection<<TypeName>> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
        return Bogus(count, faker);
    }
}
```

Before you write `Bogus()`, read `references/bogus-determinism.md`: ID conventions, nested child factories (pool versus inline), the `Enumerable.Range` shape, and the value table by type.

### Create() default conventions

- **All primitives**: always reference a `ValidXxx` constant — never inline a magic literal. Covers `string`, `int`, `decimal`, `bool`, `DateOnly`, `DateTimeOffset`.
- **SmartEnum / SmartFlagEnum**: define a `public static readonly ValidXxx` constant using the most representative member (e.g. `NameSuffix.Jr`, `HallOfFameCategory.SuperiorPerformance`); reference it in `Create()`.
- **IDs**: always `SomeId.New()` inline — no constant.
- **Nullable optional properties**: default to `null` — no constant, no fallback value.
- **Complex types** (another domain type): call the corresponding factory's `Create()` — no constant.

When a primitive property is the serialized form of a domain type (a SmartEnum's `.Name`, a typed ID's `.Value.ToString()`), lift the `Create()` parameter to the domain type. Rules and examples: `references/domain-backed-primitives.md`.

## Step 5 — Write the file

Write the factory directly to `tests/Neba.TestFactory/<Area>/<TypeName>Factory.cs` using the Write tool. Do not ask for confirmation first. Do not create any other files (no README, no docs).

After writing, briefly note the file path and any correlation choices made.

## Notes

- If the type already has a factory, ask whether to update it or start fresh.
- If the type is abstract or an interface, ask what concrete implementations should be covered.
- Never use `null!` — the factory always provides valid defaults.
- Never manually instantiate domain entities (aggregates, child entities with `internal` factories) — those factories must call aggregate methods or the `internal` factory if in the same assembly. For viewmodels, DTOs, responses, and records with `init` setters, direct construction is fine.
- Do not add error handling or validation in the factory — factories produce valid data by construction.

## Gotchas

- `Ulid.NewUlid()`, `Random.Shared` and `new Random(seed)` inside `Bogus()` ignore the seed and break Verify snapshots. Use `Ulid.Bogus(faker)`, `Ulid.BogusString(faker)` and `faker.Random.*`.
- Inside a `Select`, pass the shared `faker` to child factories. Passing a seed (or nothing) gives every parent the same children, or non-deterministic ones.
- `Create()` must build a persistable entity with no arguments. If it cannot, fix the default here.
- A primitive property named after a domain type (`string TournamentType`) takes the domain type as its `Create()` parameter, never a magic string.
- Never use `null!`. Never hand-build an aggregate or a child entity with an `internal` factory.
