namespace Neba.Api.Contracts.Series.CreateStageOverride;

/// <summary>Created stage override response.</summary>
public sealed record CreatedStageOverrideResponse
{
    /// <summary>The override id.</summary>
    public required string OverrideId { get; init; }

    /// <summary>The Version after the write.</summary>
    public required int Version { get; init; }
}