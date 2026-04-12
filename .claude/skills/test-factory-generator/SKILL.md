---
name: test-factory-generator
description: Generate a Neba.TestFactory class for a given C# class, record, DTO, viewmodel, or response type. Produces a Create() method with nullable parameters and valid constant defaults, and a Bogus() method using Bogus/Faker with appropriate correlation logic for ranked or related collections. Usage: /test-factory-generator <TypeName>
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

### Assess correlation needs

If the type has properties that must be logically consistent across a generated collection (a `Bogus()` call), determine the correlation strategy **before** writing code. Look at existing factories for precedent:

**Ranked leaderboard types** (have `Rank` property):
- Use a closure `var rank = 1;` incremented in `CustomInstantiator`
- The primary ordered field (average, block score, points, wins) should decrease as rank increases using a step calculation, so rank 1 has the best value
- Avoid having the primary sort field generated purely randomly — it would contradict the rank

**Win/loss types** (have `Wins`/`Loses` or similar complementary fields):
- Derive one from the other: e.g., `var loses = currentRank - 1; var wins = totalGames - loses;`

**Hierarchical counts** (e.g., `Finals ≤ Tournaments ≤ Entries`):
- Build upward: `var finals = ...; var tournaments = finals + f.Random.Int(...); var entries = tournaments + f.Random.Int(...);`

**Related IDs across a collection** (e.g., bowlers — each row must have a distinct `BowlerId`):
- Pass a `UniquePool<BowlerId>? bowlerIds = null` parameter to `Bogus()` so callers can inject correlated IDs
- Default to generating them inline when the parameter is omitted

**No meaningful correlation** (simple DTOs, responses with independent fields):
- Use random values within reasonable domain ranges

If correlation is needed and the strategy is not obvious from the type, **ask the user before generating**. For example:
- "This type has `Wins` and `Loses` — should they sum to a fixed total per row, or be independent?"
- "Should `HighBlock` decrease as rank increases?"

### Assess valid constants

Define a `ValidXxx` constant for **every primitive-typed property** — this includes `string`, `int`, `decimal`, `bool`, `DateOnly`, `DateTimeOffset`, and any other value type. Use `public const` for compile-time constants (`string`, `int`, `decimal`, `bool`) and `public static readonly` for types that cannot be `const` (`DateOnly`, `DateTimeOffset`, SmartEnum/SmartFlagEnum members).

Properties that do **not** get constants:
- IDs — always use `SomeId.New()` or `Ulid.NewUlid()` inline
- Nullable optional properties — default to `null`; no constant needed
- Complex type properties that delegate to another factory's `Create()`
- Collection properties

If the type has no primitive scalar properties at all (only IDs, collections, or complex types), no constants are needed — say so in the response.

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

    public static IReadOnlyCollection<<TypeName>> Bogus(int count, int? seed = null)
    {
        // Pre-compute correlation variables here if needed

        var faker = new Faker<<TypeName>>()
            .CustomInstantiator(f =>
            {
                // compute per-row correlation inside instantiator

                return new <TypeName>
                {
                    ...
                };
            });

        if (seed.HasValue)
        {
            faker.UseSeed(seed.Value);
        }

        return faker.Generate(count);
    }
}
```

### ID property conventions

`Create()` uses `SomeId.New()` or `Ulid.NewUlid()` — these are fine because `Create()` is for unit tests that don't snapshot IDs.

`Bogus()` must **always** use `Ulid.Bogus(f)` or `Ulid.BogusString(f)` from `UlidFakerExtension` — **never** `Ulid.NewUlid()`. The extension methods derive the ULID from the faker's seeded RNG, making them deterministic. `Ulid.NewUlid()` calls the system clock and bypasses the seed, which breaks Verify snapshot tests.

| Property type                     | `Create()` default                      | `Bogus()` value                       |
|-----------------------------------|-----------------------------------------|---------------------------------------|
| `SomeId` (StronglyTypedId)        | `id ?? SomeId.New()`                    | `new SomeId(Ulid.BogusString(f))`     |
| `Ulid` (raw, e.g. stored as Ulid) | `bowlerId?.Value ?? Ulid.NewUlid()`     | `Ulid.Bogus(f)`                       |
| `BowlerId?` typed param           | `bowlerId ?? BowlerId.New()`            | `new BowlerId(Ulid.BogusString(f))`   |

When a ViewModel/Response stores `BowlerId` as a raw `Ulid` (exposed as the `.Value` property), use the `bowlerId?.Value ?? Ulid.NewUlid()` pattern in `Create()` and `Ulid.Bogus(f)` in `Bogus()`.

### Bogus value conventions by type

| C# type             | Bogus expression                                 |
|---------------------|--------------------------------------------------|
| `string` (name)     | `f.Name.FullName()` or `f.Person.FullName`       |
| `string` (label)    | `f.Random.Words(2)`                              |
| `string` (year label, e.g. "2025 Season") | `` $"{f.Date.PastDateOnly(100).Year} Season" `` |
| `decimal` (average) | `f.Random.Decimal(150, 250)` (domain range)      |
| `decimal` (money)   | `f.Random.Decimal(0, 10000)`                     |
| `int` (count)       | `f.Random.Int(0, 20)`                            |
| `int` (year)        | `f.Date.Past(50).Year`                           |
| `bool`              | `f.Random.Bool()`                                |
| `DateOnly`          | `f.Date.FutureDateOnly()` or `f.Date.PastDateOnly(n)` |
| `DateTimeOffset`    | `f.Date.PastOffset(n)`                           |
| `Uri`               | `new Uri(f.Internet.Avatar())`                   |
| `SmartEnum`         | `f.PickRandom(SomeEnum.List)`                    |
| `SmartFlagEnum`     | `[.. f.PickRandom(SomeEnum.List, f.Random.Int(1, SomeEnum.List.Count))]` |
| `IReadOnlyCollection<string>` from enum names | `[.. f.PickRandom(SomeEnum.List).Select(c => c.Name)]` |

### Create() default conventions

- **All primitives**: always reference a `ValidXxx` constant — never inline a magic literal. Covers `string`, `int`, `decimal`, `bool`, `DateOnly`, `DateTimeOffset`.
- **SmartEnum / SmartFlagEnum**: define a `public static readonly ValidXxx` constant using the most representative member (e.g. `NameSuffix.Jr`, `HallOfFameCategory.SuperiorPerformance`); reference it in `Create()`.
- **IDs**: always `SomeId.New()` inline — no constant.
- **Nullable optional properties**: default to `null` — no constant, no fallback value.
- **Complex types** (another domain type): call the corresponding factory's `Create()` — no constant.

## Step 5 — Write the file

Write the factory directly to `tests/Neba.TestFactory/<Area>/<TypeName>Factory.cs` using the Write tool. Do not ask for confirmation first. Do not create any other files (no README, no docs).

After writing, briefly note the file path and any correlation choices made.

## Notes

- If the type already has a factory, ask whether to update it or start fresh.
- If the type is abstract or an interface, ask what concrete implementations should be covered.
- Never use `null!` — the factory always provides valid defaults.
- Never manually instantiate domain entities (aggregates, child entities with `internal` factories) — those factories must call aggregate methods or the `internal` factory if in the same assembly. For viewmodels, DTOs, responses, and records with `init` setters, direct construction is fine.
- Do not add error handling or validation in the factory — factories produce valid data by construction.
