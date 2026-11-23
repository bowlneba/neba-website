using ErrorOr;

using Neba.Api.Features.Tournaments.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>One tournament's role in a Series.</summary>
public sealed class SeriesStage
{
    /// <summary>Unique ID, stable across edits.</summary>
    public required SeriesStageId Id { get; init; }

    /// <summary>1-based position within the Series. Follows tournament start date.</summary>
    public int Order { get; private set; }

    /// <summary>The Stage kind.</summary>
    public StageKind Kind { get; private set; } = StageKind.Qualifying;

    /// <summary>The tournament for this Stage, by ID only.</summary>
    public TournamentId TournamentId { get; private set; }

    /// <summary>
    /// Top N finishers (Qualifying; teams in a team event) or bowlers advancing (Survivor): required and above zero.
    /// Always null on the Placement Stage; the Finale carries its own count.
    /// </summary>
    public int? AdvancingCount { get; private set; }

    /// <summary>
    /// Processing state. Advancement sets it through <see cref="MarkComputed"/> and <see cref="MarkOfficial"/>. The setter
    /// stays internal as a test seam.
    /// </summary>
    public StageStatus Status { get; internal set; } = StageStatus.Pending;

    /// <summary>Set for a Survivor Stage, otherwise null.</summary>
    public SquadDesignation? SquadDesignation { get; private set; }

    /// <summary>True when the Stage's inputs may have changed since it was computed.</summary>
    public bool IsStale { get; private set; }

    /// <summary>When the Stage was computed (offset 0). Null while pending.</summary>
    public DateTimeOffset? ComputedAt { get; private set; }

    /// <summary>When the Stage became Official (offset 0). Null unless Official.</summary>
    public DateTimeOffset? OfficialAt { get; private set; }

    /// <summary>Number of slots the Stage filled or must fill. Null until computed.</summary>
    public int? SlotCount { get; private set; }

    private readonly List<SurvivorOutcome> _survivorOutcomes = [];

    /// <summary>The Survivor Round results. Empty on other Stage kinds.</summary>
    public IReadOnlyCollection<SurvivorOutcome> SurvivorOutcomes
        => _survivorOutcomes.AsReadOnly();

    private readonly List<StageOverride> _overrides = [];

    /// <summary>The admin overrides on this Stage.</summary>
    public IReadOnlyCollection<StageOverride> Overrides
        => _overrides.AsReadOnly();

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
        var errors = new[]
        {
            AdvancingCountError(order, definition, facts),
            SquadError(order, definition, facts),
        }.OfType<Error>().ToList();

        var hasSquad = definition.SquadDate is not null && definition.SquadStartTime is not null;

        if (errors.Count > 0)
        {
            return errors;
        }

        Order = order;
        Kind = definition.Kind;
        TournamentId = definition.TournamentId;
        AdvancingCount = definition.AdvancingCount;
        SquadDesignation = hasSquad ? DescribeKeepingSquad(definition) : null;

        return Result.Updated;
    }

    // A resolved squad survives an update that leaves the date and start time alone.
    private SquadDesignation DescribeKeepingSquad(SeriesStageDefinition definition)
        => SquadDesignation is { } current
            && current.Date == definition.SquadDate
            && current.StartTime == definition.SquadStartTime
            ? current
            : SquadDesignation.Describe(definition.SquadDate!.Value, definition.SquadStartTime!.Value);

    internal void MarkComputed(DateTimeOffset now)
    {
        Status = StageStatus.Computed;
        ComputedAt = now;
        OfficialAt = null;
        IsStale = false;
    }

    internal void MarkOfficial(DateTimeOffset now)
    {
        Status = StageStatus.Official;
        OfficialAt = now;
    }

    internal void MarkStale()
        => IsStale = true;

    internal void ClearStale()
        => IsStale = false;

    internal void SetSlotCount(int? slotCount)
        => SlotCount = slotCount;

    internal void ResolveSquad(SquadId squadId)
        => SquadDesignation = SquadDesignation?.Resolve(squadId);

    internal void UnresolveSquad()
        => SquadDesignation = SquadDesignation?.Unresolve();

    internal void AddOverride(StageOverride stageOverride)
        => _overrides.Add(stageOverride);

    internal void RemoveOverride(StageOverride stageOverride)
        => _overrides.Remove(stageOverride);

    internal void ApplyOverrideEffect(OverrideEffect effect)
        => _overrides.Single(o => o.Id == effect.OverrideId).Applied = effect.Applied;

    // Updates in place by bowler (the key is composite), removes the rest, adds new ones.
    internal void ReplaceOutcomes(IReadOnlyList<SurvivorOutcomeDraft> target)
    {
        _survivorOutcomes.RemoveAll(o => target.All(t => t.BowlerId != o.BowlerId));

        foreach (var draft in target)
        {
            var existing = _survivorOutcomes.Find(o => o.BowlerId == draft.BowlerId);
            if (existing is null)
            {
                _survivorOutcomes.Add(SurvivorOutcome.Create(draft.BowlerId, draft.BlockTotal, draft.Advanced).Value);
            }
            else
            {
                _ = existing.Update(draft.BlockTotal, draft.Advanced).Value; // computed data is valid; an error here is a bug
            }
        }
    }

    // #8: required and above zero on Qualifying and Survivor; a Placement Stage has none (the Finale carries its own count).
    private static Error? AdvancingCountError(int order, SeriesStageDefinition definition, StageTournamentFacts facts)
    {
        if (definition.Kind == StageKind.Placement)
        {
            return definition.AdvancingCount is null
                ? null
                : SeriesErrors.AdvancingCountNotAllowed(order, facts.TournamentName);
        }

        if (definition.AdvancingCount is null)
        {
            return SeriesErrors.AdvancingCountRequired(order, facts.TournamentName, definition.Kind);
        }

        return definition.AdvancingCount <= 0
            ? SeriesErrors.AdvancingCountNotPositive(order, facts.TournamentName)
            : null;
    }

    private static Error? SquadError(int order, SeriesStageDefinition definition, StageTournamentFacts facts)
    {
        var hasSquad = definition.SquadDate is not null && definition.SquadStartTime is not null;

        if (definition.Kind == StageKind.Survivor)
        {
            if (!hasSquad)
            {
                return SeriesErrors.SquadRequired(order, facts.TournamentName);
            }
        }
        else if (definition.SquadDate is not null || definition.SquadStartTime is not null)
        {
            return SeriesErrors.SquadNotAllowed(order, facts.TournamentName, definition.Kind);
        }

        return hasSquad && (definition.SquadDate < facts.StartDate || definition.SquadDate > facts.EndDate)
            ? SeriesErrors.SquadDateOutsideTournament(order, facts.TournamentName, definition.SquadDate.Value, facts.StartDate, facts.EndDate)
            : null;
    }

    internal bool Matches(int order, SeriesStageDefinition definition)
        => Order == order && MatchesIgnoringOrder(definition);

    internal bool MatchesIgnoringOrder(SeriesStageDefinition definition)
        => Kind == definition.Kind
            && TournamentId == definition.TournamentId
            && AdvancingCount == definition.AdvancingCount
            && SquadDesignation?.Date == definition.SquadDate
            && SquadDesignation?.StartTime == definition.SquadStartTime;
}