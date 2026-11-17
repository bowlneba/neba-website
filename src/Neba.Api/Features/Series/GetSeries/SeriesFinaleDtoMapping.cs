using Neba.Api.Contracts.Series.GetSeries;

namespace Neba.Api.Features.Series.GetSeries;

internal static class SeriesFinaleDtoMapping
{
    extension(SeriesFinaleDto finale)
    {
        public SeriesFinaleResponse ToResponse()
            => new()
            {
                FinalistCount = finale.FinalistCount,
                Date = finale.Date,
                StartTime = finale.StartTime,
                BowlingCenterId = finale.BowlingCenterId,
                BowlingCenterName = finale.BowlingCenterName,
                BowlingCenterCity = finale.BowlingCenterCity,
                BowlingCenterState = finale.BowlingCenterState,
            };
    }
}
