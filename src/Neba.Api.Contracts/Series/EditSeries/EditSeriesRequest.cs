using Neba.Api.Contracts.Series.CreateSeries;

namespace Neba.Api.Contracts.Series.EditSeries;

/// <summary>Edit series request.</summary>
public sealed record EditSeriesRequest
{
    /// <summary>The id.</summary>
    public required string Id { get; init; }
    /// <summary>The series.</summary>
    public required EditSeriesInput Series { get; init; }
}