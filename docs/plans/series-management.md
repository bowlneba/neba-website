# Series Management (Vincent Hall Memorial Series, Phase 1)

GitHub issue: [#258](https://github.com/bowlneba/neba-website/issues/258) (parent [#257](https://github.com/bowlneba/neba-website/issues/257))

Admins create and maintain a **Series** for a season: they assign tournaments to ordered **Stages**, set each Stage's kind and advancing count, and describe the squad a Survivor Round uses. Everyone can view a Series and its Stage schedule. This phase stores configuration only. Nothing is computed from results (phase 2) and no payout is stored (phase 3).

## Decisions locked in during scoping

- **Source of truth.** `vincent-hall-series-ul.md` (attached to #257) holds the rule language, ubiquitous language, model and invariants. It wins over #258 where they differ. Code names match it; a mismatch is a defect.
- **Not critical/high-stakes.** No NFR checkpoint. Configuration only: no money, no new PII, a few Series per season with about 11 Stages each. Concurrent edits are last write wins. Audit goes through the existing Audit.NET setup.
- **Permissions.** Reads are public (`AllowAnonymous`). Writes use four new permissions: `Series.CreateSeries`, `Series.EditSeries`, `Series.DeleteSeries`, and `Series.EditSeriesLogo`. Admin gets them through `Permissions.List`; Manager is seeded with all four. No new role. Logo staging uses a `CanUploadSeriesLogo` policy (any of Create, Edit, EditSeriesLogo), mirroring `CanUploadTournamentLogo`. Phase 3 will likely add a separate money permission (such as `Series.EditPayout`), so the write permissions stay per action and must not cover payouts.
- **Finale is its own event (updated 2026-11-17; supersedes "optional Finale count on the Placement Stage").** A Series has at most one `SeriesFinale`, owned by the Series and not linked to a tournament: finalist count, date, start time, and an optional bowling center (empty means "host center to be announced", the same as a tournament with no host). A Series has a Finale exactly when it has this child (no separate flag). The Placement Stage no longer carries a count; it ranks by tournament position and nothing else. Qualifying and Survivor counts stay required and above zero. A Finale's count is above zero and no larger than the last Survivor count. **No Stage can come after the Finale:** every Stage's tournament must end before the Finale's date. Survivor Rounds are optional (if everyone who qualifies goes to the Finale, a Series can run Qualifying events straight into it), and a Series needs no Placement Stage to have a Finale. The lock covers whether a Finale exists and its count; its date, start time and center stay editable while locked, since a Finale can be rescheduled. Without a Finale, the detail page's Placement row reads "Final standings (no Finale)" and the field chart ends at the last Survivor bar, with a "No Finale" caption. The Finale also appears as its own card in the tournament list (see "Finale on the tournament list").
- **Announce applies to Qualifying and Survivor Stages only** (UL). The Placement Round has no Announce step. No Stage changes status in this phase, so it's recorded in the UL only; phase 2 enforces it.
- **Series logo, like tournaments.** `Series.Logo` (`StoredFile?`), staged through `POST /series/logo`, set on create and edit, and editable alone through `PUT /series/{id}/logo` with `Series.EditSeriesLogo`. A stored denial of that permission blocks a logo change through the full edit too, the same as tournaments. The logo is presentation only, so it stays editable while the Series is locked. No sponsor fallback (a Series has no sponsor). It reuses the shared upload helpers in `Neba.Api/Uploads` (`StoredFileClaim`, `StoredFileCleanup`, `PendingUploadCleaner`, `DeleteStoredFilesJob`).
- **Shared logo modal.** `EditTournamentLogoModal` and `TournamentLogoEditButton` move to `Components/EditLogoModal` and `Components/LogoEditButton` with neutral names; Tournaments and Series both host them. The tournament JSON contract doesn't change.
- **#258 synced with the plan.** The ticket body is updated (with the user's approval) to cover the public list and detail pages, no season filter, the logo, and the permissions. Updated again on 2026-11-17 for the Series tag and callout on tournaments and for the Finale as its own event. The UL source (`vincent-hall-series-ul.md`, attached to #257) still describes the Finale as a count on the Placement Round and needs the same change before the UL commit.
- **Whole-Series save.** `POST /series` and `PUT /series/{id}` take the name plus the full ordered Stage list. The aggregate reconciles its Stages and checks every invariant in one pass. No Stage sub-resource routes. A Stage's order is its position in the submitted list.
- **Season is fixed after create.** `PUT` does not take a season. To move a Series to another season, delete it and create it again.
- **Stage order.** The admin sets the order in the grid (move up/down). Save rejects any order that doesn't follow tournament start dates, or that breaks the kind order (Qualifying, then Survivor, then Placement).
- **Public pages move into scope.** #258 deferred public Series pages; the user brought them in. The list and detail pages are public and work like Tournaments: one page, with admin controls shown only to users who hold the matching permission.
- **List page has no season picker.** `/series` lists every Series, newest season first, each labeled with its season's date range (for example 1/1/2027 to 12/31/2027).
- **List layout (mockup option 1).** Series whose season hasn't ended are featured, each with its **next Stage** (the first Stage whose tournament hasn't ended). The rest sit under "Past Series". This adds `NextStage` to the list response in Phase 1.
- **Detail layout (mockup option 1).** A "How the field narrows" chart (up to N qualifiers → each Survivor count → the Finale's finalist count), a Finale block with its date, time and host center,, then the Stages grouped by kind.
- **Team events show the bowler total.** "Top 2 teams" with "(6 bowlers)" on the line below it on the detail page; the editor hint is "Teams of 3" with "(6 bowlers)" below.
- **Detail page shows the Stage schedule.** Name, season date range, and the ordered Stages: kind, tournament (linked to `/tournaments/{id}` so bowlers can sign up), tournament dates, advancing count ("teams" for team events), and squad for Survivor rows. The field and standings arrive in phase 2.
- **Release path.** The work branches from `release/v1.3.0` and the PR targets it. `<Version>` stays `1.3.0` (`check-version-bump.sh` requires a release-branch PR to match `X.Y.Z`), so there is no version commit.
- **UL source goes into the repo docs.** The downloaded `vincent-hall-series-ul.md` (attached to #257) becomes a new `## Series` section in `docs/ubiquitous-language.md`. That covers its terms, its rule language as each term's **Rules** list, the Series vs Lineage and Survivor vs Placement notes, a short Computation note, and the rules gaps from #257 (the doc's open items are now "None"). The doc's Part 2 "Proposed model" is covered by each term's **In Code** block.
- **Navigation.** A "Series" item in the main nav's Tournaments dropdown, visible to everyone. Moving the admin links out of the right-hand account menu is a separate concern, not part of this plan.
- **Series on tournaments (added 2026-11-17).** The tournament list summary and the tournament detail show the Series a tournament is a Stage of (the Finale gets its own card; see the Finale decision above), with the Series logo (when it has one; initials otherwise) and the round kind. It is the last piece of phase 1. Both list tabs (upcoming and past) and the detail page show it. Mockups: `tournaments-list-series.html`, `tournament-detail-series.html`.
- **Cancelled tournaments keep their Series tag.** A cancelled tournament's round will likely be rescheduled elsewhere. Until an admin removes the Stage, the tag stays, in case the committee decides the cancelled round's advancers return to the BOTY pool as if all N qualifiers had already qualified. The Series page's Stage list also keeps the row.
- **List tag style: the pill (chosen).** A pill in the card's top row. The banner strip was dropped: the tournament cards already carry a Defending Champions banner, and a second banner would make the cards crowded and tall.
- **One Series shape on both reads.** A single optional `Series` object (`TournamentSeriesResponse`) sits on the list item and the detail response, so the list sends advancing count and squad it doesn't display. Two types would cost more than those few bytes. `null` means the tournament isn't a Stage.
- **Tournament caches clear on every Series write.** The tournament list and detail are cached under the `neba:tournaments` tag (FusionCache). The Series create, edit, delete and logo handlers remove that tag, so a renamed Series or a new logo shows at once. Series writes are rare, so clearing the whole tag costs little.

### Confirmed UI flow

1. **`/series` (public).** Every Series, newest season first: logo (or initials), name, season date range, Stage counts by kind, "Locked" badge for admins. Empty state. `FabCreateButton` for `CreateSeries`.
2. **`/series/{id}` (public).** Logo, name, season date range, Stage schedule with tournament links. Edit (`EditSeries`), Delete (`DeleteSeries`, confirm dialog) and an edit-logo button (`EditSeriesLogo`, opens the shared logo modal) for admins; Delete disabled with the reason when locked.
3. **`/series/new` (`CreateSeries`).** Name, season, logo upload, Stage grid (add, remove, move up/down, kind, tournament, advancing count, squad date and start time on Survivor rows). The tournament picker lists the season's tournaments not already in another Series. One save; messages appear next to the row they name. `DirtyFormGuard`.
4. **`/series/{id}/edit` (`EditSeries`).** Same form, pre-filled, season read-only. When locked: a banner, a read-only grid, and only Name and logo editable.
5. **Nav.** "Series" in the Tournaments dropdown.
6. **Tournament list (`/tournaments`).** Every tournament card (upcoming and past) that is a Stage shows a Series tag: Series logo, name, and the round kind ("Qualifying Round", "Survivor Round", "Placement Round"). The tag links to `/series/{id}`.
7. **Tournament detail (`/tournaments/{id}`).** A Stage tournament shows a Series callout under the hero: logo, name, round kind, what that round decides, and a "View Series" link. On a Placement Round of a Series with a Finale, the callout also names the Finale's date and time.
8. **Finale on the tournament list.** A Series' Finale is its own card in the same timeline, sorted by date, on both tabs: the Series logo as the tile, "{Series name} Finale" as the title, date, start time, host center (or "Host center to be announced"), "N finalists", and a link to `/series/{id}`. No price or registration.
9. **Series detail and editor.** The Series page shows the Finale block (date, time, host). The editor has a Finale section under the Stage grid: a "This Series has a Finale" toggle, then finalists, date, start time and host center.

## Phase 1: API

New feature folder `src/Neba.Api/Features/Series/`. Inside `Neba.Api.Features.Series.*`, the simple name `Series` resolves to the namespace, not the type, so use-case code writes `Domain.Series` (it resolves without an alias). Code outside the feature (`AppDbContext`, configurations, factories outside `Neba.TestFactory.Series`) uses `Series` through a normal `using`.

Rules from #258 are cited by number: #1 tournament exists and in one Series only, #2 same season, #3 kind order and one Placement last, #4 date order, #5 Survivor counts decrease, #6 a Finale's count is above zero and no larger than the last Survivor count, #7 squad designation, #8 Qualifying and Survivor counts required and above zero and a Placement Stage has none, #9 a Series has a Finale exactly when it has a `SeriesFinale`, #10 a Finale has a date and start time, and its bowling center, when set, is Open (a center already saved on the Finale stays valid if it later closes), #11 no Stage can come after the Finale: every Stage's tournament ends before the Finale's date. The lock covers #9 and the count in #6; the Finale's date, start time and center stay editable while locked.

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

    /// <summary>The final Stage. Ranks by tournament position. With a Finale, the top finishers go to it.</summary>
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

#### `Domain/SeriesFinale.cs`, `Domain/SeriesFinaleDefinition.cs`

The Finale is a value owned by the Series, stored in the `series` table's `finale_*` columns. It has no identity of its own and no tournament. Its name is always "{Series name} Finale" and is never stored.

```csharp
using Neba.Api.Features.BowlingCenters.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>The Series' last event, held outside the system after the Placement Round.</summary>
public sealed record SeriesFinale
{
    /// <summary>How many bowlers from the Placement Round go to the Finale.</summary>
    public int FinalistCount { get; init; }

    /// <summary>The day the Finale is bowled.</summary>
    public DateOnly Date { get; init; }

    /// <summary>The start time, local to the bowling center.</summary>
    public TimeOnly StartTime { get; init; }

    /// <summary>Where it is held; null until announced.</summary>
    public CertificationNumber? BowlingCenterId { get; init; }

    private SeriesFinale()
    { }

    internal static SeriesFinale Describe(SeriesFinaleDefinition definition)
        => new()
        {
            FinalistCount = definition.FinalistCount,
            Date = definition.Date,
            StartTime = definition.StartTime,
            BowlingCenterId = definition.BowlingCenterId,
        };
}

/// <summary>The submitted Finale. Null on the command means the Series has no Finale.</summary>
public sealed record SeriesFinaleDefinition
{
    public required int FinalistCount { get; init; }
    public required DateOnly Date { get; init; }
    public required TimeOnly StartTime { get; init; }
    public CertificationNumber? BowlingCenterId { get; init; }
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

    /// <summary>Required on Qualifying and Survivor Stages; a Placement Stage has none.</summary>
    public int? AdvancingCount { get; init; }
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

    /// <summary>
    /// Top N finishers (Qualifying; teams in a team event) or bowlers advancing (Survivor): required and above zero.
    /// Always null on the Placement Stage; the Finale carries its own count.
    /// </summary>
    public int? AdvancingCount { get; private set; }

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

        // #8: required and above zero on Qualifying and Survivor; a Placement Stage has none (the Finale carries its own count).
        if (definition.Kind == StageKind.Placement)
        {
            if (definition.AdvancingCount is not null)
            {
                errors.Add(SeriesErrors.AdvancingCountNotAllowed(order, facts.TournamentName));
            }
        }
        else if (definition.AdvancingCount is null)
        {
            errors.Add(SeriesErrors.AdvancingCountRequired(order, facts.TournamentName, definition.Kind));
        }
        else if (definition.AdvancingCount <= 0)
        {
            errors.Add(SeriesErrors.AdvancingCountNotPositive(order, facts.TournamentName));
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
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>
/// A season-scoped competition made up of ordered Stages, each linked to one tournament, and an
/// optional Finale (the Vincent Hall Memorial Series).
/// </summary>
public sealed class Series
{
    internal const int NameMaxLength = 127;

    public required SeriesId Id { get; init; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>The season this Series belongs to. Fixed after creation.</summary>
    public SeasonId SeasonId { get; private set; }

    /// <summary>The Series' own logo, or null. Presentation only, so it stays editable while locked.</summary>
    public StoredFile? Logo { get; private set; }

    /// <summary>The Finale, or null when the Series has none (#9). Owned by the Series; not a tournament.</summary>
    public SeriesFinale? Finale { get; private set; }

    private readonly List<SeriesStage> _stages = [];

    /// <summary>The Stages, in order.</summary>
    public IReadOnlyCollection<SeriesStage> Stages
        => _stages.AsReadOnly();

    /// <summary>
    /// True once any Stage has left pending. While locked only <see cref="Name"/>, <see cref="Logo"/> and the
    /// Finale's date, start time and center can change.
    /// </summary>
    public bool IsLocked
        => _stages.Exists(stage => stage.Status != StageStatus.Pending);

    /// <summary>True when the Series has a Finale (#9). There is no separate flag.</summary>
    public bool HasFinale
        => Finale is not null;

    private Series()
    { }

    /// <summary>Creates a Series. It may be incomplete, for example with no Placement Stage yet.</summary>
    public static ErrorOr<Series> Create(
        string name,
        SeasonId seasonId,
        StoredFile? logo,
        IReadOnlyList<SeriesStageDefinition> stages,
        SeriesFinaleDefinition? finale,
        IReadOnlyDictionary<TournamentId, StageTournamentFacts> tournaments)
    {
        var series = new Series { Id = SeriesId.New(), SeasonId = seasonId, Logo = logo };

        var renamed = series.Rename(name);
        if (renamed.IsError)
        {
            return renamed.Errors;
        }

        var applied = series.ReplaceConfiguration(stages, finale, tournaments);

        return applied.IsError ? applied.Errors : series;
    }

    /// <summary>Replaces or removes the logo. Allowed while locked. The handler has already checked the file was staged.</summary>
    public void ChangeLogo(StoredFile? logo)
        => Logo = logo;

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
    /// Replaces the Stages and the Finale with the submitted configuration, checking every rule in one pass.
    /// Matched Stage IDs update in place, new rows are added, missing Stages are removed. A null
    /// <paramref name="finale"/> removes the Finale. While locked, only the Finale's date, start time and center can differ
    /// from what is stored; the handler's bowling center check has already run.
    /// </summary>
    public ErrorOr<Updated> ReplaceConfiguration(
        IReadOnlyList<SeriesStageDefinition> stages,
        SeriesFinaleDefinition? finale,
        IReadOnlyDictionary<TournamentId, StageTournamentFacts> tournaments)
    {
        var stagesUnchanged = StagesMatch(stages);
        var finaleShapeUnchanged = (Finale is null) == (finale is null) && Finale?.FinalistCount == finale?.FinalistCount;

        if (IsLocked && !(stagesUnchanged && finaleShapeUnchanged))
        {
            return SeriesErrors.Locked;
        }

        var errors = new List<Error>();
        var candidates = new List<(int Order, SeriesStageDefinition Definition, StageTournamentFacts Facts)>();

        // Unchanged Stages are not re-checked, so a locked Series can still be renamed or have its Finale rescheduled.
        if (!stagesUnchanged)
        {
            candidates = CheckStages(stages, tournaments, errors);
            errors.AddRange(CheckSequence(candidates));
        }

        var lastSurvivorCount = stagesUnchanged
            ? _stages.Where(s => s.Kind == StageKind.Survivor).Select(s => s.AdvancingCount).LastOrDefault()
            : stages.Where(d => d.Kind == StageKind.Survivor).Select(d => d.AdvancingCount).LastOrDefault();

        // Tournament facts of the Stages the Finale must follow: the stored Stages when unchanged, else the submitted ones.
        var stageFacts = stagesUnchanged
            ? _stages.Select(s => tournaments.GetValueOrDefault(s.TournamentId)).OfType<StageTournamentFacts>().ToList()
            : candidates.Select(c => c.Facts).ToList();

        errors.AddRange(CheckFinale(finale, lastSurvivorCount, stageFacts));

        if (errors.Count > 0)
        {
            return errors;
        }

        if (!stagesUnchanged)
        {
            ApplyStages(stages, candidates);
        }

        Finale = finale is null ? null : SeriesFinale.Describe(finale);

        return Result.Updated;
    }

    /// <summary>Refuses deletion while locked.</summary>
    public ErrorOr<Success> EnsureCanDelete()
        => IsLocked ? SeriesErrors.Locked : Result.Success;

    private bool StagesMatch(IReadOnlyList<SeriesStageDefinition> stages)
        => stages.Count == _stages.Count && stages.Select((d, i) => (d, i)).All(x =>
            _stages.Exists(s => s.Id == x.d.StageId && s.Matches(x.i + 1, x.d)));

    // Per-Stage checks: existence, duplicates, other Series (#1), season (#2), Stage's own invariants (#7, #8).
    private List<(int Order, SeriesStageDefinition Definition, StageTournamentFacts Facts)> CheckStages(
        IReadOnlyList<SeriesStageDefinition> definitions,
        IReadOnlyDictionary<TournamentId, StageTournamentFacts> tournaments,
        List<Error> errors)
    {
        var candidates = new List<(int Order, SeriesStageDefinition Definition, StageTournamentFacts Facts)>();
        var seen = new HashSet<TournamentId>();

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

        return candidates;
    }

    private void ApplyStages(
        IReadOnlyList<SeriesStageDefinition> definitions,
        IReadOnlyList<(int Order, SeriesStageDefinition Definition, StageTournamentFacts Facts)> candidates)
    {
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
    }

    // Cross-Stage checks over Stages whose tournament resolved (#3 to #5).
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

            if (definition.Kind == StageKind.Survivor && definition.AdvancingCount is { } survivorCount)
            {
                if (lastSurvivorCount is { } previousCount && survivorCount >= previousCount)
                {
                    yield return SeriesErrors.SurvivorCountNotDecreasing(order, facts.TournamentName, survivorCount, previousCount);
                }

                lastSurvivorCount = survivorCount;
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

                placementSeen = true;
            }
        }
    }

    // #6: the Finale's count is above zero and no larger than the last Survivor count (no check without a Survivor Stage).
    // #11: no Stage can come after the Finale, so every Stage's tournament ends before its date. Only the latest-ending
    // Stage is reported. #10's date and time are structural (the input type cannot be empty); the center is checked by the handler.
    private static IEnumerable<Error> CheckFinale(
        SeriesFinaleDefinition? finale, int? lastSurvivorCount, IReadOnlyCollection<StageTournamentFacts> stageFacts)
    {
        if (finale is null)
        {
            yield break;
        }

        if (finale.FinalistCount <= 0)
        {
            yield return SeriesErrors.FinaleCountNotPositive;
        }
        else if (lastSurvivorCount is { } lastSurvivor && finale.FinalistCount > lastSurvivor)
        {
            yield return SeriesErrors.FinaleCountExceedsSurvivor(finale.FinalistCount, lastSurvivor);
        }

        if (stageFacts.MaxBy(facts => facts.EndDate) is { } latest && latest.EndDate >= finale.Date)
        {
            yield return SeriesErrors.StageNotBeforeFinale(finale.Date, latest.TournamentName, latest.EndDate);
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

    /// <summary>Metadata key naming the request property a non-Stage error belongs to (for example <c>Series.Finale.Date</c>).</summary>
    public const string PropertyKey = "Property";

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
        description: "This Series is locked because processing has begun. Only its name, its logo and the Finale's date, start time and center can change, and it cannot be deleted.");

    public static Error LogoNotUploaded
        => Error.Validation("Series.Logo.NotUploaded", "Logo must be a file uploaded through the Series logo upload endpoint.");

    public static Error LogoChangeDenied
        => Error.Forbidden("Series.Logo.ChangeDenied", "Your account is not allowed to change Series logos.");

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

    public static Error AdvancingCountNotAllowed(int order, string tournament)
        => Stage(order, tournament, "Series.Stage.AdvancingCountNotAllowed", "a Placement Stage has no advancing count. Set the Finale on the Series instead.", ErrorType.Validation);

    // Finale errors belong to the Series, not a Stage row. The PropertyKey metadata names the field they land on.
    public static readonly Error FinaleCountNotPositive = Error.Validation(
        code: "Series.Finale.FinalistCount.NotPositive",
        description: "The Finale needs a whole number of finalists greater than zero. Remove the Finale if the Series has none.",
        metadata: new Dictionary<string, object> { { PropertyKey, "Series.Finale.FinalistCount" } });

    public static Error FinaleCountExceedsSurvivor(int count, int survivorCount) => Error.Validation(
        code: "Series.Finale.FinalistCount.ExceedsSurvivor",
        description: $"A Finale of {count} cannot be larger than the last Survivor Stage's {survivorCount}.",
        metadata: new Dictionary<string, object> { { PropertyKey, "Series.Finale.FinalistCount" } });

    public static Error StageNotBeforeFinale(DateOnly finaleDate, string tournament, DateOnly tournamentEndDate) => Error.Validation(
        code: "Series.Finale.Date.StageNotBefore",
        description: $"No Stage can come after the Finale, but {tournament} ends {Format(tournamentEndDate)} and the Finale is {Format(finaleDate)}.",
        metadata: new Dictionary<string, object> { { PropertyKey, "Series.Finale.Date" } });

    public static Error FinaleBowlingCenterNotAvailable(string bowlingCenterId) => Error.Validation(
        code: "Series.Finale.BowlingCenter.NotAvailable",
        description: "The selected Finale bowling center does not exist or is not open.",
        metadata: new Dictionary<string, object> { { PropertyKey, "Series.Finale.BowlingCenterId" }, { "BowlingCenterId", bowlingCenterId } });

    public static Error AdvancingCountRequired(int order, string tournament, StageKind kind)
        => Stage(order, tournament, "Series.Stage.AdvancingCountRequired", $"a {kind.Name} Stage needs an advancing count.", ErrorType.Validation);

    public static Error AdvancingCountNotPositive(int order, string tournament)
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
    public static StoredFile? ToLogo(SeriesLogoInput? logo)
        => logo is null
            ? null
            : new StoredFile { Container = logo.Container, Path = logo.Path, ContentType = logo.ContentType, SizeInBytes = logo.SizeInBytes };

    public static SeriesFinaleDefinition? ToFinale(SeriesFinaleInput? finale)
        => finale is null
            ? null
            : new SeriesFinaleDefinition
            {
                FinalistCount = finale.FinalistCount,
                Date = finale.Date!.Value,
                StartTime = finale.StartTime!.Value,
                BowlingCenterId = string.IsNullOrWhiteSpace(finale.BowlingCenterId)
                    ? null
                    : new CertificationNumber { Value = finale.BowlingCenterId },
            };

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

`Features/Series/SeriesFinaleInputValidator.cs` (structural only: the date and start time are present, the center id is the right length; the count's range is rule #6 in the domain):

```csharp
using FluentValidation;

using Neba.Api.Contracts.Series.CreateSeries;

namespace Neba.Api.Features.Series;

internal sealed class SeriesFinaleInputValidator
    : AbstractValidator<SeriesFinaleInput>
{
    public SeriesFinaleInputValidator()
    {
        RuleFor(f => f.Date)
            .NotNull()
            .WithErrorCode("SeriesFinaleInput.DateRequired")
            .WithMessage("The Finale needs a date.");

        RuleFor(f => f.StartTime)
            .NotNull()
            .WithErrorCode("SeriesFinaleInput.StartTimeRequired")
            .WithMessage("The Finale needs a start time.");

        RuleFor(f => f.BowlingCenterId)
            .MaximumLength(6)
            .When(f => !string.IsNullOrWhiteSpace(f.BowlingCenterId))
            .WithErrorCode("SeriesFinaleInput.BowlingCenterIdInvalid")
            .WithMessage("Bowling center must be a certification number.");
    }
}
```

`Features/Series/SeriesFinaleChecks.cs`: the one query the Finale needs (rule #10's center check), shared by create and edit. Bowling centers are never deleted; they get a status (Open, Closed, Uncertified) and only Open centers show in the list. So a center newly chosen for the Finale must exist and be Open. A center the Finale already has stays valid if it later closes, so a locked Series can still be rescheduled and a Series can be renamed. Like the season check, it returns immediately on a miss.

```csharp
using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Series.Domain;

namespace Neba.Api.Features.Series;

internal static class SeriesFinaleChecks
{
    public static async Task<ErrorOr<Success>> EnsureBowlingCenterUsableAsync(
        AppDbContext appDbContext,
        SeriesFinaleDefinition? finale,
        CertificationNumber? currentCenterId,
        CancellationToken cancellationToken)
    {
        if (finale?.BowlingCenterId is not { } centerId || centerId == currentCenterId)
        {
            return Result.Success; // no Finale, host still to be announced, or the center the Finale already has
        }

        var usable = await appDbContext.BowlingCenters
            .AnyAsync(center => center.CertificationNumber == centerId && center.Status == BowlingCenterStatus.Open, cancellationToken);

        return usable ? Result.Success : SeriesErrors.FinaleBowlingCenterNotAvailable(centerId.Value);
    }
}
```

`Features/Series/SeriesMutationResultSender.cs`: Validation errors first (422, all of them); otherwise Conflicts (409, all of them). A lock Conflict comes back alone, so it lands on 409. Each Stage error becomes a `ValidationFailure` named `Series.Stages[i]`; a Finale error is named by its `PropertyKey` (`Series.Finale.FinalistCount`, `Series.Finale.Date`, `Series.Finale.BowlingCenterId`).

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
        // Stage errors name their row; Finale errors name their field (PropertyKey); the rest belong to the Series.
        var property = "Series";

        if (error.Metadata is { } metadata)
        {
            if (metadata.TryGetValue(SeriesErrors.StageOrderKey, out var order) && order is int stageOrder)
            {
                property = $"Series.Stages[{stageOrder - 1}]";
            }
            else if (metadata.TryGetValue(SeriesErrors.PropertyKey, out var named) && named is string name)
            {
                property = name;
            }
        }

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
    public Uri? LogoUrl { get; init; }

    /// <summary>Projection only; not mapped to the response.</summary>
    public string? LogoContainer { get; init; }

    /// <summary>Projection only; not mapped to the response.</summary>
    public string? LogoPath { get; init; }

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
internal sealed class ListSeriesQueryHandler(AppDbContext appDbContext, IFileStorageService fileStorageService, TimeProvider timeProvider)
    : IQueryHandler<ListSeriesQuery, IReadOnlyCollection<SeriesSummaryDto>>
{
    public async Task<IReadOnlyCollection<SeriesSummaryDto>> HandleAsync(ListSeriesQuery query, CancellationToken cancellationToken)
    {
        // Same cut-off as ListUpcomingTournaments: back up one day so a tournament ending today
        // still counts in every U.S. time zone.
        var cutoff = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime).AddDays(-1);

        List<SeriesSummaryDto> summaries = await appDbContext.Series.AsNoTracking()
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
                LogoContainer = x.series.Logo == null ? null : x.series.Logo.Container,
                LogoPath = x.series.Logo == null ? null : x.series.Logo.Path,
            })
            .ToListAsync(cancellationToken);

        // LogoContainer/LogoPath are internal projection fields on the DTO record; the URL is built here,
        // the same way GetTournamentQueryHandler builds LogoUrl with IFileStorageService.GetBlobUri.
        summaries = [.. summaries.Select(s => s.LogoContainer is not null && s.LogoPath is not null
            ? s with { LogoUrl = fileStorageService.GetBlobUri(s.LogoContainer, s.LogoPath) }
            : s)];

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
    /// <summary>The Finale, or null when the Series has none.</summary>
    public SeriesFinaleDto? Finale { get; init; }

    /// <summary>The Series' own logo (no fallback). The edit page resends container, path, type and size.</summary>
    public Uri? LogoUrl { get; init; }
    public string? LogoContainer { get; init; }
    public string? LogoPath { get; init; }
    public string? LogoContentType { get; init; }
    public long? LogoSizeInBytes { get; init; }

    public required IReadOnlyCollection<SeriesStageDto> Stages { get; init; }
}

internal sealed record SeriesStageDto
{
    public required SeriesStageId Id { get; init; }
    public required int Order { get; init; }
    public required string Kind { get; init; }
    public required string Status { get; init; }
    public int? AdvancingCount { get; init; }
    public DateOnly? SquadDate { get; init; }
    public TimeOnly? SquadStartTime { get; init; }
    public required TournamentId TournamentId { get; init; }
    public required string TournamentName { get; init; }
    public required DateOnly TournamentStartDate { get; init; }
    public required DateOnly TournamentEndDate { get; init; }
    public required string TournamentType { get; init; }
    public required int TeamSize { get; init; }
}

// SeriesFinaleDto.cs
internal sealed record SeriesFinaleDto
{
    public required int FinalistCount { get; init; }
    public required DateOnly Date { get; init; }
    public required TimeOnly StartTime { get; init; }
    public string? BowlingCenterId { get; init; }
    public string? BowlingCenterName { get; init; }
    public string? BowlingCenterCity { get; init; }
    public string? BowlingCenterState { get; init; }
}

// GetSeriesQueryHandler.cs
internal sealed class GetSeriesQueryHandler(AppDbContext appDbContext, IFileStorageService fileStorageService)
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
                    series.Logo,
                    series.Finale,
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

        // The Finale's host center, when one is set (no navigation from the owned Finale, so a second small query).
        SeriesFinaleDto? finale = null;
        if (header.Finale is { } storedFinale)
        {
            var center = storedFinale.BowlingCenterId is { } centerId
                ? await appDbContext.BowlingCenters.AsNoTracking()
                    .Where(bowlingCenter => bowlingCenter.CertificationNumber == centerId)
                    .Select(bowlingCenter => new { bowlingCenter.Name, bowlingCenter.Address.City, State = bowlingCenter.Address.Region })
                    .SingleOrDefaultAsync(cancellationToken)
                : null;

            finale = new SeriesFinaleDto
            {
                FinalistCount = storedFinale.FinalistCount,
                Date = storedFinale.Date,
                StartTime = storedFinale.StartTime,
                BowlingCenterId = storedFinale.BowlingCenterId?.Value,
                BowlingCenterName = center?.Name,
                BowlingCenterCity = center?.City,
                BowlingCenterState = center?.State,
            };
        }

        return new SeriesDetailDto
        {
            Id = header.Id,
            Name = header.Name,
            SeasonId = header.SeasonId,
            SeasonDescription = header.Description,
            SeasonStartDate = header.StartDate,
            SeasonEndDate = header.EndDate,
            IsLocked = header.IsLocked,
            Finale = finale,
            LogoUrl = header.Logo is null ? null : fileStorageService.GetBlobUri(header.Logo.Container, header.Logo.Path),
            LogoContainer = header.Logo?.Container,
            LogoPath = header.Logo?.Path,
            LogoContentType = header.Logo?.ContentType,
            LogoSizeInBytes = header.Logo?.SizeInBytes,
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

#### `ListSeriesFinales/`

The tournament list shows each Finale as its own card, so it needs every Finale, not only those of one Series. One public read, not cached (a handful of rows, the same as the other Series reads).

```csharp
// ListSeriesFinalesQuery.cs
internal sealed record ListSeriesFinalesQuery
    : IQuery<IReadOnlyCollection<SeriesFinaleSummaryDto>>;

// SeriesFinaleSummaryDto.cs
internal sealed record SeriesFinaleSummaryDto
{
    public required SeriesId SeriesId { get; init; }
    public required string SeriesName { get; init; }
    public required SeasonId SeasonId { get; init; }
    public Uri? LogoUrl { get; init; }
    public required SeriesFinaleDto Finale { get; init; }
}

// ListSeriesFinalesQueryHandler.cs
internal sealed class ListSeriesFinalesQueryHandler(AppDbContext appDbContext, IFileStorageService fileStorageService)
    : IQueryHandler<ListSeriesFinalesQuery, IReadOnlyCollection<SeriesFinaleSummaryDto>>
{
    public async Task<IReadOnlyCollection<SeriesFinaleSummaryDto>> HandleAsync(ListSeriesFinalesQuery query, CancellationToken cancellationToken)
    {
        var rows = await appDbContext.Series.AsNoTracking()
            .Where(series => series.Finale != null)
            .Select(series => new
            {
                series.Id, series.Name, series.SeasonId, series.Finale,
                LogoContainer = series.Logo == null ? null : series.Logo.Container,
                LogoPath = series.Logo == null ? null : series.Logo.Path,
            })
            .ToListAsync(cancellationToken);

        var centerIds = rows.Select(row => row.Finale!.BowlingCenterId).OfType<CertificationNumber>().Distinct().ToList();

        var centers = await appDbContext.BowlingCenters.AsNoTracking()
            .Where(center => centerIds.Contains(center.CertificationNumber))
            .Select(center => new { center.CertificationNumber, center.Name, center.Address.City, State = center.Address.Region })
            .ToDictionaryAsync(center => center.CertificationNumber, cancellationToken);

        return [.. rows
            .OrderBy(row => row.Finale!.Date)
            .ThenBy(row => row.Finale!.StartTime)
            .Select(row =>
            {
                var finale = row.Finale!;
                var center = finale.BowlingCenterId is { } id ? centers.GetValueOrDefault(id) : null;

                return new SeriesFinaleSummaryDto
                {
                    SeriesId = row.Id,
                    SeriesName = row.Name,
                    SeasonId = row.SeasonId,
                    LogoUrl = row.LogoContainer is not null && row.LogoPath is not null
                        ? fileStorageService.GetBlobUri(row.LogoContainer, row.LogoPath)
                        : null,
                    Finale = new SeriesFinaleDto
                    {
                        FinalistCount = finale.FinalistCount,
                        Date = finale.Date,
                        StartTime = finale.StartTime,
                        BowlingCenterId = finale.BowlingCenterId?.Value,
                        BowlingCenterName = center?.Name,
                        BowlingCenterCity = center?.City,
                        BowlingCenterState = center?.State,
                    },
                };
            })];
    }
}
```

`ListSeriesFinalesEndpoint`: `EndpointWithoutRequest<CollectionResponse<SeriesFinaleSummaryResponse>>`, `Get("finales")` on `SeriesEndpointGroup`, `AllowAnonymous()`, `WithName("ListSeriesFinales")`, tag `Public`, produces 200 and 500. FastEndpoints matches the static `finales` segment before `{id}`, so `GET /series/finales` never reaches `GetSeries`; an integration test pins this. `ListSeriesFinalesSummary`: "Lists the Finale of every Series that has one, soonest first (date, then start time), with the Series' name and logo and the host center."

#### `CreateSeries/`

```csharp
// CreateSeriesCommand.cs
internal sealed record CreateSeriesCommand
    : ICommand<SeriesId>
{
    public required string Name { get; init; }
    public required SeasonId SeasonId { get; init; }
    public StoredFile? Logo { get; init; }
    public required IReadOnlyList<SeriesStageDefinition> Stages { get; init; }

    /// <summary>Null means the Series has no Finale.</summary>
    public SeriesFinaleDefinition? Finale { get; init; }
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

        var finaleCenter = await SeriesFinaleChecks.EnsureBowlingCenterUsableAsync(appDbContext, command.Finale, currentCenterId: null, cancellationToken);
        if (finaleCenter.IsError)
        {
            return finaleCenter.Errors;
        }

        var tournaments = await StageTournamentFactsReader.ReadAsync(
            appDbContext, [.. command.Stages.Select(s => s.TournamentId).Distinct()], excludingSeriesId: null, cancellationToken);

        var logoClaim = await StoredFileClaim.EnsureClaimableAsync(
            appDbContext, command.Logo, current: null, SeriesErrors.LogoNotUploaded, cancellationToken);
        if (logoClaim.IsError)
        {
            return logoClaim.Errors;
        }

        var series = Domain.Series.Create(command.Name, command.SeasonId, command.Logo, command.Stages, command.Finale, tournaments);
        if (series.IsError)
        {
            return series.Errors;
        }

        await appDbContext.Series.AddAsync(series.Value, cancellationToken);
        await PendingUploadCleaner.RemoveClaimedAsync(appDbContext, command.Logo, cancellationToken);
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

        RuleFor(r => r.Series.Finale!).SetValidator(new SeriesFinaleInputValidator())
            .When(r => r.Series.Finale is not null);
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
            Logo = SeriesStageInputMapper.ToLogo(req.Series.Logo),
            Stages = SeriesStageInputMapper.ToDefinitions(req.Series.Stages),
            Finale = SeriesStageInputMapper.ToFinale(req.Series.Finale),
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
    public required Ulid CallerUserId { get; init; }
    public required string Name { get; init; }
    public StoredFile? Logo { get; init; }
    public required IReadOnlyList<SeriesStageDefinition> Stages { get; init; }

    /// <summary>Null removes the Finale (refused while locked).</summary>
    public SeriesFinaleDefinition? Finale { get; init; }
}

// EditSeriesCommandHandler.cs
internal sealed class EditSeriesCommandHandler(
        AppDbContext appDbContext,
        IPermissionDenialReader permissionDenialReader,
        IBackgroundJobScheduler backgroundJobScheduler)
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

        // Same rule as EditTournament: a stored denial of the logo permission blocks changing the logo here too.
        var logoChanged = !StoredFileClaim.IsSameFile(command.Logo, series.Logo);
        if (logoChanged && await permissionDenialReader.IsDeniedAsync(
                command.CallerUserId, Contracts.Security.Permissions.EditSeriesLogo, cancellationToken))
        {
            return SeriesErrors.LogoChangeDenied;
        }

        var logoClaim = await StoredFileClaim.EnsureClaimableAsync(
            appDbContext, command.Logo, series.Logo, SeriesErrors.LogoNotUploaded, cancellationToken);
        if (logoClaim.IsError)
        {
            return logoClaim.Errors;
        }

        var finaleCenter = await SeriesFinaleChecks.EnsureBowlingCenterUsableAsync(
            appDbContext, command.Finale, series.Finale?.BowlingCenterId, cancellationToken);
        if (finaleCenter.IsError)
        {
            return finaleCenter.Errors;
        }

        var tournaments = await StageTournamentFactsReader.ReadAsync(
            appDbContext, [.. command.Stages.Select(s => s.TournamentId).Distinct()], command.SeriesId, cancellationToken);

        var renamed = series.Rename(command.Name);
        var replaced = series.ReplaceConfiguration(command.Stages, command.Finale, tournaments);

        List<Error> errors = [.. renamed.ErrorsOrEmptyList, .. replaced.ErrorsOrEmptyList];
        if (errors.Count > 0)
        {
            return errors;
        }

        // Snapshot before ChangeLogo: the property is replaced in place.
        var previousLogo = series.Logo;
        var logo = StoredFileClaim.Resolve(command.Logo, previousLogo);
        series.ChangeLogo(logo); // allowed while locked

        await PendingUploadCleaner.RemoveClaimedAsync(appDbContext, logo, cancellationToken);
        await appDbContext.SaveChangesAsync(cancellationToken);

        StoredFileCleanup.ScheduleDeleteOfReplaced(backgroundJobScheduler, previousLogo, logo);

        return Result.Updated;
    }
}
```

`EditSeriesRequestValidator`: the same rules as create, minus the season, plus `Id` NotEmpty/Length(26). `EditSeriesEndpoint`: `Put("{id}")`, `Policies(PermissionCatalog.EditSeries.PolicyName)`, `WithName("EditSeries")`, produces 204, 400, 401, 403, 404, 409, 422. It reads `User.CallerUserId` (401 when missing, the same as `EditTournamentEndpoint`) and maps `Logo` through `SeriesStageInputMapper.ToLogo` and `Finale` through `SeriesStageInputMapper.ToFinale`. NotFound → `Send.NotFoundAsync`; a Forbidden error (`LogoChangeDenied`) → 403; other errors go through `SeriesMutationResultSender`; success → `Send.NoContentAsync`.

The edit page always resends the current logo, so an unchanged logo is a no-op (`StoredFileClaim.Resolve` keeps the stored content type and size).

#### `DeleteSeries/`

```csharp
// DeleteSeriesCommandHandler.cs
internal sealed class DeleteSeriesCommandHandler(AppDbContext appDbContext, IBackgroundJobScheduler backgroundJobScheduler)
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

        var logo = series.Logo;

        appDbContext.Series.Remove(series);
        await appDbContext.SaveChangesAsync(cancellationToken);

        StoredFileCleanup.ScheduleDeleteOfReplaced(backgroundJobScheduler, logo, current: null);

        return Result.Deleted;
    }
}
```

`DeleteSeriesCommand { SeriesId }`, `DeleteSeriesRequest { [BindFrom("id")] Id }` + validator, `DeleteSeriesEndpoint`: `Delete("{id}")`, `Policies(PermissionCatalog.DeleteSeries.PolicyName)`, `WithName("DeleteSeries")`, produces 204, 400, 401, 403, 409. `DeleteSeriesSummary` says a missing Series still returns 204 and a locked Series returns 409.

#### `UploadSeriesLogo/`

The same staging flow as `UploadTournamentLogoEndpoint`: the file goes to `PendingUploads` and only attaches through a command that passes the claim check.

```csharp
internal sealed class UploadSeriesLogoEndpoint(IUploadStagingService stagingService, IFileStorageService fileStorageService)
    : Endpoint<UploadSeriesLogoRequest, UploadedFileResponse>
{
    public override void Configure()
    {
        Post("logo");
        Group<SeriesEndpointGroup>();
        AllowFileUploads();

        Options(options => options
            .WithVersionSet("Series")
            .MapToApiVersion(new ApiVersion(1, 0)));

        Policies(PermissionCatalog.CanUploadSeriesLogoPolicyName);

        Description(description => description
            .WithName("UploadSeriesLogo")
            .WithTags("Admin")
            .Produces<UploadedFileResponse>(StatusCodes.Status200OK)
            .ProducesProblemDetails(StatusCodes.Status400BadRequest)
            .ProducesProblemDetails(StatusCodes.Status401Unauthorized)
            .ProducesProblemDetails(StatusCodes.Status403Forbidden));
    }

    public override async Task HandleAsync(UploadSeriesLogoRequest req, CancellationToken ct)
    {
        var storedFile = await stagingService.StageUploadAsync(req.File, "bowlneba-public", "series/logo", null, ct);

        // Stryker disable once Statement
        await Send.OkAsync(new UploadedFileResponse
        {
            Container = storedFile.Container,
            Path = storedFile.Path,
            FileName = req.File.FileName,
            ContentType = storedFile.ContentType,
            SizeInBytes = storedFile.SizeInBytes,
            Url = fileStorageService.GetBlobUri(storedFile.Container, storedFile.Path),
        }, ct);
    }
}
```

`UploadSeriesLogoRequest` (`IFormFile File`) + `UploadSeriesLogoRequestValidator` (same image type and size rules as the tournament logo validator, through `FileUploadValidationRules`) + `UploadSeriesLogoSummary`.

#### `EditSeriesLogo/`

Logo-only edit, for a caller who holds `Series.EditSeriesLogo` but not `Series.EditSeries`. Works while locked.

```csharp
internal sealed record EditSeriesLogoCommand
    : ICommand<Updated>
{
    public required SeriesId SeriesId { get; init; }

    /// <summary>Null removes the logo.</summary>
    public StoredFile? Logo { get; init; }
}

