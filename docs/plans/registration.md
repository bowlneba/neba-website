# Registration

Adds `Registration` — a bowler's check-in for a single Squad — as its own aggregate. Covers lane
assignment, entry-fee payment, and withdrawal/DNF for that (Squad, Bowler) pair. Complements
`scorecard.md`; does not change any of that document's decisions. Also documents `Entry`, a
related concept identified during this discussion but deliberately **not** modeled yet, so
`Registration` isn't reshaped when it's built.

## Status

**Design only.** Nothing in this plan has been implemented. Some pieces here are pressure-tested
to the same depth as `scorecard.md`; others (persistence mapping, application/API layers, the
`Entry` aggregate, Registration status) are intentionally left open — see Deferred.

## Relationship to ScoreCard

Revisited and reaffirmed during this discussion: `scorecard.md`'s decisions stand as originally
written. `ScoreCard` stays un-persisted, identity `(SquadId, BowlerId)`, no table, no ID. The
original candidate reason to change that — hanging DNF/Forfeit off it — no longer applies, because
those facts now belong to `Registration` and the future `Entry` instead. `ScoreCard`/`GameScore`
are untouched by anything in this document.

## Decisions locked in during scoping

- **`Registration` is its own aggregate, grain `(SquadId, BowlerId)`** — same grain as `ScoreCard`,
  but a separate aggregate with its own identity and table. It owns participation facts (lane,
  payment, DNF); `ScoreCard` owns scoring facts. Two aggregates because they have different
  invariants and different write patterns, not because of any relationship between them.
- **DNF requires the entry fee to be paid.** A `Registration` may only be marked Did Not Finish
  when `EntryFeePaid` is true. This is independent of `BowlerMembershipId` — membership payment and
  entry-fee payment are two separate facts that happen to both flow through the same check-in
  transaction.
- **DNF is independent of games bowled.** The common case is a bowler starting a block and not
  finishing it. The rare case is a bowler who hasn't bowled at all, whose withdrawal request the
  Tournament Director declines — that's still a DNF, with zero `GameScore` rows behind it. Nothing
  about DNF should assume any games exist.
- **Refund, not games bowled, decides delete-vs-keep.** If a TD grants a refund before any games
  are bowled, the `Registration` is deleted — the bowler is treated as never having entered. If no
  refund is granted, the `Registration` stays and is functionally identical whether zero games or
  some games were bowled: a DNF/withdrawal-without-refund. Deletion is a real operation here, not a
  status transition — a deliberate contrast with `scorecard.md`'s "raw scores are never deleted"
  rule, because a refunded registration was never a real participation.
