# ScoreCard

Adds `ScoreCard`/`SquadScore` — a bowler's game-by-game scores for a single Squad — as their own aggregate, independent of the `Tournament` aggregate. Covers qualifying scoring today; designed so a future Cashers round can reuse it without a rename. Match Play and Step Ladder are explicitly **not** covered — they're a different shape (head-to-head, not squad-scoped) and get their own model whenever they're built.

## Status

**`SquadScore` entity + persistence built; `ScoreCard` aggregate not yet built.** The 2026 qualifying-score data migration only needed `SquadScore` as a directly-persisted row (no in-memory aggregate loaded/saved around it), so that's what got built first: `SquadScoreId`, `SquadScore` (`src/Neba.Api/Features/Tournaments/Domain/`), `SquadScoreConfiguration`, and the `squad_scores` migration. `ScoreCard` — the aggregate that groups a bowler's `SquadScore` rows for a Squad and owns `RecordGame`/`RemoveGame` — is still design-only; nothing in the Domain Layer's `ScoreCard`/`ScoreCardErrors` code blocks below has been implemented, and neither has anything in Application + API Layers. Circle back to this plan when Score Card work actually starts (website-driven scoring, not just the data migration).

Two small deltas from the code blocks below, reflecting what actually got built:

- `SquadScore`'s pin-count property is named `Score`, not `Value` (both the C# property and the `squad_scores.score` column).
- `SquadScore.Create`/`UpdateValue` stayed `internal` per the Always-Valid Child Entity pattern even though `ScoreCard` doesn't exist yet — the data migration constructs rows through a handler in the same assembly, so nothing external needed public access.

This document remains the source of truth to build from for the rest of the plan — domain shape and methods for `ScoreCard` are worked out in detail; persistence and application/API layers for it are sketched but intentionally left loose until that work is actually picked up, same as `squad.md` did for its own deferred layers.

## Decisions locked in during scoping

