# Tournament Results

Adds `TournamentResult` — one bowler's place, prize money, and points earned in a tournament — as a child entity of the `Tournament` aggregate. Covers 2026-forward tournaments run entirely on this site, as full participants in the domain rather than historical facts. Pre-2026 data stays in the existing `HistoricalTournamentResult`/`tournament_results` (historical schema) table and is out of scope here.

## Status

**Domain, persistence, and the query-layer merge are built.** `Tournament.Complete`/`Tournament.CompleteTournament()`, `TournamentResult`/`TournamentResultId`/`TournamentResultErrors`, `Tournament.AddResult`, the `TournamentResultConfiguration` EF mapping, and the `20260818163034_TournamentResults_Init` migration are all implemented, along with the `GetTournamentQueryHandler` merge described in Query Layer below. **Deferred** — see Future Work: match play as a domain concept (and the background job that derives results from it), tournament-level base points and the placement-to-points curve, completion invariants beyond a bare flag flip, and edit capability. The Migration Backdoor sync described below is also built (see `docs/plans/software-backdoor-complete-tournament.md`).

## Decisions locked in during scoping

- **Child entity of `Tournament`, not a separate aggregate.** Same reasoning as `Squad`/`SquadScore`: a result's only invariant (one result per bowler per tournament) is checked against data `Tournament` already owns, and `AddResult` follows the `Season.AddHighBlockWinner` shape — a guard, an internal factory call, add to a backing list, single `ErrorOr` return.
- **Same four data columns as the legacy `ResultsStats` table — minus side cut and audit fields.** `BowlerId`, `Place`, prize money (payout), and points. No `SideCutId`/`SideCut` reference at all, and no created/modified audit columns (TPH audit fields from the legacy system don't apply to this table). **Scope correction**: `SideCutId` on the legacy table relates only to BOTY points calculation, not to `PrizeMoney` — NEBA changed how side-cut-earned BOTY points work, and `PrizeMoney` was never scoped by that column either way. Dropping `SideCutId` here doesn't remove any prize-money boundary, since there wasn't one; if side cuts are ever reintroduced as a points source, this table's rules get revisited then, not anticipated now.
- **A bowler gets a `TournamentResult` row based on a paid, non-refunded Registration for the tournament — full stop.** DNF status has no bearing on inclusion or on getting a real, ranked `Place`: a DNF bowler still bowled games and still gets a ranked `Place` (typically last, never omitted or treated as a null/placeholder case), even if the DNF squad was their only squad in the tournament. The `Points` base-for-entering value is likewise earned by any paid, non-refunded entry regardless of DNF. Population keys off Registration's payment/refund state, not games-played or the `DNF` flag.
- **No user input creates these records.** Two population paths, both machine-driven:
  1. **During migration**: a legacy backdoor endpoint (see Migration Backdoor section) queries nebamgmt-v3 directly and populates `TournamentResult` rows for tournaments already run.
  2. **Going forward**: completing a tournament triggers a background job that derives results from match play (a concept that doesn't exist in this system yet) and calls `Tournament.AddResult(...)` for each bowler. See Future Work — this plan does not design that job or match play itself, only the `AddResult` shape it will call.
- **No edit capability planned.** If a correction is ever needed, that's a future backlog item — deliberately not designed now, before there's a real case to shape it around.
- **`Tournament` gets a `Complete` flag.** Tournament has no lifecycle/status concept today. `AddResult` needs the same guard `Season.AddHighBlockWinner` uses (`if (!Complete) return ...NotComplete;`), so this plan introduces `Tournament.Complete` (bool, mirrors `Season.Complete` exactly — set `false` at `Create`, flipped by a new `Complete()` method) rather than a richer status enum. A fuller state machine (Draft/Published/InProgress/etc.) is not needed by anything in this plan; if a future feature needs more states, that's a separate migration.
- **`Complete()` carries no business-rule gate for now.** Today, a legacy backdoor endpoint is the only caller — it simply reports that nebamgmt-v3 has marked the tournament complete, and nebamgmt-v3 is already enforcing whatever rules govern when a tournament may be completed, so duplicating those rules here would be redundant. Once the legacy endpoint is retired, a UI-driven endpoint takes over as the caller, and that is when this codebase adds its own invariants for what "may be completed" means. `Complete()` itself stays a bare flag flip (guarded only against being called twice) until that future work defines what else belongs there.
- **`Place` ranks every entrant going forward — no nulls.** The legacy `ResultsStats`/`HistoricalTournamentResult.Place` is `null` for anyone who didn't make match play/finals. For 2026+, `TournamentResult.Place` is calculated for every bowler in the field: finalists are placed by match play result, and everyone who didn't advance is placed below them by best qualifying score. This is a genuine behavior change from the historical data, not a port — `Place` becomes a required `int`, never `null`, on the new table.
- **`Points` is never negative, and is driven by a not-yet-modeled tournament-level "base points" concept.** Every entrant earns at least a base points value defined per tournament (awarded just for entering), with additional points earned the further a bowler advances (higher placement earns more). The tournament-level base-points concept and the placement-to-points curve are both future work — see Future Work — so `TournamentResult.Points` here is just the computed total with a `>= 0` floor; how that total gets computed is out of scope for this plan.
- **Results are queried directly off the aggregate at read time**, same as `Squads`/`Sponsors` — `GetTournamentQueryHandler` projects from `Tournament.Results` (via the shadow FK) for 2026+ tournaments, replacing today's `// check future stats tables for 2026+ tournament data` placeholder comment. The existing `HistoricalTournamentResult` query path is untouched and keeps serving pre-2026 tournaments.

## Domain Layer

### `Tournament.Complete`

Add to `Tournament` (`src/Neba.Api/Features/Tournaments/Domain/Tournament.cs`), directly mirroring `Season.Complete`:

```csharp
/// <summary>
/// Whether this tournament has finished and its results are final. Must be
/// <see langword="true"/> before <see cref="AddResult"/> may be called.
/// </summary>
public bool Complete { get; private set; }

/// <summary>
/// Marks the tournament complete, allowing results to be recorded. Returns an error if
/// already complete. Carries no other business-rule gate today — the caller (currently the
/// legacy backdoor sync; later a UI-driven endpoint) is responsible for deciding a tournament
/// is actually done. Aggregate-level invariants for what "may be completed" are deferred until
/// that UI-driven endpoint replaces the legacy backdoor as the caller.
/// </summary>
public ErrorOr<Success> Complete()
{
    if (Complete)
    {
        return TournamentErrors.AlreadyComplete;
    }

    Complete = true;

    return Result.Success;
}
```

(`Complete` defaults to `false` in `Tournament.Create`, same as `Season.Create`.)

**Caller today**: the legacy backdoor sync endpoint (see Migration Backdoor section) calls `Complete()` when nebamgmt-v3 reports the tournament finished — nebamgmt-v3 already enforces its own completion rules, so this plan doesn't re-derive them. Once that backdoor is retired, a new UI-driven endpoint takes over as the caller, and that is the point at which real invariants (e.g. all squads scored, match play finished) get added to `Complete()`.

**Forward-looking note, no code change today**: no `TournamentCompleted` domain event exists yet — `Complete()` stays a bare flag flip, consistent with today's only caller (the legacy backdoor) driving `Complete()` and `AddResult()` directly in the same pass. Once the legacy backdoor is retired and `Complete()` gains real invariants, a `TournamentCompleted` (or similarly named) domain event is the expected mechanism for triggering result derivation — and is expected to grow beyond that single subscriber, with squad result report generation and financial calculations both named as plausible future subscribers. Flagging this now so whoever implements that transition designs `Complete()` with an event in mind rather than retrofitting one awkwardly later.

### `TournamentResultId`

`src/Neba.Api/Features/Tournaments/Domain/TournamentResultId.cs`:

```csharp
using StronglyTypedIds;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Unique identifier for a tournament result.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct TournamentResultId;
```

### `TournamentResult`

`src/Neba.Api/Features/Tournaments/Domain/TournamentResult.cs`:

```csharp
using ErrorOr;

using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// One bowler's outcome in a completed Tournament: finishing place, prize money earned, and
/// points earned. Constructed and mutated only through the owning Tournament, once
/// <see cref="Tournament.Complete"/> is <see langword="true"/>.
/// </summary>
public sealed class TournamentResult
{
    /// <summary>
    /// Gets the unique identifier for this result.
    /// </summary>
    public required TournamentResultId Id { get; init; }

    /// <summary>
    /// Gets the bowler this result belongs to.
    /// </summary>
    public required BowlerId BowlerId { get; init; }

    // EF-only navigation, needed for the real foreign key configured in TournamentResultConfiguration.
    // Same pattern as HighBlockAward.Bowler / SquadScore.Bowler — never referenced outside EF configuration.
    internal Bowler Bowler { get; init; } = null!;

    /// <summary>
    /// Gets the bowler's finishing place among the full field. Unlike the legacy historical
    /// data, this is never <see langword="null"/> — bowlers who didn't advance past qualifying
    /// are still ranked, by best qualifying score, below the match play finishers, and a DNF
    /// bowler is always included and ranked (typically last), never omitted or treated as a
    /// null/placeholder case. Not guaranteed unique within a tournament — ties, and
    /// doubles/trios partners in team events, share the same <see cref="Place"/> value.
    /// </summary>
    public int Place { get; private set; }

    /// <summary>
    /// Gets the prize money earned, in dollars. Zero if none earned.
    /// </summary>
    public decimal PrizeMoney { get; private set; }

    /// <summary>
    /// Gets the points earned toward season standings. Includes the tournament's base
    /// points-for-entering plus any additional points earned by placement. Never negative.
    /// </summary>
    public int Points { get; private set; }

    internal static ErrorOr<TournamentResult> Create(
        BowlerId bowlerId, int place, decimal prizeMoney, int points)
    {
        var validated = Validate(place, prizeMoney, points);
        return validated.IsError
            ? validated.Errors
            : new TournamentResult
            {
                Id = TournamentResultId.New(),
                BowlerId = bowlerId,
                Place = place,
                PrizeMoney = prizeMoney,
                Points = points
            };
    }

    private static ErrorOr<Success> Validate(int place, decimal prizeMoney, int points)
    {
        if (place <= 0)
        {
            return TournamentResultErrors.InvalidPlace(place);
        }

        if (prizeMoney < 0)
        {
            return TournamentResultErrors.InvalidPrizeMoney(prizeMoney);
        }

        return points < 0
            ? TournamentResultErrors.InvalidPoints(points)
            : Result.Success;
    }
}
```

### `Tournament.AddResult`

Add to `Tournament`, following the `Season.AddHighBlockWinner` shape exactly:

```csharp
private readonly List<TournamentResult> _results = [];

/// <summary>
/// Results recorded for this tournament, one per bowler; empty until the tournament is
/// complete and results have been recorded.
/// </summary>
public IReadOnlyCollection<TournamentResult> Results
    => _results;

/// <summary>
/// Records a bowler's result; returns an error if the tournament isn't complete or the
/// bowler already has a result recorded.
/// </summary>
public ErrorOr<Success> AddResult(BowlerId bowlerId, int place, decimal prizeMoney, int points)
{
    if (!Complete)
    {
        return TournamentErrors.TournamentNotComplete;
    }

    if (_results.Any(result => result.BowlerId == bowlerId))
    {
        return TournamentErrors.ResultAlreadyRecorded(bowlerId);
    }

    var result = TournamentResult.Create(bowlerId, place, prizeMoney, points);
    if (result.IsError)
    {
        return result.Errors;
    }

    _results.Add(result.Value);

    return Result.Success;
}
```

**Confirmed as drafted — no change**: the duplicate-check above stays `BowlerId`-scoped, and shared `Place` values across multiple rows (ties, team-event partners) are expected, so `Place` itself carries no uniqueness constraint. Idempotency for the migration backdoor re-syncing an already-synced tournament is an orchestration-layer concern, not an aggregate concern — `AddResult` keeps returning `TournamentErrors.ResultAlreadyRecorded` on a duplicate `BowlerId`, and the backdoor's caller is responsible for catching/skipping that on retry.

### Errors

Add to `TournamentErrors` (`src/Neba.Api/Features/Tournaments/Domain/TournamentErrors.cs`, or wherever the existing `SquadNotFound`/`SquadBowlingDateTimeAlreadyUsed` errors live):

- `TournamentErrors.AlreadyComplete` — `Tournament.AlreadyComplete`, `Error.Conflict`
- `TournamentErrors.TournamentNotComplete` — `Tournament.NotComplete`, `Error.Conflict` (matches `SeasonErrors.SeasonNotComplete`'s shape)
- `TournamentErrors.ResultAlreadyRecorded(BowlerId)` — `Tournament.ResultAlreadyRecorded`, `Error.Conflict`

New `TournamentResultErrors` (structural, entity-owned — same split as `SquadScoreErrors.InvalidValue`):

- `TournamentResultErrors.InvalidPlace(int)` — `TournamentResult.Place.Invalid`, `Error.Validation`
- `TournamentResultErrors.InvalidPrizeMoney(decimal)` — `TournamentResult.PrizeMoney.Invalid`, `Error.Validation`
- `TournamentResultErrors.InvalidPoints(int)` — `TournamentResult.Points.Invalid`, `Error.Validation`

## Persistence

`src/Neba.Api/Database/Configurations/TournamentResultConfiguration.cs`, following `SquadScoreConfiguration` exactly (shadow-FK ULID pattern, not the legacy composite-int-key pattern `HistoricalTournamentResultConfiguration` uses):

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class TournamentResultConfiguration : IEntityTypeConfiguration<TournamentResult>
{
    public void Configure(EntityTypeBuilder<TournamentResult> builder)
    {
        builder.ToTable("tournament_results", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(result => result.Id)
            .IsUlid();

        builder.HasAlternateKey(result => result.Id);

        builder.Property<int>(TournamentConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne<Tournament>()
            .WithMany(tournament => tournament.Results)
            .HasForeignKey(TournamentConfiguration.ForeignKeyName)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(result => result.BowlerId)
            .IsUlid(BowlerConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne(result => result.Bowler)
            .WithMany()
            .HasForeignKey(result => result.BowlerId)
            .HasPrincipalKey(bowler => bowler.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(result => result.Place)
            .IsRequired();

        builder.Property(result => result.PrizeMoney)
            .HasPrecision(6, 2)
            .IsRequired();

        builder.Property(result => result.Points)
            .IsRequired();

        builder.HasAlternateKey(result => new
        {
            TournamentDbId = EF.Property<int>(result, TournamentConfiguration.ForeignKeyName),
            result.BowlerId
        });
    }
}
```

**Naming collision note**: the schema-qualified table name `tournament_results` is already used by `HistoricalTournamentResult` in the `historical` schema (`AppDbContext.HistoricalSchema`). Since this new table lives in `AppDbContext.DefaultSchema`, the fully-qualified names don't collide — but flagging it so the migration author doesn't second-guess seeing the same table name twice in the schema.

`PrizeMoney` precision `(6, 2)` matches the legacy column exactly — carried forward, not reconsidered.

Add `builder.HasIndex(...)`-level uniqueness via the `HasAlternateKey` above (one result per bowler per tournament, enforced at the DB level in addition to the aggregate-level check in `AddResult`).

## Query Layer

`GetTournamentQueryHandler.cs` line 217's comment (`// If Results or EntryCount are empty/null, check future stats tables for 2026+ tournament data`) is the placeholder this plan resolves. Replace the historical-only sourcing with a merge: query `Tournament.Results` (via `appDbContext.Set<TournamentResult>()` or a navigation-based projection) for the given tournament, and only fall back to `HistoricalTournamentResult` when no 2026+ rows exist. `TournamentResultDto` (`GetTournament/TournamentResultDto.cs`) already has the right shape (bowler name, place, prize money, points) — project either source into the same DTO, no contract change needed.

**Naming note**: the application-layer DTO is already called `TournamentResultDto`, and this plan's new domain entity is `TournamentResult` — different namespaces (`Features.Tournaments.GetTournament` vs. `Features.Tournaments.Domain`), no actual collision, but worth having in mind when both are open side by side.

**`Place` nullability at the DTO/response boundary**: `TournamentResultDto.Place` (and the corresponding API response field) stays nullable, since it still needs to represent legacy historical rows where `Place` genuinely is `null`. For any row sourced from the new `TournamentResult` table, `Place` is always populated (never `null`) — only the historical-sourced rows can produce a `null` at that boundary.

## Migration Backdoor (separate task)

Populating `TournamentResult` for tournaments already run in nebamgmt-v3 during the migration window is a `/backdoor-feature` task, not part of this plan — that skill designs the sync endpoint/job and the Software-side call site(s) on its own. Noting here only so it isn't forgotten as a dependency: nothing in this plan blocks it, since `AddResult` is the same entry point both the backdoor sync and the eventual match-play job will call. That backdoor sync is also the current caller of `Tournament.Complete()` (see the Domain Layer section above) — it reports both "this tournament is complete" and its results in one pass, straight from nebamgmt-v3.

## Future Work (explicitly out of scope here)

- **Match play** as a domain concept doesn't exist yet. The background job that derives `TournamentResult` rows from match play outcomes when a tournament completes depends on that concept being designed first — this plan only fixes the `AddResult` shape that job will call, not the job itself. **When that job is designed, its bowler population must be sourced from paid, non-refunded Registrations for the tournament** — not "bowlers who bowled" or "bowlers who advanced" — so a DNF bowler with a valid paid entry is still included and still gets a real, ranked `Place`.
- **Tournament-level base points and the placement-to-points curve.** Every entrant's `Points` is meant to be a base "points for entering" value (defined per tournament) plus additional points for how far they advanced, but neither the base-points field on `Tournament` nor the placement curve exists yet. This plan only carries the already-computed `Points` total on `TournamentResult`; where that number comes from is a follow-up design.
- **Completion invariants.** `Complete()` has no business-rule gate today because the legacy system is still the source of truth for when a tournament is actually done. Once the UI-driven completion endpoint replaces the legacy backdoor as the caller, real invariants (e.g. all squads scored, match play finished, every entrant has a derived result) need to be added at that point.
- **Editing a recorded result.** No mechanism planned. If a real correction scenario comes up, design it around that actual case rather than speculatively now.

## SME Review Resolutions

Both Open Questions below are resolved as of SME review. Kept as a record of what was asked and how it was answered — see the inline updates above (`TournamentResult.Place` doc comment, "Decisions locked in" bullets) for where each resolution landed in the working plan.

1. **Qualifying-score tiebreaks for non-finalists — resolved.** For bowlers who didn't advance to match play, `Place` is assigned by: (a) best qualifying block score, descending; (b) ties broken by high game within that best qualifying block; (c) remaining ties share the same `Place` value. This confirms `Place` is not unique per tournament by design.
2. **Where "base points for entering" lives — resolved.** Confirmed on `Tournament`, set at creation (`BasePoints`, following the naming in Future Work above). Conceptually `Season` would be the "purer" owner (base points shouldn't change mid-season), but `Tournament` is the pragmatic, YAGNI-consistent home — no separate Season-level configuration surface needed for a value that's really set per tournament anyway. Still future work (see above) — this only confirms where it will live once designed.
