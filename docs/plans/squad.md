# Squad

Adds the concept of a Squad — a scheduled bowling session within a Tournament — as a child entity of the `Tournament` aggregate.

## Decisions locked in during scoping

- **Child entity of `Tournament`, not a separate aggregate.** A Squad's only invariants right now (bowling date/time within the tournament's date range, no duplicate date/time within the tournament) are checked against data `Tournament` already owns. Matches the `TournamentSponsor`/`TournamentOilPattern` precedent, and gives Squad its own surrogate ID via the `HighBlockAward` precedent (owned child entity with a real strongly-typed ID, not a composite natural key like `TournamentSponsor`/`TournamentOilPattern` use) — future features (registration, scoring) will need to reference a specific Squad by ID.
- **No Singles/Team squad split carried forward from nebamgmt-v3.** The old system distinguished squad types because Team squads tracked a "teams assigned" flag with validations gated on it. Team creation is being reimagined, so that flag and its associated rules aren't modeled here. One `Squad` type serves both Singles and Team tournaments.
- **Minimal field set for now**: `BowlingDateTime`, `MaxEntries` (nullable), `LegacyId` (nullable, mirrors `Tournament.LegacyId`). No `Round`/capacity-by-lane/label fields — those are deferred until the features that need them (registration, scoring) are designed.
- **`MaxEntries` is an Entry count, not a bowler count.** An "Entry" is the existing Team Size unit ubiquitous-language already uses (`Tournament Type.TeamSize`) — one entry is one team in a Team tournament, one bowler in a Singles tournament. `MaxEntries` is the cap on how many entries may bowl that squad; `null` means uncapped.
- **Bowling date/time must fall within `[Tournament.StartDate, Tournament.EndDate]` inclusive.** Enforced by the aggregate at add/update time, same shape as `Season.AssignHighAverageWinner`'s cross-aggregate-fact pattern, except here the fact (`StartDate`/`EndDate`) is already owned by the same aggregate — no query needed.
- **No two squads on the same tournament may share the exact same `BowlingDateTime`.** Matches the existing "squads run one at a time within a tournament" note in `docs/architecture/backend.md`. Enforced by the aggregate, not the database — see the Persistence section for why a DB-level unique index isn't layered on top. **This is a deliberate tightening beyond nebamgmt-v3, not a port** — see Findings below.

## Findings from nebamgmt-v3

Researched via the Explore agent against `Squad`/`SinglesSquad`/`TeamSquad`, `TournamentValidation`, `AddTeamsBO`, and the check-in/qualifying-score repositories. Confirms the plan above and surfaces a few points worth being explicit about:

- **Date-in-range is a direct port.** nebamgmt-v3's `TournamentValidation` base class enforces `Squad.BowlDate.Date` within `[Tournament.Start.Date, Tournament.End.Date]` — exactly what `ValidateSquadDateInRange` implements above.
- **Date/time uniqueness and `MaxEntries` are both new, not ports.** nebamgmt-v3 has no DB or code-level check preventing two squads sharing a `BowlDate`, and no squad-level capacity field at all — capacity was only ever implicit in the check-in UI's lane grid. Both rules in this plan are deliberate tightenings; flagging so they're not mistaken for parity requirements later.
- **`TeamsAssigned` is confirmed pure legacy plumbing** — safe to drop, per the original scoping decision. It stood in for two things that are still real rules, just not modeled by this plan:
  - Once a squad's roster is locked for scoring, its entries/check-ins shouldn't be editable.
  - A bowler can't belong to two different team compositions within the same tournament (a `AddTeamsBO` cross-squad check). Both belong to the future Registration/Team-assignment feature, not Squad itself.
- **nebamgmt-v3 blocks deleting a check-in once a qualifying score exists for that bowler on that squad.** Real invariant, but it's about *entries* (registration), which this plan explicitly defers — nothing creates entries yet, so there's nothing to guard.
- **Squads are append-only in nebamgmt-v3 — there is no edit or delete anywhere in the legacy codebase, only insert at tournament creation.** This plan's `UpdateSquad`/`RemoveSquad` are new capability, not a port. That means the "don't let entries/scores get orphaned by a reschedule or delete" guard nebamgmt-v3 never needed to build doesn't exist to copy — see Deferred below for how this plan handles that gap today.
- **No squad numbering/sequence field exists in nebamgmt-v3** — ordering is implied entirely by `BowlDate`. Confirms the decision to skip a `Round`/sequence field on `Squad`.
- **Lane-assignment locking** (`LaneAssignmentLockBO`) is UI-session concurrency control (prevents two staff double-booking a lane during check-in), not a domain invariant — out of scope for Squad itself.

## Domain Layer

