namespace Neba.Api.Contracts.Series.CreateStageOverride;

/// <summary>Create stage override request.</summary>
public sealed record CreateStageOverrideRequest
{
    /// <summary>The series id.</summary>
    public required string Id { get; init; }

    /// <summary>The Stage id.</summary>
    public required string StageId { get; init; }

    /// <summary>The override.</summary>
    public required CreateStageOverrideInput StageOverride { get; init; }
}