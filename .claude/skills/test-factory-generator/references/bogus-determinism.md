# Deterministic Bogus() factories

Read this before you write any `Bogus()` method. A call that skips the seeded `Faker` breaks Verify snapshot tests.

## ID property conventions

`Create()` uses `SomeId.New()` or `Ulid.NewUlid()` — these are fine because `Create()` is for unit tests that don't snapshot IDs.

`Bogus()` must **always** use `Ulid.Bogus(faker)` or `Ulid.BogusString(faker)` from `UlidFakerExtension` — **never** `Ulid.NewUlid()`. The extension methods derive the ULID from the faker's seeded RNG, making them deterministic. `Ulid.NewUlid()` calls the system clock and bypasses the seed, which breaks Verify snapshot tests.

| Property type                     | `Create()` default                      | `Bogus()` value                           |
|-----------------------------------|-----------------------------------------|-------------------------------------------|
| `SomeId` (StronglyTypedId)        | `id ?? SomeId.New()`                    | `new SomeId(Ulid.BogusString(faker))`     |
| `Ulid` (raw, e.g. stored as Ulid) | `bowlerId?.Value ?? Ulid.NewUlid()`     | `Ulid.Bogus(faker)`                       |
| `BowlerId?` typed param           | `bowlerId ?? BowlerId.New()`            | `new BowlerId(Ulid.BogusString(faker))`   |

When a ViewModel/Response stores `BowlerId` as a raw `Ulid` (exposed as the `.Value` property), use the `bowlerId?.Value ?? Ulid.NewUlid()` pattern in `Create()` and `Ulid.Bogus(faker)` in `Bogus()`.

## Nested factory calls in Bogus()

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

## Bogus() implementation — always use Enumerable.Range + Select with a shared Faker

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

## Bogus value conventions by type

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
