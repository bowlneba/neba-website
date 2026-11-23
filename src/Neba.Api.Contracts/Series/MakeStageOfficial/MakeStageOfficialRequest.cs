namespace Neba.Api.Contracts.Series.MakeStageOfficial;

/// <summary>Make stage official request.</summary>
public sealed record MakeStageOfficialRequest
{
    /// <summary>The series id.</summary>
    public required string Id { get; init; }

    /// <summary>The Stage id.</summary>
    public required string StageId { get; init; }

    /// <summary>The confirmation that carries the Version.</summary>
    public required MakeStageOfficialInput Official { get; init; }
}
