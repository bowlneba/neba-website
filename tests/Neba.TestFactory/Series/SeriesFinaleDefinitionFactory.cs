using Neba.Api.Features.BowlingCenters.Domain;
using Neba.Api.Features.Series.Domain;

namespace Neba.TestFactory.Series;

public static class SeriesFinaleDefinitionFactory
{
    public const int ValidFinalistCount = 4;
    public static readonly DateOnly ValidDate = new(2025, 9, 27);
    public static readonly TimeOnly ValidStartTime = new(14, 0);

    /// <summary>Four finalists on 9/27/2025 at 2:00 PM, no host center yet.</summary>
    public static SeriesFinaleDefinition Create(
        int? finalistCount = null,
        DateOnly? date = null,
        TimeOnly? startTime = null,
        CertificationNumber? bowlingCenterId = null)
        => new()
        {
            FinalistCount = finalistCount ?? ValidFinalistCount,
            Date = date ?? ValidDate,
            StartTime = startTime ?? ValidStartTime,
            BowlingCenterId = bowlingCenterId,
        };
}
