using Neba.Api.Contracts.Series.GetSeries;

namespace Neba.TestFactory.Series;

public static class SeriesDetailResponseFactory
{
    public const string ValidId = SeriesSummaryResponseFactory.ValidId;
    public const string ValidName = SeriesSummaryResponseFactory.ValidName;

    /// <summary>One Qualifying Stage, no Finale and no logo.</summary>
    public static SeriesDetailResponse Create(
        string? id = null,
        string? name = null,
        string? seasonId = null,
        string? seasonDescription = null,
        DateOnly? seasonStartDate = null,
        DateOnly? seasonEndDate = null,
        bool? isLocked = null,
        SeriesFinaleResponse? finale = null,
        Uri? logoUrl = null,
        string? logoContainer = null,
        string? logoPath = null,
        string? logoContentType = null,
        long? logoSizeInBytes = null,
        IReadOnlyCollection<SeriesStageResponse>? stages = null)
        => new()
        {
            Id = id ?? ValidId,
            Name = name ?? ValidName,
            SeasonId = seasonId ?? SeriesSummaryResponseFactory.ValidSeasonId,
            SeasonDescription = seasonDescription ?? SeriesSummaryResponseFactory.ValidSeasonDescription,
            SeasonStartDate = seasonStartDate ?? SeriesSummaryResponseFactory.ValidSeasonStartDate,
            SeasonEndDate = seasonEndDate ?? SeriesSummaryResponseFactory.ValidSeasonEndDate,
            IsLocked = isLocked ?? false,
            Finale = finale,
            LogoUrl = logoUrl,
            LogoContainer = logoContainer,
            LogoPath = logoPath,
            LogoContentType = logoContentType,
            LogoSizeInBytes = logoSizeInBytes,
            Stages = stages ?? [SeriesStageResponseFactory.Create()],
        };

    public static IReadOnlyCollection<SeriesDetailResponse> Bogus(int count, int? seed = null)
    {
        var faker = new Faker();
        if (seed.HasValue) faker.Random = new Randomizer(seed.Value);

        return [.. Enumerable.Range(0, count).Select(_ =>
        {
            var year = faker.Random.Int(2020, 2030);
            return Create(
                id: Ulid.BogusString(faker),
                name: $"{year} {faker.Company.CompanyName()} Series",
                seasonId: Ulid.BogusString(faker),
                seasonDescription: $"{year} Season",
                seasonStartDate: new DateOnly(year, 1, 1),
                seasonEndDate: new DateOnly(year, 12, 31),
                isLocked: faker.Random.Bool(),
                finale: faker.Random.Bool() ? SeriesFinaleResponseFactory.Bogus(1, faker.Random.Int()).Single() : null,
                stages: SeriesStageResponseFactory.Bogus(faker.Random.Int(1, 11), faker.Random.Int()));
        })];
    }
}