### `SquadId`

`src/Neba.Api/Features/Tournaments/Domain/SquadId.cs`:

```csharp
using StronglyTypedIds;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Unique identifier for a squad.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SquadId;
```

### `Squad`

`src/Neba.Api/Features/Tournaments/Domain/Squad.cs`:

```csharp
using ErrorOr;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// A scheduled bowling session within a Tournament. Bowlers (Singles) or teams (Team formats)
/// compete in a Squad to establish a score toward advancement. A Tournament has one or more Squads.
/// </summary>
public sealed class Squad
{
    /// <summary>
    /// Gets the unique identifier for this squad.
    /// </summary>
    public required SquadId Id { get; init; }

    /// <summary>
    /// Gets the date and time this squad bowls. Must fall within the owning tournament's
    /// start and end date (inclusive).
    /// </summary>
    public DateTimeOffset BowlingDateTime { get; private set; }

    /// <summary>
    /// Gets the maximum number of entries (teams for a Team format, bowlers for Singles) that
    /// may bowl this squad, or <see langword="null"/> if uncapped.
    /// </summary>
    public int? MaxEntries { get; private set; }

    /// <summary>
    /// Gets the legacy numeric identifier for this squad, carried over from the previous
    /// system. <see langword="null"/> for squads created after the system migration.
    /// </summary>
    public int? LegacyId { get; internal set; }

    internal Tournament Tournament { get; init; } = null!;

    internal static ErrorOr<Squad> Create(DateTimeOffset bowlingDateTime, int? maxEntries, int? legacyId)
    {
        var squad = new Squad { Id = SquadId.New(), LegacyId = legacyId };

        var result = squad.UpdateDetails(bowlingDateTime, maxEntries);

        return result.IsError ? result.Errors : squad;
    }

    internal ErrorOr<Updated> UpdateDetails(DateTimeOffset bowlingDateTime, int? maxEntries)
    {
        if (maxEntries is <= 0)
        {
            return SquadErrors.InvalidMaxEntries(maxEntries.Value);
        }

        BowlingDateTime = bowlingDateTime;
        MaxEntries = maxEntries;

        return Result.Updated;
    }
}
```

`src/Neba.Api/Features/Tournaments/Domain/SquadErrors.cs`:

```csharp
using ErrorOr;

namespace Neba.Api.Features.Tournaments.Domain;

internal static class SquadErrors
{
    public static Error InvalidMaxEntries(int maxEntries)
        => Error.Validation(
            code: "Squad.MaxEntries.Invalid",
            description: "Max entries must be greater than zero when specified.",
            metadata: new Dictionary<string, object> { { "MaxEntries", maxEntries } });
}
```

`MaxEntries` positivity is Squad's own structural invariant, so it's owned by `Squad` (via `UpdateDetails`, reused by `Create`) — not by `Tournament`. This is the same split the CLAUDE.md "Always-Valid Child Entities" pattern describes: the entity validates its own shape, the aggregate validates cross-entity/aggregate-state rules.

### `Tournament` additions

```csharp
private readonly List<Squad> _squads = [];

/// <summary>
/// The squads scheduled for this tournament.
/// </summary>
public IReadOnlyCollection<Squad> Squads
    => _squads;

/// <summary>
/// Schedules a new squad; returns an error if the date/time falls outside the tournament's
/// date range or collides with an existing squad's date/time.
/// </summary>
public ErrorOr<Success> AddSquad(DateTimeOffset bowlingDateTime, int? maxEntries = null, int? legacyId = null)
{
    var rangeCheck = ValidateSquadDateInRange(bowlingDateTime);
    if (rangeCheck.IsError)
    {
        return rangeCheck.Errors;
    }

    if (_squads.Any(squad => squad.BowlingDateTime == bowlingDateTime))
    {
        return TournamentErrors.SquadBowlingDateTimeAlreadyUsed(bowlingDateTime);
    }

    var squad = Squad.Create(bowlingDateTime, maxEntries, legacyId);
    if (squad.IsError)
    {
        return squad.Errors;
    }

    _squads.Add(squad.Value);

    return Result.Success;
}

/// <summary>
/// Reschedules or edits a squad; returns an error if the squad doesn't exist, the new
/// date/time falls outside the tournament's date range, or it collides with another squad.
/// </summary>
public ErrorOr<Updated> UpdateSquad(SquadId squadId, DateTimeOffset bowlingDateTime, int? maxEntries)
{
    var squad = _squads.SingleOrDefault(s => s.Id == squadId);
    if (squad is null)
    {
        return TournamentErrors.SquadNotFound(squadId);
    }

    var rangeCheck = ValidateSquadDateInRange(bowlingDateTime);
    if (rangeCheck.IsError)
    {
        return rangeCheck.Errors;
    }

    if (_squads.Any(s => s.Id != squadId && s.BowlingDateTime == bowlingDateTime))
    {
        return TournamentErrors.SquadBowlingDateTimeAlreadyUsed(bowlingDateTime);
    }

    return squad.UpdateDetails(bowlingDateTime, maxEntries);
}

/// <summary>
/// Removes a squad; returns an error if it doesn't exist.
/// </summary>
public ErrorOr<Deleted> RemoveSquad(SquadId squadId)
{
    var squad = _squads.SingleOrDefault(s => s.Id == squadId);
    if (squad is null)
    {
        return TournamentErrors.SquadNotFound(squadId);
    }

    _squads.Remove(squad);

    return Result.Deleted;
}

private ErrorOr<Success> ValidateSquadDateInRange(DateTimeOffset bowlingDateTime)
{
    var bowlingDate = DateOnly.FromDateTime(bowlingDateTime.DateTime);

    return bowlingDate < StartDate || bowlingDate > EndDate
        ? TournamentErrors.SquadDateOutOfRange(bowlingDateTime, StartDate, EndDate)
        : Result.Success;
}
```

