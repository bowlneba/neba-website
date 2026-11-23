namespace Neba.Api.Contracts.Series.PickStageSquad;

/// <summary>Pick stage squad input.</summary>
public sealed record PickStageSquadInput
{
    /// <summary>The squad id.</summary>
    public required string SquadId { get; init; }

    /// <summary>The Version from the last GET.</summary>
    public required int Version { get; init; }
}