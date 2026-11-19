namespace Neba.Api.Contracts.Series.CreateSeries;

/// <summary>Series input.</summary>
public sealed record SeriesInput
{
    /// <summary>The name.</summary>
    public required string Name { get; init; }
    /// <summary>The season id.</summary>
    public required string SeasonId { get; init; }

    /// <summary>A file staged through <c>POST /series/logo</c>, or null for no logo.</summary>
    public SeriesLogoInput? Logo { get; init; }

    /// <summary>Stages in order. Order is the list position.</summary>
    public required IReadOnlyList<SeriesStageInput> Stages { get; init; }

    /// <summary>The Finale, or null for a Series with none.</summary>
    public SeriesFinaleInput? Finale { get; init; }
}