`TournamentErrors` additions:

```csharp
public static Error SquadDateOutOfRange(DateTimeOffset bowlingDateTime, DateOnly startDate, DateOnly endDate)
    => Error.Validation(
        code: "Tournament.Squad.DateOutOfRange",
        description: $"Squad bowling date must fall between {startDate:d} and {endDate:d}.",
        metadata: new Dictionary<string, object>
        {
            { "BowlingDateTime", bowlingDateTime },
            { "StartDate", startDate },
            { "EndDate", endDate }
        });

public static Error SquadBowlingDateTimeAlreadyUsed(DateTimeOffset bowlingDateTime)
    => Error.Conflict(
        code: "Tournament.Squad.DateTimeAlreadyUsed",
        description: "Another squad in this tournament already bowls at that date and time.",
        metadata: new Dictionary<string, object> { { "BowlingDateTime", bowlingDateTime } });

public static Error SquadNotFound(SquadId squadId)
    => Error.NotFound(
        code: "Tournament.Squad.NotFound",
        description: $"Squad '{squadId}' was not found on this tournament.");
```

`SquadDateOutOfRange` is `Error.Validation` (422) — the supplied date/time doesn't fit the tournament it's being attached to, structurally. `SquadBowlingDateTimeAlreadyUsed` is `Error.Conflict` (409) — the same payload could succeed later if the colliding squad is moved or removed, per the retry test in the 400/422/409 convention.

## Persistence

`src/Neba.Api/Database/Configurations/SquadConfiguration.cs`, following the `HighBlockAward` precedent (owned child entity with its own surrogate id + ULID alternate key, not the natural-composite-key shape `TournamentSponsor`/`TournamentOilPattern` use):

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Database.Configurations;

