namespace Neba.Api.Contracts.Series.PickStageSquad;

/// <summary>Pick stage squad request.</summary>
public sealed record PickStageSquadRequest
{
    /// <summary>The series id.</summary>
    public required string Id { get; init; }

    /// <summary>The Stage id.</summary>
    public required string StageId { get; init; }

    /// <summary>The squad pick.</summary>
    public required PickStageSquadInput Squad { get; init; }
}