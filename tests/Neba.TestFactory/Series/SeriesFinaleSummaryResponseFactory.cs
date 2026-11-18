using Neba.Api.Contracts.Series.GetSeries;
using Neba.Api.Contracts.Series.ListSeriesFinales;

namespace Neba.TestFactory.Series;

public static class SeriesFinaleSummaryResponseFactory
{
    public const string ValidSeriesId = "01JBQ5Y7K3M9N2P4R6T8V0W1X4";
    public const string ValidSeriesName = "Vincent Hall Memorial Series";
    public const string ValidSeasonId = SeriesInputFactory.ValidSeasonId;

    public static SeriesFinaleSummaryResponse Create(
        string? seriesId = null,
        string? seriesName = null,
        string? seasonId = null,
        Uri? logoUrl = null,
        SeriesFinaleResponse? finale = null)
        => new()
        {
            SeriesId = seriesId ?? ValidSeriesId,
            SeriesName = seriesName ?? ValidSeriesName,
            SeasonId = seasonId ?? ValidSeasonId,
            LogoUrl = logoUrl,
            Finale = finale ?? SeriesFinaleResponseFactory.Create(),
        };

    public static IReadOnlyCollection<SeriesFinaleSummaryResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ => Create(
            seriesId: Ulid.BogusString(faker),
            seriesName: $"{faker.Company.CompanyName()} Series",
            seasonId: Ulid.BogusString(faker),
            finale: SeriesFinaleResponseFactory.Bogus(1, faker.Random.Int()).Single()))];
    }
}