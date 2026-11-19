using Neba.Api.Features.BowlingCenters.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>The submitted Finale. Null on the command means the Series has no Finale.</summary>
public sealed record SeriesFinaleDefinition
{
    /// <summary>Finalists from the Placement Round.</summary>
    public required int FinalistCount { get; init; }
    /// <summary>The Finale date.</summary>
    public required DateOnly Date { get; init; }
    /// <summary>The Finale start time.</summary>
    public required TimeOnly StartTime { get; init; }
    /// <summary>Host center, or null until announced.</summary>
    public CertificationNumber? BowlingCenterId { get; init; }
}