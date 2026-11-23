using Neba.Api.Features.Seasons.Domain;
using Neba.Api.Features.Series.Domain;
using Neba.Api.Features.Series.ListSeriesFinales;

namespace Neba.TestFactory.Series;

internal static class SeriesFinaleSummaryDtoFactory
{
    public static SeriesFinaleSummaryDto Create(
        SeriesId? seriesId = null,
        string? seriesName = null,
        SeasonId? seasonId = null,
        Uri? logoUrl = null,
        SeriesFinaleScheduleDto? finale = null)
        => new()
        {
            SeriesId = seriesId ?? SeriesId.New(),
            SeriesName = seriesName ?? SeriesFactory.ValidName,
            SeasonId = seasonId ?? SeasonId.New(),
            LogoUrl = logoUrl,
            Finale = finale ?? SeriesFinaleScheduleDtoFactory.Create(),
        };

    public static IReadOnlyCollection<SeriesFinaleSummaryDto> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            seriesName: $"{faker.Company.CompanyName()} Series",
            finale: SeriesFinaleScheduleDtoFactory.Bogus(1, faker.Random.Int()).Single()))];
    }
}