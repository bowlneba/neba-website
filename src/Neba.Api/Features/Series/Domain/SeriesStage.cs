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
        SquadDesignation = hasSquad
            ? SquadDesignation.Describe(definition.SquadDate!.Value, definition.SquadStartTime!.Value)
            : null;

        return Result.Updated;
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
        => Order == order
            && Kind == definition.Kind
            && TournamentId == definition.TournamentId
            && AdvancingCount == definition.AdvancingCount
            && SquadDesignation?.Date == definition.SquadDate
            && SquadDesignation?.StartTime == definition.SquadStartTime;
}