internal sealed class EditSeriesLogoCommandHandler(AppDbContext appDbContext, IBackgroundJobScheduler backgroundJobScheduler)
    : ICommandHandler<EditSeriesLogoCommand, Updated>
{
    public async Task<ErrorOr<Updated>> HandleAsync(EditSeriesLogoCommand command, CancellationToken cancellationToken)
    {
        var series = await appDbContext.Series.SingleOrDefaultAsync(s => s.Id == command.SeriesId, cancellationToken);
        if (series is null)
        {
            return SeriesErrors.NotFound(command.SeriesId);
        }

        var logoClaim = await StoredFileClaim.EnsureClaimableAsync(
            appDbContext, command.Logo, series.Logo, SeriesErrors.LogoNotUploaded, cancellationToken);
        if (logoClaim.IsError)
        {
            return logoClaim.Errors;
        }

        var previousLogo = series.Logo;
        var logo = StoredFileClaim.Resolve(command.Logo, previousLogo);
        series.ChangeLogo(logo);

        await PendingUploadCleaner.RemoveClaimedAsync(appDbContext, logo, cancellationToken);
        await appDbContext.SaveChangesAsync(cancellationToken);

        StoredFileCleanup.ScheduleDeleteOfReplaced(backgroundJobScheduler, previousLogo, logo);

        return Result.Updated;
    }
}
```

`EditSeriesLogoEndpoint`: `Put("{id}/logo")`, `Policies(PermissionCatalog.EditSeriesLogo.PolicyName)`, `WithName("EditSeriesLogo")`, produces 204, 400, 401, 403, 404, 422. The request is `EditSeriesLogoRequest { Id, Input: EditSeriesLogoInput { Logo: SeriesLogoInput? } }`; validator checks `Id`. Plus `EditSeriesLogoSummary`.

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
| `POST /series/logo` | `UploadSeriesLogoEndpoint` | `CanUploadSeriesLogo` (any of Create, Edit, EditSeriesLogo) | 200, 400, 401, 403 |
| `PUT /series/{id}/logo` | `EditSeriesLogoEndpoint` | `Permission:Series.EditSeriesLogo` | 204, 400, 401, 403, 404, 422 |

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

#### Series on the tournament list and detail

Both tournament reads gain an optional `Series`. The Tournaments feature reads Series data through `AppDbContext` using Domain types only, so `FeatureBoundaryTests` stays green. Inside `Neba.Api.Features.Tournaments.*`, the simple name `Series` resolves to the `Neba.Api.Features.Series` namespace, so the code below never names the type; it reaches the table through `appDbContext.Series`.

`src/Neba.Api.Contracts/Tournaments/TournamentSeriesResponse.cs` (next to `DefendingTeamResponse`, which `SeasonTournamentResponse` already uses the same way):

```csharp
namespace Neba.Api.Contracts.Tournaments;

