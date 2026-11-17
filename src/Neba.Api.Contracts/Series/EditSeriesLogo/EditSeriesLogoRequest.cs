using Neba.Api.Contracts.Series.CreateSeries;

namespace Neba.Api.Contracts.Series.EditSeriesLogo;

/// <summary>Edit series logo request.</summary>
public sealed record EditSeriesLogoRequest
{
    /// <summary>The id.</summary>
    public required string Id { get; init; }
    /// <summary>The input.</summary>
    public required EditSeriesLogoInput Input { get; init; }
}
