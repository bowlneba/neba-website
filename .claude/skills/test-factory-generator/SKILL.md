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
- Use a closure `var rank = 1;` incremented in the `Select` lambda
- The primary ordered field (average, block score, points, wins) should decrease as rank increases using a step calculation, so rank 1 has the best value
- Avoid having the primary sort field generated purely randomly — it would contradict the rank

**Win/loss types** (have `Wins`/`Loses` or similar complementary fields):
- Derive one from the other: e.g., `var loses = currentRank - 1; var wins = totalGames - loses;`

**Hierarchical counts** (e.g., `Finals ≤ Tournaments ≤ Entries`):
- Build upward: `var finals = ...; var tournaments = finals + faker.Random.Int(...); var entries = tournaments + faker.Random.Int(...);`

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

### ID property conventions

`Create()` uses `SomeId.New()` or `Ulid.NewUlid()` — these are fine because `Create()` is for unit tests that don't snapshot IDs.

`Bogus()` must **always** use `Ulid.Bogus(faker)` or `Ulid.BogusString(faker)` from `UlidFakerExtension` — **never** `Ulid.NewUlid()`. The extension methods derive the ULID from the faker's seeded RNG, making them deterministic. `Ulid.NewUlid()` calls the system clock and bypasses the seed, which breaks Verify snapshot tests.

| Property type                     | `Create()` default                      | `Bogus()` value                           |
|-----------------------------------|-----------------------------------------|-------------------------------------------|
| `SomeId` (StronglyTypedId)        | `id ?? SomeId.New()`                    | `new SomeId(Ulid.BogusString(faker))`     |
| `Ulid` (raw, e.g. stored as Ulid) | `bowlerId?.Value ?? Ulid.NewUlid()`     | `Ulid.Bogus(faker)`                       |
| `BowlerId?` typed param           | `bowlerId ?? BowlerId.New()`            | `new BowlerId(Ulid.BogusString(faker))`   |

When a ViewModel/Response stores `BowlerId` as a raw `Ulid` (exposed as the `.Value` property), use the `bowlerId?.Value ?? Ulid.NewUlid()` pattern in `Create()` and `Ulid.Bogus(faker)` in `Bogus()`.

### Nested factory calls in Bogus()

There are two distinct patterns depending on WHERE the child factory is called. Using the wrong pattern causes all parent items to receive identical child collections.

#### Pattern A — Pre-computation (pool): derive a pool seed from the faker, then build the pool

When you need a unique pool of child objects distributed across parent items (e.g., each parent gets a distinct name from a pre-shuffled pool), derive a pool seed from the faker first so `UniquePool.Create` is deterministic, then call the child `Bogus()` with the same `faker` to share RNG state:

```csharp
// ✅ Correct — pool seed derived from faker; child factory shares same faker
var poolSeed = faker.Random.Int();
var namePool = UniquePool.Create(NameFactory.Bogus(count, faker), poolSeed);

return [.. Enumerable.Range(0, count).Select(_ => new Bowler { Name = namePool.GetNext(), ... })];
```

#### Pattern B — Inside the Select: pass faker directly to nested child factories

When each parent item needs its own independently-generated child collection, pass `faker` directly to the child factory's `Bogus(int count, Faker faker)` overload. All factories share the same RNG — each `faker.Random.*` call advances state, so children are unique per parent item while remaining fully deterministic:

```csharp
// ✅ Correct — shared faker flows through; children unique per parent item
return [.. Enumerable.Range(0, count).Select(_ => new Parent
{
    Results = ChildFactory.Bogus(faker.Random.Int(1, 10), faker),
    Champions = ChampionFactory.Bogus(faker.Random.Int(1, 4), faker),
})];

// ❌ Wrong — same seed passed every iteration → all parents get identical children
return [.. Enumerable.Range(0, count).Select(_ => new Parent
{
    Results = ChildFactory.Bogus(faker.Random.Int(1, 10), seed),
})];

// ❌ Wrong — no faker → children non-deterministic, breaks Verify snapshot tests
return [.. Enumerable.Range(0, count).Select(_ => new Parent
{
    Results = ChildFactory.Bogus(faker.Random.Int(1, 10)),
})];
```

Every factory exposes `Bogus(int count, Faker faker)` as `internal` (called only from other factories to share RNG state) and `Bogus(int count, int? seed = null)` as `public` (called from tests). The seed overload creates a seeded `Faker` and delegates to the primary — it never duplicates implementation:

```csharp
internal static IReadOnlyCollection<PointsRaceTournamentResponse> Bogus(int count, Faker faker)
{
    // ... implementation
}

public static IReadOnlyCollection<PointsRaceTournamentResponse> Bogus(int count, int? seed = null)
{
    var faker = new Faker();
    if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
    return Bogus(count, faker);
}
```

### Bogus() implementation — always use Enumerable.Range + Select with a shared Faker

**Always** implement `Bogus(int count, Faker faker)` using `Enumerable.Range(...).Select(_ => ...)` with the passed `faker` driving all RNG calls. The `Bogus(int count, int? seed = null)` overload is a thin wrapper that creates a seeded `Faker` and delegates to the primary — it never duplicates the construction logic.

**Never** use `Random.Shared`, `new Random(seed)`, or `Ulid.NewUlid()` inside `Bogus()` — these bypass Bogus's seeded RNG and break Verify snapshot tests.