/// <summary>The Series a tournament is a Stage of.</summary>
public sealed record TournamentSeriesResponse
{
    public required string SeriesId { get; init; }
    public required string Name { get; init; }

    /// <summary>The Series' own logo, or null (the page shows initials).</summary>
    public Uri? LogoUrl { get; init; }

    /// <summary>Qualifying, Survivor or Placement (SmartEnum name).</summary>
    public required string Kind { get; init; }

    /// <summary>Top N (Qualifying) or bowlers advancing (Survivor). Null on a Placement Stage.</summary>
    public int? AdvancingCount { get; init; }

    /// <summary>Survivor Stages only.</summary>
    public DateOnly? SquadDate { get; init; }
    public TimeOnly? SquadStartTime { get; init; }

    /// <summary>Placement Stages only, and only when the Series has a Finale: how many finalists go to it.</summary>
    public int? FinaleCount { get; init; }

    /// <summary>Placement Stages only, when the Series has a Finale.</summary>
    public DateOnly? FinaleDate { get; init; }
    public TimeOnly? FinaleStartTime { get; init; }
}
```

Add `public TournamentSeriesResponse? Series { get; init; }` to `SeasonTournamentResponse` and `TournamentDetailResponse`, with a `<summary>`: "The Series this tournament is a Stage of, or null." Team size is already on both responses (`TeamSize`), so the "(6 bowlers)" line needs nothing new.

`Features/Tournaments/TournamentSeriesDto.cs` (same fields including the three Finale ones, `TournamentSeriesDto`, with `SeriesId` as `SeriesId`, `Kind` as `string`, `LogoUrl` as `Uri?`).

`Features/Tournaments/TournamentSeriesReader.cs`: one batched read for the tournaments on a page, the same shape as `DefendingTeamsReader`. A tournament appears in at most one Series (rule #1), so the dictionary has no duplicates. Cancelled tournaments are not filtered out (see Decisions).

```csharp
using Microsoft.EntityFrameworkCore;

using Neba.Api.Database;
using Neba.Api.Features.Tournaments.Domain;
using Neba.Api.Storage;

namespace Neba.Api.Features.Tournaments;

