using Neba.Api.Features.BowlingCenters.Domain;

namespace Neba.Api.Features.Series.Domain;

/// <summary>The Series' last event, held outside the system after the Placement Round.</summary>
public sealed record SeriesFinale
{
    /// <summary>How many bowlers from the Placement Round go to the Finale.</summary>
    public int FinalistCount { get; init; }

    /// <summary>The day the Finale is bowled.</summary>
    public DateOnly Date { get; init; }

    /// <summary>The start time, local to the bowling center.</summary>
    public TimeOnly StartTime { get; init; }

    /// <summary>Where it is held; null until announced.</summary>
    public CertificationNumber? BowlingCenterId { get; init; }

    private SeriesFinale()
    { }

    internal static SeriesFinale Describe(SeriesFinaleDefinition definition)
        => new()
        {
            FinalistCount = definition.FinalistCount,
            Date = definition.Date,
            StartTime = definition.StartTime,
            BowlingCenterId = definition.BowlingCenterId,
        };
}
