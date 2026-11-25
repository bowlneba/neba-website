# Correlation and valid constants

Read this in Step 3, before you write the factory.

## Assess correlation needs

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

## Assess valid constants

Define a `ValidXxx` constant for **every primitive-typed property** — this includes `string`, `int`, `decimal`, `bool`, `DateOnly`, `DateTimeOffset`, and any other value type. Use `public const` for compile-time constants (`string`, `int`, `decimal`, `bool`) and `public static readonly` for types that cannot be `const` (`DateOnly`, `DateTimeOffset`, SmartEnum/SmartFlagEnum members).

Properties that do **not** get constants:
- IDs — always use `SomeId.New()` or `Ulid.NewUlid()` inline
- Nullable optional properties — default to `null`; no constant needed
- Complex type properties that delegate to another factory's `Create()`
- Collection properties

If the type has no primitive scalar properties at all (only IDs, collections, or complex types), no constants are needed — say so in the response.