```csharp
// ✅ Correct — faker is the engine; shared RNG state flows through all nested calls
internal static IReadOnlyCollection<MyType> Bogus(int count, Faker faker)
{
    ArgumentNullException.ThrowIfNull(faker);
    return [.. Enumerable.Range(0, count).Select(_ => new MyType
    {
        Id = new MyTypeId(Ulid.BogusString(faker)),
        Name = faker.Random.Words(2),
    })];
}

public static IReadOnlyCollection<MyType> Bogus(int count, int? seed = null)
{
    var faker = new Faker();
    if (seed.HasValue) faker.Random = new Randomizer(seed.Value);
    return Bogus(count, faker);
}

// ❌ Wrong — Random.Shared bypasses Bogus RNG entirely
return [.. Enumerable.Range(0, count).Select(_ => new MyType { Name = Random.Shared.Next().ToString() })];

// ❌ Wrong — new Random(seed) is .NET RNG, not Bogus
var rng = seed.HasValue ? new Random(seed.Value) : new Random();
return [.. Enumerable.Range(0, count).Select(_ => new MyType { ... })];
```

### Bogus value conventions by type

| C# type             | Bogus expression                                              |
|---------------------|---------------------------------------------------------------|
| `string` (name)     | `faker.Name.FullName()` or `faker.Person.FullName`           |
| `string` (label)    | `faker.Random.Words(2)`                                      |
| `string` (year label, e.g. "2025 Season") | `` $"{faker.Date.PastDateOnly(100).Year} Season" `` |
| `decimal` (average) | `faker.Random.Decimal(150, 250)` (domain range)              |
| `decimal` (money)   | `faker.Random.Decimal(0, 10000)`                             |
| `int` (count)       | `faker.Random.Int(0, 20)`                                    |
| `int` (year)        | `faker.Date.Past(50).Year`                                   |
| `bool`              | `faker.Random.Bool()`                                        |
| `DateOnly`          | `faker.Date.FutureDateOnly()` or `faker.Date.PastDateOnly(n)` |
| `DateTimeOffset`    | `faker.Date.PastOffset(n)`                                   |
| `Uri`               | `new Uri(faker.Internet.Avatar())`                           |
| `SmartEnum`         | `faker.PickRandom(SomeEnum.List)`                            |
| `SmartFlagEnum`     | `[.. faker.PickRandom(SomeEnum.List, faker.Random.Int(1, SomeEnum.List.Count))]` |
| `IReadOnlyCollection<string>` from enum names | `[.. faker.PickRandom(SomeEnum.List).Select(c => c.Name)]` |

### Create() default conventions

- **All primitives**: always reference a `ValidXxx` constant — never inline a magic literal. Covers `string`, `int`, `decimal`, `bool`, `DateOnly`, `DateTimeOffset`.
- **SmartEnum / SmartFlagEnum**: define a `public static readonly ValidXxx` constant using the most representative member (e.g. `NameSuffix.Jr`, `HallOfFameCategory.SuperiorPerformance`); reference it in `Create()`.
- **IDs**: always `SomeId.New()` inline — no constant.
- **Nullable optional properties**: default to `null` — no constant, no fallback value.
- **Complex types** (another domain type): call the corresponding factory's `Create()` — no constant.

### Primitive properties backed by domain types

When a primitive property (typically `string`) is the serialized form of a domain type — a SmartEnum's `.Name`, a StronglyTypedId's `.Value.ToString()`, or a value object primitive — **lift the `Create()` parameter to the domain type** so callers pass the real domain value rather than a magic primitive.

**Detection**: when a property's name matches a domain type that exists in the codebase (e.g., `string TournamentType` → `TournamentType` SmartEnum exists; `string TournamentId` → `TournamentId` StronglyTypedId exists).

| Scenario | Property type | Parameter type | `Create()` extraction | `Bogus()` value |
|----------|--------------|---------------|----------------------|-----------------|
| SmartEnum-backed string | `string TournamentType` | `TournamentType? tournamentType = null` | `(tournamentType ?? ValidTournamentType).Name` | `faker.PickRandom(TournamentType.List).Name` |
| StronglyTypedId-backed string | `string TournamentId` | `TournamentId? tournamentId = null` | `tournamentId?.Value.ToString() ?? TournamentId.New().Value.ToString()` | `Ulid.BogusString(faker)` |
| Value object-backed primitive | depends on shape | lift to value object type | extract the underlying primitive | generate the primitive directly |

**Constants**: define `public static readonly TournamentType ValidTournamentType = TournamentType.Singles;` using the domain type — no magic string constant.

In `Bogus()`, continue generating the primitive directly (SmartEnum `.List` pick, `Ulid.BogusString(faker)`) since `Bogus()` is seeded-random and domain type instantiation adds no test value there.

```csharp
// ✅ Correct — caller passes TournamentType.Singles, not "Singles"
public static BowlerTitle Create(
    TournamentId? tournamentId = null,
    TournamentType? tournamentType = null, ...)
    => new()
    {
        TournamentId = tournamentId?.Value.ToString() ?? TournamentId.New().Value.ToString(),
        TournamentType = (tournamentType ?? ValidTournamentType).Name,
        ...
    };

// ❌ Wrong — caller must pass the magic string "Singles"
public static BowlerTitle Create(string? tournamentType = null, ...)
    => new() { TournamentType = tournamentType ?? "Singles", ... };
```

## Step 5 — Write the file

Write the factory directly to `tests/Neba.TestFactory/<Area>/<TypeName>Factory.cs` using the Write tool. Do not ask for confirmation first. Do not create any other files (no README, no docs).

After writing, briefly note the file path and any correlation choices made.

## Notes

- If the type already has a factory, ask whether to update it or start fresh.
- If the type is abstract or an interface, ask what concrete implementations should be covered.
- Never use `null!` — the factory always provides valid defaults.
- Never manually instantiate domain entities (aggregates, child entities with `internal` factories) — those factories must call aggregate methods or the `internal` factory if in the same assembly. For viewmodels, DTOs, responses, and records with `init` setters, direct construction is fine.
- Do not add error handling or validation in the factory — factories produce valid data by construction.
