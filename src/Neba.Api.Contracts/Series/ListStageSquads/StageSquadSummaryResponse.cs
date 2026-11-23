namespace Neba.Api.Contracts.Series.ListStageSquads;

/// <summary>A squad of a Stage's tournament.</summary>
public sealed record StageSquadSummaryResponse
{
    /// <summary>The squad id.</summary>
    public required string SquadId { get; init; }

    /// <summary>The Eastern date.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>The Eastern start time.</summary>
    public required TimeOnly StartTime { get; init; }

    /// <summary>True when the squad has score rows.</summary>
    public required bool HasScores { get; init; }

    /// <summary>True when the Stage uses this squad.</summary>
    public required bool IsSelected { get; init; }
}