internal sealed class TournamentSeriesReader(AppDbContext appDbContext, IFileStorageService fileStorageService)
{
    public async Task<IReadOnlyDictionary<TournamentId, TournamentSeriesDto>> LoadAsync(
        IReadOnlyCollection<TournamentId> tournamentIds, CancellationToken cancellationToken)
    {
        if (tournamentIds.Count == 0)
        {
            return new Dictionary<TournamentId, TournamentSeriesDto>();
        }

        var rows = await appDbContext.Series.AsNoTracking()
            .SelectMany(series => series.Stages
                .Where(stage => tournamentIds.Contains(stage.TournamentId))
                .Select(stage => new
                {
                    stage.TournamentId,
                    SeriesId = series.Id,
                    series.Name,
                    LogoContainer = series.Logo == null ? null : series.Logo.Container,
                    LogoPath = series.Logo == null ? null : series.Logo.Path,
                    stage.Kind,
                    stage.AdvancingCount,
                    SquadDate = stage.SquadDesignation == null ? (DateOnly?)null : stage.SquadDesignation.Date,
                    SquadStartTime = stage.SquadDesignation == null ? (TimeOnly?)null : stage.SquadDesignation.StartTime,
                    FinaleCount = series.Finale == null ? (int?)null : series.Finale.FinalistCount,
                    FinaleDate = series.Finale == null ? (DateOnly?)null : series.Finale.Date,
                    FinaleStartTime = series.Finale == null ? (TimeOnly?)null : series.Finale.StartTime,
                }))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            row => row.TournamentId,
            row => new TournamentSeriesDto
            {
                SeriesId = row.SeriesId,
                Name = row.Name,
                LogoUrl = row.LogoContainer is not null && row.LogoPath is not null
                    ? fileStorageService.GetBlobUri(row.LogoContainer, row.LogoPath)
                    : null,
                Kind = row.Kind.Name,
                AdvancingCount = row.AdvancingCount,
                SquadDate = row.SquadDate,
                SquadStartTime = row.SquadStartTime,

                // Only the Placement Stage points at the Finale; other rounds don't mention it.
                FinaleCount = row.Kind == StageKind.Placement ? row.FinaleCount : null,
                FinaleDate = row.Kind == StageKind.Placement ? row.FinaleDate : null,
                FinaleStartTime = row.Kind == StageKind.Placement ? row.FinaleStartTime : null,
            });
    }
}
```

Register it scoped, next to `DefendingTeamsReader`. If EF can't translate the `SelectMany` with `Contains` over the owned Stages (the `StageTournamentFactsReader` above uses the same pattern, so it should), fall back to querying `appDbContext.Series.SelectMany(series => series.Stages, ...)` and filtering after the join.

`Features/Tournaments/TournamentSeriesDtoMapping.cs`: `ToResponse()` on `TournamentSeriesDto`, in the C# 14 `extension` style the other mapping files use (`SeriesId.Value.ToString()`).

Handlers:

- `ListTournamentsInSeasonQueryHandler`: take `TournamentSeriesReader`; after `rows` loads, `var seriesByTournament = await tournamentSeriesReader.LoadAsync([.. rows.Select(row => row.Id)], cancellationToken);` and set `Series = seriesByTournament.GetValueOrDefault(row.Id)` on each `SeasonTournamentDto`. `ListUpcomingTournamentsQueryHandler` calls this handler, so the upcoming tab gets it with no change. Add `TournamentSeriesDto? Series` to `SeasonTournamentDto`, and `Series = tournament.Series?.ToResponse()` in `SeasonTournamentDtoMapping.ToResponse`.
- `GetTournamentQueryHandler`: take the reader; `LoadAsync([query.Id], ...)` and set `Series = seriesByTournament.GetValueOrDefault(query.Id)`. Add `TournamentSeriesDto? Series` to `TournamentDetailDto`, and `Series = dto.Series?.ToResponse()` in `GetTournamentEndpoint`'s mapping.
- The `GetTournamentSummary` and `ListTournamentsInSeasonSummary` text gain: "Includes the Series the tournament is a Stage of, when it is one."

Cache. Both reads are cached under the `neba:tournaments` tag (`CacheDescriptors.Tournaments.ListForSeason` and `TournamentDetail`), so a Series change has to clear it. In `CacheDescriptors.Tournaments`, change `private const string Tag` to `public const string Tag`. Each of `CreateSeriesCommandHandler`, `EditSeriesCommandHandler`, `DeleteSeriesCommandHandler` and `EditSeriesLogoCommandHandler` takes `IFusionCache cache` (not `HybridCache`; query caching in this repo uses FusionCache) and ends with:

```csharp
await cache.RemoveByTagAsync(CacheDescriptors.Tournaments.Tag, token: cancellationToken);
```

after `SaveChangesAsync`. This adds `IFusionCache cache` to those handler constructors; update the earlier handler snippets when coding. Nothing else changes in the Series write path.

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

        builder.HasStoredFile(series => series.Logo,
            containerColumnName: "logo_container",
            filePathColumnName: "logo_file_path",
            contentTypeColumnName: "logo_content_type",
            sizeInBytesColumnName: "logo_size_in_bytes");

        // The Finale is owned by the Series and stored in its row. Its required columns make EF read it as
        // null when every finale_* column is null (a Series with no Finale).
        builder.OwnsOne(series => series.Finale, finale =>
        {
            finale.Property(f => f.FinalistCount).HasColumnName("finale_finalist_count").IsRequired();
            finale.Property(f => f.Date).HasColumnName("finale_date").IsRequired();
            finale.Property(f => f.StartTime).HasColumnName("finale_start_time").IsRequired();
            finale.Property(f => f.BowlingCenterId).HasColumnName("finale_bowling_center_id").HasMaxLength(6);

            finale.HasOne<BowlingCenter>()
                .WithMany()
                .HasForeignKey(f => f.BowlingCenterId)
                .HasPrincipalKey(bowlingCenter => bowlingCenter.CertificationNumber)
                .OnDelete(DeleteBehavior.NoAction);
        });

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
        builder.Ignore(series => series.HasFinale);
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

`dotnet ef migrations add AddSeries --project src/Neba.Api` adds `series` (with nullable `logo_*` columns and the nullable `finale_*` columns: `finale_finalist_count`, `finale_date`, `finale_start_time`, `finale_bowling_center_id`, with an FK to `bowling_centers.certification_number`, `NO ACTION`, and a check that the first three are all set or all null) and `series_stages` (`advancing_count` nullable: always null on a Placement Stage), the unique indexes on `(series_id, stage_order)` and `tournament_id`, and FKs to `seasons` and `tournaments` (`NO ACTION`) and to `series` (`CASCADE`).

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

    /// <summary>Lists the Finale of every Series that has one, soonest first. Used by the tournament list.</summary>
    [Get("/series/finales")]
    Task<IApiResponse<CollectionResponse<SeriesFinaleSummaryResponse>>> ListSeriesFinalesAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets a Series and its Stage schedule.</summary>
    [Get("/series/{seriesId}")]
    Task<IApiResponse<SeriesDetailResponse>> GetSeriesAsync(string seriesId, CancellationToken cancellationToken = default);

    /// <summary>Creates a Series. Requires Series.CreateSeries.</summary>
    [Post("/series")]
    Task<IApiResponse<CreatedSeriesResponse>> CreateSeriesAsync(CreateSeriesRequest request, CancellationToken cancellationToken = default);

    /// <summary>Saves a Series' name, full Stage list and Finale. Requires Series.EditSeries.</summary>
    [Put("/series/{seriesId}")]
    Task<IApiResponse> EditSeriesAsync(string seriesId, EditSeriesRequest request, CancellationToken cancellationToken = default);

    /// <summary>Deletes an unlocked Series and its Stages. Requires Series.DeleteSeries.</summary>
    [Delete("/series/{seriesId}")]
    Task<IApiResponse> DeleteSeriesAsync(string seriesId, CancellationToken cancellationToken = default);

    /// <summary>Stages a logo file. Requires Series.CreateSeries, Series.EditSeries, or Series.EditSeriesLogo.</summary>
    [Multipart]
    [Post("/series/logo")]
    Task<IApiResponse<UploadedFileResponse>> UploadSeriesLogoAsync([AliasAs("file")] StreamPart file, CancellationToken cancellationToken = default);

    /// <summary>Replaces or removes the logo only. Requires Series.EditSeriesLogo. Works while locked.</summary>
    [Put("/series/{seriesId}/logo")]
    Task<IApiResponse> EditSeriesLogoAsync(string seriesId, EditSeriesLogoRequest request, CancellationToken cancellationToken = default);
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

    /// <summary>A file staged through <c>POST /series/logo</c>, or null for no logo.</summary>
    public SeriesLogoInput? Logo { get; init; }

    /// <summary>Stages in order. Order is the list position.</summary>
    public required IReadOnlyList<SeriesStageInput> Stages { get; init; }

    /// <summary>The Finale, or null for a Series with none.</summary>
    public SeriesFinaleInput? Finale { get; init; }
}

// CreateSeries/SeriesStageInput.cs
public sealed record SeriesStageInput
{
    /// <summary>Existing Stage ID when editing; null for a new Stage.</summary>
    public string? Id { get; init; }

    /// <summary>"Qualifying", "Survivor", or "Placement".</summary>
    public required string Kind { get; init; }

    public required string TournamentId { get; init; }

    /// <summary>Required on Qualifying and Survivor Stages; a Placement Stage has none.</summary>
    public int? AdvancingCount { get; init; }

    /// <summary>Survivor Stages only.</summary>
    public DateOnly? SquadDate { get; init; }

    /// <summary>Survivor Stages only.</summary>
    public TimeOnly? SquadStartTime { get; init; }
}

// CreateSeries/SeriesFinaleInput.cs
public sealed record SeriesFinaleInput
{
    /// <summary>Bowlers from the Placement Round who go to the Finale.</summary>
    public required int FinalistCount { get; init; }

    /// <summary>Nullable so a missing date is a 400 naming the field, not a default date.</summary>
    public DateOnly? Date { get; init; }
    public TimeOnly? StartTime { get; init; }

    /// <summary>Certification number of the host bowling center, or null while it is to be announced.</summary>
    public string? BowlingCenterId { get; init; }
}

// CreateSeries/SeriesLogoInput.cs (same shape as TournamentLogoInput)
public sealed record SeriesLogoInput
{
    public required string Container { get; init; }
    public required string Path { get; init; }
    public required string ContentType { get; init; }
    public required long SizeInBytes { get; init; }
}

// EditSeriesLogo/EditSeriesLogoRequest.cs, EditSeriesLogoInput.cs
public sealed record EditSeriesLogoRequest
{
    public required string Id { get; init; }
    public required EditSeriesLogoInput Input { get; init; }
}

public sealed record EditSeriesLogoInput
{
    /// <summary>Null removes the logo.</summary>
    public SeriesLogoInput? Logo { get; init; }
}

// UploadSeriesLogo/UploadSeriesLogoRequest is API-side (IFormFile); the client sends a StreamPart.

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

    /// <summary>The current logo (resent unchanged), a newly staged file, or null to remove it.</summary>
    public SeriesLogoInput? Logo { get; init; }
    public required IReadOnlyList<SeriesStageInput> Stages { get; init; }

    /// <summary>The Finale, or null to remove it (refused while locked).</summary>
    public SeriesFinaleInput? Finale { get; init; }
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
    public Uri? LogoUrl { get; init; }

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

    /// <summary>The Finale, or null when the Series has none.</summary>
    public SeriesFinaleResponse? Finale { get; init; }
    public Uri? LogoUrl { get; init; }
    public string? LogoContainer { get; init; }
    public string? LogoPath { get; init; }
    public string? LogoContentType { get; init; }
    public long? LogoSizeInBytes { get; init; }
    public required IReadOnlyCollection<SeriesStageResponse> Stages { get; init; }
}

public sealed record SeriesFinaleResponse
{
    public required int FinalistCount { get; init; }
    public required DateOnly Date { get; init; }
    public required TimeOnly StartTime { get; init; }

    /// <summary>Host center, or null while it is to be announced.</summary>
    public string? BowlingCenterId { get; init; }
    public string? BowlingCenterName { get; init; }
    public string? BowlingCenterCity { get; init; }
    public string? BowlingCenterState { get; init; }
}

// ListSeriesFinales/SeriesFinaleSummaryResponse.cs
public sealed record SeriesFinaleSummaryResponse
{
    public required string SeriesId { get; init; }
    public required string SeriesName { get; init; }
    public required string SeasonId { get; init; }
    public Uri? LogoUrl { get; init; }
    public required SeriesFinaleResponse Finale { get; init; }
}

public sealed record SeriesStageResponse
{
    public required string Id { get; init; }
    public required int Order { get; init; }
    public required string Kind { get; init; }
    public required string Status { get; init; }
    public int? AdvancingCount { get; init; }
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

/// <summary>
/// Permission to replace or remove a Series' logo without being able to edit the rest of the Series.
/// </summary>
public static readonly Permissions EditSeriesLogo = new("Series.EditSeriesLogo", "Edit Series Logo");

/// <summary>
/// Permissions that let a caller upload a Series logo to staging.
/// </summary>
public static readonly IReadOnlyCollection<Permissions> SeriesLogoUploadPermissions =
[
    CreateSeries,
    EditSeries,
    EditSeriesLogo
];

/// <summary>
/// Policy name satisfied when the caller holds any permission in <see cref="SeriesLogoUploadPermissions"/>.
/// </summary>
public const string CanUploadSeriesLogoPolicyName = "CanUploadSeriesLogo";

#endregion
```

#### `Security/Infrastructure/SecurityRoleSeeder.cs`

Add to the `[Roles.Manager]` list:

```csharp
Permissions.CreateSeries,
Permissions.EditSeries,
Permissions.DeleteSeries,
Permissions.EditSeriesLogo,
```

#### `Neba.Api.Contracts/Security/PolicyExtensions.cs`

Registered once for both the API and the website (`AddNebaPolicies()`):

```csharp
builder.AddPolicy(Permissions.CanUploadSeriesLogoPolicyName, policy => policy
    .RequireAssertion(context => context.User.HasAnyPermission(Permissions.SeriesLogoUploadPermissions)));
```

Admin already has them through `Permissions.List`.

### Tests and factories

#### Factories (`tests/Neba.TestFactory/Series/`)

Tournament-side factories (in `tests/Neba.TestFactory/Tournaments/`, next to `DefendingTeamDtoFactory`): `TournamentSeriesDtoFactory` and `TournamentSeriesResponseFactory`, each with `Create()` (Qualifying, const defaults) and `Bogus(count, seed?)`; a `series` parameter (default null) on `SeasonTournamentDtoFactory` and `TournamentDetailDtoFactory`.

```csharp
public static class SeriesFactory
{
    public const string ValidName = "Vincent Hall Memorial Series";

    /// <summary>No Stages by default (an incomplete Series is valid), so it persists with only a season.</summary>
    public static Series Create(
        string? name = null,
        SeasonId? seasonId = null,
        StoredFile? logo = null,
        IReadOnlyList<SeriesStageDefinition>? stages = null,
        SeriesFinaleDefinition? finale = null,
        IReadOnlyDictionary<TournamentId, StageTournamentFacts>? tournaments = null)
    {
        var result = Series.Create(
            name ?? ValidName,
            seasonId ?? SeasonId.New(),
            logo,
            stages ?? [],
            finale,
            tournaments ?? new Dictionary<TournamentId, StageTournamentFacts>());

        return result.IsError
            ? throw new InvalidOperationException($"Failed to create series: {result.Errors[0].Description}")
            : result.Value;
    }

    /// <summary>Builds a Series whose Stages run against the given tournaments, deriving the facts from them.</summary>
    public static Series CreateFor(
        Season season,
        IReadOnlyList<(Tournament Tournament, StageKind Kind, int? AdvancingCount, DateOnly? SquadDate, TimeOnly? SquadStartTime)> stages,
        SeriesFinaleDefinition? finale = null,
        string? name = null);

    public static IReadOnlyCollection<Series> Bogus(int count, int? seed = null);
}

public static class SeriesStageDefinitionFactory
{
    public static SeriesStageDefinition Create(
        SeriesStageId? stageId = null,
        StageKind? kind = null,
        TournamentId? tournamentId = null,
        int? advancingCount = ValidAdvancingCount, // pass null for a Placement Stage, which has no count
        DateOnly? squadDate = null,
        TimeOnly? squadStartTime = null);
}

public static class SeriesFinaleDefinitionFactory
{
    /// <summary>Four finalists on 9/27/2025 at 2:00 PM, no host center yet.</summary>
    public static SeriesFinaleDefinition Create(
        int? finalistCount = null,
        DateOnly? date = null,
        TimeOnly? startTime = null,
        CertificationNumber? bowlingCenterId = null);
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
    /// <param name="withFinale">True: a Finale of 4 finalists (the 2025 Series) on 9/27/2025 at 2:00 PM with no host center yet. False: the same Stages and no Finale.</param>
    public static (Season Season, IReadOnlyList<Tournament> Tournaments, IReadOnlyList<SeriesStageDefinition> Stages, SeriesFinaleDefinition? Finale) Create(bool withFinale = true);
}
```

Response and DTO factories: `SeriesLogoInputFactory`, `EditSeriesLogoInputFactory`, `SeriesSummaryDtoFactory`, `SeriesNextStageDtoFactory`, `SeriesNextStageResponseFactory`, `SeriesDetailDtoFactory`, `SeriesStageDtoFactory`, `SeriesSummaryResponseFactory`, `SeriesDetailResponseFactory`, `SeriesStageResponseFactory`, `SeriesInputFactory`, `SeriesStageInputFactory`. Each has `Create()` with const defaults and `Bogus(count, seed?)`.

#### Domain unit tests (`tests/Neba.Api.Tests/Features/Series/Domain/`)

All tests carry `[UnitTest]`, `[Component("Series")]`, a `DisplayName`, and AAA comments.

- `SeriesTests`
  - Create: valid, empty (incomplete), blank name, name too long.
  - #1: tournament not found, duplicate in the same Series, in another Series (Conflict).
  - #2: tournament in a different season.
  - #3: Survivor before Qualifying, Qualifying after Placement, two Placements, Placement not last.
  - #4: equal start date, earlier start date.
  - #5: Survivor count equal to the previous one, larger than the previous one.
  - #11: a Finale dated before, on the same day as, and the day after the latest Stage's end date (only the day after passes); a multi-day Stage's end date, not its start date, is what counts; the error names the latest-ending Stage only; a Finale with no Stages passes; a Qualifying-only Series (no Survivor, no Placement) with a Finale passes; a locked Series rescheduling its Finale before the last Stage → error and nothing changes.
  - #6: Finale count of 0 and of -1 (`FinaleCountNotPositive`, property `Series.Finale.FinalistCount`); Finale count larger than the last Survivor count; equal to it is allowed; allowed when there is no Survivor Stage; no Finale passes with no check.
  - #8: an empty count on a Qualifying Stage and on a Survivor Stage (`AdvancingCountRequired`); a count on a Placement Stage (`AdvancingCountNotAllowed`).
  - #9: `HasFinale` true when a Finale is given, false when it is null, with or without a Placement Stage; a Finale on a Series with no Placement Stage yet is allowed (an incomplete Series).
  - Several errors returned in one pass, each with the right `StageOrder`.
  - `ReplaceConfiguration`: updates in place and keeps the Stage id, reorders, removes a missing Stage, adds a new Stage, unknown Stage id → error; adds, changes and removes the Finale; Stage and Finale errors come back in one pass.
  - Lock: `ReplaceConfiguration` with changed Stages → `Locked`; adding a Finale, removing it, or changing its count while locked → `Locked`; changing only the Finale's date, start time or center while locked → Updated; unchanged → Updated; `Rename` and `ChangeLogo` allowed; `EnsureCanDelete` → `Locked`. The test sets `Status = StageStatus.Computed` through the internal setter.
  - `Stages` collection regression: `((ICollection<SeriesStage>)series.Stages)` is read-only and the backing list accepts adds through `ReplaceConfiguration` (EF collection learning).
- `SeriesStageTests`
  - #8: advancing count of 0 and -1 on Qualifying and Survivor; empty on Qualifying and Survivor; empty on Placement accepted; any count on Placement rejected.
  - #7: Survivor without a squad, Qualifying with a squad, Placement with a squad, squad date before or after the tournament, squad on a multi-day tournament's second day allowed.
  - `Matches`.
- `SeriesErrorsTests`: descriptions name the Stage and tournament; metadata carries `StageOrder`; Finale errors carry the `PropertyKey` metadata (`Series.Finale.FinalistCount`, `Series.Finale.BowlingCenterId`) and no `StageOrder`.
- `SeriesFinaleTests`: `Describe` copies every field; a record with the same values is equal.

#### Handler unit tests

These use the in-memory `AppDbContext` test helper the other handler tests use (check `Features/Tournaments` handler tests for the current pattern).