- **`BowlerMembershipId` is set only when the membership was purchased through this registration's
  own transaction** — never when linking a pre-existing membership. A bowler who already has a
  membership (e.g., paid independently via a general "Add Bowler Membership" flow, such as a board
  member who doesn't bowl tournaments) never gets that membership linked here. This is what makes
  the eventual cascade-delete of the membership when a refunded Registration is deleted safe: the
  FK's mere presence *is* the ownership signal. It also drives at-tournament-site financial
  reporting (how many memberships were paid at the tournament vs. elsewhere) — not designed here,
  but the reason the FK exists in this shape.
    - **This is a process guarantee, not a database constraint.** Nothing stops a future code path
      from setting `BowlerMembershipId` to a pre-existing membership by mistake and silently breaking
      the cascade-delete assumption. Worth a code comment or explicit guard when this is built.
- **Lane assignment is a plain field on `Registration`, not enforced pairing — for now.** USBC Rule
  316 requires paired Entries to share a lane pair even in Singles, and today's system infers
  "teamed" from two bowlers landing on the same lane/spot. Both the pairing-enforcement logic and
  the planned shift to "assign the lane to the team, not the individual" are explicitly a UI/
  workflow concern tied to team check-in, deferred until `Entry`/team tournaments are addressed.
  `LaneNumber` just needs to exist as data now.
- **`Entry` is a distinct future concept — not `Team`.** USBC Rule 311 already uses "Entry" for
  what's submitted for an event (one or more named bowlers); "Team" is reserved in the rules for
  the specific 4/5-player team-event format, so reusing it for the general concept would give the
  UL two meanings for one word. `Entry` covers Singles (one bowler) through Team (four or five) —
  Dave's "team of one" framing.
- **`Entry` is scoped per Tournament, not per Squad** — it has to persist across Squads to detect a
  partner change, which is the whole reason it exists (today's `Teams.Forfeited` flag).
- **Forfeit lives on `Entry`, not on `Registration` or `ScoreCard`.** When an Entry forfeits (e.g.,
  a doubles pair changes partners between squads), the raw scores already bowled stay recorded —
  they're just excluded from standings/advancement. Matches `scorecard.md`'s "raw scores are the
  only persisted source of truth, never deleted" principle.
- **Registration status (Reserved / lane-assigned-but-unpaid / Paid / etc.) is not modeled here.**
  Bowlers can hold a spot before a lane is assigned, be lane-assigned before paying, etc. Likely
  derivable from other fields (presence of `LaneNumber`, `EntryFeePaid`) rather than a stored enum,
  but that's explicitly open until persistence work on `Registration` actually begins.

## Domain Layer (sketch)

Same conventions as `scorecard.md`: `StronglyTypedId` ULID identity, `ErrorOr` for validation,
`AggregateRoot` base. Method bodies below are illustrative, not final — validated invariants are
called out; everything else is a placeholder for the implementation pass.

### `RegistrationId`

`src/Neba.Api/Features/Tournaments/Domain/RegistrationId.cs`:

```csharp
using StronglyTypedIds;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// Unique identifier for a bowler's registration/check-in for a Squad.
/// </summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct RegistrationId;
```

### `Registration`

`src/Neba.Api/Features/Tournaments/Domain/Registration.cs`:

```csharp
using ErrorOr;

using Neba.Api.Domain;
using Neba.Api.Features.Bowlers.Domain;

namespace Neba.Api.Features.Tournaments.Domain;

/// <summary>
/// A bowler's check-in for a single Squad — lane assignment, entry-fee payment, and
/// withdrawal/DNF status. Aggregate root, persisted. Identity is <see cref="Id"/>, grain is
/// one Registration per (Squad, Bowler) — the same grain as <see cref="ScoreCard"/>, but a
/// separate aggregate with separate invariants.
/// </summary>
public sealed class Registration
    : AggregateRoot
{
    public required RegistrationId Id { get; init; }

    public required SquadId SquadId { get; init; }

    public required BowlerId BowlerId { get; init; }

    /// <summary>
    /// Assigned lane, if any. Not yet paired/validated against a partner's lane — see
    /// Deferred: lane pairing is a UI/workflow concern until Entry/team tournaments are built.
    /// </summary>
    public short? LaneNumber { get; private set; }

    /// <summary>
    /// Whether this Squad's entry fee has been paid. Gates <see cref="MarkDidNotFinish"/>.
    /// </summary>
    public bool EntryFeePaid { get; private set; }

    /// <summary>
    /// Whether this bowler did not finish (or withdrew without a refund). Independent of how
    /// many games were bowled — may be true with zero GameScore rows recorded.
    /// </summary>
    public bool DidNotFinish { get; private set; }

    // TODO: placeholder type. BowlerMembership doesn't exist as an aggregate yet — this is a
    // forward reference, same pattern as ScoreCard's expected-game-count dependency on
    // TournamentType. Set only by the "register + pay membership" flow, never by "Add Bowler
    // Membership" — see the process-guarantee note above.
    public Guid? BowlerMembershipId { get; private set; }

    // TODO: once Entry exists — public EntryId? EntryId { get; private set; }

    public static Registration Create(SquadId squadId, BowlerId bowlerId)
        => new()
        {
            Id = RegistrationId.New(),
            SquadId = squadId,
            BowlerId = bowlerId
        };

    public void AssignLane(short laneNumber)
        => LaneNumber = laneNumber;

    public void RecordEntryFeePayment()
        => EntryFeePaid = true;

    /// <summary>
    /// Links a membership purchased as part of this registration's own transaction. Must never
    /// be called to attach a pre-existing membership — see the process-guarantee note above.
    /// </summary>
    public void LinkPurchasedMembership(Guid bowlerMembershipId)
        => BowlerMembershipId = bowlerMembershipId;

    public ErrorOr<Success> MarkDidNotFinish()
    {
        if (!EntryFeePaid)
        {
            return RegistrationErrors.DidNotFinishRequiresPayment(Id);
        }

        DidNotFinish = true;
        return Result.Success;
    }
}
```

`src/Neba.Api/Features/Tournaments/Domain/RegistrationErrors.cs`:

```csharp
using ErrorOr;

namespace Neba.Api.Features.Tournaments.Domain;

internal static class RegistrationErrors
{
    public static Error DidNotFinishRequiresPayment(RegistrationId id)
        => Error.Validation(
            code: "Registration.DidNotFinish.RequiresPayment",
            description: "A Registration can only be marked Did Not Finish if the entry fee has been paid.",
            metadata: new Dictionary<string, object> { { "RegistrationId", id } });
}
```

**Deletion (the refund case) is not a domain method.** Matching `scorecard.md`'s no-repository
pattern, a refunded, zero-game Registration is removed via `appDbContext.Set<Registration>().Remove(...)`
from the command handler — no `Registration.Delete()`/soft-delete method, since the decision was
to actually delete the row, not transition a status.

**Still open for the implementation pass**:

- `BowlerMembershipId`'s real type, once the Membership context/`BowlerMembership` aggregate is designed.
- Whether `LaneNumber` stays a bare `short?` or becomes a small value object once lane pairing is designed.
- Table name, EF configuration, and whether `Registration` follows the same direct-`DbSet`,
  no-repository pattern as `GameScore` (expected, not confirmed for this aggregate specifically).
- Cascade-delete of `BowlerMembership` when a Registration that created it is deleted — mechanism
  not designed (EF `OnDelete(DeleteBehavior.Cascade)` vs. application-layer orchestration), since
  `BowlerMembership` itself doesn't exist yet.

## Ubiquitous Language

Add to `docs/ubiquitous-language.md`, in the `## Tournaments` section, directly after the
`### Game Score` entry from `scorecard.md`:

```markdown
### Registration

**Definition**: One bowler's check-in for a single Squad — records lane assignment, entry-fee
payment, and withdrawal status. A Registration exists for exactly one (Squad, Bowler) pair.

**Rules**:

- A Registration may only be marked Did Not Finish (DNF) once its entry fee is paid.
- DNF is independent of games bowled — a bowler may DNF having bowled zero games (rare — a
  Tournament Director declines a withdrawal request) or having bowled part of a block (common).
- If a bowler's entry fee is refunded before any games are bowled, the Registration is deleted
  rather than marked DNF.
- A Registration's linked Bowler Membership is only ever one purchased as part of that
  registration's own transaction — never a pre-existing membership.

**In Code**:

- Namespace: `Neba.Api.Features.Tournaments.Domain`
- Type: `Registration` (aggregate root; persisted)
- Identity type: `RegistrationId` (ULID-backed strongly-typed ID)
- Operations: `Registration.AssignLane(...)`, `Registration.RecordEntryFeePayment()`, `Registration.MarkDidNotFinish()`

---

### Entry *(concept identified, not yet built)*

**Definition**: The group of one or more Bowlers sharing a single tournament entry — one bowler
for Singles, two for Doubles, three for Trios, four or five for a Team event. Declared once per
Tournament (not per Squad) so a change in partnership across Squads can be detected. Carries the
Forfeited fact when a partnership changes mid-tournament.

**Status**: Not modeled. Referenced here so `Registration` isn't reshaped when it's built — the
eventual field is `Registration.EntryId`.

---
```

## Deferred / not in this plan

- **`Entry`** — the Tournament-scoped grouping of 1+ Bowlers, carrying `Forfeited`. Replaces the
  current `Teams` table's role. Not designed beyond the shape captured above.
- **Registration status** (Reserved / lane-assigned-but-unpaid / Paid / etc.) — likely derived from
  existing fields rather than stored, but not designed until `Registration` persistence work
  actually begins.
- **Lane pairing / team check-in workflow** — today's "same lane infers teamed" behavior, and the
  planned future of assigning a lane to the Entry rather than inferring pairing from individual
  lane/spot assignments. Explicitly a UI/workflow concern, deferred to when `Entry`/team
  tournaments are designed.
- **`BowlerMembership` aggregate itself** — the Membership bounded context doesn't exist yet.
  `Registration.BowlerMembershipId` is a forward reference only.
- **Cascade-delete mechanism** for `BowlerMembership` when its originating `Registration` is
  deleted (refund case) — the *rule* is confirmed (see above), the *mechanism* isn't.
- **At-tournament-site financial reporting** (memberships paid via Registration vs. via the
  general "Add Bowler Membership" flow) — confirmed as a reason `BowlerMembershipId` exists in this
  shape, but the report itself isn't designed here.
- **Persistence / Application / API layers** — not designed in this pass, same status
  `scorecard.md` was in before those sections were filled in.