internal sealed class SquadConfiguration
    : IEntityTypeConfiguration<Squad>
{
    public void Configure(EntityTypeBuilder<Squad> builder)
    {
        builder.ToTable("squads", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(squad => squad.Id)
            .IsUlid();

        builder.HasAlternateKey(squad => squad.Id);

        builder.Property<int>(TournamentConfiguration.ForeignKeyName)
            .IsRequired();

        builder.HasOne<Tournament>()
            .WithMany(tournament => tournament.Squads)
            .HasForeignKey(TournamentConfiguration.ForeignKeyName)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(squad => squad.BowlingDateTime)
            .HasColumnName("bowling_date_time")
            .IsRequired();

        builder.Property(squad => squad.MaxEntries)
            .HasColumnName("max_entries");

        builder.Property(squad => squad.LegacyId)
            .ValueGeneratedNever();

        builder.HasIndex(squad => squad.LegacyId)
            .IsUnique()
            .AreNullsDistinct();
    }
}
```

**No DB-level unique index on `(tournament_id, bowling_date_time)`.** The uniqueness rule is enforced by `Tournament.AddSquad`/`UpdateSquad` reading the already-loaded `Squads` collection, same as how `AddSponsor`'s title-sponsor-conflict check has no DB constraint backing it — command handlers always load the full aggregate (`.Include(t => t.Squads)`) before mutating it, so there's no concurrent-writer gap a unique index would be closing that the aggregate check doesn't already close.

## Application + API Layers

Follows the `AddTournamentSponsor`/`RemoveTournamentSponsor` use-case-folder shape exactly — not detailed line-by-line here since it's mechanical once the domain method exists:

- `Features/Tournaments/AddSquad/` — `AddSquadCommand` (`TournamentId`, `DateTimeOffset BowlingDateTime`, `int? MaxEntries`), handler loads the tournament with `.Include(t => t.Squads)`, calls `tournament.AddSquad(...)`, saves, evicts the same two cache tags `AddTournamentSponsorCommandHandler` does. Endpoint: `POST {id}/squads`.
- `Features/Tournaments/EditSquad/` — `EditSquadCommand` (`TournamentId`, `SquadId`, `DateTimeOffset BowlingDateTime`, `int? MaxEntries`), calls `tournament.UpdateSquad(...)`. Endpoint: `PUT {id}/squads/{squadId}`.
- `Features/Tournaments/RemoveSquad/` — `RemoveSquadCommand` (`TournamentId`, `SquadId`), calls `tournament.RemoveSquad(...)`. Endpoint: `DELETE {id}/squads/{squadId}`.
- No `LegacyId` on `AddSquad`/`EditSquad`'s public request contracts — same as `Tournament.LegacyId`, it's populated only by the legacy-migration path (`internal set`), never by an authenticated end user.
- Contracts (`Neba.Api.Contracts`) mirror `AddTournamentSponsorRequest`/`RemoveTournamentSponsorRequest` shape: a `SquadInput` (`BowlingDateTime`, `MaxEntries`) wrapped by request records.
- Squads should be surfaced on `TournamentDetailDto`/`SeasonTournamentDto` (a `SquadDto` list: `Id`, `BowlingDateTime`, `MaxEntries`) the same way `Sponsors`/`OilPatterns` already are — left for the implementation pass to wire into `GetTournamentQueryHandler`/`ListTournamentsInSeasonQueryHandler`.

## Ubiquitous Language

Add to `docs/ubiquitous-language.md`, in the `## Tournaments` section, directly after the `### Tournament` entry (before `### Added Money`):

```markdown
### Squad

**Definition**: A scheduled bowling session within a Tournament. Bowlers (Singles formats) or teams (Team formats) compete in a Squad to establish a score toward advancement. A Tournament has one or more Squads, each bowling at a distinct date and time within the tournament's start and end date (inclusive). Squads run one at a time within a tournament — no two overlap.

**Rules**:

- A Squad's bowling date must fall within its Tournament's Start Date and End Date, inclusive
- No two Squads within the same Tournament may share the same bowling date and time
- Squads are assigned exclusively through the Tournament aggregate

**In Code**:

- Namespace: `Neba.Api.Features.Tournaments.Domain`
- Type: `Squad` (child entity of `Tournament`)
- Identity type: `SquadId` (ULID-backed strongly-typed ID)
- Property: `Tournament.Squads` (`IReadOnlyCollection<Squad>`)
- Operations: `Tournament.AddSquad(...)`, `Tournament.UpdateSquad(...)`, `Tournament.RemoveSquad(SquadId)`

---

### Squad Max Entries

**Definition**: The maximum number of Entries (see `### Eligible Entry` — one Entry is one team in a Team-format Tournament, one bowler in a Singles-format Tournament) permitted to bowl a given Squad. `null` means the Squad has no entry cap.

**In Code**:

- Property: `Squad.MaxEntries` (`int?`)

---
```

Also update the existing `**Definition**` line for `### Tournament` (line 733) — it currently says "consisting of one or more qualifying squads," written before Squad existed as a modeled concept. Once this lands, cross-reference it: append "(see `### Squad`)" after "qualifying squads" the same way other UL entries cross-reference related terms.

## Deferred / not in this plan

- **Registration** (which bowlers/teams are entered in a given Squad) — a separate future feature; `MaxEntries` exists now so the constraint is in place before registration needs to enforce it, but nothing enforces it yet since nothing creates entries yet.
- **Blocking `UpdateSquad`/`RemoveSquad` once a squad has entries or scores** — nebamgmt-v3 never had to solve this (squads are append-only there), so there's no legacy behavior to port. This plan's `UpdateSquad`/`RemoveSquad` are unconditional for now. Must be revisited the moment Registration exists: reschedule/removal should be blocked (or at least confirmed/cascaded deliberately) once a squad has entries, and blocked once it has scores, mirroring nebamgmt-v3's check-in-deletion guard.
- **Round classification** on Squad (tying a Squad to `TournamentRound`) — deferred per the "just date/time + legacy id [+ MaxEntries]" scoping decision. Revisit once Cashers/MatchPlay scheduling needs it.
- **Time zone handling for `BowlingDateTime`** — `DateTimeOffset` is used per the codebase-wide convention, but this plan doesn't pin down what offset the create/edit UI will actually send (Eastern-fixed vs. venue-derived). Flagging as an open item for the implementation pass, not resolved here.