- `ListSeriesQueryHandlerTests` (integration, real database): next Stage is the first unfinished one by order; a tournament ending today still counts (cut-off minus one day, via `FakeTimeProvider`); null when every tournament has ended; newest season first.
- `CreateSeriesCommandHandlerTests`: season missing → 422 error; Finale center missing or not Open → `Series.Finale.BowlingCenter.NotAvailable`; unstaged logo → `LogoNotUploaded`; domain errors bubble up; saves on success and removes the claimed pending upload.
- `EditSeriesCommandHandlerTests`: not found; Finale center missing or not Open → error and nothing saved; keeping a center that has since closed succeeds; rescheduling the Finale on a locked Series succeeds; removing it on a locked Series → `Locked`; rename on a locked Series with unchanged Stages succeeds; logo change on a locked Series succeeds; the Series' own tournaments aren't reported as "in another Series"; a changed logo with a stored `EditSeriesLogo` denial → `LogoChangeDenied` (strict mock of `IPermissionDenialReader`); an unchanged logo skips the denial read; an unstaged logo → `LogoNotUploaded`; a replaced logo schedules `DeleteStoredFilesJob`.
- `EditSeriesLogoCommandHandlerTests`: not found; unstaged → `LogoNotUploaded`; replace schedules deletion of the old file; null removes; works while locked.
- `DeleteSeriesCommandHandlerTests`: missing → Deleted; locked → Conflict; removes the Series and schedules deletion of its logo.
- `DeleteTournamentCommandHandlerTests` (existing file): new case where the tournament is a Stage → `Tournament.IsSeriesStage`.
- `SeriesFinaleChecksTests` (integration, real database): no Finale and a Finale with no center both pass without a query; an Open center passes; an unknown center and a Closed or Uncertified center → `Series.Finale.BowlingCenter.NotAvailable`; the center the Finale already has passes even when it is now Closed.
- `ListSeriesFinalesQueryHandlerTests` (integration, real database): returns only Series that have a Finale, soonest first (date, then start time); the host name, city and state come from the center and are null with no center; the logo URL is set only when the Series has a logo (strict mock of `IFileStorageService.GetBlobUri`); empty when no Series has a Finale.
- `TournamentSeriesReaderTests` (integration, real database): a Stage tournament returns the Series id, name, kind, advancing count and, for Survivor, the squad; a tournament in no Series is absent; a Series with a logo returns its URL (strict mock of `IFileStorageService.GetBlobUri`) and one without returns null; a Placement Stage returns the Finale count, date and start time when the Series has a Finale and nulls when it has none; Qualifying and Survivor Stages never carry the Finale fields; a cancelled tournament still returns its Series; an empty id list returns an empty dictionary without querying; only the requested ids come back.
- `ListTournamentsInSeasonQueryHandlerTests` and `GetTournamentQueryHandlerTests` (existing files): `Series` is set for a Stage tournament and null otherwise (strict mock of the reader's collaborators, or the real reader against the test database, whichever the file already uses).
- `CreateSeriesCommandHandlerTests`, `EditSeriesCommandHandlerTests`, `DeleteSeriesCommandHandlerTests`, `EditSeriesLogoCommandHandlerTests`: each verifies `RemoveByTagAsync("neba:tournaments")` runs after a successful save, and not on a failed one (strict mock of `IFusionCache`).
- `SeriesMutationResultSenderTests`: 422 when any Validation error is present; 409 when all are Conflicts; property name `Series.Stages[i]` for a Stage error and the `PropertyKey` value for a Finale error.
- `SeriesStageInputValidatorTests`, `SeriesFinaleInputValidatorTests` (missing date, missing start time, center id too long), `CreateSeriesRequestValidatorTests`, `EditSeriesRequestValidatorTests`.

#### Integration tests (`[IntegrationTest]`)

- `SeriesEndpointsTests`:
  - Anonymous `GET /series` and `GET /series/{id}` work; `GET` of an unknown id → 404.
  - Writes: anonymous → 401; Member → 403; Manager and Admin → success.
  - `GET /series/finales` is anonymous, lists Finales soonest first, and is not captured by `GET /series/{id}`.
  - `POST` → 201 with a Location header; a broken row → 422 with `errors[].name == "Series.Stages[2]"`; a tournament in another Series → 409.
  - `PUT` → 204; renaming a locked Series → 204; changing a locked Series' Stages → 409; rescheduling a locked Series' Finale → 204; removing it, or changing its count → 409.
  - A Finale dated on or before a Stage's end date → 422 naming `Series.Finale.Date`; a Finale with an unknown or closed center → 422 naming `Series.Finale.BowlingCenterId`; a count of 0 → 422 naming `Series.Finale.FinalistCount`; a missing date → 400 naming the field; a count on a Placement Stage → 422 naming that Stage.
  - `DELETE` → 204, again → 204; locked → 409.
- `Series2025ReferenceTests`: seeds the season and the 2025 tournaments, `POST`s the reference configuration, `GET`s it back, and asserts all 11 Stages: order, kind, advancing count, squad on rows 9 and 10. Runs twice: with a Finale (a Finale of 4 finalists, 9/27/2025 at 2:00 PM, host center set; row 11 has no count) and without one (the `finale` object absent).
- `SeriesLogoEndpointsTests`: `POST /series/logo` stages a file for each of the three permissions and is 403 without them; `PUT /series/{id}/logo` sets, replaces and removes; 403 without `EditSeriesLogo`; works on a locked Series; `GET /series/{id}` returns the logo URL and fields; `PUT /series/{id}` with a changed logo is 403 for a caller with a stored denial.
- `SeriesConfigurationTests`: round-trips a Series with mixed Stages, including a null designation, and a Series with a Finale (with and without a center) and one without; the center FK rejects an unknown certification number (centers are never deleted, so no delete case); the unique `tournament_id` index rejects a second Stage on the same tournament across Series.
- `DeleteTournamentEndpointTests` (existing): 409 naming the Series, then 204 once the Stage is removed.
- Tournament list and detail endpoint tests (existing files): the response carries `series` for a Stage tournament (all three kinds; a Placement Stage with and without a Finale) and omits it otherwise; the upcoming list carries it too; a cancelled Stage tournament still carries it.
- Cache: read the tournament list and detail, rename the Series with `PUT /series/{id}`, read again and see the new name; the same after a logo change and after a delete (the tag is gone).
- `EfAuditIntegrationTests` (existing): Series and SeriesStage changes produce audit events.
- `FeatureBoundaryTests` stays green with no new entries in `KnownSliceViolations`.

### Docs

- [ ] `docs/ubiquitous-language.md`: new `## Series` section after `## Tournaments`, taken from the downloaded `vincent-hall-series-ul.md`. Each entry uses the file's format (**Definition**, **Rules**, **In Code**, separated by `---`).

  | Entry | Content from the source doc | Status note |
  | --- | --- | --- |
  | Series | Part 1 definition; "Proposed rule: Series" 1 to 4 (a Series Position's payout may be zero); one season, normally one Series, may have more; with or without a Finale; Series vs Lineage note | In Code: `Series`, `SeriesId`, `Series.Logo`, `Series.Finale`, `Series.HasFinale`, `series` table |
  | Stage | Definition; a tournament is a Stage in at most one Series; at most one Placement Round and it's last; order follows tournament date; locked once processing begins (Locked Series; Name, logo and the Finale's date, start time and center stay editable) | In Code: `SeriesStage`, `SeriesStageId`, `Series.ReplaceStages` |
  | Stage Kind | The three kinds; Survivor Round vs Placement Round note | In Code: `StageKind` |
  | Qualifying Event | Definition; "Proposed rule: Qualifying" 1 to 4 | Rules 2 to 4 computed in phase 2 |
  | Survivor Round | Definition; "Proposed rule: Survivor Rounds" 1 to 5 (rule 4: a bowler who doesn't finish the squad is scored on the games bowled) | Advancement computed in phase 2 |
  | Placement Round | Definition (with a Finale, the top finishers go to it; without, its positions are the final standings; a Placement Stage has no advancing count of its own); "Proposed rule: Placement Round" 1 to 4 (no-shows ranked last, alphabetically; everyone gets a Series Position and its payout, which may be zero) | Ranking computed in phase 2 |
  | Advancing Count | Required and above zero on Qualifying (top N; teams count N × team size) and Survivor (bowlers advancing), and Survivor counts decrease; the Placement Round has none (the Finale's finalist count is a separate term) | In Code: `SeriesStage.AdvancingCount` (`int?`, null on Placement) |
  | Squad Designation | Described (date + start time, as bowlers say it) vs Resolved (`SquadId`); resolves automatically when exactly one squad matches, otherwise an admin picks; an unresolved Survivor Round is never processed | Described in phase 1; resolution in phase 2 |
  | Stage Status | Pending, Computed, Announced; any non-pending Stage locks the Series; a job writes a Stage's output only while it isn't announced; an announced field changes only through an admin recompute; **Announce applies to Qualifying and Survivor Stages only**, and the Placement Round's commit points are a replacement or Finale result (with a Finale) or recorded payouts (without one) | In Code: `StageStatus`; transitions in phase 2 |
  | Series Field | Definition; stored at each Stage boundary; advancing count never grows the field, ties can exceed it | Phase 2, no code yet |
  | Backfill | Definition (open-category BOTY points as of the last-completed qualifier, date stored with the field; ties advance); no passing slots down the finish list; the Backfill trigger steps 1 to 4 | Phase 2, no code yet |
  | Series Position | A bowler's final rank: from the Placement Round, or from the Finale for finalists; unique | Phase 2/3, no code yet |
  | Finale | Optional last event of a Series, bowled outside the system after the Placement Round, with its own date, start time and host bowling center (the center can be set later); not a tournament and not linked to one; a Series has a Finale exactly when it has one, no separate flag; its **finalist count** is above zero and no larger than the last Survivor count; no Stage can come after it (every Stage's tournament ends before its date), and Survivor Rounds and a Placement Round are optional before it; finalists' positions provisional until the Finale result; replacement swaps Series Position and payout ("Proposed rule: Finale" 1); its name is always "{Series name} Finale"; appears in the tournament list; locked once processing begins, except its date, start time and center | In Code: `SeriesFinale`, `SeriesFinaleDefinition`, `Series.Finale`; result and positions in phase 3 |
  | Series Champion | The Finale winner, or Position 1 of the Placement Round when there's no Finale; distinct from the NEBA Title; the defending Series Champion is display only | Phase 3, no code yet |
  | Series Payout | Amount per Series Position, set per Series by the special tournament committee with the sponsor; zero or more (zero is valid); counts in the same earnings figures as tournament winnings; feeds the 1099s | Phase 3, no code yet |

  - The `## Series` section intro carries the summary paragraph from the source doc (including "earlier Series had no Finale"), the rules basis (posted 2025 rules; special tournament committee with the sponsor sets payouts; USBC rules outrank tournament rules), and the 2025 reference configuration from #258, both with a Finale and without one, as the worked example.
  - A short **Computation (phase 2)** note from the source doc's Computation section: replayable (a fold over the Stages in order), automatic on tournament completion plus an admin recompute, Qualifying from result places alone, Placement from `TournamentResult.Place`, partial Survivor squads count, team bowlers share a place.
  - A **Rules gaps (NEBA practice, not yet codified)** list, from #257's "Rules gaps to codify" 1 to 9 (9: the Finale is optional). Add a 10th: the Finale is its own event with a date, start time and host, and can be rescheduled after processing begins.
  - No open-items list: the source doc now says "Open items: None".
  - The existing `### Cup` entry under `## Stats` gains one line: "Not the Vincent Hall Memorial Series (formerly the Paul Forry Memorial Cup). Cup is a reserved term for the legacy points-based Women's Cup and is not modeled."
  - "Lineage" is named only in the Series vs Lineage note. It belongs to the Defending Champion work (`defending-champion-ul.md`) and gets its own entry with that work.
- [ ] `docs/policies/README.md`: one row each for `Permission:Series.CreateSeries`, `Permission:Series.EditSeries`, `Permission:Series.DeleteSeries`, `Permission:Series.EditSeriesLogo` (seeded to Admin through `Permissions.List`, and Manager), and `CanUploadSeriesLogo` (OR of Create, Edit, EditSeriesLogo; gates `POST /series/logo`; the logo-only edit gates on `Series.EditSeriesLogo`; `PUT /series/{id}` refuses a changed logo for a caller with a stored denial). Same wording pattern as the `CanUploadTournamentLogo` row.
- [ ] `docs/help/`: `create-series.md`, `edit-series.md`, `delete-series.md`, `edit-series-logo.md` (ADR-0007). Screenshots come with the UI in phase 2 of this plan.

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
| `series-detail-option-1.html` | **Chosen.** Detail: "How the field narrows" (up to 62 → 40 → 20 → Finale 4, bars to scale), then Stages grouped by kind, then the Finale block (date, time, host). |
| `series-detail-option-2.html` | Not chosen. Detail: a month-by-month timeline of Stages with an "At a glance" sidebar that holds the admin actions. |
| `series-detail-option-3.html` | Not chosen. Detail: a summary band plus one `neba-data-table` (columns 1 to 3 primary, the rest expand on tap on phones). |
| `tournaments-list-series.html` | **Pill chosen.** The tournaments list with the Series tag (pill in the card's top row) on upcoming and past cards, and the Finale as its own card in the timeline. Checkboxes show today's cards without Series tags and hide the Finale. |
| `tournament-detail-series.html` | **For approval.** The tournament detail with the Series callout under the hero. A dropdown covers Qualifying (singles and team), Survivor with its squad, and Placement with and without a Finale; a checkbox shows today's page and another shows the initials tile. |
| `series-editor.html` | **Approved, Finale section added (pending review).** Create and edit (single mockup): season picker with change-and-clear confirmation, Stage grid, row errors from a failed save, locked read-only mode. |

**Added after approval (2026-11-17):** the Series logo (`vincent-hall-logo.png`, copied from the sponsor artwork) on the list's featured card, an initials tile on past rows, the detail hero with an admin edit-logo button, and a Logo section in the editor. Also the optional Finale: a "No Finale" toggle on the detail mockup (Placement row reads "Final standings (no Finale)", no Finale bar, a caption under the chart). **Revised the same day:** the Finale became its own event, so the detail mockup shows a Finale block (date, time, host or "to be announced"), and the editor's Placement row has no count and a separate Finale section below the grid holds the toggle, finalists, date, start time and host center.

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
    public Uri? LogoUrl { get; init; }
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
    public SeriesFinaleViewModel? Finale { get; init; }
    public Uri? LogoUrl { get; init; }

    /// <summary>The Series has a Finale exactly when it has one (the Razor reads this, not a null check).</summary>
    public bool HasFinale => Finale is not null;

    /// <summary>The stored logo, resent unchanged by the edit page.</summary>
    public SeriesLogoInput? Logo { get; init; }
    public required IReadOnlyList<SeriesStageViewModel> Stages { get; init; }
}

internal sealed record SeriesFinaleViewModel
{
    public required int FinalistCount { get; init; }
    public required DateOnly Date { get; init; }
    public required TimeOnly StartTime { get; init; }
    public string? BowlingCenterId { get; init; }

    /// <summary>"Callahan's Bowl-O-Rama, Seekonk, MA", or null while the host center is to be announced.</summary>
    public string? Host { get; init; }
}

internal sealed record SeriesStageViewModel
{
    public required string Id { get; init; }
    public required int Order { get; init; }
    public required string Kind { get; init; }
    public int? AdvancingCount { get; init; }
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

    /// <summary>
    /// "Top 8"; "Top 2 teams" + "(6 bowlers)"; "40 advance"; "Top 4 to the Finale";
    /// "Final standings" + "(no Finale)" for a Placement Stage when the Series has no Finale.
    /// A Placement Stage has no count of its own, so the caller passes the Finale's finalist count.
    /// </summary>
    public static AdvancingLabel Advancing(string kind, int? count, int teamSize, int? finaleCount = null)
        => (kind, count) switch
        {
            (StageKinds.Placement, _) when finaleCount is null => new AdvancingLabel("Final standings", "(no Finale)"),
            (StageKinds.Placement, _) => new AdvancingLabel("Top " + finaleCount!.Value.ToString(Culture) + " to the Finale", null),
            (StageKinds.Qualifying, { } n) when teamSize > 1 => new AdvancingLabel(
                "Top " + "team".ToQuantity(n),
                "(" + "bowler".ToQuantity(n * teamSize) + ")"),
            (StageKinds.Qualifying, { } n) => new AdvancingLabel("Top " + n.ToString(Culture), null),
            (StageKinds.Survivor, { } n) => new AdvancingLabel(n.ToString(Culture) + " advance", null),
            _ => new AdvancingLabel("—", null), // a saved Qualifying/Survivor Stage always has a count
        };

    /// <summary>"9:00 AM squad on 7/12"; null when the Stage has no squad.</summary>
    public static string? Squad(DateOnly? date, TimeOnly? startTime)
        => date is { } d && startTime is { } t
            ? t.ToString("h:mm tt", Culture) + " squad on " + d.ToString("M/d", Culture)
            : null;

    /// <summary>"Teams of 3" + "(6 bowlers)" under a Qualifying row's advancing input.</summary>
    public static AdvancingLabel? TeamHint(string kind, int? count, int teamSize)
        => kind == StageKinds.Qualifying && teamSize > 1
            ? new AdvancingLabel("Teams of " + teamSize.ToString(Culture),
                count is { } n ? "(" + "bowler".ToQuantity(n * teamSize) + ")" : null)
            : null;

    /// <summary>"Sat 9/27 at 2:00 PM": when the Finale is bowled.</summary>
    public static string FinaleWhen(DateOnly date, TimeOnly startTime)
        => date.ToString("ddd M/d", Culture) + " at " + startTime.ToString("h:mm tt", Culture);

    /// <summary>"Callahan's Bowl-O-Rama, Seekonk, MA"; null while the host center is to be announced.</summary>
    public static string? Host(string? name, string? city, string? state)
        => name is null
            ? null
            : string.Join(", ", new[] { name, city, state }.Where(part => !string.IsNullOrWhiteSpace(part)));

    /// <summary>"Top 4 of the Placement Round go to the Finale".</summary>
    public static string FinaleCaption(int finalistCount)
        => "Top " + finalistCount.ToString(Culture) + " of the Placement Round";

    /// <summary>"Vincent Hall Memorial Series Finale" (the name is always derived, never stored).</summary>
    public static string FinaleName(string seriesName)
        => seriesName + " Finale";

    /// <summary>The most bowlers who can qualify: each Qualifying Stage's count times its team size.</summary>
    public static int QualifierCeiling(IEnumerable<SeriesStageViewModel> stages)
        => stages.Where(s => s.Kind == StageKinds.Qualifying).Sum(s => (s.AdvancingCount ?? 0) * s.TeamSize);

    /// <summary>Steps for "How the field narrows". Only steps that exist are returned.</summary>
    public static IReadOnlyList<FieldStep> FieldSteps(IReadOnlyList<SeriesStageViewModel> stages, SeriesFinaleViewModel? finale)
    {
        var steps = new List<FieldStep>();
        var qualifying = stages.Where(s => s.Kind == StageKinds.Qualifying).ToList();

        if (qualifying.Count > 0)
        {
            steps.Add(new FieldStep("Qualifiers", "event".ToQuantity(qualifying.Count) + ", top finishers",
                QualifierCeiling(qualifying), StageKinds.Qualifying, IsCeiling: true));
        }

        steps.AddRange(stages
            .Where(s => s.Kind == StageKinds.Survivor && s.AdvancingCount is not null)
            .Select(s => new FieldStep(s.TournamentName, "Survivor · " + TournamentDates(s.TournamentStartDate, s.TournamentEndDate),
                s.AdvancingCount!.Value, StageKinds.Survivor, IsCeiling: false)));

        // No Finale step without a Finale; the chart's caption explains the Placement Round instead.
        if (finale is not null)
        {
            steps.Add(new FieldStep("Finale", FinaleWhen(finale.Date, finale.StartTime),
                finale.FinalistCount, StageKinds.Placement, IsCeiling: false));
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

    /// <summary>Two initials for a logo-less tile: "2025 Vincent Hall Memorial Series" -> "VH".</summary>
    public static string Initials(string name)
        => string.Concat(name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => char.IsLetter(word[0]))
            .Take(2)
            .Select(word => char.ToUpperInvariant(word[0])));

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
                LogoUrl = response.LogoUrl,
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
                Finale = response.Finale is { } finale
                    ? new SeriesFinaleViewModel
                    {
                        FinalistCount = finale.FinalistCount,
                        Date = finale.Date,
                        StartTime = finale.StartTime,
                        BowlingCenterId = finale.BowlingCenterId,
                        Host = SeriesDisplay.Host(finale.BowlingCenterName, finale.BowlingCenterCity, finale.BowlingCenterState),
                    }
                    : null,
                LogoUrl = response.LogoUrl,
                Logo = response.LogoContainer is not null && response.LogoPath is not null
                    ? new SeriesLogoInput
                    {
                        Container = response.LogoContainer,
                        Path = response.LogoPath,
                        ContentType = response.LogoContentType ?? "application/octet-stream",
                        SizeInBytes = response.LogoSizeInBytes ?? 0,
                    }
                    : null,
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

    /// <summary>The logo to save: the stored one (edit), a newly staged one, or null for none.</summary>
    public SeriesLogoInput? Logo { get; set; }

    /// <summary>Preview URL for <see cref="Logo"/>.</summary>
    public Uri? LogoUrl { get; set; }

    public List<SeriesStageRowModel> Stages { get; } = [];

    /// <summary>Null means the Series has no Finale. The editor's "This Series has a Finale" toggle sets or clears it.</summary>
    public SeriesFinaleFormModel? Finale { get; set; }
}

internal sealed class SeriesFinaleFormModel
{
    /// <summary>Bowlers from the Placement Round who go to the Finale. Required and above zero.</summary>
    [Required(ErrorMessage = "Number of finalists is required.")]
    public int? FinalistCount { get; set; }

    [Required(ErrorMessage = "Date is required.")]
    public DateOnly? Date { get; set; }

    [Required(ErrorMessage = "Start time is required.")]
    public TimeOnly? StartTime { get; set; }

    /// <summary>Certification number of the host center; null while it is to be announced.</summary>
    public string? BowlingCenterId { get; set; }

    /// <summary>Server errors named <c>Series.Finale.*</c>.</summary>
    public List<string> Errors { get; } = [];
}

internal sealed class SeriesStageRowModel
{
    /// <summary>Stable key for Razor <c>@key</c>; never sent to the API.</summary>
    public Guid RowKey { get; } = Guid.NewGuid();

    /// <summary>Existing Stage ID on edit; null for a new row.</summary>
    public string? StageId { get; init; }

    public string Kind { get; set; } = StageKinds.Qualifying;
    public string? TournamentId { get; set; }

    /// <summary>Qualifying and Survivor rows only; the Placement row has no count (the Finale carries its own). The server enforces the rest.</summary>
    public int? AdvancingCount { get; set; } = 8;
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
        => new() { Name = model.Name.Trim(), SeasonId = model.SeasonId!, Logo = model.Logo, Stages = ToStageInputs(model), Finale = ToFinaleInput(model.Finale) };

    public static EditSeriesInput ToEditInput(SeriesFormModel model)
        => new() { Name = model.Name.Trim(), Logo = model.Logo, Stages = ToStageInputs(model), Finale = ToFinaleInput(model.Finale) };

    private static SeriesFinaleInput? ToFinaleInput(SeriesFinaleFormModel? finale)
        => finale is null
            ? null
            : new SeriesFinaleInput
            {
                FinalistCount = finale.FinalistCount ?? 0, // an empty count reaches the server as 0, which it rejects by name
                Date = finale.Date,
                StartTime = finale.StartTime,
                BowlingCenterId = string.IsNullOrWhiteSpace(finale.BowlingCenterId) ? null : finale.BowlingCenterId,
            };

    private static IReadOnlyList<SeriesStageInput> ToStageInputs(SeriesFormModel model)
        => [.. model.Stages.Select(row => new SeriesStageInput
        {
            Id = row.StageId,
            Kind = row.Kind,
            TournamentId = row.TournamentId ?? string.Empty,
            AdvancingCount = row.Kind == StageKinds.Placement ? null : row.AdvancingCount,
            SquadDate = row.Kind == StageKinds.Survivor ? row.SquadDate : null,
            SquadStartTime = row.Kind == StageKinds.Survivor ? row.SquadStartTime : null,
        })];

    public static SeriesFormModel FromDetail(SeriesDetailViewModel detail)
    {
        var model = new SeriesFormModel
        {
            Name = detail.Name,
            SeasonId = detail.SeasonId,
            Logo = detail.Logo,
            LogoUrl = detail.LogoUrl,
            Finale = detail.Finale is { } finale
                ? new SeriesFinaleFormModel
                {
                    FinalistCount = finale.FinalistCount,
                    Date = finale.Date,
                    StartTime = finale.StartTime,
                    BowlingCenterId = finale.BowlingCenterId,
                }
                : null,
        };
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

    /// <summary>Routes <c>Series.Stages[i]</c> errors onto rows and <c>Series.Finale.*</c> errors onto the Finale section; returns the rest for the banner.</summary>
    public static IReadOnlyList<string> ApplyFieldErrors(SeriesFormModel model, IReadOnlyList<Services.ApiFieldError> errors)
    {
        foreach (var row in model.Stages)
        {
            row.Errors.Clear();
        }

        model.Finale?.Errors.Clear();

        var general = new List<string>();

        foreach (var error in errors)
        {
            if (TryRowIndex(error.Name, out var index) && index < model.Stages.Count)
            {
                model.Stages[index].Errors.Add(error.Reason);
            }
            else if (model.Finale is not null && error.Name.StartsWith("Series.Finale", StringComparison.OrdinalIgnoreCase))
            {
                model.Finale.Errors.Add(error.Reason);
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
                <SeriesLogoTile LogoUrl="@series.LogoUrl" Name="@series.Name" Size="SeriesLogoTile.Large" />
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
                            <SeriesLogoTile LogoUrl="@series.LogoUrl" Name="@series.Name" Size="SeriesLogoTile.Small" />
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
            <div class="series-detail__logo">
                <SeriesLogoTile LogoUrl="@_model.LogoUrl" Name="@_model.Name" Size="SeriesLogoTile.Large" />
                <LogoEditButton Policy="@Permissions.EditSeriesLogo.PolicyName" OwnerName="@_model.Name" OnClick="@(() => _isLogoEditorOpen = true)" />
            </div>
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
            <SeriesFieldChart Steps="@_steps" HasFinale="@_model.HasFinale" HasPlacementStage="@HasPlacementStage" />
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
                        var advancing = SeriesDisplay.Advancing(stage.Kind, stage.AdvancingCount, stage.TeamSize, _model.Finale?.FinalistCount);
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

        @if (_model.Finale is { } finale)
        {
            <section aria-labelledby="series-finale-title">
                <h2 class="series-group-title" id="series-finale-title">Finale <span>@SeriesDisplay.FinaleCaption(finale.FinalistCount)</span></h2>
                <div class="neba-card series-finale">
                    <span class="series-finale__when">@SeriesDisplay.FinaleWhen(finale.Date, finale.StartTime)</span>
                    <span class="series-finale__host">@(finale.Host ?? "Host center to be announced")</span>
                </div>
            </section>
        }
    </div>

    <SeriesLogoEditor IsOpen="@_isLogoEditorOpen"
                      SeriesId="@Id"
                      SeriesName="@_model.Name"
                      CurrentLogoUrl="@_model.LogoUrl"
                      OnClose="@(() => _isLogoEditorOpen = false)"
                      OnSaved="HandleLogoSavedAsync" />

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
    private bool _isLogoEditorOpen;

    private bool HasPlacementStage => _model!.Stages.Any(s => s.Kind == StageKinds.Placement);

    private async Task HandleLogoSavedAsync()
    {
        _isLogoEditorOpen = false;
        await OnParametersSetAsync(); // reload so the hero shows the new logo
    }

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
            _steps = SeriesDisplay.FieldSteps(_model.Stages, _model.Finale);
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

The logo tile sits at the left of the title bar; when there is no logo, `SeriesLogoTile` shows the Series' initials on a navy tile (no sponsor fallback, unlike tournaments).

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
                              FinaleHost="@_detail?.Finale?.Host"
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

#### Tournament list and detail: Series tag and callout (mockups: `tournaments-list-series.html`, `tournament-detail-series.html`)

`Tournaments/TournamentSeriesViewModel.cs` (in `Neba.Website.Server.Tournaments`, used by both the schedule and the detail view models):

```csharp
namespace Neba.Website.Server.Tournaments;

internal sealed record TournamentSeriesViewModel
{
    public required string SeriesId { get; init; }
    public required string Name { get; init; }
    public Uri? LogoUrl { get; init; }
    public required string Kind { get; init; }
    public int? AdvancingCount { get; init; }
    public DateOnly? SquadDate { get; init; }
    public TimeOnly? SquadStartTime { get; init; }

    /// <summary>Placement Stages only, when the Series has a Finale.</summary>
    public int? FinaleCount { get; init; }
    public DateOnly? FinaleDate { get; init; }
    public TimeOnly? FinaleStartTime { get; init; }

    public string Href => "/series/" + SeriesId;
}
```

Mapping: a `ToViewModel()` extension on `TournamentSeriesResponse`, next to `DefendingTeamResponse`'s. `SeasonTournamentViewModel` and `TournamentDetailViewModel` each gain `TournamentSeriesViewModel? Series` and a `bool IsSeriesStage => Series is not null` (the Razor reads the bool, not a null check). The schedule mapping and `TournamentDetailMappingExtensions` copy `response.Series?.ToViewModel()`.

`SeriesDisplay` additions (all text rules stay out of the markup):

```csharp
/// <summary>"Qualifying Round", "Survivor Round", "Placement Round".</summary>
public static string RoundLabel(string kind)
    => kind + " Round";

/// <summary>What the round decides, for the detail callout. Secondary is the "(6 bowlers)" or squad line.</summary>
public static AdvancingLabel CalloutRule(
    string kind, int? count, int teamSize, DateOnly? squadDate, TimeOnly? squadStartTime,
    int? finaleCount, DateOnly? finaleDate, TimeOnly? finaleStartTime)
    => (kind, count) switch
    {
        (StageKinds.Placement, _) when finaleCount is null => new AdvancingLabel("Final standings (no Finale).", null),
        (StageKinds.Placement, _) => new AdvancingLabel(
            "Final standings. The top " + finaleCount!.Value.ToString(Culture) + " go to the Finale.",
            finaleDate is { } fd && finaleStartTime is { } ft ? "Finale: " + FinaleWhen(fd, ft) + "." : null),
        (StageKinds.Qualifying, { } n) when teamSize > 1 => new AdvancingLabel(
            "Top " + "team".ToQuantity(n) + " enter the Series field.",
            "(" + "bowler".ToQuantity(n * teamSize) + ")"),
        (StageKinds.Qualifying, { } n) => new AdvancingLabel("Top " + n.ToString(Culture) + " finishers enter the Series field.", null),
        (StageKinds.Survivor, { } n) => new AdvancingLabel(
            n.ToString(Culture) + " bowlers advance.",
            squadDate is { } d && squadStartTime is { } t
                ? "Series squad: " + t.ToString("h:mm tt", Culture) + " on " + d.ToString("M/d", Culture) + "."
                : null),
        _ => new AdvancingLabel("Part of the Series.", null), // a saved Qualifying/Survivor Stage always has a count
    };
```

`Series/SeriesLogoTile.razor` gains two sizes beside `Large` and `Small`: `Tag` (22px, round, no border) for the list pill, and `Card` (96px, the tournament card's logo size) for the Finale card. The tile keeps the initials fallback, so a Series with no logo still shows a tag.

`Tournaments/TournamentSeriesTag.razor(.css)` (list pill, style A): an `<a class="tournament-series-tag" href="@Series.Href">` holding the `SeriesLogoTile` (`Tag` size), the Series name, a separator, and the round label in a `kind-chip` colored by `SeriesDisplay.KindClass`. It takes `[Parameter, EditorRequired] TournamentSeriesViewModel Series`, has `title="View the Series"`, and renders in the eyebrow row of both cards after the date and status pill. `TournamentUpcomingCard.razor` and `TournamentPastCard.razor` add:

```razor
@if (Tournament.IsSeriesStage)
{
    <span aria-hidden="true">·</span>
    <TournamentSeriesTag Series="@Tournament.Series!" />
}
```

On a phone the eyebrow row already wraps, so the pill drops to its own line.

`Tournaments/Detail/TournamentSeriesCallout.razor(.css)`: an `<aside class="neba-card tournament-series-callout tournament-series-callout--@kind">` under the hero, before the registration and pattern sections. Content: `SeriesLogoTile` (`Large`), the line "This tournament is part of a Series", the Series name as an `<h2>`, the round label, the `CalloutRule` primary and secondary lines, and a "View Series →" secondary button to `Series.Href`. The left border takes the kind color (`--kind-qualifying`, `--kind-survivor`, `--kind-placement`). It shows for cancelled tournaments too (see Decisions). Parameters: `Series` and `TeamSize`. `TournamentDetail.razor` renders it when `_model.IsSeriesStage`.

`Tournaments/Detail/TournamentDetail.razor` and the schedule `.razor.css` files add the `.tournament-series-tag` and `.tournament-series-callout` rules, using the same theme tokens as the mockup CSS (kind colors already in `wwwroot/app.css` from the Series pages).

#### Finale on the tournament list (mockup: `tournaments-list-series.html`)

A Finale isn't a tournament, but bowlers look for it where they look for tournaments. It shows as its own card in the same timeline, sorted by date. It reads the new `GET /series/finales` call through `ISeriesApi`, so the tournament API and its caches don't change.

`Tournaments/Schedule/SeriesFinaleItemViewModel.cs`:

```csharp
namespace Neba.Website.Server.Tournaments.Schedule;

internal sealed record SeriesFinaleItemViewModel
{
    public required string SeriesId { get; init; }
    public required string SeriesName { get; init; }
    public required string SeasonId { get; init; }
    public Uri? LogoUrl { get; init; }
    public required int FinalistCount { get; init; }
    public required DateOnly Date { get; init; }
    public required TimeOnly StartTime { get; init; }
    public string? Host { get; init; }

    /// <summary>"{Series name} Finale".</summary>
    public string Title => Series.SeriesDisplay.FinaleName(SeriesName);

    public string Href => "/series/" + SeriesId;
}
```

A `ToFinaleItem()` extension on `SeriesFinaleSummaryResponse` builds it (host through `SeriesDisplay.Host`).

`Tournaments/Schedule/ScheduleEntry.cs`, `UpcomingSchedule.cs` (additions): a `ScheduleEntry` is one tournament or one Finale, with a `SortDate` (a tournament's start date, a Finale's date) and `SortTime`. `UpcomingSchedule.Merge(tournaments, finales, newestFirst)` returns the entries in date order (ascending for the Upcoming tab, newest first for the Past tab, the same direction each tab uses today). `GroupBySeason` groups the merged entries by season, so a Finale sits under the season of its Series.

`Tournaments/Schedule/Tournaments.razor`:

- `LoadTournamentsAndHeroAsync` also calls `ISeriesApi.ListSeriesFinalesAsync` through `ApiExecutor`. A failure leaves the Finales out and logs through the executor; the tournament list still renders (no page notice for a missing Finale).
- A Finale is upcoming when its date is today or later (the page's own `DateTime.Today`, the same rule as a tournament's end date) and past otherwise. The Past tab shows the Finales of the selected season.
- The search box also matches the Finale's title, Series name and host. The format filter (Singles, Doubles, Trios) hides Finales, since a Finale has no format; "All" shows them.
- The hero (next tournament to register for), the registration status logic and the timeline dots ignore Finales.
- The page renders each entry through the matching card.

`Tournaments/Schedule/SeriesFinaleCard.razor(.css)` (one component, `[Parameter, EditorRequired] SeriesFinaleItemViewModel Finale`, `[Parameter] bool IsPast`): the same grid and tokens as `TournamentUpcomingCard`.

- **Tile:** `SeriesLogoTile` (`Card` size) with the Series logo, or the Series initials when it has none. The Finale card never uses a tournament's default logo, and it has no edit-logo button (the logo is edited on the Series page).
- **Eyebrow:** the date, a separator, the start time (`SeriesDisplay.FinaleWhen`), then a `kind-chip kind-finale` "Finale" chip. `--kind-finale` (the accent red) is new in `wwwroot/app.css`.
- **Title:** an `<h3>` with the Finale title, linking to `/series/{id}`.
- **Host line:** the same location-pin line as tournament cards. Without a host, it reads "Host center to be announced".
- **Chips:** "N finalists" (`"finalist".ToQuantity(n)`).
- **Right column:** no price, capacity or registration. A "View Series →" link to the Series page, in both tabs.
- Past Finales use the muted past styling. Results and the champion come in phase 3.
- `role="listitem"` like the other cards; the card carries `aria-label` "{title}, {date and time}".

### Components

#### `Series/SeriesEditor.razor`

Shared by create and edit. It owns the `EditForm`, dirty tracking, the save call, and the error routing.

```razor
@using Microsoft.AspNetCore.Components.Forms
@using Neba.Api.Contracts.BowlingCenters
@using Neba.Api.Contracts.BowlingCenters.ListBowlingCenters
@using Neba.Api.Contracts.Series
@using Neba.Api.Contracts.Series.CreateSeries
@using Neba.Api.Contracts.Series.EditSeries
@using Neba.Api.Contracts.Uploads
@using Neba.Website.Server.Services
@implements IDisposable

@inject ApiExecutor ApiExecutor
@inject ISeriesApi SeriesApi
@inject IBowlingCentersApi BowlingCentersApi
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
                <NebaAlert Severity="NotifySeverity.Warning" Title="This Series is locked" Message="Processing has begun for this Series. Its Stages and whether it has a Finale are fixed. The name, the logo, and the Finale's date, time and center can still change." />
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

            <section class="series-editor__logo">
                <span class="series-editor__label">Logo</span>
                @if (Model.LogoUrl is not null)
                {
                    <div class="series-editor__logo-current">
                        <SeriesLogoTile LogoUrl="@Model.LogoUrl" Name="@Model.Name" Size="SeriesLogoTile.Large" />
                        <button type="button" class="neba-btn neba-btn-secondary" @onclick="RemoveLogo">Remove logo</button>
                    </div>
                }
                else
                {
                    <FileUpload MaxFiles="1" Accept="image/*" MaxFileSizeBytes="@(5 * 1024 * 1024)" Label="Upload a logo"
                                OnUploadRequestedAsync="UploadLogoAsync"
                                OnFileUploaded="HandleLogoUploaded"
                                OnFileRemoved="@(_ => RemoveLogo())"
                                OnBusyChanged="@(busy => _isLogoUploading = busy)" />
                }
                <p class="series-editor__hint">Shown on the Series list and detail pages. It can change even after processing begins.</p>
            </section>

            <SeriesStageGrid Rows="@Model.Stages"
                             Options="@_options"
                             HeldElsewhere="@_heldElsewhere"
                             ReadOnly="@IsLocked"
                             OnChanged="MarkDirty" />

            <SeriesFinaleSection Model="@Model"
                                 BowlingCenters="@_bowlingCenters"
                                 CurrentHost="@FinaleHost"
                                 IsLocked="@IsLocked"
                                 OnChanged="MarkDirty" />

            <div class="series-editor__actions">
                <button type="button" class="neba-btn neba-btn-secondary" @onclick="HandleCancel">Cancel</button>
                <button type="submit" class="neba-btn neba-btn-primary" disabled="@(_isSubmitting || _isLogoUploading)">@SaveLabel</button>
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

    /// <summary>Edit only: the Finale's saved host text, for a center that has left the Open list.</summary>
    [Parameter]
    public string? FinaleHost { get; set; }

    private EditContext _editContext = default!;
    private bool _isDirty;
    private bool _isSubmitting;
    private IReadOnlyList<SeriesTournamentOption> _options = [];
    private IReadOnlyCollection<BowlingCenterSummaryResponse> _bowlingCenters = [];
    private int _heldElsewhere;
    private List<string> _generalErrors = [];
    private string? _pendingSeasonId;
    private bool _isLogoUploading;

    private Task<ErrorOr.ErrorOr<UploadedFileResponse>> UploadLogoAsync(
        Stream stream, string fileName, string contentType, IProgress<int> progress, CancellationToken ct)
        => ApiExecutor.ExecuteAsync("SeriesApi", "UploadSeriesLogo",
            c => SeriesApi.UploadSeriesLogoAsync(new Refit.StreamPart(stream, fileName, contentType), c), ct);

    private void HandleLogoUploaded(UploadedFileResponse response)
    {
        Model.Logo = new SeriesLogoInput
        {
            Container = response.Container,
            Path = response.Path,
            ContentType = response.ContentType,
            SizeInBytes = response.SizeInBytes,
        };
        Model.LogoUrl = response.Url;
        MarkDirty();
    }

    private void RemoveLogo()
    {
        Model.Logo = null;
        Model.LogoUrl = null;
        MarkDirty();
    }

    private string SaveLabel => IsCreate ? "Create Series" : "Save changes";

    protected override async Task OnInitializedAsync()
    {
        _editContext = new EditContext(Model);
        _editContext.OnFieldChanged += HandleFieldChanged;
        await LoadOptionsAsync();
        await LoadBowlingCentersAsync();
    }

    public void Dispose() => _editContext.OnFieldChanged -= HandleFieldChanged;

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs e) => MarkDirty();

    private void MarkDirty() => _isDirty = true;

    // The Finale's host picker, loaded the way EditTournament loads its picker.
    private async Task LoadBowlingCentersAsync()
    {
        var result = await ApiExecutor.ExecuteAsync("BowlingCenters", "ListBowlingCenters", BowlingCentersApi.ListBowlingCentersAsync);
        if (result.IsError)
        {
            _generalErrors = [result.FirstError.Description];
            return;
        }

        _bowlingCenters = result.Value.Items;
    }

    private async Task LoadOptionsAsync()
    {
        // Loaded even when locked: the read-only grid uses the options for tournament labels and team sizes.
        if (string.IsNullOrEmpty(Model.SeasonId))
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
                        @if (row.Kind == StageKinds.Placement)
                        {
                            <span class="series-grid__cell-label series-grid__cell-label--always">Advancing</span>
                            <span class="series-grid__no-count">No count</span>
                            <div class="series-editor__hint">Ranks by tournament position. Set the Finale below.</div>
                        }
                        else
                        {
                            <label class="series-grid__cell-label series-grid__cell-label--always" for="@FieldId("count", i)">Advancing</label>
                            <input id="@FieldId("count", i)" type="number" min="1" class="neba-input" value="@row.AdvancingCount" @onchange="@(e => HandleCountChanged(row, e))" />
                            @if (SeriesDisplay.TeamHint(row.Kind, row.AdvancingCount, option?.TeamSize ?? 1) is { } hint)
                            {
                                <div class="series-editor__hint">@hint.Primary<br />@hint.Secondary</div>
                            }
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
        => row.Kind == StageKinds.Placement
            ? "Final standings"
            : SeriesDisplay.Advancing(row.Kind, row.AdvancingCount, option?.TeamSize ?? 1).Primary;

    private Task HandleKindChanged(SeriesStageRowModel row, ChangeEventArgs e)
    {
        row.Kind = e.Value?.ToString() ?? StageKinds.Qualifying;

        // A Placement Stage has no count; switching away leaves the count empty for the admin to fill in.
        if (row.Kind == StageKinds.Placement)
        {
            row.AdvancingCount = null;
        }

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

    /// <summary>Empty input means no count; the server rejects an empty Qualifying or Survivor count.</summary>
    private Task HandleCountChanged(SeriesStageRowModel row, ChangeEventArgs e)
    {
        row.AdvancingCount = int.TryParse(e.Value?.ToString(), out var count) ? count : null;
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

#### `Series/SeriesFinaleSection.razor`

The Finale part of the editor: a toggle, then four fields. It sits under the Stage grid because the Finale follows the Placement Round. Mockup: `series-editor.html`.

```razor
@using Neba.Api.Contracts.BowlingCenters.ListBowlingCenters

<section class="series-finale-editor" aria-labelledby="series-finale-heading">
    <span class="series-editor__label" id="series-finale-heading">Finale</span>

    <label class="series-finale-editor__toggle">
        <input type="checkbox" checked="@HasFinale" disabled="@IsLocked" @onchange="HandleToggleAsync" />
        This Series has a Finale
    </label>

    <p class="series-editor__hint">The Finale is its own event, held after the Placement Round. It shows in the tournament list with the Series logo.</p>

    @if (IsLocked)
    {
        <p class="series-editor__hint">Whether a Series has a Finale, and how many finalists go, is fixed once processing begins. The date, start time and center can still change.</p>
    }

    @if (Model.Finale is { } finale)
    {
        <div class="series-finale-editor__fields">
            <div>
                <FormLabel TargetId="finale-count" For="@(() => finale.FinalistCount)">Finalists</FormLabel>
                <input id="finale-count" type="number" min="1" class="neba-input" value="@finale.FinalistCount" disabled="@IsLocked"
                       @onchange="@(e => HandleCountChangedAsync(finale, e))" />
                <p class="series-editor__hint">Top finishers of the Placement Round. No more than the last Survivor Round's count.</p>
            </div>

            <div>
                <FormLabel TargetId="finale-date" For="@(() => finale.Date)">Date</FormLabel>
                <NebaDateInput id="finale-date" Value="@finale.Date" ValueChanged="@((DateOnly? d) => HandleDateChangedAsync(finale, d))" ValueExpression="@(() => finale.Date)" />
                <p class="series-editor__hint">After every Stage's tournament ends.</p>
            </div>

            <div>
                <FormLabel TargetId="finale-time" For="@(() => finale.StartTime)">Start time</FormLabel>
                <input id="finale-time" type="time" class="neba-input" value="@TimeValue(finale)" @onchange="@(e => HandleTimeChangedAsync(finale, e))" />
            </div>

            <div>
                <label class="series-editor__label" for="finale-center">Host center</label>
                <NebaAutocomplete Id="finale-center" TValue="string" TItem="BowlingCenterSummaryResponse"
                                  Value="@finale.BowlingCenterId"
                                  ValueChanged="@(id => HandleCenterChangedAsync(finale, id))"
                                  Items="@BowlingCenters"
                                  DisplayText="@(center => center.Name + " — " + center.City + ", " + center.State)"
                                  ItemValue="@(center => center.CertificationNumber)"
                                  Placeholder="Search bowling centers..."
                                  EmptyLabel="To be announced" />
                @if (CurrentHostNotListed)
                {
                    <p class="series-editor__hint">Current host (no longer listed): @CurrentHost</p>
                }
            </div>
        </div>

        @foreach (var error in finale.Errors)
        {
            <p class="series-editor__row-error" role="alert">@error</p>
        }
    }
</section>

@code {
    [Parameter, EditorRequired]
    public SeriesFormModel Model { get; set; } = default!;

    [Parameter, EditorRequired]
    public IReadOnlyCollection<BowlingCenterSummaryResponse> BowlingCenters { get; set; } = [];

    [Parameter]
    public bool IsLocked { get; set; }

    /// <summary>The saved host's name, city and state (edit page), shown when that center is no longer in the Open list.</summary>
    [Parameter]
    public string? CurrentHost { get; set; }

    [Parameter]
    public EventCallback OnChanged { get; set; }

    private bool HasFinale => Model.Finale is not null;

    private bool CurrentHostNotListed
        => CurrentHost is not null
            && Model.Finale?.BowlingCenterId is { } id
            && !BowlingCenters.Any(center => center.CertificationNumber == id);

    private static string TimeValue(SeriesFinaleFormModel finale)
        => finale.StartTime?.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;

    // Turning it off drops what was typed; turning it on starts empty. The toggle is disabled while locked.
    private Task HandleToggleAsync(ChangeEventArgs e)
    {
        Model.Finale = e.Value is true ? new SeriesFinaleFormModel() : null;
        return OnChanged.InvokeAsync();
    }

    // Empty input leaves the count empty; the server rejects it by name (Series.Finale.FinalistCount).
    private Task HandleCountChangedAsync(SeriesFinaleFormModel finale, ChangeEventArgs e)
    {
        finale.FinalistCount = int.TryParse(e.Value?.ToString(), out var count) ? count : null;
        return OnChanged.InvokeAsync();
    }

    private Task HandleDateChangedAsync(SeriesFinaleFormModel finale, DateOnly? date)
    {
        finale.Date = date;
        return OnChanged.InvokeAsync();
    }

    private Task HandleTimeChangedAsync(SeriesFinaleFormModel finale, ChangeEventArgs e)
    {
        finale.StartTime = TimeOnly.TryParse(e.Value?.ToString(), System.Globalization.CultureInfo.InvariantCulture, out var time) ? time : null;
        return OnChanged.InvokeAsync();
    }

    private Task HandleCenterChangedAsync(SeriesFinaleFormModel finale, string? certificationNumber)
    {
        finale.BowlingCenterId = string.IsNullOrWhiteSpace(certificationNumber) ? null : certificationNumber;
        return OnChanged.InvokeAsync();
    }
}
```

`SeriesFinaleFormModel` properties carry `[Required]` (finalists, date, start time) only so `FormLabel` shows "(required)"; `DataAnnotationsValidator` doesn't reach nested models, so the server's named errors (`Series.Finale.*`) do the validating and land in the section through `SeriesInputMapping.ApplyFieldErrors`. The editor's Finale center list is the same `ListBowlingCenters` call as `EditTournament`; a failed load shows the banner and leaves the picker empty. `ListBowlingCenters` returns only Open centers. If the Finale's saved center has since closed, it isn't in the list, so the section shows its saved name from the Series detail as "Current host (no longer listed): {name}" under the picker (the editor takes a `FinaleHost` parameter, filled from `SeriesFinaleViewModel.Host` on the edit page); picking another center or "To be announced" replaces it.

#### `Series/SeriesLogoTile.razor`

```razor
<span class="@CssClass" aria-hidden="@(LogoUrl is null ? "true" : null)">
    @if (LogoUrl is not null)
    {
        <img src="@LogoUrl" alt="@AltText" loading="lazy" />
    }
    else
    {
        <span class="series-logo-tile__initials">@Initials</span>
    }
</span>

@code {
    public const string Large = "large";
    public const string Small = "small";

    [Parameter]
    public Uri? LogoUrl { get; set; }

    [Parameter, EditorRequired]
    public string Name { get; set; } = string.Empty;

    [Parameter]
    public string Size { get; set; } = Large;

    private string CssClass => "series-logo-tile series-logo-tile--" + Size;

    private string AltText => Name + " logo";

    /// <summary>"Vincent Hall Memorial Series" -> "VH": the first two capitalized words that aren't a year.</summary>
    private string Initials => SeriesDisplay.Initials(Name);
}
```

`SeriesDisplay.Initials(string name)` takes the first letters of the first two words that start with a letter (skipping a leading year), upper-cased. CSS: a square tile (`72px` large, `36px` small), `object-fit: contain` on a white background for an uploaded logo; navy (`--neba-blue-brand`) with white Manrope initials when there is none.

#### `Series/SeriesLogoEditor.razor`

The Series host for the shared logo modal, the same shape as `TournamentLogoEditor`.

```razor
@using ErrorOr
@using Neba.Api.Contracts.Series
@using Neba.Api.Contracts.Series.CreateSeries
@using Neba.Api.Contracts.Series.EditSeriesLogo
@using Neba.Api.Contracts.Uploads
@using Neba.Website.Server.Services
@using Refit

@inject ApiExecutor ApiExecutor
@inject ISeriesApi SeriesApi
@inject ToastService ToastService

<EditLogoModal IsOpen="@IsOpen"
               OwnerName="@SeriesName"
               CurrentLogoUrl="@CurrentLogoUrl"
               OnClose="OnClose"
               OnSaved="HandleSavedAsync"
               OnUploadRequestedAsync="UploadLogoAsync"
               OnSubmitRequestedAsync="SaveLogoAsync" />

@code {
    [Parameter, EditorRequired]
    public bool IsOpen { get; set; }

    [Parameter, EditorRequired]
    public string SeriesId { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string SeriesName { get; set; } = string.Empty;

    [Parameter]
    public Uri? CurrentLogoUrl { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnClose { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnSaved { get; set; }

    private Task<ErrorOr<UploadedFileResponse>> UploadLogoAsync(
        Stream stream, string fileName, string contentType, IProgress<int> progress, CancellationToken ct)
        => ApiExecutor.ExecuteAsync("SeriesApi", "UploadSeriesLogo",
            c => SeriesApi.UploadSeriesLogoAsync(new StreamPart(stream, fileName, contentType), c), ct);

    private Task<ErrorOr<Success>> SaveLogoAsync(UploadedFileResponse? staged, CancellationToken ct)
    {
        var request = new EditSeriesLogoRequest
        {
            Id = SeriesId,
            Input = new EditSeriesLogoInput
            {
                Logo = staged is null
                    ? null
                    : new SeriesLogoInput { Container = staged.Container, Path = staged.Path, ContentType = staged.ContentType, SizeInBytes = staged.SizeInBytes },
            },
        };

        return ApiExecutor.ExecuteAsync("SeriesApi", "EditSeriesLogo", c => SeriesApi.EditSeriesLogoAsync(SeriesId, request, c), ct);
    }

    private async Task HandleSavedAsync()
    {
        ToastService.Show("Logo Saved", "The logo for \"" + SeriesName + "\" was saved.", NotifySeverity.Success);
        await OnSaved.InvokeAsync();
    }
}
```

#### Shared logo modal and edit button (generalized from Tournaments)

- [ ] **Move** `Tournaments/EditTournamentLogoModal.razor(.css)` to `Components/EditLogoModal.razor(.css)` with neutral names:
  - `TournamentName` → `OwnerName` (subtitle under "Edit logo").
  - `OnSubmitRequestedAsync` becomes `Func<UploadedFileResponse?, CancellationToken, Task<ErrorOr<Success>>>`. The modal hands back the staged upload (null removes the logo), and each host maps it to its own contract type. That removes the modal's only `Contracts.Tournaments` dependency, and the tournament JSON contract doesn't change.
  - CSS class prefix `elm-` stays (it already reads as "edit logo modal").
- [ ] **Move** `Tournaments/TournamentLogoEditButton.razor` to `Components/LogoEditButton.razor`, with a `Policy` parameter (`[Parameter, EditorRequired] public string Policy`) and `TournamentName` → `OwnerName`. The `tournament-logo-edit` classes become `logo-edit-button` in `app.css`.
- [ ] **Update** `Tournaments/TournamentLogoEditor.razor` to host `EditLogoModal` and map `UploadedFileResponse?` → `TournamentLogoInput?` in `SaveLogoAsync`. Update `Tournaments/Schedule/Tournaments.razor` and `Tournaments/Detail/TournamentDetail.razor` to use `LogoEditButton Policy="@Permissions.EditTournamentLogo.PolicyName"`.
- [ ] **Tests:** `EditTournamentLogoModalTests` moves to `tests/Neba.Website.Tests/Components/EditLogoModalTests.cs` (assertions unchanged apart from names, plus "submit passes the staged upload; remove passes null"). `TournamentLogoEditorTests` gains the mapping assertion. New `LogoEditButtonTests` (hidden without the policy, shown with it).

#### `Series/SeriesFieldChart.razor`

```razor
<section class="neba-card series-field" aria-labelledby="series-field-title">
    <h2 id="series-field-title">How the field narrows</h2>
    <p>@Intro</p>
    <div class="series-field__rows">
        @foreach (var step in Steps)
        {
            <div class="series-field__row">
                <div class="series-field__label">@step.Label<small>@step.Detail</small></div>
                <div class="@BarClass(step)" style="@SeriesDisplay.BarWidthStyle(step.Count, Max)">@BarText(step)</div>
            </div>
        }
    </div>
    @if (HasPlacementStage && !HasFinale)
    {
        <p class="series-field__caption">No Finale: the Placement Round sets the final standings.</p>
    }
</section>

@code {
    [Parameter, EditorRequired]
    public IReadOnlyList<FieldStep> Steps { get; set; } = default!;

    [Parameter, EditorRequired]
    public bool HasFinale { get; set; }

    [Parameter, EditorRequired]
    public bool HasPlacementStage { get; set; }

    private string Intro => HasFinale
        ? "Bowlers qualify at the early events, are cut by squad score, and the last round decides the Finale."
        : "Bowlers qualify at the early events, are cut by squad score, and the last round decides the final standings.";

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

Screenshots for `docs/help/create-series.md`, `edit-series.md`, `delete-series.md` and `edit-series-logo.md` through the `help-documentation` skill, once the pages run.

### Tests

#### bUnit (`tests/Neba.Website.Tests/Series/`)

All tests carry `[UnitTest]`, `[Component("Series")]`, a `DisplayName`, and AAA comments. API interfaces are mocked with `MockBehavior.Strict`, and `ApiExecutor` gets `NullLogger`.

- `SeriesDisplayTests`:
  - `Advancing`: Qualifying singles, Qualifying team (secondary "(6 bowlers)"), Survivor, Placement with a finale count passed in ("Top 4 to the Finale"), Placement without one ("Final standings" + "(no Finale)").
  - `TeamHint` with an empty count; `Initials`; `FinaleWhen` ("Sat 9/27 at 2:00 PM"); `Host` with a name only, with city and state, and null; `FinaleName`; `FinaleCaption`.
  - `Squad` formatting, `TournamentDates` for one-day and multi-day tournaments, `SeasonRange`.
  - `QualifierCeiling` = 62 for the 2025 config.
  - `FieldSteps`: full (62 / 40 / 20 / 4, first step `IsCeiling`), only Qualifying, no Placement, a null Finale (no Finale step); the Finale step reads its date and time.
  - `BarWidthStyle` floor of 8%.
  - `RowsNeedChanges` singular and plural.
- `SeriesMappingExtensionsTests`: summary with and without `NextStage` and logo; detail sorted by order, `Finale` mapped with its host text (and null host when no center), `HasFinale`, and `Logo` built only when container and path are present.
- `SeriesLogoEditorTests`: upload calls `UploadSeriesLogoAsync`; save maps the staged upload to `SeriesLogoInput` and null to removal; success toasts and raises `OnSaved`.
- `SeriesLogoTileTests`: image with alt text, or initials.
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
  - Field chart steps; without a Finale, no Finale bar, the "final standings" intro and the "No Finale" caption; the Placement row reads "Final standings" / "(no Finale)"; with a Finale, the Placement row reads "Top 4 to the Finale" and the Finale block shows the date, time and host ("Host center to be announced" with none).
  - Logo tile shows the logo or the initials; `LogoEditButton` only with `Series.EditSeriesLogo`; a saved logo reloads the page.
  - Edit and Delete by permission; Delete disabled with the note when locked.
  - Confirm → `DeleteSeriesAsync` → toast → navigate. A 409 shows the error toast.
  - Not-found message.
- `SeriesStageGridTests`:
  - Add, remove, move up and down; first and last buttons disabled.
  - Switching to Survivor sets the squad date to the tournament start; switching away clears it.
  - Picking a tournament removes it from other rows.
  - Team hint updates with the count.
  - A Placement row shows "No count" and no input; switching a row to Placement clears its count; a cleared Qualifying count becomes null (the server rejects it).
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
  - Logo: upload sets `Model.Logo` and the preview; remove clears both; save is disabled while uploading; the locked editor still allows the logo change; create and edit requests carry the logo.
- `SeriesTournamentOptionsLoaderTests`: excludes tournaments held by other Series in the season (not the one being edited) and cancelled tournaments; sorts by date; reports `HeldElsewhere`.
- `ApiExecutorTests` (existing): `errors[].name` lands in `GetFieldErrors()`; the flattened description is unchanged; a body without names yields an empty list.
- `NavMenuTests` (existing, if present): the Series link is in the Tournaments dropdown.
- `TournamentSeriesTagTests`: the link goes to `/series/{id}`; the round label reads "Qualifying Round", "Survivor Round" and "Placement Round" with the matching kind class; the logo shows with alt text, or initials when there is none.
- `TournamentSeriesCalloutTests`: the Series name, round label and "View Series" link; `CalloutRule` text for Qualifying singles, Qualifying team ("(6 bowlers)" on the second line), Survivor (squad line), Placement with a Finale (second line "Finale: Sat 9/27 at 2:00 PM.") and Placement without one; initials with no logo.
- `SeriesFinaleSectionTests`: the toggle shows and hides the fields and sets and clears `Model.Finale`; each field writes the model and calls `OnChanged`; an empty count stays null; with no host, the picker shows "To be announced"; locked: the toggle and the count are disabled while date, time and host stay editable; `Finale.Errors` render under the section.
- `SeriesFinaleCardTests`: the title is "{Series name} Finale" and links to `/series/{id}`; the Series logo shows, or the initials when there is none (never a tournament default logo); date and time in the eyebrow; "Host center to be announced" with no host; "N finalists" singular and plural; no price, registration or edit-logo button; the past styling when `IsPast`.
- `ScheduleEntry` / `UpcomingScheduleTests` additions: `Merge` orders tournaments and Finales together by date (ascending for Upcoming, newest first for Past); a Finale sits under its Series' season; a Finale today is upcoming and yesterday's is past.
- `TournamentsTests` (existing page tests): a failed `ListSeriesFinalesAsync` still renders the tournaments; the format filter hides Finales and "All" shows them; the search box matches the Finale title, Series name and host; the hero ignores Finales.
- `SeriesInputMappingTests` additions: the Finale maps to `SeriesFinaleInput` (a null Finale stays null, an empty count goes out as 0, an empty host as null); `FromDetail` fills the Finale; `Series.Finale.*` errors land in `Model.Finale.Errors`; a Placement row's count is never sent.
- Existing card and detail tests (`TournamentUpcomingCardTests`, `TournamentPastCardTests`, `TournamentDetailTests`): the tag and callout render for a Stage tournament and are absent otherwise; they render for a cancelled tournament.
- `SeriesDisplayTests` additions: `RoundLabel` and each `CalloutRule` case above, including a Survivor Stage with no squad (no secondary line).

#### View-model factories (`tests/Neba.TestFactory/Series/`)

`TournamentSeriesViewModelFactory` (in `tests/Neba.TestFactory/Tournaments/`, next to `DefendingTeamViewModelFactory`), with `Create()` and `Bogus(count, seed?)`, and a `series` parameter (default null) on the existing schedule and detail view-model factories.

`SeriesSummaryViewModelFactory`, `SeriesNextStageViewModelFactory`, `SeriesDetailViewModelFactory`, `SeriesStageViewModelFactory`, `SeriesTournamentOptionFactory`, `SeasonOptionFactory`. Each has `Create()` with const defaults and `Bogus(count, seed?)`. `SeriesDetailViewModelFactory.Reference2025(bool withFinale = true)` returns the 11-Stage configuration, with or without a Finale. `SeriesFinaleViewModelFactory` and `SeriesFinaleItemViewModelFactory` (the tournament-list card) follow the same `Create()` and `Bogus()` pattern.

#### Playwright (`tests/e2e/Series.spec.ts`)

- Public: `/series` shows the featured current Series with its next Stage → open it → field chart reads "Up to 62", "40", "20", "4" → a tournament link goes to `/tournaments/{id}`.
- Nav: the Tournaments dropdown has Series.
- Admin: create a Series with Qualifying, Survivor (squad date and time) and Placement rows → lands on the detail page.
- Admin: the mock returns 422 with `errors[{ name: "Series.Stages[1]", reason: "..." }]` → the message shows under row 2 (a 4xx, per the testing learning).
- Admin: delete with confirm → back on `/series`.
- Public: a Series without a Finale shows "Final standings" on the Placement row and the "No Finale" caption; one with a Finale shows the Finale block with its date, time and host.
- Public: `/tournaments` shows the Series' Finale as its own card in date order (Series logo, "{Series name} Finale", host or "Host center to be announced", "4 finalists"); its title goes to `/series/{id}`; choosing a format filter hides it.
- Admin: in the editor, turn the Finale toggle on, leave the count empty and save → the mock returns 422 naming `Series.Finale.FinalistCount` and the message shows in the Finale section.
- Admin: edit the logo from the detail page (upload → save) and the hero shows it.
- Phone viewport: detail rows stack, and the editor grid collapses.
- Public: `/tournaments` shows the Series tag on a Stage tournament's card (upcoming and past tabs), the tag links to `/series/{id}`, and a non-Stage card has none.
- Public: `/tournaments/{id}` for a Stage tournament shows the callout with the round kind and "View Series"; one for a non-Stage tournament shows none.

#### E2E mock API (`tests/e2e/mock-api/mock-api-server.ts`)

`GET /series` (one current Series with `nextStage` and `logoUrl`, one past), `GET /series/{id}` (the 2025 reference with all required fields, including a `finale` object with a host center; a second id returns the no-Finale variant with no `finale`), `POST /series` (201, or 422 when the name is "fail"), `PUT /series/{id}` (204), `DELETE /series/{id}` (204), `POST /series/logo` (200 with an `UploadedFileResponse`), `PUT /series/{id}/logo` (204), and `GET /series/finales` (one upcoming Finale with a host center, one with none, one past). The mock's tournament list and `GET /tournaments/{id}` responses include a `series` object (with and without a `logoUrl`) for one tournament; the field is optional, so the other mock tournaments don't change.

### Deferred or out of scope (this phase)

- Field, standings and payouts on the public pages (phases 2 and 3 of #257).
- Moving the admin links out of the account menu (separate concern).
- Client-side pre-validation of the Series rules. The server is the single source, and its row-named errors drive the UI.
- The Series tag on other tournament views (Defending Champions, team eligibility, the home page's upcoming strip). Add it when someone asks.
- Showing a rescheduled round's history on a cancelled tournament. The tag only says it was a Stage.
- The Finale in the Series list's "next Stage" block and on the tournament timeline dots. The list page's next-Stage block still stops at the last Stage; add the Finale there if bowlers ask.
- Finale results, finalist positions and payouts (phase 3). The Finale card and block show the schedule only.
- Cancelling or postponing a Finale as its own state. An admin changes its date, time and host center; a Finale that won't be held is removed (refused once the Series is locked, so it needs the phase 2 admin path).
- A guard for a bowling center that hosts a Finale. Centers are never deleted (only their status changes), so the FK needs none. A Finale keeps showing a host that later closes; only choosing a new host is limited to Open centers.

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
   - `src/Neba.Api.Contracts/Security/Permission.cs` (Create, Edit, Delete, EditSeriesLogo, `SeriesLogoUploadPermissions`), `src/Neba.Api.Contracts/Security/PolicyExtensions.cs` (`CanUploadSeriesLogo`), `src/Neba.Api/Security/Infrastructure/SecurityRoleSeeder.cs`, `docs/policies/README.md`, seeder, permission and policy tests
   - Lands the authorization model first, so every endpoint commit can gate on it. Manager gets all four; payouts stay out of them.

3. **Add Series aggregate and Stage rules**
   - `src/Neba.Api/Features/Series/Domain/*` (`Series` with `Logo`, `Finale` and `HasFinale`, `SeriesStage`, `SeriesFinale`, `SeriesFinaleDefinition`, `SquadDesignation`, `StageKind`, `StageStatus`, ids, `StageTournamentFacts`, `SeriesStageDefinition`, `SeriesErrors`)
   - `tests/Neba.TestFactory/Series/` domain factories (`SeriesFactory`, `SeriesStageDefinitionFactory`, `StageTournamentFactsFactory`, `SquadDesignationFactory`, `SeriesFinaleDefinitionFactory`, `Series2025ReferenceFactory` with and without a Finale)
   - `tests/Neba.Api.Tests/Features/Series/Domain/*`
   - The rules from #258 are the core of the feature, including the optional Finale and its count rule; proving each in isolation before persistence keeps later failures easy to place.

4. **Persist Series and Stages**
   - `Database/Configurations/SeriesConfiguration.cs` (logo columns), `SeriesStageConfiguration.cs`, `Database/AppDbContext.cs`, the `AddSeries` migration, `Auditing/AuditingConfiguration.cs`
   - `SeriesConfigurationTests` (including a Series with and without a Finale, with and without its center, and a stored logo), plus the `EfAuditIntegrationTests` additions
   - Ships the full schema (status, nullable `advancing_count`, nullable `squad_id`, logo) now so phase 2 of #257 needs no data migration. The unique `tournament_id` index backs up rule #1.

5. **Add Series API contracts**
   - `src/Neba.Api.Contracts/Series/**` (`ISeriesApi` including the logo calls, inputs including `SeriesLogoInput` and `EditSeriesLogo*`, responses including `SeriesNextStageResponse`, `SeriesFinaleInput`, `SeriesFinaleResponse`, `SeriesFinaleSummaryResponse`, the `ListSeriesFinalesAsync` call and the logo fields)
   - Contract input and response factories in `tests/Neba.TestFactory/Series/`
   - One commit for the public shape, so the endpoint commits and the website both build against it.

6. **Add list and get Series endpoints**
   - `Features/Series/SeriesEndpointGroup.cs`, `Features/Series/ListSeries/*`, `Features/Series/ListSeriesFinales/*`, `Features/Series/GetSeries/*`
   - DTO factories (`SeriesSummaryDtoFactory`, `SeriesNextStageDtoFactory`, `SeriesDetailDtoFactory`, `SeriesStageDtoFactory`, `SeriesFinaleDtoFactory`, `SeriesFinaleSummaryDtoFactory`)
   - `ListSeriesQueryHandlerTests` (next Stage and cutoff), `ListSeriesFinalesQueryHandlerTests`, plus the read cases in `SeriesEndpointsTests` (including `GET /series/finales`)
   - Public reads first (the Series list and detail, and the Finale list the tournament page needs): they need no write path, and they let the create tests read back what they saved.

7. **Add create and edit Series endpoints**
   - `Features/Series/StageTournamentFactsReader.cs`, `SeriesStageInputMapper.cs` (including `ToLogo` and `ToFinale`), `SeriesStageInputValidator.cs`, `SeriesFinaleInputValidator.cs`, `SeriesFinaleChecks.cs`, `SeriesMutationResultSender.cs`, `Features/Series/CreateSeries/*`, `Features/Series/EditSeries/*`
   - Handler, validator and sender unit tests (including the Finale ones); the create and edit cases in `SeriesEndpointsTests`; `Series2025ReferenceTests` (with and without a Finale)
   - The whole-Series save is one idea; create and edit share the reader, mapper and sender. Both handle the logo field (claim check, denial check on edit, cleanup) so the save is complete before the logo-only endpoints arrive.

8. **Add delete Series endpoint**
   - `Features/Series/DeleteSeries/*`, `DeleteSeriesCommandHandlerTests`, the delete cases in `SeriesEndpointsTests`
   - Kept separate because it carries its own conventions: an idempotent 204, a 409 when locked, and the logo file cleanup.

9. **Add Series logo upload and logo-only edit**
   - `Features/Series/UploadSeriesLogo/*`, `Features/Series/EditSeriesLogo/*`, `EditSeriesLogoCommandHandlerTests`, `SeriesLogoEndpointsTests`
   - Mirrors the tournament logo flow, so a caller with only `Series.EditSeriesLogo` can change the logo, including on a locked Series.

10. **Block deleting a tournament that is a Series Stage**
    - `Features/Tournaments/Domain/TournamentErrors.cs`, `Features/Tournaments/DeleteTournament/DeleteTournamentCommandHandler.cs`, `DeleteTournamentSummary.cs`
    - The new cases in `DeleteTournamentCommandHandlerTests` and `DeleteTournamentEndpointTests`
    - A change to the Tournaments feature, so it gets its own commit for review. Without it, the FK fails as a 500.

11. **Show the Series on tournaments (API)**
    - `src/Neba.Api.Contracts/Tournaments/TournamentSeriesResponse.cs`, `Seasons/ListTournamentsInSeason/SeasonTournamentResponse.cs` and `Tournaments/GetTournament/TournamentDetailResponse.cs` (new optional `Series`)
    - `Features/Tournaments/TournamentSeriesDto.cs`, `TournamentSeriesReader.cs`, `TournamentSeriesDtoMapping.cs`, `ListTournamentsInSeason/*` and `GetTournament/*` (DTO, handler, mapping), `src/Neba.Api/Caching/CacheDescriptors.cs` (`Tournaments.Tag` public)
    - `IFusionCache` eviction in the Series create, edit, delete and logo handlers (`Features/Series/CreateSeries`, `EditSeries`, `DeleteSeries`, `EditSeriesLogo`)
    - `TournamentSeriesDtoFactory`, `TournamentSeriesResponseFactory`, `series` parameter on `SeasonTournamentDtoFactory` and `TournamentDetailDtoFactory`; `TournamentSeriesReaderTests`, handler tests, the eviction cases in the Series handler tests, and the list and detail cases in the tournament endpoint tests
    - Closes phase 1 of #257: a bowler can see which tournaments belong to a Series. It sits after the Series write endpoints because their handlers must evict the tournament caches, and it is a Tournaments feature change, so it gets its own commit for review.

12. **docs: update ubiquitous language - add Series terms**
    - `docs/ubiquitous-language.md` (new `## Series` section from the downloaded `vincent-hall-series-ul.md`, plus the `### Cup` clarification)
    - Brings the source doc into the repo so code names and their meaning are checked against one place. The subject follows the UL file's own commit format.

13. **Add Series admin help docs**
    - `docs/help/create-series.md`, `edit-series.md`, `delete-series.md`, `edit-series-logo.md` (text only)
    - Meets ADR-0007 alongside the API. Screenshots follow with the UI.

### Phase 2: UI

14. **Keep named ProblemDetails errors in ApiExecutor**
    - `src/Neba.Website.Server/Services/ApiFieldError.cs`, `Services/ApiExecutor.cs`, the `ApiExecutorTests` additions
    - A shared website change that other forms can reuse. Isolating it shows that existing callers see no difference.

15. **Share the logo edit modal between features**
    - `Components/EditLogoModal.razor(.css)` (moved from `Tournaments/EditTournamentLogoModal`), `Components/LogoEditButton.razor` (moved from `Tournaments/TournamentLogoEditButton`), `wwwroot/app.css` (`logo-edit-button`), `Tournaments/TournamentLogoEditor.razor`, `Tournaments/Schedule/Tournaments.razor`, `Tournaments/Detail/TournamentDetail.razor`
    - `tests/Neba.Website.Tests/Components/EditLogoModalTests.cs` (moved), `LogoEditButtonTests`, `TournamentLogoEditorTests` updates
    - A pure refactor with no behavior change for tournaments, kept apart so the Series pages can use it and review can confirm tournaments are untouched.

16. **Add Series view models and display rules**
    - `Series/StageKinds.cs`, `SeriesDisplay.cs`, the view models, `SeriesMappingExtensions.cs`, `SeriesFormModel.cs` (with `SeriesFinaleFormModel`), `SeriesStageRowModel.cs`, `SeriesTournamentOption.cs`, `SeasonOption.cs`, `SeriesInputMapping.cs`, `SeriesTournamentOptionsLoader.cs`
    - `Services/ApiServicesConfiguration.cs` (`ISeriesApi`) and `Program.cs` (loader registration)
    - View-model factories, plus `SeriesDisplayTests`, `SeriesMappingExtensionsTests`, `SeriesInputMappingTests`, `SeasonOptionTests`, `SeriesTournamentOptionsLoaderTests`
    - Pure C# with no markup, so the label rules (including "no Finale"), the Finale view models and form model, and the error routing are reviewed and tested before the pages use them.

17. **Add public Series list and detail pages**
    - `Series/SeriesList.razor(.css)`, `SeriesDetail.razor(.css)`, `SeriesFieldChart.razor(.css)`, `SeriesLogoTile.razor(.css)`, `SeriesLogoEditor.razor`, `wwwroot/app.css` (kind chips, locked badge), `Layout/NavMenu.razor`
    - `SeriesListTests`, `SeriesDetailTests`, `SeriesLogoTileTests`, `SeriesLogoEditorTests`, nav test
    - What bowlers see: the public landing pages with the logo and their nav entry, plus admin delete and logo edit.

18. **Show the Series on tournament list and detail (UI)**
    - `Tournaments/TournamentSeriesViewModel.cs`, `Tournaments/TournamentSeriesTag.razor(.css)`, `Tournaments/Detail/TournamentSeriesCallout.razor(.css)`, `Series/SeriesLogoTile.razor(.css)` (new `Tag` size), `Series/SeriesDisplay.cs` (`RoundLabel`, `CalloutRule`)
    - `Tournaments/Schedule/TournamentUpcomingCard.razor`, `TournamentPastCard.razor`, `SeasonTournamentViewModel.cs`, `Tournaments/Detail/TournamentDetail.razor`, `TournamentDetailViewModel.cs`, `TournamentDetailMappingExtensions.cs` and the schedule mapping
    - `TournamentSeriesViewModelFactory`, `TournamentSeriesTagTests`, `TournamentSeriesCalloutTests`, `SeriesDisplayTests` additions, and the card and detail cases in the existing tournament page tests
    - What bowlers see on the pages they already use. Separate from the Series pages so review of the Tournaments pages stays small.

19. **Show the Finale on the tournament list (UI)**
    - `Tournaments/Schedule/SeriesFinaleItemViewModel.cs`, `ScheduleEntry.cs`, `UpcomingSchedule.cs` (merge and grouping), `SeriesFinaleCard.razor(.css)`, `Tournaments.razor`, `Series/SeriesLogoTile.razor(.css)` (`Card` size), `Series/SeriesDisplay.cs` (`FinaleName` is already there from the Series view models), `wwwroot/app.css` (`--kind-finale`)
    - `SeriesFinaleItemViewModelFactory`, `SeriesFinaleCardTests`, `UpcomingScheduleTests` additions, the Finale cases in `TournamentsTests`
    - Its own commit because it changes how the schedule page builds its list (merged entries, filters, a second API call), unlike the tag and callout, which only add markup to existing cards.

20. **Add Series create and edit pages**
    - `Series/CreateSeries.razor`, `EditSeries.razor`, `SeriesEditor.razor`, `SeriesFinaleSection.razor`, `SeriesStageGrid.razor(.css)`
    - `SeriesEditorTests`, `SeriesStageGridTests`, `SeriesFinaleSectionTests`
    - The admin data-entry flow, kept apart from the public pages because it carries the dirty guard, row errors, the Finale section (`SeriesFinaleSection.razor`, the host picker), the logo upload and lock handling.

21. **Add Series end-to-end tests**
    - `tests/e2e/mock-api/mock-api-server.ts`, `tests/e2e/Series.spec.ts`
    - Proves the real browser flows (public view with and without a Finale, create, a row error from a 4xx, delete, logo edit) over HTTP. Updates the mock with every required contract field so Playwright doesn't fail on a missing one.

22. **Add Series help screenshots**
    - Screenshot images and the `docs/help/*-series*.md` image references (via the `help-documentation` skill)
    - Screenshots need the finished pages, so they come last.
