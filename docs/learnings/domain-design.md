# Domain design learnings

Moved from CLAUDE.md. Read before adding a child entity, an assign method, or an invariant that needs another aggregate's data.

### Always-Valid Entities and Aggregate Assignment

Child entities owned by an aggregate use `internal static ErrorOr<T> Create(...)` factory methods that validate the entity's own structural invariants. The `internal` modifier restricts construction to the same assembly (`Neba.Api`); by convention, only the owning aggregate root calls these factories — never handler or test code directly.

The aggregate root's assign methods take raw properties, call the internal factory, enforce aggregate-level invariants (e.g., `Complete == true`), and return a single `ErrorOr<Success>` to the caller:

```csharp
// Child entity owns its own invariants — internal so only Season can construct it
internal static ErrorOr<HighBlockAward> Create(BowlerId bowlerId, int blockScore)
{
    if (blockScore <= 0)
        return Error.Validation("HighBlockAward.BlockScore", "Block score must be greater than zero.");
    return new HighBlockAward { Id = SeasonAwardId.New(), BowlerId = bowlerId, BlockScore = blockScore };
}

// Aggregate enforces its own invariant and delegates entity validation to the entity
public ErrorOr<Success> AssignHighBlockAward(BowlerId bowlerId, int blockScore)
{
    if (!Complete)
        return Error.Conflict("Season.NotComplete", "Awards may only be assigned to a completed season.");
    var award = HighBlockAward.Create(bowlerId, blockScore);
    if (award.IsError) return award.Errors;
    _highBlockAwards.Add(award.Value);
    return Result.Success;
}
```

**Why this matters**: If entity validation lived on the aggregate, the aggregate would absorb invariants that have nothing to do with it. If `Create()` were public, the entity could be constructed in an invalid state outside the aggregate. The internal factory gives call-site simplicity (single `ErrorOr` chain) while keeping each invariant owned by the right type.

### Aggregate Invariants Requiring Cross-Aggregate Data

When an assign method's invariant depends on data owned by another aggregate, the handler queries that data and passes it as a parameter. The aggregate enforces the rule; the handler provides the facts.

**The deciding factor — persist on aggregate vs. pass as parameter**:

- **Live data owned by another aggregate** → pass as a parameter. The other aggregate remains the single source of truth. Duplicating it creates redundancy. Example: `statEligibleTournamentCount` for `AssignHighAverageWinner` — tournaments own this fact, not Season.
- **Per-instance formula coefficients** → persist on the aggregate, set at a lifecycle transition. The formula belongs in the domain; the coefficient may legitimately vary per instance and must be frozen with the aggregate's closed state. Example: `_minimumGamesMultiplier` is set at `Season.Close()` because an abbreviated season might use a different threshold than a regular season.

```csharp
// Application layer provides the cross-aggregate fact; aggregate enforces the rule
public ErrorOr<Success> AssignHighAverageWinner(
    BowlerId bowlerId, decimal average, int games, int? tournamentsParticipated,
    int statEligibleTournamentCount)
{
    if (!Complete)
        return SeasonErrors.SeasonNotComplete;

    var minimumGames = ComputeMinimumGames(statEligibleTournamentCount);
    if (games < minimumGames)
        return SeasonErrors.InsufficientGames(games, minimumGames);

    var award = HighAverageAward.Create(bowlerId, average, games, tournamentsParticipated);
    if (award.IsError) return award.Errors;
    _highAverageAwards.Add(award.Value);
    return Result.Success;
}

// Formula is domain logic — lives on the aggregate, not the handler
private int ComputeMinimumGames(int statEligibleTournaments) =>
    (int)Math.Floor(_minimumGamesMultiplier * statEligibleTournaments);
```

The handler orchestrates — queries the cross-aggregate fact once, then drives the aggregate:

```csharp
var statEligibleCount = await appDbContext.Tournaments
    .CountAsync(t => t.SeasonId == command.SeasonId && t.StatEligible, ct);
season.AssignHighAverageWinner(command.BowlerId, command.Average, command.Games,
    command.TournamentsParticipated, statEligibleCount);
```

**Anti-pattern**: Computing a domain formula in the handler and passing the derived result (e.g., pre-computing `minimumGames` and passing it in). When the formula changes, the fix belongs in the domain — not scattered across handlers.
