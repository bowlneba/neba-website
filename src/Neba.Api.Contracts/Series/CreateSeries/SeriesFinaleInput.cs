namespace Neba.Api.Contracts.Series.CreateSeries;

/// <summary>Series finale input.</summary>
public sealed record SeriesFinaleInput
{
    /// <summary>Bowlers from the Placement Round who go to the Finale.</summary>
    public required int FinalistCount { get; init; }

    /// <summary>Nullable so a missing date is a 400 naming the field, not a default date.</summary>
    public DateOnly? Date { get; init; }
    /// <summary>The start time.</summary>
    public TimeOnly? StartTime { get; init; }

    /// <summary>Certification number of the host bowling center, or null while it is to be announced.</summary>
    public string? BowlingCenterId { get; init; }
}
