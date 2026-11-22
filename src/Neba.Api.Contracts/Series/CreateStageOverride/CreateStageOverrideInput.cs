namespace Neba.Api.Contracts.Series.CreateStageOverride;

/// <summary>Create stage override input.</summary>
public sealed record CreateStageOverrideInput
{
    /// <summary>The kind value: see <see cref="OverrideKindValues"/>.</summary>
    public required int Kind { get; init; }

    /// <summary>The bowler id.</summary>
    public required string BowlerId { get; init; }

    /// <summary>Why, up to 500 characters.</summary>
    public required string Reason { get; init; }

    /// <summary>The Version from the last GET.</summary>
    public required int Version { get; init; }
}