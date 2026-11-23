using System.Diagnostics.CodeAnalysis;

using ErrorOr;

using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Storage.Domain;
using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>
/// A season-scoped competition made up of ordered Stages, each linked to one tournament, and an
/// optional Finale (the Vincent Hall Memorial Series).
/// </summary>
[SuppressMessage("Naming", "CA1724:Type names should not match namespaces",
    Justification = "Series is the established domain term and the feature folder is named after it.")]
public sealed partial class Series
{
    internal const int NameMaxLength = 127;

    /// <summary>Unique ID.</summary>
    public required SeriesId Id { get; init; }

    /// <summary>The Series name.</summary>
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

    /// <summary>The as-of date for the Backfill (the last Qualifying tournament's end date). Null until that Stage computes.</summary>
    public DateOnly? BackfillAsOf { get; private set; }

    /// <summary>Optimistic concurrency token. Advancement writes bump it, including writes that touch only child rows.</summary>
    public int Version { get; private set; }

    private readonly List<SeriesFieldEntry> _fieldEntries = [];

    /// <summary>The Series Field: bowlers who qualified, and from where.</summary>
    public IReadOnlyCollection<SeriesFieldEntry> FieldEntries
        => _fieldEntries.AsReadOnly();

    private readonly List<SeriesPosition> _positions = [];

    /// <summary>The Placement Round positions.</summary>
    public IReadOnlyCollection<SeriesPosition> Positions
        => _positions.AsReadOnly();

    /// <summary>Refuses a write made from a stale screen.</summary>
    public ErrorOr<Success> EnsureVersion(int expected)
        => Version == expected ? Result.Success : SeriesAdvancementErrors.VersionConflict;

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
        if (applied.IsError)
        {
            return applied.Errors;
        }

        series.Version = 0; // the first Stage list is not a change to a stored Series

        return series;
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
        ArgumentNullException.ThrowIfNull(stages);
        ArgumentNullException.ThrowIfNull(tournaments);

        var stagesUnchanged = StagesMatch(stages);
        var finaleShapeUnchanged = (Finale is null) == (finale is null) && Finale?.FinalistCount == finale?.FinalistCount;

        var cancelledRemoval = !stagesUnchanged && IsOnlyCancelledPendingRemoval(stages, tournaments);

        if (IsLocked && !((stagesUnchanged || cancelledRemoval) && finaleShapeUnchanged))
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
            : [.. candidates.Select(c => c.Facts)];

        errors.AddRange(CheckFinale(finale, lastSurvivorCount, stageFacts));

        if (errors.Count > 0)
        {
            return errors;
        }

        if (!stagesUnchanged)
        {
            ApplyStages(stages, candidates);
            Version++;
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

    // Locked Series: the submitted list is the stored list minus Pending Stages whose tournament is Cancelled.
    private bool IsOnlyCancelledPendingRemoval(
        IReadOnlyList<SeriesStageDefinition> stages,
        IReadOnlyDictionary<TournamentId, StageTournamentFacts> tournaments)
    {
        if (stages.Any(definition => definition.StageId is null))
        {
            return false;
        }

        var keptIds = stages.Select(definition => definition.StageId!.Value).ToHashSet();
        var removed = _stages.Where(stage => !keptIds.Contains(stage.Id)).ToList();

        if (removed.Count == 0 || !removed.TrueForAll(stage => stage.Status == StageStatus.Pending
                && tournaments.TryGetValue(stage.TournamentId, out var facts)
                && facts.Status == TournamentStatus.Cancelled))
        {
            return false;
        }

        var kept = _stages.Where(stage => keptIds.Contains(stage.Id)).OrderBy(stage => stage.Order).ToList();

        return kept.Count == stages.Count
            && stages.Select((definition, index) => kept[index].Id == definition.StageId && kept[index].MatchesIgnoringOrder(definition)).All(same => same);
    }

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
                errors.Add(SeriesErrors.StageNotFound(order));
                continue;
            }

            if (!tournaments.TryGetValue(definition.TournamentId, out var facts))
            {
                errors.Add(SeriesErrors.TournamentNotFound(order));
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
        List<(int Order, SeriesStageDefinition Definition, StageTournamentFacts Facts)> stages)
    {
        var placementSeen = false;
        int? lastSurvivorCount = null;

        for (var i = 0; i < stages.Count; i++)
        {
            var (order, definition, facts) = stages[i];

            foreach (var error in CheckAgainstPrevious(stages, i))
            {
                yield return error;
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
                if (CheckPlacement(placementSeen, i == stages.Count - 1, order, facts.TournamentName) is { } placementError)
                {
                    yield return placementError;
                }

                placementSeen = true;
            }
        }
    }

    private static Error? CheckPlacement(bool placementSeen, bool isLast, int order, string tournamentName)
    {
        if (placementSeen)
        {
            return SeriesErrors.MultiplePlacementStages(order, tournamentName);
        }

        return isLast ? null : SeriesErrors.PlacementNotLast(order, tournamentName);
    }

    private static IEnumerable<Error> CheckAgainstPrevious(
        List<(int Order, SeriesStageDefinition Definition, StageTournamentFacts Facts)> stages, int index)
    {
        if (index == 0)
        {
            yield break;
        }

        var (order, definition, facts) = stages[index];
        var (_, previousDefinition, previousFacts) = stages[index - 1];

        if (definition.Kind.Value < previousDefinition.Kind.Value)
        {
            yield return SeriesErrors.KindOutOfOrder(order, facts.TournamentName, definition.Kind, previousDefinition.Kind);
        }

        if (facts.StartDate < previousFacts.StartDate)
        {
            yield return SeriesErrors.DateBeforePrevious(order, facts.TournamentName, facts.StartDate, previousFacts.StartDate);
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