- **No generic, all-formats `Scores` table.** Qualifying and a future Cashers round share one shape: bowler, squad, game number, score. Match Play and Step Ladder don't — they need an opponent/pairing and bracket state, which would mean nullable columns bolted onto every qualifying row for no reason. Build those on their own model when they're actually specified.
- **Retiring the scores→stats conversion entirely, not porting it.** nebamgmt-v3 aggregates `QualifyingScores` into a `QualifyingStats` row (`SquadId`, `Score` total, `Games` count, `HighGame`) at tournament completion, then the raw scores are gone. Every field on that stats row is a pure `SUM`/`COUNT`/`MAX` over the raw score rows — nothing there is information the raw rows don't already have. Going forward: raw `SquadScore` rows are the only persisted source; anything nebamgmt-v3 read off `QualifyingStats` becomes a query projection computed on demand. "Completing" a tournament becomes a pure status transition — no score-transformation side effect, and no deletion of raw scores, ever.
- **"Entries" and "TotalEntries" defined.** Confirmed against nebamgmt-v3's `GetBowlerSeasonStats`: `Entries` = count of squads a bowler bowled in *stat-eligible* tournaments; `TotalEntries` = count of squads bowled across *all* tournaments, eligible or not. Both are a `COUNT(DISTINCT SquadId)` over that bowler's `SquadScore` rows, filtered by tournament eligibility for `Entries`. This is a **different concept** from `statEligibleTournamentCount` already used by `Season.AssignHighAverageWinner` — that one counts tournaments, this counts squads. Don't conflate them when the season-stats report gets rebuilt.
- **`Score`/`ScoreCard` is its own aggregate root — not a child entity of `Tournament` or `Squad`.** Its invariants don't need the full Tournament graph, only two cross-aggregate facts supplied by the caller: whether the squad exists/is open for scoring, and the tournament format's expected game count. Nesting it under `Tournament` would mean loading the whole aggregate (season, sponsors, oil patterns, every squad) to record one game score — unnecessary weight for zero consistency benefit.
- **Aggregate grain is one `ScoreCard` per `(SquadId, BowlerId)`** — not one per row, not one per squad. Per-row leaves the "no duplicate game number" invariant to a bare DB constraint with no real aggregate behind it. Per-squad bundles every bowler in a squad into one consistency boundary and one write-lock for no domain reason (a `Season`/`Tournament`-style "God Aggregate" mistake) — and doesn't fit the real-time editing workload (see below), where one browser field blur should only ever touch one bowler's data. Per-bowler-per-squad is the smallest grain that still has an invariant worth enforcing (no duplicate game number for that bowler in that squad).
- **`ScoreCard` is not itself a persisted row.** It has no table and no ID of its own — its identity is the `(SquadId, BowlerId)` pair. This was reconsidered when DNF came up as a possible reason to give it a real row, but DNF turned out to belong to a future Registration/Check-in aggregate, not to scoring (see Deferred, below) — so there's currently no fact that needs a home on `ScoreCard` itself. If a genuine future need shows up (an event payload needing to reference a scorecard by ID, a concurrency token, etc.), that's the trigger to promote it to a real row — not before.
- **`SquadScore` carries `SquadId`/`BowlerId` directly**, not as a shadow FK reached through a parent navigation. `Squad`'s shadow FK to `Tournament` works because `Squad` is only ever reached by traversing `Tournament.Squads` — there's no equivalent parent here, since `ScoreCard` isn't an EF entity. `SquadScore` has to be self-sufficient for a command handler to query it directly by `(SquadId, BowlerId)`, and both columns are real foreign keys to `squads`/`bowlers` — see Persistence.
- **No repository layer.** This codebase is Vertical Slice Architecture, not Clean Architecture, despite the `ddd-clean-architecture` skill's name — command handlers query `AppDbContext` directly (`appDbContext.Set<SquadScore>()`), same as every other handler in `Features/Tournaments`. No `IScoreCardRepository`/`ISquadScoreRepository` abstraction gets introduced for this feature.
- **Table name: `squad_scores`.** Not `QualifyingScores` (round-specific — would force a rename the day Cashers reuses this shape). Not `ScoreCards` (nothing persisted under that name). Not `game_scores` (redundant, and misleading — the row is a score *on a squad*, not a standalone "game" concept; `squad_` is also what actually disambiguates this from a future match-play table, since match play scores are scoped by `MatchId`, not `SquadId`, and could never live in this table regardless of what it's called). The entity name `SquadScore` was chosen to match this table name directly, rather than the earlier working name `GameScore` — a bowler's score on one game of a squad is still fundamentally a squad score, and the type name should say so.
- **`SquadScore.Value` must be 0–300.** Owned by `SquadScore`'s own internal factory/update method — a structural, entity-owned invariant (Always-Valid Child Entity pattern), not merely a request-validator concern.
- **`GameNumber` may not exceed the tournament format's expected game count.** An aggregate-level rule on `ScoreCard`, using that expected count as a cross-aggregate fact passed in by the caller — same pattern as `Season.AssignHighAverageWinner`. This is a ceiling, not a requirement: a `ScoreCard` with 3 of 5 games recorded is a perfectly valid in-progress state. Nothing here enforces a *minimum* — that's tied up with DNF, which is out of scope (see Deferred).
- **DNF is explicitly out of scope for this plan.** It's a fact about a bowler's Registration/Check-in for a squad, not about their scores — no schema or domain impact on `ScoreCard`/`SquadScore`. Flagged in Deferred because it will matter later (standings sort order, gating a live squad-results view on "everyone who isn't DNF has finished this game"). Now designed in `registration.md`: DNF lives on `Registration`, gated on the entry fee being paid, and is independent of games bowled — a DNF may have zero `SquadScore` rows behind it. No change to the decision here, just confirmation.
- **Route shape for later: `/tournaments/{tournamentId}/squads/{squadId}/scores`.** Nested for discoverability — a presentation-layer concern, independent of the aggregate boundary (a write-consistency concern). The two don't have to match, and here they deliberately don't: the URL is nested three levels deep, the aggregate underneath is scoped to just `(SquadId, BowlerId)`. Route avoids "qualifying-scores" for the same reuse-by-Cashers reason as the table name.
- **Bulk writes are multiple aggregate instances in one transaction, not a coarser aggregate.** Two known bulk paths: the legacy WinForms bridge syncing a whole squad's scores in one call, and (maybe) an initial squad-wide entry screen. Both are handled by a command handler that loads/creates several `ScoreCard` instances and saves them together via one `SaveChangesAsync` — atomicity here is a unit-of-work concern, not a reason to model `ScoreCard` at squad granularity.
- **Legacy sync semantics: full replace per squad.** Matches nebamgmt-v3's own "delete all, recreate" behavior for that round. A bowler absent from the incoming payload has their `ScoreCard` removed entirely; a bowler present has their `SquadScore` collection replaced wholesale. Risk flagged, not solved here: once Registration/DNF exists, a legacy sync could stomp a fact the website-only side set (legacy has no concept of DNF) — needs a real answer when that handler is built, not before.
- **Real-time editing is the workload that confirmed the aggregate grain.** Planned UI: a spreadsheet-like score entry screen where each field blur sends one `(SquadId, BowlerId, GameNumber, Score)` change over SignalR. That's one small `ScoreCard` loaded, one `SquadScore` added/updated/removed, one save — cheap per keystroke. A coarser aggregate would mean every blur event loads and locks every bowler in the squad.

## Findings from nebamgmt-v3

Researched via `QualifyingScores`/`QualifyingStats`/`Stats` tables, `AddQualifyingStatsBO`, `CompleteTournament`, and `GetBowlerSeasonStats`:

- `QualifyingScores`: `Id` (identity int), `BowlerId`, `SquadId`, `Game`, `Score` — the row-per-game shape this plan carries forward, minus the surrogate-int PK (ULID here, per project convention).
- No persisted table exists anywhere in nebamgmt-v3 for Match Play or Step Ladder results — both are computed on the fly from qualifying data (`SquadCuts.cs`, `GetBowlersTopScore.cs`). Confirms there's no legacy shape to generalize from for those formats; they'll need their own design when built.
- `QualifyingStats` (`SquadId`, `Score` total, `Games` count, `HighGame`) is populated by a separate step (`AddQualifyingStatsBO`) after `CompleteTournament`, then read by `GetBowlerSeasonStats` for the season-stats report (averages, high game, high block, entries, field average) and by `BowlersRepository`'s earnings report (entry counts). Every one of those reads is a `Sum`/`Count`/`Max` over rows keyed by bowler+squad — nothing there survives as a reason to keep a separate stats table.
- The 2026 qualifying-score import data (`data-migration/qualifying-scores/*.json`) is shaped as `TournamentId → Squads[] → SquadScores[] → { BowlerId, Scores: [{ Game, Score }] }` — confirmed flat, game-per-bowler-per-squad, no team-level totals even for Doubles/Trios formats. Matches the `SquadScore` row shape directly; the import itself isn't designed here, but the source data is already the right shape for it.

## Domain Layer

### `SquadScoreId`

`src/Neba.Api/Features/Tournaments/Domain/SquadScoreId.cs`:

```csharp
using StronglyTypedIds;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Unique identifier for a squad score.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SquadScoreId;
```

### `SquadScore`

`src/Neba.Api/Features/Tournaments/Domain/SquadScore.cs`:

```csharp
using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// One bowler's score for a single game within a Squad. Always constructed and mutated through
/// the owning <see cref="ScoreCard"/> — never directly.
/// </summary>
public sealed class SquadScore
{
    /// <summary>
    /// Gets the unique identifier for this squad score.
    /// </summary>
    public required SquadScoreId Id { get; init; }

    /// <summary>
    /// Gets the Squad this game was bowled in.
    /// </summary>
    public required SquadId SquadId { get; init; }

    /// <summary>
    /// Gets the bowler who bowled this game.
    /// </summary>
    public required BowlerId BowlerId { get; init; }

    // EF-only navigations, needed for the real foreign keys configured in SquadScoreConfiguration.
    // Same pattern as HighBlockAward.Bowler — never referenced outside EF configuration.
    internal Squad Squad { get; init; } = null!;
    internal Bowler Bowler { get; init; } = null!;

    /// <summary>
    /// Gets the game number within the Squad (1-based).
    /// </summary>
    public short GameNumber { get; private set; }

    /// <summary>
    /// Gets the number of pins knocked down, 0-300 inclusive.
    /// </summary>
    public int Value { get; private set; }

    internal static ErrorOr<SquadScore> Create(SquadId squadId, BowlerId bowlerId, short gameNumber, int score)
    {
        var validated = ValidateScore(score);
        if (validated.IsError)
        {
            return validated.Errors;
        }

        return new SquadScore
        {
            Id = SquadScoreId.New(),
            SquadId = squadId,
            BowlerId = bowlerId,
            GameNumber = gameNumber,
            Value = score
        };
    }

    internal ErrorOr<Updated> UpdateValue(int score)
    {
        var validated = ValidateScore(score);
        if (validated.IsError)
        {
            return validated.Errors;
        }

        Value = score;
        return Result.Updated;
    }

    private static ErrorOr<Success> ValidateScore(int score)
        => score is < 0 or > 300
            ? SquadScoreErrors.InvalidValue(score)
            : Result.Success;
}
```

`src/Neba.Api/Features/Tournaments/Domain/SquadScoreErrors.cs`:

```csharp
using ErrorOr;

namespace Neba.Api.Features.Tournaments.Domain;

internal static class SquadScoreErrors
{
    public static Error InvalidValue(int score)
        => Error.Validation(
            code: "SquadScore.Value.Invalid",
            description: "A game score must be between 0 and 300.",
            metadata: new Dictionary<string, object> { { "Value", score } });
}
```

### `ScoreCard`

`src/Neba.Api/Features/Tournaments/Domain/ScoreCard.cs`:

```csharp
using ErrorOr;

using Neba.Api.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// A bowler's game-by-game scores for a single Squad. Aggregate root, but not itself persisted —
/// its identity is the (SquadId, BowlerId) pair; the only persisted rows are its <see cref="SquadScore"/> children.
/// </summary>
public sealed class ScoreCard
    : AggregateRoot
{
    /// <summary>
    /// Gets the Squad this Score Card belongs to.
    /// </summary>
    public required SquadId SquadId { get; init; }

    /// <summary>
    /// Gets the bowler this Score Card belongs to.
    /// </summary>
    public required BowlerId BowlerId { get; init; }

    private readonly List<SquadScore> _games = [];

    /// <summary>
    /// The recorded games for this bowler in this Squad.
    /// </summary>
    public IReadOnlyCollection<SquadScore> Games => _games;

    public static ScoreCard Create(SquadId squadId, BowlerId bowlerId)
        => new() { SquadId = squadId, BowlerId = bowlerId };

    /// <summary>
    /// Adds or corrects a single game's score. Upsert by design — callers (SignalR blur handler,
    /// legacy sync) never need to know whether the game already exists.
    /// </summary>
    public ErrorOr<Success> RecordGame(short gameNumber, int score, short expectedGameCount)
    {
        if (gameNumber < 1 || gameNumber > expectedGameCount)
        {
            return ScoreCardErrors.GameNumberOutOfRange(gameNumber, expectedGameCount);
        }

        var existing = _games.SingleOrDefault(game => game.GameNumber == gameNumber);
        if (existing is not null)
        {
            var updated = existing.UpdateValue(score);
            return updated.IsError ? updated.Errors : Result.Success;
        }

        var created = SquadScore.Create(SquadId, BowlerId, gameNumber, score);
        if (created.IsError)
        {
            return created.Errors;
        }

        _games.Add(created.Value);
        return Result.Success;
    }

    /// <summary>
    /// Clears a game's score. Idempotent — removing an already-absent game is not an error,
    /// matching the project's delete-idempotency convention.
    /// </summary>
    public ErrorOr<Deleted> RemoveGame(short gameNumber)
    {
        var existing = _games.SingleOrDefault(game => game.GameNumber == gameNumber);
        if (existing is not null)
        {
            _games.Remove(existing);
        }

        return Result.Deleted;
    }
}
```

`src/Neba.Api/Features/Tournaments/Domain/ScoreCardErrors.cs`:

```csharp
using ErrorOr;

namespace Neba.Api.Features.Tournaments.Domain;

internal static class ScoreCardErrors
{
    public static Error GameNumberOutOfRange(short gameNumber, short expectedGameCount)
        => Error.Validation(
            code: "ScoreCard.GameNumber.OutOfRange",
            description: $"Game {gameNumber} exceeds this squad's expected game count of {expectedGameCount}.",
            metadata: new Dictionary<string, object>
            {
                { "GameNumber", gameNumber },
                { "ExpectedGameCount", expectedGameCount }
            });
}
```

`SquadScore.Create`/`UpdateValue` are `internal` per the Always-Valid Child Entity pattern — only `ScoreCard` constructs or mutates them. `ScoreCard.Create` is `public`: it's the aggregate root, not nested under anything else.

## Persistence

**Table**: `squad_scores`, mapping `SquadScore` directly (there's no `ScoreCard` table to map). Both `SquadId` and `BowlerId` are real foreign keys, not just indexed columns — following the cross-aggregate-reference precedent `HighBlockAward.BowlerId`/`HallOfFameInduction.BowlerId` already use (real ULID value as the FK, `HasPrincipalKey` pointing at the target's ULID alternate key, since that's the value `SquadScore` actually holds — not the target's internal shadow-int PK):

```csharp
internal sealed class SquadScoreConfiguration : IEntityTypeConfiguration<SquadScore>
{
    public void Configure(EntityTypeBuilder<SquadScore> builder)
    {
        builder.ToTable("squad_scores", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(squadScore => squadScore.Id)
            .IsUlid();

        builder.HasAlternateKey(squadScore => squadScore.Id);

        builder.Property(squadScore => squadScore.SquadId)
            .IsUlid(SquadConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne(squadScore => squadScore.Squad)
            .WithMany()
            .HasForeignKey(squadScore => squadScore.SquadId)
            .HasPrincipalKey(squad => squad.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(squadScore => squadScore.BowlerId)
            .IsUlid(BowlerConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne(squadScore => squadScore.Bowler)
            .WithMany()
            .HasForeignKey(squadScore => squadScore.BowlerId)
            .HasPrincipalKey(bowler => bowler.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(squadScore => squadScore.GameNumber)
            .HasColumnName("game_number")
            .IsRequired();

        builder.Property(squadScore => squadScore.Value)
            .HasColumnName("value")
            .IsRequired();

        builder.HasAlternateKey(squadScore => new { squadScore.SquadId, squadScore.BowlerId, squadScore.GameNumber });
    }
}
```

**`SquadConfiguration` needs a small prerequisite addition**: it doesn't currently expose a `ForeignKeyName` constant (nothing has referenced `Squad` from outside `Tournament` until now). Add `internal const string ForeignKeyName = "squad_id";`, matching the constant `BowlerConfiguration` already has.

**No repository — `AppDbContext` gets a `SquadScores` `DbSet<SquadScore>`** (alongside `Sponsors`, `Bowlers`, etc.), queried directly from the command handler:

```csharp
var scoreCardRows = await appDbContext.SquadScores
    .Where(squadScore => squadScore.SquadId == command.SquadId && squadScore.BowlerId == command.BowlerId)
    .ToListAsync(cancellationToken);
```

with the handler reconstructing/mutating a `ScoreCard` in memory from those rows, then adding/updating/removing the underlying `SquadScore` tracked entities before `SaveChangesAsync` — no `IScoreCardRepository`/`ISquadScoreRepository` abstraction. Accessibility of the `DbSet` property (`public` vs. `internal`) follows whichever of the existing precedents (`Sponsors` vs. `HistoricalTournamentChampions`) fits once the consuming handlers are actually written.

**Still open for the implementation pass**:

- Where "expected game count" comes from for `ScoreCard.RecordGame`'s ceiling check — likely `TournamentType` or a new field on `Tournament`, not yet modeled. Needs an answer before the command handler can be built.

## Application + API Layers (deferred)

Intentionally not designed in detail yet — see Status. For when this is picked up:

- Real-time path: a `SetGameScoreCommand` (`SquadId`, `BowlerId`, `GameNumber`, `int? Score` — null meaning delete) driven by a SignalR hub, one call per field blur.
- Legacy bridge path: a `SyncSquadScoresCommand` (`SquadId`, per-bowler game dictionaries) doing the full-replace-per-squad diff described above.
- Route: `POST /tournaments/{tournamentId}/squads/{squadId}/scores` and friends, per the routing decision above. The tournament/squad existence-and-open-for-scoring check needs a lightweight lookup — `Squad` doesn't currently expose `TournamentId` as a real property (it's an EF shadow FK internal to `Tournament`'s configuration), so that check can't go through `Squad` as written today. Needs its own small read, not a full `Tournament` load.
- Read side: a query projecting `squad_scores` rows directly into whatever DTOs a results/standings page needs — no involvement from `ScoreCard`/`SquadScore` at all, same as every other query in this codebase bypasses the write-side aggregate.
- All of the above via `AppDbContext` directly in the handler, per the no-repository decision above — no application-layer repository interface gets defined for this feature.

## Ubiquitous Language

**Done**: `### Squad Score` has been added to `docs/ubiquitous-language.md`'s `## Tournaments` section (directly after `### Squad Max Entries`), reflecting what's actually built — see Status above. It's written standalone, with no reference to `ScoreCard` as an aggregate, since that type doesn't exist in code yet.

**Deferred**: the `### Score Card` entry below has not been added — add it (and update `### Squad Score`'s "In Code" section to describe it as a child entity of `ScoreCard`, per the original text underneath) once the `ScoreCard` aggregate itself is actually built:

```markdown
### Score Card

**Definition**: One bowler's game-by-game scores for a single Squad. A Score Card exists for exactly one (Squad, Bowler) pair. It is not itself a persisted record — its identity is that pair, and it's a grouping over that bowler's Squad Scores for the Squad, loaded and saved as a unit.

**Rules**:

- A Score Card holds at most one Squad Score per game number
- A game number may not exceed the Tournament format's expected game count for that Squad
- Squad Scores are added, corrected, and removed exclusively through the Score Card

**In Code**:

- Namespace: `Neba.Api.Features.Tournaments.Domain`
- Type: `ScoreCard` (aggregate root; not itself persisted)
- Property: `ScoreCard.Games` (`IReadOnlyCollection<SquadScore>`)
- Operations: `ScoreCard.RecordGame(...)`, `ScoreCard.RemoveGame(short)`

---
```

## Deferred / not in this plan

- **Cashers round** — expected to reuse this exact shape (squad-scoped, game-per-bowler), since a Cashers round is still just another Squad. No design changes anticipated, but not confirmed until it's actually built.
- **Match Play / Step Ladder scoring** — genuinely different shape (head-to-head, bracket state, opponent pairing). Gets its own aggregate/table when designed; explicitly not this plan's concern.
- **DNF** — belongs to `Registration` (see `registration.md`), not to `ScoreCard`. Confirmed there: DNF requires the entry fee to be paid, and is independent of games bowled — a DNF may have zero `SquadScore` rows behind it (the rare case: a withdrawal request the TD declines before the bowler ever throws a ball). Will eventually interact with scoring at the read side only: sorting DNF bowlers to the bottom of standings, and gating a live squad-results view (SSE) on "every non-DNF bowler has finished this game" before advancing the displayed game number. No schema or domain impact on this plan.
- **Forfeit** — belongs to the future `Entry` aggregate (see `registration.md`'s Deferred section), not to `ScoreCard`. Same read-side shape as DNF above: when an Entry forfeits (e.g., a doubles pair changes partners between squads), the raw `SquadScore` rows already bowled stay recorded — matching this plan's "raw scores are never deleted" rule — and are simply excluded from standings/advancement at query time. When the future standings/live-results query is designed, it needs to exclude on *both* signals (DNF and Forfeit), not just DNF.
- **Registration itself** — which bowlers are entered in a squad at all. `ScoreCard`/`SquadScore` don't check that a `BowlerId` is actually registered for the `SquadId` it's scoring against; that's `Registration`'s job (see `registration.md`, itself deferred to its own future implementation pass), not scoring's. `Registration`'s grain — `(SquadId, BowlerId)` — matches `ScoreCard`'s exactly, though they remain separate aggregates with separate tables.
- **The 2026 data import** — `data-migration/qualifying-scores/*.json` is confirmed to already be the right shape for `SquadScore`, but the actual import tooling/job isn't designed here.
- **Season-stats report rebuild** (`Entries`/`TotalEntries`/`HighGame`/`HighBlock`/`FieldAverage`/etc., porting `GetBowlerSeasonStats`) — confirmed these all become query projections over `squad_scores`, but the query itself isn't designed here.
