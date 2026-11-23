namespace Neba.Api.Contracts.Series.MakeStageOfficial;

/// <summary>Make stage official input.</summary>
public sealed record MakeStageOfficialInput
{
    /// <summary>The Version from the last GET.</summary>
    public required int Version { get; init; }
}
