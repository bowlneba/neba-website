# Primitive properties backed by domain types

Read this when a `string` or other primitive property holds a SmartEnum name, a strongly typed ID, or a value object.

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
