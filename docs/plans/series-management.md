# Series Management (Vincent Hall Memorial Series, Phase 1)

GitHub issue: [#258](https://github.com/bowlneba/neba-website/issues/258) (parent [#257](https://github.com/bowlneba/neba-website/issues/257))

Admins create and maintain a **Series** for a season: they assign tournaments to ordered **Stages**, set each Stage's kind and advancing count, and describe the squad a Survivor Round uses. Everyone can view a Series and its Stage schedule. This phase stores configuration only. Nothing is computed from results (phase 2) and no payout is stored (phase 3).

## Decisions locked in during scoping

- **Source of truth.** `vincent-hall-series-ul.md` (attached to #257) holds the rule language, ubiquitous language, model and invariants. It wins over #258 where they differ. Code names match it; a mismatch is a defect.
- **Not critical/high-stakes.** No NFR checkpoint. Configuration only: no money, no new PII, a few Series per season with about 11 Stages each. Concurrent edits are last write wins. Audit goes through the existing Audit.NET setup.
- **Permissions.** Reads are public (`AllowAnonymous`). Writes use three new permissions: `Series.CreateSeries`, `Series.EditSeries`, `Series.DeleteSeries`. Admin gets them through `Permissions.List`; Manager is seeded with all three. No new role. Phase 3 will likely add a separate money permission (such as `Series.EditPayout`), so the write permissions stay per action and must not cover payouts.
- **Whole-Series save.** `POST /series` and `PUT /series/{id}` take the name plus the full ordered Stage list. The aggregate reconciles its Stages and checks every invariant in one pass. No Stage sub-resource routes. A Stage's order is its position in the submitted list.
- **Season is fixed after create.** `PUT` does not take a season. To move a Series to another season, delete it and create it again.
- **Stage order.** The admin sets the order in the grid (move up/down). Save rejects any order that doesn't follow tournament start dates, or that breaks the kind order (Qualifying, then Survivor, then Placement).
- **Public pages move into scope.** #258 deferred public Series pages; the user brought them in. The list and detail pages are public and work like Tournaments: one page, with admin controls shown only to users who hold the matching permission.
- **List page has no season picker.** `/series` lists every Series, newest season first, each labeled with its season's date range (for example 1/1/2027 to 12/31/2027).
- **List layout (mockup option 1).** Series whose season hasn't ended are featured, each with its **next Stage** (the first Stage whose tournament hasn't ended). The rest sit under "Past Series". This adds `NextStage` to the list response in Phase 1.
- **Detail layout (mockup option 1).** A "How the field narrows" chart (up to N qualifiers → each Survivor count → Finale N), then the Stages grouped by kind.
- **Team events show the bowler total.** "Top 2 teams" with "(6 bowlers)" on the line below it on the detail page; the editor hint is "Teams of 3" with "(6 bowlers)" below.
- **Detail page shows the Stage schedule.** Name, season date range, and the ordered Stages: kind, tournament (linked to `/tournaments/{id}` so bowlers can sign up), tournament dates, advancing count ("teams" for team events), and squad for Survivor rows. The field and standings arrive in phase 2.
- **Release path.** The work branches from `release/v1.3.0` and the PR targets it. `<Version>` stays `1.3.0` (`check-version-bump.sh` requires a release-branch PR to match `X.Y.Z`), so there is no version commit.
- **UL source goes into the repo docs.** The downloaded `vincent-hall-series-ul.md` (attached to #257) becomes a new `## Series` section in `docs/ubiquitous-language.md`. That covers its terms, its rule language as each term's **Rules** list, the Series vs Lineage and Survivor vs Placement notes, the rules gaps from #257, and the open items. The doc's Part 2 "Proposed model" is covered by each term's **In Code** block.
- **Navigation.** A "Series" item in the main nav's Tournaments dropdown, visible to everyone. Moving the admin links out of the right-hand account menu is a separate concern, not part of this plan.

### Confirmed UI flow

1. **`/series` (public).** Every Series, newest season first: name, season date range, Stage counts by kind, "Locked" badge for admins. Empty state. `FabCreateButton` for `CreateSeries`.
2. **`/series/{id}` (public).** Name, season date range, Stage schedule with tournament links. Edit (`EditSeries`) and Delete (`DeleteSeries`, confirm dialog) for admins; Delete disabled with the reason when locked.
3. **`/series/new` (`CreateSeries`).** Name, season, Stage grid (add, remove, move up/down, kind, tournament, advancing count, squad date and start time on Survivor rows). The tournament picker lists the season's tournaments not already in another Series. One save; messages appear next to the row they name. `DirtyFormGuard`.
4. **`/series/{id}/edit` (`EditSeries`).** Same form, pre-filled, season read-only. When locked: a banner, a read-only grid, and only Name editable.
5. **Nav.** "Series" in the Tournaments dropdown.

## Phase 1: API

New feature folder `src/Neba.Api/Features/Series/`. Inside `Neba.Api.Features.Series.*`, the simple name `Series` resolves to the namespace, not the type, so use-case code writes `Domain.Series` (it resolves without an alias). Code outside the feature (`AppDbContext`, configurations, factories outside `Neba.TestFactory.Series`) uses `Series` through a normal `using`.

Rules from #258 are cited by number: #1 tournament exists and in one Series only, #2 same season, #3 kind order and one Placement last, #4 date order, #5 Survivor counts decrease, #6 Finale N ≤ last Survivor count, #7 squad designation, #8 advancing count > 0.

### Domain

#### `Domain/SeriesId.cs`, `Domain/SeriesStageId.cs`

```csharp
using StronglyTypedIds;

namespace Neba.Api.Features.Series.Domain;

/// <summary>Unique identifier for a Series.</summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SeriesId;

/// <summary>Unique identifier for a Stage within a Series. Stable across edits.</summary>
[StronglyTypedId("ulid-full")]
public readonly partial struct SeriesStageId;
```

#### `Domain/StageKind.cs`

```csharp
using Ardalis.SmartEnum;

namespace Neba.Api.Features.Series.Domain;

/// <summary>A Stage's role in a Series. Values define the required order of kinds.</summary>
public sealed class StageKind
    : SmartEnum<StageKind>
{
    /// <summary>Top N finishers by final tournament position enter the Series Field.</summary>
    public static readonly StageKind Qualifying = new(nameof(Qualifying), 1);

    /// <summary>Series bowlers advance on total score over a designated squad.</summary>
    public static readonly StageKind Survivor = new(nameof(Survivor), 2);

    /// <summary>The final Stage. Ranks by tournament position; the top N go to the Finale.</summary>
    public static readonly StageKind Placement = new(nameof(Placement), 3);

    private StageKind(string name, int value)
        : base(name, value)
    { }
}
```

The API uses `.Name` / `FromName` in both directions. The int values are arbitrary sequence numbers, the same reasoning as the `TournamentType` exception in the SmartEnum convention.

#### `Domain/StageStatus.cs`

```csharp
using Ardalis.SmartEnum;

namespace Neba.Api.Features.Series.Domain;

/// <summary>Processing state of a Stage. Any non-pending Stage locks the Series.</summary>
public sealed class StageStatus
    : SmartEnum<StageStatus>
{
    /// <summary>Not yet processed. Every Stage is pending until phase 2.</summary>
    public static readonly StageStatus Pending = new(nameof(Pending), 0);

    /// <summary>Advancement has been computed but not announced.</summary>
    public static readonly StageStatus Computed = new(nameof(Computed), 1);

    /// <summary>Advancement has been confirmed and announced to bowlers.</summary>
    public static readonly StageStatus Announced = new(nameof(Announced), 2);

    private StageStatus(string name, int value)
        : base(name, value)
    { }
}
```

#### `Domain/SquadDesignation.cs`

```csharp
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>
/// The squad a Survivor Round uses. Described by date and start time, as bowlers refer to it;
/// resolved to a <see cref="Tournaments.Domain.SquadId"/> in phase 2.
/// </summary>
public sealed record SquadDesignation
{
    /// <summary>The date the squad bowls.</summary>
    public DateOnly Date { get; init; }

    /// <summary>The squad's start time, local to the bowling center.</summary>
    public TimeOnly StartTime { get; init; }

    /// <summary>The resolved squad. Always null until phase 2.</summary>
    public SquadId? SquadId { get; init; }

    private SquadDesignation()
    { }

    internal static SquadDesignation Describe(DateOnly date, TimeOnly startTime)
        => new() { Date = date, StartTime = startTime };
}
```

#### `Domain/StageTournamentFacts.cs`, `Domain/SeriesStageDefinition.cs`

```csharp
using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>Tournament data a Series needs to check its Stages. Queried by the handler.</summary>
public sealed record StageTournamentFacts
{
    public required TournamentId TournamentId { get; init; }
    public required string TournamentName { get; init; }
    public required SeasonId SeasonId { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }

    /// <summary>Name of another Series that already has this tournament as a Stage, if any.</summary>
    public string? OtherSeriesName { get; init; }
}

/// <summary>One submitted Stage. Its order is its position in the submitted list.</summary>
public sealed record SeriesStageDefinition
{
    /// <summary>Existing Stage to update; null adds a new Stage.</summary>
    public SeriesStageId? StageId { get; init; }
    public required StageKind Kind { get; init; }
    public required TournamentId TournamentId { get; init; }
    public required int AdvancingCount { get; init; }
    public DateOnly? SquadDate { get; init; }
    public TimeOnly? SquadStartTime { get; init; }
}
```

#### `Domain/SeriesStage.cs`

```csharp
using ErrorOr;

using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>One tournament's role in a Series.</summary>
public sealed class SeriesStage
{
    public required SeriesStageId Id { get; init; }

    /// <summary>1-based position within the Series. Follows tournament start date.</summary>
    public int Order { get; private set; }

    public StageKind Kind { get; private set; } = StageKind.Qualifying;

    /// <summary>The tournament for this Stage, by ID only.</summary>
    public TournamentId TournamentId { get; private set; }

    /// <summary>Top N finishers (Qualifying; teams in a team event), bowlers advancing (Survivor), or Finale N (Placement).</summary>
    public int AdvancingCount { get; private set; }

    /// <summary>Always <see cref="StageStatus.Pending"/> in phase 1. Internal setter is the phase 2 and test seam.</summary>
    public StageStatus Status { get; internal set; } = StageStatus.Pending;

    /// <summary>Set for a Survivor Stage, otherwise null.</summary>
    public SquadDesignation? SquadDesignation { get; private set; }

    private SeriesStage()
    { }

    internal static ErrorOr<SeriesStage> Create(int order, SeriesStageDefinition definition, StageTournamentFacts facts)
    {
        var stage = new SeriesStage { Id = SeriesStageId.New() };
        var result = stage.Update(order, definition, facts);

        return result.IsError ? result.Errors : stage;
    }

    internal ErrorOr<Updated> Update(int order, SeriesStageDefinition definition, StageTournamentFacts facts)
    {
        var errors = new List<Error>();

        if (definition.AdvancingCount <= 0)
        {
            errors.Add(SeriesErrors.AdvancingCountNotPositive(order, facts.TournamentName, definition.AdvancingCount));
        }

        var hasSquad = definition.SquadDate is not null && definition.SquadStartTime is not null;

        if (definition.Kind == StageKind.Survivor && !hasSquad)
        {
            errors.Add(SeriesErrors.SquadRequired(order, facts.TournamentName));
        }
        else if (definition.Kind != StageKind.Survivor && (definition.SquadDate is not null || definition.SquadStartTime is not null))
        {
            errors.Add(SeriesErrors.SquadNotAllowed(order, facts.TournamentName, definition.Kind));
        }
        else if (hasSquad && (definition.SquadDate < facts.StartDate || definition.SquadDate > facts.EndDate))
        {
            errors.Add(SeriesErrors.SquadDateOutsideTournament(order, facts.TournamentName, definition.SquadDate!.Value, facts.StartDate, facts.EndDate));
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        Order = order;
        Kind = definition.Kind;
        TournamentId = definition.TournamentId;
        AdvancingCount = definition.AdvancingCount;
        SquadDesignation = hasSquad
            ? SquadDesignation.Describe(definition.SquadDate!.Value, definition.SquadStartTime!.Value)
            : null;

        return Result.Updated;
    }

    internal bool Matches(int order, SeriesStageDefinition definition)
        => Order == order
            && Kind == definition.Kind
            && TournamentId == definition.TournamentId
            && AdvancingCount == definition.AdvancingCount
            && SquadDesignation?.Date == definition.SquadDate
            && SquadDesignation?.StartTime == definition.SquadStartTime;
}
```

#### `Domain/Series.cs`

```csharp
using ErrorOr;

using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>
/// A season-scoped competition made up of ordered Stages, each linked to one tournament
/// (the Vincent Hall Memorial Series).
/// </summary>
public sealed class Series
{
    internal const int NameMaxLength = 127;

    public required SeriesId Id { get; init; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>The season this Series belongs to. Fixed after creation.</summary>
    public SeasonId SeasonId { get; private set; }

    private readonly List<SeriesStage> _stages = [];

    /// <summary>The Stages, in order.</summary>
    public IReadOnlyCollection<SeriesStage> Stages
        => _stages.AsReadOnly();

    /// <summary>True once any Stage has left pending. Only <see cref="Name"/> can change while locked.</summary>
    public bool IsLocked
        => _stages.Exists(stage => stage.Status != StageStatus.Pending);

    private Series()
    { }

    /// <summary>Creates a Series. It may be incomplete, for example with no Placement Stage yet.</summary>
    public static ErrorOr<Series> Create(
        string name,
        SeasonId seasonId,
        IReadOnlyList<SeriesStageDefinition> stages,
        IReadOnlyDictionary<TournamentId, StageTournamentFacts> tournaments)
    {
        var series = new Series { Id = SeriesId.New(), SeasonId = seasonId };

        var renamed = series.Rename(name);
        if (renamed.IsError)
        {
            return renamed.Errors;
        }

        var applied = series.ApplyStages(stages, tournaments);

        return applied.IsError ? applied.Errors : series;
    }

    /// <summary>Changes the name. Allowed while locked.</summary>
    public ErrorOr<Updated> Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return SeriesErrors.NameRequired;
        }

        if (name.Length > NameMaxLength)
        {
            return SeriesErrors.NameTooLong(NameMaxLength);
        }

        Name = name.Trim();

        return Result.Updated;
    }

    /// <summary>
    /// Replaces the Stages with the submitted list: matched Stage IDs update in place, new rows are added,
    /// missing Stages are removed. A no-op when nothing changed, so a locked Series can still be saved.
    /// </summary>
    public ErrorOr<Updated> ReplaceStages(
        IReadOnlyList<SeriesStageDefinition> stages,
        IReadOnlyDictionary<TournamentId, StageTournamentFacts> tournaments)
    {
        if (stages.Count == _stages.Count && stages.Select((d, i) => (d, i)).All(x =>
                _stages.Exists(s => s.Id == x.d.StageId && s.Matches(x.i + 1, x.d))))
        {
            return Result.Updated;
        }

        if (IsLocked)
        {
            return SeriesErrors.Locked;
        }

        var applied = ApplyStages(stages, tournaments);

        return applied.IsError ? applied.Errors : Result.Updated;
    }

    /// <summary>Refuses deletion while locked.</summary>
    public ErrorOr<Success> EnsureCanDelete()
        => IsLocked ? SeriesErrors.Locked : Result.Success;

    private ErrorOr<Success> ApplyStages(
        IReadOnlyList<SeriesStageDefinition> definitions,
        IReadOnlyDictionary<TournamentId, StageTournamentFacts> tournaments)
    {
        var errors = new List<Error>();
        var candidates = new List<(int Order, SeriesStageDefinition Definition, StageTournamentFacts Facts)>();
        var seen = new HashSet<TournamentId>();

        // Per-Stage checks: existence, duplicates, other Series (#1), season (#2), Stage's own invariants (#7, #8).
        for (var index = 0; index < definitions.Count; index++)
        {
            var order = index + 1;
            var definition = definitions[index];

            if (definition.StageId is { } stageId && !_stages.Exists(s => s.Id == stageId))
            {
                errors.Add(SeriesErrors.StageNotFound(order, stageId));
                continue;
            }

            if (!tournaments.TryGetValue(definition.TournamentId, out var facts))
            {
                errors.Add(SeriesErrors.TournamentNotFound(order, definition.TournamentId));
                continue;
            }

            if (!seen.Add(definition.TournamentId))
            {
                errors.Add(SeriesErrors.DuplicateTournament(order, facts.TournamentName));
            }

            if (facts.OtherSeriesName is { } otherSeries)
            {
                errors.Add(SeriesErrors.TournamentInAnotherSeries(order, facts.TournamentName, otherSeries));
            }

            if (facts.SeasonId != SeasonId)
            {
                errors.Add(SeriesErrors.TournamentNotInSeason(order, facts.TournamentName));
            }

            var stageCheck = SeriesStage.Create(order, definition, facts);
            if (stageCheck.IsError)
            {
                errors.AddRange(stageCheck.Errors);
            }

            candidates.Add((order, definition, facts));
        }

        // Cross-Stage checks over Stages whose tournament resolved (#3 to #6).
        errors.AddRange(CheckSequence(candidates));

        if (errors.Count > 0)
        {
            return errors;
        }

        var keptIds = definitions.Where(d => d.StageId is not null).Select(d => d.StageId!.Value).ToHashSet();
        _stages.RemoveAll(stage => !keptIds.Contains(stage.Id));

        foreach (var (order, definition, facts) in candidates)
        {
            var existing = definition.StageId is { } id ? _stages.Single(s => s.Id == id) : null;

            if (existing is not null)
            {
                existing.Update(order, definition, facts);
            }
            else
            {
                _stages.Add(SeriesStage.Create(order, definition, facts).Value);
            }
        }

        _stages.Sort((a, b) => a.Order.CompareTo(b.Order));

        return Result.Success;
    }

    private static IEnumerable<Error> CheckSequence(
        IReadOnlyList<(int Order, SeriesStageDefinition Definition, StageTournamentFacts Facts)> stages)
    {
        var placementSeen = false;
        int? lastSurvivorCount = null;

        for (var i = 0; i < stages.Count; i++)
        {
            var (order, definition, facts) = stages[i];

            if (i > 0)
            {
                var previous = stages[i - 1];

                if (definition.Kind.Value < previous.Definition.Kind.Value)
                {
                    yield return SeriesErrors.KindOutOfOrder(order, facts.TournamentName, definition.Kind, previous.Definition.Kind);
                }

                if (facts.StartDate <= previous.Facts.StartDate)
                {
                    yield return SeriesErrors.DateNotAfterPrevious(order, facts.TournamentName, facts.StartDate, previous.Facts.StartDate);
                }
            }

            if (definition.Kind == StageKind.Survivor)
            {
                if (lastSurvivorCount is { } previousCount && definition.AdvancingCount >= previousCount)
                {
                    yield return SeriesErrors.SurvivorCountNotDecreasing(order, facts.TournamentName, definition.AdvancingCount, previousCount);
                }

                lastSurvivorCount = definition.AdvancingCount;
            }

            if (definition.Kind == StageKind.Placement)
            {
                if (placementSeen)
                {
                    yield return SeriesErrors.MultiplePlacementStages(order, facts.TournamentName);
                }
                else if (i != stages.Count - 1)
                {
                    yield return SeriesErrors.PlacementNotLast(order, facts.TournamentName);
                }

                if (lastSurvivorCount is { } survivorCount && definition.AdvancingCount > survivorCount)
                {
                    yield return SeriesErrors.PlacementCountExceedsSurvivor(order, facts.TournamentName, definition.AdvancingCount, survivorCount);
                }

                placementSeen = true;
            }
        }
    }
}
```

#### `Domain/SeriesErrors.cs`

Every Stage error names the Stage in its description and carries `StageOrder` metadata, which the endpoint turns into a row-named ProblemDetails error.

```csharp
using System.Globalization;

using ErrorOr;

using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.Domain;

internal static class SeriesErrors
{
    public const string StageOrderKey = "StageOrder";

    public static readonly Error NameRequired = Error.Validation(
        code: "Series.Name.Required",
        description: "Series name must not be empty.");

    public static Error NameTooLong(int maxLength) => Error.Validation(
        code: "Series.Name.TooLong",
        description: $"Series name must be {maxLength} characters or fewer.");

    public static Error SeasonNotFound(string seasonId) => Error.Validation(
        code: "Series.Season.NotFound",
        description: "The selected season does not exist.",
        metadata: new Dictionary<string, object> { { "SeasonId", seasonId } });

    public static Error NotFound(SeriesId id) => Error.NotFound(
        code: "Series.NotFound",
        description: "Series not found.",
        metadata: new Dictionary<string, object> { { "SeriesId", id.ToString() } });

    public static readonly Error Locked = Error.Conflict(
        code: "Series.Locked",
        description: "This Series is locked because processing has begun. Only its name can change, and it cannot be deleted.");

    // Stage errors. "Stage 9 (Callahan's Bowl-O-Rama): ..." names the offending row.
    public static Error StageNotFound(int order, SeriesStageId stageId)
        => Stage(order, null, "Series.Stage.NotFound", "this Stage no longer exists. Reload the Series and try again.", ErrorType.Validation);

    public static Error TournamentNotFound(int order, TournamentId tournamentId)
        => Stage(order, null, "Series.Stage.TournamentNotFound", "the selected tournament does not exist.", ErrorType.Validation);

    public static Error DuplicateTournament(int order, string tournament)
        => Stage(order, tournament, "Series.Stage.DuplicateTournament", "this tournament is already a Stage in this Series.", ErrorType.Validation);

    public static Error TournamentInAnotherSeries(int order, string tournament, string otherSeries)
        => Stage(order, tournament, "Series.Stage.TournamentInAnotherSeries", $"this tournament is already a Stage in the {otherSeries} Series.", ErrorType.Conflict);

    public static Error TournamentNotInSeason(int order, string tournament)
        => Stage(order, tournament, "Series.Stage.TournamentNotInSeason", "this tournament is not in the Series' season.", ErrorType.Validation);

    public static Error KindOutOfOrder(int order, string tournament, StageKind kind, StageKind previous)
        => Stage(order, tournament, "Series.Stage.KindOutOfOrder", $"a {kind.Name} Stage cannot follow a {previous.Name} Stage. Stages run Qualifying, then Survivor, then Placement.", ErrorType.Validation);

    public static Error MultiplePlacementStages(int order, string tournament)
        => Stage(order, tournament, "Series.Stage.MultiplePlacement", "a Series has at most one Placement Stage.", ErrorType.Validation);

    public static Error PlacementNotLast(int order, string tournament)
        => Stage(order, tournament, "Series.Stage.PlacementNotLast", "the Placement Stage must be the last Stage.", ErrorType.Validation);

    public static Error DateNotAfterPrevious(int order, string tournament, DateOnly startDate, DateOnly previousStartDate)
        => Stage(order, tournament, "Series.Stage.DateNotAfterPrevious", $"this tournament starts {Format(startDate)}, which is not after the previous Stage's tournament ({Format(previousStartDate)}). Stages must follow tournament dates.", ErrorType.Validation);

    public static Error SurvivorCountNotDecreasing(int order, string tournament, int count, int previousCount)
        => Stage(order, tournament, "Series.Stage.SurvivorCountNotDecreasing", $"{count} advancing must be fewer than the previous Survivor Stage's {previousCount}.", ErrorType.Validation);

    public static Error PlacementCountExceedsSurvivor(int order, string tournament, int count, int survivorCount)
        => Stage(order, tournament, "Series.Stage.PlacementCountExceedsSurvivor", $"a Finale of {count} cannot be larger than the last Survivor Stage's {survivorCount}.", ErrorType.Validation);

    public static Error AdvancingCountNotPositive(int order, string tournament, int count)
        => Stage(order, tournament, "Series.Stage.AdvancingCountNotPositive", "advancing count must be a whole number greater than zero.", ErrorType.Validation);

    public static Error SquadRequired(int order, string tournament)
        => Stage(order, tournament, "Series.Stage.SquadRequired", "a Survivor Stage needs a squad date and start time.", ErrorType.Validation);

    public static Error SquadNotAllowed(int order, string tournament, StageKind kind)
        => Stage(order, tournament, "Series.Stage.SquadNotAllowed", $"a {kind.Name} Stage has no designated squad.", ErrorType.Validation);

    public static Error SquadDateOutsideTournament(int order, string tournament, DateOnly squadDate, DateOnly startDate, DateOnly endDate)
        => Stage(order, tournament, "Series.Stage.SquadDateOutsideTournament", $"the squad date {Format(squadDate)} is outside the tournament's dates ({Format(startDate)} to {Format(endDate)}).", ErrorType.Validation);

    private static Error Stage(int order, string? tournament, string code, string reason, ErrorType type)
    {
        var label = tournament is null ? $"Stage {order}" : $"Stage {order} ({tournament})";
        var metadata = new Dictionary<string, object> { { StageOrderKey, order } };
        var description = $"{label}: {reason}";

        return type == ErrorType.Conflict
            ? Error.Conflict(code, description, metadata)
            : Error.Validation(code, description, metadata);
    }

    private static string Format(DateOnly date)
        => date.ToString("M/d/yyyy", CultureInfo.InvariantCulture);
}
```

### Application (use cases)

#### Shared in the feature root

`Features/Series/StageTournamentFactsReader.cs`: queries the facts both create and edit pass to the aggregate. Same feature, so no boundary issue. Reading `Tournament` and `Series` through `AppDbContext` uses Domain types only.

```csharp
using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series;

internal static class StageTournamentFactsReader
{
    public static async Task<IReadOnlyDictionary<TournamentId, StageTournamentFacts>> ReadAsync(
        AppDbContext appDbContext,
        IReadOnlyCollection<TournamentId> tournamentIds,
        SeriesId? excludingSeriesId,
        CancellationToken cancellationToken)
    {
        var otherSeries = appDbContext.Series.AsNoTracking();
        if (excludingSeriesId is { } excluded)
        {
            otherSeries = otherSeries.Where(series => series.Id != excluded);
        }

        var holders = await otherSeries
            .SelectMany(series => series.Stages
                .Where(stage => tournamentIds.Contains(stage.TournamentId))
                .Select(stage => new { stage.TournamentId, SeriesName = series.Name }))
            .ToDictionaryAsync(x => x.TournamentId, x => x.SeriesName, cancellationToken);

        return await appDbContext.Tournaments
            .AsNoTracking()
            .Where(tournament => tournamentIds.Contains(tournament.Id))
            .Select(tournament => new StageTournamentFacts
            {
                TournamentId = tournament.Id,
                TournamentName = tournament.Name,
                SeasonId = tournament.SeasonId,
                StartDate = tournament.StartDate,
                EndDate = tournament.EndDate,
            })
            .ToDictionaryAsync(
                facts => facts.TournamentId,
                facts => holders.TryGetValue(facts.TournamentId, out var name) ? facts with { OtherSeriesName = name } : facts,
                cancellationToken);
    }
}
```

`Features/Series/SeriesStageInputMapper.cs`: maps contract input to domain definitions.

```csharp
using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series;

internal static class SeriesStageInputMapper
{
    public static IReadOnlyList<SeriesStageDefinition> ToDefinitions(IReadOnlyList<SeriesStageInput> stages)
        => [.. stages.Select(stage => new SeriesStageDefinition
        {
            StageId = stage.Id is null ? null : new SeriesStageId(stage.Id),
            Kind = StageKind.FromName(stage.Kind),
            TournamentId = new TournamentId(stage.TournamentId),
            AdvancingCount = stage.AdvancingCount,
            SquadDate = stage.SquadDate,
            SquadStartTime = stage.SquadStartTime,
        })];
}
```

`Features/Series/SeriesStageInputValidator.cs` (structural only; presence per kind is rule #7 in the domain):

```csharp
using FluentValidation;

using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Series;

internal sealed class SeriesStageInputValidator
    : AbstractValidator<SeriesStageInput>
{
    public SeriesStageInputValidator()
    {
        RuleFor(s => s.Id)
            .Length(26).When(s => s.Id is not null)
            .WithErrorCode("SeriesStageInput.IdInvalidLength")
            .WithMessage("Stage ID must be a 26-character ULID.");

        RuleFor(s => s.Kind)
            .Must(kind => StageKind.TryFromName(kind, out _))
            .WithErrorCode("SeriesStageInput.KindInvalid")
            .WithMessage("Stage kind must be Qualifying, Survivor, or Placement.");

        RuleFor(s => s.TournamentId)
            .NotEmpty().Length(26)
            .WithErrorCode("SeriesStageInput.TournamentIdInvalid")
            .WithMessage("Each Stage needs a tournament.");

        RuleFor(s => s)
            .Must(s => (s.SquadDate is null) == (s.SquadStartTime is null))
            .WithName(nameof(SeriesStageInput.SquadDate))
            .WithErrorCode("SeriesStageInput.SquadIncomplete")
            .WithMessage("Squad date and start time go together.");
    }
}
```

`Features/Series/SeriesMutationResultSender.cs`: Validation errors first (422, all of them); otherwise Conflicts (409, all of them). A lock Conflict comes back alone, so it lands on 409. Each Stage error becomes a `ValidationFailure` named `Series.Stages[i]`.

```csharp
using ErrorOr;

using FluentValidation.Results;

using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Series;

internal static class SeriesMutationResultSender
{
    public static async Task SendAsync(
        IReadOnlyList<Error> errors,
        Action<ValidationFailure> addError,
        Func<int, CancellationToken, Task> sendErrorsAsync,
        CancellationToken ct)
    {
        var validation = errors.Where(e => e.Type != ErrorType.Conflict).ToList();
        var toSend = validation.Count > 0 ? validation : errors;

        foreach (var error in toSend)
        {
            addError(ToFailure(error));
        }

        await sendErrorsAsync(
            validation.Count > 0 ? StatusCodes.Status422UnprocessableEntity : StatusCodes.Status409Conflict, ct);
    }

    private static ValidationFailure ToFailure(Error error)
    {
        var property = error.Metadata?.TryGetValue(SeriesErrors.StageOrderKey, out var value) == true && value is int order
            ? $"Series.Stages[{order - 1}]"
            : "Series";

        return new ValidationFailure(property, error.Description) { ErrorCode = error.Code };
    }
}
```

#### `ListSeries/`

```csharp
// ListSeriesQuery.cs: not cached (see Decisions)
internal sealed record ListSeriesQuery
    : IQuery<IReadOnlyCollection<SeriesSummaryDto>>;

// SeriesSummaryDto.cs
internal sealed record SeriesSummaryDto
{
    public required SeriesId Id { get; init; }
    public required string Name { get; init; }
    public required SeasonId SeasonId { get; init; }
    public required string SeasonDescription { get; init; }
    public required DateOnly SeasonStartDate { get; init; }
    public required DateOnly SeasonEndDate { get; init; }
    public required int QualifyingStageCount { get; init; }
    public required int SurvivorStageCount { get; init; }
    public required int PlacementStageCount { get; init; }
    public required bool IsLocked { get; init; }

    /// <summary>The first Stage (by order) whose tournament hasn't ended; null when all have.</summary>
    public SeriesNextStageDto? NextStage { get; init; }
}

// SeriesNextStageDto.cs
internal sealed record SeriesNextStageDto
{
    public required string Kind { get; init; }
    public required TournamentId TournamentId { get; init; }
    public required string TournamentName { get; init; }
    public required DateOnly TournamentStartDate { get; init; }
    public required DateOnly TournamentEndDate { get; init; }
}

// ListSeriesQueryHandler.cs
internal sealed class ListSeriesQueryHandler(AppDbContext appDbContext, TimeProvider timeProvider)
    : IQueryHandler<ListSeriesQuery, IReadOnlyCollection<SeriesSummaryDto>>
{
    public async Task<IReadOnlyCollection<SeriesSummaryDto>> HandleAsync(ListSeriesQuery query, CancellationToken cancellationToken)
    {
        // Same cut-off as ListUpcomingTournaments: back up one day so a tournament ending today
        // still counts in every U.S. time zone.
        var cutoff = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime).AddDays(-1);

        var summaries = await appDbContext.Series.AsNoTracking()
            .Join(appDbContext.Seasons.AsNoTracking(), series => series.SeasonId, season => season.Id,
                (series, season) => new { series, season })
            .OrderByDescending(x => x.season.StartDate)
            .ThenBy(x => x.series.Name)
            .Select(x => new SeriesSummaryDto
            {
                Id = x.series.Id,
                Name = x.series.Name,
                SeasonId = x.season.Id,
                SeasonDescription = x.season.Description,
                SeasonStartDate = x.season.StartDate,
                SeasonEndDate = x.season.EndDate,
                QualifyingStageCount = x.series.Stages.Count(s => s.Kind == StageKind.Qualifying),
                SurvivorStageCount = x.series.Stages.Count(s => s.Kind == StageKind.Survivor),
                PlacementStageCount = x.series.Stages.Count(s => s.Kind == StageKind.Placement),
                IsLocked = x.series.Stages.Any(s => s.Status != StageStatus.Pending),
            })
            .ToListAsync(cancellationToken);

        // Upcoming Stages across all Series (a few dozen rows at most), reduced to the first per Series.
        var upcoming = await appDbContext.Series.AsNoTracking()
            .SelectMany(series => series.Stages, (series, stage) => new { SeriesId = series.Id, stage.Order, stage.Kind, stage.TournamentId })
            .Join(appDbContext.Tournaments.AsNoTracking(), x => x.TournamentId, t => t.Id,
                (x, t) => new { x.SeriesId, x.Order, x.Kind, x.TournamentId, t.Name, t.StartDate, t.EndDate })
            .Where(x => x.EndDate >= cutoff)
            .ToListAsync(cancellationToken);

        var nextBySeries = upcoming
            .GroupBy(x => x.SeriesId)
            .ToDictionary(g => g.Key, g => g.MinBy(x => x.Order)!);

        return [.. summaries.Select(summary => nextBySeries.TryGetValue(summary.Id, out var next)
            ? summary with
            {
                NextStage = new SeriesNextStageDto
                {
                    Kind = next.Kind.Name,
                    TournamentId = next.TournamentId,
                    TournamentName = next.Name,
                    TournamentStartDate = next.StartDate,
                    TournamentEndDate = next.EndDate,
                },
            }
            : summary)];
    }
}
```

`ListSeriesEndpoint`: `EndpointWithoutRequest<CollectionResponse<SeriesSummaryResponse>>`, `Get(string.Empty)`, `Group<SeriesEndpointGroup>()`, version set `Series` 1.0, `AllowAnonymous()`, `WithName("ListSeries")`, tag `Public`, produces 200 and 500. Maps each DTO onto `SeriesSummaryResponse` field for field (ids `.Value.ToString()`; `NextStage` onto `SeriesNextStageResponse`, or null). `ListSeriesSummary`: "Lists every Series, newest season first, each with its next Stage (the first whose tournament hasn't ended)."

#### `GetSeries/`

```csharp
// GetSeriesRequest.cs
internal sealed class GetSeriesRequest
{
    [BindFrom("id")]
    public required string SeriesId { get; set; }
}

// GetSeriesRequestValidator.cs: NotEmpty + Length(26), codes "GetSeriesRequest.SeriesIdRequired" / "...InvalidLength"

// GetSeriesQuery.cs
internal sealed record GetSeriesQuery
    : IQuery<ErrorOr<SeriesDetailDto>>
{
    public required SeriesId Id { get; init; }
}

// SeriesDetailDto.cs / SeriesStageDto.cs
internal sealed record SeriesDetailDto
{
    public required SeriesId Id { get; init; }
    public required string Name { get; init; }
    public required SeasonId SeasonId { get; init; }
    public required string SeasonDescription { get; init; }
    public required DateOnly SeasonStartDate { get; init; }
    public required DateOnly SeasonEndDate { get; init; }
    public required bool IsLocked { get; init; }
    public required IReadOnlyCollection<SeriesStageDto> Stages { get; init; }
}

internal sealed record SeriesStageDto
{
    public required SeriesStageId Id { get; init; }
    public required int Order { get; init; }
    public required string Kind { get; init; }
    public required string Status { get; init; }
    public required int AdvancingCount { get; init; }
    public DateOnly? SquadDate { get; init; }
    public TimeOnly? SquadStartTime { get; init; }
    public required TournamentId TournamentId { get; init; }
    public required string TournamentName { get; init; }
    public required DateOnly TournamentStartDate { get; init; }
    public required DateOnly TournamentEndDate { get; init; }
    public required string TournamentType { get; init; }
    public required int TeamSize { get; init; }
}

// GetSeriesQueryHandler.cs
internal sealed class GetSeriesQueryHandler(AppDbContext appDbContext)
    : IQueryHandler<GetSeriesQuery, ErrorOr<SeriesDetailDto>>
{
    public async Task<ErrorOr<SeriesDetailDto>> HandleAsync(GetSeriesQuery query, CancellationToken cancellationToken)
    {
        var header = await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == query.Id)
            .Join(appDbContext.Seasons.AsNoTracking(), series => series.SeasonId, season => season.Id,
                (series, season) => new
                {
                    series.Id, series.Name, SeasonId = season.Id, season.Description, season.StartDate, season.EndDate,
                    IsLocked = series.Stages.Any(s => s.Status != StageStatus.Pending),
                })
            .SingleOrDefaultAsync(cancellationToken);

        if (header is null)
        {
            return SeriesErrors.NotFound(query.Id);
        }

        var stages = await appDbContext.Series.AsNoTracking()
            .Where(series => series.Id == query.Id)
            .SelectMany(series => series.Stages)
            .Join(appDbContext.Tournaments.AsNoTracking(), stage => stage.TournamentId, tournament => tournament.Id,
                (stage, tournament) => new { stage, tournament })
            .OrderBy(x => x.stage.Order)
            .Select(x => new
            {
                x.stage.Id, x.stage.Order, x.stage.Kind, x.stage.Status, x.stage.AdvancingCount,
                SquadDate = x.stage.SquadDesignation == null ? (DateOnly?)null : x.stage.SquadDesignation.Date,
                SquadStartTime = x.stage.SquadDesignation == null ? (TimeOnly?)null : x.stage.SquadDesignation.StartTime,
                TournamentId = x.tournament.Id, TournamentName = x.tournament.Name,
                x.tournament.StartDate, x.tournament.EndDate, x.tournament.TournamentType,
            })
            .ToListAsync(cancellationToken);

        return new SeriesDetailDto
        {
            Id = header.Id,
            Name = header.Name,
            SeasonId = header.SeasonId,
            SeasonDescription = header.Description,
            SeasonStartDate = header.StartDate,
            SeasonEndDate = header.EndDate,
            IsLocked = header.IsLocked,
            Stages = [.. stages.Select(s => new SeriesStageDto
            {
                Id = s.Id,
                Order = s.Order,
                Kind = s.Kind.Name,
                Status = s.Status.Name,
                AdvancingCount = s.AdvancingCount,
                SquadDate = s.SquadDate,
                SquadStartTime = s.SquadStartTime,
                TournamentId = s.TournamentId,
                TournamentName = s.TournamentName,
                TournamentStartDate = s.StartDate,
                TournamentEndDate = s.EndDate,
                TournamentType = s.TournamentType.Name,
                TeamSize = s.TournamentType.TeamSize,
            })],
        };
    }
}
```

`GetSeriesEndpoint`: `Endpoint<GetSeriesRequest, SeriesDetailResponse>`, `Get("{id}")`, `AllowAnonymous()`, `WithName("GetSeries")`, tag `Public`, produces 200, 400, 404, 500. A NotFound error goes through `Send.NotFoundAsync`, the same way as `GetTournamentEndpoint`. Maps the DTO to `SeriesDetailResponse`.

#### `CreateSeries/`

```csharp
// CreateSeriesCommand.cs
internal sealed record CreateSeriesCommand
    : ICommand<SeriesId>
{
    public required string Name { get; init; }
    public required SeasonId SeasonId { get; init; }
    public required IReadOnlyList<SeriesStageDefinition> Stages { get; init; }
}

// CreateSeriesCommandHandler.cs
internal sealed class CreateSeriesCommandHandler(AppDbContext appDbContext)
    : ICommandHandler<CreateSeriesCommand, SeriesId>
{
    public async Task<ErrorOr<SeriesId>> HandleAsync(CreateSeriesCommand command, CancellationToken cancellationToken)
    {
        if (!await appDbContext.Seasons.AnyAsync(season => season.Id == command.SeasonId, cancellationToken))
        {
            return SeriesErrors.SeasonNotFound(command.SeasonId.ToString());
        }

        var tournaments = await StageTournamentFactsReader.ReadAsync(
            appDbContext, [.. command.Stages.Select(s => s.TournamentId).Distinct()], excludingSeriesId: null, cancellationToken);

        var series = Domain.Series.Create(command.Name, command.SeasonId, command.Stages, tournaments);
        if (series.IsError)
        {
            return series.Errors;
        }

        await appDbContext.Series.AddAsync(series.Value, cancellationToken);
        await appDbContext.SaveChangesAsync(cancellationToken);

        return series.Value.Id;
    }
}

// CreateSeriesRequestValidator.cs
internal sealed class CreateSeriesRequestValidator
    : Validator<CreateSeriesRequest>
{
    public CreateSeriesRequestValidator()
    {
        RuleFor(r => r.Series.Name)
            .NotEmpty().WithErrorCode("CreateSeriesRequest.NameRequired").WithMessage("Series name is required.")
            .MaximumLength(127).WithErrorCode("CreateSeriesRequest.NameTooLong").WithMessage("Series name must be 127 characters or fewer.");

        RuleFor(r => r.Series.SeasonId)
            .NotEmpty().Length(26).WithErrorCode("CreateSeriesRequest.SeasonIdInvalid").WithMessage("Season is required.");

        RuleForEach(r => r.Series.Stages).SetValidator(new SeriesStageInputValidator());
    }
}

// CreateSeriesEndpoint.cs
internal sealed class CreateSeriesEndpoint(Messaging.ICommandHandler<CreateSeriesCommand, SeriesId> commandHandler)
    : Endpoint<CreateSeriesRequest, CreatedSeriesResponse>
{
    private readonly Messaging.ICommandHandler<CreateSeriesCommand, SeriesId> _commandHandler = commandHandler;

    public override void Configure()
    {
        Post(string.Empty);
        Group<SeriesEndpointGroup>();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CreateSeries.PolicyName);

        Description(description => description
            .WithName("CreateSeries")
            .WithTags("Admin")
            .Produces<CreatedSeriesResponse>(StatusCodes.Status201Created)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden)
            .ProducesProblemDetails(StatusCodes.Status409Conflict)
            .ProducesProblemDetails(StatusCodes.Status422UnprocessableEntity));
    }

    public override async Task HandleAsync(CreateSeriesRequest req, CancellationToken ct)
    {
        var command = new CreateSeriesCommand
        {
            Name = req.Series.Name,
            SeasonId = new SeasonId(req.Series.SeasonId),
            Stages = SeriesStageInputMapper.ToDefinitions(req.Series.Stages),
        };

        var result = await _commandHandler.HandleAsync(command, ct);

        if (result.IsError)
        {
            await SeriesMutationResultSender.SendAsync(result.Errors, failure => AddError(failure), Send.ErrorsAsync, ct);
            // Stryker disable once Statement
            return;
        }

        var id = result.Value.Value.ToString();

        // Stryker disable once Statement
        await Send.CreatedAtAsync("GetSeries", routeValues: new { id }, responseBody: new CreatedSeriesResponse { SeriesId = id }, cancellation: ct);
    }
}
```

`CreateSeriesSummary` covers 201 (example id), 400 (structural), 401, 403 ("lacks Series.CreateSeries"), 409 ("a tournament is already a Stage in another Series"), and 422 ("season not found or a Stage breaks a Series rule; each error names the Stage").

#### `EditSeries/`

```csharp
// EditSeriesCommand.cs
internal sealed record EditSeriesCommand
    : ICommand<Updated>
{
    public required SeriesId SeriesId { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<SeriesStageDefinition> Stages { get; init; }
}

// EditSeriesCommandHandler.cs
internal sealed class EditSeriesCommandHandler(AppDbContext appDbContext)
    : ICommandHandler<EditSeriesCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(EditSeriesCommand command, CancellationToken cancellationToken)
    {
        var series = await appDbContext.Series
            .Include(s => s.Stages)
            .SingleOrDefaultAsync(s => s.Id == command.SeriesId, cancellationToken);

        if (series is null)
        {
            return SeriesErrors.NotFound(command.SeriesId);
        }

        var tournaments = await StageTournamentFactsReader.ReadAsync(
            appDbContext, [.. command.Stages.Select(s => s.TournamentId).Distinct()], command.SeriesId, cancellationToken);

        var renamed = series.Rename(command.Name);
        var replaced = series.ReplaceStages(command.Stages, tournaments);

        List<Error> errors = [.. renamed.ErrorsOrEmptyList, .. replaced.ErrorsOrEmptyList];
        if (errors.Count > 0)
        {
            return errors;
        }

        await appDbContext.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
```

`EditSeriesRequestValidator`: the same rules as create, minus the season, plus `Id` NotEmpty/Length(26). `EditSeriesEndpoint`: `Put("{id}")`, `Policies(PermissionCatalog.EditSeries.PolicyName)`, `WithName("EditSeries")`, produces 204, 400, 401, 403, 404, 409, 422. NotFound → `Send.NotFoundAsync`; other errors go through `SeriesMutationResultSender`; success → `Send.NoContentAsync`.

#### `DeleteSeries/`

```csharp
// DeleteSeriesCommandHandler.cs
internal sealed class DeleteSeriesCommandHandler(AppDbContext appDbContext)
    : ICommandHandler<DeleteSeriesCommand, Deleted>
{
    public async Task<ErrorOr<Deleted>> HandleAsync(DeleteSeriesCommand command, CancellationToken cancellationToken)
    {
        var series = await appDbContext.Series
            .Include(s => s.Stages)
            .SingleOrDefaultAsync(s => s.Id == command.SeriesId, cancellationToken);

        if (series is null)
        {
            return Result.Deleted;
        }

        var canDelete = series.EnsureCanDelete();
        if (canDelete.IsError)
        {
            return canDelete.Errors;
        }

        appDbContext.Series.Remove(series);
        await appDbContext.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
```

`DeleteSeriesCommand { SeriesId }`, `DeleteSeriesRequest { [BindFrom("id")] Id }` + validator, `DeleteSeriesEndpoint`: `Delete("{id}")`, `Policies(PermissionCatalog.DeleteSeries.PolicyName)`, `WithName("DeleteSeries")`, produces 204, 400, 401, 403, 409. `DeleteSeriesSummary` says a missing Series still returns 204 and a locked Series returns 409.

#### `SeriesEndpointGroup.cs`

```csharp
internal sealed class SeriesEndpointGroup
    : SubGroup<BaseEndpointGroup>
{
    public SeriesEndpointGroup()
    {
        VersionSets.CreateApi("Series", v => v
            .HasApiVersion(new ApiVersion(1, 0)));

        Configure("series", endpoint => endpoint
            .Description(description => description
                .WithTags("Series")
                .ProducesProblemDetails(500)));
    }
}
```

| Route | Endpoint | Auth | Responses |
| --- | --- | --- | --- |
| `GET /series` | `ListSeriesEndpoint` | `AllowAnonymous()` | 200 |
| `GET /series/{id}` | `GetSeriesEndpoint` | `AllowAnonymous()` | 200, 400, 404 |
| `POST /series` | `CreateSeriesEndpoint` | `Permission:Series.CreateSeries` | 201, 400, 401, 403, 409, 422 |
| `PUT /series/{id}` | `EditSeriesEndpoint` | `Permission:Series.EditSeries` | 204, 400, 401, 403, 404, 409, 422 |
| `DELETE /series/{id}` | `DeleteSeriesEndpoint` | `Permission:Series.DeleteSeries` | 204, 400, 401, 403, 409 |

#### Tournament delete guard

`Features/Tournaments/Domain/TournamentErrors.cs`:

```csharp
public static Error IsSeriesStage(TournamentId id, string seriesName)
    => Error.Conflict(
        code: "Tournament.IsSeriesStage",
        description: $"This tournament is a Stage in the {seriesName} Series. Remove it from the Series before deleting it.",
        metadata: new Dictionary<string, object>
        {
            { "TournamentId", id.ToString() },
            { "SeriesName", seriesName },
        });
```

`Features/Tournaments/DeleteTournament/DeleteTournamentCommandHandler.cs`, after the null check and before `HasHistoricalRecordsAsync`:

```csharp
var seriesName = await appDbContext.Series
    .Where(series => series.Stages.Any(stage => stage.TournamentId == command.TournamentId))
    .Select(series => series.Name)
    .FirstOrDefaultAsync(cancellationToken);

if (seriesName is not null)
{
    return TournamentErrors.IsSeriesStage(command.TournamentId, seriesName);
}
```

`DeleteTournamentSummary`: add the new 409 reason. The endpoint already maps Conflicts to 409.

### Infrastructure

#### `Database/Configurations/SeriesConfiguration.cs`

```csharp
internal sealed class SeriesConfiguration
    : IEntityTypeConfiguration<Series>
{
    internal const string ForeignKeyName = "series_id";

    public void Configure(EntityTypeBuilder<Series> builder)
    {
        builder.ToTable("series", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(series => series.Id)
            .IsUlid();

        builder.HasAlternateKey(series => series.Id);

        builder.Property(series => series.Name)
            .HasMaxLength(Series.NameMaxLength)
            .IsRequired();

        builder.Property(series => series.SeasonId)
            .IsUlid(SeasonConfiguration.ForeignKeyName);

        builder.HasOne<Season>()
            .WithMany()
            .HasForeignKey(series => series.SeasonId)
            .HasPrincipalKey(season => season.Id)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(series => series.Stages)
            .WithOne()
            .HasForeignKey(ForeignKeyName)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(series => series.Stages)
            .HasField("_stages")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(series => series.IsLocked);
    }
}
```

#### `Database/Configurations/SeriesStageConfiguration.cs`

```csharp
internal sealed class SeriesStageConfiguration
    : IEntityTypeConfiguration<SeriesStage>
{
    public void Configure(EntityTypeBuilder<SeriesStage> builder)
    {
        builder.ToTable("series_stages", AppDbContext.DefaultSchema);

        builder.ConfigureShadowId();

        builder.Property(stage => stage.Id)
            .IsUlid();

        builder.HasAlternateKey(stage => stage.Id);

        builder.Property<int>(SeriesConfiguration.ForeignKeyName)
            .IsRequired();

        builder.Property(stage => stage.Order)
            .HasColumnName("stage_order") // "order" is a reserved word
            .IsRequired();

        builder.HasIndex(SeriesConfiguration.ForeignKeyName, nameof(SeriesStage.Order))
            .IsUnique();

        builder.Property(stage => stage.Kind)
            .IsRequired();

        builder.Property(stage => stage.Status)
            .IsRequired();

        builder.Property(stage => stage.AdvancingCount)
            .IsRequired();

        builder.Property(stage => stage.TournamentId)
            .IsUlid(TournamentConfiguration.ForeignKeyName);

        builder.HasOne<Tournament>()
            .WithMany()
            .HasForeignKey(stage => stage.TournamentId)
            .HasPrincipalKey(tournament => tournament.Id)
            .OnDelete(DeleteBehavior.NoAction);

        // Backstop for rule #1 when two saves race.
        builder.HasIndex(stage => stage.TournamentId)
            .IsUnique();

        // Optional complex type (precedent: HallOfFameInduction.PhotoFocalPoint).
        builder.ComplexProperty(stage => stage.SquadDesignation, squad =>
        {
            squad.Property(s => s.Date).HasColumnName("squad_date");
            squad.Property(s => s.StartTime).HasColumnName("squad_start_time");

            // No FK: EF cannot place one inside a complex type. Phase 2 resolves it.
            squad.Property(s => s.SquadId)
                .HasColumnName("squad_id")
                .HasMaxLength(26)
                .IsFixedLength();
        });
    }
}
```

The strongly typed id inside the complex type needs the same ULID value converter the conventions apply elsewhere. If the convention doesn't reach complex-type properties, add `.HasConversion<SquadId.EfCoreValueConverter>()`. Confirm when building the migration.

#### `Database/AppDbContext.cs`

```csharp
public DbSet<Series> Series
    => Set<Series>();
```

Match the style of the surrounding `DbSet` properties.

#### Migration

`dotnet ef migrations add AddSeries --project src/Neba.Api` adds `series` and `series_stages`, the unique indexes on `(series_id, stage_order)` and `tournament_id`, and FKs to `seasons` and `tournaments` (`NO ACTION`) and to `series` (`CASCADE`).

#### `Auditing/AuditingConfiguration.cs`

Add `Series` and `SeriesStage` to the `ForContext<AppDbContext>` include list, next to `Tournament` and `Squad`.

### Contracts (`src/Neba.Api.Contracts/Series/`)

```csharp
// ISeriesApi.cs
public interface ISeriesApi
{
    /// <summary>Lists every Series, newest season first.</summary>
    [Get("/series")]
    Task<IApiResponse<CollectionResponse<SeriesSummaryResponse>>> ListSeriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets a Series and its Stage schedule.</summary>
    [Get("/series/{seriesId}")]
    Task<IApiResponse<SeriesDetailResponse>> GetSeriesAsync(string seriesId, CancellationToken cancellationToken = default);

    /// <summary>Creates a Series. Requires Series.CreateSeries.</summary>
    [Post("/series")]
    Task<IApiResponse<CreatedSeriesResponse>> CreateSeriesAsync(CreateSeriesRequest request, CancellationToken cancellationToken = default);

    /// <summary>Saves a Series' name and full Stage list. Requires Series.EditSeries.</summary>
    [Put("/series/{seriesId}")]
    Task<IApiResponse> EditSeriesAsync(string seriesId, EditSeriesRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes an unlocked Series and its Stages. Requires Series.DeleteSeries.</summary>
    [Delete("/series/{seriesId}")]
    Task<IApiResponse> DeleteSeriesAsync(string seriesId, CancellationToken cancellationToken = default);
}

// CreateSeries/CreateSeriesRequest.cs
public sealed record CreateSeriesRequest
{
    public required SeriesInput Series { get; init; }
}

// CreateSeries/SeriesInput.cs
public sealed record SeriesInput
{
    public required string Name { get; init; }
    public required string SeasonId { get; init; }

    /// <summary>Stages in order. Order is the list position.</summary>
    public required IReadOnlyList<SeriesStageInput> Stages { get; init; }
}

// CreateSeries/SeriesStageInput.cs
public sealed record SeriesStageInput
{
    /// <summary>Existing Stage ID when editing; null for a new Stage.</summary>
    public string? Id { get; init; }

    /// <summary>"Qualifying", "Survivor", or "Placement".</summary>
    public required string Kind { get; init; }

    public required string TournamentId { get; init; }
    public required int AdvancingCount { get; init; }

    /// <summary>Survivor Stages only.</summary>
    public DateOnly? SquadDate { get; init; }

    /// <summary>Survivor Stages only.</summary>
    public TimeOnly? SquadStartTime { get; init; }
}

// CreateSeries/CreatedSeriesResponse.cs
public sealed record CreatedSeriesResponse
{
    public required string SeriesId { get; init; }
}

// EditSeries/EditSeriesRequest.cs, EditSeries/EditSeriesInput.cs
public sealed record EditSeriesRequest
{
    public required string Id { get; init; }
    public required EditSeriesInput Series { get; init; }
}

/// <summary>No season: it is fixed after creation.</summary>
public sealed record EditSeriesInput
{
    public required string Name { get; init; }
    public required IReadOnlyList<SeriesStageInput> Stages { get; init; }
}

// ListSeries/SeriesSummaryResponse.cs
public sealed record SeriesSummaryResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string SeasonId { get; init; }
    public required string SeasonDescription { get; init; }
    public required DateOnly SeasonStartDate { get; init; }
    public required DateOnly SeasonEndDate { get; init; }
    public required int QualifyingStageCount { get; init; }
    public required int SurvivorStageCount { get; init; }
    public required int PlacementStageCount { get; init; }
    public required bool IsLocked { get; init; }

    /// <summary>The first Stage whose tournament hasn't ended; null once every Stage's tournament is over.</summary>
    public SeriesNextStageResponse? NextStage { get; init; }
}

// ListSeries/SeriesNextStageResponse.cs
public sealed record SeriesNextStageResponse
{
    /// <summary>"Qualifying", "Survivor", or "Placement".</summary>
    public required string Kind { get; init; }
    public required string TournamentId { get; init; }
    public required string TournamentName { get; init; }
    public required DateOnly TournamentStartDate { get; init; }
    public required DateOnly TournamentEndDate { get; init; }
}

// GetSeries/SeriesDetailResponse.cs, GetSeries/SeriesStageResponse.cs
public sealed record SeriesDetailResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string SeasonId { get; init; }
    public required string SeasonDescription { get; init; }
    public required DateOnly SeasonStartDate { get; init; }
    public required DateOnly SeasonEndDate { get; init; }
    public required bool IsLocked { get; init; }
    public required IReadOnlyCollection<SeriesStageResponse> Stages { get; init; }
}

public sealed record SeriesStageResponse
{
    public required string Id { get; init; }
    public required int Order { get; init; }
    public required string Kind { get; init; }
    public required string Status { get; init; }
    public required int AdvancingCount { get; init; }
    public DateOnly? SquadDate { get; init; }
    public TimeOnly? SquadStartTime { get; init; }
    public required string TournamentId { get; init; }
    public required string TournamentName { get; init; }
    public required DateOnly TournamentStartDate { get; init; }
    public required DateOnly TournamentEndDate { get; init; }
    public required string TournamentType { get; init; }

    /// <summary>Bowlers per entry. Greater than 1 means AdvancingCount counts teams (Qualifying).</summary>
    public required int TeamSize { get; init; }
}
```

The tournament fields are flattened onto the Stage response, so no nested `SeriesStageTournamentResponse` or `SeriesSeasonResponse` types are needed.

#### `Security/Permission.cs`

```csharp
#region Series

/// <summary>
/// Permission to create a Series.
/// </summary>
public static readonly Permissions CreateSeries = new("Series.CreateSeries", "Create Series");

/// <summary>
/// Permission to edit a Series' name and Stages. Does not cover payouts.
/// </summary>
public static readonly Permissions EditSeries = new("Series.EditSeries", "Edit Series");

/// <summary>
/// Permission to delete an unlocked Series.
/// </summary>
public static readonly Permissions DeleteSeries = new("Series.DeleteSeries", "Delete Series");

#endregion
```

#### `Security/Infrastructure/SecurityRoleSeeder.cs`

Add to the `[Roles.Manager]` list:

```csharp
Permissions.CreateSeries,
Permissions.EditSeries,
Permissions.DeleteSeries,
```

Admin already has them through `Permissions.List`.

### Tests and factories

#### Factories (`tests/Neba.TestFactory/Series/`)

```csharp
public static class SeriesFactory
{
    public const string ValidName = "Vincent Hall Memorial Series";

    /// <summary>No Stages by default (an incomplete Series is valid), so it persists with only a season.</summary>
    public static Series Create(
        string? name = null,
        SeasonId? seasonId = null,
        IReadOnlyList<SeriesStageDefinition>? stages = null,
        IReadOnlyDictionary<TournamentId, StageTournamentFacts>? tournaments = null)
    {
        var result = Series.Create(
            name ?? ValidName,
            seasonId ?? SeasonId.New(),
            stages ?? [],
            tournaments ?? new Dictionary<TournamentId, StageTournamentFacts>());

        return result.IsError
            ? throw new InvalidOperationException($"Failed to create series: {result.Errors[0].Description}")
            : result.Value;
    }

    /// <summary>Builds a Series whose Stages run against the given tournaments, deriving the facts from them.</summary>
    public static Series CreateFor(
        Season season,
        IReadOnlyList<(Tournament Tournament, StageKind Kind, int AdvancingCount, DateOnly? SquadDate, TimeOnly? SquadStartTime)> stages,
        string? name = null);

    public static IReadOnlyCollection<Series> Bogus(int count, int? seed = null);
}

public static class SeriesStageDefinitionFactory
{
    public static SeriesStageDefinition Create(
        SeriesStageId? stageId = null,
        StageKind? kind = null,
        TournamentId? tournamentId = null,
        int? advancingCount = null,
        DateOnly? squadDate = null,
        TimeOnly? squadStartTime = null);
}

public static class StageTournamentFactsFactory
{
    public static StageTournamentFacts Create(
        TournamentId? tournamentId = null,
        string? tournamentName = null,
        SeasonId? seasonId = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? otherSeriesName = null);
}

/// <summary>The 2025 reference configuration from #258, as tournaments + definitions.</summary>
public static class Series2025ReferenceFactory
{
    public static (Season Season, IReadOnlyList<Tournament> Tournaments, IReadOnlyList<SeriesStageDefinition> Stages) Create();
}
```

Response and DTO factories: `SeriesSummaryDtoFactory`, `SeriesNextStageDtoFactory`, `SeriesNextStageResponseFactory`, `SeriesDetailDtoFactory`, `SeriesStageDtoFactory`, `SeriesSummaryResponseFactory`, `SeriesDetailResponseFactory`, `SeriesStageResponseFactory`, `SeriesInputFactory`, `SeriesStageInputFactory`. Each has `Create()` with const defaults and `Bogus(count, seed?)`.

#### Domain unit tests (`tests/Neba.Api.Tests/Features/Series/Domain/`)

All tests carry `[UnitTest]`, `[Component("Series")]`, a `DisplayName`, and AAA comments.

- `SeriesTests`
  - Create: valid, empty (incomplete), blank name, name too long.
  - #1: tournament not found, duplicate in the same Series, in another Series (Conflict).
  - #2: tournament in a different season.
  - #3: Survivor before Qualifying, Qualifying after Placement, two Placements, Placement not last.
  - #4: equal start date, earlier start date.
  - #5: Survivor count equal to the previous one, larger than the previous one.
  - #6: Finale N larger than the last Survivor count; allowed when there is no Survivor Stage.
  - Several errors returned in one pass, each with the right `StageOrder`.
  - `ReplaceStages`: updates in place and keeps the Stage id, reorders, removes a missing Stage, adds a new Stage, unknown Stage id → error.
  - Lock: `ReplaceStages` with changes → `Locked`; unchanged → Updated; `Rename` allowed; `EnsureCanDelete` → `Locked`. The test sets `Status = StageStatus.Computed` through the internal setter.
  - `Stages` collection regression: `((ICollection<SeriesStage>)series.Stages)` is read-only and the backing list accepts adds through `ReplaceStages` (EF collection learning).
- `SeriesStageTests`
  - #8: advancing count of 0 and -1.
  - #7: Survivor without a squad, Qualifying with a squad, Placement with a squad, squad date before or after the tournament, squad on a multi-day tournament's second day allowed.
  - `Matches`.
- `SeriesErrorsTests`: descriptions name the Stage and tournament; metadata carries `StageOrder`.

#### Handler unit tests

These use the in-memory `AppDbContext` test helper the other handler tests use (check `Features/Tournaments` handler tests for the current pattern).

- `ListSeriesQueryHandlerTests` (integration, real database): next Stage is the first unfinished one by order; a tournament ending today still counts (cut-off minus one day, via `FakeTimeProvider`); null when every tournament has ended; newest season first.
- `CreateSeriesCommandHandlerTests`: season missing → 422 error; domain errors bubble up; saves on success.
- `EditSeriesCommandHandlerTests`: not found; rename on a locked Series with unchanged Stages succeeds; the Series' own tournaments aren't reported as "in another Series".
- `DeleteSeriesCommandHandlerTests`: missing → Deleted; locked → Conflict; removes the Series.
- `DeleteTournamentCommandHandlerTests` (existing file): new case where the tournament is a Stage → `Tournament.IsSeriesStage`.
- `SeriesMutationResultSenderTests`: 422 when any Validation error is present; 409 when all are Conflicts; property name `Series.Stages[i]`.
- `SeriesStageInputValidatorTests`, `CreateSeriesRequestValidatorTests`, `EditSeriesRequestValidatorTests`.

#### Integration tests (`[IntegrationTest]`)

- `SeriesEndpointsTests`:
  - Anonymous `GET /series` and `GET /series/{id}` work; `GET` of an unknown id → 404.
  - Writes: anonymous → 401; Member → 403; Manager and Admin → success.
  - `POST` → 201 with a Location header; a broken row → 422 with `errors[].name == "Series.Stages[2]"`; a tournament in another Series → 409.
  - `PUT` → 204; renaming a locked Series → 204; changing a locked Series' Stages → 409.
  - `DELETE` → 204, again → 204; locked → 409.
- `Series2025ReferenceTests`: seeds the season and the 2025 tournaments, `POST`s the reference configuration, `GET`s it back, and asserts all 11 Stages: order, kind, advancing count, squad on rows 9 and 10.
- `SeriesConfigurationTests`: round-trips a Series with mixed Stages, including a null designation; the unique `tournament_id` index rejects a second Stage on the same tournament across Series.
- `DeleteTournamentEndpointTests` (existing): 409 naming the Series, then 204 once the Stage is removed.
- `EfAuditIntegrationTests` (existing): Series and SeriesStage changes produce audit events.
- `FeatureBoundaryTests` stays green with no new entries in `KnownSliceViolations`.

### Docs

- [ ] `docs/ubiquitous-language.md`: new `## Series` section after `## Tournaments`, taken from the downloaded `vincent-hall-series-ul.md`. Each entry uses the file's format (**Definition**, **Rules**, **In Code**, separated by `---`).

  | Entry | Content from the source doc | Status note |
  | --- | --- | --- |
  | Series | Part 1 definition; rules "Proposed rule: Series" 1 to 4; one season, normally one Series, may have more; Series vs Lineage note | In Code: `Series`, `SeriesId`, `series` table |
  | Stage | Definition; at most one Placement Round and it's last; order follows tournament date; a tournament is a Stage in at most one Series; locked once processing begins (Locked Series) | In Code: `SeriesStage`, `SeriesStageId`, `Series.ReplaceStages` |
  | Stage Kind | The three kinds, Survivor Round vs Placement Round note | In Code: `StageKind` |
  | Qualifying Event | Definition; "Proposed rule: Qualifying" 1 to 4 | Rules 2 to 4 are computed in phase 2 |
  | Survivor Round | Definition; "Proposed rule: Survivor Rounds" 1 to 4 | Advancement computed in phase 2 |
  | Placement Round | Definition; "Proposed rule: Placement Round" 1 to 3 | Ranking computed in phase 2 |
  | Advancing Count | Meaning per kind (top N, teams count N × team size; bowlers advancing; Finale N); must be > 0; Survivor counts decrease; Finale N ≤ last Survivor count | In Code: `SeriesStage.AdvancingCount` |
  | Squad Designation | Described (date + start time, as bowlers say it) vs Resolved (`SquadId`) | Resolved in phase 2 |
  | Stage Status | Pending, Computed, Announced; any non-pending Stage locks the Series | Transitions in phase 2 |
  | Series Field | Definition; invariants "advancing count never grows the field; ties can exceed it" | Phase 2, no code yet |
  | Backfill | Definition (open-category BOTY points as of the last Qualifying Event; ties advance); no passing slots down the finish list | Phase 2, no code yet |
  | Series Position | Definition; unique | Phase 3, no code yet |
  | Finale | Definition; bowled outside the system; replacement swaps Series Position and payout ("Proposed rule: Finale" 1) | Phase 3, no code yet |
  | Series Champion | Definition; distinct from the NEBA Title; defending Series Champion is display only | Phase 3, no code yet |

  - The `## Series` section intro carries the summary paragraph from the source doc, the rules basis (posted 2025 rules; special tournament committee with the sponsor sets payouts; USBC rules outrank tournament rules), and the 2025 reference configuration table from #258 as the worked example.
  - A **Rules gaps (NEBA practice, not yet codified)** list, from #257's "Rules gaps to codify" 1 to 8.
  - An **Open items** list, from the source doc's "Open items" (a tournament in more than one Series is now settled as "no", per this plan; the rest stay open).
  - The existing `### Cup` entry under `## Stats` gains one line: "Not the Vincent Hall Memorial Series (formerly the Paul Forry Memorial Cup). Cup is a reserved term for the legacy points-based Women's Cup and is not modeled."
  - "Lineage" is named only in the Series vs Lineage note. It belongs to the Defending Champion work (`defending-champion-ul.md`) and gets its own entry with that work.
- [ ] `docs/policies/README.md`: one row each for `Permission:Series.CreateSeries`, `Permission:Series.EditSeries`, `Permission:Series.DeleteSeries` (seeded to Admin through `Permissions.List`, and Manager).
- [ ] `docs/help/`: `create-series.md`, `edit-series.md`, `delete-series.md` (ADR-0007). Screenshots come with the UI in phase 2 of this plan.

### Deferred or out of scope (this phase)

- Everything #258 defers: computation, squad resolution, stage status transitions, payouts (and a separate payout permission), completeness check, reacting to tournament date or season changes, extra Series properties.
- Caching of Series reads.

## Phase 2: UI

New folder `src/Neba.Website.Server/Series/`. Pages call `ISeriesApi` through `ApiExecutor`, the way `EditTournament.razor` does. Component names avoid a bare `Series` class, which would clash with the `Neba.Website.Server.Series` namespace. Display rules (labels, comparisons, string building) live in `SeriesDisplay.cs`, so the Razor markup stays free of `< N =>` lambdas and string interpolation (Razor learning). The confirmed mockups (list option 1, detail option 1, editor) are the source for markup and classes.

### Mockups

Static HTML under `docs/plans/mockups/series-management/` (shared `mockup.css` uses the site's theme tokens; `series-data.js` holds the 2025 reference configuration). Each page has a dark bar at the top with review toggles (admin vs public, locked, empty state, failed save). Those toggles aren't part of the design.

| File | Shows |
| --- | --- |
| `series-list-option-1.html` | **Chosen.** List: the current Series featured with its next Stage, past Series as compact rows. `NextStage` was added to the Phase 1 list response for it. |
| `series-list-option-2.html` | Not chosen. List: every Series as an equal row, with a strip of colored ticks showing the Stage kinds. |
| `series-detail-option-1.html` | **Chosen.** Detail: "How the field narrows" (up to 62 → 40 → 20 → Finale 4, bars to scale), then Stages grouped by kind. |
| `series-detail-option-2.html` | Not chosen. Detail: a month-by-month timeline of Stages with an "At a glance" sidebar that holds the admin actions. |
| `series-detail-option-3.html` | Not chosen. Detail: a summary band plus one `neba-data-table` (columns 1 to 3 primary, the rest expand on tap on phones). |
| `series-editor.html` | **Approved.** Create and edit (single mockup): season picker with change-and-clear confirmation, Stage grid, row errors from a failed save, locked read-only mode. |

The "up to 62" figure is worked out in the UI from the Stage config (sum of Qualifying advancing count × team size). It's a ceiling, since ties in the Backfill can push the field higher. No API change needed.

### API client

#### `Services/ApiFieldError.cs`

```csharp
using ErrorOr;

namespace Neba.Website.Server.Services;

/// <summary>One named error from an API ProblemDetails body (FastEndpoints <c>errors[].name</c>).</summary>
internal sealed record ApiFieldError(string Name, string Reason);

internal static class ApiFieldErrorExtensions
{
    public const string MetadataKey = "FieldErrors";

    extension(Error error)
    {
        /// <summary>The named errors the API returned; empty when there were none.</summary>
        public IReadOnlyList<ApiFieldError> GetFieldErrors()
            => error.Metadata?.TryGetValue(MetadataKey, out var value) == true
                && value is IReadOnlyList<ApiFieldError> fieldErrors
                ? fieldErrors
                : [];
    }
}
```

#### `Services/ApiExecutor.cs`

`TryExtractErrorDetail` also returns the named errors. The flattened description stays the same, so existing callers don't change.

```csharp
// in HandleErrorResponse, replacing the current detail block
var (detail, fieldErrors) = statusCode is >= 400 and < 500 && response.HasResponseError(out var responseError)
    ? TryExtractErrorDetail(responseError.Content)
    : (null, []);

if (detail is not null)
{
    var metadata = fieldErrors.Count > 0
        ? new Dictionary<string, object> { [ApiFieldErrorExtensions.MetadataKey] = fieldErrors }
        : null;

    return statusCode == StatusCodes.Status409Conflict
        ? Error.Conflict($"{apiName}.{operationName}.Conflict", detail, metadata)
        : Error.Validation($"{apiName}.{operationName}.Validation", detail, metadata);
}

// TryExtractErrorDetail now returns (string? Detail, IReadOnlyList<ApiFieldError> FieldErrors)
private (string? Detail, IReadOnlyList<ApiFieldError> FieldErrors) TryExtractErrorDetail(string? content)
{
    if (string.IsNullOrWhiteSpace(content))
    {
        return (null, []);
    }

    try
    {
        var payload = JsonSerializer.Deserialize<ProblemDetailsPayload>(content, JsonSerializerOptions.Web);

        var errors = payload?.Errors
            .Where(error => !string.IsNullOrWhiteSpace(error.Reason))
            .ToList() ?? [];

        IReadOnlyList<ApiFieldError> fieldErrors = [.. errors
            .Where(error => !string.IsNullOrWhiteSpace(error.Name))
            .Select(error => new ApiFieldError(error.Name, error.Reason))];

        if (errors.Count > 0)
        {
            return (string.Join(" ", errors.Select(error => error.Reason)), fieldErrors);
        }

        return (string.IsNullOrWhiteSpace(payload?.Detail) ? null : payload.Detail, fieldErrors);
    }
    catch (JsonException ex)
    {
        logger.LogErrorDetailUnparseable(ex);
        return (null, []);
    }
}

private sealed class ProblemDetailsError
{
    public string Name { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;
}
```

#### `Services/ApiServicesConfiguration.cs`

```csharp
services.RegisterApiEndpoint<ISeriesApi>();
```

### View models, display rules, mapping

#### `Series/SeriesSummaryViewModel.cs`, `Series/SeriesDetailViewModel.cs`, `Series/SeriesStageViewModel.cs`

```csharp
namespace Neba.Website.Server.Series;

internal sealed record SeriesSummaryViewModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int SeasonYear { get; init; }
    public required DateOnly SeasonEndDate { get; init; }
    public required string SeasonDateRange { get; init; }
    public required int QualifyingStageCount { get; init; }
    public required int SurvivorStageCount { get; init; }
    public required int PlacementStageCount { get; init; }
    public required bool IsLocked { get; init; }
    public SeriesNextStageViewModel? NextStage { get; init; }
}

internal sealed record SeriesNextStageViewModel
{
    public required string Kind { get; init; }
    public required string TournamentName { get; init; }
    public required string Dates { get; init; }
}

internal sealed record SeriesDetailViewModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string SeasonId { get; init; }
    public required string SeasonDescription { get; init; }
    public required string SeasonDateRange { get; init; }
    public required bool IsLocked { get; init; }
    public required IReadOnlyList<SeriesStageViewModel> Stages { get; init; }
}

internal sealed record SeriesStageViewModel
{
    public required string Id { get; init; }
    public required int Order { get; init; }
    public required string Kind { get; init; }
    public required int AdvancingCount { get; init; }
    public DateOnly? SquadDate { get; init; }
    public TimeOnly? SquadStartTime { get; init; }
    public required string TournamentId { get; init; }
    public required string TournamentName { get; init; }
    public required DateOnly TournamentStartDate { get; init; }
    public required DateOnly TournamentEndDate { get; init; }
    public required int TeamSize { get; init; }
}
```

#### `Series/StageKinds.cs`

```csharp
namespace Neba.Website.Server.Series;

/// <summary>Stage kind names as the API sends them (SmartEnum names, both directions).</summary>
internal static class StageKinds
{
    public const string Qualifying = "Qualifying";
    public const string Survivor = "Survivor";
    public const string Placement = "Placement";

    public static readonly IReadOnlyList<string> All = [Qualifying, Survivor, Placement];
}
```

#### `Series/SeriesDisplay.cs`

All display rules live here, so Razor markup holds no comparisons or string interpolation (Razor learning).

```csharp
using System.Globalization;

using Humanizer;

namespace Neba.Website.Server.Series;

internal sealed record AdvancingLabel(string Primary, string? Secondary);

internal sealed record FieldStep(string Label, string Detail, int Count, string Kind, bool IsCeiling);

internal static class SeriesDisplay
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    /// <summary>"1/1/2025 to 12/31/2025".</summary>
    public static string SeasonRange(DateOnly start, DateOnly end)
        => start.ToString("M/d/yyyy", Culture) + " to " + end.ToString("M/d/yyyy", Culture);

    /// <summary>"1/24 to 1/25", or "2/21" for a one-day tournament.</summary>
    public static string TournamentDates(DateOnly start, DateOnly end)
        => start == end
            ? start.ToString("M/d", Culture)
            : start.ToString("M/d", Culture) + " to " + end.ToString("M/d", Culture);

    /// <summary>"Top 8"; "Top 2 teams" + "(6 bowlers)"; "40 advance"; "Top 4 to the Finale".</summary>
    public static AdvancingLabel Advancing(string kind, int count, int teamSize)
        => kind switch
        {
            StageKinds.Qualifying when teamSize > 1 => new AdvancingLabel(
                "Top " + "team".ToQuantity(count),
                "(" + "bowler".ToQuantity(count * teamSize) + ")"),
            StageKinds.Qualifying => new AdvancingLabel("Top " + count.ToString(Culture), null),
            StageKinds.Survivor => new AdvancingLabel(count.ToString(Culture) + " advance", null),
            _ => new AdvancingLabel("Top " + count.ToString(Culture) + " to the Finale", null),
        };

    /// <summary>"9:00 AM squad on 7/12"; null when the Stage has no squad.</summary>
    public static string? Squad(DateOnly? date, TimeOnly? startTime)
        => date is { } d && startTime is { } t
            ? t.ToString("h:mm tt", Culture) + " squad on " + d.ToString("M/d", Culture)
            : null;

    /// <summary>"Teams of 3" + "(6 bowlers)" under a Qualifying row's advancing input.</summary>
    public static AdvancingLabel? TeamHint(string kind, int count, int teamSize)
        => kind == StageKinds.Qualifying && teamSize > 1
            ? new AdvancingLabel("Teams of " + teamSize.ToString(Culture), "(" + "bowler".ToQuantity(count * teamSize) + ")")
            : null;

    /// <summary>The most bowlers who can qualify: each Qualifying Stage's count times its team size.</summary>
    public static int QualifierCeiling(IEnumerable<SeriesStageViewModel> stages)
        => stages.Where(s => s.Kind == StageKinds.Qualifying).Sum(s => s.AdvancingCount * s.TeamSize);

    /// <summary>Steps for "How the field narrows". Only steps that exist are returned.</summary>
    public static IReadOnlyList<FieldStep> FieldSteps(IReadOnlyList<SeriesStageViewModel> stages)
    {
        var steps = new List<FieldStep>();
        var qualifying = stages.Where(s => s.Kind == StageKinds.Qualifying).ToList();

        if (qualifying.Count > 0)
        {
            steps.Add(new FieldStep("Qualifiers", "event".ToQuantity(qualifying.Count) + ", top finishers",
                QualifierCeiling(qualifying), StageKinds.Qualifying, IsCeiling: true));
        }

        steps.AddRange(stages
            .Where(s => s.Kind == StageKinds.Survivor)
            .Select(s => new FieldStep(s.TournamentName, "Survivor · " + TournamentDates(s.TournamentStartDate, s.TournamentEndDate),
                s.AdvancingCount, StageKinds.Survivor, IsCeiling: false)));

        if (stages.FirstOrDefault(s => s.Kind == StageKinds.Placement) is { } placement)
        {
            steps.Add(new FieldStep("Finale", "From " + placement.TournamentName,
                placement.AdvancingCount, StageKinds.Placement, IsCeiling: false));
        }

        return steps;
    }

    /// <summary>Inline width for a field bar, scaled to the largest step, never below 8%.</summary>
    public static string BarWidthStyle(int count, int max)
        => "width:" + Math.Max(8, max == 0 ? 0 : count * 100 / max).ToString(Culture) + "%";

    public static string KindClass(string kind)
        => "kind-" + kind.ToLowerInvariant();

    public static string GroupHeading(string kind)
        => kind switch
        {
            StageKinds.Qualifying => "Qualifying events",
            StageKinds.Survivor => "Survivor rounds",
            _ => "Placement round",
        };

    public static string GroupNote(string kind)
        => kind switch
        {
            StageKinds.Qualifying => "Top finishers enter the Series",
            StageKinds.Survivor => "Cut by the designated squad score",
            _ => "Ranked by tournament position",
        };

    /// <summary>"1 Stage needs changes…" / "2 Stages need changes…" for the editor banner.</summary>
    public static string RowsNeedChanges(int count)
        => "Stage".ToQuantity(count) + (count == 1 ? " needs" : " need") + " changes before this Series can be saved.";

    /// <summary>"Sat 7/12 · Callahan's Bowl-O-Rama" for the tournament picker.</summary>
    public static string TournamentOption(DateOnly start, string name)
        => start.ToString("ddd M/d", Culture) + " · " + name;
}
```

#### `Series/SeriesMappingExtensions.cs`

```csharp
using Neba.Api.Contracts.Series.GetSeries;
using Neba.Api.Contracts.Series.ListSeries;

namespace Neba.Website.Server.Series;

internal static class SeriesMappingExtensions
{
    extension(SeriesSummaryResponse response)
    {
        public SeriesSummaryViewModel ToViewModel() =>
            new()
            {
                Id = response.Id,
                Name = response.Name,
                SeasonYear = response.SeasonStartDate.Year,
                SeasonEndDate = response.SeasonEndDate,
                SeasonDateRange = SeriesDisplay.SeasonRange(response.SeasonStartDate, response.SeasonEndDate),
                QualifyingStageCount = response.QualifyingStageCount,
                SurvivorStageCount = response.SurvivorStageCount,
                PlacementStageCount = response.PlacementStageCount,
                IsLocked = response.IsLocked,
                NextStage = response.NextStage is { } next
                    ? new SeriesNextStageViewModel
                    {
                        Kind = next.Kind,
                        TournamentName = next.TournamentName,
                        Dates = SeriesDisplay.TournamentDates(next.TournamentStartDate, next.TournamentEndDate),
                    }
                    : null,
            };
    }

    extension(SeriesDetailResponse response)
    {
        public SeriesDetailViewModel ToViewModel() =>
            new()
            {
                Id = response.Id,
                Name = response.Name,
                SeasonId = response.SeasonId,
                SeasonDescription = response.SeasonDescription,
                SeasonDateRange = SeriesDisplay.SeasonRange(response.SeasonStartDate, response.SeasonEndDate),
                IsLocked = response.IsLocked,
                Stages = [.. response.Stages.OrderBy(s => s.Order).Select(s => new SeriesStageViewModel
                {
                    Id = s.Id,
                    Order = s.Order,
                    Kind = s.Kind,
                    AdvancingCount = s.AdvancingCount,
                    SquadDate = s.SquadDate,
                    SquadStartTime = s.SquadStartTime,
                    TournamentId = s.TournamentId,
                    TournamentName = s.TournamentName,
                    TournamentStartDate = s.TournamentStartDate,
                    TournamentEndDate = s.TournamentEndDate,
                    TeamSize = s.TeamSize,
                })],
            };
    }
}
```

#### `Series/SeriesFormModel.cs`, `Series/SeriesStageRowModel.cs`, `Series/SeriesTournamentOption.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace Neba.Website.Server.Series;

internal sealed class SeriesFormModel
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(127, ErrorMessage = "Name must be 127 characters or fewer.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Set on create; read-only on edit.</summary>
    [Required(ErrorMessage = "Season is required.")]
    public string? SeasonId { get; set; }

    public List<SeriesStageRowModel> Stages { get; } = [];
}

internal sealed class SeriesStageRowModel
{
    /// <summary>Stable key for Razor <c>@key</c>; never sent to the API.</summary>
    public Guid RowKey { get; } = Guid.NewGuid();

    /// <summary>Existing Stage ID on edit; null for a new row.</summary>
    public string? StageId { get; init; }

    public string Kind { get; set; } = StageKinds.Qualifying;
    public string? TournamentId { get; set; }
    public int AdvancingCount { get; set; } = 8;
    public DateOnly? SquadDate { get; set; }
    public TimeOnly? SquadStartTime { get; set; }

    /// <summary>Server errors named <c>Series.Stages[i]</c> for this row.</summary>
    public List<string> Errors { get; } = [];
}

internal sealed record SeriesTournamentOption(
    string Id, string Label, DateOnly StartDate, DateOnly EndDate, int TeamSize);
```

#### `Series/SeriesInputMapping.cs`

```csharp
using Neba.Api.Contracts.Series.CreateSeries;
using Neba.Api.Contracts.Series.EditSeries;

namespace Neba.Website.Server.Series;

internal static class SeriesInputMapping
{
    public static SeriesInput ToCreateInput(SeriesFormModel model)
        => new() { Name = model.Name.Trim(), SeasonId = model.SeasonId!, Stages = ToStageInputs(model) };

    public static EditSeriesInput ToEditInput(SeriesFormModel model)
        => new() { Name = model.Name.Trim(), Stages = ToStageInputs(model) };

    private static IReadOnlyList<SeriesStageInput> ToStageInputs(SeriesFormModel model)
        => [.. model.Stages.Select(row => new SeriesStageInput
        {
            Id = row.StageId,
            Kind = row.Kind,
            TournamentId = row.TournamentId ?? string.Empty,
            AdvancingCount = row.AdvancingCount,
            SquadDate = row.Kind == StageKinds.Survivor ? row.SquadDate : null,
            SquadStartTime = row.Kind == StageKinds.Survivor ? row.SquadStartTime : null,
        })];

    public static SeriesFormModel FromDetail(SeriesDetailViewModel detail)
    {
        var model = new SeriesFormModel { Name = detail.Name, SeasonId = detail.SeasonId };
        model.Stages.AddRange(detail.Stages.Select(stage => new SeriesStageRowModel
        {
            StageId = stage.Id,
            Kind = stage.Kind,
            TournamentId = stage.TournamentId,
            AdvancingCount = stage.AdvancingCount,
            SquadDate = stage.SquadDate,
            SquadStartTime = stage.SquadStartTime,
        }));
        return model;
    }

    /// <summary>Routes <c>Series.Stages[i]</c> errors onto rows; returns the rest for the banner.</summary>
    public static IReadOnlyList<string> ApplyFieldErrors(SeriesFormModel model, IReadOnlyList<Services.ApiFieldError> errors)
    {
        foreach (var row in model.Stages)
        {
            row.Errors.Clear();
        }

        var general = new List<string>();

        foreach (var error in errors)
        {
            if (TryRowIndex(error.Name, out var index) && index < model.Stages.Count)
            {
                model.Stages[index].Errors.Add(error.Reason);
            }
            else
            {
                general.Add(error.Reason);
            }
        }

        return general;
    }

    // "Series.Stages[3]" or "Series.Stages[3].AdvancingCount" (validator) -> 3
    private static bool TryRowIndex(string name, out int index)
    {
        index = -1;
        const string prefix = "Series.Stages[";
        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var close = name.IndexOf(']', prefix.Length);
        return close > prefix.Length
            && int.TryParse(name.AsSpan(prefix.Length, close - prefix.Length), out index);
    }
}
```

#### `Series/SeriesTournamentOptionsLoader.cs`

Builds the picker list from existing resources (the pragmatism guardrail: no screen-specific endpoint).

```csharp
using ErrorOr;

using Neba.Api.Contracts.Seasons;
using Neba.Api.Contracts.Series;

using Neba.Website.Server.Services;

namespace Neba.Website.Server.Series;

internal sealed class SeriesTournamentOptionsLoader(ApiExecutor apiExecutor, ISeasonsApi seasonsApi, ISeriesApi seriesApi)
{
    /// <summary>
    /// The season's tournaments, minus any held by another Series and cancelled ones, sorted by start date.
    /// Also returns how many were left out, for the grid's note.
    /// </summary>
    public async Task<ErrorOr<(IReadOnlyList<SeriesTournamentOption> Options, int HeldElsewhere)>> LoadAsync(
        string seasonId, string? editingSeriesId, CancellationToken ct = default)
    {
        var tournaments = await apiExecutor.ExecuteAsync("SeasonsApi", "ListTournamentsInSeason",
            token => seasonsApi.ListTournamentsInSeasonAsync(seasonId, token), ct);
        if (tournaments.IsError)
        {
            return tournaments.Errors;
        }

        var allSeries = await apiExecutor.ExecuteAsync("SeriesApi", "ListSeries",
            token => seriesApi.ListSeriesAsync(token), ct);
        if (allSeries.IsError)
        {
            return allSeries.Errors;
        }

        var held = new HashSet<string>(StringComparer.Ordinal);
        foreach (var other in allSeries.Value.Items.Where(s => s.SeasonId == seasonId && s.Id != editingSeriesId))
        {
            var detail = await apiExecutor.ExecuteAsync("SeriesApi", "GetSeries",
                token => seriesApi.GetSeriesAsync(other.Id, token), ct);
            if (detail.IsError)
            {
                return detail.Errors;
            }

            held.UnionWith(detail.Value.Stages.Select(stage => stage.TournamentId));
        }

        IReadOnlyList<SeriesTournamentOption> options = [.. tournaments.Value.Items
            .Where(t => t.Status != "Cancelled" && !held.Contains(t.Id))
            .OrderBy(t => t.StartDate)
            .Select(t => new SeriesTournamentOption(t.Id, SeriesDisplay.TournamentOption(t.StartDate, t.Name), t.StartDate, t.EndDate, t.TeamSize))];

        return (options, held.Count);
    }
}
```

Registered in `Program.cs`: `builder.Services.AddScoped<SeriesTournamentOptionsLoader>();`.

### Pages

#### `Series/SeriesList.razor` (mockup: list option 1)

```razor
@page "/series"
@using Neba.Api.Contracts.Security
@using Neba.Api.Contracts.Series
@using Neba.Website.Server.Services
@using Neba.Website.Server.Time
@rendermode InteractiveServer

@inject ApiExecutor ApiExecutor
@inject ISeriesApi SeriesApi
@inject TimeProvider TimeProvider
@inject IClientTimeZoneService ClientTimeZoneService

<PageTitle>Series - BowlNEBA</PageTitle>

<div class="neba-space-y-6">
    <div class="page-title-bar">
        <div class="page-title-inner">
            <h1>Series</h1>
            <p>The Vincent Hall Memorial Series runs across designated NEBA tournaments each season. Qualify at the early events, survive the cuts, and finish in the money.</p>
        </div>
    </div>

    @if (_isLoading)
    {
        <NebaSkeletonLoader Type="SkeletonType.Card" Count="2" />
    }
    else if (_hasError)
    {
        <NebaAlert Severity="NotifySeverity.Error" Title="Unable to load Series" Message="Something went wrong while loading the Series. Try again later." />
    }
    else if (_current.Count == 0 && _past.Count == 0)
    {
        <div class="neba-card series-empty">
            <h2>No Series yet</h2>
            <p>When a Series is set up for a season, its qualifying events and cuts appear here.</p>
        </div>
    }
    else
    {
        @foreach (var series in _current)
        {
            <a class="neba-card series-current" href="@SeriesHref(series)">
                <div>
                    <p class="series-current__season">@series.SeasonDateRange</p>
                    <h2 class="series-current__name">@series.Name</h2>
                    <div class="series-kind-counts">
                        <span class="kind-chip kind-qualifying">@series.QualifyingStageCount Qualifying</span>
                        <span class="kind-chip kind-survivor">@series.SurvivorStageCount Survivor</span>
                        <span class="kind-chip kind-placement">@series.PlacementStageCount Placement</span>
                    </div>
                </div>
                @if (series.NextStage is { } next)
                {
                    <div class="series-current__next">
                        <p class="series-current__next-label">Next Stage</p>
                        <p class="series-current__next-name">@next.TournamentName</p>
                        <p class="series-current__next-date">@next.Kind · @next.Dates</p>
                    </div>
                }
            </a>
        }

        @if (_past.Count > 0)
        {
            <h2 class="series-past-heading">Past Series</h2>
            <ul class="neba-card series-past">
                @foreach (var series in _past)
                {
                    <li>
                        <a href="@SeriesHref(series)">
                            <span class="series-past__year">@series.SeasonYear</span>
                            <span class="series-past__name">@series.Name <span class="series-muted">· @series.SeasonDateRange</span></span>
                            @if (series.IsLocked)
                            {
                                <AuthorizeView Policy="@Permissions.EditSeries.PolicyName">
                                    <span class="series-locked-badge">Locked</span>
                                </AuthorizeView>
                            }
                        </a>
                    </li>
                }
            </ul>
        }
    }
</div>

<AuthorizeView Policy="@Permissions.CreateSeries.PolicyName">
    <FabCreateButton Href="/series/new" Label="Create Series" />
</AuthorizeView>

@code {
    private bool _isLoading = true;
    private bool _hasError;
    private List<SeriesSummaryViewModel> _current = [];
    private List<SeriesSummaryViewModel> _past = [];

    protected override async Task OnInitializedAsync()
    {
        var result = await ApiExecutor.ExecuteAsync("SeriesApi", "ListSeries", ct => SeriesApi.ListSeriesAsync(ct));

        if (result.IsError)
        {
            _hasError = true;
            _isLoading = false;
            return;
        }

        var local = await ClientTimeZoneService.ToLocalAsync(TimeProvider.GetUtcNow());
        var today = DateOnly.FromDateTime(local.DateTime);
        var all = result.Value.Items.Select(s => s.ToViewModel()).ToList();

        _current = [.. all.Where(s => s.SeasonEndDate >= today)];
        _past = [.. all.Where(s => s.SeasonEndDate < today)];
        _isLoading = false;
    }

    private static string SeriesHref(SeriesSummaryViewModel series) => "/series/" + series.Id;
}
```

The `<` comparison in `_past` sits inside a method body, not a `< N =>` lambda in markup, so the Razor parser is fine. If the analyzer flags it, move the split into `SeriesDisplay.SplitCurrent(all, today)`.

`Series/SeriesList.razor.css` carries the mockup's `.current`, `.next-stage`, `.past-list`, and empty-state rules, renamed to the `series-*` classes above. The gold left edge (`border-left: 6px solid var(--neba-gold)`) marks a current Series. Phone layout: the next-Stage block stacks under the name.

#### `Series/SeriesDetail.razor` (mockup: detail option 1)

```razor
@page "/series/{Id}"
@using Neba.Api.Contracts.Security
@using Neba.Api.Contracts.Series
@using Neba.Website.Server.Services
@rendermode InteractiveServer

@inject ApiExecutor ApiExecutor
@inject ISeriesApi SeriesApi
@inject NavigationManager Navigation
@inject ToastService ToastService

<PageTitle>@PageTitleText - BowlNEBA</PageTitle>

@if (_isLoading)
{
    <div class="series-detail-loading"><div class="neba-spinner"></div></div>
}
else if (_model is null)
{
    <main class="neba-content-wrapper series-detail">
        <p class="series-detail__message">@(_notFound ? "This Series doesn't exist. It may have been deleted." : "Something went wrong while loading this Series. Try again later.")</p>
        <a href="/series" class="neba-link">Back to Series</a>
    </main>
}
else
{
    <div class="neba-space-y-6 series-detail">
        <div class="page-title-bar">
            <div class="page-title-inner">
                <h1>@_model.Name</h1>
                <p>@_model.SeasonDateRange</p>
            </div>
            <div class="series-detail__actions">
                <AuthorizeView Policy="@Permissions.EditSeries.PolicyName">
                    <a href="@EditHref" class="neba-btn neba-btn-secondary">Edit</a>
                </AuthorizeView>
                <AuthorizeView Policy="@Permissions.DeleteSeries.PolicyName">
                    <button type="button" class="neba-btn neba-btn-danger" disabled="@_model.IsLocked" @onclick="@(() => _isDeleteConfirmOpen = true)">Delete</button>
                    <HelpButton DocName="delete-series" Light="true" />
                    @if (_model.IsLocked)
                    {
                        <p class="series-detail__lock-note">Processing has begun, so this Series can't be deleted.</p>
                    }
                </AuthorizeView>
            </div>
        </div>

        @if (_steps.Count > 0)
        {
            <SeriesFieldChart Steps="@_steps" />
        }

        @foreach (var kind in StageKinds.All)
        {
            var stages = StagesOf(kind);
            if (stages.Count == 0)
            {
                continue;
            }

            <section aria-labelledby="@GroupId(kind)">
                <h2 class="series-group-title" id="@GroupId(kind)">@SeriesDisplay.GroupHeading(kind) <span>@SeriesDisplay.GroupNote(kind)</span></h2>
                <ol class="neba-card series-stage-list">
                    @foreach (var stage in stages)
                    {
                        var advancing = SeriesDisplay.Advancing(stage.Kind, stage.AdvancingCount, stage.TeamSize);
                        <li @key="stage.Id">
                            <span class="series-stage__order">@stage.Order</span>
                            <span class="series-stage__tournament">
                                <a href="@TournamentHref(stage)">@stage.TournamentName</a>
                                <small>@TournamentMeta(stage)</small>
                            </span>
                            <span class="series-stage__advancing">
                                @advancing.Primary
                                @if (advancing.Secondary is not null)
                                {
                                    <small class="series-stage__advancing-sub">@advancing.Secondary</small>
                                }
                            </span>
                            <span class="series-stage__squad">@SeriesDisplay.Squad(stage.SquadDate, stage.SquadStartTime)</span>
                        </li>
                    }
                </ol>
            </section>
        }
    </div>

    <ConfirmActionModal IsOpen="@_isDeleteConfirmOpen"
                        Title="Delete Series?"
                        Message="@DeleteMessage"
                        IsBusy="@_isDeleteBusy"
                        OnConfirm="ConfirmDeleteAsync"
                        OnCancel="@(() => _isDeleteConfirmOpen = false)" />
}

@code {
    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    private bool _isLoading = true;
    private bool _notFound;
    private SeriesDetailViewModel? _model;
    private IReadOnlyList<FieldStep> _steps = [];
    private bool _isDeleteConfirmOpen;
    private bool _isDeleteBusy;

    private string PageTitleText => _model?.Name ?? "Series";
    private string EditHref => "/series/" + Id + "/edit";
    private string DeleteMessage => "\"" + (_model?.Name ?? string.Empty) + "\" and its Stages will be deleted. The tournaments stay on the schedule.";

    protected override async Task OnParametersSetAsync()
    {
        _isLoading = true;
        var result = await ApiExecutor.ExecuteAsync("SeriesApi", "GetSeries", ct => SeriesApi.GetSeriesAsync(Id, ct));

        if (result.IsError)
        {
            _notFound = result.FirstError.Type == ErrorOr.ErrorType.NotFound;
            _model = null;
        }
        else
        {
            _model = result.Value.ToViewModel();
            _steps = SeriesDisplay.FieldSteps(_model.Stages);
        }

        _isLoading = false;
    }

    private List<SeriesStageViewModel> StagesOf(string kind) => [.. _model!.Stages.Where(s => s.Kind == kind)];

    private static string GroupId(string kind) => "series-group-" + kind.ToLowerInvariant();

    private static string TournamentHref(SeriesStageViewModel stage) => "/tournaments/" + stage.TournamentId;

    private static string TournamentMeta(SeriesStageViewModel stage)
    {
        var dates = SeriesDisplay.TournamentDates(stage.TournamentStartDate, stage.TournamentEndDate);
        return stage.TeamSize > 1 ? dates + " · " + stage.TeamSize + "-bowler teams" : dates;
    }

    private async Task ConfirmDeleteAsync()
    {
        _isDeleteBusy = true;
        var result = await ApiExecutor.ExecuteAsync("SeriesApi", "DeleteSeries", ct => SeriesApi.DeleteSeriesAsync(Id, ct));
        _isDeleteBusy = false;
        _isDeleteConfirmOpen = false;

        if (result.IsError)
        {
            ToastService.Show("Delete Failed", result.FirstError.Description, NotifySeverity.Error);
            return;
        }

        ToastService.Show("Series Deleted", "\"" + _model!.Name + "\" was deleted.", NotifySeverity.Success);
        Navigation.NavigateTo("/series");
    }
}
```

`Series/SeriesDetail.razor.css` ports the mockup's `.stage-list`, `.section-title`, and phone rules (`grid-template-columns: 28px 1fr`, advancing and squad wrap under the tournament). `.series-stage__advancing-sub` is the muted second line (`display:block; font-weight:400; font-size:13px`).

#### `Series/CreateSeries.razor`

```razor
@page "/series/new"
@using Neba.Api.Contracts.Seasons
@using Neba.Api.Contracts.Security
@using Neba.Website.Server.Services
@using Neba.Website.Server.Time
@rendermode InteractiveServer

@inject ApiExecutor ApiExecutor
@inject ISeasonsApi SeasonsApi
@inject TimeProvider TimeProvider
@inject IClientTimeZoneService ClientTimeZoneService

<PageTitle>Create Series - BowlNEBA</PageTitle>

<AuthorizeView Policy="@Permissions.CreateSeries.PolicyName" Context="authContext">
    <Authorized>
        <div class="neba-space-y-6">
            <div class="page-title-bar">
                <div class="page-title-inner">
                    <h1>Create Series</h1>
                    <p>Set the Stages in order. Each Stage is one tournament's role in the Series.</p>
                </div>
                <div class="page-title-help"><HelpButton DocName="create-series" Light="true" /></div>
            </div>

            @if (_seasons is null)
            {
                <NebaSkeletonLoader Type="SkeletonType.Card" Count="1" />
            }
            else
            {
                <SeriesEditor Model="@_model" Seasons="@_seasons" IsCreate="true" IsLocked="false" />
            }
        </div>
    </Authorized>
    <NotAuthorized>
        <p class="neba-content-wrapper">You don't have permission to create a Series.</p>
    </NotAuthorized>
</AuthorizeView>

@code {
    private readonly SeriesFormModel _model = new();
    private IReadOnlyList<SeasonOption>? _seasons;

    protected override async Task OnInitializedAsync()
    {
        var result = await ApiExecutor.ExecuteAsync("SeasonsApi", "ListSeasons", ct => SeasonsApi.ListSeasonsAsync(ct));
        var seasons = result.IsError ? [] : result.Value.Items.Select(SeasonOption.From).OrderByDescending(s => s.StartDate).ToList();

        var local = await ClientTimeZoneService.ToLocalAsync(TimeProvider.GetUtcNow());
        _model.SeasonId = SeasonOption.Default(seasons, DateOnly.FromDateTime(local.DateTime))?.Id;
        _seasons = seasons;
    }
}
```

`Series/SeasonOption.cs`:

```csharp
using Neba.Api.Contracts.Seasons.ListSeasons;

namespace Neba.Website.Server.Series;

internal sealed record SeasonOption(string Id, string Label, DateOnly StartDate, DateOnly EndDate)
{
    public static SeasonOption From(SeasonResponse season)
        => new(season.Id, season.Description + " (" + SeriesDisplay.SeasonRange(season.StartDate, season.EndDate) + ")", season.StartDate, season.EndDate);

    /// <summary>The season containing today, else the next one to start, else the newest.</summary>
    public static SeasonOption? Default(IReadOnlyList<SeasonOption> seasons, DateOnly today)
        => seasons.FirstOrDefault(s => s.StartDate <= today && today <= s.EndDate)
            ?? seasons.Where(s => s.StartDate > today).MinBy(s => s.StartDate)
            ?? seasons.MaxBy(s => s.StartDate);
}
```

#### `Series/EditSeries.razor`

```razor
@page "/series/{Id}/edit"
@using Neba.Api.Contracts.Security
@using Neba.Api.Contracts.Series
@using Neba.Website.Server.Services
@rendermode InteractiveServer

@inject ApiExecutor ApiExecutor
@inject ISeriesApi SeriesApi

<PageTitle>Edit Series - BowlNEBA</PageTitle>

<AuthorizeView Policy="@Permissions.EditSeries.PolicyName" Context="authContext">
    <Authorized>
        <div class="neba-space-y-6">
            <div class="page-title-bar">
                <div class="page-title-inner">
                    <h1>Edit Series</h1>
                    <p>Set the Stages in order. Each Stage is one tournament's role in the Series.</p>
                </div>
                <div class="page-title-help"><HelpButton DocName="edit-series" Light="true" /></div>
            </div>

            @if (_loadError is not null)
            {
                <NebaAlert Severity="NotifySeverity.Error" Title="Unable to load Series" Message="@_loadError" />
            }
            else if (_model is null || _detail is null)
            {
                <NebaSkeletonLoader Type="SkeletonType.Card" Count="1" />
            }
            else
            {
                <SeriesEditor Model="@_model"
                              SeriesId="@Id"
                              SeasonLabel="@SeasonLabel"
                              IsCreate="false"
                              IsLocked="@_detail.IsLocked" />
            }
        </div>
    </Authorized>
    <NotAuthorized>
        <p class="neba-content-wrapper">You don't have permission to edit this Series.</p>
    </NotAuthorized>
</AuthorizeView>

@code {
    [Parameter, EditorRequired]
    public string Id { get; set; } = string.Empty;

    private SeriesDetailViewModel? _detail;
    private SeriesFormModel? _model;
    private string? _loadError;

    private string SeasonLabel => _detail is null ? string.Empty : _detail.SeasonDescription + " (" + _detail.SeasonDateRange + ")";

    protected override async Task OnInitializedAsync()
    {
        var result = await ApiExecutor.ExecuteAsync("SeriesApi", "GetSeries", ct => SeriesApi.GetSeriesAsync(Id, ct));

        if (result.IsError)
        {
            _loadError = result.FirstError.Type == ErrorOr.ErrorType.NotFound
                ? "This Series doesn't exist. It may have been deleted."
                : "Something went wrong while loading this Series. Try again later.";
            return;
        }

        _detail = result.Value.ToViewModel();
        _model = SeriesInputMapping.FromDetail(_detail);
    }
}
```

### Components

#### `Series/SeriesEditor.razor`

Shared by create and edit. It owns the `EditForm`, dirty tracking, the save call, and the error routing.

```razor
@using Microsoft.AspNetCore.Components.Forms
@using Neba.Api.Contracts.Series
@using Neba.Api.Contracts.Series.CreateSeries
@using Neba.Api.Contracts.Series.EditSeries
@using Neba.Website.Server.Services
@implements IDisposable

@inject ApiExecutor ApiExecutor
@inject ISeriesApi SeriesApi
@inject SeriesTournamentOptionsLoader OptionsLoader
@inject NavigationManager NavigationManager
@inject ToastService ToastService

<DirtyFormGuard IsDirty="@_isDirty" />

<div class="neba-card">
    <EditForm EditContext="_editContext" FormName="SeriesForm" OnValidSubmit="HandleSaveAsync">
        <DataAnnotationsValidator />
        <div class="neba-space-y-6">

            @if (IsLocked)
            {
                <NebaAlert Severity="NotifySeverity.Warning" Title="This Series is locked" Message="Processing has begun for this Series. Only its name can change." />
            }

            @if (_generalErrors.Count > 0)
            {
                <NebaAlert Severity="NotifySeverity.Error" Title="Unable to save the Series" Message="@string.Join(" ", _generalErrors)" Dismissible="true"
                           OnDismiss="@(() => _generalErrors = [])" />
            }

            <div class="series-editor__field">
                <FormLabel TargetId="series-name" For="@(() => Model.Name)">Name</FormLabel>
                <InputText id="series-name" @bind-Value="Model.Name" class="neba-input" aria-describedby="series-name-error" />
                <NebaValidationMessage For="@(() => Model.Name)" Id="series-name-error" Class="block text-sm text-red-600 mt-1" />
            </div>

            @if (IsCreate)
            {
                <div class="series-editor__field">
                    <FormLabel TargetId="series-season" For="@(() => Model.SeasonId)">Season</FormLabel>
                    <select id="series-season" class="neba-select" value="@Model.SeasonId" @onchange="HandleSeasonChanging" aria-describedby="series-season-hint">
                        @foreach (var season in Seasons)
                        {
                            <option value="@season.Id">@season.Label</option>
                        }
                    </select>
                    <p id="series-season-hint" class="series-editor__hint">Fixed after the Series is created. Tournaments come from this season.</p>
                </div>
            }
            else
            {
                <div class="series-editor__field">
                    <span class="series-editor__label">Season</span>
                    <span class="series-editor__readonly">@SeasonLabel</span>
                </div>
            }

            <SeriesStageGrid Rows="@Model.Stages"
                             Options="@_options"
                             HeldElsewhere="@_heldElsewhere"
                             ReadOnly="@IsLocked"
                             OnChanged="MarkDirty" />

            <div class="series-editor__actions">
                <button type="button" class="neba-btn neba-btn-secondary" @onclick="HandleCancel">Cancel</button>
                <button type="submit" class="neba-btn neba-btn-primary" disabled="@_isSubmitting">@SaveLabel</button>
            </div>
        </div>
    </EditForm>
</div>

<ConfirmActionModal IsOpen="@(_pendingSeasonId is not null)"
                    Title="Change season?"
                    Message="Tournaments belong to one season, so changing it clears the Stages you've added."
                    ConfirmLabel="Change and clear Stages"
                    BusyLabel="Changing..."
                    CancelLabel="Keep season"
                    IsBusy="false"
                    OnConfirm="ConfirmSeasonChangeAsync"
                    OnCancel="@(() => _pendingSeasonId = null)" />

@code {
    [Parameter, EditorRequired]
    public SeriesFormModel Model { get; set; } = default!;

    [Parameter, EditorRequired]
    public bool IsCreate { get; set; }

    [Parameter, EditorRequired]
    public bool IsLocked { get; set; }

    /// <summary>Create only.</summary>
    [Parameter]
    public IReadOnlyList<SeasonOption> Seasons { get; set; } = [];

    /// <summary>Edit only.</summary>
    [Parameter]
    public string? SeriesId { get; set; }

    /// <summary>Edit only.</summary>
    [Parameter]
    public string SeasonLabel { get; set; } = string.Empty;

    private EditContext _editContext = default!;
    private bool _isDirty;
    private bool _isSubmitting;
    private IReadOnlyList<SeriesTournamentOption> _options = [];
    private int _heldElsewhere;
    private List<string> _generalErrors = [];
    private string? _pendingSeasonId;

    private string SaveLabel => IsCreate ? "Create Series" : "Save changes";

    protected override async Task OnInitializedAsync()
    {
        _editContext = new EditContext(Model);
        _editContext.OnFieldChanged += HandleFieldChanged;
        await LoadOptionsAsync();
    }

    public void Dispose() => _editContext.OnFieldChanged -= HandleFieldChanged;

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs e) => MarkDirty();

    private void MarkDirty() => _isDirty = true;

    private async Task LoadOptionsAsync()
    {
        if (string.IsNullOrEmpty(Model.SeasonId) || IsLocked)
        {
            _options = [];
            return;
        }

        var result = await OptionsLoader.LoadAsync(Model.SeasonId, SeriesId);
        if (result.IsError)
        {
            _generalErrors = [result.FirstError.Description];
            return;
        }

        (_options, _heldElsewhere) = result.Value;
    }

    private async Task HandleSeasonChanging(ChangeEventArgs e)
    {
        var seasonId = e.Value?.ToString();
        if (seasonId == Model.SeasonId)
        {
            return;
        }

        if (Model.Stages.Count > 0)
        {
            _pendingSeasonId = seasonId; // confirmation modal; the select re-renders with the old value
            return;
        }

        Model.SeasonId = seasonId;
        MarkDirty();
        await LoadOptionsAsync();
    }

    private async Task ConfirmSeasonChangeAsync()
    {
        Model.SeasonId = _pendingSeasonId;
        Model.Stages.Clear();
        _pendingSeasonId = null;
        MarkDirty();
        await LoadOptionsAsync();
    }

    private async Task HandleSaveAsync()
    {
        _isSubmitting = true;
        _generalErrors = [];

        ErrorOr.ErrorOr<string> result;
        if (IsCreate)
        {
            var request = new CreateSeriesRequest { Series = SeriesInputMapping.ToCreateInput(Model) };
            var created = await ApiExecutor.ExecuteAsync("SeriesApi", "CreateSeries", ct => SeriesApi.CreateSeriesAsync(request, ct));
            result = created.IsError ? created.Errors : created.Value.SeriesId;
        }
        else
        {
            var request = new EditSeriesRequest { Id = SeriesId!, Series = SeriesInputMapping.ToEditInput(Model) };
            var edited = await ApiExecutor.ExecuteAsync("SeriesApi", "EditSeries", ct => SeriesApi.EditSeriesAsync(SeriesId!, request, ct));
            result = edited.IsError ? edited.Errors : SeriesId!;
        }

        _isSubmitting = false;

        if (result.IsError)
        {
            var fieldErrors = result.FirstError.GetFieldErrors();
            _generalErrors = fieldErrors.Count == 0
                ? [result.FirstError.Description]
                : [.. SeriesInputMapping.ApplyFieldErrors(Model, fieldErrors)];

            var rowsWithErrors = Model.Stages.Count(row => row.Errors.Count > 0);
            if (rowsWithErrors > 0 && _generalErrors.Count == 0)
            {
                _generalErrors = [SeriesDisplay.RowsNeedChanges(rowsWithErrors)];
            }

            return;
        }

        _isDirty = false;
        ToastService.Show(IsCreate ? "Series Created" : "Series Saved", "\"" + Model.Name.Trim() + "\" was saved.", NotifySeverity.Success);

        // Same as EditTournament: let the guard see IsDirty=false before navigating.
        StateHasChanged();
        await Task.Yield();

        NavigationManager.NavigateTo("/series/" + result.Value);
    }

    private void HandleCancel()
        => NavigationManager.NavigateTo(IsCreate ? "/series" : "/series/" + SeriesId);
}
```

#### `Series/SeriesStageGrid.razor`

```razor
@using Microsoft.AspNetCore.Components.Forms

<div class="series-grid">
    <p class="series-editor__label">Stages <small>(order must follow tournament dates: Qualifying, then Survivor, then Placement)</small></p>

    <div class="series-grid__head" aria-hidden="true">
        <span>#</span><span>Kind</span><span>Tournament</span><span>Advancing</span><span>Squad (Survivor only)</span><span></span>
    </div>

    <ol class="series-grid__rows">
        @for (var index = 0; index < Rows.Count; index++)
        {
            var i = index;
            var row = Rows[i];
            var option = OptionFor(row);
            var order = i + 1;

            <li @key="row.RowKey" class="@RowClass(row)">
                <span class="series-grid__order">@order</span>

                @if (ReadOnly)
                {
                    <span class="series-grid__ro">@row.Kind</span>
                    <span class="series-grid__ro series-grid__tournament">@(option?.Label ?? "—")</span>
                    <span class="series-grid__ro">@ReadOnlyCount(row, option)</span>
                    <span class="series-grid__ro series-grid__squad">@(SeriesDisplay.Squad(row.SquadDate, row.SquadStartTime) ?? "—")</span>
                    <span></span>
                }
                else
                {
                    <div>
                        <label class="series-grid__cell-label" for="@FieldId("kind", i)">Kind</label>
                        <select id="@FieldId("kind", i)" class="neba-select" value="@row.Kind" @onchange="@(e => HandleKindChanged(row, e))">
                            @foreach (var kind in StageKinds.All)
                            {
                                <option value="@kind">@kind</option>
                            }
                        </select>
                    </div>

                    <div class="series-grid__tournament">
                        <label class="series-grid__cell-label" for="@FieldId("tournament", i)">Tournament</label>
                        <select id="@FieldId("tournament", i)" class="neba-select" value="@row.TournamentId" @onchange="@(e => HandleTournamentChanged(row, e))">
                            <option value="">Choose a tournament</option>
                            @foreach (var choice in ChoicesFor(row))
                            {
                                <option value="@choice.Id">@choice.Label</option>
                            }
                        </select>
                    </div>

                    <div>
                        <label class="series-grid__cell-label" for="@FieldId("count", i)">Advancing</label>
                        <input id="@FieldId("count", i)" type="number" min="1" class="neba-input" value="@row.AdvancingCount" @onchange="@(e => HandleCountChanged(row, e))" />
                        @if (SeriesDisplay.TeamHint(row.Kind, row.AdvancingCount, option?.TeamSize ?? 1) is { } hint)
                        {
                            <div class="series-editor__hint">@hint.Primary<br />@hint.Secondary</div>
                        }
                        else if (row.Kind == StageKinds.Placement)
                        {
                            <div class="series-editor__hint">Finale size</div>
                        }
                    </div>

                    <div class="series-grid__squad">
                        <span class="series-grid__cell-label">Squad</span>
                        @if (row.Kind == StageKinds.Survivor)
                        {
                            <div class="series-grid__squad-inputs">
                                <NebaDateInput id="@FieldId("squad-date", i)" Value="@row.SquadDate" ValueChanged="@((DateOnly? d) => HandleSquadDateChanged(row, d))" ValueExpression="@(() => row.SquadDate)" aria-label="@AriaLabel("squad date", order)" />
                                <input id="@FieldId("squad-time", i)" type="time" class="neba-input" value="@TimeValue(row)" @onchange="@(e => HandleSquadTimeChanged(row, e))" aria-label="@AriaLabel("squad start time", order)" />
                            </div>
                        }
                        else
                        {
                            <span class="series-grid__no-squad">No squad</span>
                        }
                    </div>

                    <div class="series-grid__actions">
                        <button type="button" class="neba-btn-icon" disabled="@(i == 0)" @onclick="@(() => Move(i, -1))" aria-label="@AriaLabel("move up", order)">↑</button>
                        <button type="button" class="neba-btn-icon" disabled="@IsLast(i)" @onclick="@(() => Move(i, 1))" aria-label="@AriaLabel("move down", order)">↓</button>
                        <button type="button" class="neba-btn-icon" @onclick="@(() => Remove(i))" aria-label="@AriaLabel("remove", order)">✕</button>
                    </div>
                }

                @foreach (var error in row.Errors)
                {
                    <p class="series-grid__error" role="alert">@error</p>
                }
            </li>
        }
    </ol>

    @if (!ReadOnly)
    {
        <div class="series-grid__foot">
            <button type="button" class="neba-btn neba-btn-secondary" @onclick="Add">+ Add Stage</button>
            @if (HeldElsewhere > 0)
            {
                <span class="series-editor__hint">@HeldElsewhereNote</span>
            }
        </div>
    }
</div>

@code {
    [Parameter, EditorRequired]
    public List<SeriesStageRowModel> Rows { get; set; } = default!;

    [Parameter, EditorRequired]
    public IReadOnlyList<SeriesTournamentOption> Options { get; set; } = default!;

    [Parameter, EditorRequired]
    public bool ReadOnly { get; set; }

    [Parameter]
    public int HeldElsewhere { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnChanged { get; set; }

    private string HeldElsewhereNote
        => "tournament".ToQuantity(HeldElsewhere) + " in this season " + (HeldElsewhere == 1 ? "is" : "are") + " in another Series and not listed.";

    private SeriesTournamentOption? OptionFor(SeriesStageRowModel row)
        => Options.FirstOrDefault(o => o.Id == row.TournamentId);

    /// <summary>A tournament picked on another row drops out of this row's list.</summary>
    private IEnumerable<SeriesTournamentOption> ChoicesFor(SeriesStageRowModel row)
        => Options.Where(o => o.Id == row.TournamentId || !Rows.Any(other => other.TournamentId == o.Id));

    private static string RowClass(SeriesStageRowModel row)
        => row.Errors.Count > 0 ? "series-grid__row series-grid__row--error" : "series-grid__row";

    private static string FieldId(string field, int index) => "stage-" + index + "-" + field;

    private static string AriaLabel(string what, int order) => "Stage " + order + " " + what;

    private bool IsLast(int index) => index == Rows.Count - 1;

    private static string? TimeValue(SeriesStageRowModel row) => row.SquadStartTime?.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);

    private static string ReadOnlyCount(SeriesStageRowModel row, SeriesTournamentOption? option)
        => SeriesDisplay.Advancing(row.Kind, row.AdvancingCount, option?.TeamSize ?? 1).Primary;

    private Task HandleKindChanged(SeriesStageRowModel row, ChangeEventArgs e)
    {
        row.Kind = e.Value?.ToString() ?? StageKinds.Qualifying;

        if (row.Kind == StageKinds.Survivor)
        {
            row.SquadDate ??= OptionFor(row)?.StartDate;
        }
        else
        {
            row.SquadDate = null;
            row.SquadStartTime = null;
        }

        return ChangedAsync(row);
    }

    private Task HandleTournamentChanged(SeriesStageRowModel row, ChangeEventArgs e)
    {
        row.TournamentId = string.IsNullOrEmpty(e.Value?.ToString()) ? null : e.Value!.ToString();

        if (row.Kind == StageKinds.Survivor)
        {
            row.SquadDate = OptionFor(row)?.StartDate;
        }

        return ChangedAsync(row);
    }

    private Task HandleCountChanged(SeriesStageRowModel row, ChangeEventArgs e)
    {
        row.AdvancingCount = int.TryParse(e.Value?.ToString(), out var count) ? count : 0;
        return ChangedAsync(row);
    }

    private Task HandleSquadDateChanged(SeriesStageRowModel row, DateOnly? date)
    {
        row.SquadDate = date;
        return ChangedAsync(row);
    }

    private Task HandleSquadTimeChanged(SeriesStageRowModel row, ChangeEventArgs e)
    {
        row.SquadStartTime = TimeOnly.TryParse(e.Value?.ToString(), System.Globalization.CultureInfo.InvariantCulture, out var time) ? time : null;
        return ChangedAsync(row);
    }

    private Task Move(int index, int offset)
    {
        (Rows[index], Rows[index + offset]) = (Rows[index + offset], Rows[index]);
        ClearErrors();
        return OnChanged.InvokeAsync();
    }

    private Task Remove(int index)
    {
        Rows.RemoveAt(index);
        ClearErrors();
        return OnChanged.InvokeAsync();
    }

    private Task Add()
    {
        Rows.Add(new SeriesStageRowModel());
        return OnChanged.InvokeAsync();
    }

    /// <summary>A row's server errors refer to its old position, so a move or removal clears them all.</summary>
    private void ClearErrors()
    {
        foreach (var row in Rows)
        {
            row.Errors.Clear();
        }
    }

    private Task ChangedAsync(SeriesStageRowModel row)
    {
        row.Errors.Clear();
        return OnChanged.InvokeAsync();
    }
}
```

The `for` loop with `index < Rows.Count` is a statement, not a `< N =>` lambda, so Razor parses it. If it trips the parser, iterate `Rows.Select((row, i) => (row, i))` instead.

`Series/SeriesStageGrid.razor.css` ports the mockup grid: columns `34px 140px minmax(220px,1fr) 110px 300px 108px` with a `1fr 135px` squad pair. The error row is tinted with an inset red edge. Below 1000px the row collapses to `34px 1fr 1fr`, tournament and squad span both columns, and `.series-grid__cell-label` shows.

#### `Series/SeriesFieldChart.razor`

```razor
<section class="neba-card series-field" aria-labelledby="series-field-title">
    <h2 id="series-field-title">How the field narrows</h2>
    <p>Bowlers qualify at the early events, are cut by squad score, and the last round decides the Finale.</p>
    <div class="series-field__rows">
        @foreach (var step in Steps)
        {
            <div class="series-field__row">
                <div class="series-field__label">@step.Label<small>@step.Detail</small></div>
                <div class="@BarClass(step)" style="@SeriesDisplay.BarWidthStyle(step.Count, Max)">@BarText(step)</div>
            </div>
        }
    </div>
</section>

@code {
    [Parameter, EditorRequired]
    public IReadOnlyList<FieldStep> Steps { get; set; } = default!;

    private int Max => Steps.Count == 0 ? 0 : Steps.Max(s => s.Count);

    private static string BarClass(FieldStep step) => "series-field__bar series-field__bar--" + step.Kind.ToLowerInvariant();

    private static string BarText(FieldStep step) => step.IsCeiling ? "Up to " + step.Count : step.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
```

`Series/SeriesFieldChart.razor.css`: the mockup's `.funnel` rules. Bar colors are `--neba-blue-600` (Qualifying), `--neba-gold-deep` (Survivor), and `--neba-blue-brand` (Placement; `.dark` uses `--neba-blue-700`). Label column 170px, stacked on phones.

#### Shared styles: `wwwroot/app.css`

```css
/* Stage kind chips (Series list and detail) */
.kind-chip { display: inline-flex; align-items: center; gap: 6px; font-size: 13px; font-weight: 600; white-space: nowrap; }
.kind-chip::before { content: ""; width: 8px; height: 8px; border-radius: 2px; background: currentColor; }
.kind-qualifying { color: var(--neba-blue-600); }
.kind-survivor { color: var(--neba-gold-deep); }
.kind-placement { color: var(--neba-blue-brand); }
.dark .kind-placement { color: var(--neba-blue-300); }

.series-locked-badge { font-size: 12px; font-weight: 600; color: var(--neba-warning); background: rgb(217 119 6 / 0.1); border-radius: 999px; padding: 2px 10px; }
```

The site switches themes with a `.dark` class on the root (`neba_theme.css`), so dark overrides use `.dark …`, not a media query.

### Navigation

#### `Layout/NavMenu.razor`

```razor
<NavLink class="neba-dropdown-link" href="tournaments" Match="NavLinkMatch.All" role="menuitem">
    Schedule</NavLink>
<NavLink class="neba-dropdown-link" href="series" role="menuitem">Series</NavLink>
<div class="neba-dropdown-divider"></div>
```

### Help docs

Screenshots for `docs/help/create-series.md`, `edit-series.md` and `delete-series.md` through the `help-documentation` skill, once the pages run.

### Tests

#### bUnit (`tests/Neba.Website.Tests/Series/`)

All tests carry `[UnitTest]`, `[Component("Series")]`, a `DisplayName`, and AAA comments. API interfaces are mocked with `MockBehavior.Strict`, and `ApiExecutor` gets `NullLogger`.

- `SeriesDisplayTests`:
  - `Advancing`: Qualifying singles, Qualifying team (secondary "(6 bowlers)"), Survivor, Placement.
  - `Squad` formatting, `TournamentDates` for one-day and multi-day tournaments, `SeasonRange`.
  - `QualifierCeiling` = 62 for the 2025 config.
  - `FieldSteps`: full (62 / 40 / 20 / 4, first step `IsCeiling`), only Qualifying, no Placement.
  - `BarWidthStyle` floor of 8%.
  - `RowsNeedChanges` singular and plural.
- `SeriesMappingExtensionsTests`: summary with and without `NextStage`; detail sorted by order.
- `SeriesInputMappingTests`:
  - Row order becomes list order.
  - Squad stripped from non-Survivor rows.
  - `FromDetail` keeps Stage ids.
  - `ApplyFieldErrors` routes `Series.Stages[2]` and `Series.Stages[2].AdvancingCount` to row 2, and `Series` to the banner.
  - An out-of-range index goes to the banner.
- `SeasonOptionTests`: `Default` picks the season containing today, then the next to start, then the newest.
- `SeriesListTests`:
  - Current vs past split by season end date (`FakeTimeProvider`).
  - Next-Stage block shown only when `NextStage` is set.
  - "Past Series" hidden when there are none.
  - Empty state and error state.
  - FAB and Locked badge only with the permission (bUnit `AddAuthorization().SetPolicies(...)`).
- `SeriesDetailTests`:
  - Groups render in kind order; empty groups hidden.
  - Tournament links go to `/tournaments/{id}`.
  - Team row shows "Top 2 teams" with "(6 bowlers)" on the second line.
  - Squad label only on Survivor rows.
  - Field chart steps.
  - Edit and Delete by permission; Delete disabled with the note when locked.
  - Confirm → `DeleteSeriesAsync` → toast → navigate. A 409 shows the error toast.
  - Not-found message.
- `SeriesStageGridTests`:
  - Add, remove, move up and down; first and last buttons disabled.
  - Switching to Survivor sets the squad date to the tournament start; switching away clears it.
  - Picking a tournament removes it from other rows.
  - Team hint updates with the count.
  - Row errors render under the right row; a move clears errors.
  - `ReadOnly` renders text with no inputs.
  - `OnChanged` fires on each edit.
- `SeriesEditorTests`:
  - Create sends `CreateSeriesRequest` in row order and navigates to `/series/{id}`.
  - Edit sends `EditSeriesRequest` with Stage ids.
  - A 422 with field errors marks rows and shows the count banner.
  - A 409 with no field errors shows the banner text.
  - The season change with Stages opens the confirmation; confirming clears rows and reloads options; cancelling keeps the season.
  - The locked editor hides grid inputs and still saves the name.
  - Dirty: field change and grid change set `IsDirty`; a successful save clears it before navigating.
- `SeriesTournamentOptionsLoaderTests`: excludes tournaments held by other Series in the season (not the one being edited) and cancelled tournaments; sorts by date; reports `HeldElsewhere`.
- `ApiExecutorTests` (existing): `errors[].name` lands in `GetFieldErrors()`; the flattened description is unchanged; a body without names yields an empty list.
- `NavMenuTests` (existing, if present): the Series link is in the Tournaments dropdown.

#### View-model factories (`tests/Neba.TestFactory/Series/`)

`SeriesSummaryViewModelFactory`, `SeriesNextStageViewModelFactory`, `SeriesDetailViewModelFactory`, `SeriesStageViewModelFactory`, `SeriesTournamentOptionFactory`, `SeasonOptionFactory`. Each has `Create()` with const defaults and `Bogus(count, seed?)`. `SeriesDetailViewModelFactory.Reference2025()` returns the 11-Stage configuration.

#### Playwright (`tests/e2e/Series.spec.ts`)

- Public: `/series` shows the featured current Series with its next Stage → open it → field chart reads "Up to 62", "40", "20", "4" → a tournament link goes to `/tournaments/{id}`.
- Nav: the Tournaments dropdown has Series.
- Admin: create a Series with Qualifying, Survivor (squad date and time) and Placement rows → lands on the detail page.
- Admin: the mock returns 422 with `errors[{ name: "Series.Stages[1]", reason: "..." }]` → the message shows under row 2 (a 4xx, per the testing learning).
- Admin: delete with confirm → back on `/series`.
- Phone viewport: detail rows stack, and the editor grid collapses.

#### E2E mock API (`tests/e2e/mock-api/mock-api-server.ts`)

`GET /series` (one current Series with `nextStage`, one past), `GET /series/{id}` (the 2025 reference with all required fields), `POST /series` (201, or 422 when the name is "fail"), `PUT /series/{id}` (204), `DELETE /series/{id}` (204).

### Deferred or out of scope (this phase)

- Field, standings and payouts on the public pages (phases 2 and 3 of #257).
- Moving the admin links out of the account menu (separate concern).
- Client-side pre-validation of the Series rules. The server is the single source, and its row-named errors drive the UI.

## Commit plan

Each commit builds and passes its own tests, and holds the tests for its code. Stage only the listed files by name (never `git add -A`). Every message ends with:

```text
Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

The branch is cut from `release/v1.3.0` and the PR targets it. `<Version>` already says `1.3.0`, which a release-branch PR must match, so there is no version commit.

### Phase 1: API

1. **Add Series management plan**
   - `docs/plans/series-management.md`, `docs/plans/mockups/series-management/*`
   - Gives reviewers the scoping decisions and mockups the later commits build on.

2. **Add Series permissions**
   - `src/Neba.Api.Contracts/Security/Permission.cs`, `src/Neba.Api/Security/Infrastructure/SecurityRoleSeeder.cs`, `docs/policies/README.md`, seeder and permission tests
   - Lands the authorization model first, so every endpoint commit can gate on it. Manager gets the three permissions; payouts stay out of them.

3. **Add Series aggregate and Stage rules**
   - `src/Neba.Api/Features/Series/Domain/*` (`Series`, `SeriesStage`, `SquadDesignation`, `StageKind`, `StageStatus`, ids, `StageTournamentFacts`, `SeriesStageDefinition`, `SeriesErrors`)
   - `tests/Neba.TestFactory/Series/` domain factories (`SeriesFactory`, `SeriesStageDefinitionFactory`, `StageTournamentFactsFactory`, `SquadDesignationFactory`, `Series2025ReferenceFactory`)
   - `tests/Neba.Api.Tests/Features/Series/Domain/*`
   - The rules from #258 are the core of the feature; proving each one in isolation before persistence keeps later failures easy to place.

4. **Persist Series and Stages**
   - `Database/Configurations/SeriesConfiguration.cs`, `SeriesStageConfiguration.cs`, `Database/AppDbContext.cs`, the `AddSeries` migration, `Auditing/AuditingConfiguration.cs`
   - `SeriesConfigurationTests`, plus the `EfAuditIntegrationTests` additions
   - Ships the full Stage schema (status, nullable `squad_id`) now so phase 2 of #257 needs no data migration. The unique `tournament_id` index backs up rule #1.

5. **Add Series API contracts**
   - `src/Neba.Api.Contracts/Series/**` (`ISeriesApi`, inputs, responses including `SeriesNextStageResponse`)
   - Contract input and response factories in `tests/Neba.TestFactory/Series/`
   - One commit for the public shape, so the endpoint commits and the website both build against it.

6. **Add list and get Series endpoints**
   - `Features/Series/SeriesEndpointGroup.cs`, `Features/Series/ListSeries/*`, `Features/Series/GetSeries/*`
   - DTO factories (`SeriesSummaryDtoFactory`, `SeriesNextStageDtoFactory`, `SeriesDetailDtoFactory`, `SeriesStageDtoFactory`)
   - `ListSeriesQueryHandlerTests` (next Stage and cutoff), plus the read cases in `SeriesEndpointsTests`
   - Public reads first: they need no write path, and they let the create tests read back what they saved.

7. **Add create and edit Series endpoints**
   - `Features/Series/StageTournamentFactsReader.cs`, `SeriesStageInputMapper.cs`, `SeriesStageInputValidator.cs`, `SeriesMutationResultSender.cs`, `Features/Series/CreateSeries/*`, `Features/Series/EditSeries/*`
   - Handler, validator and sender unit tests; the create and edit cases in `SeriesEndpointsTests`; `Series2025ReferenceTests`
   - The whole-Series save is one idea; create and edit share the reader, mapper and sender. The 2025 configuration proves the acceptance criterion end to end.

8. **Add delete Series endpoint**
   - `Features/Series/DeleteSeries/*`, `DeleteSeriesCommandHandlerTests`, the delete cases in `SeriesEndpointsTests`
   - Kept separate because it carries its own conventions: an idempotent 204, and a 409 when locked.

9. **Block deleting a tournament that is a Series Stage**
   - `Features/Tournaments/Domain/TournamentErrors.cs`, `Features/Tournaments/DeleteTournament/DeleteTournamentCommandHandler.cs`, `DeleteTournamentSummary.cs`
   - The new cases in `DeleteTournamentCommandHandlerTests` and `DeleteTournamentEndpointTests`
   - A change to the Tournaments feature, so it gets its own commit for review. Without it, the FK fails as a 500.

10. **docs: update ubiquitous language - add Series terms**
    - `docs/ubiquitous-language.md` (new `## Series` section from the downloaded `vincent-hall-series-ul.md`, plus the `### Cup` clarification)
    - Brings the source doc into the repo so code names and their meaning are checked against one place. The subject follows the UL file's own commit format.

11. **Add Series admin help docs**
    - `docs/help/create-series.md`, `edit-series.md`, `delete-series.md` (text only)
    - Meets ADR-0007 alongside the API. Screenshots follow with the UI.

### Phase 2: UI

12. **Keep named ProblemDetails errors in ApiExecutor**
    - `src/Neba.Website.Server/Services/ApiFieldError.cs`, `Services/ApiExecutor.cs`, the `ApiExecutorTests` additions
    - A shared website change that other forms can reuse. Isolating it shows that existing callers see no difference.

13. **Add Series view models and display rules**
    - `Series/StageKinds.cs`, `SeriesDisplay.cs`, the view models, `SeriesMappingExtensions.cs`, `SeriesFormModel.cs`, `SeriesStageRowModel.cs`, `SeriesTournamentOption.cs`, `SeasonOption.cs`, `SeriesInputMapping.cs`, `SeriesTournamentOptionsLoader.cs`
    - `Services/ApiServicesConfiguration.cs` (`ISeriesApi`) and `Program.cs` (loader registration)
    - View-model factories, plus `SeriesDisplayTests`, `SeriesMappingExtensionsTests`, `SeriesInputMappingTests`, `SeasonOptionTests`, `SeriesTournamentOptionsLoaderTests`
    - Pure C# with no markup, so the label and error-routing rules are reviewed and tested before the pages use them.

14. **Add public Series list and detail pages**
    - `Series/SeriesList.razor(.css)`, `SeriesDetail.razor(.css)`, `SeriesFieldChart.razor(.css)`, `wwwroot/app.css` (kind chips, locked badge), `Layout/NavMenu.razor`
    - `SeriesListTests`, `SeriesDetailTests`, nav test
    - What bowlers see: the public landing pages and their nav entry, including admin delete.

15. **Add Series create and edit pages**
    - `Series/CreateSeries.razor`, `EditSeries.razor`, `SeriesEditor.razor`, `SeriesStageGrid.razor(.css)`
    - `SeriesEditorTests`, `SeriesStageGridTests`
    - The admin data-entry flow, kept apart from the public pages because it carries the dirty guard, row errors and lock handling.

16. **Add Series end-to-end tests**
    - `tests/e2e/mock-api/mock-api-server.ts`, `tests/e2e/Series.spec.ts`
    - Proves the real browser flows (public view, create, a row error from a 4xx, delete) over HTTP. Updates the mock with every required contract field so Playwright doesn't fail on a missing one.

17. **Add Series help screenshots**
    - Screenshot images and the `docs/help/*-series.md` image references (via the `help-documentation` skill)
    - Screenshots need the finished pages, so they come last.
