using Neba.Api.Contracts.Series.CreateSeries;

namespace Neba.Api.Contracts.Series.EditSeries;

/// <summary>No season: it is fixed after creation.</summary>
public sealed record EditSeriesInput
{
    /// <summary>The name.</summary>
    public required string Name { get; init; }

    /// <summary>The current logo (resent unchanged), a newly staged file, or null to remove it.</summary>
    public SeriesLogoInput? Logo { get; init; }
    /// <summary>The stages.</summary>
    public required IReadOnlyList<SeriesStageInput> Stages { get; init; }

    /// <summary>The Finale, or null to remove it (refused while locked).</summary>
    public SeriesFinaleInput? Finale { get; init; }
}