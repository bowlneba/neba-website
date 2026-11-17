using Neba.Api.Contracts.Series.CreateSeries;

namespace Neba.Api.Contracts.Series.EditSeriesLogo;

/// <summary>Edit series logo input.</summary>
public sealed record EditSeriesLogoInput
{
    /// <summary>Null removes the logo.</summary>
    public SeriesLogoInput? Logo { get; init; }
}
